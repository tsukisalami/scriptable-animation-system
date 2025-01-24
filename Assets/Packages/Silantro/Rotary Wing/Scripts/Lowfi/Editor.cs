using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Oyedoyin.RotaryWing.LowFidelity
{

#if UNITY_EDITOR
    // --------------------------------------------- Turboshaft
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Controller))]
    public class ControllerEditor : Editor
    {

        Color backgroundColor;
        Color silantroColor = new Color(1.0f, 0.40f, 0f);
        Controller controller;
        SerializedProperty m_helper;
        public int toolbarTab;
        public string currentTab;

        public int toolbarTabC; public string currentTabC;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        private void OnEnable() { controller = (Controller)target; m_helper = serializedObject.FindProperty("m_mouse"); }


        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        public override void OnInspectorGUI()
        {
            backgroundColor = GUI.backgroundColor;
            //DrawDefaultInspector ();
            serializedObject.Update();


            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(2f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Config", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(2f);
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("m_type"), new GUIContent("Type"));
            //GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inputType"), new GUIContent("Input"));
            if(controller.inputType == Controller.InputType.Mouse)
            {
                GUILayout.Space(2f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Mouse Control", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_range"), new GUIContent("Aim Range"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_control_sensitivity"), new GUIContent("Control Sensitivity"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_mouse_sensitivity"), new GUIContent("Mouse Sensitivity"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_damp_speed"), new GUIContent("Damp Speed"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_turn_angle"), new GUIContent("Turn Angle"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_hold_key"), new GUIContent("Hold Key"));

                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Output Control", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("solver"), new GUIContent("Solver Method"));
                if (controller.m_mouse.solver == MouseControl.m_solver.PID)
                {
                    GUILayout.Space(3f);
                    GUI.color = Color.white;
                    EditorGUILayout.HelpBox("Gains", MessageType.None);
                    GUI.color = backgroundColor;
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_pitch_gain"), new GUIContent("Pitch Gain"));
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_roll_gain"), new GUIContent("Roll Gain"));
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_yaw_gain"), new GUIContent("Yaw Gain"));
                }

                GUILayout.Space(6f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("keyboard_override"), new GUIContent("Keyboard Override"));
                if(controller.keyboard_override == Controller.m_override.Active)
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_key_overide"), new GUIContent("Threshold"));
                }

                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("HUD Control", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_target"), new GUIContent("Mouse Pointer"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_forward"), new GUIContent("Helicopter Pointer"));

                GUILayout.Space(5f);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Camera Control", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_camera"), new GUIContent("Camera"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_camera_pivot"), new GUIContent("Camera Pivot"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_container"), new GUIContent("Camera Holder"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(m_helper.FindPropertyRelative("m_target_object"), new GUIContent("Target Object"));

                

                GUILayout.Space(13f);
            }

            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startMode"), new GUIContent("Start Mode"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("antiTorque"), new GUIContent("Tail Control"));

            if (controller.m_type == Controller.ModelType.Arcade)
            {
                // ----------------------------------------------------------------------------------------------------------------------------------------------------------
                GUILayout.Space(15f);
                GUI.color = silantroColor;
                EditorGUILayout.HelpBox("Arcade Controls", MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(2f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_couple"), new GUIContent("Roll-Yaw Couple"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_balance"), new GUIContent("Moment Balance"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_climb"), new GUIContent("Climb Key"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_decend"), new GUIContent("Decend Key"));

                GUILayout.Space(8f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_collective_speed"), new GUIContent("Collective Speed"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_collective_balance"), new GUIContent("Lift Balance Factor"));
                if (controller.m_balance == Controller.MomentBalance.Active)
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_pitch_balance_factor"), new GUIContent("Pitch Balance Factor"));
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_roll_balance_factor"), new GUIContent("Roll Balance Factor"));
                }
                if (controller.m_couple == Controller.RollYawCouple.Combined)
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_couple_level"), new GUIContent("Couple Level"));
                }
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_power_limit"), new GUIContent("Power Limit"));
                GUILayout.Space(3f);
            }
            else
            {

            }




            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(25f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Variables", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(2f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mainRotorRadius"), new GUIContent("Rotor Radius"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumRotorLift"), new GUIContent("Max Lift"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumRotorTorque"), new GUIContent("Max Torque"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumTailThrust"), new GUIContent("Max Tail Thrust"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MomentFactor"), new GUIContent("Moment Factor"));
            //GUILayout.Space(3f);
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("mainRotorRPM"), new GUIContent("Main Rotor RPM"));
            //GUILayout.Space(3f);
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("tailRotorRPM"), new GUIContent("Tail Rotor RPM"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumClimbRate"), new GUIContent("Max Climb Rate (ft/min)"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumDecentSpeed"), new GUIContent("Max Decent Rate (ft/min)"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngularSpeed"), new GUIContent("Max Angular Speed (rad/s)"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationDrag"), new GUIContent("Angular Drag"));

            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(10f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Power Control", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(2f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseCoreAcceleration"), new GUIContent("Engine Acceleration"));
           
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("functionalRPM"), new GUIContent("Functional RPM"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idlePercentage"), new GUIContent("Idle Percentage"));


            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(25f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Fuel & Weight", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(2f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fuelCapacity"), new GUIContent("Fuel Capacity"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bingoFuel"), new GUIContent("Bingo Fuel"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fuelConsumption"), new GUIContent("Burn Rate (kg/s)"));
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Fuel Level", controller.fuelLevel.ToString("0.0") + " kg");
            GUILayout.Space(6f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("emptyWeight"), new GUIContent("Empty Weight"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumWeight"), new GUIContent("Maximum Weight"));
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Current Weight", controller.currentWeight.ToString("0.0") + " kg");



            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(15f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Inertia Settings", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(2f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("xInertia"), new GUIContent("Pitch Axis"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("zInertia"), new GUIContent("Roll Axis"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("yInertia"), new GUIContent("Yaw Axis"));

            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(25f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Camera Settings", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            toolbarTabC = GUILayout.Toolbar(toolbarTabC, new string[] { "Exterior Camera", "Interior Camera" });
            switch (toolbarTabC)
            {
                case 0: currentTabC = "Exterior Camera"; break;
                case 1: currentTabC = "Interior Camera"; break;
            }


            switch (currentTabC)
            {
                case "Exterior Camera":
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("normalExterior"), new GUIContent("Exterior Camera"));
                    GUILayout.Space(5f);
                    GUI.color = Color.white;
                    EditorGUILayout.HelpBox("Camera Movement", MessageType.None);
                    GUI.color = backgroundColor;
                    GUILayout.Space(2f);
                    serializedObject.FindProperty("elevationSensitivity").floatValue = EditorGUILayout.Slider("Elevation Sensitivity", serializedObject.FindProperty("elevationSensitivity").floatValue, 0f, 5f);
                    GUILayout.Space(3f);
                    serializedObject.FindProperty("azimuthSensitivity").floatValue = EditorGUILayout.Slider("Azimuth Sensitivity", serializedObject.FindProperty("azimuthSensitivity").floatValue, 0f, 5f);
                    GUILayout.Space(5f);
                    GUI.color = Color.white;
                    EditorGUILayout.HelpBox("Camera Positioning", MessageType.None);
                    GUI.color = backgroundColor;
                    GUILayout.Space(2f);
                    controller.maximumRadius = EditorGUILayout.FloatField("Camera Distance", controller.maximumRadius);

                    break;
                case "Interior Camera":
                    GUILayout.Space(3f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("normalInterior"), new GUIContent("Interior Camera"));
                    GUILayout.Space(5f);
                    GUI.color = Color.white;
                    EditorGUILayout.HelpBox("Camera Movement", MessageType.None);
                    GUI.color = backgroundColor;
                    GUILayout.Space(2f);
                    serializedObject.FindProperty("mouseSensitivity").floatValue = EditorGUILayout.Slider("Mouse Sensitivity", serializedObject.FindProperty("mouseSensitivity").floatValue, 0f, 100f);
                    GUILayout.Space(5f);
                    GUI.color = Color.white;
                    EditorGUILayout.HelpBox("Camera Positioning", MessageType.None);
                    GUI.color = backgroundColor;
                    GUILayout.Space(2f);
                    serializedObject.FindProperty("clampAngle").floatValue = EditorGUILayout.Slider("View Angle", serializedObject.FindProperty("clampAngle").floatValue, 10f, 210f);


                    GUILayout.Space(10f);
                    GUI.color = Color.white;
                    EditorGUILayout.HelpBox("Zoom Settings", MessageType.None);
                    GUI.color = backgroundColor;
                    GUILayout.Space(5f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("zoomEnabled"));
                    if (controller.zoomEnabled)
                    {
                        GUILayout.Space(3f);
                        serializedObject.FindProperty("maximumFOV").floatValue = EditorGUILayout.Slider("Minimum FOV", serializedObject.FindProperty("maximumFOV").floatValue, 0f, 100f);
                        GUILayout.Space(3f);
                        serializedObject.FindProperty("zoomSensitivity").floatValue = EditorGUILayout.Slider("Zoom Sensitivity", serializedObject.FindProperty("zoomSensitivity").floatValue, 0f, 20f);
                    }
                    break;
            }



            // ----------------------------------------------------------------------------------------------------------------------------------------------------------
            GUILayout.Space(25f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Connections", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(2f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mainRotorPosition"), new GUIContent("Main Rotor"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mainRotorAxis"), new GUIContent("Rotation Axis"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mainRotorDirection"), new GUIContent("Rotation Direction"));


            GUILayout.Space(6f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tailRotorPosition"), new GUIContent("Tail Rotor"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tailRotorAxis"), new GUIContent("Rotation Axis"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tailRotorDirection"), new GUIContent("Rotation Direction"));

            GUILayout.Space(6f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("centerOfGravity"), new GUIContent("COG Position"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraFocus"), new GUIContent("Camera Focus"));





            GUILayout.Space(10f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Effects Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            SerializedProperty ematerials = serializedObject.FindProperty("m_effects");
            GUIContent earrelLabel = new GUIContent("Effect Count");
            EditorGUILayout.PropertyField(ematerials.FindPropertyRelative("Array.size"), earrelLabel);
            GUILayout.Space(3f);
            for (int i = 0; i < ematerials.arraySize; i++)
            {
                //GUIContent label = new GUIContent("Effect " + (i + 1).ToString());
                //EditorGUILayout.PropertyField(ematerials.GetArrayElementAtIndex(i), label);

                SerializedProperty reference = ematerials.GetArrayElementAtIndex(i);
                SerializedProperty particule = reference.FindPropertyRelative("m_effect_particule");
                SerializedProperty emission = reference.FindPropertyRelative("m_effect_limit");

                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Effect : " + (i + 1).ToString(), MessageType.None);
                GUI.color = backgroundColor;
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(particule, new GUIContent("Particle"));
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(emission, new GUIContent("Emission Limit"));
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();


            GUILayout.Space(10f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Visuals Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("visualType"), new GUIContent(" "));
            if (controller.visualType == Controller.VisulType.Complete)
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
            if (controller.visualType == Controller.VisulType.Partial)
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
            GUILayout.Space(25f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Sound Configuration", MessageType.None);
            GUI.color = backgroundColor;
            //GUILayout.Space(3f);
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("soundMode"), new GUIContent("Mode"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("interiorMode"), new GUIContent("Cabin Sounds"));
            GUILayout.Space(5f);
            if (controller.soundMode == Controller.SoundMode.Basic)
            {
                if (controller.interiorMode == Controller.InteriorMode.Off)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ignitionExterior"), new GUIContent("Ignition Sound"));
                    GUILayout.Space(2f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("backIdle"), new GUIContent("Idle Sound"));
                    GUILayout.Space(2f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("shutdownExterior"), new GUIContent("Shutdown Sound"));
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
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("ignitionExterior"), new GUIContent("Ignition Sound"));
                            GUILayout.Space(2f);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("backIdle"), new GUIContent("Idle Sound"));
                            GUILayout.Space(2f);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("shutdownExterior"), new GUIContent("Shutdown Sound"));
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
            }
            else
            {
                GUILayout.Space(3f);
                if (controller.interiorMode == Controller.InteriorMode.Off)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ignitionExterior"), new GUIContent("Exterior Ignition"));
                    GUILayout.Space(2f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("frontIdle"), new GUIContent("Front Idle Sound"));
                    GUILayout.Space(2f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sideIdle"), new GUIContent("Side Idle Sound"));
                    GUILayout.Space(2f);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("backIdle"), new GUIContent("Rear Idle Sound"));
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
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("frontIdle"), new GUIContent("Front Idle Sound"));
                            GUILayout.Space(2f);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("sideIdle"), new GUIContent("Side Idle Sound"));
                            GUILayout.Space(2f);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("backIdle"), new GUIContent("Rear Idle Sound"));
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
            }

            GUILayout.Space(5f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotorRunning"), new GUIContent("Rotor Running"));
            
            
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
