using UnityEngine;

namespace Ballistics
{
    [System.Serializable] public class OnSurfaceInteractionEvent : UnityEngine.Events.UnityEvent<SurfaceInteractionInfo> { }
    [System.Serializable] public class OnImpactEvent : UnityEngine.Events.UnityEvent<ImpactInfo> { }

    /// Note! By default BulletBallistics does not check for an IImpactHandler component on the hit collider game object!
    /// Use a GenericImpactHandler to check for per-collider impact handlers on hit objects!
    [AddComponentMenu("Ballistics/Impact Handler/Collider Impact Handler")]
    public class ColliderImpactHandler : MonoBehaviour, IImpactHandler
    {
        public OnSurfaceInteractionEvent OnSurfaceInteraction;
        public OnImpactEvent OnImpact;

        public HandledFlags HandleSurfaceInteraction(in SurfaceInteractionInfo surfaceImpactInfo, HandledFlags flags)
        {
            OnSurfaceInteraction.Invoke(surfaceImpactInfo);
            return flags;
        }

        public HandledFlags HandleImpact(in ImpactInfo impactInfo, HandledFlags flags)
        {
            OnImpact.Invoke(impactInfo);
            return flags;
        }
    }
}