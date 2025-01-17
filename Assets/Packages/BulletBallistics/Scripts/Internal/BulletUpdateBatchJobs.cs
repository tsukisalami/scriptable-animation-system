using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Ballistics
{
    /// bullet update jobs

    [BurstCompile]
    public struct InitializeUpdateJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> Indices;
        [NativeDisableParallelForRestriction] public NativeArray<BulletNative> Bullets;
        [WriteOnly] public NativeArray<RaycastCommand> Raycasts;
        public float timeStep;

        public void Execute(int id)
        {
            var index = Indices[id];
            ref var nativeData = ref Bullets.GetRef(index);
            nativeData.FlyTime = math.min(nativeData.LifeTime, timeStep);
            nativeData.LifeTime -= timeStep;
            Raycasts[id] = nativeData.GetRaycastCommand();
        }
    }

    [BurstCompile]
    public struct ProcessInteractionsJob : IJobParallelFor
    {
        public const float MinimumRicochetSpeedSqr = .001f;
        [ReadOnly] public NativeArray<BulletInteraction> Interactions;
        [NativeDisableParallelForRestriction] public NativeArray<BulletNative> Bullets;
        [WriteOnly] public NativeArray<int> Indices;
        [WriteOnly] public NativeArray<RaycastCommand> Raycasts;
        [ReadOnly] public Environment Environment;
        [ReadOnly] public uint BaseSeed;

        public static void UpdateVelocity(ref BulletNative bullet, float distanceTraveled, float timeTraveled, in Environment environment)
        {
            if (bullet.EnergyLossPerUnit >= 0) {
                var newEnergy = bullet.Energy - bullet.EnergyLossPerUnit * distanceTraveled;
                if (newEnergy <= 0) {
                    bullet.Stop();
                } else {
                    bullet.Velocity = math.sqrt(2 * newEnergy / bullet.Mass) * math.normalize(bullet.Velocity);
                }
            } else {
                if (environment.EnableGravity) {
                    bullet.Velocity += environment.Gravity * timeTraveled;
                }
#if !BB_NO_AIR_RESISTANCE
                if (environment.EnableAirResistance) {
                    // approximate bullet as a solid sphere
                    var relativeAirVelocity = bullet.Velocity - environment.WindVelocity;
                    var relativeAirSpeedSqr = math.lengthsq(relativeAirVelocity);
                    var drag = relativeAirSpeedSqr * timeTraveled * bullet.PrecalculatedDrag * environment.AirDensity;
                    bullet.Velocity -= math.normalize(relativeAirVelocity) * drag;
#if !BB_NO_SPIN
                    if (environment.EnableBulletSpin) {
                        var relativeSpin = math.mul(quaternion.LookRotation(bullet.Velocity, math.up()), bullet.Spin);
                        var f = (4f / 3f) * math.PI * environment.AirDensity * bullet.Radius * bullet.Radius * bullet.Radius * math.cross(relativeAirVelocity, relativeSpin);
                        bullet.Velocity += timeTraveled * (f / bullet.Mass);

                        // inertia solid sphere (2/5) * mass * radius * radius
                        var inertia = (2f / 5f) * bullet.Radius * bullet.Radius * bullet.Mass;
                        // viscous torque on sphere 8 * pi * radius ^ 3 * viscosity * angularVelocity
                        var viscousTorque = -8f * math.PI * bullet.Radius * bullet.Radius * bullet.Radius * bullet.Spin * environment.AirViscosity;
                        bullet.Spin += (viscousTorque / inertia) * timeTraveled;
                    }
#endif
                }
#endif
            }
        }

        [BurstCompile]
        public static void SpreadBulletPath(ref BulletNative bullet, in float3 surfaceNormal, float spread, ref Unity.Mathematics.Random rng)
        {
            if (spread == 0)
                return;
            var forward = math.normalize(bullet.Velocity);
            var right = math.cross(forward, math.up());
            var vel = math.mul(quaternion.AxisAngle(forward, rng.NextFloat(0f, math.PI)), math.mul(quaternion.AxisAngle(right, rng.NextFloat(-spread, spread)), bullet.Velocity));
            var t = math.dot(vel, surfaceNormal); // ensure velocity point to same side of impact normal as before
            if (t <= BallisticsUtil.Epsilon)
                vel -= surfaceNormal * (t - BallisticsUtil.Epsilon);
            bullet.Velocity = vel;
        }

        public void Execute(int jobID)
        {
            var interaction = Interactions[jobID];
            Indices[jobID] = interaction.Index;
            if (interaction.Index < 0)
                return;
            ref var bullet = ref Bullets.GetRef(interaction.Index);
#if !BB_NO_DISTANCE_TRACKING
            var fromPosition = bullet.Position;
#endif
            var rng = Unity.Mathematics.Random.CreateFromIndex(BaseSeed + (uint)jobID);

            // if exit flag is set, the bullet exit the material and then enters the next one;
            // it can never enter and then exit the same collider in a single update step
            if ((interaction.Flags & InteractionFlags.EXIT) == InteractionFlags.EXIT) {
                var speed = bullet.Speed;
                var distance = math.distance(bullet.Position, interaction.ExitPoint);
                var timeInside = distance / speed;
                bullet.Position = interaction.ExitPoint;
                bullet.Position += math.float3(interaction.ExitNormal) * BallisticsUtil.Epsilon; // move slightly outside of collider to prevent double collision (when queries-hit-backfaces is enabled)
                bullet.FlyTime = math.max(0, bullet.FlyTime - timeInside);
                UpdateVelocity(ref bullet, distance, timeInside, Environment);
                bullet.EnergyLossPerUnit = -1;

                SpreadBulletPath(ref bullet, interaction.ExitNormal, interaction.ExitSpreadAngle, ref rng);
                Raycasts[jobID] = bullet.GetRaycastCommand();
            }

            if ((interaction.Flags & InteractionFlags.HIT) == InteractionFlags.HIT) {
                var distanceTraveled = math.distance(bullet.Position, interaction.EntryPoint);
                var timeTraveled = distanceTraveled / bullet.Speed;
                bullet.Position = interaction.EntryPoint;
                bullet.Velocity *= interaction.SpeedFactor;
                bullet.FlyTime = math.max(0, bullet.FlyTime - timeTraveled);
                UpdateVelocity(ref bullet, distanceTraveled, timeTraveled, Environment);
                if ((interaction.Flags & InteractionFlags.ENTRY) == InteractionFlags.ENTRY) { // material penetration
                    bullet.Position -= math.float3(interaction.EntryNormal) * BallisticsUtil.Epsilon; // move slightly inside of collider to prevent double collision (when queries-hit-backfaces is enabled)
                    bullet.EnergyLossPerUnit = interaction.EnergyLossPerUnit;
                } else { // ricochet
                    bullet.Position += math.float3(interaction.EntryNormal) * BallisticsUtil.Epsilon; // move slightly outside of collider to prevent double collision (when queries-hit-backfaces is enabled)
                    bullet.Velocity = math.reflect(bullet.Velocity, interaction.EntryNormal);
                    SpreadBulletPath(ref bullet, interaction.EntryNormal, interaction.RicochetSpreadAngle, ref rng);
                    if (math.lengthsq(bullet.Velocity) <= MinimumRicochetSpeedSqr)
                        bullet.Stop();
                }
                Raycasts[jobID] = bullet.GetRaycastCommand();
            } else { // no interaction
                var deltaPosition = bullet.Velocity * bullet.FlyTime;
                bullet.Position += deltaPosition;
                UpdateVelocity(ref bullet, math.length(deltaPosition), bullet.FlyTime, Environment);
                bullet.FlyTime = 0;
            }
#if !BB_NO_DISTANCE_TRACKING
            bullet.Distance += math.distance(fromPosition, bullet.Position);
#endif
        }
    }
}