using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Ballistics
{
    /// allow for unit conversion of basic physical types in the inspector
    /// BulletBallistics uses SI units as the base unit for all calculations

    public enum PhysicalType : int
    {
        LENGTH,
        SPEED,
        MASS,
        TIME,
        ROTATIONSPEED,
        TEMPERATURE,
        PRESSURE,
        ANGLE,
        PERCENTAGE
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PhysicalValue
    {
        [FieldOffset(0)] public PhysicalType Holds;
        [FieldOffset(4)] public int ActiveRepresentation;
        [FieldOffset(4)] public LengthType LengthType;
        [FieldOffset(4)] public SpeedType SpeedType;
        [FieldOffset(4)] public MassType MassType;
        [FieldOffset(4)] public TimeType TimeType;
        [FieldOffset(4)] public RotationSpeedType RotationSpeedType;
        [FieldOffset(4)] public TemperatureType TemperatureType;
        [FieldOffset(4)] public PressureType PressureType;
        [FieldOffset(4)] public AngleType AngleType;
        [FieldOffset(4)] public PercentageType PercentageType;

        public void Set(PhysicalType holds, int representation)
        {
            Holds = holds;
            ActiveRepresentation = representation;
        }

        public double ToSi(double value) => Holds switch {
            PhysicalType.LENGTH => LengthType.ToSi(value),
            PhysicalType.SPEED => SpeedType.ToSi(value),
            PhysicalType.MASS => MassType.ToSi(value),
            PhysicalType.TIME => TimeType.ToSi(value),
            PhysicalType.ROTATIONSPEED => RotationSpeedType.ToSi(value),
            PhysicalType.TEMPERATURE => TemperatureType.ToSi(value),
            PhysicalType.PRESSURE => PressureType.ToSi(value),
            PhysicalType.ANGLE => AngleType.ToSi(value),
            PhysicalType.PERCENTAGE => PercentageType.ToSi(value),
            _ => value,
        };
        public Vector2 ToSi(Vector2 value) => new((float)ToSi(value.x), (float)ToSi(value.y));
        public Vector3 ToSi(Vector3 value) => new((float)ToSi(value.x), (float)ToSi(value.y), (float)ToSi(value.z));

        public double FromSi(double value) => Holds switch {
            PhysicalType.LENGTH => LengthType.FromSi(value),
            PhysicalType.SPEED => SpeedType.FromSi(value),
            PhysicalType.MASS => MassType.FromSi(value),
            PhysicalType.TIME => TimeType.FromSi(value),
            PhysicalType.ROTATIONSPEED => RotationSpeedType.FromSi(value),
            PhysicalType.TEMPERATURE => TemperatureType.FromSi(value),
            PhysicalType.PRESSURE => PressureType.FromSi(value),
            PhysicalType.ANGLE => AngleType.FromSi(value),
            PhysicalType.PERCENTAGE => PercentageType.FromSi(value),
            _ => value,
        };
        public Vector2 FromSi(Vector2 value) => new((float)FromSi(value.x), (float)FromSi(value.y));
        public Vector3 FromSi(Vector3 value) => new((float)FromSi(value.x), (float)FromSi(value.y), (float)FromSi(value.z));

        public Dictionary<int, UnitDescriptor> Representations() => Holds switch {
            PhysicalType.LENGTH => PhysicalValueConversion.LengthTypeRepresentations,
            PhysicalType.SPEED => PhysicalValueConversion.SpeedTypeRepresentations,
            PhysicalType.MASS => PhysicalValueConversion.MassTypeRepresentations,
            PhysicalType.TIME => PhysicalValueConversion.TimeTypeRepresentations,
            PhysicalType.ROTATIONSPEED => PhysicalValueConversion.RotationSpeedTypeRepresentations,
            PhysicalType.TEMPERATURE => PhysicalValueConversion.TemperatureTypeRepresentations,
            PhysicalType.PRESSURE => PhysicalValueConversion.PressureTypeRepresentations,
            PhysicalType.ANGLE => PhysicalValueConversion.AngleTypeRepresentations,
            PhysicalType.PERCENTAGE => PhysicalValueConversion.PercentageTypeRepresentations,
            _ => null
        };
    }

    public readonly struct UnitDescriptor
    {
        public readonly string FullName;
        public readonly string Abbreviation;
        public UnitDescriptor(string full, string abbrev)
        {
            FullName = full;
            Abbreviation = abbrev;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            return this == (UnitDescriptor)obj;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(FullName, Abbreviation);
        }

        public static bool operator ==(UnitDescriptor a, UnitDescriptor b)
        {
            return a.FullName == b.FullName && a.Abbreviation == b.Abbreviation;
        }

        public static bool operator !=(UnitDescriptor a, UnitDescriptor b)
        {
            return !(a == b);
        }
    }

    public static class PhysicalValueConversion
    {
        public static readonly Dictionary<int, UnitDescriptor> LengthTypeRepresentations = new() {
            { (int)LengthType.METER , new("meter", "m") },
            { (int)LengthType.CENTIMETER , new("centimeter", "cm") },
            { (int)LengthType.MILLIMETER , new("millimeter", "mm") },
            { (int)LengthType.KILOMETER , new("kilometer", "km") },
            { (int)LengthType.INCH , new("inch", "in") },
            { (int)LengthType.FOOT , new("foot", "ft") },
            { (int)LengthType.YARD , new("yard", "yd") },
            { (int)LengthType.MILE , new("mile", "mi") },
        };
        public static double ToSi(this LengthType type, double value) => value * type switch {
            LengthType.CENTIMETER => 1.0 / 100.0,
            LengthType.MILLIMETER => 1.0 / 1000.0,
            LengthType.KILOMETER => 1000,
            LengthType.INCH => 1.0 / 39.37,
            LengthType.FOOT => 1.0 / 3.281,
            LengthType.YARD => 1.0 / 1.094,
            LengthType.MILE => 1609.34,
            _ => 1
        };
        public static double FromSi(this LengthType target, double siValue) => siValue / target.ToSi(1);

        public static readonly Dictionary<int, UnitDescriptor> SpeedTypeRepresentations = new() {
            { (int)SpeedType.METERS_PER_SECOND, new("meters per second", "m/s") },
            { (int)SpeedType.CENTIMETERS_PER_SECOND, new("centimeters per second", "cm/s") },
            { (int)SpeedType.MILLIMETERS_PER_SECOND, new("millimeters per second", "mm/s") },
            { (int)SpeedType.KILOMETERS_PER_SECOND, new("kilometers per second", "km/s") },
            { (int)SpeedType.INCHES_PER_SECOND, new("inches per second", "in/s") },
            { (int)SpeedType.FEET_PER_SECOND, new("feet per second", "ft/s") },
            { (int)SpeedType.YARDS_PER_SECOND, new("yards per second", "yd/s") },
            { (int)SpeedType.MILES_PER_SECOND, new("miles per second", "mi/s") }
        };
        public static double ToSi(this SpeedType type, double value) => value * type switch {
            SpeedType.CENTIMETERS_PER_SECOND => 1.0 / 100.0,
            SpeedType.MILLIMETERS_PER_SECOND => 1.0 / 1000.0,
            SpeedType.KILOMETERS_PER_SECOND => 1000,
            SpeedType.INCHES_PER_SECOND => 1.0 / 39.37,
            SpeedType.FEET_PER_SECOND => 1.0 / 3.281,
            SpeedType.YARDS_PER_SECOND => 1.0 / 1.094,
            SpeedType.MILES_PER_SECOND => 1609.34,
            _ => 1
        };
        public static double FromSi(this SpeedType target, double siValue) => siValue / target.ToSi(1);

        public static readonly Dictionary<int, UnitDescriptor> MassTypeRepresentations = new() {
            { (int)MassType.KILOGRAM, new("kilogram", "kg") },
            { (int)MassType.GRAM, new("gram", "g") },
            { (int)MassType.GRAIN, new("grain", "gr") },
            { (int)MassType.OUNCE, new("ounce", "oz") },
            { (int)MassType.PUND, new("pound", "lb") }
        };
        public static double ToSi(this MassType type, double value) => value * type switch {
            MassType.GRAM => 1.0 / 1000,
            MassType.GRAIN => 1.0 / 15432,
            MassType.OUNCE => 1 / 35.274,
            MassType.PUND => 1 / 2.205,
            _ => 1
        };
        public static double FromSi(this MassType target, double siValue) => siValue / target.ToSi(1);


        public static readonly Dictionary<int, UnitDescriptor> TimeTypeRepresentations = new() {
            { (int)TimeType.SECOND, new("second", "s") } ,
            { (int)TimeType.MILLISECOND, new("millisecond", "ms") }
        };
        public static double ToSi(this TimeType type, double value) => value * type switch {
            TimeType.MILLISECOND => 1.0 / 1000.0,
            _ => 1
        };
        public static double FromSi(this TimeType target, double siValue) => siValue / target.ToSi(1);


        public static readonly Dictionary<int, UnitDescriptor> RotationSpeedTypeRepresentations = new() {
            { (int)RotationSpeedType.RADIANS_PER_SECOND, new("radians per second", "rad/s") },
            { (int)RotationSpeedType.REVOLUTIONS_PER_MINUTE, new("revolutions per minute", "rpm") },
            { (int)RotationSpeedType.DEGREE_PER_SECOND, new("degree per second", "deg/s") }
        };
        public static double ToSi(this RotationSpeedType type, double value) => value * type switch {
            RotationSpeedType.REVOLUTIONS_PER_MINUTE => 0.10472,
            RotationSpeedType.DEGREE_PER_SECOND => (2 * math.PI_DBL) / 360.0,
            _ => 1
        };
        public static double FromSi(this RotationSpeedType target, double siValue) => siValue / target.ToSi(1);

        public static readonly Dictionary<int, UnitDescriptor> TemperatureTypeRepresentations = new() {
            { (int)TemperatureType.KELVIN, new("kelvin", "K") },
            { (int)TemperatureType.CELSIUS, new("celsius", "°C") },
            { (int)TemperatureType.FAHRENHEIT, new("fahrenheit", "°F") }
        };
        public static double ToSi(this TemperatureType type, double value) => type switch {
            TemperatureType.CELSIUS => value + 273.15,
            TemperatureType.FAHRENHEIT => (value + 459.67) * (5.0 / 9.0),
            _ => value
        };
        public static double FromSi(this TemperatureType target, double siValue) => target switch {
            TemperatureType.CELSIUS => siValue - 273.15,
            TemperatureType.FAHRENHEIT => siValue * (9.0 / 5.0) - 459.67,
            _ => siValue
        };

        public static readonly Dictionary<int, UnitDescriptor> PressureTypeRepresentations = new() {
            { (int)PressureType.PASCAL, new("pascal", "Pa") },
            { (int)PressureType.HEKTOPASCAL, new("hektopascal", "hPa") },
            { (int)PressureType.BAR, new("bar", "bar") }
        };
        public static double ToSi(this PressureType type, double value) => value * type switch {
            PressureType.HEKTOPASCAL => 100,
            PressureType.BAR => 100000,
            _ => 1
        };
        public static double FromSi(this PressureType target, double siValue) => siValue / target.ToSi(1);

        public static readonly Dictionary<int, UnitDescriptor> AngleTypeRepresentations = new() {
            { (int)AngleType.RADIANS, new("radians", "rad") },
            { (int)AngleType.DEGREE, new("degree", "deg") }
        };
        public static double ToSi(this AngleType type, double value) => value * type switch {
            AngleType.DEGREE => math.PI_DBL / 180.0,
            _ => 1
        };
        public static double FromSi(this AngleType target, double siValue) => siValue / target.ToSi(1);

        public static readonly Dictionary<int, UnitDescriptor> PercentageTypeRepresentations = new() {
            { (int)PercentageType.PERCENTAGE, new("percentage", "%") }
        };
        public static double ToSi(this PercentageType type, double value) => value * type switch {
            PercentageType.PERCENTAGE => 1.0 / 100.0,
            _ => 1
        };
        public static double FromSi(this PercentageType target, double siValue) => siValue / target.ToSi(1);
    }

    public enum LengthType : int
    {
        METER,
        CENTIMETER,
        MILLIMETER,
        KILOMETER,
        INCH,
        FOOT,
        YARD,
        MILE
    }
    public enum SpeedType : int // TODO: this could be implicitly calculated from length/time
    {
        METERS_PER_SECOND,
        CENTIMETERS_PER_SECOND,
        MILLIMETERS_PER_SECOND,
        KILOMETERS_PER_SECOND,
        INCHES_PER_SECOND,
        FEET_PER_SECOND,
        YARDS_PER_SECOND,
        MILES_PER_SECOND
    }
    public enum MassType : int
    {
        KILOGRAM,
        GRAM,
        GRAIN,
        OUNCE,
        PUND
    }
    public enum TimeType : int
    {
        SECOND,
        MILLISECOND
    }
    public enum RotationSpeedType : int
    {
        RADIANS_PER_SECOND,
        DEGREE_PER_SECOND,
        REVOLUTIONS_PER_MINUTE
    }
    public enum TemperatureType : int
    {
        KELVIN,
        CELSIUS,
        FAHRENHEIT
    }
    public enum PressureType : int
    {
        PASCAL,
        HEKTOPASCAL,
        BAR
    }
    public enum AngleType : int
    {
        RADIANS,
        DEGREE
    }
    public enum PercentageType : int
    {
        PERCENTAGE
    }
}

