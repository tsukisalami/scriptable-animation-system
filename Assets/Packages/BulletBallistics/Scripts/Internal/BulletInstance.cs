using System;
using Unity.Mathematics;
using UnityEngine;

namespace Ballistics
{
    /// Internal bullet representations
    public readonly struct BulletInstance : ISplitable<BulletManaged, BulletNative>
    {
        public readonly Weapon Weapon;
        public readonly Vector3 Position;
        public readonly Vector3 Direction;
        public readonly BulletInfo Info;
        public readonly IVisualBullet Projectile;

        public BulletInstance(Weapon weapon, Vector3 position, Vector3 direction, BulletInfo info, IVisualBullet projectile)
        {
            Weapon = weapon;
            Position = position;
            Direction = direction;
            Info = info;
            Projectile = projectile;
        }

        public void ToManaged(ref BulletManaged managed)
        {
            managed.Weapon = Weapon;
            managed.Info = Info;
            managed.SetVisualBullet(Projectile);
            managed.ImpactMaterial = null;
            managed.ExitInfo = default;
        }

        public void ToNative(ref BulletNative native)
        {
            native.Position = Position;
            native.Velocity = Direction * Info.Speed;
            native.FlyTime = 0;
            native.LifeTime = Info.Lifetime;
            native.Mass = Info.Mass;
#if !BB_NO_AIR_RESISTANCE
            native.PrecalculatedDrag = Info.PrecalculatedDragFactor;
#endif
#if !(BB_NO_AIR_RESISTANCE || BB_NO_SPIN)
            native.Spin = Info.Spin;
            native.Radius = Info.Diameter * .5f;
#endif
#if !BB_NO_DISTANCE_TRACKING
            native.Distance = 0;
#endif
            native.EnergyLossPerUnit = -1;
            native.HitMask = Info.HitMask;
        }
    }

    public struct BulletManaged
    {
        public Weapon Weapon;
        public BulletInfo Info;
        private bool hasVisualBullet;
        private IVisualBullet visualBullet;
        public IBallisticMaterial ImpactMaterial;
        public RaycastHit ExitInfo;

        public void SetVisualBullet(IVisualBullet bullet)
        {
            hasVisualBullet = bullet != null;
            visualBullet = bullet;
        }

        public float EnterMaterial(in RaycastHit impact, in BulletNative bullet, IBallisticMaterial material, out float exitDistance)
        {
            if (impact.collider.FindExit(impact.point, math.normalize(bullet.Velocity), out var localExitInfo)) {
                var newExitDistSq = math.distancesq(impact.point, localExitInfo.point);
                exitDistance = math.sqrt(newExitDistSq);
                var oldExitDistSq = IsInsideMaterial ? (HasExit ? math.distancesq(impact.point, ExitInfo.point) : float.MaxValue) : 0;
                if (newExitDistSq > oldExitDistSq) { // keep further away exit point
                    ExitInfo = localExitInfo;
                    ImpactMaterial = material;
                }
                return ImpactMaterial.GetEnergyLossPerUnitSafe();
            } else { // no exit point -> stuck in material for ever
                ExitInfo = default;
                ImpactMaterial = material;
                var energyLossPerUnit = material.GetEnergyLossPerUnitSafe();
                exitDistance = bullet.Energy / energyLossPerUnit; // distance until stuck
                return energyLossPerUnit;
            }
        }

        public bool UpdateImpact(in BulletNative bullet, in RaycastHit entryHit, out IBallisticMaterial exitMaterial, out RaycastHit exitInfo)
        {
            if (HasExit) {
                var bulletEndPoint = bullet.Position + bullet.Velocity * bullet.FlyTime;
                var exitCurrentMat = math.dot(bulletEndPoint - (float3)ExitInfo.point, bullet.Velocity) >= 0;
                var enterNewBefore = entryHit.colliderInstanceID != 0                   // hit some collider in the current update step
                    && entryHit.colliderInstanceID != ExitInfo.colliderInstanceID       // this can only occur, if a mesh-collider detected its own inside -> hit should be ignored
                    && math.dot(ExitInfo.point - entryHit.point, bullet.Velocity) >= 0; // hits new collider before exiting
                if (exitCurrentMat && !enterNewBefore) {   // exit current collider, and does not enter an overlapping collider before
                    exitMaterial = ImpactMaterial;
                    exitInfo = ExitInfo;
                    ExitMaterial();
                    return true;
                }
            }
            exitMaterial = null;
            exitInfo = default;
            return false;
        }

        public void ExitMaterial()
        {
            ImpactMaterial = null;
            ExitInfo = default;
        }

        public bool IsInsideMaterial => ImpactMaterial != null;
        public bool HasExit => ExitInfo.colliderInstanceID != 0;

        public readonly void UpdateVisualBullet(in BulletPose pose)
        {
            if (!hasVisualBullet)
                return;
            try {
                visualBullet.UpdateBullet(pose);
            } catch (Exception e) { Debug.LogErrorFormat("BulletBallistics: Error thrown in `UpdateBullet` call. " + e); }
        }

        public readonly void DestroyVisualBullet()
        {
            if (!hasVisualBullet)
                return;
            try {
                visualBullet.DestroyBullet();
            } catch (Exception e) { Debug.LogErrorFormat("BulletBallistics: Error thrown in `DestroyBullet` call. " + e); }
        }
    }

    public struct BulletNative
    {
        public float3 Position;
        public float3 Velocity;
        public float LifeTime;
        public float FlyTime;
        public float Mass;
#if !BB_NO_AIR_RESISTANCE
        public float PrecalculatedDrag;
#endif
#if !(BB_NO_AIR_RESISTANCE || BB_NO_SPIN)
        public float3 Spin;
        public float Radius;
#endif
#if !BB_NO_DISTANCE_TRACKING
        public float Distance;
#endif
        public int HitMask; // unity 2022.2 QueryParameters
        public float EnergyLossPerUnit; // negative when outside of any ballistic object

        public float Speed { get => math.length(Velocity); }
        public float Energy { get => 0.5f * Mass * math.lengthsq(Velocity); }

        public RaycastCommand GetRaycastCommand()
        {
            var speed = math.length(Velocity);
            var maxDistance = speed * FlyTime;
            return new RaycastCommand(Position, speed > 0 ? Velocity / speed : new float3(1, 0, 0), new QueryParameters(HitMask, false, QueryTriggerInteraction.UseGlobal, false), maxDistance);
        }

        public void Stop()
        {
            Velocity = Vector3.zero;
            LifeTime = -1;
            FlyTime = -1;
        }
    }

    public readonly struct BulletPose
    {
        public readonly float3 Position;
        public readonly float3 Velocity;
        public readonly InteractionFlags Flags;

        public BulletPose(in float3 position, in float3 velocity, InteractionFlags flags)
        {
            Position = position;
            Velocity = velocity;
            Flags = flags;
        }
    }
}