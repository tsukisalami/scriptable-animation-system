using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Ballistics
{
    /// Utility for finding angles to hit a target x-distance away, with a given gravity, air density, bullet drag coefficient, etc.
    /// Zeroing calculations do currently not include bullet spin! 
    public static class Zeroing
    {
        public const float MaxDistanceError = .5f;
        public const int FindMaxSamples = 10;
        public const int FindMaxIterations = 4;
        public const int BinarySearchMaxIterations = 10;

        public struct Result
        {
            public float Distance, Angle;

            public Result(float distance, float angle)
            {
                Distance = distance;
                Angle = angle;
            }
        }

        public static Vector3 Apply(Vector3 direction, float zeroingAngle)
        {
            return Quaternion.AngleAxis(zeroingAngle, Vector3.Cross(direction, -Core.Environment.Gravity)) * direction;
        }

        public static Result[] ZeroingAnglesNoDrag(List<float> distances, float v, float g)
        {
            var result = new Result[distances.Count];
            for (var i = result.Length - 1; i >= 0; i--) {
                var angleOpt = ZeroingAngleNoDrag(distances[i], v, g);
                if (angleOpt.TryGet(out var angle)) {
                    result[i] = new Result(distances[i], angle);
                } else {
                    Debug.LogWarningFormat($"Failed to calculate zeroing angle for distance {0}! Distance is physically unreachable with current configuration.", distances[i]);
                    result[i] = new Result(distances[i], 0);
                }
            }
            return result;
        }

        public static Option<float> ZeroingAngleNoDrag(float d, float v, float g)
        {
            var x = math.abs(g * d) / (v * v);
            return x <= 1f ? Option<float>.Some(math.degrees(.5f * math.asin(x))) : Option<float>.None;
        }

#if !BB_NO_AIR_RESISTANCE
        public static SimulateJobHandle ApproximateZeroingAnglesWithDrag(List<float> distances, BulletInfo info, float gravity, float airDensity = 1.22f, float timeStep = .015f)
        {
#if UNITY_EDITOR
            if (info.Spin.sqrMagnitude > 0)
                Debug.LogWarning("The zeroing approximation does not take into account bullet spin!");
#endif
            return ApproximateZeroingAnglesWithDrag(distances, info.Speed, info.Diameter * .5f, info.Mass, gravity, info.DragCoefficient, info.Lifetime, airDensity, timeStep);
        }
#endif

        // impossible to solve -> velocity for x and y is not independent because of quadratic drag calculation -> approximate numerically
        public static SimulateJobHandle ApproximateZeroingAnglesWithDrag(List<float> distances, float v, float r, float m, float g, float dragCoefficient, float lifeTime, float airDensity = 1.22f, float timeStep = .015f)
        {
            var data = new NativeArray<SimulatePathJob.Data>(distances.Count, Allocator.Persistent);
            for (int i = 0; i < distances.Count; i++)
                data[i] = new(distances[i]);
            return new SimulateJobHandle(data, v, r, m, g, dragCoefficient, airDensity, timeStep, Mathf.CeilToInt(lifeTime / timeStep));
        }

        public readonly struct SimulateJobHandle : System.IDisposable
        {
            private readonly NativeArray<SimulatePathJob.Data> Data;
            private readonly JobHandle Handle;

            public SimulateJobHandle(NativeArray<SimulatePathJob.Data> data, float v, float r, float m, float g, float dragCoefficient, float airDensity, float timeStep, int iterations)
            {
                Data = data;
                var job = new SimulatePathJob() {
                    Speed = v,
                    PrecalculatedDragFactor = SimulatePathJob.PrecalculateDragFactor(r, m, dragCoefficient, airDensity, timeStep),
                    Gravity = g,
                    TimeStep = timeStep,
                    MaxIterations = iterations,
                    Entries = data
                };
                Handle = job.Schedule();
            }

            public Result[] Get()
            {
                Handle.Complete();
                var result = new Result[Data.Length];
                for (var i = 0; i < Data.Length; i++) {
                    var optimal = Data[i].Optimal;
                    if (optimal.x <= 0) {
                        Debug.LogWarningFormat("Failed to approximate zeroing angle for distance {0}! Distance unreachable, or bullet life time too short.", Data[i].Distance);
                        result[i] = new Result(Data[i].Distance, 0);
                    } else {
                        result[i] = new Result(Data[i].Distance, math.degrees(optimal.y));
                    }
                }
                return result;
            }

            public bool IsActive => Data.IsCreated;

            public void Dispose()
            {
                Handle.Complete();
                Data.Dispose();
            }
        }

        [BurstCompile]
        public struct SimulatePathJob : IJob
        {
            public float Speed;
            public float PrecalculatedDragFactor;
            public float Gravity;
            public float TimeStep;
            public int MaxIterations;

            public struct Data
            {
                public readonly float Distance;
                public float MinAngle, MinDistance;
                public float MaxAngle, MaxDistance;

                public Data(float distance)
                {
                    Distance = distance;
                    MinAngle = 0;
                    MinDistance = 0;
                    MaxAngle = math.radians(45);    // maximum range without drag
                    MaxDistance = float.MaxValue;
                }

                public void Apply(float dist, float angle)
                {
                    if (dist <= Distance && dist > MinDistance) {
                        MinDistance = dist;
                        MinAngle = angle;
                    }
                    if (dist >= Distance && dist < MaxDistance) {
                        MaxDistance = dist;
                        MaxAngle = angle;
                    }
                }

                public void SetUnreachable()
                {
                    MinDistance = MaxDistance = -1;
                }

                public float NextAngle => .5f * (MinAngle + MaxAngle);

                public float2 Optimal => math.abs(MinDistance - Distance) < math.abs(MaxDistance - Distance)
                        ? new float2(MinDistance, MinAngle)
                        : new float2(MaxDistance, MaxAngle);

                public bool IsValid => math.min(math.abs(MinDistance - Distance), math.abs(MaxDistance - Distance)) < MaxDistanceError;
                public bool IsUnreachable => MaxDistance <= 0;
            }
            public NativeArray<Data> Entries;

            public static float PrecalculateDragFactor(float radius, float mass, float dragCoefficient, float airDensity, float timeStep)
            {
                return .5f * timeStep * airDensity * radius * radius * math.PI * dragCoefficient / mass;
            }

            [BurstCompile]
            public float Simulate(float angle)
            {
                var position = float2.zero;
                var velocity = new float2(Mathf.Cos(angle), Mathf.Sin(angle)) * Speed;
                var scaledGravity = Gravity * TimeStep;
                for (var i = MaxIterations; i > 0; i--) {
                    position += velocity * TimeStep;
                    velocity -= velocity * math.length(velocity) * PrecalculatedDragFactor;
                    velocity.y += scaledGravity;
                    if (position.y < 0)
                        return -position.y * (velocity.x / velocity.y) + position.x; // find intersection with x axis
                }
                return -position.x; // negative to indicate we did not hit the ground
            }

            [BurstCompile]
            public void ApplyToAll(float distance, float angle)
            {
                for (var n = Entries.Length - 1; n >= 0; n--) {
                    var entry = Entries[n];
                    if (!entry.IsUnreachable) {
                        entry.Apply(distance, angle);
                        Entries[n] = entry;
                    }
                }
            }

            private static float SampleAngle(float2 range, int index, int count)
            {
                return math.lerp(range.x, range.y, index / (float)count);
            }

            [BurstCompile]
            public void Execute()
            {
                // find maximum reachable distance/ angle
                var angleRange = new float2(0, math.radians(45));
                var maxRange = -1f;
                for (var x = FindMaxIterations; x >= 0; x--) {
                    var maxDistance = maxRange;
                    var lastMaxDistance = maxDistance;
                    for (var i = 1; i <= FindMaxSamples; i++) {
                        var angle = SampleAngle(angleRange, i, FindMaxSamples);
                        var distance = Simulate(angle);
                        if (distance > 0 && distance > maxDistance) {
                            ApplyToAll(distance, angle);
                            lastMaxDistance = maxDistance;
                            maxDistance = distance;
                        } else {
                            if (x > 0) {
                                maxRange = lastMaxDistance;
                                angleRange.x = SampleAngle(angleRange, math.max(i - 2, 0), FindMaxSamples);
                            } else {
                                maxRange = maxDistance;
                                angleRange.x = SampleAngle(angleRange, i - 1, FindMaxSamples);
                            }
                            angleRange.y = angle;
                            break;
                        }
                    }
                }

                for (var i = Entries.Length - 1; i >= 0; i--) {
                    if (Entries[i].Distance <= 0 || Entries[i].Distance >= maxRange) {
                        var entry = Entries[i];
                        entry.SetUnreachable();
                        Entries[i] = entry;
                        continue;
                    }
                    for (var x = BinarySearchMaxIterations; x >= 0 && !Entries[i].IsValid; x--) {
                        var angle = Entries[i].NextAngle;   // binary search best zeroing angle
                        ApplyToAll(Simulate(angle), angle);
                    }
                }
            }
        }
    }
}