using UnityEngine;
using Oyedoyin.Common;
using Oyedoyin.Analysis;
using Oyedoyin.Mathematics;



namespace Oyedoyin.RotaryWing.LowFidelity
{
    [System.Serializable]
    public class Helper
    {

        // --------------------------------------- Connections
        public Controller controller;




        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        public void ToggleCamera()
        {
            if (controller.cameraState == Controller.CameraState.Exterior) { controller.ActivateInteriorCamera(); }
            else { controller.ActivateExteriorCamera(); }
        }



        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        public void StartEngine()
        {
            if (controller.backIdle == null || controller.ignitionExterior == null || controller.shutdownExterior == null) { Debug.Log("Engine " + controller.transform.name + " cannot start due to incorrect Audio configuration"); }
            else
            {
                if (controller.fuelLevel > 1f)
                {
                    if (controller.startMode == Controller.StartMode.Cold) { controller.start = true; }
                    if (controller.startMode == Controller.StartMode.Hot) { controller.active = true; controller.StateActive(); controller.clutching = false; controller.CurrentEngineState = Controller.EngineState.Active; }
                }
                else { Debug.Log("Engine " + controller.transform.name + " cannot start due to low fuel"); }
            }
        }



        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        public void ShutDownEngine() { controller.shutdown = true; }



        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        public void ToggleLightState() { if (controller != null && controller.isControllable) { foreach (SilantroBulb light in controller.bulbs) { if (light.state == SilantroBulb.CurrentState.On) { light.SwitchOff(); } else { light.SwitchOn(); } } } }
        public void TurnOffLights() { if (controller != null && controller.isControllable) { foreach (SilantroBulb light in controller.bulbs) { if (light.state == SilantroBulb.CurrentState.On) { light.SwitchOff(); } } } }
        public void ToggleCameraState() { if (controller != null && controller.isControllable) { controller.ToggleCamera(); } }
    }
}



namespace Oyedoyin.RotaryWing.LowFidelity
{

    [System.Serializable]
    public class MouseControl
    {
        // ------------------------------ Connections
        public Controller m_controller;
        public RectTransform m_target;
        public RectTransform m_forward;
        public Camera m_camera;
        public Transform m_camera_pivot;
        public Transform m_target_object;
        public Transform m_container;

        public enum m_function { Update, FixedUpdate }
        public m_function function = m_function.Update;
        public KeyCode m_hold_key = KeyCode.B;
        public enum m_solver { PID, Simple}
        public m_solver solver = m_solver.Simple;


        // ------------------------------ Variables
        public float m_damp_speed = 5.0f;
        public float m_range = 500f;
        public float m_mouse_sensitivity = 5f;
        private Vector3 m_hold_direction = Vector3.forward;
        private bool m_hold;
        public float m_control_sensitivity = 5f;
        public float m_turn_angle = 10f;



        // ------------------------------ Output
        public float m_roll;
        public float m_pitch;
        public float m_yaw;


        public FPID pitchSolver;
        public FPID rollSolver;
        public FPID yawSolver;

        public Vector3 m_pitch_gain = new Vector3(1, 0, 0.5f);
        public Vector3 m_roll_gain = new Vector3(1, 0, 0.5f);
        public Vector3 m_yaw_gain = new Vector3(1, 0, 0.5f);

        public Vector3 m_forward_direction { get { return m_controller == null ? m_container.forward * m_range : (m_controller.transform.forward * m_range) + m_controller.transform.position; } }
        public Vector3 m_target_position { get { if (m_target_object != null) { return m_hold ? m_hold_position() : m_target_object.position + (m_target_object.forward * m_range); } else { return m_container.forward * m_range; } } }
        private Vector3 m_hold_position() { if (m_target_object != null) { return m_target_object.position + (m_hold_direction * m_range); } else { return m_container.forward * m_range; } }




        public void m_update()
        {
            if (m_camera)
            {
                //AIRCRAFT CROSSHAIR
                if (m_target != null)
                {
                    m_target.position = m_camera.WorldToScreenPoint(m_target_position);
                    m_target.gameObject.SetActive(m_target.position.z > 1f);
                }

                //MOUSE AIM
                if (m_forward != null)
                {
                    m_forward.position = m_camera.WorldToScreenPoint(m_forward_direction);
                    m_forward.gameObject.SetActive(m_forward.position.z > 1f);
                }
            }


            if (m_controller != null)
            {
                // ----------------------- Mouse Logic
                m_container.position = m_controller.transform.position;
                if (Input.GetKeyDown(m_hold_key)) { m_hold = true; m_hold_direction = m_target_object.forward; }
                else if (Input.GetKeyUp(m_hold_key)) { m_hold = false; m_target_object.forward = m_hold_direction; }
                float m_horizontal = Input.GetAxis("Mouse X") * m_mouse_sensitivity;
                float m_vertical = -Input.GetAxis("Mouse Y") * m_mouse_sensitivity;
                m_target_object.Rotate(m_camera.transform.right, m_vertical, Space.World);
                m_target_object.Rotate(m_camera.transform.up, m_horizontal, Space.World);
                Vector3 m_velocity = (Mathf.Abs(m_target_object.forward.y) > 0.9f) ? m_camera_pivot.up : Vector3.up;
                m_camera_pivot.rotation = MathBase.Damp(m_camera_pivot.rotation, Quaternion.LookRotation(m_target_object.forward, m_velocity), m_damp_speed, Time.deltaTime);




                // ----------------------- Control Logic
                Vector3 m_local = m_controller.transform.InverseTransformPoint(m_target_position).normalized * m_control_sensitivity;
                float m_target_offset = Vector3.Angle(m_controller.transform.forward, m_target_position - m_controller.transform.position);
                float m_yaw_error = Mathf.Clamp(m_local.x, -1f, 1f);
                float m_pitch_error = -Mathf.Clamp(m_local.y, -1f, 1f);

                if(solver == m_solver.Simple)
                {
                    m_pitch = m_pitch_error;
                    m_yaw = m_yaw_error;
                }
                else
                {
                    pitchSolver.m_Kp = m_pitch_gain.x;
                    pitchSolver.m_Ki = m_pitch_gain.y;
                    pitchSolver.m_Kd = m_pitch_gain.z;

                    yawSolver.m_Kp = m_yaw_gain.x;
                    yawSolver.m_Ki = m_yaw_gain.y;
                    yawSolver.m_Kd = m_yaw_gain.z;


                    rollSolver.m_Kp = m_roll_gain.x;
                    rollSolver.m_Ki = m_yaw_gain.y;
                    rollSolver.m_Kd = m_yaw_gain.z;

                    m_pitch = (float)pitchSolver.Compute(m_pitch_error, Time.deltaTime);
                    m_yaw = (float)yawSolver.Compute(m_yaw_error, Time.deltaTime);
                }
                m_roll = Mathf.Lerp(m_controller.transform.right.y, Mathf.Clamp(m_local.x, -1f, 1f), Mathf.InverseLerp(0f, m_turn_angle, m_target_offset));
            }
        }
    }
}