using UnityEngine;

namespace Ballistics
{
    /// Basic ImpactHandler, that instantiates an ImpactObject on the impact point.
    /// reuses old ImpactObjects when MaxInstances is reached.
    [CreateAssetMenu(fileName = "PooledImpactInstantiator", menuName = "Ballistics/Impact Handler/Pooled Impact Instantiator", order = 1)]
    public class PooledImpactInstantiator : ImpactHandlerObject
    {
        public HandledFlags HandledFlags;
        public ImpactObject ImpactPrefab;
        public int MaxInstances = 100;

        private class Instance
        {
            public ImpactObject ImpactObject;
            public Instance(ImpactObject prefab)
            {
                ImpactObject = Instantiate(prefab);
            }
        }

        // TODO: use cyclic pool
        private int index;
        private Instance[] instances;

        public override void Initialize()
        {
            index = 0;
            instances = new Instance[MaxInstances];
            Core.OnBeforeSceneChange += () => {
                for (int i = 0; i < MaxInstances; i++)
                    instances[i] = null;
            };
        }

        private ImpactObject GetNext()
        {
            index = (index + 1) % MaxInstances;
            if (instances[index] == null || instances[index].ImpactObject == null)
                instances[index] = new Instance(ImpactPrefab);
            return instances[index].ImpactObject;
        }

        public override HandledFlags HandleSurfaceInteraction(in SurfaceInteractionInfo impact, HandledFlags flags)
        {
            var impactObject = GetNext();
            var impactTransform = impactObject.transform;
            impactTransform.localScale = Vector3.one;
            impactTransform.SetParent(impact.HitInfo.transform, false);
            impactTransform.position = impact.HitInfo.point;
            impactTransform.rotation = Quaternion.LookRotation(impact.HitInfo.normal);
            var globalScale = impactTransform.lossyScale;
            impactTransform.localScale = new Vector3(1f / globalScale.x, 1f / globalScale.y, 1f / globalScale.z);
            impactObject.HandleSurfaceInteraction(impact, flags);
            return flags | HandledFlags;
        }

        public override HandledFlags HandleImpact(in ImpactInfo impact, HandledFlags flags)
        {
            return flags;
        }
    }
}