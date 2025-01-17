using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Ballistics
{
    [AddComponentMenu("Ballistics/Bullet/Trajectory Visualizer")]
    public class TrajectoryVisualizer : MonoBehaviour
    {
        private const int MaxIterations = 10000000;
        public Weapon Weapon;
        public LineRenderer LineRenderer;

        [Header("Indicator")] public Renderer Indicator;
        [Range(0f, 0.1f)] public float IndicatorOffset = .01f;

        private TrajectoryUtil.TrajectoryData data;

        /// Disable AutoSimulate before calling manually! Only call this once per frame!
        /// Simulate a bullet shot from a given position in the given direction. 
        public void Simulate(Vector3 position, Vector3 direction, Vector3 visualOffset, BulletInfo info, in Environment environment)
        {
            var count = Mathf.Clamp(Mathf.CeilToInt(info.Lifetime / environment.MaximumDeltaTime), 1, MaxIterations);
            if (data.Length != count) {
                if (data.Length > 0)
                    data.Dispose();
                data = new TrajectoryUtil.TrajectoryData(count);
            }
            data.Handle.Complete();
            TrajectoryUtil.Calculate(ref data, new(null, position, direction, info, null), visualOffset, environment);
        }

        private void Start()
        {
            Core.OnUpdateCompleted += UpdateTrajectory;
            Indicator.enabled = false;
        }

        private void LateUpdate()
        {
            var bulletSpawn = Weapon.BulletSpawnPoint;
            Simulate(bulletSpawn.position, bulletSpawn.forward, Weapon.VisualSpawnPoint.position - bulletSpawn.position, Weapon.BulletInfo, Core.Environment);
        }

        private void UpdateTrajectory()
        {
            data.Render(LineRenderer);
            if (Indicator && data.TryGetHit(out var hit) && hit.collider != null) {
                Indicator.enabled = true;
                Indicator.transform.position = hit.point + hit.normal * IndicatorOffset;
                Indicator.transform.rotation = Quaternion.LookRotation(hit.normal);
            } else {
                Indicator.enabled = false;
            }
        }

        private void OnDestroy()
        {
            Core.OnUpdateCompleted -= UpdateTrajectory;
            data.Dispose();
        }
    }

    public static class TrajectoryUtil
    {
        /// internal Job data used for the simulation
        public struct TrajectoryData : System.IDisposable
        {
            public readonly int Length;
            public readonly NativeArray<float3> Points;
            public readonly NativeArray<RaycastCommand> Raycasts;
            public readonly NativeArray<RaycastHit> Hits;
            public readonly NativeArray<int> Count;
            public JobHandle Handle;
            public bool CalculationStarted;

            public TrajectoryData(int length)
            {
                Length = length;
                Points = new NativeArray<float3>(length + 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                Raycasts = new NativeArray<RaycastCommand>(length, Allocator.Persistent);
                Hits = new NativeArray<RaycastHit>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                Count = new NativeArray<int>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                Handle = default;
                CalculationStarted = false;
            }

            public void Render(LineRenderer lineRenderer)
            {
                if (!CalculationStarted)
                    return;
                Handle.Complete();
                var count = Count[0];
                lineRenderer.positionCount = count;
                lineRenderer.SetPositions(Points.Reinterpret<Vector3>().Slice(0, count));
            }

            public bool TryGetHit(out RaycastHit rayHit)
            {
                if (CalculationStarted) {
                    Handle.Complete();
                    var count = Count[0];
                    if (count >= 2) {
                        rayHit = Hits[count - 2];
                        return true;
                    }
                }
                rayHit = default;
                return false;
            }

            public void Dispose()
            {
                CalculationStarted = false;
                Handle.Complete();
                if (Points.IsCreated) {
                    Points.Dispose();
                    Raycasts.Dispose();
                    Hits.Dispose();
                    Count.Dispose();
                }
            }
        }

        private const int RaycastCommandBatchSize = 256;
        public static void Calculate(ref TrajectoryData data, in BulletInstance bullet, Vector3 visualOffset, Environment environment)
        {
            var native = new BulletNative();
            bullet.ToNative(ref native);
            data.Handle = new Process() {
                Bullet = native,
                Environment = environment,
                Commands = data.Raycasts
            }.Schedule();
            data.Handle = RaycastCommand.ScheduleBatch(data.Raycasts, data.Hits, RaycastCommandBatchSize, data.Handle);
            data.Handle = new FindIntersections() {
                Commands = data.Raycasts,
                Hits = data.Hits,
                Trajectory = data.Points,
                Count = data.Count,
                Environment = environment,
                VisualOffset = visualOffset
            }.Schedule(data.Handle);
            data.CalculationStarted = true;
        }
    }

    [BurstCompile]
    public struct FindIntersections : IJob
    {
        [ReadOnly] public NativeArray<RaycastCommand> Commands;
        [ReadOnly] public NativeArray<RaycastHit> Hits;
        [WriteOnly] public NativeArray<float3> Trajectory;
        [WriteOnly] public NativeArray<int> Count;
        [ReadOnly] public float3 VisualOffset;
        [ReadOnly] public Environment Environment;
        public void Execute()
        {
            var offset = new float4(VisualOffset, 1);
            var lastPos = (float3)Commands[0].from;
            for (int i = 0; i < Commands.Length; i++) {
                var position = (float3)Commands[i].from;
                offset.w = math.max(0, offset.w - math.distance(lastPos, position) * Environment.VisualToPhysicalDistanceInv);
                lastPos = position;
                Trajectory[i] = position + offset.xyz * offset.w * offset.w;

                if (Hits[i].colliderInstanceID != 0) {
                    Trajectory[i + 1] = (float3)Hits[i].point;
                    Count[0] = i + 2;
                    return;
                }
            }
            Count[0] = Commands.Length;
        }
    }

    [BurstCompile]
    public struct Process : IJob
    {
        public BulletNative Bullet;
        public Environment Environment;
        [WriteOnly] public NativeArray<RaycastCommand> Commands;

        public void Execute()
        {
            var bullet = Bullet;
            var lastPosition = bullet.Position;
            for (int i = 0; i < Commands.Length; i++) {
                bullet.Position += bullet.Velocity * Environment.MaximumDeltaTime;
                ProcessInteractionsJob.UpdateVelocity(ref bullet, 0, Environment.MaximumDeltaTime, Environment);
                var dir = bullet.Position - lastPosition;
                var dist = math.length(dir);
                Commands[i] = new RaycastCommand(lastPosition, dist > 0 ? dir / dist : new float3(1, 0, 0), new QueryParameters(bullet.HitMask, false, QueryTriggerInteraction.UseGlobal, false), dist);
                lastPosition = bullet.Position;
            }
        }
    }
}
