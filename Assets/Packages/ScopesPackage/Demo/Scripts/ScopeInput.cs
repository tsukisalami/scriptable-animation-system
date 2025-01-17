using UnityEngine;

namespace UltimateScopes.Demo
{
    public class ScopeInput : MonoBehaviour
    {
        public float CurrentMag = 0;
        public float Sens = .1f;
        public float SmoothTime = 5;

        private float _targetMag;
        private ScopeController controller;

        private void Awake()
        {
            TryGetComponent<ScopeController>(out controller);
            _targetMag = CurrentMag;
        }

        void Update()
        {
            //CurrentMag += Input.mouseScrollDelta.y * Sens;
            _targetMag += Input.mouseScrollDelta.y * Sens;
            _targetMag = Mathf.Clamp01(_targetMag);

            CurrentMag = Mathf.MoveTowards(CurrentMag, _targetMag, Time.deltaTime * SmoothTime);

            controller.SetMagnification01(CurrentMag);
        }
    }
}