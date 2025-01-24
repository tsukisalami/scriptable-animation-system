using UnityEngine;
using Oyedoyin.Mathematics;

/// <summary>
/// 
/// </summary>
namespace Oyedoyin.Analysis
{
    /// <summary>
    /// 
    /// </summary>
    public class RMath
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="maximumClimbRate"></param>
        public static void PlotClimbCorrection(out AnimationCurve curve, float maximumClimbRate)
        {
            curve = new AnimationCurve();

            curve.AddKey(new Keyframe(-0.909f * maximumClimbRate, 1 / 0.649f));
            curve.AddKey(new Keyframe(-0.636f * maximumClimbRate, 1 / 0.707f));
            curve.AddKey(new Keyframe(-0.455f * maximumClimbRate, 1 / 0.773f));
            curve.AddKey(new Keyframe(-0.273f * maximumClimbRate, 1 / 0.847f));
            curve.AddKey(new Keyframe(0f, 1));
            curve.AddKey(new Keyframe(0.273f * maximumClimbRate, 1 / 1.751f));
            curve.AddKey(new Keyframe(0.455f * maximumClimbRate, 1 / 2.333f));
            curve.AddKey(new Keyframe(0.636f * maximumClimbRate, 1 / 3.492f));
            curve.AddKey(new Keyframe(0.818f * maximumClimbRate, 1 / 6.943f));
            curve.AddKey(new Keyframe(1.000f * maximumClimbRate, 0f));
            MathBase.LinearizeCurve(curve);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="curve"></param>
        public static void PlotInflowCorrection(out AnimationCurve curve)
        {
            curve = new AnimationCurve();

            curve.AddKey(new Keyframe(0f, 0.00632f));
            curve.AddKey(new Keyframe(5f, 0.00653f));
            curve.AddKey(new Keyframe(10f, 0.00703f));
            curve.AddKey(new Keyframe(15f, 0.00760f));
            curve.AddKey(new Keyframe(20f, 0.00813f));
            curve.AddKey(new Keyframe(25f, 0.00852f));
            curve.AddKey(new Keyframe(30f, 0.00880f));
            curve.AddKey(new Keyframe(35f, 0.00903f));
            curve.AddKey(new Keyframe(40f, 0.00912f));
            curve.AddKey(new Keyframe(45f, 0.00932f));
            curve.AddKey(new Keyframe(50f, 0.00942f));
            curve.AddKey(new Keyframe(55f, 0.00950f));
            curve.AddKey(new Keyframe(60f, 0.00958f));
            curve.AddKey(new Keyframe(65f, 0.00963f));
            curve.AddKey(new Keyframe(70f, 0.00969f));
            curve.AddKey(new Keyframe(75f, 0.00973f));
            curve.AddKey(new Keyframe(80f, 0.00977f));
            curve.AddKey(new Keyframe(85f, 0.00980f));
            curve.AddKey(new Keyframe(90f, 0.00983f));
            curve.AddKey(new Keyframe(95f, 0.00986f));
            curve.AddKey(new Keyframe(100f, 0.00988f));
            MathBase.LinearizeCurve(curve);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="curve"></param>
        public static void PlotGroundCorrection(out AnimationCurve curve)
        {
            curve = new AnimationCurve();

            curve.AddKey(new Keyframe(0.2402f, 1.3395f));
            curve.AddKey(new Keyframe(0.3672f, 1.2644f));
            curve.AddKey(new Keyframe(0.4908f, 1.2088f));
            curve.AddKey(new Keyframe(0.6174f, 1.1706f));
            curve.AddKey(new Keyframe(0.7343f, 1.1386f));
            curve.AddKey(new Keyframe(0.8576f, 1.1143f));
            curve.AddKey(new Keyframe(1.1657f, 1.0747f));
            curve.AddKey(new Keyframe(1.4769f, 1.047f));
            curve.AddKey(new Keyframe(1.7752f, 1.0282f));
            curve.AddKey(new Keyframe(2.0766f, 1.0144f));
            curve.AddKey(new Keyframe(2.6954f, 1.0097f));
            curve.AddKey(new Keyframe(3.0000f, 1.0000f));
            MathBase.LinearizeCurve(curve);
        }

        private const int maximumIterations = 15;
        private const double ԑH = 0.0005;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="µ"></param>
        /// <param name="λh"></param>
        /// <param name="λc"></param>
        /// <param name="λ0"></param>
        /// <returns></returns>
        public static FComplex ForwardInflow(FComplex µ, FComplex λh, FComplex λc, FComplex λ0)
        {
            // Control Factors
            bool finished = false; FComplex λ = λ0;
            FComplex αTPP = FComplex.Atan(λc / (µ + 0.00001f));
            int iterationCount = 0;
            // Iteration
            while (!finished)
            {
                FComplex zero = λ - µ * FComplex.Tan(αTPP) - (λh * λh) / (FComplex.Sqrt((µ * µ) + (λ * λ)));
                if (zero.m_real < 0.0001) { finished = true; } else { λ -= zero / 2; iterationCount++; }
                if (iterationCount >= maximumIterations) { finished = true; }
            }
            return λ;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="CT"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static FComplex SwirlFactor(FComplex CT, FComplex x)
        {
            FComplex swirl = ((FComplex.Pow((1 - FComplex.Sqrt(1 - ((2 * CT) / (x * x)))), 2) * FComplex.Pow(x, 3)) / CT);
            return swirl;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="swirlCorrection"></param>
        /// <param name="powerCorrection"></param>
        /// <param name="thrustCorrection"></param>
        public static void DrawCorrectionCurves(out AnimationCurve swirlCorrection, out AnimationCurve powerCorrection, out AnimationCurve thrustCorrection)
        {
            swirlCorrection = new AnimationCurve();
            powerCorrection = new AnimationCurve();
            thrustCorrection = new AnimationCurve();

            //-----------------------------------SWIRL
            Keyframe a1 = new Keyframe(0.005f, 0.013f);
            Keyframe b1 = new Keyframe(0.010f, 0.025f);
            Keyframe c1 = new Keyframe(0.015f, 0.037f);
            Keyframe d1 = new Keyframe(0.020f, 0.049f);
            Keyframe e1 = new Keyframe(0.025f, 0.057f);
            Keyframe f1 = new Keyframe(0.030f, 0.063f);
            Keyframe g1 = new Keyframe(0.035f, 0.075f);
            Keyframe h1 = new Keyframe(0.040f, 0.086f);
            Keyframe i1 = new Keyframe(0.045f, 0.093f);
            Keyframe j1 = new Keyframe(0.050f, 0.11f);
            //PLOT
            swirlCorrection.AddKey(a1); swirlCorrection.AddKey(b1); swirlCorrection.AddKey(c1); swirlCorrection.AddKey(d1); swirlCorrection.AddKey(e1); swirlCorrection.AddKey(f1);
            swirlCorrection.AddKey(g1); swirlCorrection.AddKey(h1); swirlCorrection.AddKey(i1); swirlCorrection.AddKey(j1);
            MathBase.LinearizeCurve(swirlCorrection);


            //----------------------------------POWER
            Keyframe a2 = new Keyframe(0.3f, 1.010f);
            Keyframe b2 = new Keyframe(0.4f, 1.022f);
            Keyframe c2 = new Keyframe(0.5f, 1.040f);
            Keyframe d2 = new Keyframe(0.6f, 1.043f);
            Keyframe e2 = new Keyframe(0.7f, 1.058f);
            Keyframe f2 = new Keyframe(0.8f, 1.061f);
            Keyframe g2 = new Keyframe(0.9f, 1.064f);
            Keyframe h2 = new Keyframe(1.0f, 1.068f);
            Keyframe i2 = new Keyframe(1.1f, 1.073f);
            Keyframe j2 = new Keyframe(1.2f, 1.080f);
            //PLOT
            powerCorrection.AddKey(a2); powerCorrection.AddKey(b2); powerCorrection.AddKey(c2); powerCorrection.AddKey(d2); powerCorrection.AddKey(e2);
            powerCorrection.AddKey(f2); powerCorrection.AddKey(g2); powerCorrection.AddKey(h2); powerCorrection.AddKey(i2); powerCorrection.AddKey(j2);
            MathBase.LinearizeCurve(powerCorrection);




            //----------------------------------THRUST
            Keyframe j3 = new Keyframe(-0.06f, 1.340f);
            Keyframe k3 = new Keyframe(-0.05f, 1.286f);
            Keyframe l3 = new Keyframe(-0.04f, 1.232f);
            Keyframe m3 = new Keyframe(-0.03f, 1.178f);
            Keyframe n3 = new Keyframe(-0.02f, 1.121f);
            Keyframe o3 = new Keyframe(-0.01f, 1.067f);

            Keyframe a3 = new Keyframe(0.00f, 1.000f);

            Keyframe b3 = new Keyframe(0.01f, 0.9472f);
            Keyframe c3 = new Keyframe(0.02f, 0.8910f);
            Keyframe d3 = new Keyframe(0.03f, 0.7750f);
            Keyframe e3 = new Keyframe(0.04f, 0.7170f);
            Keyframe f3 = new Keyframe(0.05f, 0.6850f);
            Keyframe g3 = new Keyframe(0.06f, 0.6210f);
            Keyframe h3 = new Keyframe(0.07f, 0.5680f);
            Keyframe i3 = new Keyframe(0.08f, 0.5104f);

            thrustCorrection.AddKey(j3);
            thrustCorrection.AddKey(k3);
            thrustCorrection.AddKey(l3);
            thrustCorrection.AddKey(m3);
            thrustCorrection.AddKey(n3);
            thrustCorrection.AddKey(o3);
            thrustCorrection.AddKey(a3);
            thrustCorrection.AddKey(b3);
            thrustCorrection.AddKey(c3);
            thrustCorrection.AddKey(d3);
            thrustCorrection.AddKey(e3);
            thrustCorrection.AddKey(f3);
            thrustCorrection.AddKey(g3);
            thrustCorrection.AddKey(h3);
            thrustCorrection.AddKey(i3);
            MathBase.LinearizeCurve(thrustCorrection);
        }
    }
}
