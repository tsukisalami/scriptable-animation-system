using UnityEngine;

namespace UltimateScopes.Demo
{
    public class DemoController : MonoBehaviour
    {
        public float MoveSpeed = .1f;
        public float[] PositionOffsets;

        private Vector2 _yawPitch;
        private bool _viewingRedDot;
        private Transform _pivotTransform;
        private Transform _pivotTransform1;
        private float _forwardBack;
        private float _leftRight;
        private float _upDown;
        private int _currentSight;
        private void Awake()
        {
            _pivotTransform = transform.GetChild(0);
            _pivotTransform1 = _pivotTransform.GetChild(0);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                _currentSight = (_currentSight + 1) % PositionOffsets.Length;

            _yawPitch += Vector2.right * Input.GetAxis("Mouse X");
            _yawPitch -= Vector2.up * Input.GetAxis("Mouse Y");
            _yawPitch = new Vector2(Mathf.Clamp(_yawPitch.x, -30, 30), Mathf.Clamp(_yawPitch.y, -30, 30));

            transform.eulerAngles = new Vector3(_yawPitch.y, _yawPitch.x, 0);

            if (Input.GetKey(KeyCode.Q))
                _forwardBack += Time.deltaTime * MoveSpeed;
            if (Input.GetKey(KeyCode.E))
                _forwardBack -= Time.deltaTime * MoveSpeed;

            if (Input.GetKey(KeyCode.W))
                _upDown -= Time.deltaTime * MoveSpeed;
            if (Input.GetKey(KeyCode.S))
                _upDown += Time.deltaTime * MoveSpeed;

            if (Input.GetKey(KeyCode.A))
                _leftRight += Time.deltaTime * MoveSpeed;
            if (Input.GetKey(KeyCode.D))
                _leftRight -= Time.deltaTime * MoveSpeed;

            _forwardBack = Mathf.Clamp(_forwardBack, -.05f, .05f);
            _leftRight = Mathf.Clamp(_leftRight, -.05f, .05f);
            _upDown = Mathf.Clamp(_upDown, -.05f, .05f);

            _pivotTransform1.localPosition = Vector3.zero;
            _pivotTransform1.position += _pivotTransform1.forward * _forwardBack;
            _pivotTransform1.position += _pivotTransform1.right * _leftRight;
            _pivotTransform1.position += _pivotTransform1.up * _upDown;

            Vector3 targetPosition = Vector3.right * -PositionOffsets[_currentSight];
            _pivotTransform.localPosition = Vector3.MoveTowards(_pivotTransform.localPosition, targetPosition, Time.deltaTime);
            //if (_viewingRedDot)
            //    _pivotTransform.localPosition = Vector3.MoveTowards(_pivotTransform.localPosition, Vector3.right * -.1f, Time.deltaTime);
            //else
            //    _pivotTransform.localPosition = Vector3.MoveTowards(_pivotTransform.localPosition, Vector3.zero, Time.deltaTime);
        }
    }
}