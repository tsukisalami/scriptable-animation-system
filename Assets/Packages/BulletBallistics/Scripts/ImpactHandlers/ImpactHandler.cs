using Unity.Mathematics;
using UnityEngine;

namespace Ballistics
{
    public interface IImpactHandler
    {
        /// Called when a bullet enters/ricochets/exits a collider
        HandledFlags HandleSurfaceInteraction(in SurfaceInteractionInfo impact, HandledFlags flags);

        /// Called when a bullet penetrates a collider
        HandledFlags HandleImpact(in ImpactInfo impact, HandledFlags flags);
    }

    public abstract class ImpactHandlerObject : InitializableScriptableObject, IImpactHandler
    {
        public abstract HandledFlags HandleSurfaceInteraction(in SurfaceInteractionInfo impact, HandledFlags flags);
        public abstract HandledFlags HandleImpact(in ImpactInfo impact, HandledFlags flags);
    }

    [System.Flags]
    public enum HandledFlags : uint
    {
        DAMAGE = 1 << 0,
        AUDIO = 1 << 1,
        PARTICLES = 1 << 2,
        BULLETHOLE = 1 << 3,
        PHYSICS = 1 << 4,

        [Tooltip("This will always be the first unreserved flag. Define your custom Flags as 'MYFLAG = CUSTOM << 0, MYFLAG2 = CUSTOM << 1, ...'")]
        CUSTOM = 1 << 5
    }

    public readonly struct SurfaceInteractionInfo
    {
        public enum InteractionType
        {
            STOP,
            ENTER,
            RICOCHET,
            EXIT,
        }
        public static InteractionType TypeFromImpactResult(MaterialImpact.Result result) => result switch {
            MaterialImpact.Result.STOP => InteractionType.STOP,
            MaterialImpact.Result.ENTER => InteractionType.ENTER,
            MaterialImpact.Result.RICOCHET => InteractionType.RICOCHET,
            _ => InteractionType.STOP,
        };

        public readonly InteractionType Type;
        public readonly RaycastHit HitInfo;
        public readonly float3 Velocity;
        public readonly float SpreadAngle;
        public readonly float SpeedFactor;
        public readonly float RemainingLifeTime;
#if !BB_NO_DISTANCE_TRACKING
        public readonly float DistanceTraveled;
#endif
        public readonly BulletInfo BulletInfo;
        public readonly Weapon Weapon;

        public SurfaceInteractionInfo(InteractionType type, in RaycastHit info, float spreadAngle, float speedFactor, in BulletManaged managed, in BulletNative native)
        {
            Type = type;
            HitInfo = info;
            Velocity = native.Velocity;
            SpreadAngle = spreadAngle;
            SpeedFactor = speedFactor;
            RemainingLifeTime = native.LifeTime + native.FlyTime;
#if !BB_NO_DISTANCE_TRACKING
            DistanceTraveled = native.Distance;
#endif
            BulletInfo = managed.Info;
            Weapon = managed.Weapon;
        }
    }

    public readonly struct ImpactInfo
    {
        public readonly RaycastHit HitInfo;
        public readonly float3 EntryVelocity;
        public readonly float ImpactDepth;
        public readonly float EnergyLossPerUnit;
        public readonly float RemainingLifeTime;
#if !BB_NO_DISTANCE_TRACKING
        public readonly float DistanceTraveled;
#endif
        public readonly BulletInfo BulletInfo;
        public readonly Weapon Weapon;

        public ImpactInfo(in RaycastHit hitInfo, float energyLossPerUnit, float impactDepth, in BulletManaged managed, in BulletNative native)
        {
            HitInfo = hitInfo;
            EntryVelocity = native.Velocity;
            EnergyLossPerUnit = energyLossPerUnit;
            ImpactDepth = impactDepth;
            RemainingLifeTime = native.LifeTime + native.FlyTime;
#if !BB_NO_DISTANCE_TRACKING
            DistanceTraveled = native.Distance;
#endif
            BulletInfo = managed.Info;
            Weapon = managed.Weapon;
        }
    }
}