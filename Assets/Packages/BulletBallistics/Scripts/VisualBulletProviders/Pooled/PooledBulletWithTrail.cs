using Unity.Mathematics;
using UnityEngine;

namespace Ballistics
{
    public class PooledBulletWithTrail : PooledBullet
    {
        [Space]
        public TrailRenderer Trail;

        public override void InitializeBullet(in BulletPose pose, in float3 visualOffset)
        {
            base.InitializeBullet(pose, visualOffset);
            Trail.Clear();
            Trail.AddPosition(transformCache.position);
        }

        public override void UpdateBullet(in BulletPose pose)
        {
            base.UpdateBullet(pose);
            Trail.AddPosition(transformCache.position);
        }
    }
}