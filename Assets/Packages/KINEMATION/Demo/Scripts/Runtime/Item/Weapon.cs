// Designed by KINEMATION, 2024.

using System;
using KINEMATION.FPSAnimationFramework.Runtime.Camera;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.KAnimationCore.Runtime.Input;
using Ballistics;

using Demo.Scripts.Runtime.AttachmentSystem;

using System.Collections.Generic;
using Demo.Scripts.Runtime.Character;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using System.Collections;

namespace Demo.Scripts.Runtime.Item
{
    public enum OverlayType
    {
        Default,
        Pistol,
        Rifle
    }

    public enum WeaponState
    {
        Idle,               // Default state, weapon ready
        AimingDownSights,   // Player is aiming
        Reloading,         // Reloading in progress
        CheckingChamber,    // Checking if round is chambered
        CheckingMagazine,   // Checking remaining ammo
        JammedBolt,        // Bolt/slide is stuck
        FailureToFeed,     // Round failed to feed properly
        Misfire,           // Round failed to fire
        FixingMalfunction   // Player clearing a malfunction
    }

    public class Weapon : FPSItem
    {
        [Header("General")]
        [SerializeField] [Range(0f, 120f)] private float fieldOfView = 90f;

        [SerializeField] private Transform bolt; // Reference to the bolt GameObject
        [SerializeField] private Transform casingEjectPoint; // Reference to the eject point
        [SerializeField] private float ejectForce = 1.5f; // Force applied to the casing
        [SerializeField] private float casingLifetime = 5f; // Lifetime of the casing before it gets destroyed
        [SerializeField] private float casingTorque = 100f; 

        [Header("Visual Effects")]
        [SerializeField] private VisualEffect muzzleFlashVFX;
        [SerializeField] private VisualEffect muzzleSmokeVFX;
        [SerializeField] private VisualEffect chamberSmokeVFX;
        [SerializeField] private float effectDuration = 1.5f;


        [Header("Animations")]
        [SerializeField] private FPSAnimationAsset reloadClip;
        [SerializeField] private FPSCameraAnimation cameraReloadAnimation;

        [SerializeField] private FPSAnimationAsset grenadeClip;
        [SerializeField] private FPSCameraAnimation cameraGrenadeAnimation;
        [SerializeField] private OverlayType overlayType;

        [Header("Recoil")]
        [SerializeField] private FPSAnimatorProfile animatorProfile;
        [SerializeField] private RecoilAnimData recoilData;
        [SerializeField] private RecoilPatternSettings recoilPatternSettings;
        [SerializeField] private FPSCameraShake cameraShake;
        [Min(0f)] [SerializeField] private float fireRate;
        [SerializeField] private float crouchedRecoilMultiplier = 0.5f;

        [SerializeField] private bool isShotgun;
        [SerializeField] private bool supportsAuto;
        [SerializeField] private bool supportsBurst;
        [SerializeField] private int burstLength;

        [Space]
        [Header("Attachments")]
        [SerializeField]
        private AttachmentGroup<BaseAttachment> barrelAttachments = new AttachmentGroup<BaseAttachment>();

        [SerializeField]
        private AttachmentGroup<BaseAttachment> gripAttachments = new AttachmentGroup<BaseAttachment>();

        [SerializeField]
        private List<AttachmentGroup<ScopeAttachment>> scopeGroups = new List<AttachmentGroup<ScopeAttachment>>();

        [SerializeField]
        private AttachmentGroup<BaseAttachment> tacticalDevices = new AttachmentGroup<BaseAttachment>();

        //~ Controller references
        private FPSController _fpsController;
        private Animator _controllerAnimator;
        private UserInputController _userInputController;
        private IPlayablesController _playablesController;
        private FPSCameraController _fpsCameraController;

        private FPSAnimator _fpsAnimator;
        private FPSAnimatorEntity _fpsAnimatorEntity;

        private RecoilAnimation _recoilAnimation;
        private RecoilPattern _recoilPattern;

        //~ Controller references

        private Animator _weaponAnimator;
        private int _scopeIndex;

        private float _lastRecoilTime;
        private int _bursts;
        private FireMode _fireMode = FireMode.Semi;

        private static readonly int OverlayType = Animator.StringToHash("OverlayType");
        private static readonly int CurveEquip = Animator.StringToHash("CurveEquip");
        private static readonly int CurveUnequip = Animator.StringToHash("CurveUnequip");

        //~ Action references
        private MagazineManager _magazineManager; // Now private and not assigned via inspector
        private bool _refillMagazine = false;
        private bool _cycleBullet = false;
        private bool _fireWeapon = false;

        private Ballistics.Weapon ballisticsWeapon;
        private GameObject chamberedBulletModel;
        private BulletInfo chamberedBulletInfo;
        private WeaponSound weaponSound;
        private GameObject _casingModel;

        public SpreadController SpreadController;
        private int pelletsPerShot = 1;
        private bool canHoldBreath = true;

        private bool isAiming;
        private bool isCrouched;

        // Current state of the weapon
        [SerializeField] private WeaponState currentState = WeaponState.Idle;
        
        // Malfunction chances (0-1 range)
        [Header("Malfunction Parameters")]
        [Range(0f, 1f)] public float jamChance = 0.001f;
        [Range(0f, 1f)] public float feedFailureChance = 0.002f;
        [Range(0f, 1f)] public float misfireChance = 0.001f;
        
        // State durations
        [Header("State Durations")]
        public float adsTime = 0.3f;
        public float reloadTime = 2.0f;
        public float checkChamberTime = 1.0f;
        public float checkMagazineTime = 1.5f;
        public float malfunctionFixTime = 3.0f;

        // Properties
        public WeaponState CurrentState => currentState;
        public bool IsOperational => currentState == WeaponState.Idle || currentState == WeaponState.AimingDownSights;

        // Add property to check if weapon is currently firing
        private bool IsFiring => _fireWeapon;  // Assuming _fireWeapon is your existing firing flag

        private void Start()
        {
            AssignManagers();
            AssignBallisticsWeapon();
            //UpdateMuzzleSystems();
            chamberedBulletInfo = null;
            CycleBullet(); // Load the chamber at start
        }

        private void AssignManagers()
        {   
            weaponSound = GetComponent<WeaponSound>();
            if (weaponSound == null)
            {
                Debug.LogError("No WeaponSound assigned.");
            }
            _magazineManager = GetComponentInChildren<MagazineManager>();
            if (_magazineManager == null)
            {
                Debug.LogError("No MagazineManager found in children.");
            }
        }

        private void AssignBallisticsWeapon()
        {
            ballisticsWeapon = GetComponent<Ballistics.Weapon>();
            if (ballisticsWeapon == null)
            {
                Debug.LogError("No BallisticsWeapon assigned.");
            }
        }

        /*private void UpdateMuzzleSystems()
        {
            _muzzleSmoke = GetComponentInChildren<MuzzleSmoke>();
            if (_muzzleSmoke == null)
            {
                Debug.LogError("No MuzzleSmoke assigned.");
            }
        }
        */

        private void OnActionEnded()
        {
            if (_fpsController == null) return;
            _fpsController.ResetActionState();
        }

        protected void UpdateTargetFOV(bool isAiming)
        {
            float fov = fieldOfView;
            float sensitivityMultiplier = 1f;

            if (isAiming && scopeGroups.Count != 0)
            {
                var scope = scopeGroups[_scopeIndex].GetActiveAttachment();
                fov *= scope.aimFovZoom;

                sensitivityMultiplier = scopeGroups[_scopeIndex].GetActiveAttachment().sensitivityMultiplier;
            }

            _userInputController.SetValue("SensitivityMultiplier", sensitivityMultiplier);
            _fpsCameraController.UpdateTargetFOV(fov);
        }

        public void UpdateSensitivityMultiplier(bool isAiming)
        {
            float sensitivityMultiplier = 1f;

            if (isAiming && scopeGroups.Count != 0)
            {
                var scope = scopeGroups[_scopeIndex].GetActiveAttachment();

                sensitivityMultiplier = scopeGroups[_scopeIndex].GetActiveAttachment().sensitivityMultiplier;
            }

            _userInputController.SetValue("SensitivityMultiplier", sensitivityMultiplier);
        }

        protected void UpdateAimPoint()
        {
            if (scopeGroups.Count == 0) return;

            var scope = scopeGroups[_scopeIndex].GetActiveAttachment().aimPoint;
            _fpsAnimatorEntity.defaultAimPoint = scope;
        }

        protected void InitializeAttachments()
        {
            foreach (var attachmentGroup in scopeGroups)
            {
                attachmentGroup.Initialize(_fpsAnimator);
            }

            _scopeIndex = 0;
            if (scopeGroups.Count == 0) return;

            UpdateAimPoint();
            UpdateTargetFOV(false);
        }

        public override void OnEquip(GameObject parent)
        {
            if (parent == null) return;

            _fpsAnimator = parent.GetComponent<FPSAnimator>();
            _fpsAnimatorEntity = GetComponent<FPSAnimatorEntity>();

            _fpsController = parent.GetComponent<FPSController>();
            _weaponAnimator = GetComponentInChildren<Animator>();

            _controllerAnimator = parent.GetComponent<Animator>();
            _userInputController = parent.GetComponent<UserInputController>();
            _playablesController = parent.GetComponent<IPlayablesController>();
            _fpsCameraController = parent.GetComponentInChildren<FPSCameraController>();

            InitializeAttachments();

            _recoilAnimation = parent.GetComponent<RecoilAnimation>();
            _recoilPattern = parent.GetComponent<RecoilPattern>();

            _controllerAnimator.SetFloat(OverlayType, (float)overlayType);
            _fpsAnimator.LinkAnimatorProfile(gameObject);

            barrelAttachments.Initialize(_fpsAnimator);
            gripAttachments.Initialize(_fpsAnimator);

            _recoilAnimation.Init(recoilData, fireRate, _fireMode);

            if (_recoilPattern != null)
            {
                _recoilPattern.Init(recoilPatternSettings);
            }

            _controllerAnimator.CrossFade(CurveEquip, 0.15f);

        }

        public override void OnUnEquip()
        {
            _controllerAnimator.CrossFade(CurveUnequip, 0.15f);
        }

        /*public override void OnUnarmedEnabled()
        {
            _controllerAnimator.SetFloat(OverlayType, 0);
            _userInputController.SetValue(FPSANames.PlayablesWeight, 0f);
            _userInputController.SetValue(FPSANames.StabilizationWeight, 0f);
        }

        public override void OnUnarmedDisabled()
        {
            _controllerAnimator.SetFloat(OverlayType, (int)overlayType);
            _userInputController.SetValue(FPSANames.PlayablesWeight, 1f);
            _userInputController.SetValue(FPSANames.StabilizationWeight, 1f);
            _fpsAnimator.LinkAnimatorProfile(gameObject);
        }*/

        public override bool OnAimPressed()
        {
            _userInputController.SetValue(FPSANames.IsAiming, true);
            UpdateTargetFOV(true);
            UpdateSensitivityMultiplier(true);
            _recoilAnimation.isAiming = true;
            isAiming = true;

            return true;
        }

        public override bool OnAimReleased()
        {
            _userInputController.SetValue(FPSANames.IsAiming, false);
            UpdateTargetFOV(false);
            UpdateSensitivityMultiplier(false);
            _recoilAnimation.isAiming = false;
            isAiming = false;

            return true;
 
        }

        public override bool OnFirePressed()
        {
            // Do not allow firing faster than the allowed fire rate.
            if (Time.unscaledTime - _lastRecoilTime < 60f / fireRate)
            {
                return false;
            }

            // Check if there is a bullet in the chamber
            if (chamberedBulletInfo == null)
            {
                return false;
            }

            _lastRecoilTime = Time.unscaledTime;
            _bursts = burstLength;

            OnFire();

            return true;
        }


        public override bool OnFireReleased()
        {
            if (_recoilAnimation != null)
            {
                _recoilAnimation.Stop();
            }

            if (_recoilPattern != null)
            {
                _recoilPattern.OnFireEnd();
            }

            CancelInvoke(nameof(OnFire));
            return true;
        }

        public override bool OnReload()
        {
            if (!FPSAnimationAsset.IsValid(reloadClip))
            {
                return false;
            }

            _playablesController.PlayAnimation(reloadClip, 0f);

            if (_weaponAnimator != null)
            {
                _weaponAnimator.Rebind();
                _weaponAnimator.Play("Reload", 0);
            }

            if (_fpsCameraController != null)
            {
                _fpsCameraController.PlayCameraAnimation(cameraReloadAnimation);
            }

            Invoke(nameof(OnActionEnded), reloadClip.clip.length * 0.85f);
            
            /*Invoke(nameof(RefillMagazine), reloadClip.clip.length * 0.85f);
            Invoke(nameof(CycleBullet), reloadClip.clip.length * 0.85f);*/
            _refillMagazine = true;
            _cycleBullet = true;

            if (weaponSound != null)
            {
                weaponSound.PlayReloadSound();
            }

            OnFireReleased();
            return true;
        }


        private void RefillMagazine()
        {
            _magazineManager.FillMagazineWithDefaultBullets();
        }

        public override bool OnGrenadeThrow()
        {
            if (!FPSAnimationAsset.IsValid(grenadeClip))
            {
                return false;
            }

            _playablesController.PlayAnimation(grenadeClip, 0f);

            if (_fpsCameraController != null)
            {
                _fpsCameraController.PlayCameraAnimation(cameraGrenadeAnimation);
            }

            Invoke(nameof(OnActionEnded), grenadeClip.clip.length * 0.8f);
            return true;
        }

        private void OnFire()
        {
            //check if there is a bullet in the chamber before firing
            if (chamberedBulletInfo == null)
            {
                //Stop firing if there are no bullets in the chamber
                OnFireReleased();
                return;
            }

            if (_weaponAnimator != null)
            {
                _weaponAnimator.Play("Fire", 0, 0f);
            }

            _fpsCameraController.PlayCameraShake(cameraShake);

            if (_recoilAnimation != null && recoilData != null)
            {
                _recoilAnimation.Play();
            }

            if (_recoilPattern != null)
            {
                _recoilPattern.OnFireStart();
            }

            //handle the semi-fire mode
            if (_recoilAnimation.fireMode == FireMode.Semi)
            {
                Invoke(nameof(OnFireReleased), 60f / fireRate);
                _fireWeapon = true;
                _cycleBullet = true;
                return;
            }

            //handle the burst mode
            if (_recoilAnimation.fireMode == FireMode.Burst)
            {
                _bursts--;
                _fireWeapon = true;
                _cycleBullet = true;

                if (_bursts == 0)
                {
                    OnFireReleased();
                    return;
                }
            }

            //handle full auto mode
            Invoke(nameof(OnFire), 60f / fireRate);
            _fireWeapon = true;
            _cycleBullet = true;
        }

        private void CycleBullet()
        {
            // If there is a chambered bullet, destroy it
            if (chamberedBulletInfo != null)
            {
                EjectCasing();
                Destroy(chamberedBulletModel.gameObject); // Destroy the bullet model
                chamberedBulletInfo = null; 
            }

            // If there are bullets in the magazine, load the next one into the chamber
            if (_magazineManager.bulletsInMag.Count > 0)
            {
                chamberedBulletInfo = _magazineManager.bulletsInMag[0];
                _magazineManager.UseBullet(); // Consume a bullet

                // Instantiate the bullet and parent it to the bolt or weapon
                chamberedBulletModel = Instantiate(chamberedBulletInfo.RoundPrefab, bolt != null ? bolt : this.transform);
                chamberedBulletModel.transform.localPosition = Vector3.zero;
                chamberedBulletModel.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

                if (bolt == null)
                {
                    Debug.LogWarning("Bolt is null, instantiating bullet in weapon instead");
                }
            }
            else
            {
                // If there are no bullets in the magazine, clear the chamber
                chamberedBulletInfo = null;
            }
        }
        private void EjectCasing()
        {
            if (casingEjectPoint != null && chamberedBulletInfo != null && chamberedBulletInfo.CasingPrefab != null)
            {
                // Instantiate the casing prefab at the eject point
                _casingModel = Instantiate(chamberedBulletInfo.CasingPrefab, casingEjectPoint.position, casingEjectPoint.rotation);
                Rigidbody casingRigidbody = _casingModel.AddComponent<Rigidbody>();

                // Randomized ejection force and direction
                float forceVariation = UnityEngine.Random.Range(0.8f, 1.2f);
                Vector3 ejectDirection = (casingEjectPoint.forward + casingEjectPoint.up * 0.5f).normalized;
                Vector3 randomTorque = new Vector3(
                    UnityEngine.Random.Range(-casingTorque, casingTorque),
                    UnityEngine.Random.Range(-casingTorque, casingTorque),
                    UnityEngine.Random.Range(-casingTorque, casingTorque)
                );

                // Apply force to the casing to simulate ejection
                casingRigidbody.AddForce(ejectDirection * ejectForce * forceVariation, ForceMode.Impulse);

                // Apply random torque to simulate realistic rotation
                casingRigidbody.AddTorque(randomTorque);

                // Set the casing to self-destruct after a certain time
                Destroy(_casingModel, casingLifetime);

                // Debug information
                //Debug.Log($"Ejecting casing with force: {ejectDirection * ejectForce * forceVariation} and torque: {randomTorque}");
            }
            else
            {
                Debug.LogWarning("Casing eject point or bullet info or casing prefab is null, cannot eject casing");
            }
        }


        public override void OnCycleScope()
        {
            if (scopeGroups.Count == 0) return;

            _scopeIndex++;
            _scopeIndex = _scopeIndex > scopeGroups.Count - 1 ? 0 : _scopeIndex;

            UpdateAimPoint();
            UpdateTargetFOV(true);
        }

        private void CycleFireMode()
        {
            if (_fireMode == FireMode.Semi && supportsBurst)
            {
                _fireMode = FireMode.Burst;
                _bursts = burstLength;
                return;
            }

            if (_fireMode != FireMode.Auto && supportsAuto)
            {
                _fireMode = FireMode.Auto;
                return;
            }

            _fireMode = FireMode.Semi;
        }

        public override void OnChangeFireMode()
        {
            CycleFireMode();
            _recoilAnimation.fireMode = _fireMode;
        }

        public override void OnAttachmentChanged(int attachmentTypeIndex)
        {
            if (attachmentTypeIndex == 1)
            {
                barrelAttachments.CycleAttachments(_fpsAnimator);
                return;
            }

            if (attachmentTypeIndex == 2)
            {
                gripAttachments.CycleAttachments(_fpsAnimator);
                return;
            }

            if (scopeGroups.Count == 0) return;
            scopeGroups[_scopeIndex].CycleAttachments(_fpsAnimator);
            UpdateAimPoint();
            //UpdateMuzzleSystems();
        }

        public void Shoot(int pelletsPerShot)
        {
            for (var i = pelletsPerShot; i > 0; i--)
            //ballisticsWeapon.Shoot(SpreadController.CalculateShootDirection(ballisticsWeapon), CurrentZeroing().Angle);
            ballisticsWeapon.Shoot(SpreadController.CalculateShootDirection(ballisticsWeapon), 0);
        }

        private void Update()
        {
            // Check for malfunctions when firing
            if (IsFiring && IsOperational)
            {
                CheckForMalfunctions();
            }

            // Handle state-specific updates
            UpdateWeaponState();

            if (_refillMagazine)
            {
                RefillMagazine();
                _refillMagazine = false;
            }

            if (_cycleBullet)
            {
                CycleBullet();
                _cycleBullet = false;
            }

            if (Input.mouseScrollDelta.y > 0 || Input.mouseScrollDelta.y < 0)
            {
                if (isAiming)
                {
                    UpdateSensitivityMultiplier(true);
                }
                else 
                {
                    UpdateSensitivityMultiplier(false);
                }
            }


            if(_fireWeapon)
            {
                if (isShotgun == true)
                {
                    pelletsPerShot = chamberedBulletInfo.PelletsPerShot;
                    Shoot(pelletsPerShot);
                }
                else
                {
                    ballisticsWeapon.Shoot();
                }   

                if (weaponSound != null)
                {
                    weaponSound.PlayShotSound();
                }

                if (muzzleFlashVFX != null && muzzleSmokeVFX != null && chamberSmokeVFX != null)
                {
                    // Instantiate muzzle flash
                    muzzleFlashVFX.Play();

                    // Instantiate muzzle smoke
                    VisualEffect muzzleSmokeInstance = Instantiate(muzzleSmokeVFX, muzzleSmokeVFX.transform.position, muzzleSmokeVFX.transform.rotation);
                    muzzleSmokeInstance.Play();

                    // Instantiate chamber smoke
                    VisualEffect chamberSmokeInstance = Instantiate(chamberSmokeVFX, chamberSmokeVFX.transform.position, chamberSmokeVFX.transform.rotation);
                    chamberSmokeInstance.Play();

                    // Destroy the instantiated VFX after a certain duration
                    Destroy(muzzleSmokeInstance.gameObject, effectDuration);
                    Destroy(chamberSmokeInstance.gameObject, effectDuration);
                }
                else
                    Debug.Log("One of the weapon shooting VFX is not assigned");


                _fireWeapon = false;
            }

            /*if (Input.GetMouseButtonDown(2))
            {
                canHoldBreath = scopeGroups[_scopeIndex].GetActiveAttachment().canHoldBreath;
                if (canHoldBreath)
                {
                    Debug.Log("Holding breath");
                    _userInputController.SetValue("HoldBreath", 0);

                }
                else 
                {
                    Debug.Log("Attachment does not support holding breath");
                }
            }
            if (Input.GetMouseButtonUp(2))
            {
                if (canHoldBreath)
                {
                    Debug.Log("Not holding breath");
                    _userInputController.SetValue("HoldBreath", 1);
                }
            }*/

        }

        private void CheckForMalfunctions()
        {
            float random = UnityEngine.Random.value;  // Explicitly use UnityEngine.Random
            
            if (random < jamChance)
            {
                SetWeaponState(WeaponState.JammedBolt);
            }
            else if (random < feedFailureChance)
            {
                SetWeaponState(WeaponState.FailureToFeed);
            }
            else if (random < misfireChance)
            {
                SetWeaponState(WeaponState.Misfire);
            }
        }

        private void UpdateWeaponState()
        {
            switch (currentState)
            {
                case WeaponState.JammedBolt:
                case WeaponState.FailureToFeed:
                case WeaponState.Misfire:
                    // These states require manual intervention
                    if (Input.GetKeyDown(KeyCode.T)) // T for "Troubleshoot"
                    {
                        SetWeaponState(WeaponState.FixingMalfunction);
                        StartCoroutine(FixMalfunction());
                    }
                    break;
            }
        }

        public void SetWeaponState(WeaponState newState)
        {
            // Don't allow state changes while fixing a malfunction
            if (currentState == WeaponState.FixingMalfunction)
                return;

            currentState = newState;
            OnWeaponStateChanged(newState);
        }

        private void OnWeaponStateChanged(WeaponState newState)
        {
            // Handle state transition effects/animations
            switch (newState)
            {
                case WeaponState.JammedBolt:
                    PlayMalfunctionSound("jam");
                    break;
                case WeaponState.FailureToFeed:
                    PlayMalfunctionSound("feedfailure");
                    break;
                case WeaponState.Misfire:
                    PlayMalfunctionSound("misfire");
                    break;
            }
        }

        private IEnumerator FixMalfunction()
        {
            // Play fixing animation
            // animator.SetTrigger("FixMalfunction");
            
            yield return new WaitForSeconds(malfunctionFixTime);
            
            // Return to idle state
            SetWeaponState(WeaponState.Idle);
        }

        private void PlayMalfunctionSound(string soundType)
        {
            // Play appropriate malfunction sound effect
            // audioSource.PlayOneShot(GetMalfunctionClip(soundType));
        }

        // Change from override to new method since there's no base method to override
        public bool CanFire()
        {
            return IsOperational;
        }
    }
}
