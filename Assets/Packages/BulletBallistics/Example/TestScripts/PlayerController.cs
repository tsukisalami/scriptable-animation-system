using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ballistics
{
    // very basic player controller
    // just for testing purposes. Please do not use this in your projects, it's bad.. :)
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float MoveSpeed;
        public float TurnSpeed;
        public float JumpForce;

        [Header("Weapons")]
        public List<WeaponData> Weapons;
        public Transform HandTransform;
        public float SpreadAngleWalking;

        [Header("Recoil")]
        public Vector3 RecoilHandMovement;
        public Vector3 RecoilHandRotation;
        public float RecoilCorrectionTime;

        public int CurrentWeaponIndex { get; private set; } = 0;

        public CinematicBulletFollower Follower;

        private Transform myTransform;
        private Camera myCamera;
        private CharacterController controller;
        private Vector3 defaultHandPosition;
        private float defaultFov;
        private float defaultFixedDeltaTime;
        private float recoilAmount;
        private bool aiming;
        public bool CinematicMode { get; private set; } = false;

        void Start()
        {
            myTransform = transform;
            controller = GetComponent<CharacterController>();
            myCamera = Camera.main;
            defaultFov = myCamera.fieldOfView;
            defaultHandPosition = HandTransform.localPosition;
            defaultFixedDeltaTime = Time.fixedDeltaTime;
            foreach (var weapon in Weapons)
                weapon.Controller.Weapon.OnShoot.AddListener(OnShoot);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return)) {
                Debug.LogError("Break");
                Debug.Break();
            }
            if (Input.GetMouseButtonDown(0)) {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            // scene reload
            if (Input.GetKeyDown(KeyCode.O)) {
                Core.PrepareForSceneChange();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            if (Input.GetKeyDown(KeyCode.C)) {
                CinematicMode = !CinematicMode;
                if (!CinematicMode) {
                    Follower.Stop();
                    Follower.StopAllCoroutines();
                    Follower.Camera.enabled = false;
                }
            }

            Move();
            SwitchWeapons();
            HandleWeapon();

            // slow-motion
            var slowmotion = Input.GetKey(KeyCode.LeftShift);
            Time.timeScale = Mathf.Lerp(Time.timeScale, slowmotion ? 0.02f : 1, Time.unscaledDeltaTime * 30);
            Time.fixedDeltaTime = Mathf.Lerp(Time.fixedDeltaTime, defaultFixedDeltaTime * (slowmotion ? 0.02f : 1), Time.unscaledDeltaTime * 30);
        }

        void HandleWeapon()
        {
            // zeroing
            if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.E))
                Weapons[CurrentWeaponIndex].Controller.CurrentZeroingIndex++;
            if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Q))
                Weapons[CurrentWeaponIndex].Controller.CurrentZeroingIndex--;

            // spread
            var spreadController = Weapons[CurrentWeaponIndex].Controller.SpreadController as DefaultSpreadController;
            if (spreadController != null)
                spreadController.SetBaseSpread(Mathf.Clamp01(controller.velocity.magnitude) * SpreadAngleWalking, aiming ? .25f : 1f);

            // aim/ recoil
            aiming = Input.GetButton("Fire2");
            var scopePos = Weapons[CurrentWeaponIndex].ScopePos.localPosition;
            var defaultPos = aiming ? new Vector3(-scopePos.x, -scopePos.y, -scopePos.z) : defaultHandPosition;
            HandTransform.localPosition = Vector3.Lerp(defaultPos, defaultPos + RecoilHandMovement, recoilAmount * (aiming ? .1f : 1f));
            recoilAmount = Mathf.Clamp01(recoilAmount - (Time.deltaTime / RecoilCorrectionTime));
            myCamera.fieldOfView = Mathf.Lerp(myCamera.fieldOfView, defaultFov * (aiming ? 0.35f : 1f), Time.deltaTime * 15);

            // set trigger
            if (CinematicMode) {
                if (Input.GetButtonDown("Fire1"))
                    Follower.Shoot(Weapons[CurrentWeaponIndex].Controller.Weapon, Weapons[CurrentWeaponIndex].Controller.CurrentZeroing().Angle);
            } else {
                Weapons[CurrentWeaponIndex].Controller.SetTrigger(Input.GetButton("Fire1"));
            }

            if (Input.GetKeyDown(KeyCode.R))
                StartCoroutine(Reload(CurrentWeaponIndex));
        }

        private void SwitchWeapons()
        {
            if (Input.mouseScrollDelta.y != 0) {
                Weapons[CurrentWeaponIndex].Controller.SetTrigger(false);
                CurrentWeaponIndex = Util.ClampRepeating(CurrentWeaponIndex + (int)Mathf.Sign(Input.mouseScrollDelta.y), Weapons.Count);
                for (int i = 0; i < Weapons.Count; i++)
                    Weapons[i].Controller.gameObject.SetActive(i == CurrentWeaponIndex);
            }
        }

        private IEnumerator Reload(int weaponId)
        {
            yield return new WaitForSeconds(Weapons[weaponId].ReloadTime);
            if (weaponId == CurrentWeaponIndex)
                (Weapons[CurrentWeaponIndex].Controller.MagazineController as DefaultMagazineController)?.Reload();
        }

        private void Move()
        {
            var rot = new Vector2(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X")) * TurnSpeed * Time.timeScale;
            myCamera.transform.Rotate(rot.x, 0, 0);
            myTransform.Rotate(0, rot.y, 0);

            var keyInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * MoveSpeed;
            var yVel = !controller.isGrounded ? controller.velocity.y + Physics.gravity.y * Time.deltaTime : -.01f;
            if (Input.GetButton("Jump") && controller.isGrounded)
                yVel = JumpForce;
            controller.Move((myTransform.TransformDirection(keyInput) + Vector3.up * yVel) * Time.deltaTime);
        }

        private void OnShoot()
        {
            recoilAmount = Mathf.Clamp01(recoilAmount + Weapons[CurrentWeaponIndex].RecoilAmount);
            Weapons[CurrentWeaponIndex].Particle?.Play();
            Weapons[CurrentWeaponIndex].Audio?.Play();
        }

        private void OnTriggerEnter(Collider col)
        {
            if (col.tag == "supply") {
                for (int i = 0; i < Weapons.Count; i++)
                    (Weapons[CurrentWeaponIndex].Controller.MagazineController as DefaultMagazineController)?.Initialize();
            }
        }
    }

    [System.Serializable]
    public struct WeaponData
    {
        public WeaponController Controller;
        public AudioSource Audio;
        public ParticleSystem Particle;
        public Transform ScopePos;
        public float ReloadTime;
        public float RecoilAmount;
    }
}

