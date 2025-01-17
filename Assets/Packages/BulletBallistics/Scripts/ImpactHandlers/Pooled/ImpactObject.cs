using UnityEngine;

namespace Ballistics
{
    [AddComponentMenu("Ballistics/Impact Handler/Impact Object/Base")]
    public class ImpactObject : MonoBehaviour
    {
        public virtual void HandleSurfaceInteraction(in SurfaceInteractionInfo impact, HandledFlags flags)
        { }
    }
}

