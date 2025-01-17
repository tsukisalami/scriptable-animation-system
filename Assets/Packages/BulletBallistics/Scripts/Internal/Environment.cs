using Unity.Mathematics;

namespace Ballistics
{
    /// Environment configuration for the ballistics simulation
    public readonly struct Environment
    {
        public readonly bool EnableGravity;
        public readonly float3 Gravity;
        public readonly float MaximumDeltaTime;
        public readonly float VisualToPhysicalDistanceInv;

#if !BB_NO_AIR_RESISTANCE
        public readonly bool EnableAirResistance;
        public readonly float AirDensity;
        public readonly float3 WindVelocity;
#endif
#if !(BB_NO_AIR_RESISTANCE || BB_NO_SPIN)
        public readonly bool EnableBulletSpin;
        public readonly float AirViscosity;
#endif

        public Environment(BallisticSettings settings, float3 windVelocity, float3 gravity)
        {
            if (settings) {
                EnableGravity = settings.Gravity;
                MaximumDeltaTime = (float)settings.MaximumDeltaTime;
                VisualToPhysicalDistanceInv = settings.VisualToPhysicalDistance != 0 ? 1f / (float)settings.VisualToPhysicalDistance : float.MaxValue;
            } else {
                EnableGravity = true;
                MaximumDeltaTime = .016666f;
                VisualToPhysicalDistanceInv = .1f;
            }
            Gravity = gravity;

#if !BB_NO_AIR_RESISTANCE
            if (settings) {
                EnableAirResistance = settings.AirResistance;
                AirDensity = settings.AirDensity;
            } else {
                EnableAirResistance = true;
                AirDensity = 1.22f;
            }
            WindVelocity = windVelocity;
#endif
#if !(BB_NO_AIR_RESISTANCE || BB_NO_SPIN)
            if (settings) {
                EnableBulletSpin = settings.BulletSpin;
                AirViscosity = (float)BallisticsUtil.ViscosityAir(settings.Temperature);
            } else {
                EnableBulletSpin = true;
                AirViscosity = 1.48e-5f;
            }
#endif
        }
    }
}