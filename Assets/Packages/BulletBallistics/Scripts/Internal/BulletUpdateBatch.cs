using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Ballistics
{
    /// internal handler for updating a batch of bullets
    public class BulletUpdateBatch : IDisposable
    {
        public const int ParallelBatchSize = 32;
        public readonly LinkedNativeData<BulletManaged, BulletNative> BulletData;
        private NativeArray<int> Indices;
        private NativeArray<RaycastCommand> Commands;
        private NativeArray<RaycastHit> Hits;
        private NativeArray<BulletInteraction> Interactions;
        private readonly List<DelayedImpactHandling<SurfaceInteractionInfo>> scheduledSurfaceInteractions;
        private readonly List<DelayedImpactHandling<ImpactInfo>> scheduledImpacts;

        private InitializeUpdateJob initializeJob = new();
        private ProcessInteractionsJob processInteractionsJob = new();

        public BulletUpdateBatch(int capacity)
        {
            // initialize managed data
            scheduledImpacts = new List<DelayedImpactHandling<ImpactInfo>>(capacity);
            scheduledSurfaceInteractions = new List<DelayedImpactHandling<SurfaceInteractionInfo>>(capacity);
            // initialize native arrays
            BulletData = new LinkedNativeData<BulletManaged, BulletNative>(capacity);
            Indices = new NativeArray<int>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            Commands = new NativeArray<RaycastCommand>(capacity, Allocator.Persistent, NativeArrayOptions.ClearMemory); // ClearMemory ensures maxhits is initialized
            Hits = new NativeArray<RaycastHit>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            Interactions = new NativeArray<BulletInteraction>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            // initialize jobs
            initializeJob.Indices = Indices;
            initializeJob.Bullets = BulletData.Native;
            initializeJob.Raycasts = Commands;
            processInteractionsJob.Indices = Indices;
            processInteractionsJob.Bullets = BulletData.Native;
            processInteractionsJob.Interactions = Interactions;
            processInteractionsJob.Raycasts = Commands;
        }

        public void Reset()
        {
            var active = BulletData.CopyUsedIndices(ref Indices);
            for (int i = 0; i < active; i++) {
                var index = Indices[i];
                BulletData.Managed[index].DestroyVisualBullet();
                BulletData.MarkFree(index);
            }
        }

        public int ActiveBullets() => BulletData.ActiveCount;

        public JobHandle InitializeUpdate(float timeStep)
        {
            var activeCount = BulletData.CopyUsedIndices(ref Indices);
            initializeJob.timeStep = timeStep;
            return ScheduleRaycasts(activeCount, initializeJob.Schedule(activeCount, ParallelBatchSize));
        }

        public JobHandle ProcessInteractions(InteractionsResult interactionsResult, Environment environment)
        {
            processInteractionsJob.Environment = environment;
            processInteractionsJob.BaseSeed = (uint)UnityEngine.Random.Range(0, int.MaxValue);
            return ScheduleRaycasts(interactionsResult.ActiveCount, processInteractionsJob.Schedule(interactionsResult.TotalCount, ParallelBatchSize));
        }

        private JobHandle ScheduleRaycasts(int activeCount, JobHandle dependsOn)
        {
            if (activeCount > 0)
                return RaycastCommand.ScheduleBatch(Commands.GetSubArray(0, activeCount), Hits.GetSubArray(0, activeCount), ParallelBatchSize, dependsOn);
            else
                return dependsOn;
        }

        public InteractionsResult GatherManagedInteractions(InteractionsResult lastResult, bool isLastUpdateStepInFrame)
        {
            for (int i = lastResult.ActiveCount; i < lastResult.TotalCount; i++) {
                var index = Indices[i];
                if (index < 0)
                    continue;
                ref var managed = ref BulletData.Managed[index];
                ref var native = ref BulletData.Native.GetRef(index);
                if (isLastUpdateStepInFrame) {
                    managed.UpdateVisualBullet(new(native.Position, native.Velocity, InteractionFlags.NONE));
                }
                if (native.LifeTime <= 0) {
                    managed.DestroyVisualBullet();
                    BulletData.MarkFree(index);
                }
            }

            var nextActiveCount = 0;
            var lastIndex = lastResult.ActiveCount - 1;
            for (int i = 0; i < lastResult.ActiveCount; i++) {
                var index = Indices[i];
                ref var managed = ref BulletData.Managed[index];
                ref var native = ref BulletData.Native.GetRef(index);
                if (HandleManagedInteraction(ref managed, ref native, Hits[i], out var info)) {
                    info.Index = index;
                    Interactions[info.Flags != InteractionFlags.NONE ? nextActiveCount++ : lastIndex--] = info;
                } else { // bullet has been stopped
                    managed.DestroyVisualBullet();
                    BulletData.MarkFree(index);
                    info.Index = -1;
                    Indices[i] = -1;
                    Interactions[lastIndex--] = info;
                }
            }
            return new InteractionsResult(nextActiveCount, lastResult.ActiveCount);
        }

        private bool HandleManagedInteraction(ref BulletManaged managed, ref BulletNative native, in RaycastHit hit, out BulletInteraction info)
        {
            info = default;
            if (native.LifeTime <= 0) // died during last ProcessInteractionsJob
                return false;
            if (managed.IsInsideMaterial) {
                if (managed.HasExit && !managed.ExitInfo.collider) {
                    // inside a collider, but the collider was destroyed last update -> ignore 
                    managed.ExitMaterial();
                    native.EnergyLossPerUnit = 0;
                } else if (managed.UpdateImpact(native, hit, out var exitMat, out var exitInfo)) {
                    // Debug.Log("Exit");
                    info.Flags |= InteractionFlags.EXIT; // bullet exits the material, it is currently inside of, in this update step
                    info.ExitNormal = math.half3(exitInfo.normal);
                    info.ExitPoint = exitInfo.point;
                    info.ExitSpreadAngle = exitMat.GetSpreadAngleSafe();
                    if (native.Energy > math.distance(native.Position, info.ExitPoint) * exitMat.GetEnergyLossPerUnitSafe()) { // has remaining energy -> schedule exit surface interaction
                        managed.UpdateVisualBullet(new(exitInfo.point, native.Velocity, info.Flags));
                        ScheduleSurfaceInteraction(exitMat, new(SurfaceInteractionInfo.InteractionType.EXIT, exitInfo, info.ExitSpreadAngle, 1, managed, native));
                    } else {
                        // Debug.Log("Stuck inside collider");
                        return false;   // bullet stuck in current collider -> stop
                    }
                    if (exitInfo.colliderInstanceID == hit.colliderInstanceID && math.distancesq(exitInfo.point, hit.point) < BallisticsUtil.Epsilon) {
                        // when queries-hit-backfaces is enabled, mesh colliders will detect their own surface from the inside -> impact info is invalid. Wait for unity 2022.2 to disable this locally https://docs.unity3d.com/2022.2/Documentation/ScriptReference/RaycastCommand-queryParameters.html
                        return true;
                    }
                }
            }

            if (hit.colliderInstanceID != 0) {
                if (!BallisticMaterialCache.TryGet(hit.collider.sharedMaterial, out var hitMaterial))
                    return false; // hit collider without ballistic material, and no global material set
                var impact = hitMaterial.HandleImpactSafe(native, managed.Info, hit);
                if (impact.ImpactResult == MaterialImpact.Result.IGNORE)
                    return true;
                info.Flags |= InteractionFlags.HIT;
                info.EntryPoint = hit.point;
                info.EntryNormal = math.half3(hit.normal);
                info.SpeedFactor = 1;
                managed.UpdateVisualBullet(new(hit.point, native.Velocity, info.Flags));
                bool enterMaterial = true;
                if (!managed.IsInsideMaterial) {
                    ScheduleSurfaceInteraction(hitMaterial, new(SurfaceInteractionInfo.TypeFromImpactResult(impact.ImpactResult), hit, impact.SpreadAngle, impact.SpeedFactor, managed, native));
                    if (impact.ImpactResult == MaterialImpact.Result.RICOCHET) {
                        enterMaterial = false;
                        info.RicochetSpreadAngle = impact.SpreadAngle;
                        // Debug.Log("Ricochet");
                    }
                    info.SpeedFactor = impact.SpeedFactor;
                } else {
                    // Debug.Log("Hit " + hit.collider + " and already inside " + managed.ExitInfo.collider);
                }
                if (impact.ImpactResult == MaterialImpact.Result.STOP)
                    return false;
                if (enterMaterial) {
                    // Debug.Log("Enter");
                    info.Flags |= InteractionFlags.ENTRY;
                    info.EnergyLossPerUnit = managed.EnterMaterial(hit, native, hitMaterial, out var impactDepth);
                    if (info.EnergyLossPerUnit < 0) {
                        // FIXME: impact depth? -> stop instantly?
                        ScheduleImpact(hitMaterial, new(hit, info.EnergyLossPerUnit, 0, managed, native));
                        return false; // negative energyloss -> stop bullet
                    }
                    ScheduleImpact(hitMaterial, new(hit, info.EnergyLossPerUnit, impactDepth, managed, native));
                }
            }
            return true;
        }

        private void ScheduleSurfaceInteraction(IBallisticMaterial material, in SurfaceInteractionInfo surfaceImpactInfo) =>
            scheduledSurfaceInteractions.Add(new(material, surfaceImpactInfo));

        private void ScheduleImpact(IBallisticMaterial material, in ImpactInfo impactInfo) =>
            scheduledImpacts.Add(new(material, impactInfo));

        public void HandleScheduledImpacts(IImpactHandler globalImpactHandler)
        {
            void logErr(Exception e)
            {
                // errors thrown in user code, should not break the BulletBallistics core loop
                Debug.LogErrorFormat("BulletBallistics: Error thrown during impact handling. " + e);
            }

            var hasGlobalHandler = globalImpactHandler != null;

            for (var i = scheduledImpacts.Count - 1; i >= 0; i--) {
                var impact = scheduledImpacts[i];
                HandledFlags flags = 0;
                try {
                    var handler = impact.Info.BulletInfo.ImpactHandler;
                    if (handler != null)
                        flags |= handler.HandleImpact(impact.Info, flags);
                } catch (Exception e) { logErr(e); };
                try {
                    var handler = impact.Material.GetImpactHandler();
                    if (handler != null)
                        flags |= handler.HandleImpact(impact.Info, flags);
                } catch (Exception e) { logErr(e); }
                if (hasGlobalHandler) {
                    try {
                        globalImpactHandler.HandleImpact(impact.Info, flags);
                    } catch (Exception e) { logErr(e); }
                }
            }
            scheduledImpacts.Clear();
            for (var i = scheduledSurfaceInteractions.Count - 1; i >= 0; i--) {
                var impact = scheduledSurfaceInteractions[i];
                HandledFlags flags = 0;
                try {
                    var handler = impact.Info.BulletInfo.ImpactHandler;
                    if (handler != null)
                        flags |= handler.HandleSurfaceInteraction(impact.Info, flags);
                } catch (Exception e) { logErr(e); }
                try {
                    var handler = impact.Material.GetImpactHandler();
                    if (handler != null)
                        flags |= handler.HandleSurfaceInteraction(impact.Info, flags);
                } catch (Exception e) { logErr(e); }
                if (hasGlobalHandler) {
                    try {
                        globalImpactHandler.HandleSurfaceInteraction(impact.Info, flags);
                    } catch (Exception e) { logErr(e); }
                }
            }
            scheduledSurfaceInteractions.Clear();
        }

        private void ReleaseBuffers()
        {
            Indices.Dispose();
            Commands.Dispose();
            Hits.Dispose();
            Interactions.Dispose();
        }

        public void Dispose()
        {
            Reset();
            BulletData.Dispose();
            ReleaseBuffers();
        }
    }

    [Flags]
    public enum InteractionFlags : uint
    {
        NONE = 0,
        HIT = 1 << 0, // a hit without entry is a ricochet
        ENTRY = 1 << 1 | HIT,
        EXIT = 1 << 2
    }

    public struct BulletInteraction
    {
        public InteractionFlags Flags;
        public int Index;
        public float SpeedFactor;

        // exit settings
        public half3 ExitNormal;
        public float3 ExitPoint;
        public float ExitSpreadAngle; // [0-90] deg

        // entry settings
        public half3 EntryNormal;
        public float3 EntryPoint;
        public float EnergyLossPerUnit;

        // ricochet spread angle
        public float RicochetSpreadAngle; // [0-90] deg
    }

    public struct InteractionsResult
    {
        public int ActiveCount;
        public int TotalCount;

        public InteractionsResult(int active, int total)
        {
            ActiveCount = active;
            TotalCount = total;
        }

        public void Initialize(int total)
        {
            ActiveCount = total;
            TotalCount = total;
        }
    }

    public readonly struct DelayedImpactHandling<T> where T : struct
    {
        public readonly IBallisticMaterial Material;
        public readonly T Info;

        public DelayedImpactHandling(IBallisticMaterial mat, T info)
        {
            Material = mat;
            Info = info;
        }
    }
}