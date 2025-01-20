using UnityEngine;

namespace UltimateScopes.Demo
{
    public class ScopeInput : MonoBehaviour
    {
        public float CurrentMagnification = 0;
        public float MagnificationSensitivity = .1f;
        public float SmoothTime = 5;

        public float MinPowerSensitivity = .2f;
        public float MaxPowerSensitivity = .04f;
        
        public float CurrentSensitivity;

        private float _targetMagnification;
        private ScopeController controller;


        private void Awake()
        {
            TryGetComponent<ScopeController>(out controller);

            _targetMagnification = CurrentMagnification;
            UpdateCurrentSensitivity();
        }

        void Update()
        {
            _targetMagnification += Input.mouseScrollDelta.y * MagnificationSensitivity;
            _targetMagnification = Mathf.Clamp01(_targetMagnification);

            CurrentMagnification = Mathf.MoveTowards(CurrentMagnification, _targetMagnification, Time.deltaTime * SmoothTime);

            UpdateCurrentSensitivity();

            controller.SetMagnification01(CurrentMagnification);
        }

        private void UpdateCurrentSensitivity()
        {
            // Linear interpolation between MinPowerSensitivity and MaxPowerSensitivity
            CurrentSensitivity = Mathf.Lerp(MinPowerSensitivity, MaxPowerSensitivity, CurrentMagnification);
        }
    }
}
