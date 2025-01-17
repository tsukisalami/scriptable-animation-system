using Unity.Mathematics;

namespace Ballistics
{
    /// Ballistic calculation helpers
    public static class BallisticsUtil
    {
        public static readonly float Epsilon = .0001f;

        public static double DensityHumidAir(double kelvin, double relativeHumidity, double airPressure)
        {
            const double MolarMassDryAir = 0.0289652; // kg/mol
            const double MolarMassWaterVapor = 0.018016; // kg/mol
            const double UniversalGasConstant = 8.31446; // J/(K*mol)

            var celcius = kelvin - 273.15f;
            var vaporPressureWater = relativeHumidity * (6.1078f * math.pow(10, 7.5f * celcius / (celcius + 237.3f))); // Tetens' equation
            var partialPressureDryAir = airPressure - vaporPressureWater;
            return (partialPressureDryAir * MolarMassDryAir + vaporPressureWater * MolarMassWaterVapor) / (UniversalGasConstant * kelvin);
        }

        public static double ViscosityAir(double kelvin)
        {
            const double b = 1.458e-6;
            const double s = 110.4;
            return b * math.pow(kelvin, 1.5) / (kelvin + s);    // dynamic viscosity of air
        }

        public static float SpeedOfSound(float airPressure, float airDensity)
        {
            const float AirHeatCapacityRatio = 1.4f; // approx. for normal conditions
            return math.sqrt(AirHeatCapacityRatio * airPressure / airDensity);
        }
    }
}