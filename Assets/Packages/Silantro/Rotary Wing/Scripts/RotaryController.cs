using UnityEngine;
using Oyedoyin.Common;
using Oyedoyin.Mathematics;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Oyedoyin.RotaryWing
{
    #region Component
    /// <summary>
    /// 
    /// </summary>
    public class RotaryController : Controller
    {
        public enum RotorConfiguration { Tandem, Coaxial, Conventional, Syncrocopter }
        public enum TorqueMode { Conventional, Corrected }

        public RotorConfiguration m_configuration = RotorConfiguration.Conventional;
        public TorqueMode m_torqueMode = TorqueMode.Corrected;

        public RotaryController m_helicopter;
        public RotaryComputer m_computer;
        public Fuselage m_fuselage;
        public SilantroGearbox m_gearbox;

        public SilantroRotor[] m_rotors;
        public SilantroTurboshaft[] m_shafts;
        public SilantroStabilizer[] m_stabilizers;
        public float _torqueSum;
        public Vector3 centralForce;
        public Vector3 centralMoment;


        private void Start() { Initialize(); }
        private void FixedUpdate() { ComputeFixedUpdate(); }
        private void Update() { ComputeUpdate(); }
        private void LateUpdate() { ComputeLateUpdate(); }


        #region Internal Functions

        /// <summary>
        /// Check that all the required components of the aircraft are present
        /// </summary>
        protected override void CheckPrerequisites()
        {
            if (m_rotors.Length <= 1) { Debug.LogError("Prerequisites not met on Helicopter " + transform.name + ".... Rotor System not properly configured"); allOk = false; return; }
            else if (m_flcs == null || m_computer == null) { Debug.LogError("Prerequisites not met on Helicopter " + transform.name + ".... flight computer not connected"); allOk = false; return; }
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Collect Components
            m_helicopter = GetComponent<RotaryController>();
            m_computer = GetComponentInChildren<RotaryComputer>();
            m_rotors = GetComponentsInChildren<SilantroRotor>();
            m_stabilizers = GetComponentsInChildren<SilantroStabilizer>();
            m_shafts = GetComponentsInChildren<SilantroTurboshaft>();

            // Confirm needed components
            CheckPrerequisites();

            // Initialize fixed components
            if (allOk)
            {
                m_fuselage.m_core = m_core;
                m_fuselage.Initialize();
                if (m_computer)
                {
                    m_computer.m_helicopter = m_helicopter;
                    m_computer.m_autopilot._flcs = m_computer;
                }

                // --------------------- Setup Rotor System
                foreach (SilantroRotor rotor in m_rotors)
                {
                    rotor.m_controller = m_helicopter;
                    rotor.Initialize();
                }


                // --------------------- Rotation
                if (m_core.rotationDrag < 1) { m_core.rotationDrag = 1; }
                if (m_torqueMode == TorqueMode.Corrected) { m_rigidbody.angularDamping = m_core.rotationDrag; } else { m_rigidbody.angularDamping = 0.02f; }

                // --------------------- Aerofoils
                foreach (SilantroStabilizer stab in m_stabilizers)
                {
                    stab.m_controller = m_helicopter;
                    stab.Initialize();
                }

                // --------------------- Setup Engines
                if (m_shafts != null && m_shafts.Length > 0 && m_engineType == EngineType.Jet)
                {
                    foreach (SilantroTurboshaft shaft in m_shafts)
                    {
                        shaft.m_controller = m_helicopter;
                        shaft.Initialize();
                    }
                }

                // --------------------- Setup Transmission
                if (m_gearbox != null)
                {
                    // Rotors
                    if (m_rotors.Length >= 2)
                    {
                        m_gearbox.m_primary = m_rotors[0];
                        m_gearbox.m_secondary = m_rotors[1];
                    }
                    else { Debug.LogError("Prerequisites not met on Helicopter " + transform.name + ".... Rotor System not properly configured"); allOk = false; return; }

                    // Engines
                    if (m_engineType == EngineType.Piston)
                    {
                        if (m_pistons.Length == 1) { m_gearbox.m_pistonEngine = m_pistons[0]; }
                        if (m_pistons.Length > 1) { Debug.LogWarning("Excess piston engine connected to Helicopter " + transform.name); }
                        if (m_pistons.Length < 1)
                        {
                            Debug.LogError("Prerequisites not met on Helicopter " + transform.name + ".... No piston engine connected");
                            allOk = false;
                            return;
                        }
                    }
                    else if (m_engineType == EngineType.Jet)
                    {
                        if (m_shafts.Length < 1)
                        {
                            Debug.LogError("Prerequisites not met on Helicopter " + transform.name + ".... No Turboshaft engine connected");
                            allOk = false;
                            return;
                        }
                        else
                        {
                            if (m_gearbox.m_engineMode == SilantroGearbox.SystemType.SingleEngine) { m_gearbox.m_shaftEngineA = m_shafts[0]; }
                            else if (m_gearbox.m_engineMode == SilantroGearbox.SystemType.MultiEngine)
                            {
                                if (m_gearbox.engineCount == SilantroGearbox.EngineCount.E2)
                                {
                                    m_gearbox.m_shaftEngineA = m_shafts[0];
                                    m_gearbox.m_shaftEngineB = m_shafts[1];
                                }
                                else
                                {
                                    m_gearbox.m_shaftEngineA = m_shafts[0];
                                    m_gearbox.m_shaftEngineB = m_shafts[1];
                                    m_gearbox.m_shaftEngineC = m_shafts[2];
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Prerequisites not met on Helicopter " + transform.name + ".... selected engine type not compatible"); allOk = false;
                        return;
                    }

                    m_gearbox.m_controller = m_helicopter;
                    m_gearbox.Initialize();
                }

                // ------------------------- Hot Start
                if (m_startMode == StartMode.Hot && m_hotMode == HotStartMode.AfterInitialization)
                {
                    #region Activate Autopilot

                    if (m_computer.m_mode != Computer.Mode.Autonomous && m_computer != null)
                    {
                        m_computer.m_mode = Computer.Mode.Autonomous;
                    }

                    m_computer.m_autopilot.m_lateralState = Autopilot.LateralState.DriftControl;
                    m_computer.m_autopilot.m_longitudinalMode = Autopilot.LongitudinalState.AttitudeHold;
                    m_computer.m_autopilot.m_powerState = Autopilot.PowerState.AltitudeHold;

                    // Set Takeoff Commands
                    RotaryComputer r_computer = m_computer.gameObject.GetComponent<RotaryComputer>();
                    r_computer.m_autopilot.SetCommandPreset(m_startSpeed - 5, m_startAltitude - 10);
                    m_computer.m_commands.m_commandAltitude = m_startAltitude;
                    m_computer.m_commands.m_commandPitchAngle = 0; // Hold level nose
                    m_computer.m_commands.m_commandClimbRate = 500;

                    #endregion

                    StartCoroutine(m_helper.StartUpAircraft(m_controller));
                }
            }

            // Tell base Initialization process is done
            FinishInitialization();
            isInitialized = true;
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void ComputeFixedUpdate()
        {
            base.ComputeFixedUpdate();
            _torqueSum = 0;
            centralForce = Vector3.zero;
            centralMoment = Vector3.zero;


            if (allOk && isInitialized)
            {
                // Gearbox
                if (m_gearbox != null) { m_gearbox.Compute(_fixedTimestep); }
                // Rotors
                if (m_rotors != null)
                {
                    foreach (SilantroRotor rotor in m_rotors)
                    {
                        rotor.Compute(_fixedTimestep);
                        _torqueSum += (float)rotor.Torque;
                        centralForce += rotor.m_force;
                        centralMoment += rotor.m_moment;
                    }
                }
                // Turboshaft
                if (m_shafts != null) { foreach (SilantroTurboshaft shaft in m_shafts) { shaft.Compute(_fixedTimestep); } }

                if (isControllable)
                {
                    // Fuselage
                    m_fuselage.Compute();
                    centralForce += m_fuselage.force;
                    centralForce.y = 0;
                    centralMoment += m_fuselage.moment;
                    centralMoment.y = 0;
                    // Aerofoils
                    if (m_stabilizers != null) { foreach (SilantroStabilizer stab in m_stabilizers) { stab.Compute(); } }
                    // Data
                    ComputeData();
                    // Yaw Control for Coaxial, Tandem and Syncrocopter config (Slight cheating :( )
                    _torqueSum *= _yawInput;
                    Vector3 m_moment = new Vector3(0, _torqueSum, 0);
                    if (m_configuration != RotorConfiguration.Conventional && m_moment.magnitude > 1) { m_rigidbody.AddRelativeTorque(m_moment, ForceMode.Force); }
                }

                // Limit Force
                if (Math.Abs(centralForce.x) < 5) { centralForce.x = 0; }
                if (Math.Abs(centralForce.y) < 5) { centralForce.y = 0; }
                if (Math.Abs(centralForce.z) < 5) { centralForce.z = 0; }

                // Limit Moment
                if (Math.Abs(centralMoment.x) < 5) { centralMoment.x = 0; }
                if (Math.Abs(centralMoment.y) < 5) { centralMoment.y = 0; }
                if (Math.Abs(centralMoment.z) < 5) { centralMoment.z = 0; }

                // Apply Central Forces
                if (!float.IsNaN(centralForce.magnitude) && !float.IsInfinity(centralForce.magnitude) && centralForce.magnitude > 1) { m_rigidbody.AddRelativeForce(centralForce, ForceMode.Force); }
                if (!float.IsNaN(centralMoment.magnitude) && !float.IsInfinity(centralMoment.magnitude) && centralMoment.magnitude > 1) { m_rigidbody.AddRelativeTorque(centralMoment, ForceMode.Force); }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void ComputeUpdate()
        {
            base.ComputeUpdate();

            if (allOk && isInitialized && isControllable)
            {
                UpdateComponentInputs();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void ComputeData()
        {
            base.ComputeData();

            //Pistons
            if (m_engineType == EngineType.Piston && m_pistons.Length > 0)
            {
                m_engineCount = m_pistons.Length;
                if (m_pistons != null && m_pistons[0] != null)
                {
                    m_powerState = m_pistons[0].core.active;
                    m_powerLevel = m_pistons[0].core.coreFactor;
                }
                foreach (SilantroPiston piston in m_pistons)
                { if (piston.core.active) { fuelFlow += piston.Mf; } }
            }
            // Turboshaft
            if (m_engineType == EngineType.Jet && m_shafts.Length > 0)
            {
                m_powerState = m_shafts[0].state == SilantroTurboshaft.State.RUN;
                m_engineCount = m_shafts.Length;
                if (m_shafts != null && m_shafts[0] != null) { m_powerLevel = (float)m_shafts[0].m_coreFactor; }
                foreach (SilantroTurboshaft shaft in m_shafts) { fuelFlow += (float)shaft.Mf; }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void UpdateComponentInputs()
        {
            //if (allOk && Application.isFocused)

            if(allOk)
            {
                base.UpdateComponentInputs();

                // Turboshaft
                if (m_shafts != null) { foreach (SilantroTurboshaft shaft in m_shafts) { shaft.throttle = _throttleInput; } }
            }
        }

        #endregion

        #region Call Functions

        /// <summary>
        /// 
        /// </summary>
        public override void TurnOnEngines()
        {
            if (isControllable)
            {
                if (m_engineType == EngineType.Piston) { foreach (SilantroPiston engine in m_pistons) { if (!engine.core.active) { engine.core.StartEngine(); } } }
                if (m_engineType == EngineType.Jet) { foreach (SilantroTurboshaft engine in m_shafts) { if (engine.state != SilantroTurboshaft.State.RUN) { engine.StartEngine(); } } }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public override void TurnOffEngines()
        {
            if (isControllable)
            {
                if (m_engineType == EngineType.Piston) { foreach (SilantroPiston engine in m_pistons) { if (engine.core.active) { engine.core.ShutDownEngine(); } } }
                if (m_engineType == EngineType.Jet) { foreach (SilantroTurboshaft engine in m_shafts) { if (engine.state == SilantroTurboshaft.State.RUN) { engine.ShutDownEngine(); } } }
            }
        }

        #endregion
    }
    #endregion

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(RotaryController))]
    public class RotaryControllerEditor : Editor
    {
        Color backgroundColor;
        Color silantroColor = new Color(1.0f, 0.40f, 0f);
        RotaryController controller;
        SerializedProperty input;
        SerializedProperty fuselage;
        SerializedProperty gearbox;
        SerializedProperty origin;
        SerializedProperty launcher;

        /// <summary>
        /// 
        /// </summary>
        private void OnEnable()
        {
            controller = (RotaryController)target;
            controller.m_type = Controller.VehicleType.Helicopter;
            input = serializedObject.FindProperty("m_input");
            fuselage = serializedObject.FindProperty("m_fuselage");
            gearbox = serializedObject.FindProperty("m_gearbox");
            origin = serializedObject.FindProperty("m_originShift");
            launcher = serializedObject.FindProperty("m_launcher");
        }
        /// <summary>
        /// 
        /// </summary>
        public override void OnInspectorGUI()
        {
            backgroundColor = GUI.backgroundColor;
            //DrawDefaultInspector();
            serializedObject.Update();

            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Aircraft Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            controller.m_name = controller.transform.gameObject.name;
            EditorGUILayout.LabelField("Label", controller.m_name);
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_engineType"), new GUIContent("Engine Type"));


            // --------------------------------------------------------------------------------------- Input Config
            GUILayout.Space(10f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Control Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_controlType"), new GUIContent("Control Mode"));
            if (controller.m_controlType == Controller.ControlType.Internal)
            {
                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Cockpit Configuration", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(2f);
                EditorGUILayout.LabelField("Pilot on board", controller.pilotOnBoard.ToString());
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_canvas"), new GUIContent("Display Canvas"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_interiorPilot"), new GUIContent("Interior Pilot"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("getOutPosition"), new GUIContent("Exit Point"));
                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox(" ", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
            }
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_inputType"), new GUIContent("Input Mode"));
            if (controller.m_inputType == Controller.InputType.Default)
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_inputLogic"), new GUIContent(" "));
            }

            GUILayout.Space(8f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Vehicle Start Mode", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_startMode"), new GUIContent(" "));
            if (controller.m_startMode == Controller.StartMode.Hot)
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_hotMode"), new GUIContent("Activation"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_startSpeed"), new GUIContent("Start Speed (m/s)"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_startAltitude"), new GUIContent("Start Altitude (m)"));
            }

            if (controller.m_inputType == Controller.InputType.VR)
            {
                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("VR Levers", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(2f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("m_joystickLever"), new GUIContent("Control Stick"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("m_throttleLever"), new GUIContent("Throttle Lever"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("m_collectiveLever"), new GUIContent("Collective Lever"));
                GUILayout.Space(8f);
            }

            if (controller.m_inputType == Controller.InputType.Mobile)
            {
                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Touch Controls", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("m_joystickTouch"), new GUIContent("Cyclic"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("m_yawTouch"), new GUIContent("Rudder Pedals"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("m_throttleTouch"), new GUIContent("Throttle"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("m_collectiveTouch"), new GUIContent("Collective"));
                GUILayout.Space(8f);
            }

            // --------------------------------------------------------------------------------------- Fuselage Config
            GUILayout.Space(10f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Fuselage Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("m_model"), new GUIContent("Model"));
            GUILayout.Space(5f);

            if (controller.m_fuselage.m_model == Fuselage.Model.BluntBodyCurveFit)
            {
                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("X-Axis Coefficients (CXuu, CXvu, CXwu)", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CXuu"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CXvu"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CXwu"), new GUIContent(""));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Y-Axis Coefficients (CYuv, CYvv, CYwv)", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CYuv"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CYvv"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CYwv"), new GUIContent(""));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Z-Axis Coefficients (CZuw, CZvw, CZww)", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CZuw"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CZvw"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CZww"), new GUIContent(""));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Roll-Moment Coefficients (CLuu, CLww, CLup)", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CLuu"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CLww"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CLup"), new GUIContent(""));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Pitch-Moment Coefficients (CMuu, CMuw, CMuq)", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CMuu"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CMuw"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CMuq"), new GUIContent(""));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Yaw-Moment Coefficients (CNuv, CNur)", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CNuv"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("CNur"), new GUIContent(""));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Backlund model Coefficients (Cpshift_xu, Cpshift_yv, Cpshift_yu)", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("cpshift_xu"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("cpshift_yv"), new GUIContent(""));
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("cpshift_yu"), new GUIContent(""));
                EditorGUILayout.EndHorizontal();
            }
            if (controller.m_fuselage.m_model == Fuselage.Model.SimplifiedFlatPlate)
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("m_fe1"), new GUIContent("Frontal Area"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("m_fe2"), new GUIContent("Side Area"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("m_fe3"), new GUIContent("Top Area"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("m_Sref"), new GUIContent("Reference Area"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("m_lref"), new GUIContent("Reference Length"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("m_CLαf"), new GUIContent("Lift-Curve Slope"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("m_CMαf"), new GUIContent("Pitch-Moment Slope"));
            }
            if (controller.m_fuselage.m_model == Fuselage.Model.CustomTableLookup)
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("m_fuseModel"), new GUIContent("Model"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("ekf"), new GUIContent("Fuselage Interference Factor"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("ekt"), new GUIContent("Tail-Rotor Interference Factor"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(fuselage.FindPropertyRelative("kfe"), new GUIContent(" "));
            }

            // --------------------------------------------------------------------------------------- Gearbox Config
            GUILayout.Space(10f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Gearbox Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_configuration"), new GUIContent("Functionality"));
            if (controller.m_configuration == RotaryController.RotorConfiguration.Conventional)
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_torqueMode"), new GUIContent("Torque Mode"));
            }
            GUILayout.Space(5f);
            EditorGUILayout.PropertyField(gearbox.FindPropertyRelative("m_engineMode"), new GUIContent("Engine Mode"));


            if (controller.m_gearbox.m_engineMode == SilantroGearbox.SystemType.MultiEngine)
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(gearbox.FindPropertyRelative("engineCount"), new GUIContent("Engine Count"));
            }

            GUILayout.Space(4f);
            EditorGUILayout.PropertyField(gearbox.FindPropertyRelative("friction"), new GUIContent("Friction"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(gearbox.FindPropertyRelative("brakeTorque"), new GUIContent("Brake Torque"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(gearbox.FindPropertyRelative("autoBrakeRPM"), new GUIContent("Auto-Brake RPM"));
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Omega", controller.m_gearbox.Ω.ToString("0.00") + " Rad/s");


            // --------------------------------------------------------------------------------------- Fuel Config
            if (controller.m_engineType != Controller.EngineType.Electric && controller.m_engineType != Controller.EngineType.Unpowered)
            {

                if (controller.m_engineType == Controller.EngineType.Piston)
                {
                    if (controller.gasFuelType == Controller.GasFuelType.AVGas100) { controller.combustionEnergy = 42.8f; }
                    if (controller.gasFuelType == Controller.GasFuelType.AVGas100LL) { controller.combustionEnergy = 43.5f; }
                    if (controller.gasFuelType == Controller.GasFuelType.AVGas82UL) { controller.combustionEnergy = 43.6f; }
                }
                else
                {
                    if (controller.jetFuelType == Controller.JetFuelType.JetB) { controller.combustionEnergy = 42.8f; }
                    if (controller.jetFuelType == Controller.JetFuelType.JetA1) { controller.combustionEnergy = 43.5f; }
                    if (controller.jetFuelType == Controller.JetFuelType.JP6) { controller.combustionEnergy = 43.02f; }
                    if (controller.jetFuelType == Controller.JetFuelType.JP8) { controller.combustionEnergy = 43.28f; }
                }
                controller.combustionEnergy *= 1000f;


                GUILayout.Space(10f);
                GUI.color = silantroColor;
                EditorGUILayout.HelpBox("Fuel Configuration", MessageType.None);
                GUI.color = backgroundColor;
                if (controller.m_engineType == Controller.EngineType.Piston)
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("gasFuelType"), new GUIContent("Fuel Type"));
                }
                else
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("jetFuelType"), new GUIContent("Fuel Type"));

                }
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Q ", controller.combustionEnergy.ToString("0.00") + " KJ");
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Capacity", controller.fuelCapacity.ToString("0.00") + " kg");
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Fuel Level", controller.fuelLevel.ToString("0.00") + " kg");
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Flow Rate", controller.fuelFlow.ToString("0.00000") + " kg/s");

                float secs = controller.fuelLevel / controller.fuelFlow;
                if (double.IsNaN(secs) || double.IsInfinity(secs)) { secs = 0.0f; }
                System.TimeSpan time = System.TimeSpan.FromSeconds(secs);
                float hr = time.Hours;
                float mn = time.Minutes;
                float ss = time.Seconds;
                if (double.IsNaN(hr) || double.IsInfinity(hr)) { hr = 0.0f; }
                if (double.IsNaN(mn) || double.IsInfinity(mn)) { mn = 0.0f; }
                if (double.IsNaN(ss) || double.IsInfinity(ss)) { ss = 0.0f; }
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Endurance", hr.ToString("0") + " hrs | " + mn.ToString("0") + " mins | " + ss.ToString("0") + " sec");

            }

            // --------------------------------------------------------------------------------------- Weight Config
            GUILayout.Space(10f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Weight Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("emptyWeight"), new GUIContent("Empty Weight"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumWeight"), new GUIContent("Maximum Weight"));
            float fuel = 0;
            float component = 0;
            float munition = 0;
            if (controller.m_core != null)
            {
                component = controller.m_core.componentLoad;
                munition = controller.m_core.gunLoad + controller.m_core.munitionLoad;
                fuel = controller.m_core.fuelLoad;
            }
            GUILayout.Space(5f);
            EditorGUILayout.LabelField("Component Weight", component.ToString("0.0") + " kg");
            //GUILayout.Space(3f);
            //EditorGUILayout.LabelField("Fuel Weight", fuel.ToString("0.0") + " kg");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Munition Weight", munition.ToString("0.0") + " kg");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Total Weight", controller.currentWeight.ToString("0.0") + " kg");


            // --------------------------------------------------------------------------------------- Weapons Config
            GUILayout.Space(10f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Weapon Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_hardpoints"), new GUIContent("State"));

            if (controller.m_hardpoints == Controller.StoreState.Connected)
            {
                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Launcher Config", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(launcher.FindPropertyRelative("fireSound"), new GUIContent("Fire Sound"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(launcher.FindPropertyRelative("fireVolume"), new GUIContent("Fire Volume"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(launcher.FindPropertyRelative("rateOfFire"), new GUIContent("Rocket Salvo Rate"));
            }

            // --------------------------------------------------------------------------------------- Origin Shift
            GUILayout.Space(10f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Origin Shift", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(origin.FindPropertyRelative("m_state"), new GUIContent(" "));
            if (controller.m_originShift.m_state == Oyedoyin.Common.Misc.ControlState.Active)
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(origin.FindPropertyRelative("m_threshold"), new GUIContent("Threshold (m)"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(origin.FindPropertyRelative("m_sceneState"), new GUIContent("Scene State"));

                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Particles", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(origin.FindPropertyRelative("m_particleShift"), new GUIContent("State"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(origin.FindPropertyRelative("m_particleCollection"), new GUIContent("Collection Mode"));

                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Line Renderer", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(origin.FindPropertyRelative("m_lineRendererShift"), new GUIContent("State"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(origin.FindPropertyRelative("m_lineRendererCollection"), new GUIContent("Collection Mode"));

                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Trail Renderer", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(origin.FindPropertyRelative("m_trailRendererShift"), new GUIContent("State"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(origin.FindPropertyRelative("m_trailRendererCollection"), new GUIContent("Collection Mode"));


                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Debug", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Shift Count", controller.m_originShift.m_shiftCount.ToString());
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Distance Traveled", (controller.m_originShift.m_totalDistance / 1000).ToString("0.000") + " km");
            }





            serializedObject.ApplyModifiedProperties();
        }
    }

#endif

    #endregion
}