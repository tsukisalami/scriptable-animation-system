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
        // Events for editor integration
        public static event System.Action OnWeaponFired;
        public static event System.Action OnWeaponReloaded;

        [Header("General")]
        [SerializeField] [Range(0f, 120f)] private float fieldOfView = 90f;

        [SerializeField] private Transform bolt; // Reference to the bolt GameObject
        [SerializeField] private Transform casingEjectPoint; // Reference to the eject point
        [SerializeField] private float ejectForce = 1.5f; // Force applied to the casing
        [SerializeField] private float casingLifetime = 5f; // Lifetime of the casing before it gets destroyed
        [SerializeField] private float casingTorque = 100f; 

        [Header("Magazine")]
        [SerializeField] public MagazineData defaultMagazine; // Default magazine for this weapon
        private MagazineData currentMagazine; // Currently loaded magazine
        private List<BulletInfo> bulletsInMag = new List<BulletInfo>(); // Current bullets in the magazine

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
        private bool _swapMagazine = false;
        private bool _cycleBullet = false;
        private bool _fireWeapon = false;

        private Ballistics.Weapon ballisticsWeapon;
        private GameObject chamberedBulletModel;
        [SerializeField] private BulletInfo chamberedBulletInfo;
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

        // Add this private field to track initialization
        private bool _isFullyInitialized = false;

        private void Start()
        {
            AssignManagers();
            AssignBallisticsWeapon();
            
            // Initialize internal magazine list with default bullets
            // We will sync with PlayerLoadout properly in OnEquip
            if (defaultMagazine != null)
            {
                currentMagazine = ScriptableObject.Instantiate(defaultMagazine);
                bulletsInMag = new List<BulletInfo>(); // Ensure it's a new list
                MagazineExtensions.FillMagazineWithDefaultBullets(currentMagazine, bulletsInMag);
                
                // IMPORTANT: Pre-chamber a bullet at startup to ensure
                // weapons start with a chambered round before any player interaction
                if (bulletsInMag.Count > 0)
                {
                    // Force-create the chambered bullet directly
                    chamberedBulletInfo = bulletsInMag[0];
                    bulletsInMag.RemoveAt(0);
                }
            }
            
            // Always initialize in Idle state
            SetWeaponState(WeaponState.Idle);
            
            // Fire an initial weapon reloaded event to trigger UI updates
            OnWeaponReloaded?.Invoke();
        }

        private void AssignManagers()
        {   
            weaponSound = GetComponent<WeaponSound>();
            if (weaponSound == null)
            {
                Debug.LogError("No WeaponSound assigned.");
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
            
            // Make sure to set weapon to idle state after reload/action ends
            SetWeaponState(WeaponState.Idle);
            
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

            // Standard setup...
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
            
            // --- Reliable Initialization Logic --- 
            // Sync with PlayerLoadout state FIRST
            SyncWithPlayerLoadout();
            
            // ENHANCED BULLET CHAMBERING:
            // HIGH PRIORITY: Always check if we need to chamber a bullet
            if (chamberedBulletInfo == null && bulletsInMag.Count > 0)
            {
                CycleBullet(); // Force bullet chambering immediately
            }
            else if (chamberedBulletInfo != null && chamberedBulletModel == null)
            {
                InstantiateBulletModel(); // Create bullet visual if info exists but model doesn't
            }
            
            // Set weapon state to Idle to ensure it's operational  
            SetWeaponState(WeaponState.Idle);
            
            // Force update UI before marking fully initialized
            ForceUpdateMagazineUI();
            
            // Mark as fully initialized AFTER syncing and cycling
            _isFullyInitialized = true;
        }

        // Updated SyncWithPlayerLoadout method to avoid "not found" errors
        private void SyncWithPlayerLoadout()
        {
            if (_fpsController == null) 
            {
                Debug.LogError($"Weapon {gameObject.name}: FPSController reference missing!");
                return;
            }
            
            var playerLoadout = _fpsController.GetComponent<PlayerLoadout>();
            if (playerLoadout == null) 
            {
                Debug.LogError($"Weapon {gameObject.name}: PlayerLoadout component missing on controller!");
                return;
            }
            
            // Wait/check if PlayerLoadout magazine system is ready
            if (!playerLoadout.IsWeaponMagazineSystemInitialized())
            {
                Debug.LogWarning($"Weapon {gameObject.name}: PlayerLoadout magazine system not ready yet. Sync deferred.");
                return; 
            }
            
            // Get current category and item index FROM PlayerLoadout
            int categoryIndex = playerLoadout.currentCategoryIndex;
            var category = playerLoadout.GetCategory(categoryIndex);
            if (category == null || categoryIndex < 0) 
            {
                Debug.LogError($"Weapon {gameObject.name}: Could not get valid category from PlayerLoadout (Index: {categoryIndex})");
                return;
            }
            
            // First try to find the weapon by direct gameObject reference
            int itemIndex = -1;
            
            // Check if weapon in current index matches this
            if (category.currentIndex >= 0 && category.currentIndex < category.items.Count)
            {
                var prefabItem = category.items[category.currentIndex];
                
                // Compare by name instead of direct reference (won't match prefabs to instances)
                if (prefabItem != null && prefabItem.name == gameObject.name.Replace("(Clone)", ""))
                {
                    itemIndex = category.currentIndex;
                }
            }
            
            // If not found by current index, search by name in the category
            if (itemIndex < 0)
            {
                string cleanName = gameObject.name.Replace("(Clone)", "");
                
                for (int i = 0; i < category.items.Count; i++)
                {
                    if (category.items[i] != null && category.items[i].name == cleanName)
                    {
                        itemIndex = i;
                        break;
                    }
                }
            }
            
            // If still not found, try searching by type as last resort
            if (itemIndex < 0)
            {
                for (int i = 0; i < category.items.Count; i++)
                {
                    if (category.items[i] != null && 
                        category.items[i].GetType() == this.GetType())
                    {
                        itemIndex = i;
                        break;
                    }
                }
            }
            
            if (itemIndex < 0)
            {
                Debug.LogWarning($"Weapon {gameObject.name}: Not found in current PlayerLoadout category '{category.categoryName}' items. Cannot sync state.");
                return;
            }

            // Rest of the sync logic continues...
            // Ensure magazine data exists in PlayerLoadout for this specific weapon index
            if (category.weaponMagazineTemplates == null || category.weaponMagazineTemplates.Count <= itemIndex ||
                category.weaponMagazineBullets == null || category.weaponMagazineBullets.Count <= itemIndex || 
                category.weaponMagazineBullets[itemIndex] == null)
            {
                Debug.LogError($"Weapon {gameObject.name}: Magazine data structure missing in PlayerLoadout for index {itemIndex} in category {categoryIndex}.");
                return;
            }
            
            // Get the list of magazines for this weapon from PlayerLoadout
            var loadoutMagazineList = category.weaponMagazineBullets[itemIndex];

            // Ensure there's at least one magazine entry (even if empty)
            if (loadoutMagazineList.Count == 0)
            {
                Debug.LogWarning($"Weapon {gameObject.name}: No magazine entries found in PlayerLoadout for index {itemIndex}. Creating default empty.");
                // This assumes a corresponding template exists at index 0
                if(category.weaponMagazineTemplates[itemIndex].Count > 0)
                {
                     loadoutMagazineList.Add(new List<BulletInfo>()); 
                }
                else
                {
                    Debug.LogError($"Weapon {gameObject.name}: Cannot create default magazine entry as no template exists at index 0.");
                    return;
                }
            }

            // The first magazine in the loadout list (index 0) represents the *actual* current state
            var currentLoadoutMagBullets = loadoutMagazineList[0];
            
            // Overwrite the weapon's internal bullet list with the state from PlayerLoadout's current mag
            bulletsInMag.Clear();
            bulletsInMag.AddRange(currentLoadoutMagBullets);
            
            // Always update FPSController's ammo status after syncing
            _fpsController.UpdateAmmoStatus();
        }

        public override void OnUnEquip()
        {
            // --- Save State Back to PlayerLoadout ---
            if (_fpsController != null)
            {
                var playerLoadout = _fpsController.GetComponent<PlayerLoadout>();
                if (playerLoadout != null && playerLoadout.IsWeaponMagazineSystemInitialized())
                {
                    // Find this weapon's category and index in the loadout using same search strategy as SyncWithPlayerLoadout
                    int categoryIndex = -1;
                    int itemIndex = -1;
                    
                    // First try to find in the current category (most likely case)
                    int currentCatIndex = playerLoadout.currentCategoryIndex;
                    if (currentCatIndex >= 0)
                    {
                        var currentCategory = playerLoadout.GetCategory(currentCatIndex);
                        if (currentCategory != null)
                        {
                            // Find by name without (Clone)
                            string cleanName = gameObject.name.Replace("(Clone)", "");
                            itemIndex = -1;
                            
                            for (int i = 0; i < currentCategory.items.Count; i++)
                            {
                                if (currentCategory.items[i] != null && 
                                    currentCategory.items[i].name == cleanName)
                                {
                                    itemIndex = i;
                                    categoryIndex = currentCatIndex;
                                    break;
                                }
                            }
                        }
                    }
                    
                    // If not found in current category, search all categories
                    if (categoryIndex < 0)
                    {
                        string cleanName = gameObject.name.Replace("(Clone)", "");
                        
                        for (int catIdx = 0; catIdx < 6; catIdx++)
                        {
                            var category = playerLoadout.GetCategory(catIdx);
                            if (category == null) continue;
                            
                            for (int i = 0; i < category.items.Count; i++)
                            {
                                if (category.items[i] != null && 
                                    category.items[i].name == cleanName)
                                {
                                    itemIndex = i;
                                    categoryIndex = catIdx;
                                    break;
                                }
                            }
                            
                            if (categoryIndex >= 0) break;
                        }
                    }

                    if (categoryIndex >= 0 && itemIndex >= 0)
                    {
                        var category = playerLoadout.GetCategory(categoryIndex);
                        // Ensure the structure exists
                        if (category.weaponMagazineBullets != null && category.weaponMagazineBullets.Count > itemIndex &&
                            category.weaponMagazineBullets[itemIndex] != null)
                        {
                            // Ensure the list for the current mag (index 0) exists
                            if (category.weaponMagazineBullets[itemIndex].Count == 0)
                            {
                               category.weaponMagazineBullets[itemIndex].Add(new List<BulletInfo>());
                            }
                            
                            // Save the current weapon's magazine state back to PlayerLoadout's first slot
                            // Create a new list to avoid reference issues
                            category.weaponMagazineBullets[itemIndex][0] = new List<BulletInfo>(bulletsInMag);
                        }
                        else
                        {
                            Debug.LogWarning($"Weapon {gameObject.name}: Could not save state on unequip. Magazine structure not found.");
                        }
                    }
                    else
                    {
                        // Remove non-critical log and replace with more specific warning if needed
                    }
                }
            }

            _controllerAnimator.CrossFade(CurveUnequip, 0.15f);
            
            // Reset flags/state if needed when unequipped
            _isFullyInitialized = false; 
            OnFireReleased(); // Ensure firing stops
        }

        public void OnUnarmedEnabled()
        {
            _controllerAnimator.SetFloat(OverlayType, 0);
            _userInputController.SetValue(FPSANames.PlayablesWeight, 0f);
            _userInputController.SetValue(FPSANames.StabilizationWeight, 0f);
        }

        public void OnUnarmedDisabled()
        {
            _controllerAnimator.SetFloat(OverlayType, (int)overlayType);
            _userInputController.SetValue(FPSANames.PlayablesWeight, 1f);
            _userInputController.SetValue(FPSANames.StabilizationWeight, 1f);
            _fpsAnimator.LinkAnimatorProfile(gameObject);
        }

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

        // Modify OnFirePressed to check if weapon is initialized
        public override bool OnFirePressed()
        {
            // First check if weapon is fully initialized
            if (!_isFullyInitialized)
            {
                Debug.LogWarning($"Cannot fire {gameObject.name} - weapon not fully initialized yet");
                return false;
            }
            
            // Check if the weapon has a chambered bullet with detailed logging
            if (chamberedBulletInfo == null)
            {
                // Simplify the log message
                Debug.LogWarning($"Cannot fire {gameObject.name} - no bullet in chamber");
                return false;
            }
            
            // Check for rate of fire limit
            if (Time.unscaledTime - _lastRecoilTime < 60f / fireRate)
            {
                return false;
            }

            // Remove debug log about firing
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

        // Modify OnReload to check if weapon is initialized
        public override bool OnReload()
        {
            // Do not allow reloading if not fully initialized
            if (!_isFullyInitialized)
            {
                Debug.LogWarning($"Cannot reload {gameObject.name} - not fully initialized yet");
                return false;
            }
            
            if (!FPSAnimationAsset.IsValid(reloadClip))
            {
                return false;
            }
            
            // Set state to reloading first
            SetWeaponState(WeaponState.Reloading);
            
            // Check if we actually have a magazine with ammo
            var playerLoadout = _fpsController.GetComponent<PlayerLoadout>();
            if (playerLoadout != null && !playerLoadout.CurrentWeaponHasAmmo())
            {
                // No magazines with ammo available - return to idle state
                SetWeaponState(WeaponState.Idle);
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

            // Set when the reload action will end
            float reloadDuration = reloadClip.clip.length * 0.85f;
            Invoke(nameof(OnActionEnded), reloadDuration);
            
            // Set the flag to swap magazine
            _swapMagazine = true;
            
            // Only cycle bullet if there's none in the chamber
            if (chamberedBulletInfo == null)
            {
                _cycleBullet = true;
            }

            if (weaponSound != null)
            {
                weaponSound.PlayReloadSound();
            }

            OnFireReleased();
            
            // Notify editor that we reloaded
            OnWeaponReloaded?.Invoke();
            return true;
        }

        // Replace RefillMagazine method to work with the new system
        private void SwapMagazine()
        {
            var playerLoadout = _fpsController.GetComponent<PlayerLoadout>();
            if (playerLoadout == null) return;
            
            // Get the current category and item index
            int categoryIndex = playerLoadout.currentCategoryIndex;
            var category = playerLoadout.GetCategory(categoryIndex);
            if (category == null) return;
            
            int itemIndex = category.currentIndex;
            
            // Preserve any remaining bullets in the current magazine
            List<BulletInfo> remainingBullets = new List<BulletInfo>(bulletsInMag);
            
            // Make sure we have magazine lists for this weapon
            if (category.weaponMagazineTemplates.Count <= itemIndex || 
                category.weaponMagazineBullets.Count <= itemIndex)
            {
                Debug.LogError("Magazine data missing for weapon");
                return;
            }
            
            // First, save the current magazine state back to its position (index 0)
            if (category.weaponMagazineBullets[itemIndex].Count > 0)
            {
                category.weaponMagazineBullets[itemIndex][0] = remainingBullets;
            }
            
            // Find the magazine with the most bullets (after index 0)
            int bestMagIndex = -1;
            int mostBullets = 0;
            
            for (int i = 1; i < category.weaponMagazineBullets[itemIndex].Count; i++)
            {
                int bulletCount = category.weaponMagazineBullets[itemIndex][i].Count;
                
                // If this is a full magazine, use it immediately
                if (bulletCount == category.weaponMagazineTemplates[itemIndex][i].ammoCount)
                {
                    bestMagIndex = i;
                    break;
                }
                
                // Otherwise, keep track of the one with most bullets
                if (bulletCount > mostBullets)
                {
                    mostBullets = bulletCount;
                    bestMagIndex = i;
                }
            }
            
            // If we found a usable magazine, swap it with the current one
            if (bestMagIndex > 0)
            {
                // Get the magazine with bullets
                var bestMag = category.weaponMagazineBullets[itemIndex][bestMagIndex];
                var bestTemplate = category.weaponMagazineTemplates[itemIndex][bestMagIndex];
                
                // Save current (potentially empty) magazine to that position
                var currentMag = category.weaponMagazineBullets[itemIndex][0];
                var currentTemplate = category.weaponMagazineTemplates[itemIndex][0];
                
                // Swap the magazines
                category.weaponMagazineBullets[itemIndex][0] = bestMag;
                category.weaponMagazineTemplates[itemIndex][0] = bestTemplate;
                
                category.weaponMagazineBullets[itemIndex][bestMagIndex] = currentMag;
                category.weaponMagazineTemplates[itemIndex][bestMagIndex] = currentTemplate;
                
                // Load the new magazine into the weapon
                bulletsInMag.Clear();
                bulletsInMag.AddRange(bestMag);
                
                // Check if we need to chamber a round
                if (chamberedBulletInfo == null && bulletsInMag.Count > 0)
                {
                    _cycleBullet = true; // Set flag to cycle a bullet from the new magazine
                }
            }
            else
            {
                // Convert to warning for better visibility
                Debug.LogWarning("No spare magazines with ammo found");
            }
            
            // Notify editor that we reloaded
            OnWeaponReloaded?.Invoke();
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
                
                // Check if we're out of ammo completely
                if (bulletsInMag.Count == 0)
                {
                    var playerLoadout = _fpsController.GetComponent<PlayerLoadout>();
                    if (playerLoadout != null && !playerLoadout.CurrentWeaponHasAmmo())
                    {
                        // Set the OutOfAmmo flag in FPSController
                        _fpsController.IsOutOfAmmo = true;
                    }
                }
                
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
            
            // Notify editor that we fired
            OnWeaponFired?.Invoke();
        }

        // Update CycleBullet to use InstantiateBulletModel and trigger UI updates
        private void CycleBullet()
        {
            bool didChangeBullet = false;
            
            // If there is a chambered bullet, destroy it
            if (chamberedBulletInfo != null)
            {
                EjectCasing();
                if (chamberedBulletModel != null)
                {
                    Destroy(chamberedBulletModel.gameObject); // Destroy the bullet model
                    chamberedBulletModel = null;
                }
                chamberedBulletInfo = null;
                didChangeBullet = true;
            }

            // If there are bullets in the magazine, load the next one into the chamber
            if (bulletsInMag.Count > 0)
            {
                chamberedBulletInfo = bulletsInMag[0]; 
                bulletsInMag.RemoveAt(0); // Consume a bullet
                didChangeBullet = true;

                // Use the helper method to create the bullet model
                InstantiateBulletModel();

                // Ensure we're in operational state when chambering a round
                if (currentState != WeaponState.Idle)
                {
                    SetWeaponState(WeaponState.Idle);
                }
            }
            else
            {
                // If there are no bullets in the magazine, clear the chamber
                chamberedBulletInfo = null;
                chamberedBulletModel = null;
                
                // Update the out-of-ammo state in FPSController
                var playerLoadout = _fpsController?.GetComponent<PlayerLoadout>();
                if (playerLoadout != null)
                {
                    // Set OutOfAmmo flag if we have no more magazines with ammo
                    _fpsController.IsOutOfAmmo = !playerLoadout.CurrentWeaponHasAmmo();
                }
            }
            
            // Always update the UI when we cycle a bullet
            if (didChangeBullet && _isFullyInitialized)
            {
                // --- ADDED: Update PlayerLoadout with current magazine state --- 
                var playerLoadout = _fpsController?.GetComponent<PlayerLoadout>();
                if (playerLoadout != null && playerLoadout.IsWeaponMagazineSystemInitialized())
                {
                    int categoryIndex = playerLoadout.currentCategoryIndex;
                    var category = playerLoadout.GetCategory(categoryIndex);
                    if (category != null)
                    {
                        int itemIndex = category.currentIndex;
                        if (itemIndex >= 0 && itemIndex < category.weaponMagazineBullets.Count && 
                            category.weaponMagazineBullets[itemIndex].Count > 0)
                        {
                            // Update the first magazine slot (index 0) in PlayerLoadout
                            category.weaponMagazineBullets[itemIndex][0] = new List<BulletInfo>(bulletsInMag);
                        }
                    }
                }
                // --- END ADDED CODE ---

                ForceUpdateMagazineUI();
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

            if (_swapMagazine)
            {
                SwapMagazine();
                _swapMagazine = false;
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
                    VisualEffect muzzleFlashInstance = Instantiate(muzzleFlashVFX, muzzleFlashVFX.transform.position, muzzleFlashVFX.transform.rotation);
                    muzzleFlashInstance.Play();

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

        // Helper method to instantiate the bullet model
        private void InstantiateBulletModel()
        {
            if (chamberedBulletInfo == null || chamberedBulletInfo.RoundPrefab == null) return;
            
            // Clean up old model if it exists
            if (chamberedBulletModel != null)
            {
                Destroy(chamberedBulletModel);
            }
            
            // Create the bullet model
            chamberedBulletModel = Instantiate(chamberedBulletInfo.RoundPrefab, bolt != null ? bolt : this.transform);
            chamberedBulletModel.transform.localPosition = Vector3.zero;
            chamberedBulletModel.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        }

        // Add a helper method to force magazine UI updates
        public void ForceUpdateMagazineUI()
        {
            // Trigger the reload event which will update UI elements
            OnWeaponReloaded?.Invoke();
            
            // Update ammo status in FPSController if available
            if (_fpsController != null)
            {
                _fpsController.UpdateAmmoStatus();
            }
        }

        // Method for external components to force bullet chambering (like FPSController at game start)
        public bool ForceChamberBullet()
        {
            if (chamberedBulletInfo == null && bulletsInMag.Count > 0)
            {
                // Simplify log message
                CycleBullet();
                return true;
            }
            else if (chamberedBulletInfo != null && chamberedBulletModel == null)
            {
                // Make sure the visual model exists
                InstantiateBulletModel();
                return true;
            }
            
            return false; // No changes made
        }
        
        // Method to check if weapon has a bullet chambered
        public bool HasChamberedBullet()
        {
            return chamberedBulletInfo != null;
        }
        
        // Add a public method to get bullet count for UI display
        public int GetBulletsInMagazine()
        {
            return bulletsInMag.Count;
        }
        
        // Add a public method to check total ammo state
        public bool HasAnyAmmo()
        {
            return chamberedBulletInfo != null || bulletsInMag.Count > 0;
        }

        public void PlayBreathSound(bool isHolding)
        {
            /*if (!canHoldBreath || spottingScope == null)
            {
                // Just return silently without log - this is an expected condition
                return;
            }
            */
            
            // Handle breath sound logic without verbose logging
            // (Add actual sound playing code here when needed)
        }

        private void InitializeVFX()
        {
            if (muzzleFlashVFX == null || muzzleSmokeVFX == null || chamberSmokeVFX == null)
            {
                // Convert to warning with more specific guidance
                Debug.LogWarning("Some weapon VFX components are missing - this may affect visual feedback during firing.");
            }
        }
    }
}

