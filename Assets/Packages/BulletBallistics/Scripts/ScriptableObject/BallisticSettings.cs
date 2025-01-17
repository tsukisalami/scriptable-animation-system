using UnityEngine;

#pragma warning disable 0414 // private field assigned but not used.

namespace Ballistics
{
    /// Configuration of the ballistic simulation environment
    [CreateAssetMenu(fileName = "NewBallisticSettings", menuName = "Ballistics/Ballistic Settings", order = 1)]
    public class BallisticSettings : ScriptableObject
    {
        [Header("Simulation")]
        [Tooltip("Bullets will be affected by gravity")]
        public bool Gravity = true;

        [Tooltip("Bullets will be affected by air resistance")]
#if !BB_NO_AIR_RESISTANCE
        public
#else
        [HideInInspector, SerializeField]
#endif
        bool AirResistance = true;

        [Tooltip("Spinning bullets will be affected by magnus force.")]
#if !(BB_NO_AIR_RESISTANCE || BB_NO_SPIN)
        public
#else
        [HideInInspector, SerializeField]
#endif
        bool BulletSpin = true;

        [Tooltip("Maximum time between two simulation steps (if framerate drops to low, sub steps will be simulated)"), PhysicalUnit(PhysicalType.TIME)]
        public double MaximumDeltaTime = 1.0 / 60.0; // seconds

        [Tooltip("Distance after which the visual bullet position matches the physical simulation"), PhysicalUnit(PhysicalType.LENGTH)]
        public double VisualToPhysicalDistance = 5;


        [Header("Environment")]
        [PhysicalUnit(PhysicalType.PRESSURE)]
#if !BB_NO_AIR_RESISTANCE
        public
#else
        [HideInInspector, SerializeField]
#endif
        double AirPressure = 101325; // pascal

        [PhysicalUnit(PhysicalType.TEMPERATURE)]
#if !BB_NO_AIR_RESISTANCE
        public
#else
        [HideInInspector, SerializeField]
#endif
        double Temperature = 293; // kelvin

        [Range(0, 1), PhysicalUnit(PhysicalType.PERCENTAGE)]
#if !BB_NO_AIR_RESISTANCE
        public
#else
        [HideInInspector, SerializeField]
#endif
        double RelativeHumidity = .4;

#if !BB_NO_AIR_RESISTANCE
        public float AirDensity => (float)BallisticsUtil.DensityHumidAir(Temperature, RelativeHumidity, AirPressure);
#endif
    }
}