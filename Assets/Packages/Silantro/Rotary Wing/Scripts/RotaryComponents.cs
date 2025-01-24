using System;
using UnityEngine;
using Oyedoyin.Common;
using Oyedoyin.Mathematics;

namespace Oyedoyin.RotaryWing
{

    #region GearBox

    [Serializable]
    public class SilantroGearbox
    {
        public enum SystemType { SingleEngine, MultiEngine }
        public enum EngineCount { E2, E3 }


        public Controller m_controller;

        public SystemType m_engineMode = SystemType.SingleEngine;
        public EngineCount engineCount = EngineCount.E2;
        public SilantroTurboshaft m_shaftEngineA;
        public SilantroTurboshaft m_shaftEngineB;
        public SilantroTurboshaft m_shaftEngineC;
        public SilantroPiston m_pistonEngine;
        public SilantroRotor m_primary;
        public SilantroRotor m_secondary;
        public SilantroPropeller m_appendage;

        public double friction = 0.1697;
        public double brakeTorque = 75;
        public double norminalRPM = 1000f;
        public double maximumRPM = 1000;
        public double autoBrakeRPM = 1500;

        [Header("Inputs")]
        public bool BrakeEnabled;
        public double m_primaryLoad;
        public double m_secondaryLoad;
        public double m_appendageLoad;
        public double Ωeng;
        public double m_primaryInertia = 1000;
        public double m_secondaryInertia = 10;
        public double m_appendageInertia = 10;
        public double m_primaryRatio = 1;
        public double m_secondaryRatio = 1;
        public double m_appendageRatio = 1;

        [Header("Outputs")]
        public double Ω;
        public double m_primarySpeed;
        public double m_secondarySpeed;
        public double m_appendageSpeed;
        public double totalLoad, singleLoad;
        public double Inertia;
        internal bool engaged;
        public double m_powerLevel;


        /// <summary>
        /// 
        /// </summary>
        public void Initialize()
        {
            // Set RPMs
            if (m_controller.m_engineType == Controller.EngineType.Jet) { norminalRPM = m_shaftEngineA.designRPM; }
            if (m_controller.m_engineType == Controller.EngineType.Piston) { norminalRPM = m_pistonEngine.core.functionalRPM; }

            if (m_engineMode == SystemType.MultiEngine && m_controller.m_engineType == Controller.EngineType.Jet)
            {
                if (engineCount == EngineCount.E2) { if (m_shaftEngineA.designRPM != m_shaftEngineB.designRPM) { Debug.Log("Incompatible Dual Engines, Please check RPM Configuration"); return; } }
                if (engineCount == EngineCount.E2) { norminalRPM = (m_shaftEngineA.designRPM + m_shaftEngineB.designRPM) / 2f; }
                if (engineCount == EngineCount.E3) { if (m_shaftEngineA.designRPM != m_shaftEngineB.designRPM || (m_shaftEngineA.designRPM != m_shaftEngineC.designRPM) || (m_shaftEngineB.designRPM != m_shaftEngineC.designRPM)) { Debug.Log("Incompatible Engines, Please check RPM Configuration"); return; } }
                if (engineCount == EngineCount.E3) { norminalRPM = (m_shaftEngineA.designRPM + m_shaftEngineB.designRPM + m_shaftEngineC.designRPM) / 2f; }
            }

            maximumRPM = norminalRPM * 1.25;
            m_primaryRatio = norminalRPM / m_primary.funcionalRPM;
            m_secondaryRatio = norminalRPM / m_secondary.funcionalRPM;
            if (m_appendage) { m_appendageRatio = norminalRPM / m_appendage.m_ratedRPM; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        protected void AnalyseCore(double dt)
        {
            // enable auto brake when drive RPM is within the set brake RPM range
            // and engine RPM is less than drive RPM
            if (autoBrakeRPM > 1e-5 && Ω < autoBrakeRPM / 9.5492966 && Ωeng < Ω)
            {
                BrakeEnabled = true;
            }

            // Calculate rotor torque load, inertias and the core power loss
            double Qload =
                m_primaryLoad / m_primaryRatio +
                m_secondaryLoad / m_secondaryRatio +
                m_appendageLoad / m_appendageRatio +
                friction * Ω + (BrakeEnabled && Ω > 0 ? brakeTorque : 0);
            double J =
                m_primaryInertia / (m_primaryRatio * m_primaryRatio) +
                m_appendageInertia / (m_appendageRatio * m_appendageRatio) +
                m_secondaryInertia / (m_secondaryRatio * m_secondaryRatio);


            // Clutch condition:: if engine RPM >= rotor RPM, clutch engages load
            if (Ωeng > Ω)
            {
                // Engine driving
                engaged = true;
                Ω = Ωeng;
            }
            else
            {
                Ω -= Qload / J * dt;
                // Reduce the load when there is a bit of margin between rotation speeds
                // (otherwise the load just oscillates between updates)
                if (Ωeng / Ω < 0.99)
                {
                    // Clutch disengaged - rotors spin freely
                    engaged = false;
                    Qload = 0;
                    J = 0;
                }
                if (Ω < 0) { Ω = 0; }
            }
            if (double.IsNaN(Ω) || double.IsInfinity(Ω)) { Ω = 0; }

            // Limit drive RPM to maximum allowed transmission RPM of the gearbox
            if (maximumRPM > 1e-5 && Ω > maximumRPM / 9.5492966) { Ω = maximumRPM / 9.5492966; }
            double OmegaMR = Ω / m_primaryRatio;
            double OmegaTR = Ω / m_secondaryRatio;
            double OmegaAP = Ω / m_appendageRatio;


            // Validate output
            if (double.IsNaN(Qload) || double.IsInfinity(Qload)) { Qload = 0; }
            if (double.IsNaN(J) || double.IsInfinity(J)) { J = 0; }
            if (double.IsNaN(OmegaMR) || double.IsInfinity(OmegaMR)) { OmegaMR = 0; }
            if (double.IsNaN(OmegaTR) || double.IsInfinity(OmegaTR)) { OmegaTR = 0; }
            if (double.IsNaN(OmegaAP) || double.IsInfinity(OmegaAP)) { OmegaAP = 0; }

            // Set output ports
            totalLoad = Qload;
            Inertia = J;
            m_primarySpeed = OmegaMR;
            m_secondarySpeed = OmegaTR;
            m_appendageSpeed = OmegaAP;
            m_powerLevel = Ω / (norminalRPM / 9.5492966);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Compute(double timestep)
        {
            AnalyseCore(timestep);

            m_primaryLoad = Math.Abs(m_primary.Torque);
            m_secondaryLoad = Math.Abs(m_secondary.Torque);
            m_primaryInertia = Math.Abs(m_primary.Inertia);
            m_secondaryInertia = Math.Abs(m_secondary.Inertia);
            if (double.IsNaN(m_primaryLoad) || double.IsInfinity(m_primaryLoad)) { m_primaryLoad = 0; }
            if (double.IsNaN(m_secondaryLoad) || double.IsInfinity(m_secondaryLoad)) { m_secondaryLoad = 0; }
            if (double.IsNaN(m_primaryInertia) || double.IsInfinity(m_primaryInertia)) { m_primaryInertia = 0; }
            if (double.IsNaN(m_secondaryInertia) || double.IsInfinity(m_secondaryInertia)) { m_secondaryInertia = 0; }
            m_primaryLoad = MathBase.Clamp(m_primaryLoad, 1, 150000);
            m_secondaryLoad = MathBase.Clamp(m_secondaryLoad, 1, 150000);
            m_primaryInertia = MathBase.Clamp(m_primaryInertia, 1, 100000);
            m_secondaryInertia = MathBase.Clamp(m_secondaryInertia, 1, 100000);

            if (m_appendage)
            {
                m_appendageLoad = Math.Abs(m_appendage.m_Torque);
                if (double.IsNaN(m_appendageLoad) || double.IsInfinity(m_appendageLoad)) { m_appendageLoad = 0; }
                m_appendageLoad = MathBase.Clamp(m_appendageLoad, 1, 150000);
                m_appendageInertia = Math.Abs(m_appendage.m_inertia);
                m_appendageInertia = MathBase.Clamp(m_appendageInertia, 1, 100000);
            }

            // Single Engine Logic
            if (m_engineMode == SystemType.SingleEngine)
            {
                if (m_controller.m_engineType == Controller.EngineType.Jet) { Ωeng = m_shaftEngineA.Ω; }
                if (m_controller.m_engineType == Controller.EngineType.Piston) { Ωeng = m_shaftEngineA.Ω; }

                if (totalLoad > 1)
                {
                    singleLoad = totalLoad;
                    if (m_controller.m_engineType == Controller.EngineType.Jet && m_shaftEngineA.state != SilantroTurboshaft.State.START) { m_shaftEngineA.load = totalLoad; }
                    if (m_controller.m_engineType == Controller.EngineType.Piston && m_pistonEngine.core.CurrentEngineState != Oyedoyin.Common.Components.EngineCore.EngineState.Starting)
                    { m_pistonEngine.core.inputLoad = (float)totalLoad; }
                }
            }

            // Double Engine Logic
            if (m_engineMode == SystemType.MultiEngine && m_controller.m_engineType == Controller.EngineType.Jet)
            {
                if (engineCount == EngineCount.E2)
                {
                    if (m_shaftEngineA.state == SilantroTurboshaft.State.RUN && m_shaftEngineB.state == SilantroTurboshaft.State.RUN)
                    { Ωeng = ((m_shaftEngineA.Ω * 2f) + (m_shaftEngineB.Ω * 2f)) / 4f; }
                    else { Ωeng = ((m_shaftEngineA.Ω * 2f) + (m_shaftEngineB.Ω * 2f)) / 2f; }

                    if (totalLoad > 1)
                    {
                        if (m_shaftEngineA.state == SilantroTurboshaft.State.RUN && m_shaftEngineB.state == SilantroTurboshaft.State.RUN) { singleLoad = totalLoad / 2f; } else { singleLoad = totalLoad; }
                        if (m_shaftEngineA.state == SilantroTurboshaft.State.RUN) { m_shaftEngineA.load = singleLoad; }
                        if (m_shaftEngineB.state == SilantroTurboshaft.State.RUN) { m_shaftEngineB.load = singleLoad; }
                    }
                }
            }

            m_primary.Ω = m_primarySpeed;
            m_secondary.Ω = m_secondarySpeed;
        }
    }

    #endregion

    #region Fuselage

    [Serializable]
    public class Fuselage
    {
        public SilantroCore m_core;
        public SilantroRotor m_mainRotor;
        public Analysis.Fuselage.AH1 _fuselageAH1;
        public Analysis.Fuselage.CH47 _fuselageCH47;
        public Analysis.Fuselage.CH54 _fuselageCH54;
        public Analysis.Fuselage.CustomHeliFuse _fuseCustomCFD;


        public enum Model
        {
            /// Fuselage / blunt body aerodynamic model.
            /// Based on standard drag model with equivalent flat-plate surface areas and curve-fit coefficients.
            /// A center of pressure shift factor scaled with airspeed is also available (based on Heffley model).
            BluntBodyCurveFit,
            SimplifiedFlatPlate,
            /// Based on NASA models with tables
            CustomTableLookup
        }
        public enum FuselageModel
        {
            AH1,
            //CH47,
            CH54,
            CustomCFD
        }

        public Model m_model = Model.BluntBodyCurveFit;
        public FuselageModel m_fuseModel = FuselageModel.AH1;


        // Curve-fit coefficients
        public double CXuu = -1;
        public double CXvu;
        public double CXwu;
        public double CYuv;
        public double CYvv = -15.5;
        public double CYwv;
        public double CZuw;
        public double CZvw;
        public double CZww = -7.9;
        public double CLuu = 0.5;
        public double CLww = -10;
        public double CLup;
        public double CMuu;
        public double CMuw;
        public double CMuq;
        public double CNuv;
        public double CNur;
        // center of pressure shift scaling factor (Backlund model)
        public double cpshift_xu = 2.5;
        public double cpshift_yv;
        public double cpshift_yu = -1.0;

        public double ekf = 0.5f;
        public double ekt = 1.8f;
        public double kfe = 0.0243f;

        // fuselage flat plate areas
        public double m_fe1 = 1.5f; // frontal area
        public double m_fe2 = 5.0f; // side area
        public double m_fe3 = 20.0f; // top area
        public double m_Sref = 4.0f;
        public double m_lref = 2.5f;
        public double m_CLαf = 1.12f;
        public double m_CMαf = 0.42975;

        [Header("Data")]
        public double ub;
        public double vb, wb;
        public double pb, qb, rb;
        public double ρ = 1.225;
        public double λ, μ, CT, νi, Tmr;
        public double αf;
        public double αfdeg;
        public double αfl;
        public double αfldeg;
        public double emr;
        public double it0, it, itdeg;
        public double βf;
        public double ψwt, ψwtdeg;

        [Header("Output")]
        public Vector3 force;
        public Vector3 moment;
        private Vector m_force;
        private Vector m_moment;

        /// <summary>
        /// 
        /// </summary>
        public void Initialize()
        {
            if (m_model == Model.CustomTableLookup)
            {
                if (m_fuseModel == FuselageModel.AH1) { _fuselageAH1.Initialize(); }
                if (m_fuseModel == FuselageModel.CH54) { _fuselageCH54.Initialize(); }
                if (m_fuseModel == FuselageModel.CustomCFD) { _fuseCustomCFD.Initialize(); }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Compute()
        {
            // Collect Data
            ub = m_core.u;
            vb = m_core.v;
            wb = m_core.w;
            pb = m_core.p;
            qb = m_core.q;
            rb = m_core.r;

            // Calculate main rotor inflow effect on the fuselage 
            double V = Math.Sqrt((ub * ub) + (vb * vb) + (wb * wb));
            if (m_mainRotor != null)
            {
                λ = m_mainRotor.λ.m_real;
                μ = m_mainRotor.μ;
                CT = m_mainRotor.CT;
                νi = m_mainRotor.νi.m_real;
                Tmr = m_mainRotor.Thrust;
            }
            else { λ = 0; μ = 0; CT = 0; νi = 0; Tmr = 0; }
            emr = CT / (2 * ((λ * λ) + (μ * μ)));
            if (double.IsNaN(emr) || double.IsInfinity(emr)) { emr = 0.0; }
            αf = Math.Atan(wb / ub);
            αfdeg = αf * Mathf.Rad2Deg;
            αfl = αf - (emr * ekf);
            αfldeg = αfl * Mathf.Rad2Deg;
            it = it0 - (emr * (ekt - ekf));
            itdeg = it * Mathf.Rad2Deg;
            βf = Math.Asin(vb / V);
            ψwt = -βf;
            ψwtdeg = ψwt * Mathf.Rad2Deg;
            if (double.IsNaN(ψwtdeg) || double.IsInfinity(ψwtdeg)) { ψwtdeg = 0.0; }
            if (double.IsNaN(αf) || double.IsInfinity(αf)) { αf = 0.0; }
            if (double.IsNaN(αfdeg) || double.IsInfinity(αfdeg)) { αfdeg = 0.0; }
            if (double.IsNaN(αfl) || double.IsInfinity(αfl)) { αfl = 0.0; }
            if (double.IsNaN(αfldeg) || double.IsInfinity(αfldeg)) { αfldeg = 0.0; }
            if (double.IsNaN(it) || double.IsInfinity(it)) { it = 0.0; }
            if (double.IsNaN(itdeg) || double.IsInfinity(itdeg)) { itdeg = 0.0; }
            if (double.IsNaN(βf) || double.IsInfinity(βf)) { βf = 0.0; }
            if (double.IsNaN(ψwt) || double.IsInfinity(ψwt)) { ψwt = 0.0; }


            // Sum up forces using curve-fit coefficients, Dreier eq (8.11)
            if (m_model == Model.BluntBodyCurveFit)
            {
                double CX = CXuu * ub * Math.Abs(ub) + CXvu * vb * ub + CXwu * wb * ub;
                double CY = CYuv * ub * vb + CYvv * vb * Math.Abs(vb) + CYwv * wb * vb;
                double CZ = CZuw * ub * wb + CZvw * vb * wb + CZww * wb * Math.Abs(wb);
                m_force = new Vector(CX, CY, CZ) * 0.5 * ρ;

                double Cl = CLuu * ub * Math.Abs(ub) + CLww * wb * Math.Abs(wb) + CLup * ub * pb;
                double Cm = CMuu * ub * Math.Abs(ub) + CMuw * ub * wb + CMuq * ub * qb;
                double Cn = CNuv * ub * vb + CNur * ub * rb;
                m_moment = new Vector(Cl, Cm, Cn) * 0.5 * ρ;
            }
            if (m_model == Model.SimplifiedFlatPlate)
            {
                double phx = 0.5 * ρ;
                double D1 = phx * m_fe1;
                double D2 = phx * m_fe2;
                double D3 = phx * m_fe3;
                double L1 = phx * m_Sref * m_CLαf;
                double M1 = phx * m_Sref * m_lref * m_CMαf;
                double N1 = M1;

                double FX = ub * ((-D1 * Math.Abs(ub)) + (L1 * (Math.Pow(wb, 2) / Math.Abs(ub))));
                double FY = vb * ((-D2 * Math.Abs(vb)) - (L1 * Math.Abs(ub)));
                double FZ = wb * ((-D3 * Math.Abs(wb)) - (L1 * Math.Abs(ub)));
                if (double.IsNaN(FX) || double.IsInfinity(FX)) { FX = 0.0; }
                if (double.IsNaN(FY) || double.IsInfinity(FY)) { FY = 0.0; }
                if (double.IsNaN(FZ) || double.IsInfinity(FZ)) { FZ = 0.0; }
                m_force = new Vector(FX, FY, FZ);

                double MX = 0f;
                double MY = M1 * wb * Math.Abs(ub);
                double MZ = -N1 * vb * ub;
                if (double.IsNaN(MX) || double.IsInfinity(MX)) { MX = 0.0; }
                if (double.IsNaN(MY) || double.IsInfinity(MY)) { MY = 0.0; }
                if (double.IsNaN(MZ) || double.IsInfinity(MZ)) { MZ = 0.0; }
                m_moment = new Vector(MX, MY, MZ);
            }
            if (m_model == Model.CustomTableLookup)
            {
                if (m_fuseModel == FuselageModel.AH1)
                {
                    _fuselageAH1.CT = CT;
                    _fuselageAH1.λ = λ;
                    _fuselageAH1.μ = μ;
                    _fuselageAH1.vi = νi;
                    _fuselageAH1.ub = ub;
                    _fuselageAH1.vb = vb;
                    _fuselageAH1.wb = wb;
                    _fuselageAH1.pb = pb;
                    _fuselageAH1.qb = qb;
                    _fuselageAH1.rb = rb;
                    _fuselageAH1.Thrust = Tmr;
                    _fuselageAH1.Compute();
                    m_force = _fuselageAH1.force;
                    m_moment = _fuselageAH1.moment;
                }

                if (m_fuseModel == FuselageModel.CH54)
                {
                    _fuselageCH54.ub = ub;
                    _fuselageCH54.vb = vb;
                    _fuselageCH54.wb = wb;
                    _fuselageCH54.pb = pb;
                    _fuselageCH54.qb = qb;
                    _fuselageCH54.rb = rb;
                    _fuselageCH54.CT = CT;
                    _fuselageCH54.λ = λ;
                    _fuselageCH54.μ = μ;
                    _fuselageCH54.Thrust = Tmr;
                    _fuselageCH54.Compute();
                    m_force = _fuselageCH54.force;
                    m_moment = _fuselageCH54.moment;
                }

                if (m_fuseModel == FuselageModel.CustomCFD)
                {
                    _fuseCustomCFD.ub = ub;
                    _fuseCustomCFD.vb = vb;
                    _fuseCustomCFD.wb = wb;
                    _fuseCustomCFD.pb = pb;
                    _fuseCustomCFD.qb = qb;
                    _fuseCustomCFD.rb = rb;

                    _fuseCustomCFD.Compute();
                    m_force = _fuseCustomCFD.force;
                    m_moment = _fuseCustomCFD.moment;
                }
            }

            if (Math.Abs(m_force.x) < 0.5) { m_force.x = 0; }
            if (Math.Abs(m_force.y) < 0.5) { m_force.y = 0; }
            if (Math.Abs(m_force.z) < 0.5) { m_force.z = 0; }

            // Limit Moment
            if (Math.Abs(m_moment.x) < 0.5) { m_moment.x = 0; }
            if (Math.Abs(m_moment.y) < 0.5) { m_moment.y = 0; }
            if (Math.Abs(m_moment.z) < 0.5) { m_moment.z = 0; }
            force = Transformation.ForceToUnity(m_force);
            moment = Transformation.ForceToUnity(m_moment);
        }

    }

    #endregion
}