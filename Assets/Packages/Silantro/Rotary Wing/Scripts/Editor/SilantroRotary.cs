using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System.IO;
using System.Linq;
using Oyedoyin.Common;
using System.Collections;
using Oyedoyin.Common.Misc;
using UnityEngine.Networking;


/// <summary>
/// 
/// </summary>
namespace Oyedoyin.RotaryWing.Editors
{
    #region Component Editors

    #region Rotor
#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SilantroRotor))]
    public class PhantomRotorEditor : Editor
    {
        Color backgroundColor;
        Color silantroColor = new Color(1.0f, 0.40f, 0f);
        SilantroRotor rotor;
        RotaryController m_controller;

        /// <summary>
        /// 
        /// </summary>
        private void OnEnable()
        {
            rotor = (SilantroRotor)target;
            m_controller = rotor.transform.gameObject.GetComponentInParent<RotaryController>();
        }

        /// <summary>
        /// 
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (m_controller != null) { rotor.rotorConfiguration = m_controller.m_configuration; }

            backgroundColor = GUI.backgroundColor;
            //DrawDefaultInspector();
            serializedObject.Update();
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Rotor Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("model"), new GUIContent("Inflow Model"));

            if (m_controller == null)
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotorConfiguration"), new GUIContent("Rotor Type"));
            }

            GUILayout.Space(5f);
            // ------------------------------------------ Conventional
            if (rotor.rotorConfiguration == RotaryController.RotorConfiguration.Conventional)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotorType"), new GUIContent(" "));
            }

            // ------------------------------------------ Coaxial
            if (rotor.rotorConfiguration == RotaryController.RotorConfiguration.Coaxial)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("coaxialPosition"), new GUIContent(" "));
            }

            // ------------------------------------------ Tandem
            if (rotor.rotorConfiguration == RotaryController.RotorConfiguration.Tandem)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tandemPosition"), new GUIContent(" "));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tandemAnalysis"), new GUIContent("Analysis Method"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("kov"), new GUIContent("kov"));
            }

            // ------------------------------------------ Inter meshing
            if (rotor.rotorConfiguration == RotaryController.RotorConfiguration.Syncrocopter)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("syncroPosition"), new GUIContent(" "));
            }


            if (rotor.rotorType != SilantroRotor.RotorType.TailRotor)
            {
                GUILayout.Space(10f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Ground Effect Component", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("groundEffect"), new GUIContent(" "));
                if (rotor.groundEffect == SilantroRotor.GroundEffectState.Consider)
                {
                    GUILayout.Space(5f);
                    EditorGUILayout.LabelField("Lift Percentage", (rotor.δT * 100f).ToString("0.00") + " %");
                }
            }



            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(10f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Dimensions", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotorRadius"), new GUIContent("Rotor Radius (m)"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotorHeadRadius"), new GUIContent("Rotor Head (m)"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Nb"), new GUIContent("Blade Count"));

            GUILayout.Space(15f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Blade Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("flappingState"), new GUIContent("Flapping State"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("twistType"), new GUIContent("Twist"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("surfaceFinish"), new GUIContent("Surface Finish"));


            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(10f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Weight", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("weightUnit"), new GUIContent("Unit"));
            if (rotor.weightUnit == WeightUnit.Pounds)
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bladeMass"), new GUIContent("Mass (lbs)"));
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Blade Mass", (rotor.actualWeight).ToString("0.00") + " kg");
            }
            else
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bladeMass"), new GUIContent("Mass (kg)"));
            }


            GUILayout.Space(10f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Dimensions", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            if (rotor.twistType != SilantroRotor.TwistType.None)
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bladeWashout"), new GUIContent("Washout (°)"));
            }
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bladeChord"), new GUIContent("Blade Chord"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("re"), new GUIContent("Hinge Ratio"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rootCutOut"), new GUIContent("Root Cutout %"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rootDeviation"), new GUIContent("Root Deviation"));


            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(10f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Blade Data", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Blade Radius", (rotor.bladeRadius).ToString("0.000") + " m");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Root Cut", (rotor.rootcut).ToString("0.000") + " m");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Hinge Offset", (rotor.hingeOffset).ToString("0.000") + " m");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Aspect Ratio", (rotor.aspectRatio).ToString("0.000"));
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Rotational Inertia", (rotor.J).ToString("0.00") + " kg/m2");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Flapping Inertia", (rotor.Iβ).ToString("0.00") + " kg/m2");


            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(10f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Airfoil Component", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("foilType"), new GUIContent("Type"));
            GUILayout.Space(3f);
            if (rotor.foilType == SilantroRotor.FoilType.Conventional)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rootAirfoil"), new GUIContent("Root Airfoil"));
                GUILayout.Space(5f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_tipAirfoil"), new GUIContent("Tip Airfoil"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("drawFoils"), new GUIContent("Draw Foil"));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rootSuperfoil"), new GUIContent("Root Airfoil"));
                GUILayout.Space(5f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_tipSuperfoil"), new GUIContent("Tip Airfoil"));
            }


            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(15f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Dynamic Configuration", MessageType.None);
            GUI.color = backgroundColor;

            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("funcionalRPM"), new GUIContent("Functional RPM"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_hinge"), new GUIContent("Hinge"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationAxis"), new GUIContent("Rotation Axis"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotorDirection"), new GUIContent("Rotation Direction"));
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Omega", rotor.Ω.ToString("0.0") + " Rad/s");


            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(15f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Audio Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("soundState"), new GUIContent("State"));
            if (rotor.soundState == SilantroRotor.SoundState.Active)
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_bladeChop"), new GUIContent("Rotor Sound"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_maximumPitch"), new GUIContent("Maximum Pitch"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_interiorVolume"), new GUIContent("Interior Volume"));
            }


            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(10f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Blur Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("visualType"), new GUIContent(" "));
            if (rotor.visualType == SilantroRotor.VisulType.Complete)
            {
                GUILayout.Space(2f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Blurred Rotor Materials", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                SerializedProperty bmaterials = serializedObject.FindProperty("blurredRotor");
                GUIContent barrelLabel = new GUIContent("Material Count");
                EditorGUILayout.PropertyField(bmaterials.FindPropertyRelative("Array.size"), barrelLabel);
                GUILayout.Space(3f);
                for (int i = 0; i < bmaterials.arraySize; i++)
                {
                    GUIContent label = new GUIContent("Material " + (i + 1).ToString());
                    EditorGUILayout.PropertyField(bmaterials.GetArrayElementAtIndex(i), label);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Normal Rotor Materials", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                SerializedProperty nmaterials = serializedObject.FindProperty("normalRotor");
                GUIContent nbarrelLabel = new GUIContent("Material Count");
                EditorGUILayout.PropertyField(nmaterials.FindPropertyRelative("Array.size"), nbarrelLabel);
                GUILayout.Space(3f);
                for (int i = 0; i < nmaterials.arraySize; i++)
                {
                    GUIContent label = new GUIContent("Material " + (i + 1).ToString());
                    EditorGUILayout.PropertyField(nmaterials.GetArrayElementAtIndex(i), label);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            if (rotor.visualType == SilantroRotor.VisulType.Partial)
            {
                GUILayout.Space(2f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Blurred Rotor Materials", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                SerializedProperty bmaterials = serializedObject.FindProperty("blurredRotor");
                GUIContent barrelLabel = new GUIContent("Material Count");
                EditorGUILayout.PropertyField(bmaterials.FindPropertyRelative("Array.size"), barrelLabel);
                GUILayout.Space(3f);
                for (int i = 0; i < bmaterials.arraySize; i++)
                {
                    GUIContent label = new GUIContent("Material " + (i + 1).ToString());
                    EditorGUILayout.PropertyField(bmaterials.GetArrayElementAtIndex(i), label);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(15f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Output", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("ꭙ", (rotor.ꭙ.m_real).ToString("0.00") + " °");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("υ", (rotor.νi.m_real).ToString("0.000") + " m/s");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("CT", (rotor.CT).ToString("0.00000"));
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("CQ", (rotor.CQ).ToString("0.00000"));
            //GUILayout.Space(3f);
            //EditorGUILayout.LabelField("CH", (rotor.CH).ToString("0.00000"));
            //GUILayout.Space(3f);
            //EditorGUILayout.LabelField("CY", (rotor.CY).ToString("0.00000"));

            GUILayout.Space(3f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Forces", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);

            EditorGUILayout.LabelField("Thrust", rotor.Thrust.ToString("0.00") + " N");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Torque", rotor.Torque.ToString("0.00") + " Nm");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Inertia", rotor.Inertia.ToString("0.00") + " kg/m2");

            if (rotor.rotorType == SilantroRotor.RotorType.MainRotor)
            {
                if (rotor.rotorConfiguration != RotaryController.RotorConfiguration.Tandem)
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.LabelField("Pitch Moment", rotor.m_moment.x.ToString("0.00") + " Nm");
                }
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Roll Moment", rotor.m_moment.z.ToString("0.00") + " Nm");
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
    #endregion

    #region Flight Computer

    [CustomEditor(typeof(RotaryComputer))]
    public class RotaryComputerEditor : Editor
    {
        Color backgroundColor;
        Color silantroColor = new Color(1.0f, 0.40f, 0f);
        RotaryComputer computer;
        RotaryController m_controller;
        SerializedProperty autopilot;

        SerializedProperty input;
        SerializedObject m_controllerObject;
        Rect curveRect = new Rect();

        /// <summary>
        /// 
        /// </summary>
        private void OnEnable()
        {
            computer = (RotaryComputer)target;
            autopilot = serializedObject.FindProperty("m_autopilot");
            if (computer != null)
            {
                m_controller = computer.transform.gameObject.GetComponentInParent<RotaryController>();
                m_controllerObject = new SerializedObject(m_controller);
                input = m_controllerObject.FindProperty("m_input");
            }
            curveRect.xMin = -1;
            curveRect.xMax = 1;
            curveRect.yMin = -1;
            curveRect.yMax = 1;
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
            EditorGUILayout.HelpBox("Control Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_mode"), new GUIContent("Mode"));

            if (computer.m_mode == Computer.Mode.Augmented)
            {
                GUILayout.Space(20f);
                GUI.color = silantroColor;
                EditorGUILayout.HelpBox("Stability Augmentation Configuration", MessageType.None);
                GUI.color = backgroundColor;

                GUILayout.Space(3f);
                GUI.color = Color.yellow;
                EditorGUILayout.HelpBox("Lateral Axis", MessageType.Info);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rollSAS"), new GUIContent("Roll SAS"));

                if (computer.m_rollSAS == ControlState.Active)
                {
                    GUILayout.Space(5f);
                    GUI.color = Color.white;
                    EditorGUILayout.HelpBox("Roll Leveler", MessageType.None);
                    GUI.color = backgroundColor;
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rollLeveler"), new GUIContent("State"));

                    if (computer.m_rollLeveler == ControlState.Active)
                    {
                        GUILayout.Space(3f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("Gaф"), new GUIContent("Gaф"));
                        GUILayout.Space(3f);
                        EditorGUILayout.LabelField("Output", computer.δrф.ToString("0.0000"));
                    }

                    GUILayout.Space(5f);
                    GUI.color = Color.white;
                    EditorGUILayout.HelpBox("Roll Rate Limiter", MessageType.None);
                    GUI.color = backgroundColor;
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rollRateLimiter"), new GUIContent("State"));

                    if (computer.m_rollRateLimiter == ControlState.Active)
                    {
                        GUILayout.Space(3f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("Gap"), new GUIContent("Gap"));
                        GUILayout.Space(3f);
                        EditorGUILayout.LabelField("Output", computer.δrp.ToString("0.0000"));
                    }

                    GUILayout.Space(5f);
                    GUI.color = Color.white;
                    EditorGUILayout.HelpBox("Roll Trim Hold", MessageType.None);
                    GUI.color = backgroundColor;
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rollTrimHold"), new GUIContent("State"));

                    if (computer.m_rollTrimHold == ControlState.Active)
                    {
                        GUILayout.Space(3f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("Gaxlat"), new GUIContent("Gaxlat"));
                        GUILayout.Space(3f);
                        EditorGUILayout.LabelField("Output", computer.δrc.ToString("0.0000"));
                    }
                }

                GUILayout.Space(3f);
                GUI.color = Color.yellow;
                EditorGUILayout.HelpBox("Longitudinal Axis", MessageType.Info);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_pitchSAS"), new GUIContent("Pitch SAS"));

                if (computer.m_pitchSAS == ControlState.Active)
                {
                    GUILayout.Space(5f);
                    GUI.color = Color.white;
                    EditorGUILayout.HelpBox("Pitch Leveler", MessageType.None);
                    GUI.color = backgroundColor;
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_pitchLeveler"), new GUIContent("State"));

                    if (computer.m_pitchLeveler == ControlState.Active)
                    {
                        GUILayout.Space(3f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("Gbθ"), new GUIContent("Gbθ"));
                        GUILayout.Space(3f);
                        EditorGUILayout.LabelField("Output", computer.δpθ.ToString("0.0000"));
                    }

                    GUILayout.Space(5f);
                    GUI.color = Color.white;
                    EditorGUILayout.HelpBox("Pitch Rate Limiter", MessageType.None);
                    GUI.color = backgroundColor;
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_pitchRateLimiter"), new GUIContent("State"));

                    if (computer.m_pitchRateLimiter == ControlState.Active)
                    {
                        GUILayout.Space(3f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("Gbq"), new GUIContent("Gbq"));
                        GUILayout.Space(3f);
                        EditorGUILayout.LabelField("Output", computer.δpq.ToString("0.0000"));
                    }

                    GUILayout.Space(5f);
                    GUI.color = Color.white;
                    EditorGUILayout.HelpBox("Pitch Trim Hold", MessageType.None);
                    GUI.color = backgroundColor;
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_pitchTrimHold"), new GUIContent("State"));

                    if (computer.m_pitchTrimHold == ControlState.Active)
                    {
                        GUILayout.Space(3f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("Gbxlon"), new GUIContent("Gbxlon"));
                        GUILayout.Space(3f);
                        EditorGUILayout.LabelField("Output", computer.δpc.ToString("0.0000"));
                    }
                }


                GUILayout.Space(3f);
                GUI.color = Color.yellow;
                EditorGUILayout.HelpBox("Directional Axis", MessageType.Info);
                GUI.color = backgroundColor;
                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Yaw Rate Limiter", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_yawRateLimiter"), new GUIContent("State"));
                if (computer.m_yawRateLimiter == ControlState.Active)
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Gθtr"), new GUIContent("Gθtr"));
                    GUILayout.Space(3f);
                    EditorGUILayout.LabelField("Output", computer.δyr.ToString("0.0000"));
                }

                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Yaw Trim Hold", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_yawTrimHold"), new GUIContent("State"));
                if (computer.m_yawTrimHold == ControlState.Active)
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("GθtѰ"), new GUIContent("GθtѰ"));
                    GUILayout.Space(3f);
                    EditorGUILayout.LabelField("Output", computer.δyѰ.ToString("0.0000"));
                }
            }

            if (computer.m_mode == Computer.Mode.Autonomous)
            {
                GUILayout.Space(20f);
                GUI.color = silantroColor;
                EditorGUILayout.HelpBox("Autopilot Configuration", MessageType.None);
                GUI.color = backgroundColor;

                GUILayout.Space(3f);
                GUI.color = Color.yellow;
                EditorGUILayout.HelpBox("Lateral Axis", MessageType.Info);
                GUI.color = backgroundColor;
                GUILayout.Space(5f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_lateralState"), new GUIContent("Mode"));
                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Rate Solver", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_rollRateSolver").FindPropertyRelative("m_Kp"), new GUIContent("Proportional"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_rollRateSolver").FindPropertyRelative("m_Ki"), new GUIContent("Integral"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_rollRateSolver").FindPropertyRelative("m_Kd"), new GUIContent("Derivative"));
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Multiplier", computer.m_autopilot.m_rollRateSolver.m_multiplier.ToString("0.000"));

                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Gains", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_bankGain"), new GUIContent("Bank Gain"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_headingGain"), new GUIContent("Heading Gain"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_turnGain"), new GUIContent("Turn Gain"));

                GUILayout.Space(8f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Drift Solver", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_driftSolver").FindPropertyRelative("m_Kp"), new GUIContent("Proportional"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_driftSolver").FindPropertyRelative("m_Ki"), new GUIContent("Integral"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_driftSolver").FindPropertyRelative("m_Kd"), new GUIContent("Derivative"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_driftSolver").FindPropertyRelative("m_antiWindup"), new GUIContent("Anti Windup"));
                if (computer.m_autopilot.m_driftSolver.m_antiWindup == Analysis.FPID.AntiWindupState.Active)
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_driftSolver").FindPropertyRelative("m_integralLimit"), new GUIContent("Cutoff Speed"));
                }

                GUILayout.Space(15f);
                GUI.color = Color.yellow;
                EditorGUILayout.HelpBox("Longitudinal Axis", MessageType.Info);
                GUI.color = backgroundColor;
                GUILayout.Space(5f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_longitudinalMode"), new GUIContent("Mode"));

                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Rate Solver", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_pitchRateSolver").FindPropertyRelative("m_Kp"), new GUIContent("Proportional"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_pitchRateSolver").FindPropertyRelative("m_Ki"), new GUIContent("Integral"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_pitchRateSolver").FindPropertyRelative("m_Kd"), new GUIContent("Derivative"));
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Multiplier", computer.m_autopilot.m_pitchRateSolver.m_multiplier.ToString("0.000"));

                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Gains", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_pitchGain"), new GUIContent("Pitch Gain"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("pressureGain"), new GUIContent("Pressure Gain"));

                GUILayout.Space(8f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Speed Solver", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_speedSolver").FindPropertyRelative("m_Kp"), new GUIContent("Proportional"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_speedSolver").FindPropertyRelative("m_Ki"), new GUIContent("Integral"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_speedSolver").FindPropertyRelative("m_Kd"), new GUIContent("Derivative"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_speedSolver").FindPropertyRelative("m_antiWindup"), new GUIContent("Anti Windup"));
                if (computer.m_autopilot.m_driftSolver.m_antiWindup == Analysis.FPID.AntiWindupState.Active)
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_speedSolver").FindPropertyRelative("m_integralLimit"), new GUIContent("Cutoff Speed"));
                }


                GUILayout.Space(15f);
                GUI.color = Color.yellow;
                EditorGUILayout.HelpBox("Power Axis", MessageType.Info);
                GUI.color = backgroundColor;
                GUILayout.Space(5f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_powerState"), new GUIContent("Mode"));
                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Rate Solver", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_climbSolver").FindPropertyRelative("m_Kp"), new GUIContent("Proportional"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_climbSolver").FindPropertyRelative("m_Ki"), new GUIContent("Integral"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_climbSolver").FindPropertyRelative("m_Kd"), new GUIContent("Derivative"));
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Multiplier", computer.m_autopilot.m_pitchRateSolver.m_multiplier.ToString("0.000"));

                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Gains", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_altitudeGain"), new GUIContent("Altitude Gain"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_altitudeIntegral"), new GUIContent("Altitude Integral"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_altitudeWindup"), new GUIContent("Altitude Windup"));


                GUILayout.Space(15f);
                GUI.color = Color.yellow;
                EditorGUILayout.HelpBox("Directional Axis", MessageType.Info);
                GUI.color = backgroundColor;
                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Rate Solver", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_yawRateSolver").FindPropertyRelative("m_Kp"), new GUIContent("Proportional"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_yawRateSolver").FindPropertyRelative("m_Ki"), new GUIContent("Integral"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_yawRateSolver").FindPropertyRelative("m_Kd"), new GUIContent("Derivative"));
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Multiplier", computer.m_autopilot.m_yawRateSolver.m_multiplier.ToString("0.000"));

                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Gains", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_transitionSpeed"), new GUIContent("Transition Speed"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(autopilot.FindPropertyRelative("m_pedalHeadingGain"), new GUIContent("Heading Gain"));


                GUILayout.Space(20f);
                GUI.color = silantroColor;
                EditorGUILayout.HelpBox("Limits", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumPitchRate"), new GUIContent("Maximum Pitch Rate"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumRollRate"), new GUIContent("Maximum Roll Rate"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumYawRate"), new GUIContent("Maximum Yaw Rate"));

                GUILayout.Space(5f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumPitchAngle"), new GUIContent("Maximum Pitch Angle"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minimumPitchAngle"), new GUIContent("Minimum Pitch Angle"));

                GUILayout.Space(5f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumTurnBank"), new GUIContent("Maximum Turn Bank"));
                GUILayout.Space(3f);
                EditorGUILayout.LabelField("Maximum Turn Rate", computer.maximumTurnRate.ToString("0.000") + " °/s");
            }

            GUILayout.Space(15f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Control Deflections", MessageType.None);
            GUI.color = backgroundColor;

            if (m_controller == null || m_controller != null && m_controller.m_configuration == RotaryController.RotorConfiguration.Conventional)
            {
                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Collective", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collectiveUpper"), new GUIContent("Upper Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collectiveLower"), new GUIContent("Lower Limit"));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Lateral Axis", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LateralUpper"), new GUIContent("Upper Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LateralLower"), new GUIContent("Lower Limit"));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Longitudinal Axis", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LongitudinalUpper"), new GUIContent("Upper Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LongitudinalLower"), new GUIContent("Lower Limit"));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Tail Rotor Collective", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("PedalUpper"), new GUIContent("Upper Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("PedalLower"), new GUIContent("Lower Limit"));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Collective Couples", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collectiveLateralCouple"), new GUIContent("Collective-Lateral Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collectivePedalCouple"), new GUIContent("Collective-Pedal Limit"));
            }

            if (m_controller == null || m_controller != null && m_controller.m_configuration == RotaryController.RotorConfiguration.Coaxial)
            {
                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Collective", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collectiveUpper"), new GUIContent("Upper Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collectiveLower"), new GUIContent("Lower Limit"));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Lateral Axis", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LateralUpper"), new GUIContent("Upper Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LateralLower"), new GUIContent("Lower Limit"));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Longitudinal Axis", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LongitudinalUpper"), new GUIContent("Upper Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LongitudinalLower"), new GUIContent("Lower Limit"));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Yaw Collective", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collectiveYaw"), new GUIContent("Upper Limit"));
            }

            if (m_controller == null || m_controller != null && m_controller.m_configuration == RotaryController.RotorConfiguration.Tandem)
            {
                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Collective", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collectiveUpper"), new GUIContent("Upper Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collectiveLower"), new GUIContent("Lower Limit"));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Lateral Axis", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LateralUpper"), new GUIContent("Upper Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LateralLower"), new GUIContent("Lower Limit"));
                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Yaw Lateral", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LateralYaw"), new GUIContent("Upper Limit"));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Longitudinal Gain", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("B1CFcurve"), new GUIContent("Forward Rotor"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("B1CRcurve"), new GUIContent("Rear Rotor"));
                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Longitudinal Collective", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collectivePitch"), new GUIContent("Upper Limit"));
            }

            if (m_controller == null || m_controller != null && m_controller.m_configuration == RotaryController.RotorConfiguration.Syncrocopter)
            {
                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Collective", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collectiveUpper"), new GUIContent("Upper Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collectiveLower"), new GUIContent("Lower Limit"));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Lateral Axis", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LateralUpper"), new GUIContent("Upper Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LateralLower"), new GUIContent("Lower Limit"));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Longitudinal Axis", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LongitudinalUpper"), new GUIContent("Upper Limit"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LongitudinalLower"), new GUIContent("Lower Limit"));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Lateral Yaw", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LateralYaw"), new GUIContent("Upper Limit"));
            }

            if (computer.m_mode != Computer.Mode.Autonomous)
            {
                m_controller.m_input.PlotInputCurves();

                GUILayout.Space(10f);
                GUI.color = silantroColor;
                EditorGUILayout.HelpBox("Input Tuning", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Pitch", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("_pitchDeadZone"), new GUIContent("Dead Zone"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("_pitchScale"), new GUIContent("Curvature"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("_pitchTrimDelta"), new GUIContent("Trim Step"));
                GUILayout.Space(3f);
                EditorGUILayout.CurveField("Curve", m_controller.m_input._pitchInputCurve, Color.green, curveRect, GUILayout.Height(100));

                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Roll", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("_rollDeadZone"), new GUIContent("Dead Zone"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("_rollScale"), new GUIContent("Curvature"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("_rollTrimDelta"), new GUIContent("Trim Step"));
                GUILayout.Space(3f);
                EditorGUILayout.CurveField("Curve", m_controller.m_input._rollInputCurve, Color.green, curveRect, GUILayout.Height(100));


                GUILayout.Space(3f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Yaw", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("_yawDeadZone"), new GUIContent("Dead Zone"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("_yawScale"), new GUIContent("Curvature"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(input.FindPropertyRelative("_yawTrimDelta"), new GUIContent("Trim Step"));
                GUILayout.Space(3f);
                EditorGUILayout.CurveField("Curve", m_controller.m_input._yawInputCurve, Color.green, curveRect, GUILayout.Height(100));
            }

            m_controllerObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
        }
    }

    #endregion

    #endregion


    /// <summary>
    /// 
    /// </summary>
    public class RotaryElements
    {
        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Aerofoil System/Sponson", false, 6100)]
        private static void AddSponsonSystem()
        {
            GameObject wing;
            if (Selection.activeGameObject != null)
            {
                wing = new GameObject("m_sponson");
                wing.transform.parent = Selection.activeGameObject.transform;
                wing.transform.localPosition = new Vector3(0, 0, 0);
            }
            else
            {
                wing = new GameObject("m_sponson");
                GameObject parent = new GameObject("m_dynamics");
                wing.transform.parent = parent.transform;
            }
            EditorSceneManager.MarkSceneDirty(wing.scene);
            SilantroStabilizer wingAerofoil = wing.AddComponent<SilantroStabilizer>();
            SilantroAirfoil foil = (SilantroAirfoil)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Airfoils/Wing/NACA 0015.asset", typeof(SilantroAirfoil));
            wingAerofoil.m_airfoil = foil;
            wingAerofoil.m_type = SilantroStabilizer.Type.Sponson;
        }

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Aerofoil System/Vertical Stabilizer", false, 6200)]
        private static void AddPlainSystem()
        {
            GameObject wing;
            if (Selection.activeGameObject != null)
            {
                wing = new GameObject("m_vertical_stabilizer");
                wing.transform.parent = Selection.activeGameObject.transform;
                wing.transform.localPosition = new Vector3(0, 0, 0);

            }
            else
            {
                wing = new GameObject("m_vertical_stabilizer");
                GameObject parent = new GameObject("m_dynamics");
                wing.transform.parent = parent.transform;
            }

            wing.transform.localRotation = Quaternion.Euler(0, 0, 90);

            EditorSceneManager.MarkSceneDirty(wing.scene);
            SilantroStabilizer wingAerofoil = wing.AddComponent<SilantroStabilizer>();
            SilantroAirfoil foil = (SilantroAirfoil)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Airfoils/Wing/NACA 0015.asset", typeof(SilantroAirfoil));
            wingAerofoil.m_airfoil = foil;
            wingAerofoil.m_type = SilantroStabilizer.Type.Vertical;
        }

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Aerofoil System/Horizontal Stabilizer", false, 6300)]
        private static void AddStabiatorSystem()
        {
            GameObject wing;
            if (Selection.activeGameObject != null)
            {
                wing = new GameObject("m_horizontal_stabilizer");
                wing.transform.parent = Selection.activeGameObject.transform;
                wing.transform.localPosition = new Vector3(0, 0, 0);

            }
            else
            {
                wing = new GameObject("m_horizontal_stabilizer");
                GameObject parent = new GameObject("m_dynamics");
                wing.transform.parent = parent.transform;
            }
            EditorSceneManager.MarkSceneDirty(wing.scene);
            SilantroStabilizer wingAerofoil = wing.AddComponent<SilantroStabilizer>();
            SilantroAirfoil foil = (SilantroAirfoil)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Airfoils/Wing/NACA 0015.asset", typeof(SilantroAirfoil));
            wingAerofoil.m_airfoil = foil;
            wingAerofoil.m_type = SilantroStabilizer.Type.Horizontal;
        }

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Power System/Piston Engine", false, 6400)]
        private static void AddPistonEngine()
        {
            if (Selection.activeGameObject != null)
            {
                GameObject exit = new GameObject { name = "_exhaust_point" };
                exit.transform.parent = Selection.activeGameObject.transform; exit.transform.localPosition = new Vector3(0, 0, -1);

                GameObject effects = new GameObject("_effects");
                effects.transform.parent = Selection.activeGameObject.transform;
                effects.transform.localPosition = new Vector3(0, 0, -2);

                Selection.activeGameObject.name = "Default Piston Engine";
                EditorSceneManager.MarkSceneDirty(Selection.activeGameObject.scene);

                SilantroPiston prop = Selection.activeGameObject.AddComponent<SilantroPiston>();
                prop.exitPoint = exit.transform;

                GameObject smoke = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Effects/Engine/Exhaust Smoke.prefab", typeof(GameObject));
                GameObject smokeEffect = Object.Instantiate(smoke, effects.transform.position, Quaternion.Euler(0, -180, 0), effects.transform);
                GameObject distortion = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Effects/Engine/Engine Distortion.prefab", typeof(GameObject));
                GameObject distortionEffect = Object.Instantiate(distortion, effects.transform.position, Quaternion.Euler(0, -180, 0), effects.transform);

                AudioClip start = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Sounds/Piston/Exterior/Piston Engine Start.wav", typeof(AudioClip));
                AudioClip stop = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Sounds/Piston/Exterior/Propeller Shutdown.wav", typeof(AudioClip));
                AudioClip run = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Sounds/Piston/Exterior/Propeller Running.wav", typeof(AudioClip));
                prop.core = new Common.Components.EngineCore
                {
                    exhaustSmoke = smokeEffect.GetComponent<ParticleSystem>(),
                    exhaustDistortion = distortionEffect.GetComponent<ParticleSystem>(),
                    distortionEmissionLimit = 20f,
                    ignitionExterior = start,
                    shutdownExterior = stop,
                    backIdle = run
                };
            }
            else
            {
                Debug.Log("Please Select GameObject to add Engine to..");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Power System/TurboShaft Engine", false, 6500)]
        private static void AddTurboshaftEngine()
        {
            if (Selection.activeGameObject != null)
            {
                GameObject exit = new GameObject { name = "_exhaust_point" };
                exit.transform.parent = Selection.activeGameObject.transform; exit.transform.localPosition = new Vector3(0, 0, -1);

                GameObject intake = new GameObject { name = "_intake_point" };
                intake.transform.parent = Selection.activeGameObject.transform; intake.transform.localPosition = new Vector3(0, 0, 1);

                GameObject effects = new GameObject("_effects");
                effects.transform.parent = Selection.activeGameObject.transform;
                effects.transform.localPosition = new Vector3(0, 0, -2);

                Selection.activeGameObject.name = "Default TurboShaft Engine";
                EditorSceneManager.MarkSceneDirty(Selection.activeGameObject.scene);
                Rigidbody parent = Selection.activeGameObject.transform.root.gameObject.GetComponent<Rigidbody>();
                if (parent == null) { Debug.Log("Engine is not parented to an Aircraft!! Create a default Rigidbody is you're just testing the Engine"); }
                SilantroTurboshaft shaft = Selection.activeGameObject.AddComponent<SilantroTurboshaft>();

                GameObject smoke = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Effects/Engine/Exhaust Smoke.prefab", typeof(GameObject));
                GameObject smokeEffect = Object.Instantiate(smoke, effects.transform.position, Quaternion.Euler(0, -180, 0), effects.transform);
                GameObject distortion = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Effects/Engine/Engine Distortion.prefab", typeof(GameObject));
                GameObject distortionEffect = Object.Instantiate(distortion, effects.transform.position, Quaternion.Euler(0, -180, 0), effects.transform);

                AudioClip start = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Rotary Wing/Sounds/Turbine/Exterior/Exterior Turbine Start.wav", typeof(AudioClip));
                AudioClip stop = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Rotary Wing/Sounds/Turbine/Exterior/Exterior Turbine Stop.wav", typeof(AudioClip));
                AudioClip run = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Rotary Wing/Sounds/Turbine/Exterior/Exterior Turbine Idle.wav", typeof(AudioClip));


                shaft.exhaustSmoke = smokeEffect.GetComponent<ParticleSystem>();
                shaft.exhaustDistortion = distortionEffect.GetComponent<ParticleSystem>();
                shaft.distortionEmissionLimit = 20f;
                shaft.ignitionExterior = start;
                shaft.shutdownExterior = stop;
                shaft.exteriorIdle = run;
            }
            else
            {
                Debug.Log("Please Select GameObject to add Engine to..");
            }
        }




        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Rotor System/Tandem", false, 6700)]
        private static void AddTandemRotor()
        {
            GameObject rotorSystem;
            rotorSystem = new GameObject("m_rotors");
            GameObject forwardRotor = new GameObject("Forward Rotor");
            GameObject backwardRotor = new GameObject("Backward Rotor");
            forwardRotor.transform.parent = rotorSystem.transform;
            forwardRotor.transform.localPosition = new Vector3(0, 0, 4);
            backwardRotor.transform.parent = rotorSystem.transform;
            backwardRotor.transform.localPosition = new Vector3(0, 0, -4);
            SilantroRotor ForwardRotor = forwardRotor.AddComponent<SilantroRotor>();
            SilantroRotor BackwardRotor = backwardRotor.AddComponent<SilantroRotor>();
            ForwardRotor.rotorType = SilantroRotor.RotorType.MainRotor;
            ForwardRotor.rotorConfiguration = RotaryController.RotorConfiguration.Tandem;
            ForwardRotor.tandemPosition = SilantroRotor.TandemPosition.Forward;
            BackwardRotor.rotorConfiguration = RotaryController.RotorConfiguration.Tandem;
            BackwardRotor.tandemPosition = SilantroRotor.TandemPosition.Rear;
            BackwardRotor.rotorDirection = RotationDirection.CCW;
            SilantroAirfoil foil = (SilantroAirfoil)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Airfoils/Wing/NACA 0015.asset", typeof(SilantroAirfoil));
            ForwardRotor.m_rootAirfoil = foil; ForwardRotor.drawFoils = true;
            ForwardRotor.m_tipAirfoil = foil; BackwardRotor.drawFoils = true;
            BackwardRotor.m_rootAirfoil = foil;
            BackwardRotor.m_tipAirfoil = foil;
            ForwardRotor.groundEffect = SilantroRotor.GroundEffectState.Consider;
            BackwardRotor.groundEffect = SilantroRotor.GroundEffectState.Consider;
        }

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Rotor System/Coaxial ", false, 6800)]
        private static void AddCoaxialRotor()
        {
            GameObject rotorSystem;
            rotorSystem = new GameObject("m_rotors");
            GameObject forwardRotor = new GameObject("Upper Rotor");
            GameObject backwardRotor = new GameObject("Lower Rotor");
            forwardRotor.transform.parent = rotorSystem.transform;
            forwardRotor.transform.localPosition = new Vector3(0, 0.5f, 0);
            backwardRotor.transform.parent = rotorSystem.transform;
            backwardRotor.transform.localPosition = new Vector3(0, -0.5f, 0);
            SilantroRotor ForwardRotor = forwardRotor.AddComponent<SilantroRotor>();
            SilantroRotor BackwardRotor = backwardRotor.AddComponent<SilantroRotor>();
            ForwardRotor.rotorType = SilantroRotor.RotorType.MainRotor;
            ForwardRotor.rotorConfiguration = RotaryController.RotorConfiguration.Coaxial;
            ForwardRotor.coaxialPosition = SilantroRotor.CoaxialPosition.Top;
            BackwardRotor.rotorConfiguration = RotaryController.RotorConfiguration.Coaxial;
            BackwardRotor.coaxialPosition = SilantroRotor.CoaxialPosition.Bottom;
            BackwardRotor.rotorDirection = RotationDirection.CCW;
            SilantroAirfoil foil = (SilantroAirfoil)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Airfoils/Wing/NACA 0015.asset", typeof(SilantroAirfoil));
            ForwardRotor.m_rootAirfoil = foil;
            ForwardRotor.m_tipAirfoil = foil;
            ForwardRotor.drawFoils = true;
            BackwardRotor.m_rootAirfoil = foil;
            BackwardRotor.drawFoils = true;
            BackwardRotor.m_tipAirfoil = foil;
            ForwardRotor.groundEffect = SilantroRotor.GroundEffectState.Consider;
            BackwardRotor.groundEffect = SilantroRotor.GroundEffectState.Consider;
        }


        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Rotor System/Syncrocopter ", false, 6900)]
        private static void AddSyncroRotor()
        {
            GameObject rotorSystem;
            rotorSystem = new GameObject("m_rotors");
            GameObject forwardRotor = new GameObject("Left Rotor");
            GameObject backwardRotor = new GameObject("Right Rotor");
            forwardRotor.transform.parent = rotorSystem.transform;
            forwardRotor.transform.localPosition = new Vector3(0, 1, 0);
            backwardRotor.transform.parent = rotorSystem.transform;
            backwardRotor.transform.localPosition = new Vector3(0, -1, 0);
            SilantroRotor ForwardRotor = forwardRotor.AddComponent<SilantroRotor>();
            SilantroRotor BackwardRotor = backwardRotor.AddComponent<SilantroRotor>();
            ForwardRotor.rotorType = SilantroRotor.RotorType.MainRotor;
            ForwardRotor.rotorConfiguration = RotaryController.RotorConfiguration.Syncrocopter;
            ForwardRotor.syncroPosition = SilantroRotor.SyncroPosition.Left;
            BackwardRotor.rotorConfiguration = RotaryController.RotorConfiguration.Syncrocopter;
            BackwardRotor.syncroPosition = SilantroRotor.SyncroPosition.Right;
            BackwardRotor.rotorDirection = RotationDirection.CCW;
            SilantroAirfoil foil = (SilantroAirfoil)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Airfoils/Wing/NACA 0015.asset", typeof(SilantroAirfoil));
            ForwardRotor.m_rootAirfoil = foil;
            ForwardRotor.m_tipAirfoil = foil; ForwardRotor.drawFoils = true;
            BackwardRotor.m_rootAirfoil = foil; BackwardRotor.drawFoils = true;
            BackwardRotor.m_tipAirfoil = foil;
            ForwardRotor.groundEffect = SilantroRotor.GroundEffectState.Consider;
            BackwardRotor.groundEffect = SilantroRotor.GroundEffectState.Consider;
        }

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Rotor System/Conventional/Main Rotor", false, 7000)]
        private static void AddMainRotor()
        {
            GameObject rotorSystem;
            rotorSystem = new GameObject("m_rotors");
            GameObject forwardRotor = new GameObject("Main Rotor");
            forwardRotor.transform.parent = rotorSystem.transform;
            forwardRotor.transform.localPosition = new Vector3(0, 1, 0);
            SilantroRotor ForwardRotor = forwardRotor.AddComponent<SilantroRotor>();
            ForwardRotor.rotorType = SilantroRotor.RotorType.MainRotor;
            ForwardRotor.rotorConfiguration = RotaryController.RotorConfiguration.Conventional;
            SilantroAirfoil foil = (SilantroAirfoil)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Airfoils/Wing/NACA 0015.asset", typeof(SilantroAirfoil));
            ForwardRotor.m_rootAirfoil = foil;
            ForwardRotor.drawFoils = true;
            ForwardRotor.m_tipAirfoil = foil;
            ForwardRotor.groundEffect = SilantroRotor.GroundEffectState.Consider;
        }

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Rotor System/Conventional/Tail Rotor", false, 7100)]
        private static void AddTailRotor()
        {
            GameObject rotorSystem;
            rotorSystem = new GameObject("m_rotors");
            GameObject backwardRotor = new GameObject("Tail Rotor");
            backwardRotor.transform.parent = rotorSystem.transform;
            backwardRotor.transform.localPosition = new Vector3(0, 0, -4);
            backwardRotor.transform.rotation = Quaternion.Euler(0, 0, -90);
            SilantroRotor BackwardRotor = backwardRotor.AddComponent<SilantroRotor>();
            BackwardRotor.rotorType = SilantroRotor.RotorType.TailRotor;
            BackwardRotor.rotorConfiguration = RotaryController.RotorConfiguration.Conventional;
            SilantroAirfoil foil = (SilantroAirfoil)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Common/Airfoils/Wing/NACA 0015.asset", typeof(SilantroAirfoil));
            BackwardRotor.m_rootAirfoil = foil;
            BackwardRotor.drawFoils = true;
            BackwardRotor.m_tipAirfoil = foil;
            BackwardRotor.soundState = SilantroRotor.SoundState.Silent;
        }


        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Create Internals", false, 7200)]
        public static void Helper()
        {
            GameObject aircraft;
            if (Selection.activeGameObject != null)
            {
                aircraft = Selection.activeGameObject;
                EditorSceneManager.MarkSceneDirty(Selection.activeGameObject.gameObject.scene);
                aircraft.name = "Default Helicopter";

                //Setup the controller
                Rigidbody sRigidbody = aircraft.GetComponent<Rigidbody>();
                if (sRigidbody == null) { sRigidbody = aircraft.AddComponent<Rigidbody>(); }
                sRigidbody.mass = 1000f;

                CapsuleCollider sCollider = aircraft.GetComponent<CapsuleCollider>();
                if (sCollider == null) { aircraft.AddComponent<CapsuleCollider>(); }

                RotaryController sController = aircraft.GetComponent<RotaryController>();
                if (sController == null) { aircraft.AddComponent<RotaryController>(); }


                GameObject core = new GameObject("m_core");
                GameObject aerodynamics = new GameObject("m_dynamics");
                GameObject rotors = new GameObject("m_rotors");
                GameObject tanks = new GameObject("m_tanks");
                GameObject power = new GameObject("m_engines");
                GameObject structure = new GameObject("m_structure");
                GameObject avionics = new GameObject("m_avionics");
                GameObject computer = new GameObject("m_computer");
                GameObject engine = new GameObject("m_engine");
                GameObject cog = new GameObject("m_empty_cog");


                GameObject body = new GameObject("_body");
                GameObject actuators = new GameObject("_actuators");
                GameObject cameraSystem = new GameObject("_cameras");
                GameObject focusPoint = new GameObject("_focus_point");
                GameObject incamera = new GameObject("Interior Camera");
                GameObject outcamera = new GameObject("Exterior Camera");

                GameObject lights = new GameObject("_lights");

                Transform aircraftParent = aircraft.transform;
                Vector3 defaultPosition = Vector3.zero;

                core.transform.parent = aircraftParent; core.transform.localPosition = defaultPosition;
                cog.transform.parent = core.transform; cog.transform.localPosition = defaultPosition;
                aerodynamics.transform.parent = aircraftParent; aerodynamics.transform.localPosition = defaultPosition;
                tanks.transform.parent = aircraftParent; tanks.transform.localPosition = defaultPosition;
                rotors.transform.parent = aircraftParent; rotors.transform.localPosition = defaultPosition;
                power.transform.parent = aircraftParent; power.transform.localPosition = defaultPosition;
                structure.transform.parent = aircraftParent; structure.transform.localPosition = defaultPosition;

                engine.transform.parent = power.transform; engine.transform.localPosition = defaultPosition;

                body.transform.parent = structure.transform; body.transform.localPosition = defaultPosition;
                avionics.transform.parent = aircraftParent; avionics.transform.localPosition = defaultPosition;
                actuators.transform.parent = avionics.transform; actuators.transform.localPosition = defaultPosition;
                cameraSystem.transform.parent = avionics.transform; cameraSystem.transform.localPosition = defaultPosition;


                lights.transform.parent = avionics.transform; lights.transform.localPosition = defaultPosition;
                incamera.transform.parent = cameraSystem.transform; incamera.transform.localPosition = defaultPosition;
                outcamera.transform.parent = cameraSystem.transform; outcamera.transform.localPosition = defaultPosition;
                focusPoint.transform.parent = cameraSystem.transform; focusPoint.transform.localPosition = defaultPosition;
                computer.transform.parent = aircraftParent; computer.transform.localPosition = defaultPosition;

                //GameObject bulb_caseR = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Rotary Wing/Prefabs/Avionics/Lights/Right Navigation.prefab", typeof(GameObject));
                //GameObject bulbR = Object.Instantiate(bulb_caseR, lights.transform.position, Quaternion.identity, lights.transform);
                //bulbR.transform.localPosition = new Vector3(2, 0, 0);
                //
                //GameObject bulb_caseL = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Silantro/Rotary Wing/Prefabs/Avionics/Lights/Left Navigation.prefab", typeof(GameObject));
                //GameObject bulbL = Object.Instantiate(bulb_caseL, lights.transform.position, Quaternion.identity, lights.transform);
                //bulbL.transform.localPosition = new Vector3(-2, 0, 0);

                //ADD CAMERAS
                Camera interior = incamera.AddComponent<Camera>();
                incamera.AddComponent<AudioListener>();
                Camera exterior = outcamera.AddComponent<Camera>();
                outcamera.AddComponent<AudioListener>();
                SilantroCamera view = cameraSystem.AddComponent<SilantroCamera>();
                view.normalExterior = exterior;
                view.normalInterior = interior;
                view.focusPoint = focusPoint.transform;
                computer.AddComponent<RotaryComputer>();
                SilantroCore module = core.AddComponent<SilantroCore>();
                module.emptyCenterOfMass = cog.transform;
            }
            else
            {
                Debug.Log("Please Select Aircraft GameObject to Setup..");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Help/Tutorials", false, 7300)]
        public static void Tutorial()
        {
            Application.OpenURL("https://youtube.com/playlist?list=PLJkxX6TkFwO9wykuD3a1fBsEn0Wyj2aIr");
        }

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Rotary Wing/Help/Report Bug", false, 7400)]
        public static void ContactBug()
        {
            Application.OpenURL("mailto:" + "silantrosimulator@gmail.com" + "?subject:" + "Silantro Rotary-Wing Toolkit Bug" + "&body:" + " ");
        }
    }
}

/// <summary>
/// 
/// </summary>
namespace Oyedoyin.Common.DefinitionSettings
{
    /// <summary>
    /// 
    /// </summary>
    [InitializeOnLoad]
    public class StartupRotary
    {
        static StartupRotary()
        {
            // Check Validity of pairs
            Definitions.ConfigurePairs();
            DefinitionSettings.SilantroTag tag = Definitions.CollectRotaryTag();
            if (tag.on == false)
            {
                tag.on = true;
                Definitions.UpdatePairInFile(tag);
                Definitions.SortDefines();
                Definitions.SetScriptDefines();
                Debug.Log("Rotary-Wing Defines Added!");
            }
        }
    }
}

/// <summary>
/// 
/// </summary>
namespace Oyedoyin.Communication
{
    /// <summary>
    /// 
    /// </summary>
    [InitializeOnLoad]
    public class RotaryWingUpdate : ScriptableObject
    {
        static RotaryWingUpdate m_Instance = null;
        private static readonly string _location = "Assets/Silantro/Common/Storage/Silantro_UPDATE.txt";
        private float l_rotary_version;
        private string l_command;

        /// <summary>
        /// 
        /// </summary>
        static RotaryWingUpdate()
        {
            EditorApplication.update += OnInit;
        }

        /// <summary>
        /// 
        /// </summary>
        static void OnInit()
        {
            EditorApplication.update -= OnInit;
            m_Instance = FindObjectOfType<RotaryWingUpdate>();
            if (m_Instance == null)
            {
                m_Instance = CreateInstance<RotaryWingUpdate>();
                if (!SessionState.GetBool("FirstInitRotaryDone", false))
                {
                    m_Instance.CheckUpdate();
                    SessionState.SetBool("FirstInitRotaryDone", true);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void CheckUpdate()
        {
            StartCoroutine(GetRequest("https://raw.githubusercontent.com/Oyedoyin/Silantro/main/rotary_wing.txt"));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_task"></param>
        protected void StartCoroutine(IEnumerator _task)
        {
            StaticCoroutine coworker = new GameObject("Worker_" + _task.ToString()).AddComponent<StaticCoroutine>();
            coworker.Work(_task);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        IEnumerator GetRequest(string uri)
        {
            if (!File.Exists(_location))
            {
                File.WriteAllText(_location, "3.515;3.515;YES");
            }
            else
            {
                // Local Data
                StreamReader m_localFile = new StreamReader(_location);
                string[] m_localData = m_localFile.ReadToEnd().Split(char.Parse(";"));
                l_rotary_version = float.Parse(string.Concat(m_localData[1].Where(c => !char.IsWhiteSpace(c))));
                l_command = string.Concat(m_localData[2].Where(c => !char.IsWhiteSpace(c)));
            }

            UnityWebRequest www = new UnityWebRequest(uri) { downloadHandler = new DownloadHandlerBuffer() };
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                if (l_command == "YES")
                {
                    Debug.Log("Unable to check for Silantro updates :( will try again next startup");
                }
            }
            else
            {
                // Internet Data
                string[] m_onlineData = www.downloadHandler.text.Split(char.Parse(";"));
                if (m_onlineData != null && m_onlineData.Length > 3)
                {
                    string o_type = string.Concat(m_onlineData[0].Where(c => !char.IsWhiteSpace(c)));
                    float o_version = float.Parse(string.Concat(m_onlineData[1].Where(c => !char.IsWhiteSpace(c))));
                    string o_mode = string.Concat(m_onlineData[2].Where(c => !char.IsWhiteSpace(c)));
                    string o_notes = m_onlineData[3].ToString();

                    if (o_version > l_rotary_version && l_command == "YES")
                    {
                        EditorUtility.DisplayDialog("Rotary Wing Update Available.  " + o_mode + " " + o_version,
                          o_notes,
                         "Close");
                    }
                }
            }
        }
    }
}
