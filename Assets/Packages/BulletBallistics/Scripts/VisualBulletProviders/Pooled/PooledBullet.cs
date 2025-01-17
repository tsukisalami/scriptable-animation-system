using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

namespace Ballistics
{
    public class PooledBullet : MonoBehaviour, IVisualBullet
    {
        protected ObjectPool<PooledBullet> BulletPool;
        public void SetSourcePool(ObjectPool<PooledBullet> pool) { BulletPool = pool; }

        protected Transform transformCache;
        protected virtual void Awake() { transformCache = transform; }

        protected float4 offset;
        public virtual void InitializeBullet(in BulletPose pose, in float3 visualOffset)
        {
            offset = new float4(visualOffset, 1);
            UpdateBullet(pose);
        }

        public virtual void UpdateBullet(in BulletPose pose)
        {
            VisualBulletUtil.UpdateVisualOffset(ref offset, pose, Core.CurrentTimeStep, Core.Environment.VisualToPhysicalDistanceInv, out var position);
            transformCache.position = position;
            transformCache.rotation = math.lengthsq(pose.Velocity) >= float.Epsilon ? Quaternion.LookRotation(pose.Velocity) : Quaternion.identity;
        }

        public virtual void DestroyBullet() { BulletPool.Release(this); }

        public virtual void PoolOnGet() { }
        public virtual void PoolOnRelease() { }
        public virtual void PoolOnDestroy() { }
    }
}
