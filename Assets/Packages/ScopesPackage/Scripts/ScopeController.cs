using UnityEngine;

namespace UltimateScopes
{
    public class ScopeController : MonoBehaviour
    {
        [Header("Configuration")]
        public Renderer LensRenderer;
        [Tooltip("Leave Blank if using single render")]
        public Camera DualRenderCamera;


        [Header("Customization")]
        public float OnePowerFOV = 65;
        public float Magnification = 4;
        public float MagnificationMin = 1f;
        public float MagnificationMax = 6f;


        [Tooltip("FFP (first focal plane) scopes have the reticle change size with magnification")]
        public bool UseFFP = true;

        private Material _scopeMaterial;
        private int ReticleZoom = Shader.PropertyToID("_Reticle_Zoom");

        public float Remap(float CurrentVal, float oldMin, float oldMax, float newMin, float newMax)
        {
            return (CurrentVal - oldMin) / (oldMax - oldMin) * (newMax - newMin) + newMin;
        }

        private void Update()
        {
            DualRenderCamera.fieldOfView = OnePowerFOV / Magnification;
            LensRenderer.sharedMaterial.SetFloat(ReticleZoom, UseFFP ? Magnification : 1);
        }

        public void SetMagnification01(float value01)
        {
            value01 = Mathf.Clamp01(value01);
            Magnification = Mathf.Lerp(MagnificationMin, MagnificationMax, value01);
        }
    }
}