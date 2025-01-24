using UnityEngine;
using Oyedoyin.Common;
#if UNITY_EDITOR
using UnityEditor;
#endif




namespace Oyedoyin.RotaryWing.LowFidelity
{
	public class Aerofoil : MonoBehaviour
	{

        // ------------------------------ Connections
        public SilantroAirfoil airfoil;
        public Rigidbody helicopter;
        public Controller controller;


        // ------------------------------ Variables
        public float span = 1;
        public float chord = 1;
        private Vector3 liftDirection;
        private float liftCoefficient = 0f;
        private float dragCoefficient = 0f;
        public float α = 0f;


        // ------------------------------ Output
        public float Lift;
        public float Drag;



        private void FixedUpdate()
        {
            if (helicopter != null && controller.isControllable)
            {
                float area = chord * span;
                Vector3 localFlow = transform.InverseTransformDirection(helicopter.GetPointVelocity(transform.position));
                localFlow.x = 0f;
                α = Vector3.Angle(Vector3.forward, localFlow);
                liftCoefficient = airfoil.liftCurve.Evaluate(α);
                dragCoefficient = airfoil.dragCurve.Evaluate(α);
                Lift = localFlow.sqrMagnitude * liftCoefficient * area * -Mathf.Sign(localFlow.y);
                Drag = localFlow.sqrMagnitude * dragCoefficient * area;
                liftDirection = Vector3.Cross(helicopter.linearVelocity, transform.right).normalized;
                helicopter.AddForceAtPosition(liftDirection * Lift, transform.position, ForceMode.Force);
                helicopter.AddForceAtPosition(-localFlow.normalized * Drag, transform.position, ForceMode.Force);
            }
        }






#if UNITY_EDITOR
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void OnDrawGizmosSelected()
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(span, 0f, chord));
            Gizmos.matrix = oldMatrix;
        }
#endif




#if UNITY_EDITOR
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void OnDrawGizmos()
        {
            Handles.color = Color.green; Handles.ArrowHandleCap(0, transform.position, transform.rotation * Quaternion.LookRotation(Vector3.up), .6f, EventType.Repaint);
        }
#endif
    }
}

