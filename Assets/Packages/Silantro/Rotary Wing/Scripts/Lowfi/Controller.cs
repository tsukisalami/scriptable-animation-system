using System;
using UnityEngine;
using Oyedoyin.Common;
using System.Collections;
using Oyedoyin.Mathematics;
using Oyedoyin.Common.Misc;
using System.Collections.Generic;


namespace Oyedoyin.RotaryWing.LowFidelity
{
    public class Controller : MonoBehaviour
    {

        // ------------------------------------- Selectibles
        public enum InputType { Keyboard, Mobile, Mouse }
        public InputType inputType = InputType.Keyboard;
        public enum ModelType { Realistic, Arcade }
        public ModelType m_type = ModelType.Arcade;
        public enum RollYawCouple { Free, Combined }
        public RollYawCouple m_couple = RollYawCouple.Free;
        public enum RotationAxis { X, Y, Z }
        public RotationAxis mainRotorAxis = RotationAxis.X;
        public RotationAxis tailRotorAxis = RotationAxis.Y;
        public enum RotationDirection { CW, CCW }
        public RotationDirection mainRotorDirection = RotationDirection.CW;
        public RotationDirection tailRotorDirection = RotationDirection.CW;
        public enum VisulType { Default, Partial, Complete }
        public VisulType visualType = VisulType.Default;
        public enum InteriorMode { Off, Active }
        public InteriorMode interiorMode = InteriorMode.Off;
        public enum EngineState { Off, Starting, Active }
        public EngineState CurrentEngineState;
        public enum StartMode { Cold, Hot }
        public StartMode startMode = StartMode.Cold;
        public enum CameraState { Exterior, Interior }
        public CameraState cameraState = CameraState.Exterior;
        public enum AntiTorque { Force, Moment }
        public AntiTorque antiTorque = AntiTorque.Force;
        public enum GroundEffect { Consider, Neglect }
        public GroundEffect groundEffect = GroundEffect.Consider;
        public enum MomentBalance { Off, Active }
        public MomentBalance m_balance = MomentBalance.Off;
        public enum SoundMode { Basic, Advanced }
        public SoundMode soundMode = SoundMode.Basic;
        public enum m_override { Active, Off}
        public m_override keyboard_override = m_override.Off;


        // -------------------------------------Rotors
        public float mainRotorRadius = 5f;
        public float maximumRotorLift = 10000f;
        public float maximumRotorTorque = 6000f;
        public float maximumTailThrust = 2000f;
        public float MomentFactor = 20000f;
        public float mainRotorRPM = 200f;
        public float tailRotorRPM = 1000f;
        public float maximumClimbRate = 1500f;
        public float maximumDecentSpeed = 1000f;
        public float maximumTurnSpeed = 20f;
        public float direction;
        public float coreMainRPM;
        public float coreTailRPM;
        public float Ω, vz, µx, δf, δv, δG, h;
        public float maxAngularSpeed = 5f;
        public float rotationDrag = 2f;


        public bool m_isclimbing;
        public float m_collective_power;
        public float m_collective_speed = 0.5f;
        public float m_lift_force;
        public float m_balance_force;
        public float m_collective_balance = 2f;
        public float m_roll_balance_factor = 1f;
        public float m_pitch_balance_factor = 1f;
        public float m_yawMoment;
        [Range(0.01f, 1f)] public float m_couple_level = 0.4f;
        [Range(0.01f, 1f)] public float m_power_limit = 0.8f;
        public KeyCode m_climb = KeyCode.Alpha1;
        bool m_press_climb;
        public KeyCode m_decend = KeyCode.Alpha2;
        bool m_press_decend;

        // ------------------------------------ Connections
        public Transform mainRotorPosition;
        public Transform tailRotorPosition;
        public Transform centerOfGravity;
        public Rigidbody helicopter;
        public Camera normalExterior;
        public Camera normalInterior;
        public Camera currentCamera;
        public Transform cameraFocus;
        public Controller controller;
        public Helper helper;
        public MouseControl m_mouse;
        public Aerofoil[] foils;
        public SilantroBulb[] bulbs;


        // ------------------------------------ Controls
        public float processedThrottle;
        public float processedCollective;
        public float processedPitch;
        public float processedRoll;
        public float processedYaw;
        public float ϴ1c;
        public float ϴ1s;


        public AnimationCurve pitchInputCurve;
        public AnimationCurve rollInputCurve;
        public AnimationCurve yawInputCurve;
        [Range(1, 3)] public float pitchScale = 1, rollScale = 1, yawScale = 1;
        public Material[] normalRotor;
        public Material[] blurredRotor;
        public Color blurredRotorColor;
        public Color normalRotorColor;
        public float alphaSettings;
        public float normalBalance = 0.2f;

        // ------------------------------ Limits
        public float minimumPitchCyclic = 10;
        public float maximumPitchCyclic = 10f;
        public float minimumRollCyclic = 10f;
        public float maximumRollCyclic = 10f;
        private float ϴ1sMax, ϴ1cMax;
        private float ϴ1sMin, ϴ1cMin;


        // ------------------------------------ Engine
        public float corePower;
        public bool start, shutdown, clutching, active;
        [Range(0.01f, 1f)] public float baseCoreAcceleration = 0.25f;
        public float coreRPM, factorRPM, norminalRPM, functionalRPM = 1000f;
        public float idlePercentage = 10f, coreFactor, fuelFactor;


        // ------------------------------------ Fuel
        public float fuelCapacity = 500f;
        public float fuelLevel = 0f;
        public float bingoFuel = 50f;
        public float fuelConsumption = 0.15f;


        // ------------------------------------ Weight & Balance
        public float emptyWeight = 1000f;
        public float maximumWeight;
        public float currentWeight;
        [Tooltip("Resistance to movement on the pitch axis")] public float xInertia = 10000;
        [Tooltip("Resistance to movement in the roll axis")] public float yInertia = 5000;
        [Tooltip("Resistance to movement in the yaw axis")] public float zInertia = 8000;



        // ------------------------------------ Sounds
        public AudioSource interiorSource;
        public AudioSource exteriorSource;
        public AudioSource backSource;
        public AudioSource rotorSource, interiorBase;
        public AudioClip interiorIdle, backIdle;
        public AudioClip ignitionInterior, ignitionExterior;
        public AudioClip shutdownInterior, shutdownExterior;
        public AudioClip rotorRunning;
        public float exteriorVolume, interiorVolume;
        public float pitchTarget, overidePitch;
        public float maximumRotorPitch = 1.2f;




        //------------------------------------------ Effects
        [Serializable]
        public class m_effect
        {
            public ParticleSystem m_effect_particule;
            public ParticleSystem.EmissionModule m_effect_module;
            public float m_effect_limit = 50f;
        }
        public List<m_effect> m_effects = new List<m_effect>();




        //------------------------------------------ Free Camera
        public float azimuthSensitivity = 1;
        public float elevationSensitivity = 1;
        public float radiusSensitivity = 10;
        private float azimuth, elevation;
        public float radius;
        public float maximumRadius = 20f;
        Vector3 filterPosition; float filerY; Vector3 filteredPosition;
        Vector3 cameraDirection;
        public float maximumInteriorVolume = 0.8f;



        //------------------------------------------ Interior Camera
        public float viewSensitivity = 80f;
        public float maximumViewAngle = 80f;
        float verticalRotation, horizontalRotation;
        Vector3 baseCameraPosition;
        Quaternion baseCameraRotation, currentCameraRotation;
        public GameObject pilotObject;
        public float mouseSensitivity = 100.0f; public float clampAngle = 80.0f;
        public float scrollValue;
        public float zoomSensitivity = 3;
        public float maximumFOV = 20, currentFOV, baseFOV;



        //------------------------------------------ Ground Effect
        public AnimationCurve groundCorrection;
        public Vector3 groundAxis = new Vector3(0.0f, -1.0f, 0.0f);
        public LayerMask groundLayer;



        //------------------------------------------ Force
        public AnimationCurve thrustCorrection;
        public AnimationCurve inflowCorrection;
        public float Thrust, Torque;
        public Vector3 wind;
        public Vector3 Force;
        public Vector3 Moment;
        public Vector3 balanceMoment;


        //------------------------------------------ Controls
        public bool allOk;
        public bool zoomEnabled = true;
        public bool isControllable = true;








        //----------------------------------------------------------------------
        //----------------------------------------------------------------------
        //----------------------------------------------------------------------
        //--------------------------------------------------------------CONTROLS
        private void Start() { InitializeHelicopter(); }
        public void SetControlState(bool state) { isControllable = state; }
        public void StartEngine() { helper.StartEngine(); }
        public void ShutDownEngine() { helper.ShutDownEngine(); }
        public void ToggleCamera() { helper.ToggleCamera(); }
        public void ToggleLight() { helper.ToggleLightState(); }
        public void ResetScene() { UnityEngine.SceneManagement.SceneManager.LoadScene(this.gameObject.scene.name); }


















        //----------------------------------------------------------------------
        //----------------------------------------------------------------------
        //----------------------------------------------------------------------
        //------------------------------------------------------STATE MANAGEMENT
        private void FixedUpdate()
        {
            // ------------------------------------ Engine
            UpdateEngine();

            // ------------------------------------ Rotors
            if (mainRotorPosition != null) { UpdateVisuals(mainRotorPosition, coreMainRPM, mainRotorDirection, mainRotorAxis); }
            if (mainRotorPosition != null) { UpdateVisuals(tailRotorPosition, coreTailRPM, tailRotorDirection, tailRotorAxis); }

            // ------------------------------------ Forces
            UpdateForces();
        }



        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        private void Update()
        {
            // ------------------------------------ Camera
            UpdateCameras();

            // ------------------------------------ Controls
            UpdateControls();
        }







        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        void InitializeHelicopter()
        {
            //---------------------------------------- Engine
            helicopter = GetComponent<Rigidbody>();
            controller = GetComponent<Controller>();
            foils = GetComponentsInChildren<Aerofoil>();
            bulbs = GetComponentsInChildren<SilantroBulb>();
            helper.controller = controller;
            MathBase.m_plot_atm(out m_pressure, out m_temperature, out m_density);
            m_mouse.m_controller = controller;

            //---------------------------------------- Engine
            fuelLevel = fuelCapacity;
            direction = mainRotorDirection == RotationDirection.CCW ? 1 : -1f;
            Vector3 inertiaTensor = new Vector3(xInertia, yInertia, zInertia);
            helicopter.inertiaTensor = inertiaTensor;
            helicopter.angularDamping = rotationDrag;
            helicopter.maxAngularVelocity = maxAngularSpeed;

            //----------------------------------------Setup Sounds
            GameObject soundPoint = new GameObject("_sources"); soundPoint.transform.parent = transform; soundPoint.transform.localPosition = Vector3.zero;
            if (backIdle) { Handler.SetupSoundSource(soundPoint.transform, backIdle, "_rear_sound_point", 150f, true, true, out backSource); }
            if (ignitionInterior && interiorMode == InteriorMode.Active) { Handler.SetupSoundSource(soundPoint.transform, ignitionInterior, "_interior_sound_point", 50f, false, false, out interiorSource); }
            if (ignitionExterior) { Handler.SetupSoundSource(soundPoint.transform, ignitionExterior, "_exterior_sound_point", 150f, false, false, out exteriorSource); }
            if (interiorIdle && interiorMode == InteriorMode.Active) { Handler.SetupSoundSource(soundPoint.transform, interiorIdle, "_interior_base_point", 80f, true, true, out interiorBase); }
            if (rotorRunning) { Handler.SetupSoundSource(soundPoint.transform, rotorRunning, "_rotor_sound", 80f, true, true, out rotorSource); }


            //---------------------------------------- Cameras
            if (inputType == InputType.Mouse)
            {
                if (normalExterior != null && normalExterior.gameObject.activeSelf) { normalExterior.gameObject.SetActive(false); }
                if (normalInterior != null && normalInterior.gameObject.activeSelf) { normalInterior.gameObject.SetActive(false); }
            }
            if (normalExterior != null && normalExterior.GetComponent<AudioListener>() == null) { normalExterior.gameObject.AddComponent<AudioListener>(); }
            if (normalInterior != null && normalInterior.GetComponent<AudioListener>() == null) { normalInterior.gameObject.AddComponent<AudioListener>(); }
            if (normalInterior != null)
            {
                baseCameraPosition = normalInterior.transform.localPosition;
                baseCameraRotation = normalInterior.transform.localRotation;
                baseFOV = normalInterior.fieldOfView;
            }
            ActivateExteriorCamera();



            //---------------------------------------- Controls
            ϴ1sMax = Mathf.Abs(maximumRollCyclic) * Mathf.Deg2Rad;
            ϴ1sMin = Mathf.Abs(minimumRollCyclic) * Mathf.Deg2Rad;
            ϴ1cMax = Mathf.Abs(maximumPitchCyclic) * Mathf.Deg2Rad;
            ϴ1cMin = Mathf.Abs(minimumPitchCyclic) * Mathf.Deg2Rad;

            pitchInputCurve = MathBase.PlotControlInputCurve(pitchScale);
            rollInputCurve = MathBase.PlotControlInputCurve(rollScale);
            yawInputCurve = MathBase.PlotControlInputCurve(yawScale);
            Oyedoyin.Analysis.RMath.PlotClimbCorrection(out thrustCorrection, maximumClimbRate);
            if (groundEffect == GroundEffect.Consider) { Oyedoyin.Analysis.RMath.PlotGroundCorrection(out groundCorrection); }
            Oyedoyin.Analysis.RMath.PlotInflowCorrection(out inflowCorrection);


            if (visualType == VisulType.Complete || visualType == VisulType.Partial)
            {
                if (blurredRotor.Length > 0 && blurredRotor[0] != null) { blurredRotorColor = blurredRotor[0].color; alphaSettings = 0; }
                if (normalRotor.Length > 0 && normalRotor[0] != null) { normalRotorColor = normalRotor[0].color; }
            }



            //---------------------------------------- Aerofoil
            foreach (Aerofoil foil in foils) { foil.controller = controller; foil.helicopter = helicopter; }

            // --------------------------- Bulbs
            foreach (SilantroBulb bulb in bulbs) { bulb.Initialize(); }
        }



        public float m_key_overide = 0.2f;
        bool m_roll_pressed, m_pitch_pressed, m_yaw_pressed;
        float rawPitchInput, rawRollInput, rawYawInput, rawThrottleInput, throttleInput, collectiveInput, rawCollectiveInput;
        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        protected void UpdateControls()
        {
            if (inputType == InputType.Keyboard || inputType == InputType.Mouse)
            {

                if (Application.isFocused)
                {
                    // ----------------------------------------- Base
                    rawPitchInput = Input.GetAxis("Pitch");
                    rawRollInput = Input.GetAxis("Roll");
                    rawYawInput = Input.GetAxis("Rudder");
                    rawThrottleInput = (Input.GetAxis("Throttle"));
                    throttleInput = (rawThrottleInput + 1) / 2;
                    rawCollectiveInput = (Input.GetAxis("Collective"));
                    collectiveInput = (rawCollectiveInput + 1) / 2;



                    // ----------------------------------------- Toggles
                    if (Input.GetButtonDown("Start Engine")) { StartEngine(); }
                    if (Input.GetButtonDown("Stop Engine")) { ShutDownEngine(); }
                    if (Input.GetButtonDown("LightSwitch")) { ToggleLight(); }
                    if (Input.GetKeyDown(KeyCode.C)) { ToggleCamera(); }
                    if (Input.GetKeyDown(KeyCode.R)) { ResetScene(); }
                }

                if (inputType == InputType.Keyboard)
                {
                    // --------------------------------------------- Send
                    float pitchInput = -pitchInputCurve.Evaluate(rawPitchInput);
                    float rollInput = rollInputCurve.Evaluate(rawRollInput);
                    float yawInput = yawInputCurve.Evaluate(rawYawInput);

                    // --------------------------------------------- Receive
                    processedCollective = collectiveInput;
                    processedPitch = pitchInput;
                    processedRoll = rollInput;
                    if (m_type == ModelType.Realistic) { processedThrottle = throttleInput; } else { processedThrottle = 1; }
                    if (m_couple == RollYawCouple.Combined) { processedYaw = (m_couple_level * -processedRoll) - yawInput; } else { processedYaw = -yawInput; }

                }
                if (inputType == InputType.Mouse)
                {
                    if (Input.GetMouseButtonDown(0) && Cursor.visible) { Cursor.visible = false; }
                    if(Input.GetKeyDown(KeyCode.Escape) && !Cursor.visible) { Cursor.visible = true; }

                    float m_roll_keyboard = Input.GetAxis("Horizontal");
                    float m_pitch_keyboard = Input.GetAxis("Vertical");
                    //float m_yaw_keyboad = Input.GetAxis("Rudder");
                    m_roll_pressed = m_pitch_pressed = m_yaw_pressed = false;
                    if (Mathf.Abs(m_roll_keyboard) > m_key_overide) { m_roll_pressed = true; }
                    if (Mathf.Abs(m_pitch_keyboard) > m_key_overide) { m_pitch_pressed = true; }
                    //if (Mathf.Abs(m_yaw_keyboad) > m_key_overide) { m_yaw_pressed = true; }

                    processedCollective = collectiveInput;
                    if (controller.transform.position.y > 4.6f)
                    {
                        processedPitch = (m_pitch_pressed) ? m_pitch_keyboard : m_mouse.m_pitch;
                        processedRoll = (m_roll_pressed) ? m_roll_keyboard : m_mouse.m_roll;
                        float m_yaw = m_mouse.m_yaw;
                        if (m_couple == RollYawCouple.Combined) { processedYaw = (m_couple_level * -processedRoll) - m_yaw; } else { processedYaw = -m_yaw; }
                    }
                    else
                    { processedPitch = 0; processedRoll = 0f; processedYaw = 0f; }
                    if (m_type == ModelType.Realistic) { processedThrottle = throttleInput; } else { processedThrottle = 1; }
                    
                }

            }
            else if (inputType == InputType.Mobile)
            {
                // processedCollective = my_collective
                // processedThrottle = my_throttle
                // processedPitch = my_pitch
                // processedRoll = my_roll
                // processedYaw = my_yaw
            }






            ϴ1s = processedPitch > 0f ? processedPitch * ϴ1sMin : processedPitch * ϴ1sMax;
            ϴ1c = processedRoll > 0f ? processedRoll * ϴ1cMin : processedRoll * ϴ1cMax;
            if (m_type == ModelType.Arcade)
            {
                if (Input.GetKey(m_climb) && m_collective_power >= 1) { m_press_climb = true; m_collective_power = Mathf.MoveTowards(m_collective_power, 1.2f, Time.deltaTime * m_collective_speed); }
                if (!Input.GetKey(m_climb) && m_collective_power >= 1) { m_press_climb = false; m_collective_power = Mathf.MoveTowards(m_collective_power, 1f, Time.deltaTime * m_collective_speed); }
                if (Input.GetKey(m_decend) && m_collective_power >= m_power_limit) { m_isclimbing = true; m_collective_power = Mathf.MoveTowards(m_collective_power, m_power_limit, Time.deltaTime * m_collective_speed); }
                if (!Input.GetKey(m_decend)) { m_isclimbing = false; if (!Input.GetKey(m_climb) && m_collective_power >= 1) { m_collective_power = Mathf.MoveTowards(m_collective_power, 1f, Time.deltaTime * m_collective_speed); } }
                if (m_collective_power < 1f && m_isclimbing == false) { m_collective_power += Time.deltaTime * m_collective_speed; }
            }
        }



        public float m_vertical_speed;
        public float m_airspeed;
        public float m_pitchrate;
        public float m_rollrate;
        public float m_yawrate;
        public float m_turnrate;
        public float m_gforce;
        float m_gspeed = 0.05f;
        float m_temp;

        public AnimationCurve m_density;
        public AnimationCurve m_pressure;
        public AnimationCurve m_temperature;

        public float m_air_temperature;
        public float m_air_pressure;
        public float m_air_density;
        public float m_sound_speed;
        public float m_mach_speed;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        protected void UpdateForces()
        {
            // ---------------------------------- Weight
            currentWeight = emptyWeight + fuelLevel;
            helicopter.mass = currentWeight;
            if (m_type == ModelType.Realistic) { helicopter.centerOfMass = centerOfGravity.localPosition; }
            Vector3 υw = helicopter.GetPointVelocity(mainRotorPosition.position) + wind;
            Vector3 υl = helicopter.transform.InverseTransformDirection(υw);
            Ω = (2 * 3.142f * (1 + coreRPM)) / 60f;
            vz = Mathf.Sqrt(υl.x * υl.x + υl.z * υl.z);
            float δTt = 1 - ((vz * (float)Constants.ms2knots) / maximumTurnSpeed); if (δTt < 0) { δTt = 0f; }
            µx = vz / (Ω * mainRotorRadius);
            δf = thrustCorrection.Evaluate(υl.y * (float)Constants.toFtMin);
            δv = inflowCorrection.Evaluate(vz) / inflowCorrection.Evaluate(0);


            Vector3 m_localflow = transform.InverseTransformDirection(helicopter.linearVelocity);
            Vector3 m_angular_velocity = transform.InverseTransformDirection(helicopter.angularVelocity);
            m_pitchrate = (float)Math.Round((-m_angular_velocity.x * Mathf.Rad2Deg), 2);
            m_yawrate = (float)Math.Round((m_angular_velocity.y * Mathf.Rad2Deg), 2);
            m_rollrate = (float)Math.Round((-m_angular_velocity.z * Mathf.Rad2Deg), 2);
            m_vertical_speed = (float)Math.Round(helicopter.linearVelocity.y, 2);
            float m_turn_radius = (Mathf.Approximately(m_angular_velocity.x, 0.0f)) ? float.MaxValue : m_localflow.z / m_angular_velocity.x;
            float m_turn_force = (Mathf.Approximately(m_turn_radius, 0.0f)) ? 0.0f : (m_localflow.z * m_localflow.z) / m_turn_radius;
            float m_bg = m_turn_force / -9.81f; m_bg += transform.up.y * (Physics.gravity.y / -9.81f);
            float m_tg = (m_bg * m_gspeed) + (m_temp * (1.0f - m_gspeed));
            m_temp = m_tg; m_gforce = (float)Math.Round(m_tg, 1);
            float bankAngle = helicopter.transform.eulerAngles.z; if (bankAngle > 180.0f) { bankAngle = -(360.0f - bankAngle); }
            m_turnrate = (1091f * Mathf.Tan(bankAngle * Mathf.Deg2Rad)) / ((m_airspeed * (float)Constants.ms2knots) + 1);


            float m_kalt = helicopter.transform.position.y / 1000f;
            m_air_density = m_density.Evaluate(m_kalt);
            m_air_pressure = m_pressure.Evaluate(m_kalt);
            m_air_temperature = m_temperature.Evaluate(m_kalt);
            m_sound_speed = Mathf.Pow((1.2f * 287f * (273.15f + m_air_temperature)), 0.5f);
            m_mach_speed = m_airspeed / m_sound_speed;


            // ---------------------------------- Control
            float β1c = -ϴ1s;
            float β1s = ϴ1c;


            if (m_type == ModelType.Realistic)
            {
                // -------------------------------------------------- Ground Effect
                if (groundEffect == GroundEffect.Consider)
                {
                    Ray groundCheck = new Ray(transform.position, groundAxis);
                    RaycastHit groundHit;

                    if (Physics.Raycast(groundCheck, out groundHit, 1000, groundLayer))
                    { h = groundHit.distance; Debug.DrawLine(transform.position, groundHit.point, Color.red); }
                    if (h > 999f) { h = 999f; }
                    float zR = h / mainRotorRadius; if (zR > 3f) { zR = 3f; }
                    δG = groundCorrection.Evaluate(zR);
                }
                else { δG = 1; }


                Thrust = processedCollective * maximumRotorLift * coreFactor;
                Torque = processedCollective * maximumRotorTorque * coreFactor;
                float Fx = Thrust * Mathf.Sin(β1c) * Mathf.Cos(β1s);
                float Fy = -Thrust * Mathf.Cos(β1c) * Mathf.Sin(β1s);
                float Fz = Thrust * Mathf.Cos(β1c) * Mathf.Cos(β1s) * δf * δG * δv;
                Force = new Vector3(-Fy, 0, -Fx);
                helicopter.AddForceAtPosition(Fz * mainRotorPosition.up, mainRotorPosition.position, ForceMode.Force);
                helicopter.AddRelativeForce(Force, ForceMode.Force);
            }
            else if (m_type == ModelType.Arcade)
            {
                if (helicopter.linearVelocity.y < 0)
                {
                    m_balance_force = helicopter.mass * (Physics.gravity.y * -1) * (m_collective_balance * m_collective_power * coreFactor) * (1 + Mathf.Abs(helicopter.linearVelocity.y));
                    helicopter.AddRelativeForce(new Vector3(0, m_balance_force, 0));
                }
                if (m_press_climb == true)
                {
                    m_lift_force = maximumRotorLift * m_collective_power * coreFactor;
                    helicopter.AddRelativeForce(new Vector3(0, m_lift_force, 0), ForceMode.Force);
                }


                float Fx = m_lift_force * Mathf.Sin(β1c) * Mathf.Cos(β1s);
                float Fy = -m_lift_force * Mathf.Cos(β1c) * Mathf.Sin(β1s);
                Force = new Vector3(-Fy, 0, -Fx);
                helicopter.AddRelativeForce(Force, ForceMode.Force);

                float m_climb_limit = maximumClimbRate / (float)Constants.toFtMin;
                float m_decent_limit = maximumDecentSpeed / (float)Constants.toFtMin;
                if (helicopter.linearVelocity.y > m_climb_limit) { helicopter.linearVelocity = new Vector3(helicopter.linearVelocity.x, m_climb_limit, helicopter.linearVelocity.z); }
                if (helicopter.linearVelocity.y < -m_decent_limit) { helicopter.linearVelocity = new Vector3(helicopter.linearVelocity.x, -m_decent_limit, helicopter.linearVelocity.z); }
            }


            float Mx = -MomentFactor * β1s * coreFactor;
            float My = -MomentFactor * β1c * coreFactor;
            Moment = new Vector3(My, 0, Mx);



            helicopter.AddRelativeTorque(Moment, ForceMode.Force);
            m_yawMoment = processedYaw * maximumTailThrust * coreFactor * Vector3.Distance(centerOfGravity.position, tailRotorPosition.position);
            if (antiTorque == AntiTorque.Force) { helicopter.AddForceAtPosition(maximumTailThrust * δTt * coreFactor * processedYaw * helicopter.transform.right, tailRotorPosition.position, ForceMode.Force); }
            else { helicopter.AddRelativeTorque(new Vector3(0, -m_yawMoment, 0), ForceMode.Force); }


            if (m_balance == MomentBalance.Active)
            {
                float m_pitch = Mathf.DeltaAngle(0, -transform.rotation.eulerAngles.x);
                float m_roll = Mathf.DeltaAngle(0, -transform.rotation.eulerAngles.z);
                float m_pitch_moment = m_pitch_balance_factor * m_lift_force * Mathf.Sin((m_pitch - mainRotorPosition.localEulerAngles.x) * Mathf.Deg2Rad);
                float m_roll_moment = m_roll_balance_factor * m_lift_force * Mathf.Sin(m_roll * Mathf.Deg2Rad);
                helicopter.AddRelativeTorque(m_pitch_moment, 0f, m_roll_moment);
            }
        }


        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        protected void UpdateEngine()
        {

            // ------------------------------------------ Core
            if (active && fuelLevel < 5) { ShutDownEngine(); }
            if (active && isControllable == false) { ShutDownEngine(); }
            if (active) { if (corePower < 1f) { corePower += Time.fixedDeltaTime * baseCoreAcceleration; } }
            else if (corePower > 0f) { corePower -= Time.fixedDeltaTime * baseCoreAcceleration; }
            if (processedThrottle > 1) { processedThrottle = 1f; }
            if (corePower > 1) { corePower = 1f; }
            if (!active && corePower < 0) { corePower = 0f; }
            if (active && fuelLevel < 1) { shutdown = true; }
            fuelLevel -= fuelConsumption * coreFactor * Time.fixedDeltaTime;


            // ------------------------------------------ Fuel
            if (active && fuelLevel < bingoFuel)
            {
                float startRange = 0.2f; float endRange = 0.85f; float cycleRange = (endRange - startRange) / 2f;
                float offset = cycleRange + startRange; fuelFactor = offset + Mathf.Sin(Time.time * 3f) * cycleRange;
            }
            else { fuelFactor = 1f; }


            // ------------------------------------------ States
            switch (CurrentEngineState) { case EngineState.Off: StateOff(); break; case EngineState.Starting: StateStart(); break; case EngineState.Active: StateActive(); break; }


            // ------------------------------------------ RPM
            if (active) { factorRPM = Mathf.Lerp(factorRPM, norminalRPM, baseCoreAcceleration * Time.fixedDeltaTime * 2); }
            else { factorRPM = Mathf.Lerp(factorRPM, 0, baseCoreAcceleration * Time.fixedDeltaTime * 2f); }
            if (factorRPM > functionalRPM) { factorRPM = functionalRPM; }
            coreRPM = factorRPM * corePower * fuelFactor;
            coreFactor = coreRPM / functionalRPM;


            // ------------------------------------------ Volume
            if (cameraState == CameraState.Exterior) { exteriorVolume = corePower * 0.6f; interiorVolume = 0f; }
            if (cameraState == CameraState.Interior) { interiorVolume = corePower * 0.6f; exteriorVolume = 0f; }
            float speedFactor = ((coreRPM + (helicopter.linearVelocity.magnitude * 1.943f) + 10f) - functionalRPM * (idlePercentage / 100f)) / (functionalRPM - functionalRPM * (idlePercentage / 100f));
            pitchTarget = 0.35f + (0.7f * speedFactor);
            if (fuelFactor < 1) { overidePitch = pitchTarget; } else { overidePitch = fuelFactor * Mathf.Lerp(overidePitch, pitchTarget, Time.fixedDeltaTime * 0.5f); }
            pitchTarget *= fuelFactor; backSource.pitch = overidePitch;
            if (interiorMode == InteriorMode.Active && interiorBase != null) { interiorBase.pitch = overidePitch; }

            backSource.volume = exteriorVolume * coreFactor;
            exteriorSource.volume = exteriorVolume;
            if (interiorMode == InteriorMode.Active && interiorBase != null && interiorSource != null)
            {
                interiorSource.volume = interiorVolume;
                interiorBase.volume = interiorVolume * coreFactor;
            }
            coreMainRPM = mainRotorRPM * coreFactor;
            coreTailRPM = tailRotorRPM * coreFactor;


            // ------------------------------------------ Rotor Sound
            float soundFactor = 1;
            if (cameraState == CameraState.Interior) { soundFactor = 0.2f; }
            if (coreFactor < 0.01f) { rotorSource.Stop(); }
            else
            {
                if (!rotorSource.isPlaying) { rotorSource.Play(); }
                rotorSource.pitch = maximumRotorPitch * coreFactor;
                rotorSource.volume = coreFactor * soundFactor;
            }
        }


        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        protected void UpdateVisuals(Transform rotor, float rpm, RotationDirection direction, RotationAxis axis)
        {
            if (rpm > 0)
            {
                if (direction == RotationDirection.CW)
                {
                    if (axis == RotationAxis.X) { rotor.transform.Rotate(new Vector3(rpm * 5f * Time.fixedDeltaTime, 0, 0)); }
                    if (axis == RotationAxis.Y) { rotor.transform.Rotate(new Vector3(0, rpm * 5f * Time.fixedDeltaTime, 0)); }
                    if (axis == RotationAxis.Z) { rotor.transform.Rotate(new Vector3(0, 0, rpm * 5f * Time.fixedDeltaTime)); }
                }
                if (direction == RotationDirection.CCW)
                {
                    if (axis == RotationAxis.X) { rotor.transform.Rotate(new Vector3(-1f * rpm * 5f * Time.fixedDeltaTime, 0, 0)); }
                    if (axis == RotationAxis.Y) { rotor.transform.Rotate(new Vector3(0, -1f * rpm * 5f * Time.fixedDeltaTime, 0)); }
                    if (axis == RotationAxis.Z) { rotor.transform.Rotate(new Vector3(0, 0, -1f * rpm * 5f * Time.fixedDeltaTime)); }
                }
            }

            alphaSettings = coreFactor;
            if (visualType == VisulType.Complete)
            {
                if (blurredRotor != null && normalRotor != null)
                {
                    foreach (Material brotor in blurredRotor) { if (brotor != null) { brotor.color = new Color(blurredRotorColor.r, blurredRotorColor.g, blurredRotorColor.b, alphaSettings); } }
                    foreach (Material nrotor in normalRotor) { if (nrotor != null) { nrotor.color = new Color(normalRotorColor.r, normalRotorColor.g, normalRotorColor.b, (1 - alphaSettings) + normalBalance); } }
                }
            }
            if (visualType == VisulType.Partial)
            {
                if (blurredRotor != null)
                {
                    foreach (Material brotor in blurredRotor) { if (brotor != null) { brotor.color = new Color(blurredRotorColor.r, blurredRotorColor.g, blurredRotorColor.b, alphaSettings); } }
                }
            }


            // ----------------------------------------- Effects
            foreach (m_effect effect in m_effects)
            {
                if (!effect.m_effect_module.enabled && effect.m_effect_particule != null) { effect.m_effect_module = effect.m_effect_particule.emission; }
                if (effect.m_effect_module.enabled) { effect.m_effect_module.rateOverTime = effect.m_effect_limit * coreFactor; }
            }
        }


        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        protected void UpdateCameras()
        {
            if (inputType == InputType.Mouse)
            {
                m_mouse.m_update();
                if (normalExterior != null && normalExterior.gameObject.activeSelf) { normalExterior.gameObject.SetActive(false); }
                if (normalInterior != null && normalInterior.gameObject.activeSelf) { normalInterior.gameObject.SetActive(false); }
            }
            else
            {
                if (cameraState == CameraState.Exterior)
                {
                    if (Input.GetMouseButton(0) && Application.isFocused)
                    {
                        azimuth -= Input.GetAxis("Mouse X") * azimuthSensitivity * Time.deltaTime;
                        elevation -= Input.GetAxis("Mouse Y") * elevationSensitivity * Time.deltaTime;
                    }


                    //CALCULATE DIRECTION AND POSITION
                    SilantroCamera.SphericalToCartesian(radius, azimuth, elevation, out cameraDirection);
                    //CLAMP ROTATION IF AIRCRAFT IS ON THE GROUND//LESS THAN radius meters
                    if (cameraFocus.position.y < maximumRadius)
                    {
                        filterPosition = cameraFocus.position + cameraDirection;
                        filerY = filterPosition.y;
                        if (filerY < 2) filerY = 2;
                        filteredPosition = new Vector3(filterPosition.x, filerY, filterPosition.z);
                        normalExterior.transform.position = filteredPosition;
                    }
                    else
                    {
                        normalExterior.transform.position = cameraFocus.position + cameraDirection; ;
                    }


                    //POSITION CAMERA
                    normalExterior.transform.LookAt(cameraFocus);
                    radius = maximumRadius;//use this to control the distance from the aircraft
                }
                else
                {
                    if (normalInterior != null && Input.GetMouseButton(0))
                    {
                        if (Application.isFocused)
                        {
                            verticalRotation += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
                            horizontalRotation += -Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
                        }

                        //CLAMP ANGLES (You can make them independent to have a different maximum for each)
                        horizontalRotation = Mathf.Clamp(horizontalRotation, -clampAngle, clampAngle);
                        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);
                        //ASSIGN ROTATION
                        currentCameraRotation = Quaternion.Euler(horizontalRotation, verticalRotation, 0.0f);
                        normalInterior.transform.localRotation = currentCameraRotation;
                    }

                    //ZOOM
                    if (zoomEnabled && normalInterior != null)
                    {
                        currentFOV = normalInterior.fieldOfView;
                        currentFOV += Input.GetAxis("Mouse ScrollWheel") * zoomSensitivity;
                        currentFOV = Mathf.Clamp(currentFOV, maximumFOV, baseFOV);
                        normalInterior.fieldOfView = currentFOV;
                    }
                }
            }
        }


















        //----------------------------------------------------------------------
        //----------------------------------------------------------------------
        //----------------------------------------------------------------------
        //--------------------------------------------------------STATE FUNCTIONS


        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        void StateStart()
        {
            if (clutching) { if (!exteriorSource.isPlaying) { CurrentEngineState = EngineState.Active; clutching = false; StateActive(); } }
            else { exteriorSource.Stop(); if (interiorSource != null) { interiorSource.Stop(); } CurrentEngineState = EngineState.Off; }

            //------------------RUN
            norminalRPM = functionalRPM * (idlePercentage / 100f);
        }




        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        void StateOff()
        {
            if (exteriorSource.isPlaying && corePower < 0.01f) { exteriorSource.Stop(); }
            if (interiorSource != null && interiorSource.isPlaying && corePower < 0.01f) { interiorSource.Stop(); }


            //------------------START ENGINE
            if (start)
            {
                exteriorSource.clip = ignitionExterior; exteriorSource.Play();
                if (interiorSource != null) { interiorSource.clip = ignitionInterior; interiorSource.Play(); }
                CurrentEngineState = EngineState.Starting; clutching = true;
                active = true; StartCoroutine(ReturnIgnition());
            }

            //------------------RUN
            norminalRPM = 0f;
        }



        //--------------------------------------------------------ENGINE STATES
        public void StateActive()
        {
            if (exteriorSource.isPlaying) { exteriorSource.Stop(); }
            if (interiorSource != null && interiorSource.isPlaying) { interiorSource.Stop(); }

            //------------------STOP ENGINE
            if (shutdown)
            {
                exteriorSource.clip = shutdownExterior; exteriorSource.Play();
                if (interiorSource != null) { interiorSource.clip = shutdownInterior; interiorSource.Play(); }
                CurrentEngineState = EngineState.Off;
                active = false; StartCoroutine(ReturnIgnition());
            }

            //------------------RUN
            norminalRPM = (functionalRPM * (idlePercentage / 100f)) + ((functionalRPM) - (functionalRPM * (idlePercentage / 100f))) * processedThrottle;
        }
        public IEnumerator ReturnIgnition() { yield return new WaitForSeconds(0.5f); start = false; shutdown = false; }






        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        public void ActivateExteriorCamera()
        {
            // ------------------ Normal Interior Camera
            if (normalInterior != null)
            {
                normalInterior.enabled = false;
                AudioListener interiorListener = normalInterior.GetComponent<AudioListener>();
                if (interiorListener != null) { interiorListener.enabled = false; }
            }


            // ------------------ Normal Exterior Camera
            normalExterior.enabled = true; currentCamera = normalExterior;
            AudioListener exteriorListener = normalExterior.GetComponent<AudioListener>();
            if (exteriorListener != null) { exteriorListener.enabled = true; }
            cameraState = CameraState.Exterior;
        }




        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        public void ActivateInteriorCamera()
        {
            // ------------------ Normal Interior Camera
            if (normalInterior != null)
            {
                normalInterior.enabled = true;
                AudioListener interiorListener = normalInterior.GetComponent<AudioListener>();
                if (interiorListener != null) { interiorListener.enabled = true; }
            }
            else { Debug.Log("Interior Camera has not been setup"); return; }


            // ------------------ Normal Exterior Camera
            normalExterior.enabled = false; currentCamera = normalInterior;
            AudioListener exteriorListener = normalExterior.GetComponent<AudioListener>();
            if (exteriorListener != null) { exteriorListener.enabled = false; }
            cameraState = CameraState.Interior;
        }
    }
}
