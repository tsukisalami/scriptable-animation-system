using System;
using UnityEngine;
using Oyedoyin.Common;
using Oyedoyin.Analysis;
using Oyedoyin.Mathematics;
using Oyedoyin.Common.Misc;

namespace Oyedoyin.RotaryWing
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Autopilot
    {
        public enum LateralState { AttitudeHold, HeadingHold, DriftControl }
        public enum LongitudinalState { AttitudeHold, SpeedHold }
        public enum PowerState { AltitudeHold, RateHold }


        public Computer _flcs;

        [Header("Lateral Components")]
        public Lag m_rollRateSensor;
        public Lag m_rollCommandFilter;
        public SecondOrderFilter m_rollStructuralFilter;
        public FPID m_rollRateSolver;
        private FPID m_bankSolver;
        public FPID m_headingSolver;
        public FPID m_driftSolver;


        public LateralState m_lateralState = LateralState.AttitudeHold;
        public double m_bankGain = 0.65;
        public double m_headingGain = 0.385;
        public double m_turnGain = 1.06;
        public double m_driftGain = 0.25;

        [Header("Lateral Data")]
        public double m_rollRate;
        public double m_rollAcceleration;
        public double m_bankAngle;
        public double m_heading;
        public double m_turnRate;
        public double m_turnRadius;
        public double m_drift;

        public PowerState m_powerState = PowerState.AltitudeHold;
        public double m_altitude;
        public double m_climbRate;
        public double m_altitudeGain = 0.15;
        public double m_altitudeIntegral = 0.05;
        public double m_altitudeWindup = 5;
        public FPID m_climbSolver;
        private FPID m_altitudeSolver;
        public AnimationCurve m_decentRateLimit;
        protected float _presetAltitude;


        [Header("Longitudinal Components")]
        public Lag m_pitchRateSensor;
        public LongitudinalState m_longitudinalMode = LongitudinalState.AttitudeHold;
        public Lag m_pitchCommandFilter;
        public SecondOrderFilter m_pitchStructuralFilter;
        private FPID m_pitchAngleSolver;
        public FPID m_pitchRateSolver;
        public FPID m_speedSolver;
        protected float _presetSpeed;

        public double m_pitchGain = 0.95;
        public double pressureGain = 0.455;

        [Header("Longitudinal Data")]
        public double m_pitchRate;
        public double m_pitchAcceleration;
        public double m_pitchAngle;
        public double m_α;
        public double m_currentSpeed;
        public double m_mach, m_acceleration;

        [Header("Directional Components")]
        public Lag m_yawRateSensor;
        public SecondOrderFilter m_yawStructuralFilter;
        public AnimationCurve m_yawEffectiveness;
        public FPID m_yawRateSolver;
        public FPID m_SideAccelerationSolver;
        private FPID m_pedalHeadingSolver;

        public double m_pedalHeadingGain = 0.45;
        public double m_transitionSpeed = 40;


        [Header("Directional Data")]
        public double m_Ay;
        public double m_yawRate;
        public double m_yawAcceleration;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="altitude"></param>
        public void SetCommandPreset(float speed, float altitude)
        {
            _presetSpeed = speed;
            _presetAltitude = altitude;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void Initialize()
        {
            // ------------------------------ Lateral
            m_rollCommandFilter.m_timeConstant = 1.0 / 10.0;
            m_rollRateSensor.m_timeConstant = 1.0 / 50.0; //50 Hz
            m_rollStructuralFilter.Initialize(4.0, 64.0, 6400.0, 1.0, 80.0, 6400.0);

            // ------------------------------ Longitudinal
            m_pitchCommandFilter.m_timeConstant = 0.20;
            m_pitchRateSensor.m_timeConstant = 0.02;
            m_pitchStructuralFilter.Initialize(4.0, 64.0, 6400.0, 1.0, 80.0, 6400.0);

            // ------------------------------ Directional
            m_yawRateSensor.m_timeConstant = 0.025;
            m_yawStructuralFilter.Initialize(4.0, 64.0, 6400.0, 1.0, 80.0, 6400.0);

            // ------------------------------ Decent Rate
            m_decentRateLimit = new AnimationCurve();
            m_decentRateLimit.AddKey(new Keyframe(40, (float)_flcs.maximumClimbRate));
            m_decentRateLimit.AddKey(new Keyframe(10, 550));
            m_decentRateLimit.AddKey(new Keyframe(5, 250));
            MathBase.LinearizeCurve(m_decentRateLimit);

            m_yawEffectiveness = new AnimationCurve();
            m_yawEffectiveness.AddKey(new Keyframe(0, 1));
            m_yawEffectiveness.AddKey(new Keyframe(10 / (float)Constants.ms2knots, 0.98f));
            m_yawEffectiveness.AddKey(new Keyframe((float)m_transitionSpeed / (float)Constants.ms2knots, 0.085f));
            MathBase.LinearizeCurve(m_yawEffectiveness);

            // Set Gains
            m_altitudeSolver = new FPID { m_Kp = m_altitudeGain, m_Ki = m_altitudeIntegral, m_Kd = 0 };
            m_altitudeSolver.m_antiWindup = FPID.AntiWindupState.Active;
            m_pitchAngleSolver = new FPID { m_Kp = m_pitchGain, m_Ki = 0, m_Kd = 0 };
            m_bankSolver = new FPID { m_Kp = m_bankGain, m_Ki = 0, m_Kd = 0 };
            m_headingSolver = new FPID { m_Kp = m_headingGain, m_Ki = 0, m_Kd = 0 };
            m_headingSolver.m_antiWindup = FPID.AntiWindupState.Active;
            //m_driftSolver = new FPID { m_Kp = m_driftGain, m_Ki = 0, m_Kd = 0 };
            m_pedalHeadingSolver = new FPID { m_Kp = m_pedalHeadingGain, m_Ki = 0, m_Kd = 0 };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timestep"></param>
        internal void Compute(double timestep)
        {
            ComputePower(timestep);
            ComputeLateral(timestep);
            ComputeLongitudinal(timestep);
            ComputeDirectional(timestep);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timestep"></param>
        protected void ComputePower(double timestep)
        {
            // ------------------------------ Collect Data
            double u = _flcs.controller.m_core.u;
            double v = _flcs.controller.m_core.v;
            m_currentSpeed = Math.Sqrt((u * u) + (v * v));
            m_climbRate = _flcs.controller.m_core.δz;
            m_altitude = _flcs.controller.m_core.z;

            if (m_powerState == PowerState.AltitudeHold)
            {
                ////// ------------------------------ Outer Loop Altitude Control
                _presetAltitude = Mathf.MoveTowards(_presetAltitude, (float)_flcs.m_commands.m_commandAltitude, (float)timestep * 2);
                double m_heightError = _presetAltitude - m_altitude;
                double m_positiveClimb = _flcs.maximumClimbRate / Constants.toFtMin;
                double m_negativeClimb = m_decentRateLimit.Evaluate((float)m_currentSpeed) / Constants.toFtMin;
                m_altitudeSolver.m_maximum = m_positiveClimb;
                m_altitudeSolver.m_minimum = -m_negativeClimb;
                _flcs.m_commands.m_commandClimbRate = m_altitudeSolver.Compute(m_heightError, m_climbRate, timestep);
            }
            if (m_powerState == PowerState.AltitudeHold || m_powerState == PowerState.RateHold)
            {
                //// ------------------------------ Inner Loop Climb Control
                double m_climbError = _flcs.m_commands.m_commandClimbRate - m_climbRate;
                m_climbSolver.m_maximum = 1;
                m_climbSolver.m_minimum = 0.01;
                _flcs.m_collectiveInput = m_climbSolver.Compute(m_climbError, timestep);
            }
            _flcs.m_throttleInput = 1;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timestep"></param>
        protected void ComputeLateral(double timestep)
        {
            // Common Components
            m_rollAcceleration = _flcs.controller.m_core.δp * Mathf.Rad2Deg;
            double rollSense = m_rollRateSensor.Compute(_flcs.controller.m_core.p, timestep);
            m_rollRate = m_rollStructuralFilter.Compute(rollSense, timestep) * Mathf.Rad2Deg;
            m_turnRate = _flcs.controller.m_core.ωф;
            m_turnRadius = _flcs.controller.m_core.Rф;
            m_bankAngle = _flcs.controller.m_core.ф;
            m_heading = _flcs.controller.transform.eulerAngles.y;
            if (m_heading > 180) { m_heading -= 360f; }
            m_drift = _flcs.controller.m_rigidbody.linearVelocity.x;

            if (m_lateralState == LateralState.DriftControl)
            {
                double m_driftError = 0 - m_drift;
                double m_driftRate = _flcs.controller.m_core.δv;
                m_driftSolver.m_maximum = _flcs.maximumTurnBank;
                m_driftSolver.m_minimum = -_flcs.maximumTurnBank;
                _flcs.m_commands.m_commandBankAngle = m_driftSolver.Compute(m_driftError, m_driftRate, timestep);
            }
            if (m_lateralState == LateralState.HeadingHold)
            {
                double m_chdg = _flcs.m_commands.m_commandHeading;
                if (m_chdg > 180) { m_chdg -= 360f; }
                double m_headingError = m_chdg - m_heading;
                double m_headingAcceleration = _flcs.controller.m_core.δψ;
                m_headingSolver.m_maximum = _flcs.maximumTurnRate;
                m_headingSolver.m_minimum = -_flcs.maximumTurnRate;
                _flcs.m_commands.m_commandTurnRate = m_headingSolver.Compute(m_headingError, m_headingAcceleration, timestep);
                float m_factor = 1 - m_yawEffectiveness.Evaluate((float)m_currentSpeed);

                _flcs.m_commands.m_commandBankAngle = Math.Atan((_flcs.m_commands.m_commandTurnRate * (_flcs.m_Vm * 1.94384)) / 1091) * Mathf.Rad2Deg;
                double m_bankLim = Math.Atan((_flcs.maximumTurnRate * (_flcs.m_Vm * 1.94384)) / 1091) * Mathf.Rad2Deg;
                _flcs.m_commands.m_commandBankAngle = MathBase.Clamp(_flcs.m_commands.m_commandBankAngle, -m_bankLim, m_bankLim);
                _flcs.m_commands.m_commandBankAngle = MathBase.Clamp(_flcs.m_commands.m_commandBankAngle, -_flcs.maximumTurnBank, _flcs.maximumTurnBank) * m_factor;
            }

            // ------------------------------ Outer Loop Attitude Control
            _flcs.m_commands.m_commandBankAngle = MathBase.Clamp(_flcs.m_commands.m_commandBankAngle, -_flcs.maximumTurnBank, _flcs.maximumTurnBank);
            double m_rollAngleError = _flcs.m_commands.m_commandBankAngle - m_bankAngle;
            double m_rollDelta = _flcs.controller.m_core.δф;
            m_bankSolver.m_maximum = _flcs.maximumRollRate;
            m_bankSolver.m_minimum = -_flcs.maximumRollRate;
            _flcs.m_commands.m_commandRollRate = m_bankSolver.Compute(m_rollAngleError, m_rollDelta, timestep);


            // ------------------------------ Inner Loop Rate Control
            _flcs.m_commands.m_commandRollRate = MathBase.Clamp(_flcs.m_commands.m_commandRollRate, -_flcs.maximumRollRate, _flcs.maximumRollRate);
            double m_rollRateError = (_flcs.m_commands.m_commandRollRate) - m_rollRate;
            _flcs.m_roll = -m_rollRateSolver.Compute(m_rollRateError, m_rollAcceleration, timestep);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timestep"></param>
        protected void ComputeLongitudinal(double timestep)
        {
            m_pitchAcceleration = _flcs.controller.m_core.δq * Mathf.Rad2Deg;
            double pitchSense = m_pitchRateSensor.Compute(_flcs.controller.m_core.q, timestep);
            m_pitchRate = m_pitchStructuralFilter.Compute(pitchSense, timestep) * Mathf.Rad2Deg;
            m_pitchAngle = _flcs.controller.m_core.θ;
            m_α = _flcs.controller.m_core.α;

            // ------------------------------ Speed Data
            double u = _flcs.controller.m_core.m_bodyVelocity.x;
            double v = _flcs.controller.m_core.m_bodyVelocity.y;
            m_currentSpeed = Math.Sqrt((u * u) + (v * v));
            double δu = _flcs.controller.m_core.δu;
            double δv = _flcs.controller.m_core.δv;
            m_acceleration = Math.Sqrt((δu * δu) + (δv * δv));
            m_mach = m_currentSpeed / _flcs.controller.m_core.m_atmosphere.a;
            double m_sx = Math.Pow(1.0 - (6.8755856E-6) * m_altitude * 3.28, 5.2558797);
            double m_s1 = 1.0 + m_mach * m_mach * 0.2;
            double m_s2 = Math.Pow(m_s1, 3.5) - 1.0;
            double m_s3 = 1.0 + m_sx * m_s2;
            double m_s4 = Math.Pow(m_s3, 0.28571428571) - 1.0;
            double KIAS = 661.4786 * Math.Pow(5.0 * m_s4, 0.5);

            double m_spt = m_transitionSpeed / Constants.ms2knots;
            if (m_currentSpeed > m_spt && _flcs.m_gainState == Computer.GainState.TakeoffLanding) { _flcs.m_gainState = Computer.GainState.Cruise; }
            if (m_currentSpeed < m_spt && _flcs.m_gainState == Computer.GainState.Cruise) { _flcs.m_gainState = Computer.GainState.TakeoffLanding; }
            double dynamicPressue = _flcs.controller.m_core.m_atmosphere.qc * 0.02;
            double m_staticPressure = _flcs.controller.m_core.m_atmosphere.Ps;
            double m_pressureFactor = 1 - ((dynamicPressue / 60) * (1 - pressureGain));
            m_pressureFactor = MathBase.Clamp(m_pressureFactor, pressureGain, 1);
            if (double.IsNaN(m_pressureFactor) || double.IsInfinity(m_pressureFactor)) { m_pressureFactor = 0f; }

            // ------------------------------ Outer Loop Speed Control
            if (m_longitudinalMode == LongitudinalState.SpeedHold)
            {
                _presetSpeed = Mathf.MoveTowards(_presetSpeed, (float)_flcs.m_commands.m_commandSpeed, (float)timestep);
                m_speedSolver.m_integralLimit = 1 + ((dynamicPressue / 60) * 4);
                m_speedSolver.m_multiplier = m_pressureFactor;
                double m_speedError = _presetSpeed - u;
                double m_speedRate = δu;
                m_speedSolver.m_maximum = _flcs.maximumPitchAngle;
                m_speedSolver.m_minimum = -_flcs.minimumPitchAngle;
                _flcs.m_commands.m_commandPitchAngle = -m_speedSolver.Compute(m_speedError, m_speedRate, timestep);
            }


            // ------------------------------ Outer Loop Attitude Control
            _flcs.m_commands.m_commandPitchAngle = MathBase.Clamp(_flcs.m_commands.m_commandPitchAngle, -_flcs.minimumPitchAngle, _flcs.maximumPitchAngle);
            double m_pitchAngleError = _flcs.m_commands.m_commandPitchAngle - m_pitchAngle;
            double m_pitchDelta = _flcs.controller.m_core.δθ;
            m_pitchAngleSolver.m_maximum = _flcs.maximumPitchRate;
            m_pitchAngleSolver.m_minimum = -_flcs.maximumPitchRate;
            _flcs.m_commands.m_commandPitchRate = m_pitchAngleSolver.Compute(m_pitchAngleError, m_pitchDelta, timestep);

            // ------------------------------ Inner Loop Rate Control
            m_pitchRateSolver.m_multiplier = 1;
            _flcs.m_commands.m_commandPitchRate = MathBase.Clamp(_flcs.m_commands.m_commandPitchRate, -_flcs.maximumPitchRate, _flcs.maximumPitchRate);
            double m_pitchRateError = (_flcs.m_commands.m_commandPitchRate) - m_pitchRate;
            _flcs.m_pitch = -m_pitchRateSolver.Compute(m_pitchRateError, m_pitchAcceleration, timestep);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timestep"></param>
        protected void ComputeDirectional(double timestep)
        {
            m_yawAcceleration = _flcs.controller.m_core.δr * Mathf.Rad2Deg;
            double yawSense = m_yawRateSensor.Compute(_flcs.controller.m_core.r, timestep);
            m_yawRate = m_yawStructuralFilter.Compute(yawSense, timestep) * Mathf.Rad2Deg;
            m_heading = _flcs.controller.m_core.ψ;

            // ------------------------------ Outer Loop Heading Control
            double m_chdg = _flcs.m_commands.m_commandHeading;
            if (m_chdg > 180) { m_chdg -= 360f; }
            double m_headingError = m_chdg - m_heading;
            double m_headingAcceleration = _flcs.controller.m_core.δψ;
            m_headingSolver.m_maximum = _flcs.maximumYawRate;
            m_headingSolver.m_minimum = -_flcs.maximumYawRate;
            _flcs.m_commands.m_commandYawRate = m_headingSolver.Compute(m_headingError, m_headingAcceleration, timestep);

            // ------------------------------ Inner Loop Rate Control
            double m_yawControl = m_yawEffectiveness.Evaluate((float)m_currentSpeed);
            m_yawRateSolver.m_maximum = m_yawControl;
            m_yawRateSolver.m_minimum = -m_yawControl;
            double m_yawRateError = _flcs.m_commands.m_commandYawRate - m_yawRate;
            _flcs.m_yaw = m_yawRateSolver.Compute(m_yawRateError, m_yawAcceleration, timestep);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class RotaryComputer : Computer
    {
        public RotaryController m_helicopter;
        public Autopilot m_autopilot;

        public double collectiveLower = 5.500005;
        public double collectiveUpper = 14.5000217500326;
        // Long axis control limits
        public double LongitudinalLower = -10.00002250005625;
        public double LongitudinalUpper = 10.002200440088;
        // Lateral axis control limits
        public double LateralLower = -9.50019000380008;
        public double LateralUpper = 9.50019000380008;
        // Tail rotor control limits
        public double PedalLower = -10.6002120042401;
        public double PedalUpper = 19.5000039000008;
        // Collective couple deflection
        public double collectiveLateralCouple = 0;
        public double collectivePedalCouple = 0.5000039000008;
        // Lateral Yaw limit
        public double LateralYaw = 8;
        // Collective Yaw Limit;
        public double collectiveYaw = 6;
        // Collective Pitch Limit;
        public double collectivePitch = 6;
        public AnimationCurve B1CFcurve, B1CRcurve;

        double δc, δp, δe, δa;


        [Header("Output")]
        public double θom, θomF, θomR;
        public double θoR;
        public double θct;
        public double Bɪc, BɪCF, BɪCR;
        public double Aɪc, AɪR;

        /// <summary>
        /// 
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            Computer m_computer = GetComponent<Computer>();
            m_autopilot._flcs = m_computer;
            m_autopilot.Initialize();

            // ----------------------------------- B1CR
            B1CRcurve = new AnimationCurve();
            B1CRcurve.AddKey(new Keyframe(-41.98069f, 0.48165f));
            B1CRcurve.AddKey(new Keyframe(-14.55958f, 0.48165f));
            B1CRcurve.AddKey(new Keyframe(10.57749f, 0.50459f));
            B1CRcurve.AddKey(new Keyframe(26.80112f, 0.49312f));
            B1CRcurve.AddKey(new Keyframe(58.10794f, 0.51606f));
            B1CRcurve.AddKey(new Keyframe(84.15904f, 0.53899f));
            B1CRcurve.AddKey(new Keyframe(99.92618f, 0.53899f));
            B1CRcurve.AddKey(new Keyframe(106.58649f, 1.27294f));
            B1CRcurve.AddKey(new Keyframe(113.24575f, 1.98394f));
            B1CRcurve.AddKey(new Keyframe(121.05228f, 2.79817f));
            B1CRcurve.AddKey(new Keyframe(129.78070f, 3.78440f));
            B1CRcurve.AddKey(new Keyframe(136.44049f, 4.50688f));
            B1CRcurve.AddKey(new Keyframe(141.26224f, 5.01147f));
            B1CRcurve.AddKey(new Keyframe(150.40314f, 5.02294f));
            B1CRcurve.AddKey(new Keyframe(160.22851f, 5.01147f));
            MathBase.LinearizeCurve(B1CRcurve);

            // ----------------------------------- B1CF
            B1CFcurve = new AnimationCurve();
            B1CFcurve.AddKey(new Keyframe(-41.56352f, -0.48471f));
            B1CFcurve.AddKey(new Keyframe(-18.53420f, -0.46305f));
            B1CFcurve.AddKey(new Keyframe(5.63518f, -0.46437f));
            B1CFcurve.AddKey(new Keyframe(29.57655f, -0.48860f));
            B1CFcurve.AddKey(new Keyframe(51.92182f, -0.46689f));
            B1CFcurve.AddKey(new Keyframe(75.17915f, -0.47962f));
            B1CFcurve.AddKey(new Keyframe(98.89251f, -0.49238f));
            B1CFcurve.AddKey(new Keyframe(106.41694f, 0.11466f));
            B1CFcurve.AddKey(new Keyframe(113.71336f, 0.71025f));
            B1CFcurve.AddKey(new Keyframe(121.23779f, 1.34021f));
            B1CFcurve.AddKey(new Keyframe(127.85016f, 1.94730f));
            B1CFcurve.AddKey(new Keyframe(134.69055f, 2.47415f));
            B1CFcurve.AddKey(new Keyframe(140.61889f, 3.02397f));
            B1CFcurve.AddKey(new Keyframe(149.96743f, 3.03492f));
            B1CFcurve.AddKey(new Keyframe(160.68404f, 3.01141f));
            MathBase.LinearizeCurve(B1CFcurve);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_timestep"></param>
        public override void Compute(double _timestep)
        {
            base.Compute(_timestep);
            if (m_mode == Mode.Augmented) { ComputeSAS(); }
            if (m_mode == Mode.Autonomous) { m_autopilot.Compute(_timestep); }
            ComputeFlightControls();
        }
        /// <summary>
        /// 
        /// </summary>
        public override void EnableSceneAutopilot()
        {
            base.EnableSceneAutopilot();
            m_modeStorage = m_mode;
            m_mode = Mode.Autonomous;
            m_autopilot.m_lateralState = Autopilot.LateralState.HeadingHold;
            m_autopilot.m_longitudinalMode = Autopilot.LongitudinalState.SpeedHold;
        }
        /// <summary>
        /// 
        /// </summary>
        public override void DisableSceneAutopilot()
        {
            base.DisableSceneAutopilot();
            m_mode = m_modeStorage;
        }

        /// <summary>
        /// 
        /// </summary>
        private void ComputeFlightControls()
        {
            if (m_mode == Mode.Manual || m_mode == Mode.Augmented)
            {
                if (controller.allOk)
                {
                    // Collect Input Variables
                    m_pitch = b_pitchInput;
                    m_roll = b_rollInput;
                    m_yaw = b_yawInput;
                    m_pitchTrim = b_pitchTrimInput;
                    m_rollTrim = b_rollTrimInput;
                    m_throttleInput = b_throttleInput;
                    m_collectiveInput = b_collectiveInput;
                }
            }

            // Rotor Controls
            δc = m_collectiveInput;
            δe = m_pitch;
            δa = -m_roll;
            δp = -m_yaw;


            if (m_helicopter.m_configuration == RotaryController.RotorConfiguration.Conventional)
            {
                double δθom = collectiveLower + ((collectiveUpper - collectiveLower) * δc);
                δθom = MathBase.Clamp(δθom, collectiveLower, collectiveUpper);
                double δBɪc = MathBase.RemapRange(δe, -1, 1, LongitudinalLower, LongitudinalUpper);
                δBɪc = MathBase.Clamp(δBɪc, LongitudinalLower, LongitudinalUpper);
                double δAɪc = MathBase.RemapRange(δa, -1, 1, LateralLower, LateralUpper) + (collectiveLateralCouple * δc);
                δAɪc = MathBase.Clamp(δAɪc, LateralLower, LateralUpper);
                double δθctp = MathBase.RemapRange(δp, -1, 1, PedalLower, PedalUpper);
                double δθct = δθctp + (collectivePedalCouple * δc);

                θom = (δθom * Mathf.Deg2Rad);
                Bɪc = (δBɪc * Mathf.Deg2Rad) + Bɪc_afcs;
                Aɪc = (δAɪc * Mathf.Deg2Rad) + Aɪc_afcs;
                if (m_helicopter.m_torqueMode == RotaryController.TorqueMode.Conventional) { θct = (δθct * Mathf.Deg2Rad) + θt_afcs; }
                else { θct = (δθctp * Mathf.Deg2Rad) + θt_afcs; ; }

                m_helicopter.m_gearbox.m_primary.θocommand = θom;
                m_helicopter.m_gearbox.m_primary.Aɪc = Aɪc;
                m_helicopter.m_gearbox.m_primary.Bɪc = Bɪc;

                m_helicopter.m_gearbox.m_secondary.θocommand = θct;
                m_helicopter.m_gearbox.m_secondary.Aɪc = 0;
                m_helicopter.m_gearbox.m_secondary.Bɪc = 0;
            }
            if (m_helicopter.m_configuration == RotaryController.RotorConfiguration.Syncrocopter)
            {
                double δθom = collectiveLower + ((collectiveUpper - collectiveLower) * δc);
                δθom = MathBase.Clamp(δθom, collectiveLower, collectiveUpper);
                double δBɪc = MathBase.RemapRange(δe, -1, 1, LongitudinalLower, LongitudinalUpper);
                δBɪc = MathBase.Clamp(δBɪc, LongitudinalLower, LongitudinalUpper);
                double δAɪc = MathBase.RemapRange(δa, -1, 1, LateralLower, LateralUpper) + (collectiveLateralCouple * δc);
                δAɪc = MathBase.Clamp(δAɪc, LateralLower, LateralUpper);

                // Lateral Yaw Controls
                double δAɪR = MathBase.RemapRange(δp, -1, 1, -LateralYaw, LateralYaw);
                δAɪR = MathBase.Clamp(δAɪR, -LateralYaw, LateralYaw);


                θom = (δθom * Mathf.Deg2Rad);
                Bɪc = (δBɪc * Mathf.Deg2Rad) + Bɪc_afcs;
                Aɪc = (δAɪc * Mathf.Deg2Rad) + Aɪc_afcs;
                AɪR = (δAɪR * Mathf.Deg2Rad) + θt_afcs;

                m_helicopter.m_gearbox.m_primary.θocommand = θom;
                m_helicopter.m_gearbox.m_primary.Aɪc = Aɪc;
                m_helicopter.m_gearbox.m_primary.Bɪc = Bɪc;

                m_helicopter.m_gearbox.m_secondary.θocommand = θom;
                m_helicopter.m_gearbox.m_secondary.Aɪc = Aɪc;
                m_helicopter.m_gearbox.m_secondary.Bɪc = Bɪc;
            }
            if (m_helicopter.m_configuration == RotaryController.RotorConfiguration.Coaxial)
            {
                double δθom = collectiveLower + ((collectiveUpper - collectiveLower) * δc);
                δθom = MathBase.Clamp(δθom, collectiveLower, collectiveUpper);
                double δBɪc = MathBase.RemapRange(δe, -1, 1, LongitudinalLower, LongitudinalUpper);
                δBɪc = MathBase.Clamp(δBɪc, LongitudinalLower, LongitudinalUpper);
                double δAɪc = MathBase.RemapRange(δa, -1, 1, LateralLower, LateralUpper);
                δAɪc = MathBase.Clamp(δAɪc, LateralLower, LateralUpper);

                // Yaw Control
                double δθctp = δp * collectiveYaw;

                θom = (δθom * Mathf.Deg2Rad);
                Bɪc = (δBɪc * Mathf.Deg2Rad) + Bɪc_afcs;
                Aɪc = (δAɪc * Mathf.Deg2Rad) + Aɪc_afcs;
                θoR = (δθctp * Mathf.Deg2Rad);

                m_helicopter.m_gearbox.m_primary.θocommand = θom + θoR;
                m_helicopter.m_gearbox.m_primary.Aɪc = Aɪc;
                m_helicopter.m_gearbox.m_primary.Bɪc = Bɪc;

                m_helicopter.m_gearbox.m_secondary.θocommand = θom - θoR;
                m_helicopter.m_gearbox.m_secondary.Aɪc = Aɪc;
                m_helicopter.m_gearbox.m_secondary.Bɪc = Aɪc;
            }
            if (m_helicopter.m_configuration == RotaryController.RotorConfiguration.Tandem)
            {
                double δθom = collectiveLower + ((collectiveUpper - collectiveLower) * δc);
                δθom = MathBase.Clamp(δθom, collectiveLower, collectiveUpper);
                double δAɪc = MathBase.RemapRange(δa, -1, 1, LateralLower, LateralUpper) + (collectiveLateralCouple * δc);
                δAɪc = MathBase.Clamp(δAɪc, LateralLower, LateralUpper);

                θom = (δθom * Mathf.Deg2Rad);
                double θrdb = (δe * collectivePitch) * Mathf.Deg2Rad;
                θomF = θom - (θrdb + Bɪc_afcs);
                θomR = θom + (θrdb + Bɪc_afcs);
                Aɪc = (δAɪc * Mathf.Deg2Rad) + Aɪc_afcs;
                BɪCF = B1CFcurve.Evaluate((float)controller.m_core.Vkts) * Mathf.Deg2Rad;
                BɪCR = B1CRcurve.Evaluate((float)controller.m_core.Vkts) * Mathf.Deg2Rad;

                // Rotor 1
                if (m_helicopter.m_gearbox.m_primary.rotorConfiguration == RotaryController.RotorConfiguration.Tandem)
                {
                    if (m_helicopter.m_gearbox.m_primary.tandemPosition == SilantroRotor.TandemPosition.Forward)
                    {
                        m_helicopter.m_gearbox.m_primary.θocommand = θomF;
                        m_helicopter.m_gearbox.m_primary.Aɪc = Aɪc;
                        m_helicopter.m_gearbox.m_primary.Bɪc = BɪCF;
                    }
                    if (m_helicopter.m_gearbox.m_primary.tandemPosition == SilantroRotor.TandemPosition.Rear)
                    {
                        m_helicopter.m_gearbox.m_primary.θocommand = θomR;
                        m_helicopter.m_gearbox.m_primary.Aɪc = Aɪc;
                        m_helicopter.m_gearbox.m_primary.Bɪc = BɪCR;
                    }
                }

                // Rotor 2
                if (m_helicopter.m_gearbox.m_secondary.rotorConfiguration == RotaryController.RotorConfiguration.Tandem)
                {
                    if (m_helicopter.m_gearbox.m_secondary.tandemPosition == SilantroRotor.TandemPosition.Forward)
                    {
                        m_helicopter.m_gearbox.m_secondary.θocommand = θomF;
                        m_helicopter.m_gearbox.m_secondary.Aɪc = Aɪc;
                        m_helicopter.m_gearbox.m_secondary.Bɪc = BɪCF;
                    }
                    if (m_helicopter.m_gearbox.m_secondary.tandemPosition == SilantroRotor.TandemPosition.Rear)
                    {
                        m_helicopter.m_gearbox.m_secondary.θocommand = θomR;
                        m_helicopter.m_gearbox.m_secondary.Aɪc = Aɪc;
                        m_helicopter.m_gearbox.m_secondary.Bɪc = BɪCR;
                    }
                }
            }
        }

        #region Stability Augmentation



        public double Gbθ = 0.281f;
        public double Gbxlon = 0.363f;
        public double Gbq = 0.727f;

        public double Gaф = -0.133f;
        public double Gaxlat = 0.475f;
        public double Gap = -0.096f;

        public double Gθtr = 0.335f;
        public double GθtѰ = 0.133f;

        double θh;
        readonly double θ0;
        private double фh;
        private readonly double ф0;
        //private double Ѱh;
        //private readonly double Ѱ0;
        //readonly double hh, h0;
        private double δθ;
        private double δф;
        //private readonly double δѰ;
        //private readonly double δh;
        double δxlat, δxlon;
        readonly double xlon0 = 0.0000000001, xlat0 = 0.0000000001;

        public ControlState m_pitchSAS;
        public ControlState m_pitchLeveler;
        public ControlState m_pitchRateLimiter;
        public ControlState m_pitchTrimHold = ControlState.Off;

        public ControlState m_rollSAS;
        public ControlState m_rollLeveler;
        public ControlState m_rollRateLimiter;
        public ControlState m_rollTrimHold = ControlState.Off;

        public ControlState m_yawRateLimiter;
        public ControlState m_yawTrimHold = ControlState.Off;

        public double δpθ;
        public double δpq;
        public double δpc;

        public double δrф;
        public double δrp;
        public double δrc;

        public double δyr;
        public double δyѰ;

        [Header("Outputs")]
        public double Bɪc_afcs;
        public double Aɪc_afcs;
        public double θt_afcs;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timestep"></param>
        private void ComputeSAS()
        {
            θh = controller.m_core.θ * Mathf.Deg2Rad;
            фh = controller.m_core.ф * Mathf.Deg2Rad;
            //Ѱh = controller.m_core.ψ * Mathf.Deg2Rad;

            // ----------------------------------------- Pitch Channel
            δθ = θh - θ0;
            δxlon = δe - xlon0;

            if (m_pitchSAS == ControlState.Active)
            {
                if (m_pitchLeveler == ControlState.Active) { δpθ = (Gbθ * δθ); } else { δpθ = 0; }
                if (m_pitchRateLimiter == ControlState.Active) { δpq = (Gbq * controller.m_core.q); } else { δpq = 0; }
                if (m_pitchTrimHold == ControlState.Active) { δpc = (Gbxlon * δxlon * 0.01f); } else { δpc = 0; }
                Bɪc_afcs = δpθ + δpq + δpc;
            }
            else { Bɪc_afcs = 0; }

            // ----------------------------------------- Roll Channel
            δф = фh - ф0;
            δxlat = δa - xlat0;

            if (m_rollSAS == ControlState.Active)
            {
                if (m_rollLeveler == ControlState.Active) { δrф = (Gaф * δф); } else { δrф = 0; }
                if (m_rollRateLimiter == ControlState.Active) { δrp = (Gap * controller.m_core.p); } else { δrp = 0; }
                if (m_rollTrimHold == ControlState.Active) { δrc = (Gaxlat * δxlat * 0.01f); } else { δrc = 0; }
                Aɪc_afcs = δrф + δrp + δrc;
            }
            else { Aɪc_afcs = 0; }


            // ----------------------------------------- Yaw Channel


            if (m_yawRateLimiter == ControlState.Active)
            {
                δyr = (Gθtr * controller.m_core.r);
            }
            else { δyr = 0; }

            θt_afcs = δyѰ + δyr;
        }


        #endregion

    }
}