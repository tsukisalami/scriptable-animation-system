using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Ballistics
{
    /// handles bullet update batches
    public class BulletUpdateLoopHandler : IDisposable
    {
        public const int UpdateBatchSize = 512;
        public Environment Environment;
        public IImpactHandler GlobalImpactHandler;
        private readonly List<BatchHandle> bulletUpdateBatches = new();
        private NativeArray<JobHandle> jobHandles;

        public float TimeStep => CurrentStepTime;
        private float CurrentStepTime;
        private int StepsRemaining;

        public List<BatchHandle> UpdateBatchHandles => bulletUpdateBatches;

        public void Consume(ref List<BulletInstance> bulletInstances)
        {
            int startIndex = 0;
            // fill up existing batch updaters with new bullets
            for (int i = 0; i < bulletUpdateBatches.Count && startIndex < bulletInstances.Count; i++)
                startIndex = bulletUpdateBatches[i].BulletUpdater.BulletData.Insert(bulletInstances, startIndex);
            // allocate new batch updaters, if required
            while (startIndex < bulletInstances.Count) {
                var handler = new BatchHandle(UpdateBatchSize);
                startIndex = handler.BulletUpdater.BulletData.Insert(bulletInstances, startIndex);
                bulletUpdateBatches.Add(handler);
            }
            bulletInstances.Clear();
        }

        public void DisposeUnusedBatches(int keepEmptyBatchCount = 1)
        {
            for (int i = bulletUpdateBatches.Count - 1; i >= 0; i--) {
                if (bulletUpdateBatches[i].JobHandle.IsCompleted && bulletUpdateBatches[i].InteractionsResult.TotalCount == 0) {
                    if (keepEmptyBatchCount <= 0) {
                        bulletUpdateBatches[i].Dispose();
                        bulletUpdateBatches.RemoveAt(i);
                    } else {
                        keepEmptyBatchCount--;
                    }
                }
            }
        }

        public void InitializeUpdate(float timeStep)
        {
            StepsRemaining = Mathf.CeilToInt(timeStep / Environment.MaximumDeltaTime);
            CurrentStepTime = timeStep / StepsRemaining;
            if (InitializeNextStep())
                JobHandle.ScheduleBatchedJobs();
        }

        private bool InitializeNextStep()
        {
            if (StepsRemaining <= 0)
                return false;
            StepsRemaining--;
            bool hasWork = false;
            for (var i = bulletUpdateBatches.Count - 1; i >= 0; i--)
                hasWork |= bulletUpdateBatches[i].Initialize(CurrentStepTime);
            return hasWork;
        }

        public bool Update()
        {
            bool workRemaining = false;
            for (var i = bulletUpdateBatches.Count - 1; i >= 0; i--)
                workRemaining |= bulletUpdateBatches[i].Update(Environment, StepsRemaining == 0);
            if (!workRemaining && InitializeNextStep())
                workRemaining = true;
            if (workRemaining)
                JobHandle.ScheduleBatchedJobs(); // begin processing next iteration, before processing managed surface impacts
            for (var i = bulletUpdateBatches.Count - 1; i >= 0; i--)
                bulletUpdateBatches[i].BulletUpdater.HandleScheduledImpacts(GlobalImpactHandler);
            return workRemaining;
        }

        public void CompleteStep()
        {
            if (bulletUpdateBatches.Count == 0)
                return;
            if (jobHandles.Length < bulletUpdateBatches.Count) {
                if (jobHandles.IsCreated)
                    jobHandles.Dispose();
                jobHandles = new NativeArray<JobHandle>(bulletUpdateBatches.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
            for (var i = bulletUpdateBatches.Count - 1; i >= 0; i--)
                jobHandles[i] = bulletUpdateBatches[i].JobHandle;
            JobHandle.CompleteAll(jobHandles.GetSubArray(0, bulletUpdateBatches.Count));
        }

        public bool IsActive()
        {
            for (var i = bulletUpdateBatches.Count - 1; i >= 0; i--)
                if (bulletUpdateBatches[i].InteractionsResult.TotalCount > 0)
                    return true;
            return false;
        }

        public int ActiveCount()
        {
            int active = 0;
            for (var i = bulletUpdateBatches.Count - 1; i >= 0; i--)
                active += bulletUpdateBatches[i].BulletUpdater.ActiveBullets();
            return active;
        }

        public void Reset()
        {
            CompleteStep();
            foreach (var batch in bulletUpdateBatches)
                batch.Reset();
            DisposeUnusedBatches();
        }

        public void Dispose()
        {
            CompleteStep();
            foreach (var batch in bulletUpdateBatches)
                batch.Dispose();
            if (jobHandles.IsCreated)
                jobHandles.Dispose();
        }
    }

    public class BatchHandle : IDisposable
    {
        public readonly BulletUpdateBatch BulletUpdater;
        public InteractionsResult InteractionsResult;
        public JobHandle JobHandle;

        public BatchHandle(int capacity)
        {
            BulletUpdater = new BulletUpdateBatch(capacity);
            InteractionsResult = default;
            JobHandle = default;
        }

        public bool Initialize(float timeStep)
        {
            InteractionsResult.Initialize(BulletUpdater.ActiveBullets());
            if (InteractionsResult.TotalCount > 0)
                JobHandle = BulletUpdater.InitializeUpdate(timeStep);
            return InteractionsResult.TotalCount > 0;
        }

        public bool Update(in Environment environment, bool inLastUpdateStepInFrame)
        {
            if (InteractionsResult.TotalCount == 0)
                return false;
            JobHandle.Complete();
            InteractionsResult = BulletUpdater.GatherManagedInteractions(InteractionsResult, inLastUpdateStepInFrame);
            if (InteractionsResult.TotalCount == 0)
                return false;
            JobHandle = BulletUpdater.ProcessInteractions(InteractionsResult, environment);
            return true;
        }

        public void Reset()
        {
            JobHandle.Complete();
            BulletUpdater.Reset();
        }

        public void Dispose()
        {
            JobHandle.Complete();
            BulletUpdater.Dispose();
        }
    }
}
