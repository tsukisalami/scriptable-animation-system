using System.Collections.Generic;
using UnityEngine;

namespace Ballistics
{
    [AddComponentMenu("Ballistics/Environment Provider")]
    public class EnvironmentProvider : MonoBehaviour
    {
        [Tooltip("BallisticSettings used for the simulation")]
        [InlineInspector] public BallisticSettings BallisticSettings;

        [Tooltip("ImpactHandler called for each bullet impact, regardless of specific bullet or material hit.")]
        [InlineInspector] public ImpactHandlerObject GlobalImpactHandler;

        [Tooltip("BallisticMaterial used for all colliders without an BallisticMaterial defined.")]
        [InlineInspector] public BallisticMaterial DefaultBallisticMaterial;

        [Tooltip("The wind velocity for the current simulation.")]
#if !BB_NO_AIR_RESISTANCE
        [PhysicalUnit(PhysicalType.SPEED)] public Vector3 WindVelocity;
#else
        [HideInInspector, SerializeField] private Vector3 WindVelocity;
#endif

        [Header("Editor Only")]
        [Tooltip("Draw bullet paths for debugging.")]
        public bool DebugBulletPaths = false;

        // keep track of active instance, to ensure only one is active at a time
        private static EnvironmentProvider instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            instance = null;
        }

        private void Awake()
        {
            if (!instance) {
                instance = this;
                UpdateCoreSetting();
                Core.UpdateEnvironment();
            } else {
                Debug.LogWarningFormat("Another instance of {0} already exists in the scene. Self destructing..", typeof(EnvironmentProvider).Name);
                Destroy(gameObject);
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            Core.EnableDebug = DebugBulletPaths;
#endif
            UpdateCoreSetting();
        }

        /// Upadate the simulation settings of the Ballistics.Core
        public void UpdateCoreSetting()
        {
            Core.BallisticSettings = BallisticSettings;
            Core.WindVelocity = WindVelocity;
            Core.Gravity = Physics.gravity;
            Core.GlobalImpactHandler = GlobalImpactHandler;
            BallisticMaterialCache.DefaultMaterial = DefaultBallisticMaterial;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}