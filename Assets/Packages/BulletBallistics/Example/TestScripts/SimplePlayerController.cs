using UnityEngine;

namespace Ballistics
{
    public class SimplePlayerController : MonoBehaviour
    {
        #region Movement
        [Header("Movement")]
        public Transform CameraTransform;
        public CharacterController controller;
        public float MoveSpeed;
        public float TurnSpeed;
        public float JumpForce;

        private void Move()
        {
            var rot = new Vector2(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X")) * TurnSpeed * Time.timeScale;
            CameraTransform.Rotate(rot.x, 0, 0);
            transform.Rotate(0, rot.y, 0);
            var keyInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * MoveSpeed;
            var yVel = !controller.isGrounded ? controller.velocity.y + Physics.gravity.y * Time.deltaTime : -.01f;
            if (Input.GetButton("Jump") && controller.isGrounded)
                yVel = JumpForce;
            controller.Move((transform.TransformDirection(keyInput) + Vector3.up * yVel) * Time.deltaTime);
        }

        private void LockCursor()
        {
            if (Input.GetMouseButtonDown(1)) {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        #endregion

        [Header("Weapon")]
        public Ballistics.Weapon Weapon;
        // public Ballistics.WeaponController WeaponController;

        void Update()
        {
            LockCursor();
            Move();

            if (Input.GetMouseButtonDown(0))
                Weapon.Shoot();
            // WeaponController.SetTrigger(Input.GetMouseButton(0));
        }
    }
}
