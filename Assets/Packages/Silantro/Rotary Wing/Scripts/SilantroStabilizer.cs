#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;
using Oyedoyin.Common;
using Oyedoyin.Mathematics;

namespace Oyedoyin.RotaryWing
{
    #region Component
    public class SilantroStabilizer : MonoBehaviour
    {
        public enum Position { Right, Left }
        public enum Type { Vertical, Horizontal, Sponson }
        public enum Sweep { None, Forward, Backward }


        [Header("Dimensions")]
        public float m_rootChord;
        public float m_span;
        public float m_section = 1;
        [Range(0, 60)] public float m_sweep;
        [Range(0, 99)] public float m_taper;
        public double m_area;

        public RotaryController m_controller;
        public SilantroAirfoil m_airfoil;
        public BoxCollider m_collider;
        public Type m_type = Type.Vertical;
        public Position m_position = Position.Right;
        public Sweep m_sweepDirection = Sweep.None;

        #region Simulation Properties

        public Vector3 localVelocity;
        private double ub, vb, wb;
        private readonly double pb, qb, rb;
        public double us, vs, ws;

        public double iw;
        public double ѡi;
        public double νi;
        public double Kv = 1.01;
        public double βrad, β;
        public double αrad, α;

        #endregion

        [Header("Output")]
        public double CL;
        public double CD;
        public double m_lift;
        public double m_drag;


        /// <summary>
        /// 
        /// </summary>
        public void Compute()
        {
            AnalyseForces();
        }
        /// <summary>
        /// 
        /// </summary>
        public void Initialize()
        {
            AnalyseStructure(false);
        }
        /// <summary>
        /// 
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) { AnalyseStructure(true); }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="draw"></param>
        private void AnalyseStructure(bool draw)
        {
            if (m_position == Position.Right) { m_section = 1; } else { m_section = -1; }
            m_rootChord = transform.localScale.z;
            m_span = transform.localScale.x;
            Vector3 m_rootCenter = transform.position;

            float _sweep = 0;
            if (m_sweepDirection == Sweep.Forward) { _sweep = m_sweep; }
            if (m_sweepDirection == Sweep.Backward) { _sweep = -m_sweep; }

            float m_kTheta = Mathf.Tan(_sweep * Mathf.Deg2Rad);
            float m_sweepDistance = m_span * m_kTheta;
            Vector3 m_tipCenter = m_rootCenter
                            + (m_span * m_section * transform.right)
                            + (transform.forward * m_sweepDistance);
            float m_tipChord = m_rootChord * ((100 - m_taper) / 100);
            Vector3 m_leading_root = m_rootCenter + (0.5f * m_rootChord * transform.forward);
            Vector3 m_trailing_root = m_rootCenter - (0.5f * m_rootChord * transform.forward);
            Vector3 m_leading_tip = m_tipCenter + (0.5f * m_tipChord * transform.forward);
            Vector3 m_trailing_tip = m_tipCenter - (0.5f * m_tipChord * transform.forward);
            m_area = MathBase.EstimatePanelSectionArea(m_leading_root, m_leading_tip, m_trailing_root, m_trailing_tip);

            // ------------------------------------------------------------------ Collider
            m_collider = transform.GetComponent<BoxCollider>();
            if (m_collider == null) { gameObject.AddComponent<BoxCollider>(); }
            float thickness = 0.15f;
            transform.localScale = new Vector3(transform.localScale.x, thickness * m_rootChord, transform.localScale.z);
            if (m_collider != null) { m_collider.center = new Vector3(m_section * 0.5f, 0, 0); }

            #region Draw Gizmos
#if UNITY_EDITOR
            if (draw)
            {
                Handles.color = Color.yellow;
                Handles.DrawDottedLine(m_rootCenter, m_tipCenter, 4f);
                Gizmos.color = Color.red; Gizmos.DrawLine(m_trailing_tip, m_trailing_root);
                Gizmos.color = Color.yellow; Gizmos.DrawLine(m_leading_tip, m_leading_root);
                if (m_airfoil == null)
                {
                    Gizmos.color = Color.yellow; Gizmos.DrawLine(m_leading_root, m_trailing_root);
                    Gizmos.color = Color.yellow; Gizmos.DrawLine(m_leading_tip, m_trailing_tip);
                }
                else
                {
                    RotorDesign.PlotAirfoil(m_leading_root, m_trailing_root, transform.up, m_airfoil, out float rootArea);
                    RotorDesign.PlotAirfoil(m_leading_tip, m_trailing_tip, transform.up, m_airfoil, out float tipArea);
                }
            }
#endif
            #endregion
        }
        /// <summary>
        /// 
        /// </summary>
        private void AnalyseForces()
        {
            localVelocity = transform.InverseTransformDirection(m_controller.m_rigidbody.GetPointVelocity(transform.position));
            Vector m_localVelocity = Transformation.UnityToVector(localVelocity);
            ub = m_localVelocity.x;
            vb = m_localVelocity.y;
            wb = m_localVelocity.z;
            if (double.IsNaN(ub) || double.IsInfinity(ub)) { ub = 0.0; }
            if (double.IsNaN(vb) || double.IsInfinity(vb)) { vb = 0.0; }
            if (double.IsNaN(wb) || double.IsInfinity(wb)) { wb = 0.0; }

            if (m_type == Type.Sponson) { us = ub; vs = vb; ws = wb; }
            if (m_type == Type.Horizontal) { us = ub; vs = vb; ws = wb + ѡi; }
            if (m_type == Type.Vertical) { us = ub; vs = vb + (νi * Kv); ws = wb + ѡi; }

            double Vf = Math.Sqrt((us * us) + (vs * vs) + (ws * ws));
            if (m_type == Type.Horizontal) { αrad = Math.Atan2(ws, us) + iw; βrad = Math.Asin(vs / Vf); }
            else { αrad = Math.Atan2(ws, us); βrad = Math.Asin(vs / Vf); }
            if (double.IsNaN(αrad) || double.IsInfinity(αrad)) { αrad = 0.0; }
            if (double.IsNaN(βrad) || double.IsInfinity(βrad)) { βrad = 0.0; }

            α = αrad * Mathf.Rad2Deg;
            β = βrad * Mathf.Rad2Deg;
            if (double.IsNaN(α) || double.IsInfinity(α)) { α = 0.0; }
            if (double.IsNaN(β) || double.IsInfinity(β)) { β = 0.0; }

            if (m_airfoil != null)
            {
                CL = m_airfoil.liftCurve.Evaluate((float)α);
                CD = m_airfoil.dragCurve.Evaluate((float)α);
            }
            else { CL = 5.73 * αrad; CD = 0.02; }

            // Calculate lift/drag.
            m_lift = Vf * Vf * CL * m_area;
            m_drag = Vf * Vf * CD * m_area;
            if (double.IsNaN(m_lift) || double.IsInfinity(m_lift)) { m_lift = 0.0; }
            if (double.IsNaN(m_drag) || double.IsInfinity(m_drag)) { m_drag = 0.0; }

            // Lift is always perpendicular to air flow.
            Vector3 liftDirection = Vector3.Cross(m_controller.m_rigidbody.linearVelocity, transform.right).normalized;
            Vector3 lift = liftDirection * (float)m_lift;
            if (lift.magnitude > 0.5) { m_controller.force += lift; m_controller.m_rigidbody.AddForceAtPosition(lift, transform.position, ForceMode.Force); }

            // Drag is always opposite of the velocity.
            Vector3 drag = -m_controller.m_rigidbody.linearVelocity.normalized * (float)m_drag;
            if (drag.magnitude > 0.5) { m_controller.force += drag; m_controller.m_rigidbody.AddForceAtPosition(drag, transform.position, ForceMode.Force); }
        }
    }




    #endregion

    #region Editor

#if UNITY_EDITOR

    [CanEditMultipleObjects]
    [CustomEditor(typeof(SilantroStabilizer))]
    public class StabilizerEditor : Editor
    {
        Color backgroundColor;
        Color silantroColor = new Color(1.0f, 0.40f, 0f);
        Color fuelColor = Color.white;
        SilantroStabilizer stabilizer;


        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        private void OnEnable() { stabilizer = (SilantroStabilizer)target; }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------
        public override void OnInspectorGUI()
        {
            backgroundColor = GUI.backgroundColor;
            //DrawDefaultInspector ();
            serializedObject.Update();


            GUILayout.Space(3f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Stabilizer Configuration", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_type"), new GUIContent("Type"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_position"), new GUIContent("Position"));

            GUILayout.Space(8f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Properties", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_taper"), new GUIContent("Taper"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sweepDirection"), new GUIContent("Sweep"));
            if (stabilizer.m_sweepDirection != SilantroStabilizer.Sweep.None)
            {
                GUILayout.Space(3f);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sweep"), new GUIContent(" "));
            }
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_airfoil"), new GUIContent("Airfoil"));
            GUILayout.Space(3f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Kv"), new GUIContent("Interference Factor"));


            GUILayout.Space(8f);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Dimensions", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Root Chord", stabilizer.m_rootChord.ToString("0.000") + " m");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Span", stabilizer.m_span.ToString("0.000") + " m");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Area", stabilizer.m_area.ToString("0.000") + " m2");

            GUILayout.Space(10f);
            GUI.color = silantroColor;
            EditorGUILayout.HelpBox("Output", MessageType.None);
            GUI.color = backgroundColor;
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("α", stabilizer.α.ToString("0.000") + " °");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("β", stabilizer.β.ToString("0.000") + " °");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Lift", stabilizer.m_lift.ToString("0.000") + " N");
            GUILayout.Space(3f);
            EditorGUILayout.LabelField("Drag", stabilizer.m_drag.ToString("0.000") + " N");

            serializedObject.ApplyModifiedProperties();
        }
    }


#endif

    #endregion
}
