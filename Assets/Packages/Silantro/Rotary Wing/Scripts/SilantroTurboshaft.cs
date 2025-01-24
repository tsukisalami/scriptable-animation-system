#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;
using Oyedoyin.Common;
using Oyedoyin.Mathematics;
using Oyedoyin.Common.Misc;
using Oyedoyin.Common.Components;

namespace Oyedoyin.RotaryWing
{
    #region Component
    /// <summary>
    /// 
    /// </summary>
    public class SilantroTurboshaft : MonoBehaviour
    {
        public enum State { FAIL, OFF, START, RUN };
        public enum PowerUnit { KW, SHP, Watts }

        public string engineIdentifier = "Default Engine";
        public State state = State.OFF;
        public PowerUnit powerUnit = PowerUnit.SHP;
        public EngineCore.EngineNumber engineNumber = EngineCore.EngineNumber.N1;
        public EngineCore.EnginePosition enginePosition = EngineCore.EnginePosition.Center;
        public RotaryController m_controller;

        // Parameters
        public double k_eng = 300;
        public double τE = 1000;
        public double Kp = 200;
        public double Ki = 2;
        public double Kd = 10;
        public double intmax = 1;
        public double J0 = 3;
        public double designRPM = 6000;
        public double setPower = 100;
        public double RatedPower = 840000;
        public double startTorque = 30;
        public double PSFC = 0.65;
        public double accelerationTorque = 250;
        public double accelerationRPM = 250;
        public double startDelay = 0.25;
        public double friction = 0.1;
        public double idleRatio = 0.2;
        public double inertia = 10;
        private double Ω0 => designRPM / 9.5492966;
        public float m_coreFactor;
        public AnimationCurve m_powerCorrection;
        public double psf, m_powerFactor;

        public EngineCore.InteriorMode interiorMode = EngineCore.InteriorMode.Off;
        public AudioClip ignitionInterior;
        public AudioClip ignitionExterior;
        public AudioClip shutdownInterior, shutdownExterior;
        public AudioClip exteriorIdle;
        public AudioClip interiorIdle;

        private AudioSource interiorSource;
        private AudioSource exteriorSource;
        private AudioSource interiorBase;
        private AudioSource exteriorBase;

        [Range(0.1f, 1)] public float m_maximumVolume = 0.6f;
        float m_interiorVolume;
        float m_exteriorVolume;
        float m_targetPitch, m_pitch, mxtv;

        public ParticleSystem exhaustSmoke;
        public ParticleSystem.EmissionModule smokeModule;
        public ParticleSystem exhaustDistortion;
        ParticleSystem.EmissionModule distortionModule;
        public float smokeEmissionLimit = 50f;
        public float distortionEmissionLimit = 20f;

        public bool start;
        public bool stop;
        public double throttle;
        public double m_runTime;
        protected Vector m_state = new Vector(0, 0, 0);
        public double m_corePower;

        public double load;
        public double Qeng;
        public double torque;
        public double Ω;
        public double δΩ;
        public double m_RPM;
        public double Mf;
        public double m_power;

        /// <summary>
        /// 
        /// </summary>
        public void StartEngine()
        {
            if (m_controller != null && m_controller.isControllable)
            {
                //MAKE SURE SOUND IS SET PROPERLY
                if (exteriorIdle == null || ignitionExterior == null || shutdownExterior == null)
                {
                    Debug.Log("Engine " + transform.name + " cannot start due to incorrect Audio configuration");
                }
                else
                {
                    //MAKE SURE THERE IS FUEL TO START THE ENGINE
                    if (m_controller && m_controller.fuelLevel > 1f)
                    {
                        //ACTUAL START ENGINE
                        if (m_controller.m_startMode == Controller.StartMode.Cold)
                        {
                            start = true;
                        }
                        if (m_controller.m_startMode == Controller.StartMode.Hot)
                        {
                            state = State.RUN;
                        }
                    }
                    else
                    {
                        Debug.Log("Engine " + transform.name + " cannot start due to low fuel");
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void ShutDownEngine()
        {
            stop = true;
        }
        /// <summary>
        /// 
        /// </summary>
        public void Initialize()
        {
            // Check Sounds
            if (ignitionExterior != null && exteriorIdle != null && shutdownExterior != null) { } else { Debug.LogError("Prerequisites not met on Engine " + transform.name + "....sound clips not assigned properly"); return; }

            // Configure Sound Sources
            GameObject soundPoint = new GameObject("_sources");
            soundPoint.transform.parent = transform;
            soundPoint.transform.localPosition = Vector3.zero;
            if (exteriorIdle) { Handler.SetupSoundSource(soundPoint.transform, exteriorIdle, "_exterior_base_point", 150f, true, true, out exteriorBase); }
            if (interiorIdle && interiorMode == EngineCore.InteriorMode.Active) { Handler.SetupSoundSource(soundPoint.transform, interiorIdle, "_interior_base_point", 80f, true, true, out interiorBase); }
            if (ignitionInterior && interiorMode == EngineCore.InteriorMode.Active) { Handler.SetupSoundSource(soundPoint.transform, ignitionInterior, "_interior_sound_point", 50f, false, false, out interiorSource); }
            if (ignitionExterior) { Handler.SetupSoundSource(soundPoint.transform, ignitionExterior, "_exterior_sound_point", 150f, false, false, out exteriorSource); }

            ConvertPower(); 

            // Plot Correction Curve
            m_powerCorrection = new AnimationCurve();
            m_powerCorrection.AddKey(new Keyframe(24083.78f, 0.357f)); // 31000 ft
            m_powerCorrection.AddKey(new Keyframe(50178.52f, 0.586f)); // 16000 ft
            m_powerCorrection.AddKey(new Keyframe(63585.00f, 0.707f)); // 12000 ft
            m_powerCorrection.AddKey(new Keyframe(71628.88f, 0.779f)); // 9500 ft
            m_powerCorrection.AddKey(new Keyframe(80630.38f, 0.850f)); // 6500 ft
            m_powerCorrection.AddKey(new Keyframe(90780.99f, 0.914f)); // 3000 ft
            m_powerCorrection.AddKey(new Keyframe(102224.4f, 1.000f)); // 0 ft
            m_powerCorrection.AddKey(new Keyframe(105959.0f, 1.029f)); // -1000 ft
#if UNITY_EDITOR
            for (int i = 0; i < m_powerCorrection.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(m_powerCorrection, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(m_powerCorrection, i, AnimationUtility.TangentMode.Linear);
            }
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        public void Compute(double dt)
        {
            // Transition to new phases
            if (state == State.START)
            {
                if (m_runTime < -0.1) { m_runTime = 0; } // first start update
                if (!exteriorSource.isPlaying) { if (Ω > idleRatio * Ω0) { state = State.RUN; } }
                m_runTime += dt;
            }
            else { m_runTime = -1; }

            // Update
            double Ωtarget;
            if (state == State.RUN) { Ωtarget = Ω0 * (idleRatio + (1.0 - idleRatio) * throttle); }
            else { Ωtarget = 0; }
            m_state.y = Ω - Ωtarget;

            // Perform RK4 Integration
            Vector k1 = dt * Step(m_state);
            Vector k2 = dt * Step(m_state + 0.5 * k1);
            Vector k3 = dt * Step(m_state + 0.5 * k2);
            Vector k4 = dt * Step(m_state + k3);
            m_state += (k1 + 2.0 * k2 + 2.0 * k3 + k4) / 6.0;

            // Collect Omega from state
            Qeng = m_state.x;
            δΩ = m_state.y;
            Ω = Ωtarget + δΩ;
            if (Ω < 0) { Ω = 0; }
            m_coreFactor = (float)(Ω / Ω0);

            // Limit power
            if ((Qeng * Ω) > RatedPower) { Qeng = RatedPower / Ω; }
            if (Qeng < 0) { Qeng = 0; }
            if (state == State.OFF || state == State.FAIL) { Qeng = 0; }
            m_state.x = Qeng;

            // Limit integrator term
            if (Math.Abs(m_state.z) > intmax)
            {
                if (m_state.z > 0) { m_state.z = intmax; }
                else { m_state.z = -intmax; }
            }

            // Set output ports
            if (double.IsNaN(Ω) || double.IsInfinity(Ω)) { Ω = 0; }
            m_RPM = Ω * 9.5492966;
            m_corePower = m_RPM / designRPM;
            if (m_RPM > 1) { torque = 9.5488 * (RatedPower * m_corePower) / m_RPM; } else { torque = 0; }

            // Altitude power correction
            psf = m_controller.m_core.m_atmosphere.Ps;
            m_powerFactor = m_powerCorrection.Evaluate((float)psf);
            m_power = RatedPower * m_powerFactor;

            double brakePower = (m_power * m_corePower / 745.6f);
            Mf = (PSFC * brakePower) / (3600f * 2.2046f);

            // Base Sounds
            AnalyseSound();
            AnalyseEffects();
            if (state == State.OFF || state == State.FAIL) { AnalyseOff(); }
            if (state == State.RUN) { AnalyseActive(); }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private Vector Step(Vector _state)
        {
            // Get load and inertia from input ports
            var Qload = load;
            var J = J0 + inertia;
            if (double.IsNaN(J) || double.IsInfinity(J) || J < 1) { J = 1; }

            Vector m_derivative = Vector.zero;

            if (double.IsNaN(m_state.x) || double.IsInfinity(m_state.x)) { m_state.x = 0; }
            if (double.IsNaN(m_state.y) || double.IsInfinity(m_state.y)) { m_state.y = 0; }
            if (double.IsNaN(m_state.z) || double.IsInfinity(m_state.z)) { m_state.z = 0; }

            if (state == State.RUN)
            {
                double kE = k_eng;
                m_derivative.x = -kE * Kd / (τE * J) * _state.x - kE * Kp / τE * _state.y - kE * Ki / τE * _state.z + kE * Kd / (τE * J) * Qload;
                m_derivative.y = 1.0 / J * _state.x - 1.0 / J * Qload;
                m_derivative.z = _state.y;
            }
            else if (state == State.OFF || state == State.FAIL)
            {
                m_derivative.x = 0;
                m_derivative.y = -Qload / J - friction * _state.y;
                m_derivative.z = 0;
            }
            else if (state == State.START)
            {
                m_derivative.x = 0;
                if (m_runTime < startDelay) { m_state.x = 0; }
                else if (Ω < accelerationRPM / 9.5492966) { m_state.x = startTorque; }
                else { m_state.x = accelerationTorque; }

                m_derivative.y = (_state.x / J) - (Qload / J);
                m_derivative.z = 0;
            }

            if (double.IsNaN(m_derivative.x) || double.IsInfinity(m_derivative.x)) { m_derivative.x = 0; }
            if (double.IsNaN(m_derivative.y) || double.IsInfinity(m_derivative.y)) { m_derivative.y = 0; }
            if (double.IsNaN(m_derivative.z) || double.IsInfinity(m_derivative.z)) { m_derivative.z = 0; }

            return m_derivative;
        }
        /// <summary>
        /// 
        /// </summary>
        private void AnalyseSound()
        {
            m_targetPitch = m_coreFactor;
            if (state == State.RUN && m_targetPitch < 0.3f) { m_targetPitch = 0.3f; }
            m_pitch = Mathf.Lerp(m_pitch, m_targetPitch, Time.fixedDeltaTime * 0.5f);

            if (m_controller.m_cameraState == SilantroCamera.CameraState.Exterior) { m_exteriorVolume = 1; m_interiorVolume = 0; }
            if (m_controller.m_cameraState == SilantroCamera.CameraState.Interior) { m_exteriorVolume = 0; m_interiorVolume = 1; }

            if (exteriorSource != null)
            {
                exteriorSource.volume = m_exteriorVolume * m_maximumVolume;
                exteriorSource.pitch = 1;
                exteriorBase.volume = m_exteriorVolume * m_maximumVolume;
                exteriorBase.pitch = m_pitch;
            }
            if (interiorSource != null)
            {
                if (m_controller != null && m_controller.m_view != null) { mxtv = m_interiorVolume * m_controller.m_view.maximumInteriorVolume; }
                else { mxtv = m_interiorVolume; }
                interiorSource.volume = mxtv;
                interiorBase.volume = mxtv;
                interiorBase.pitch = m_pitch;
                interiorSource.pitch = 1;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void AnalyseEffects()
        {
            // Collect Modules
            if (!smokeModule.enabled && exhaustSmoke != null) { smokeModule = exhaustSmoke.emission; }
            if (!distortionModule.enabled && exhaustDistortion != null) { distortionModule = exhaustDistortion.emission; }

            // Control Amount
            if (smokeModule.enabled) { smokeModule.rateOverTime = smokeEmissionLimit * m_coreFactor; }
            if (distortionModule.enabled) { distortionModule.rateOverTime = distortionEmissionLimit * m_coreFactor; }
        }
        /// <summary>
        /// 
        /// </summary>
        private void AnalyseActive()
        {
            if (exteriorSource.isPlaying) { exteriorSource.Stop(); }
            if (interiorSource != null && interiorSource.isPlaying) { interiorSource.Stop(); }
            if (stop)
            {
                exteriorSource.clip = shutdownExterior;
                exteriorSource.Play();
                if (interiorSource != null) { interiorSource.clip = shutdownInterior; interiorSource.Play(); }
                state = State.OFF;
                start = stop = false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void AnalyseOff()
        {
            if (exteriorSource.isPlaying && m_corePower < 0.01f) { exteriorSource.Stop(); }
            if (interiorSource != null && interiorSource.isPlaying && m_corePower < 0.01f) { interiorSource.Stop(); }
            if (load > 0 && m_corePower < 0.01f) { load = 0; }

            if (start)
            {
                exteriorSource.clip = ignitionExterior;
                exteriorSource.Play();
                if (interiorSource != null) { interiorSource.clip = ignitionInterior; interiorSource.Play(); }
                state = State.START;
                start = stop = false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected void ConvertPower()
        {
            if (powerUnit == PowerUnit.KW) { RatedPower = 1000 * setPower; }
            if (powerUnit == PowerUnit.SHP) { RatedPower = (1000 * setPower) / 1.34102; }
            if (powerUnit == PowerUnit.Watts) { RatedPower = setPower; }
            accelerationRPM = 0.05 * designRPM;
        }
        private void OnDrawGizmosSelected() { ConvertPower(); }
    }
    #endregion

    #region Editor
#if UNITY_EDITOR

    [CanEditMultipleObjects]
    [CustomEditor(typeof(SilantroTurboshaft))]
    public class TurboShaftEditor : Editor
    {

        Color backgroundColor;
        Color silantroColor = new Color(1.0f, 0.40f, 0f);
        SilantroTurboshaft prop;
        public int toolbarTab;
        public string currentTab;



        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        private void OnEnable() { prop = (SilantroTurboshaft)target; }


        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        public override void OnInspectorGUI()
        {
            backgroundColor = GUI.backgroundColor;
            //DrawDefaultInspector();
            serializedObject.Update();

            GUILayout.Space(2f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Engine Identifier", MessageType.None);
            GUI.color = backgroundColor;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineIdentifier"), new GUIContent(" "));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enginePosition"), new GUIContent("Position"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineNumber"), new GUIContent("Number"));


            GUILayout.Space(10f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Core Performance", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("setPower"), new GUIContent("Rated Power"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("powerUnit"), new GUIContent("Unit"));
            GUILayout.Space(3f);
            EditorGUILayout.LabelField(" ", (prop.RatedPower / 1000).ToString("0.00") + " kW");
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("designRPM"), new GUIContent("Rated RPM"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("k_eng"), new GUIContent("Engine Gain"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("τE"), new GUIContent("Time Constant"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PSFC"), new GUIContent("Fuel Consumption (PSFC)"));
            GUILayout.Space(3f);
            EditorGUILayout.LabelField(" ", prop.Mf.ToString("0.00") + " kg/s");

            GUILayout.Space(10f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Dynamic Performance", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("State", prop.state.ToString());
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startTorque"), new GUIContent("Start Torque (Nm)"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("accelerationTorque"), new GUIContent("Acceleration Torque (Nm)"));
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Start RPM Threshold ", prop.accelerationRPM.ToString("0.00") + " RPM");
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inertia"), new GUIContent("Inertia"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("friction"), new GUIContent("Friction"));


            GUILayout.Space(10f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Governor Feedback", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Kp"), new GUIContent("Proportional"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Ki"), new GUIContent("Integral"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("intmax"), new GUIContent("Integral Limit"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Kd"), new GUIContent("Derivative"));


            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(25f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Sound Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("interiorMode"), new GUIContent("Cabin Sounds"));
            GUILayout.Space(5f);
            if (prop.interiorMode == EngineCore.InteriorMode.Off)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ignitionExterior"), new GUIContent("Exterior Ignition"));
                GUILayout.Space(2f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("exteriorIdle"), new GUIContent("Exterior Idle"));
                GUILayout.Space(2f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("shutdownExterior"), new GUIContent("Exterior Shutdown"));
            }
            else
            {
                toolbarTab = GUILayout.Toolbar(toolbarTab, new string[] { "Exterior Sounds", "Interior Sounds" });
                GUILayout.Space(5f);
                switch (toolbarTab)
                {
                    case 0: currentTab = "Exterior Sounds"; break;
                    case 1: currentTab = "Interior Sounds"; break;
                }
                switch (currentTab)
                {
                    case "Exterior Sounds":
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("ignitionExterior"), new GUIContent("Exterior Ignition"));
                        GUILayout.Space(2f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("exteriorIdle"), new GUIContent("Exterior Idle"));
                        GUILayout.Space(2f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("shutdownExterior"), new GUIContent("Exterior Shutdown"));
                        break;

                    case "Interior Sounds":
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("ignitionInterior"), new GUIContent("Interior Ignition"));
                        GUILayout.Space(2f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("interiorIdle"), new GUIContent("Interior Idle"));
                        GUILayout.Space(2f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("shutdownInterior"), new GUIContent("Interior Shutdown"));
                        break;
                }
            }
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_maximumVolume"), new GUIContent("Volume Limit"));


            GUILayout.Space(10f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Effects Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(5f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("exhaustSmoke"), new GUIContent("Exhaust Smoke"));
            GUILayout.Space(2f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("smokeEmissionLimit"), new GUIContent("Maximum Emission"));
            GUILayout.Space(5f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("exhaustDistortion"), new GUIContent("Exhaust Distortion"));
            GUILayout.Space(2f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("distortionEmissionLimit"), new GUIContent("Maximum Emission"));

            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(25f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Engine Output", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Omega", prop.Ω.ToString("0.00") + " Rad/sec");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("δΩ", prop.δΩ.ToString("0.00") + " Rad/sec2");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Torque", prop.Qeng.ToString("0.00") + " Nm");

            GUILayout.Space(5f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Power", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Level", (prop.m_corePower * 100f).ToString("0.00") + " %");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Factor", prop.m_powerFactor.ToString("0.0000"));
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Actual", (prop.m_power / 1000).ToString("0.00") + " kW");

            serializedObject.ApplyModifiedProperties();
        }
    }

#endif
    #endregion
}
