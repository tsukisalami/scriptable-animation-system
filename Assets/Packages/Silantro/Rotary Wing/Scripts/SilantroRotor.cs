using System;
using UnityEngine;
using Oyedoyin.Common;
using Oyedoyin.Analysis;
using Oyedoyin.Common.Misc;
using Oyedoyin.Mathematics;
using static Oyedoyin.RotaryWing.RotaryController;

namespace Oyedoyin.RotaryWing
{
    public class SilantroRotor : MonoBehaviour
    {
        #region Enums
        public enum SimulationModel { UniformInflow, NonUniformInflow }
        public enum RotorType { MainRotor, TailRotor }
        public enum TwistType { None, Upward, Downward }
        public enum BladeFlappingState { Dynamic, Static }
        public enum GroundEffectState { Neglect, Consider }
        public enum SurfaceFinish { SmoothPaint, PolishedMetal, ProductionSheetMetal, MoldedComposite, PaintedAluminium }
        public enum SoundState { Active, Silent }
        public enum TandemAnalysisMethod { MT1, MT2, Harris }
        public enum CoaxialPosition { Top, Bottom }
        public enum TandemPosition { Forward, Rear }
        public enum SyncroPosition { Left, Right }
        public enum VisulType { None, Partial, Complete }
        public enum FoilType { Conventional, Advanced }
        #endregion

        #region Enums Properties

        public SimulationModel model = SimulationModel.NonUniformInflow;
        public RotaryController.RotorConfiguration rotorConfiguration = RotaryController.RotorConfiguration.Conventional;
        public RotorType rotorType = RotorType.MainRotor;
        public RotationAxis rotationAxis = RotationAxis.Y;
        public RotationDirection rotorDirection = RotationDirection.CW;
        public TwistType twistType = TwistType.None;
        public WeightUnit weightUnit = WeightUnit.Kilogram;
        public BladeFlappingState flappingState = BladeFlappingState.Static;
        public GroundEffectState groundEffect = GroundEffectState.Neglect;
        public SurfaceFinish surfaceFinish = SurfaceFinish.PaintedAluminium;
        public SoundState soundState = SoundState.Silent;
        public TandemAnalysisMethod tandemAnalysis = TandemAnalysisMethod.MT1;
        public CoaxialPosition coaxialPosition = CoaxialPosition.Top;
        public TandemPosition tandemPosition = TandemPosition.Forward;
        public SyncroPosition syncroPosition = SyncroPosition.Left;
        public VisulType visualType = VisulType.None;
        public FoilType foilType = FoilType.Conventional;

        #endregion

        #region Properties

        public float rotorRadius = 1f;
        [Range(2, 8)] public int Nb = 2;
        public float funcionalRPM = 1500f;
        [Range(0, 1f)] public float rotorHeadRadius = 0.1f;
        [Range(0, 0.2f)] public float re = 0.01f;
        [Range(-0.3f, 0.3f)] public float rootDeviation = 0.0f;
        [Range(0, 0.4f)] public float rootCutOut = 0.01f;
        [Range(0, 1.5f)] public float bladeChord = 0.1f;
        [Range(0, 20)] public float bladeWashout = 0f;
        [Range(0, 10)] public int subdivision = 4;
        public float bladeMass = 20f;
        public float bladeRadius;
        public float hingeOffset;
        public float rootcut = 0.01f;
        [HideInInspector] public float rootDeflection;
        public float spanEfficiency;
        public float aspectRatio;
        public float torqueFactor = 1;
        public float weightFactor, actualWeight;

        public AnimationCurve swirlCorrection;
        public AnimationCurve powerCorrection;
        public AnimationCurve thrustCorrection;
        public AnimationCurve kov;
        public AnimationCurve groundCorrection;

        #endregion

        #region Simulation Properties

        private double ub, vb, wb;
        private double ph, qh, rh;
        public double pw, qw, rw;
        private double ps, qs, rs;
        public double uh, vh, wh;
        public double uw, ww;
        public double lx, ly, lz;
        public Matrix3x3 BTS, STB;
        public Vector m_hubVelocity;
        public Vector m_shaftVelocity;
        public Vector m_localVelocity;
        public Vector m_hubRates, m_shaftRates, m_localRates;
        public Vector3 groundAxis = new Vector3(0.0f, -1.0f, 0.0f);

        public double μ, Ωm, Ω, αMax;
        public FComplex λ;
        public double λr, ν;
        public FComplex νi, ꭙ;
        public double δc;
        public double ωr;
        public float AGL = 1, zR;

        public double δν;
        public double aɪ;
        public double ɣ;
        public double β0;
        public double a1c, b1c, β1c, β1s;
        public double δθ0;
        public double ρ = 1.225;
        public double Mβ1c, Rβ1s;
        public double υz, υTip, υM;
        public double CL = 0;
        public double CD = 0;
        public double cdw, cf, Cf;
        public double m_thickness, m_ref_area, m_wetted_area;
        public double m_surface_k;

        [Header("Refined Properties")]
        public double R;
        public double A;
        public double θtw;
        public double σ;
        public double CLα = 5.73;
        public double e = 0.50;
        public double cr;
        public double Iβ;
        public double J;
        public double Mw;
        public double Ωmax;
        public double B = 0.97;
        public float δ3 = 0;
        private double δ3r;
        public double ωN;
        public double τν = 0.20f;
        public double τδ3 = 0.1f;
        public double m_height = 1;

        [HideInInspector] public Vector3 rootLeadingEdge, tipLeadingEdge, rootTrailingEdge, tipTrailingEdge;
        [HideInInspector] public Vector3 quaterRootChordPoint, quaterTipChordPoint, skewDistance;

        #endregion

        #region Connections
        public RotaryController m_controller;
        private SilantroRotor m_rotor;
        // Base airfoils
        public SilantroAirfoil m_rootAirfoil;
        public SilantroAirfoil m_tipAirfoil;
        // Airfoils with Mach corrections
        public Superfoil m_rootSuperfoil;
        public Superfoil m_tipSuperfoil;

        public Transform m_hinge;
        public Transform m_forcePoint;
        Quaternion baseRotation;
        Vector3 m_direction;

        #endregion

        #region Sound

        public AudioSource m_soundPoint;
        public AudioClip m_bladeChop;
        public float m_maximumPitch = 1.0f;
        public float m_interiorVolume = 0.2f;
        public float m_rotorVolume, m_rotorPitch;

        #endregion

        #region Visuals

        public Material[] normalRotor;
        public Material[] blurredRotor;
        public Color blurredRotorColor;
        public Color normalRotorColor;
        public float normalBalance = 0.2f;

        #endregion

        // ------------------------------ Input
        [Header("Input")]
        public double θocommand;
        public double θom;
        public double Bɪc;
        public double Aɪc;

        [Header("Output")]
        public double δT;
        public double δv = 1, δQ = 1, δf;
        public double coneAngle;
        public double featherAngle;
        public double CT, CQ;
        public FComplex CQ0, CQI, CQIB;
        public double CTσ, CY;
        public double FH, FY, Thrust;
        public double Torque;
        public double Inertia;
        public Vector m_shaftForce, m_hubForce;
        public Vector m_shaftMoment, m_hubMoment;
        public Vector3 m_force;
        public Vector3 m_moment;

        public bool drawFoils;
        public bool allOk;

        public FComplex tsum, δxt, CTt, CTi, dsum;
        public FComplex[] xtx, kpx, kix, CLx, stx;
        public FComplex[] υi, υt, λt, α, CLf, CDf;
        public double[] vMach;

        public float m_rotor_speed;
        public float m_rotor_alpha;
        public Vector3 m_rotor_rotation;


        /// <summary>
        /// 
        /// </summary>
        public void Initialize()
        {

            // ------------------------------------------- Base
            if (weightUnit == WeightUnit.Kilogram) { weightFactor = 1f; }
            if (weightUnit == WeightUnit.Pounds) { weightFactor = (1 / 2.205f); }
            actualWeight = bladeMass * weightFactor;
            A = Mathf.PI * bladeRadius * bladeRadius;
            J = Nb * actualWeight * Mathf.Pow((bladeRadius * 0.55f), 2);
            float m = actualWeight / bladeRadius;
            Iβ = ((m * Mathf.Pow(bladeRadius, 3)) / 3) * Mathf.Pow((1 - re), 3);
            double solidity = (Nb * bladeChord) / (Mathf.PI * bladeRadius);
            if (rotorType != RotorType.TailRotor)
            {
                // Torque is in the opposite direction of rotor rotation
                if (rotorDirection == RotationDirection.CCW) { torqueFactor = 1; }
                if (rotorDirection == RotationDirection.CW) { torqueFactor = -1; }
            }
            else { torqueFactor = 1; }
            rootcut = (rotorRadius * rootCutOut) - rotorHeadRadius;
            bladeRadius = ((1 - rootCutOut) * rotorRadius) + rootcut;
            hingeOffset = re * bladeRadius;
            groundAxis.Normalize();
            aspectRatio = ((bladeRadius * bladeRadius) / (bladeRadius * bladeChord));

            Mw = (actualWeight * 9.8f * Mathf.Pow(bladeRadius, 2)) / 2;
            Ωmax = funcionalRPM * 0.104733f;
            cr = bladeChord;
            e = hingeOffset;
            σ = solidity;
            R = bladeRadius;

            if (τν <= 0) { τν = 0.2; }
            if (τδ3 <= 0) { τδ3 = 0.1; }
            float eA = Mathf.Pow((Mathf.Tan(0f * Mathf.Deg2Rad)), 2);
            float eB = 4f + ((aspectRatio * aspectRatio) * (1 + eA));
            spanEfficiency = 2 / (2 - aspectRatio + Mathf.Sqrt(eB));
            RMath.DrawCorrectionCurves(out swirlCorrection, out powerCorrection, out thrustCorrection);
            if (groundEffect == GroundEffectState.Consider) { RMath.PlotGroundCorrection(out groundCorrection); }

            // ------------------------------------- Surface Factor
            if (surfaceFinish == SurfaceFinish.MoldedComposite) { m_surface_k = 0.17f; }
            if (surfaceFinish == SurfaceFinish.PaintedAluminium) { m_surface_k = 3.33f; }
            if (surfaceFinish == SurfaceFinish.PolishedMetal) { m_surface_k = 0.50f; }
            if (surfaceFinish == SurfaceFinish.ProductionSheetMetal) { m_surface_k = 1.33f; }
            if (surfaceFinish == SurfaceFinish.SmoothPaint) { m_surface_k = 2.08f; }

            // ------------------------------------- Transformation Matrices
            Vector3 m_rot = transform.localEulerAngles;
            if (rotorType == RotorType.MainRotor) { m_rot.y = 0f; } else { m_rot.x = 0f; }
            Vector3 m_orientation = Transformation.UnityToEuler(m_rot);
            float фs = m_orientation.x * Mathf.Deg2Rad;
            float θs = m_orientation.y * Mathf.Deg2Rad;
            float cosθs = Mathf.Cos(θs); float sinθs = Mathf.Sin(θs);
            float cosфs = Mathf.Cos(фs); float sinфs = Mathf.Sin(фs);
            Vector3 r1 = new Vector3(cosθs, (sinθs * sinфs), (sinθs * cosфs));
            Vector3 r2 = new Vector3(0, cosфs, -sinфs);
            Vector3 r3 = new Vector3(-sinθs, (cosθs * sinфs), (cosθs * cosфs));
            BTS = new Matrix3x3(r1, r2, r3);
            STB = Matrix3x3.Transpose(BTS);
            Vector3 m_position = Transformation.UnityToVector(m_controller.m_rigidbody.worldCenterOfMass - transform.position);

            // ------------------------------------- Position and Twist
            float ω0 = (2 * Mathf.PI * funcionalRPM) / 60f;
            ωN = ω0 * Mathf.Sqrt(1 + (((3 * re) / (2 * (1 - re)))));
            lx = m_position.x;
            ly = m_position.y;
            lz = m_position.z;
            if (twistType == TwistType.None) { θtw = 0; }
            else if (twistType == TwistType.Downward) { θtw = -bladeWashout * Mathf.Deg2Rad; }
            else if (twistType == TwistType.Upward) { θtw = bladeWashout * Mathf.Deg2Rad; }
            δ3r = δ3 * Mathf.Deg2Rad;

            DrawContainers();
            if (m_rootAirfoil != null && m_tipAirfoil != null) { CLα = MathBase.EstimateSection(m_rootAirfoil.centerLiftSlope, m_tipAirfoil.centerLiftSlope, 0.75); }
            else { CLα = 5.73; }
            if (CLα <= 1) { CLα = 5.73; }
            if (m_bladeChop && soundState == SoundState.Active) { Handler.SetupSoundSource(transform, m_bladeChop, "_rotor_sound", 80f, true, true, out m_soundPoint); }

            if (visualType == VisulType.Complete || visualType == VisulType.Partial)
            {
                if (blurredRotor.Length > 0 && blurredRotor[0] != null) { blurredRotorColor = blurredRotor[0].color; }
                if (normalRotor.Length > 0 && normalRotor[0] != null) { normalRotorColor = normalRotor[0].color; }
            }
            if (rotorConfiguration != RotorConfiguration.Conventional) { m_controller.m_torqueMode = TorqueMode.Corrected; }
            if (rotorConfiguration == RotorConfiguration.Coaxial && coaxialPosition == CoaxialPosition.Bottom) { δv = 0.5616f; δQ = 1; }

            // ------------------------------------------- Tandem Config
            if (rotorConfiguration == RotorConfiguration.Tandem)
            {
                GameObject point = new GameObject("rotor_point_" + name);
                point.transform.parent = m_controller.m_core.transform;
                point.transform.localPosition = transform.localPosition;
                point.transform.localRotation = transform.localRotation;
                m_forcePoint = point.transform;
                baseRotation = m_forcePoint.localRotation;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Compute(double dt)
        {
            AnalyseForces(dt);
            AnalyseSound();
            AnalyseVisuals();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        private void AnalyseForces(double dt)
        {
            // --------------------------------- Controls
            if (δ3 > 10 && rotorType == RotorType.TailRotor)
            {
                δθ0 = (1 / τδ3) * (θocommand - (β0 * Math.Tan(δ3r)) - θom);
                θom += δθ0 * dt;
            }
            else { θom = θocommand; }
            Ωm = Ω;
            if (Ωm <= 0) { Ωm = 5.04733; }

            // --------------------------------- Rotate Model
            if (double.IsNaN(Ωm) || double.IsInfinity(Ωm)) { Ωm = 0.0f; }

            // Tandem Lateral Yaw
            if (rotorConfiguration == RotorConfiguration.Tandem && m_forcePoint != null)
            {
                float θs = (float)m_controller.m_computer.LateralYaw * m_controller._yawInput;
                m_forcePoint.transform.localRotation = baseRotation * Quaternion.AngleAxis(-θs, new Vector3(0, 0, 1));
            }

            #region Ground Effect
            // --------------------------------- Ground Effect
            if (groundEffect == GroundEffectState.Consider && rotorType == RotorType.MainRotor)
            {
                Ray groundCheck = new Ray(transform.position, groundAxis);

                if (Physics.Raycast(groundCheck, out RaycastHit groundHit, 1000, m_controller.m_core.groundLayer))
                { AGL = groundHit.distance; Debug.DrawLine(transform.position, groundHit.point, Color.red); }
                if (AGL > 999f) { AGL = 999f; }
                zR = AGL / bladeRadius; if (zR > 3f) { zR = 3f; }
                δT = groundCorrection.Evaluate(zR);
            }
            else { δT = 1; }

            #endregion

            #region Rotate Hinge

            m_rotor_speed = (float)(Ω * 9.548f * 5.0f * dt);
            if (m_rotor_speed > 0)
            {
                if (rotorDirection == RotationDirection.CW)
                {
                    if (rotationAxis == RotationAxis.X) { m_rotor_rotation = new Vector3(m_rotor_speed, 0, 0); }
                    if (rotationAxis == RotationAxis.Y) { m_rotor_rotation = new Vector3(0, m_rotor_speed, 0); }
                    if (rotationAxis == RotationAxis.Z) { m_rotor_rotation = new Vector3(0, 0, m_rotor_speed); }
                }
                if (rotorDirection == RotationDirection.CCW)
                {
                    if (rotationAxis == RotationAxis.X) { m_rotor_rotation = new Vector3(-1.0f * m_rotor_speed, 0, 0); }
                    if (rotationAxis == RotationAxis.Y) { m_rotor_rotation = new Vector3(0, -1.0f * m_rotor_speed, 0); }
                    if (rotationAxis == RotationAxis.Z) { m_rotor_rotation = new Vector3(0, 0, -1.0f * m_rotor_speed); }
                }
            }

            m_hinge.Rotate(m_rotor_rotation);

            #endregion

            float δY = 1;
            if (m_controller.m_torqueMode == TorqueMode.Corrected) { δY = (float)Math.Abs(m_controller.m_computer.m_yaw); }

            // --------------------------------- Collect Velocity
            ub = m_controller.m_core.u;
            vb = m_controller.m_core.v;
            wb = m_controller.m_core.w;
            ph = m_controller.m_core.p;
            qh = m_controller.m_core.q;
            rh = m_controller.m_core.r;

            // --------------------------------- Body to Shaft
            uh = ub + (qh * lz) - (rh * ly);
            vh = vb + (rh * lx) - (ph * lz);
            wh = wb + (ph * ly) - (qh * lx);
            m_hubVelocity = new Vector(uh, vh, wh);
            m_shaftVelocity = BTS * m_hubVelocity;
            if (double.IsNaN(m_hubVelocity.magnitude) || double.IsInfinity(m_hubVelocity.magnitude)) { m_hubVelocity = Vector.zero; }
            if (double.IsNaN(m_shaftVelocity.magnitude) || double.IsInfinity(m_shaftVelocity.magnitude)) { m_shaftVelocity = Vector.zero; }
            double β = Math.Atan(m_shaftVelocity.y / m_shaftVelocity.x);
            if (double.IsNaN(β) || double.IsInfinity(β)) { β = 0.0; }

            // --------------------------------- Shaft to Wind
            uw = (m_shaftVelocity.x * Math.Cos(β)) + (m_shaftVelocity.y * Math.Sin(β)) + Mathf.Epsilon;
            ww = m_shaftVelocity.z - (Bɪc * Mathf.Deg2Rad * m_shaftVelocity.x) - (Aɪc * Mathf.Deg2Rad * m_shaftVelocity.y);
            m_localVelocity = new Vector(uw, 0, ww);
            m_hubRates = new Vector(ph, qh, rh);
            m_shaftRates = BTS * m_hubRates;


            // --------------------------------- Rate Transformation
            ps = m_shaftRates.x;
            qs = m_shaftRates.y;
            rs = m_shaftRates.z;
            pw = (ps * Math.Cos(β)) + (qs * Math.Sin(β));
            qw = (-ps * Math.Sin(β)) + (qs * Math.Cos(β));
            rw = rs;
            m_localRates = new Vector(pw, qw, rw);


            // --------------------------------- Central Inflow
            ωr = (Ω * R);
            if (ωr < 1) { ωr = 1; }
            μ = (uw / ωr);
            λr = (ww / ωr) - ν;
            λr = MathBase.Clamp(λr, -0.5, 0.5);
            if (double.IsNaN(μ) || double.IsInfinity(μ)) { μ = 0.0; }
            if (double.IsNaN(λr) || double.IsInfinity(λr)) { λ = 0.0; }
            double vx = 2 * Math.Sqrt((μ * μ) + (λr * λr));
            δν = (1 / τν) * ((CT / vx) - ν);
            ν += (δν * dt);
            ν = MathBase.Clamp(ν, -0.2, 0.2);
            if (double.IsNaN(δν) || double.IsInfinity(δν)) { δν = 0.0; }
            if (double.IsNaN(ν) || double.IsInfinity(ν)) { ν = 0.0; }
            νi = ((ww / ωr) - λ) * ωr;
            ꭙ = Math.Atan2(μ, λr);
            double θ75 = θom + (0.75f * θtw);
            Mβ1c = -Iβ * Ω * Ω * 3 * re / (2 * (1 - re)) * 2;
            Rβ1s = -Iβ * Ω * Ω * 3 * re / (2 * (1 - re)) * 2;


            if (model == SimulationModel.NonUniformInflow)
            {
                for (int i = 0; i < subdivision + 1; i++)
                {
                    double ri = (R * rootCutOut) + ((R * (1 - rootCutOut)) / subdivision) * (i);
                    double xi = ri / R;
                    xtx[i] = xi;
                    double vi = xi * Ωm * bladeRadius / 340f;
                    double CLαd = 0f;
                    if (vi < 0.75f) { CLαd = (0.1f / Math.Sqrt(1 - (vi * vi))) - 0.01f * vi; }
                    else if (vi > 0.75f) { CLαd = 0.677f * 0.744f * vi; }
                    double θr = (θom + xi * θtw);
                    double CLαr = CLαd * Mathf.Rad2Deg;

                    // ------------------------------------- Inflow
                    double µx = uw / (Ωm * ri);
                    double µz = -ww / (Ωm * ri);
                    if (double.IsNaN(µx) || double.IsInfinity(µx)) { µx = 0f; }
                    if (double.IsNaN(µz) || double.IsInfinity(µz)) { µz = 0f; }
                    FComplex υ = (Ωm * CLαr * Nb * bladeChord) / (16 * Mathf.PI) * (-1 + FComplex.Sqrt(1 + (32 * Mathf.PI * θr * ri) / (CLαr * Nb * bladeChord)));
                    if (υ.IsNaN() || υ.IsInfinity()) { υ = 0f; }
                    FComplex λh = (υ - ww) / (Ωm * ri);
                    if (λh.IsNaN() || λh.IsInfinity()) { λh = 0f; }
                    λ = RMath.ForwardInflow(µx, λh, µz, 1);
                    if (λ.IsNaN() || λ.IsInfinity()) { λ = 0f; }
                    FComplex ϕr = FComplex.Atan(λ);
                    if (ϕr.IsNaN() || ϕr.IsInfinity()) { ϕr = 0f; }
                    FComplex αr = θr - ϕr;

                    υz = uw;
                    υTip = (Ωm * ri) + υz;
                    double soundSpeed = m_controller.m_core.m_atmosphere.a;
                    υM = υTip / soundSpeed;
                    vMach[i] = υM;

                    // ------------------------------------- Extract Foil Coefficients
                    FComplex αs = αr * Mathf.Rad2Deg;
                    if (αs.m_real > 89) { αs = 89f; }
                    if (αs.m_real < -89) { αs = -89f; }
                    if (i == 0) { αMax = αs.m_real; }
                    if (αs.m_real > 0) { if (αs.m_real > αMax) { αMax = αs.m_real; } }
                    else { if (αs.m_real < αMax) { αMax = αs.m_real; } }
                    if (αs.IsNaN() || αs.IsInfinity()) { αs = 0f; }
                    α[i] = αs;

                    double rootCL = 0;
                    double tipCL = 0;
                    double rootCD = 0;
                    double tipCD = 0;
                    double CD_foil;

                    if (foilType == FoilType.Conventional)
                    {
                        rootCL = m_rootAirfoil.liftCurve.Evaluate((float)αs.m_real);
                        tipCL = m_tipAirfoil.liftCurve.Evaluate((float)αs.m_real);
                        rootCD = m_rootAirfoil.dragCurve.Evaluate((float)αs.m_real);
                        tipCD = m_tipAirfoil.dragCurve.Evaluate((float)αs.m_real);
                        CL = MathBase.EstimateSection(rootCL, tipCL, xi);
                        CD_foil = MathBase.EstimateSection(rootCD, tipCD, xi);
                    }
                    else
                    {
                        rootCL = m_rootSuperfoil.GetCL(υM, αs.m_real);
                        tipCL = m_tipSuperfoil.GetCL(υM, αs.m_real);
                        rootCD = m_rootSuperfoil.GetCD(υM, αs.m_real);
                        tipCD = m_tipSuperfoil.GetCD(υM, αs.m_real);

                        CL = MathBase.EstimateSection(rootCL, tipCL, xi);
                        CD_foil = MathBase.EstimateSection(rootCD, tipCD, xi);
                    }
                    CLx[i] = CL;

                    #region Drag Corrections

                    //-----------------------------------------Wave Drag
                    double rootMcr = m_rootAirfoil.Mcr;
                    double tipMcr = m_tipAirfoil.Mcr;
                    double panelMcr = MathBase.EstimateSection(rootMcr, tipMcr, xi);
                    if (panelMcr < 0.5f) { panelMcr = ((rootMcr * 2) + (tipMcr * 2)) / 2f; }
                    double Mcrit = panelMcr - (0.1f * CL);
                    double M_Mcr = 1 - Mcrit;
                    double cdw0 = 0.0264f;

                    double δm = 1 - Mcrit;
                    double xf = (-8f * (υM - (1 - (0.5f * δm)))) / δm;
                    double fmx = Math.Exp(xf);
                    double fm = 1 / (1 + fmx);
                    double kdw = 0.5f;
                    double kdwm = 0.05f;
                    double dx = Math.Pow((Math.Pow((υM - kdwm), 2f) - 1), 2) + Math.Pow(kdw, 4);
                    double correction = Math.Pow(Math.Cos(0 * Mathf.Deg2Rad), 2.5f); //Sweep correction...useful later
                    cdw = (fm * cdw0 * kdw) / (Math.Pow(dx, 0.25f)) * correction;
                    if (cdw < 0) { cdw = 0f; }

                    //----------------------------------------- Skin Friction Drag
                    m_ref_area = bladeRadius * bladeChord;
                    m_thickness = MathBase.EstimateSection(m_rootAirfoil.maximumThickness, m_tipAirfoil.maximumThickness, xi);
                    m_wetted_area = m_ref_area * (1.977f + (0.52f * m_thickness));
                    cf = MathBase.EstimateSkinDragCoefficient(bladeChord, bladeChord, (m_localVelocity.magnitude * 1.94), ρ, m_controller.m_core.m_atmosphere.μ, υM, m_surface_k);
                    Cf = (m_wetted_area / m_ref_area) * cf;
                    if (Cf > CD_foil) { Cf = CD_foil; }

                    #endregion

                    CD = CD_foil + cdw + Cf;

                    // ------------------------------------- Calculate Numeric Coefficients
                    FComplex CTB = (Nb * CL * bladeChord * B * B) / (2 * Mathf.PI * bladeRadius);
                    CTi = CTt - (0.5f * (((Nb * CL * bladeChord * xi * xi) / (2 * Mathf.PI * bladeRadius)) + CTB)) * (1 - B);
                    CT = CTi.m_real;
                    FComplex mta = 0;
                    FComplex mtb = 0f;
                    kpx[i] = (Nb * bladeChord * CD * Math.Pow(xi, 3)) / (2 * Mathf.PI * bladeRadius);
                    kix[i] = (Nb * bladeChord * CL * FComplex.Sin(ϕr) * xi * xi * xi) / (2 * Mathf.PI * bladeRadius);
                    if (i == subdivision)
                    {
                        for (int a = 1; a < subdivision; a++) { mta += kpx[a]; }
                        CQ0 = (0.085f / 2) * (kpx[0] + kpx[subdivision] + 2 * mta);
                    }
                    CQIB = (Nb * bladeChord * CL * FComplex.Sin(ϕr) * B * B * B) / (2 * Mathf.PI * bladeRadius);

                    if (i == subdivision)
                    {
                        for (int a = 0; a < subdivision; a++) { mtb += kix[a]; }
                        CQI = ((0.085f / 2) * (kix[0] + kix[subdivision] + 2 * mtb)) - ((0.5f) * (CQIB + kix[subdivision]) * (1 - B));
                    }
                }



                #region Corrections

                // ------------------------------------- Tip Correction
                δxt = (xtx[subdivision] - xtx[0]) / (subdivision); tsum = 0f;
                for (int i = 0; i < subdivision + 1; i++)
                {
                    FComplex xi = xtx[i];
                    FComplex fx = (Nb * CLx[i] * bladeChord * xi * xi) / (2 * Mathf.PI * bladeRadius);
                    if (i > 0 && i < subdivision) { fx *= 2; }
                    tsum += fx;
                }
                CTt = (δxt / 2) * tsum;
                double br = CTt.m_real;
                if (br < 0.006f) { B = 1 - (0.06f / Nb); }
                else if (br > 0.006f) { B = 1 - (Math.Sqrt(2.27f * br - 0.01f) / Nb); }
                CTσ = CT / σ;

                // ------------------------------------- Torque Correction
                FComplex δCTi = FComplex.Sqrt(2 * CTi);
                for (int i = 0; i < subdivision + 2; i++) { if (i == 0) { stx[i] = δCTi; } else { stx[i] = xtx[i - 1]; } }
                δxt = (stx[subdivision + 1] - stx[0]) / (subdivision + 2);
                dsum = 0f;
                for (int i = 0; i < subdivision + 3; i++)
                {
                    FComplex xt = stx[0] + (δxt * (i + 1));
                    FComplex fx;
                    if (i == 0) { fx = RMath.SwirlFactor(CTi, δCTi); }
                    else { fx = RMath.SwirlFactor(CTi, xt); }
                    if (i > 0 && i < subdivision + 1) { fx *= 2; }
                    dsum += fx;
                }

                FComplex δCQI = swirlCorrection.Evaluate((float)CT) * CQI;
                FComplex DL = CTi * ρ * FComplex.Pow((Ωm * R), 2);
                FComplex DLx = DL * (CTi / σ);
                float δCP = powerCorrection.Evaluate((float)DLx.m_real);
                FComplex cq = (CQI + δCQI + CQ0) * δCP;
                CQ = cq.m_real;

                #endregion
            }
            else
            {
                // --------------------------------- Thrust
                double at = (0.5f * B * B) + (0.25f * μ * μ);
                double bt = (0.3333f * B * B * B) + (0.5f * B * μ * μ) - (0.1415f * μ * μ * μ);
                double ct = (0.25f * B * B * B * B) + (0.25f * B * B * μ * μ);
                double CTσ = (CLα * 0.5) * ((at * λr) + (bt * θom) + (ct * θtw));
                CT = CTσ * σ;
                if (double.IsNaN(CT) || double.IsInfinity(CT)) { CT = 0.0; }

                // --------------------------------- Torque
                double df = (6 * CT) / (σ * CLα);
                δc = 0.009 + (0.3 * df * df);
                double aq1 = 0.00109f - (0.0036f * λr) - (0.0027f * θ75) - (1.10f * λr * λr) - (0.545f * λr * θ75) + (0.122f * θ75 * θ75);
                double aq2 = (0.00109f - (0.0027f * θ75) - (3.13f * λr * λr) - (6.35f * λr * θ75) - (1.93f * θ75 * θ75)) * μ * μ;
                double aq3 = (0.133f * λr * θ75 * μ * μ * μ);
                double aq4 = ((-0.976f * λr * λr) - (6.38f * λr * θ75) - (5.26f * θ75 * θ75)) * (μ * μ * μ * μ);
                double CQσ = aq1 + aq2 - aq3 + aq4;
                CQ = CQσ * σ;
            }

            #region Flapping 

            // --------------------------------- Cone Angle
            ɣ = (ρ * CLα * cr * R * R * R * R) / Iβ;
            if (double.IsNaN(ɣ) || double.IsInfinity(ɣ)) { ɣ = 0.0; }
            double a01 = (0.16667f * B * B * B) + (0.04f * μ * μ * μ);
            double a02 = (0.125f * B * B * B * B) + (0.125f * B * B * μ * μ);
            double a03 = (0.1f * B * B * B * B * B) + (0.08333f * B * B * B * μ * μ);
            //β0 = ɣ * ((a01 * λ.m_real) + (a02 * θom) + (a03 * θtw));
            β0 = ((2 * CTσ * ɣ) / (3 * CLα)) - ((3 * 9.81f * bladeRadius * bladeRadius) / 2) / Math.Pow((Ω * bladeRadius), 2);
            if (double.IsNaN(β0) || double.IsInfinity(β0)) { β0 = 0.0; }

            // --------------------------------- Flapping 
            double a11 = 1 / (1 - ((μ * μ) / (2 * B * B)));
            double a12 = 1 / (1 + ((μ * μ) / (2 * B * B)));
            double a13 = ((2 * λ.m_real) + (2.6667f * θ75)) * μ;
            double a14 = (16f * qw) / (B * B * B * B * ɣ * Ωm);
            double a15 = (16f * pw) / (B * B * B * B * ɣ * Ωm);
            double a16 = (1.3333f * β0 * μ);
            a1c = a11 * (a13 + (pw / Ωm) - a14);
            b1c = a12 * (a16 - (qw / Ωm) - a15);
            if (double.IsNaN(a1c) || double.IsInfinity(a1c)) { a1c = 0.0; }
            if (double.IsNaN(b1c) || double.IsInfinity(b1c)) { b1c = 0.0; }
            if (flappingState == BladeFlappingState.Dynamic)
            {
                β1c = (a1c * Math.Cos(β)) + (b1c * Math.Sin(β)) - (Bɪc);
                β1s = (b1c * Math.Cos(β)) - (a1c * Math.Sin(β)) + (Aɪc);
            }
            else if (flappingState == BladeFlappingState.Static)
            {
                β1c = -Bɪc;
                β1s = Aɪc;
            }
            if (double.IsNaN(β1c) || double.IsInfinity(β1c)) { β1c = 0.0; }
            if (double.IsNaN(β1s) || double.IsInfinity(β1s)) { β1s = 0.0; }

            #endregion

            // ------------------------------------------- Base Forces
            double maxCQ = Math.Abs(CT);
            CQ = MathBase.Clamp(CQ, -maxCQ, maxCQ);
            double T = CT * ρ * A * Math.Pow(ωr, 2) * δT * δv;
            double Q = CQ * ρ * A * Math.Pow(ωr, 2) * R;
            if (double.IsNaN(T) || double.IsInfinity(T)) { T = 0f; }
            if (double.IsNaN(Q) || double.IsInfinity(Q)) { Q = 0f; }


            // ------------------------------------------- H Force
            double ad1 = (24f * qw) / (B * B * B * B * ɣ * Ωm);
            double ad2 = (0.29f * θ75) / CTσ;
            double ad3 = ad1 * (1 - ad2);
            aɪ = a11 * (a13 - ad3);
            if (double.IsNaN(aɪ) || double.IsInfinity(aɪ)) { aɪ = 0.0; }
            FH = T * aɪ;
            if (double.IsNaN(FH) || double.IsInfinity(FH)) { FH = 0.0; }


            if (Ωm > 10)
            {
                // ------------------------------------------- Side Force
                FComplex ac1 = (0.75f * b1c * λ) - (1.5f * β0 * λ * μ) + (0.25f * a1c * b1c * μ) - (β0 * a1c * μ * μ) + (0.1667f * β0 * a1c);
                double ac2 = ((0.75f * μ * β0) - (0.3333f * b1c) - (0.5f * μ * μ * b1c)) * θ75;
                FComplex CYσ = (0.5f * CLα) * (ac1 - ac2);
                CY = CYσ.m_real * σ;
                FY = (Nb * cr * R * ρ * ωr * ωr * CYσ.m_real);
                if (double.IsNaN(FY) || double.IsInfinity(FY)) { FY = 0.0; }
            }


            // ------------------------------------------- Force Sum
            double Xs = (-FH * Math.Cos(β)) - (FY * Math.Sin(β)) + (T * -Bɪc);
            double Ys = (-FH * Math.Sin(β)) + (FY * Math.Cos(β)) + (T * Aɪc);
            double Zs = -T;

            if (Math.Abs(Xs) < 5) { Xs = 0; }
            if (Math.Abs(Ys) < 5) { Ys = 0; }
            if (Math.Abs(Zs) < 5) { Zs = 0; }

            if (double.IsNaN(Xs) || double.IsInfinity(Xs)) { Xs = 0f; }
            if (double.IsNaN(Ys) || double.IsInfinity(Ys)) { Ys = 0f; }
            if (double.IsNaN(Zs) || double.IsInfinity(Zs)) { Zs = 0f; }
            m_shaftForce = new Vector(Xs, Ys, Zs);
            m_force = Transformation.ForceToUnity(m_shaftForce);


            // --------------------------------- Moment
            double Ms;
            double Ls = Rβ1s * β1s - Q * Math.Sin(β1c) * Math.Cos(β1s);
            if (rotorConfiguration != RotorConfiguration.Tandem) { Ms = Mβ1c * β1c + Q * Math.Cos(β1c) * Math.Sin(β1s); }
            else { Ms = 0f; }
            double Ns = Q * torqueFactor;
            if (double.IsNaN(Ls) || double.IsInfinity(Ls)) { Ls = 0.0; }
            if (double.IsNaN(Ms) || double.IsInfinity(Ms)) { Ms = 0.0; }
            if (double.IsNaN(Ns) || double.IsInfinity(Ns)) { Ns = 0.0; }
            m_moment = new Vector3((float)Ms, (float)Ns, (float)Ls);

            Thrust = T;
            coneAngle = β0 * Mathf.Rad2Deg;
            featherAngle = θom * Mathf.Rad2Deg;
            Torque = Q * Math.Cos(β1c) * Math.Cos(β1s);
            Inertia = J * Math.Pow(Math.Cos(β0), 2);

            // Force vector sanity checks
            if (double.IsNaN(m_force.x) || double.IsInfinity(m_force.x)) { m_force.x = 0f; }
            if (double.IsNaN(m_force.y) || double.IsInfinity(m_force.y)) { m_force.y = 0f; }
            if (double.IsNaN(m_force.z) || double.IsInfinity(m_force.z)) { m_force.z = 0f; }
            // Moment vector sanity checks
            if (double.IsNaN(m_moment.x) || double.IsInfinity(m_moment.x)) { m_moment.x = 0f; }
            if (double.IsNaN(m_moment.y) || double.IsInfinity(m_moment.y)) { m_moment.y = 0f; }
            if (double.IsNaN(m_moment.z) || double.IsInfinity(m_moment.z)) { m_moment.z = 0f; }


            // --------------------------------- Apply
            if (rotorType == RotorType.MainRotor)
            {
                if (rotorConfiguration == RotorConfiguration.Tandem) { m_direction = m_forcePoint.up; }
                else { m_direction = transform.up; }

                // Torque
                Vector3 mainTorque = new Vector3(0, (float)Ns, 0);
                if (m_controller.m_torqueMode == TorqueMode.Conventional && mainTorque.magnitude > 0.5) { m_controller.m_rigidbody.AddRelativeTorque(mainTorque, ForceMode.Force); }

                // Lift
                Vector3 mainForce = m_force.y * m_direction;
                // Stupid fix. This is to avoid the helicopter constantly waking up and sliding on the ground even when the force is really low
                if(zR < 1.2)
                {
                    double force_limit = m_controller.m_rigidbody.mass * 9.81f * 0.65f;
                    if(mainForce.magnitude > force_limit)
                    {
                        m_controller.m_rigidbody.AddForceAtPosition(m_force.y * m_direction, transform.position, ForceMode.Force);
                        m_controller.moment += m_moment;
                    }
                }
                else
                {
                    m_controller.m_rigidbody.AddForceAtPosition(m_force.y * m_direction, transform.position, ForceMode.Force);
                    m_controller.moment += m_moment;
                }
            }
            else
            {
                Vector3 tailForce = m_force.y * δY * transform.up;
                if ((tailForce.magnitude * Ω) > 0.5) { m_controller.m_rigidbody.AddForceAtPosition(tailForce, transform.position, ForceMode.Force); }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private void AnalyseSound()
        {
            if (soundState == SoundState.Active && m_soundPoint != null)
            {
                float m_soundFactor = (float)(Ω / Ωmax);
                m_rotorPitch = m_maximumPitch * m_soundFactor;
                if (m_controller.m_cameraState == SilantroCamera.CameraState.Exterior) { m_rotorVolume = m_soundFactor; }
                else { m_rotorVolume = m_interiorVolume * m_soundFactor; }

                if (m_soundFactor < 0.01f) { m_soundPoint.Stop(); }
                else
                {
                    if (!m_soundPoint.isPlaying) { m_soundPoint.Play(); }
                    m_soundPoint.pitch = m_rotorPitch;
                    m_soundPoint.volume = m_rotorVolume;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void AnalyseVisuals()
        {
            m_rotor_alpha = (float)(Ω / Ωmax);
            if (visualType == VisulType.Complete)
            {
                if (blurredRotor != null && normalRotor != null)
                {
                    foreach (Material brotor in blurredRotor) { if (brotor != null) { brotor.color = new Color(blurredRotorColor.r, blurredRotorColor.g, blurredRotorColor.b, m_rotor_alpha); } }
                    foreach (Material nrotor in normalRotor) { if (nrotor != null) { nrotor.color = new Color(normalRotorColor.r, normalRotorColor.g, normalRotorColor.b, (1 - m_rotor_alpha) + normalBalance); } }
                }
            }
            if (visualType == VisulType.Partial)
            {
                if (blurredRotor != null)
                {
                    foreach (Material brotor in blurredRotor) { if (brotor != null) { brotor.color = new Color(blurredRotorColor.r, blurredRotorColor.g, blurredRotorColor.b, m_rotor_alpha); } }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void OnDrawGizmos()
        {
            if (m_rotor == null) { m_rotor = GetComponent<SilantroRotor>(); }
            if (!Application.isPlaying)
            {
                DrawContainers(); RotorDesign.AnalyseRotorShape(m_rotor);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void DrawContainers()
        {
            if (υt == null || υt.Length != subdivision + 1) { υt = new FComplex[subdivision + 1]; }
            if (υi == null || υi.Length != subdivision + 1) { υi = new FComplex[subdivision + 1]; }
            if (λt == null || λt.Length != subdivision + 1) { λt = new FComplex[subdivision + 1]; }
            if (α == null || α.Length != subdivision + 1) { α = new FComplex[subdivision + 1]; }
            if (CLf == null || CLf.Length != subdivision + 1) { CLf = new FComplex[subdivision + 1]; }
            if (CDf == null || CDf.Length != subdivision + 1) { CDf = new FComplex[subdivision + 1]; }

            if (kix == null || kix.Length != subdivision + 1) { kix = new FComplex[subdivision + 1]; }
            if (kpx == null || kpx.Length != subdivision + 1) { kpx = new FComplex[subdivision + 1]; }
            if (CLx == null || CLx.Length != subdivision + 1) { CLx = new FComplex[subdivision + 1]; }
            if (xtx == null || xtx.Length != subdivision + 1) { xtx = new FComplex[subdivision + 1]; }
            if (stx == null || stx.Length != subdivision + 2) { stx = new FComplex[subdivision + 2]; }
            if (vMach == null || vMach.Length != subdivision + 1) { vMach = new double[subdivision + 1]; }
        }
    }
}