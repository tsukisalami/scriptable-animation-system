using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Ballistics
{
    /// Physical specification of a bullet
    [Tooltip("Config")]
    [CreateAssetMenu(fileName = "NewBullet", menuName = "Ballistics/Bullet Info", order = 1)]
    public class BulletInfo : ScriptableObject
    {
        [Header("Physics")]
        [Tooltip("Muzzle velocity of the bullet"), PhysicalUnit(PhysicalType.SPEED)]
        public float Speed = 500;

        [Tooltip("Mass of the bullet"), PhysicalUnit(PhysicalType.MASS)]
        public float Mass = 0.005f;

        [Tooltip("Bullet diameter"), PhysicalUnit(PhysicalType.LENGTH)]
#if !BB_NO_AIR_RESISTANCE
        public
#else
        [HideInInspector, SerializeField]
#endif
        float Diameter = 0.008f;

        [Tooltip("Drag coefficient of the bullet (0.5 sphere; ~0.3 bullets)"), Range(0f, 1f)]
#if !BB_NO_AIR_RESISTANCE
        public
#else
        [HideInInspector, SerializeField]
#endif
        float DragCoefficient = .3f;

#if !BB_NO_AIR_RESISTANCE
        public float PrecalculatedDragFactor => .5f * (.5f * Diameter) * (.5f * Diameter) * math.PI * DragCoefficient / Mass;
#endif

        [Tooltip("Spin of the bullet. (x-axis right, y-axis up, z-axis in flight direction)"), PhysicalUnit(PhysicalType.ROTATIONSPEED)]
#if !(BB_NO_AIR_RESISTANCE || BB_NO_SPIN)
        public
#else
        [HideInInspector, SerializeField]
#endif
        Vector3 Spin = Vector3.zero;

        [Header("Impact")]
        [Tooltip("Damage one bullet deals at maximum velocity")]
        public float Damage = 100;

        [Tooltip("How many bullets are shot every time the gun is fired")]
        public int PelletsPerShot = 1;

        [Tooltip("The layers the bullet can interact with")]
        public LayerMask HitMask = ~0;

        [Tooltip("Time until the bullet is destroyed, if it is not stopped before"), PhysicalUnit(PhysicalType.TIME)]
        public float Lifetime = 6;

        [Tooltip("Impact Handler that handles all impacts the bullet is involved in. (can be null)"), InlineInspector]
        [FormerlySerializedAs("impactHandler")]
        public ImpactHandlerObject ImpactHandler;

        [Header("Visuals")]
        [Tooltip("Bullet model or prefab")]
        public GameObject BulletPrefab;
        public GameObject CasingPrefab;
        public GameObject RoundPrefab;
    }
}