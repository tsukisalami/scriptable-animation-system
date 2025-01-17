using Unity.Burst;
using Unity.Mathematics;

namespace Ballistics
{
    public interface IVisualBullet
    {
        void InitializeBullet(in BulletPose pose, in float3 visualOffset);
        void UpdateBullet(in BulletPose pose);
        void DestroyBullet();
    }

    public abstract class VisualBulletProviderObject : InitializableScriptableObject
    {
        public abstract IVisualBullet GetVisualBullet();
    }

    [BurstCompile]
    public static class VisualBulletUtil
    {
        /// when the physical bullet simulation does not start at the end of the barrel, we slowly move the visual bullet towards the physical bullet position over a given distance
        [BurstCompile]
        public static void UpdateVisualOffset(ref float4 offset, in BulletPose pose, float timeStep, float distInv, out float3 position)
        {
            position = pose.Position;
            if (offset.w > 0) {
                if ((pose.Flags & InteractionFlags.HIT) != 0) {
                    offset = float4.zero;
                } else {
                    offset.w = math.max(0, offset.w - math.length(pose.Velocity * timeStep) * distInv);
                    position += offset.xyz * offset.w * offset.w;
                }
            }
        }
    }
}