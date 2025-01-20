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
        public float OnePowerFOV = 36f;        // Field of view at 1x magnification
        public float Magnification = 1f;       // Current magnification level
        public float MagnificationMin = 1f;    // Minimum magnification level
        public float MagnificationMax = 6f;    // Maximum magnification level

        [Tooltip("FFP (first focal plane) scopes have the reticle change size with magnification")]
        public bool UseFFP = true;

        [Header("Input Settings")]
        public float CurrentMagnification = 0; // Current magnification value (0-1 range)
        public float MagnificationSensitivity = .1f;  // Sensitivity of magnification change
        public float SmoothTime = 5;           // Smoothing time for magnification changes
        public float MinPowerSensitivity = .2f;   // Sensitivity at minimum magnification
        public float MaxPowerSensitivity = .04f;  // Sensitivity at maximum magnification
        
        public float CurrentSensitivity;       // Current sensitivity based on magnification

        private float _targetMagnification;    // Target magnification to smoothly transition to
        private Material _scopeMaterial;       // Material used for the scope lens
        private int ReticleZoom = Shader.PropertyToID("_Reticle_Zoom");  // Shader property for reticle zoom

        private PlayerControls _playerControls;

        private void Awake()
        {
            // Initialize scope material and magnification
            _scopeMaterial = LensRenderer.sharedMaterial;
            _targetMagnification = CurrentMagnification;
            UpdateCurrentSensitivity();

            // Initialize player controls
            _playerControls = new PlayerControls();
        }
        
        private void OnEnable()
        {
            _playerControls.Enable();
            _playerControls.Gameplay.ChangeMagnification.performed += OnChangeMagnification;
            _playerControls.Gameplay.ToggleMagnification.performed += OnToggleMagnification;
        }

        private void OnDisable()
        {
            _playerControls.Gameplay.ChangeMagnification.performed -= OnChangeMagnification;
            _playerControls.Gameplay.ToggleMagnification.performed -= OnToggleMagnification;
            _playerControls.Disable();
        }

        private void Update()
        {
            UpdateMagnification();

            // Update camera FOV and reticle zoom
            DualRenderCamera.fieldOfView = OnePowerFOV / Magnification;
            _scopeMaterial.SetFloat(ReticleZoom, UseFFP ? Magnification : 1);
        }

        private void OnChangeMagnification(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            Vector2 scrollValue = context.ReadValue<Vector2>();
            float scrollDelta = Mathf.Sign(scrollValue.y); // Use sign to determine direction

            // Adjust target magnification based on scroll input
            _targetMagnification += scrollDelta * MagnificationSensitivity;
            _targetMagnification = Mathf.Clamp01(_targetMagnification);
            UpdateCurrentSensitivity();
        }

        private void OnToggleMagnification(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            Debug.Log("ToggleMagnification called");
            float halfMagnification = (MagnificationMin + MagnificationMax) / 2f;
            
            if (Magnification >= halfMagnification)
            {
                //SetMagnification01(MagnificationMin); // Set to minimum magnification
                Magnification = Mathf.Lerp(MagnificationMin, MagnificationMax, 0);
                Debug.Log("Set to minimum magnification");
            }
            else
            {
                //SetMagnification01(MagnificationMax); // Set to maximum magnification
                Magnification = Mathf.Lerp(MagnificationMin, MagnificationMax, 1);
                Debug.Log("Set to maximum magnification");
            }
        }

        private void UpdateMagnification()
        {
            // Smoothly transition current magnification to target
            CurrentMagnification = Mathf.Lerp(CurrentMagnification, _targetMagnification, Time.deltaTime * SmoothTime);
            Magnification = Mathf.Lerp(MagnificationMin, MagnificationMax, CurrentMagnification);
        }

        private void UpdateCurrentSensitivity()
        {
            // Linear interpolation between MinPowerSensitivity and MaxPowerSensitivity
            CurrentSensitivity = Mathf.Lerp(MinPowerSensitivity, MaxPowerSensitivity, CurrentMagnification);
        }

        public void SetMagnification01(float value01)
        {
            // Set magnification directly using a 0-1 value
            value01 = Mathf.Clamp01(value01);
            Magnification = Mathf.Lerp(MagnificationMin, MagnificationMax, value01);
        }

        public float Remap(float CurrentVal, float oldMin, float oldMax, float newMin, float newMax)
        {
            // Utility function to remap a value from one range to another
            return (CurrentVal - oldMin) / (oldMax - oldMin) * (newMax - newMin) + newMin;
        }
    }
}