using System;
using UnityEngine;
using Oyedoyin.Common;
using Oyedoyin.Mathematics;
using Oyedoyin.Common.Misc;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Oyedoyin.RotaryWing
{
    /// <summary>
    /// Handles the rotor design and debug draw
    /// </summary>
    public class RotorDesign
    {
        /// <summary>
        /// Handles the rotor design and debug draw
        /// </summary>

        public static void AnalyseRotorShape(SilantroRotor rotor)
        {
            float ψ = 360 / rotor.Nb;
            if (rotor.twistType == SilantroRotor.TwistType.Upward) { rotor.θtw = rotor.bladeWashout; }
            else if (rotor.twistType == SilantroRotor.TwistType.None) { rotor.θtw = 0f; }
            else { rotor.θtw = -rotor.bladeWashout; }
            rotor.rootcut = (rotor.rotorRadius * rotor.rootCutOut) - rotor.rotorHeadRadius;
            rotor.bladeRadius = ((1 - rotor.rootCutOut) * rotor.rotorRadius) + rotor.rootcut;
            rotor.hingeOffset = rotor.re * rotor.bladeRadius;
            rotor.J = rotor.Nb * rotor.bladeMass * Mathf.Pow((rotor.bladeRadius * 0.55f), 2);
            float m = rotor.bladeMass / rotor.bladeRadius;
            rotor.Iβ = ((m * Mathf.Pow(rotor.bladeRadius, 3)) / 3) * Mathf.Pow((1 - rotor.re), 3);
            rotor.aspectRatio = ((rotor.bladeRadius * rotor.bladeRadius) / (rotor.bladeRadius * rotor.bladeChord));
            if (rotor.weightUnit == WeightUnit.Kilogram) { rotor.weightFactor = 1f; }
            if (rotor.weightUnit == WeightUnit.Pounds) { rotor.weightFactor = (1 / 2.205f); }
            rotor.actualWeight = rotor.bladeMass * rotor.weightFactor;

            rotor.skewDistance = (float)Math.Sin(rotor.coneAngle * Mathf.Deg2Rad) * rotor.rotorRadius * rotor.transform.up;
            for (int i = 0; i < rotor.Nb; i++)
            {
                float currentSector = ψ * (i + 1);
                Quaternion sectorRotation = Quaternion.AngleAxis(currentSector, rotor.transform.up);
                Vector3 sectorTipPosition = rotor.transform.position + (sectorRotation * (rotor.transform.forward * rotor.rotorRadius));
                Vector3 hingePosition = rotor.transform.position + (sectorRotation * (rotor.transform.forward * rotor.hingeOffset));
                sectorTipPosition += rotor.skewDistance;

                // ---------------------------------- Base Factors
                Vector3 bladeForward = sectorTipPosition - rotor.transform.position;
                Vector3 bladeRight = Vector3.Cross(bladeForward.normalized, rotor.transform.up.normalized);
                Vector3 bladeRootCenter = rotor.transform.position + (bladeForward * rotor.rootCutOut) + (bladeRight * rotor.rootDeviation);
                var supremeFactor = bladeRight * rotor.bladeChord * 0.5f;
                float featherAngle = (float)rotor.θom * Mathf.Rad2Deg;
                if (rotor.rotorDirection == RotationDirection.CW) { featherAngle *= -1; }
                sectorTipPosition += (bladeRight * rotor.rootDeviation);
                // ------------------------------------ Structure Points
                if (rotor.rotorDirection == RotationDirection.CCW)
                {
                    rotor.tipLeadingEdge = sectorTipPosition + supremeFactor;
                    rotor.rootLeadingEdge = bladeRootCenter + supremeFactor;
                    rotor.tipTrailingEdge = sectorTipPosition - supremeFactor;
                    rotor.rootTrailingEdge = bladeRootCenter - supremeFactor;
                }
                else
                {
                    rotor.tipLeadingEdge = sectorTipPosition - supremeFactor;
                    rotor.rootLeadingEdge = bladeRootCenter - supremeFactor;
                    rotor.tipTrailingEdge = sectorTipPosition + supremeFactor;
                    rotor.rootTrailingEdge = bladeRootCenter + supremeFactor;
                }

                // ---------------------------------- Blade Feathering
                rotor.rootDeflection = -featherAngle + (float)rotor.θtw;
                if (rotor.rotorDirection == RotationDirection.CCW) { rotor.rootDeflection = -featherAngle - (float)rotor.θtw; }
                Vector3 rootSkew = Quaternion.AngleAxis(-featherAngle, bladeForward) * (rotor.tipLeadingEdge - rotor.tipTrailingEdge) * 0.5f;
                Vector3 tipSkew = Quaternion.AngleAxis(rotor.rootDeflection, bladeForward) * (rotor.tipLeadingEdge - rotor.tipTrailingEdge) * 0.5f;
                rotor.tipTrailingEdge = sectorTipPosition - tipSkew; rotor.tipLeadingEdge = sectorTipPosition + tipSkew;
                rotor.rootTrailingEdge = bladeRootCenter - rootSkew; rotor.rootLeadingEdge = bladeRootCenter + rootSkew;
                rotor.quaterRootChordPoint = MathBase.EstimateSectionPosition(rotor.rootLeadingEdge, rotor.rootTrailingEdge, 0.25f);
                rotor.quaterTipChordPoint = MathBase.EstimateSectionPosition(rotor.tipLeadingEdge, rotor.tipTrailingEdge, 0.25f);



#if UNITY_EDITOR

                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(hingePosition, 0.05f);
                Gizmos.color = Color.cyan; Gizmos.DrawLine(hingePosition, hingePosition + (rotor.transform.up * 1f));

                //DRAW DISC
                Handles.color = Color.red;
                Handles.DrawWireDisc((rotor.transform.position + rotor.skewDistance), rotor.transform.up, (rotor.rotorRadius));
                Handles.color = Color.cyan;
                Handles.DrawWireDisc(rotor.transform.position, rotor.transform.up, (rotor.rotorHeadRadius));

                Gizmos.color = Color.red;
                Gizmos.DrawLine(rotor.tipTrailingEdge, rotor.rootTrailingEdge);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(rotor.tipLeadingEdge, rotor.rootLeadingEdge);
                Gizmos.DrawLine(rotor.transform.position, rotor.rootLeadingEdge); Gizmos.DrawLine(rotor.transform.position, rotor.rootTrailingEdge);

                Gizmos.color = Color.green;
                Gizmos.DrawSphere(rotor.transform.position, 0.1f);
                Gizmos.color = Color.green; Gizmos.DrawLine(rotor.transform.position, rotor.transform.position + (rotor.transform.up * 2f));

                if (rotor.drawFoils && rotor.m_rootAirfoil != null && rotor.m_tipAirfoil != null)
                {
                    float ta;
                    List<Vector3> tb;
                    PlotAirfoil(rotor.rootLeadingEdge, rotor.rootTrailingEdge, rotor.m_rootAirfoil, rotor.transform, out ta, out tb);
                    PlotAirfoil(rotor.tipLeadingEdge, rotor.tipTrailingEdge, rotor.m_tipAirfoil, rotor.transform, out ta, out tb);


                    // ---------------------------------- Draw Subdivisions
                    if (rotor.drawFoils && rotor.m_rootAirfoil != null && rotor.m_tipAirfoil != null)
                    {
                        for (int p = 1; p < rotor.subdivision; p++)
                        {
                            float currentSection = p; float sectionLength = rotor.subdivision; float sectionFactor = currentSection / sectionLength;
                            Vector3 LeadingPointA, TrailingPointA;
                            TrailingPointA = MathBase.EstimateSectionPosition(rotor.rootTrailingEdge, rotor.tipTrailingEdge, sectionFactor);
                            LeadingPointA = MathBase.EstimateSectionPosition(rotor.rootLeadingEdge, rotor.tipLeadingEdge, sectionFactor);
                            Gizmos.color = Color.yellow; Gizmos.DrawLine(LeadingPointA, TrailingPointA);
                            float yM = Vector3.Distance(rotor.rootTrailingEdge, TrailingPointA);
                            PlotRibAirfoil(LeadingPointA, TrailingPointA, yM, 0, Color.yellow, false, rotor.m_rootAirfoil, rotor.m_tipAirfoil, rotor.rotorRadius, rotor.transform);
                        }
                    }
                }
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(rotor.rootTrailingEdge, rotor.rootLeadingEdge);
                    Gizmos.DrawLine(rotor.tipLeadingEdge, rotor.tipTrailingEdge);
                }


                // ---------------------------------- Blade Helpers
                Handles.color = Color.yellow; Handles.DrawDottedLine(rotor.quaterRootChordPoint, rotor.quaterTipChordPoint, 4f);
                Vector3 tipDirection = (rotor.tipLeadingEdge - rotor.tipTrailingEdge); Quaternion tipRotation = Quaternion.LookRotation(tipDirection, rotor.transform.up);
                Handles.color = Color.green; Handles.ArrowHandleCap(0, rotor.tipLeadingEdge, rotor.transform.rotation * Quaternion.LookRotation(Vector3.up), 0.3f, EventType.Repaint);

                Handles.color = Color.yellow; Handles.ArrowHandleCap(0, rotor.tipLeadingEdge, tipRotation, 0.8f, EventType.Repaint);
                Vector3 rootDirection = (rotor.rootLeadingEdge - rotor.rootTrailingEdge); Quaternion rootRotation = Quaternion.LookRotation(rootDirection, rotor.transform.up);
                Handles.color = Color.red; Handles.ArrowHandleCap(0, rotor.rootLeadingEdge, rootRotation, 0.8f, EventType.Repaint);

#endif
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="leadingPoint"></param>
        /// <param name="trailingPoint"></param>
        /// <param name="m_up"></param>
        /// <param name="foil"></param>
        /// <param name="foilArea"></param>
        public static void PlotAirfoil(Vector3 leadingPoint, Vector3 trailingPoint, Vector3 m_up, SilantroAirfoil foil, out float foilArea)
        {
            List<Vector3> points = new List<Vector3>();
            float chordDistance = Vector3.Distance(leadingPoint, trailingPoint);
            Vector3 PointB = Vector3.zero;

            //FIND POINTS
            if (foil.x.Count > 0)
            {
                for (int j = 0; (j < foil.x.Count); j++)
                {
                    //BASE POINT
                    Vector3 XA = leadingPoint - ((leadingPoint - trailingPoint).normalized * (foil.x[j] * chordDistance));
                    Vector3 liftDirection = m_up.normalized;
                    Vector3 PointA = XA + (liftDirection * ((foil.y[j]) * chordDistance));
                    points.Add(PointA);
                    if ((j + 1) < foil.x.Count)
                    {
                        Vector3 XB = (leadingPoint - ((leadingPoint - trailingPoint).normalized * (foil.x[j + 1] * chordDistance)));
                        PointB = XB + (liftDirection.normalized * ((foil.y[j + 1]) * chordDistance));
                    }

                    //CONNECT
                    Gizmos.color = Color.white; Gizmos.DrawLine(PointA, PointB);
                }
            }


            //PERFORM CALCULATIONS
            for (int j = 0; (j < points.Count); j++)
            {
                Gizmos.DrawLine(points[j], points[(points.Count - j - 1)]);
            }
            foilArea = Mathf.Pow(chordDistance, 2f) * (((foil.xtc * 0.01f) + 3) / 6f) * (foil.tc * 0.01f);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="leadingPoint"></param>
        /// <param name="trailingPoint"></param>
        /// <param name="foil"></param>
        /// <param name="foilTransform"></param>
        /// <param name="foilArea"></param>
        /// <param name="points"></param>
        public static void PlotAirfoil(Vector3 leadingPoint, Vector3 trailingPoint, SilantroAirfoil foil, Transform foilTransform, out float foilArea, out List<Vector3> points)
        {
            points = new List<Vector3>(); List<float> xt = new List<float>(); float chordDistance = Vector3.Distance(leadingPoint, trailingPoint);
            Vector3 PointA = Vector3.zero, PointXA = Vector3.zero, PointXB = Vector3.zero, PointB = Vector3.zero;

            //FIND POINTS
            if (foil.x.Count > 0)
            {
                for (int j = 0; (j < foil.x.Count); j++)
                {
                    //BASE POINT
                    Vector3 XA = leadingPoint - ((leadingPoint - trailingPoint).normalized * (foil.x[j] * chordDistance)); Vector3 liftDirection = foilTransform.up.normalized;
                    PointA = XA + (liftDirection * ((foil.y[j]) * chordDistance)); points.Add(PointA); if ((j + 1) < foil.x.Count) { Vector3 XB = (leadingPoint - ((leadingPoint - trailingPoint).normalized * (foil.x[j + 1] * chordDistance))); PointB = XB + (liftDirection.normalized * ((foil.y[j + 1]) * chordDistance)); }
                    //CONNECT
                    Gizmos.color = Color.white; Gizmos.DrawLine(PointA, PointB);
                }
            }

            //PERFORM CALCULATIONS
            xt = new List<float>();
            for (int j = 0; (j < points.Count); j++) { xt.Add(Vector3.Distance(points[j], points[(points.Count - j - 1)])); Gizmos.DrawLine(points[j], points[(points.Count - j - 1)]); }
            foilArea = Mathf.Pow(chordDistance, 2f) * (((foil.xtc * 0.01f) + 3) / 6f) * (foil.tc * 0.01f);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="leadingPoint"></param>
        /// <param name="trailingPoint"></param>
        /// <param name="distance"></param>
        /// <param name="wingTip"></param>
        /// <param name="ribColor"></param>
        /// <param name="drawSplits"></param>
        /// <param name="rootAirfoil"></param>
        /// <param name="tipAirfoil"></param>
        /// <param name="span"></param>
        /// <param name="foilTransform"></param>
        public static void PlotRibAirfoil(Vector3 leadingPoint, Vector3 trailingPoint, float distance, float wingTip, Color ribColor, bool drawSplits, SilantroAirfoil rootAirfoil, SilantroAirfoil tipAirfoil, float span, Transform foilTransform)
        {
            List<Vector3> points = new List<Vector3>(); List<float> xt = new List<float>(); float chordDistance = Vector3.Distance(leadingPoint, trailingPoint);
            Vector3 PointA = Vector3.zero, PointXA = Vector3.zero, PointXB = Vector3.zero;
            //FIND POINTS
            if (rootAirfoil.x.Count > 0)
            {
                for (int j = 0; (j < rootAirfoil.x.Count); j++)
                {
                    float xi = MathBase.EstimateSection(rootAirfoil.x[j], tipAirfoil.x[j], distance / span);
                    float yi = MathBase.EstimateSection(rootAirfoil.y[j], tipAirfoil.y[j], distance / span);
                    //BASE POINT
                    Vector3 XA = leadingPoint - ((leadingPoint - trailingPoint).normalized * (xi * chordDistance)); Vector3 liftDirection = foilTransform.up; PointXA = XA + (liftDirection * yi * chordDistance); points.Add(PointXA);
                    if ((j + 1) < rootAirfoil.x.Count)
                    {
                        float xii = MathBase.EstimateSection(rootAirfoil.x[j + 1], tipAirfoil.x[j + 1], distance / span);
                        float yii = MathBase.EstimateSection(rootAirfoil.y[j + 1], tipAirfoil.y[j + 1], distance / span);
                        Vector3 XB = (leadingPoint - ((leadingPoint - trailingPoint).normalized * (xii * chordDistance))); PointXB = XB + (liftDirection.normalized * (yii * chordDistance));
                    }
                    //CONNECT
                    Gizmos.color = ribColor; Gizmos.DrawLine(PointXA, PointXB);
                }
            }
        }
    }
}
