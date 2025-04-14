// Designed by KINEMATION, 2025.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.KAnimationCore.Runtime.Input;
using KINEMATION.KAnimationCore.Runtime.Rig;

using Demo.Scripts.Runtime.Item;
using System.Collections.Generic;
using System.Collections;

using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo.Scripts.Runtime.Character
{
    public enum FPSAimState
    {
        None,
        Ready,
        Aiming,
        PointAiming
    }

    public enum FPSActionState
    {
        None,
        PlayingAnimation,
        WeaponChange,
        AttachmentEditing,
        // Building - Removed as this is now handled by PlayerStateManager
    }

    [RequireComponent(typeof(CharacterController), typeof(FPSMovement))]
    public class FPSController : MonoBehaviour
    {
        // Event for ammo status changes
        public delegate void AmmoStatusChangedHandler(bool isOutOfAmmo);
        public event AmmoStatusChangedHandler OnAmmoStatusChanged;

        [SerializeField] private FPSControllerSettings settings;

        private FPSMovement _movementComponent;

        private Transform _weaponBone;
        private Vector2 _playerInput;

        public int _activeWeaponIndex;
        private int _previousWeaponIndex;

        private FPSAimState _aimState;
        public FPSActionState _actionState;

        private Animator _animator;

        // ~Scriptable Animation System Integration
        private FPSAnimator _fpsAnimator;
        private UserInputController _userInput;
        // ~Scriptable Animation System Integration

        public List<FPSItem> _instantiatedWeapons;
        private Vector2 _lookDeltaInput;

        private RecoilPattern _recoilPattern;
        private int _sensitivityMultiplierPropertyIndex;

        private static int _fullBodyWeightHash = Animator.StringToHash("FullBodyWeight");
        private static int _proneWeightHash = Animator.StringToHash("ProneWeight");
        private static int _inspectStartHash = Animator.StringToHash("InspectStart");
        private static int _inspectEndHash = Animator.StringToHash("InspectEnd");
        private static int _slideHash = Animator.StringToHash("Sliding");

        private PlayerLoadout playerLoadout;

        [SerializeField] private GameplayHUD gameplayHUD;  // Reference to UI object's GameplayHUD component

        [SerializeField] private InventoryEvents inventoryEvents;
        private InputStateManager inputStateManager;
        private PlayerStateManager playerStateManager; // Reference to our new player state manager

        // Add OutOfAmmo flag with event trigger
        private bool _isOutOfAmmo;
        public bool IsOutOfAmmo 
        { 
            get => _isOutOfAmmo;
            set 
            {
                if (_isOutOfAmmo != value)
                {
                    _isOutOfAmmo = value;
                    OnAmmoStatusChanged?.Invoke(_isOutOfAmmo);
                }
            } 
        }

        private void PlayTransitionMotion(FPSAnimatorLayerSettings layerSettings)
        {
            if (layerSettings == null)
            {
                return;
            }
            
            _fpsAnimator.LinkAnimatorLayer(layerSettings);
        }

        private bool IsSprinting()
        {
            return _movementComponent.MovementState == FPSMovementState.Sprinting;
        }
        
        private bool HasActiveAction()
        {
            return _actionState != FPSActionState.None;
        }

        private bool IsAiming()
        {
            return _aimState is FPSAimState.Aiming or FPSAimState.PointAiming;
        }

        private void InitializeMovement()
        {
            _movementComponent = GetComponent<FPSMovement>();
            
            _movementComponent.onJump = () => { PlayTransitionMotion(settings.jumpingMotion); };
            _movementComponent.onLanded = () => { PlayTransitionMotion(settings.jumpingMotion); };

            _movementComponent.onCrouch = OnCrouch;
            _movementComponent.onUncrouch = OnUncrouch;

            _movementComponent.onSprintStarted = OnSprintStarted;
            _movementComponent.onSprintEnded = OnSprintEnded;

            _movementComponent.onSlideStarted = OnSlideStarted;

            _movementComponent._slideActionCondition += () => !HasActiveAction();
            _movementComponent._sprintActionCondition += () => !HasActiveAction();
            _movementComponent._proneActionCondition += () => !HasActiveAction();
            
            _movementComponent.onStopMoving = () =>
            {
                PlayTransitionMotion(settings.stopMotion);
            };
            
            _movementComponent.onProneEnded = () =>
            {
                _userInput.SetValue(FPSANames.PlayablesWeight, 1f);
            };
        }

        private void InitializeWeapons()
        {
            _instantiatedWeapons = new List<FPSItem>();
            playerLoadout = GetComponent<PlayerLoadout>();

            if (playerLoadout == null)
            {
                Debug.LogError("FPSController: PlayerLoadout component not found!");
                return;
            }

            var allItems = playerLoadout.GetAllItems();

            // Instantiate all items first
            foreach (var itemPrefab in allItems)
            {
                var itemInstance = Instantiate(itemPrefab, transform.position, Quaternion.identity);
                var itemTransform = itemInstance.transform;

                itemTransform.parent = _weaponBone;
                itemTransform.localPosition = Vector3.zero;
                itemTransform.localRotation = Quaternion.identity;

                var itemComponent = itemInstance.GetComponent<FPSItem>();
                _instantiatedWeapons.Add(itemComponent);
                
                itemInstance.gameObject.SetActive(false);
            }

            _actionState = FPSActionState.None;
            
            // Start a coroutine to equip the initial weapon only after PlayerLoadout is ready
            StartCoroutine(EquipInitialWeaponWhenReady());
        }

        private IEnumerator EquipInitialWeaponWhenReady()
        {
            // Wait until PlayerLoadout confirms its magazine system is initialized
            while (!playerLoadout.IsWeaponMagazineSystemInitialized())
            {
                yield return null; // Wait for the next frame
            }
            
            // Now that PlayerLoadout is ready, initialize all weapons 
            
            // First ensure all weapons have proper bullet chambering (do this before equip)
            foreach (var weapon in _instantiatedWeapons)
            {
                if (weapon is Demo.Scripts.Runtime.Item.Weapon gunWeapon)
                {
                    if (!gunWeapon.HasChamberedBullet() && gunWeapon.GetBulletsInMagazine() > 0) 
                    {
                        // Force chamber a bullet in any weapons without one
                        gunWeapon.ForceChamberBullet();
                    }
                }
            }
            
            // Now that all weapons are properly initialized, equip the initial weapon
            if (_instantiatedWeapons.Count > 0)
            {
                // Set the initial category and item indices in PlayerLoadout
                // Assuming the first weapon is in the primary category (index 0)
                playerLoadout.currentCategoryIndex = 0; 
                playerLoadout.GetCategory(0).currentIndex = 0; 
                
                _activeWeaponIndex = 0; // Assuming the first instantiated weapon corresponds to the primary
                
                EquipWeapon(); // This will call OnEquip and SyncWithPlayerLoadout
            }
            else
            {
                Debug.LogWarning("No weapons instantiated to equip initially.");
            }
        }

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Application.targetFrameRate = 144;
            
            _fpsAnimator = GetComponent<FPSAnimator>();
            _fpsAnimator.Initialize();

            _weaponBone = GetComponentInChildren<KRigComponent>().GetRigTransform(settings.weaponBone);
            _animator = GetComponent<Animator>();
            
            _userInput = GetComponent<UserInputController>();
            _recoilPattern = GetComponent<RecoilPattern>();
            inputStateManager = GetComponent<InputStateManager>();
            playerStateManager = GetComponent<PlayerStateManager>(); // Get player state manager

            // Simple warning if GameplayHUD is not assigned
            if (gameplayHUD == null)
            {
                Debug.LogWarning("GameplayHUD reference is not set in FPSController. Please assign it in the inspector. Hotbar functionality will be disabled.");
            }

            InitializeMovement();
            InitializeWeapons();

            _sensitivityMultiplierPropertyIndex = _userInput.GetPropertyIndex("SensitivityMultiplier");

            // Subscribe to events
            if (inventoryEvents != null)
            {
                inventoryEvents.OnInputStateChanged += HandleInputStateChanged;
                inventoryEvents.OnAttachmentModeChanged += HandleAttachmentModeChanged;
                inventoryEvents.OnAttachmentModeExit += HandleAttachmentModeExit;
            }
            
            // Subscribe to new player state changes if available
            if (playerStateManager != null)
            {
                playerStateManager.OnStateChanged += HandlePlayerStateChanged;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (inventoryEvents != null)
            {
                inventoryEvents.OnInputStateChanged -= HandleInputStateChanged;
                inventoryEvents.OnAttachmentModeChanged -= HandleAttachmentModeChanged;
                inventoryEvents.OnAttachmentModeExit -= HandleAttachmentModeExit;
            }
            
            // Unsubscribe from player state changes
            if (playerStateManager != null)
            {
                playerStateManager.OnStateChanged -= HandlePlayerStateChanged;
            }
        }
        
        // New method to handle player state changes
        private void HandlePlayerStateChanged(PlayerStateManager.PlayerState newState, PlayerStateManager.PlayerState oldState)
        {
            // Handle any transitions between player states that need special attention
            // For example, animations for weapon lowering when entering certain states
        }

        private void HandleInputStateChanged(InputState newState)
        {
            // Only used for backward compatibility
            switch (newState)
            {
                case InputState.AttachmentMode:
                    _actionState = FPSActionState.AttachmentEditing;
                    break;
                case InputState.Normal:
                    _actionState = FPSActionState.None;
                    break;
                case InputState.ActionLocked:
                    _actionState = FPSActionState.PlayingAnimation;
                    break;
                // No need to handle BuildMode here as we have playerStateManager now
            }
        }

        private void HandleAttachmentModeChanged(int attachmentIndex)
        {
            if (_actionState != FPSActionState.AttachmentEditing) return;
            GetActiveItem()?.OnAttachmentChanged(attachmentIndex);
        }

        private void HandleAttachmentModeExit()
        {
            if (_actionState == FPSActionState.AttachmentEditing)
            {
                inputStateManager.SetState(InputState.Normal);
                _animator.CrossFade(_inspectEndHash, 0.3f);
            }
        }

        private void UnequipWeapon()
        {
            DisableAim();
            _actionState = FPSActionState.WeaponChange;
            GetActiveItem().OnUnEquip();
        }

        public void ResetActionState()
        {
            _actionState = FPSActionState.None;
        }

        private void EquipWeapon()
        {
            if (_instantiatedWeapons.Count == 0 || _activeWeaponIndex < 0 || _activeWeaponIndex >= _instantiatedWeapons.Count)
            {
                Debug.LogError($"EquipWeapon failed: Invalid index ({_activeWeaponIndex}) or empty list.");
                return;
            }

            // Ensure previous weapon is deactivated before activating new one
            if (_previousWeaponIndex >= 0 && _previousWeaponIndex < _instantiatedWeapons.Count && _previousWeaponIndex != _activeWeaponIndex)
            {
                _instantiatedWeapons[_previousWeaponIndex].gameObject.SetActive(false);
            }
            
            var activeItem = GetActiveItem();
            if (activeItem == null)
            {
                Debug.LogError($"EquipWeapon failed: Could not get active item at index {_activeWeaponIndex}");
                return;
            }
            
            activeItem.gameObject.SetActive(true);
            activeItem.OnEquip(gameObject); // This triggers the weapon's sync logic
            _actionState = FPSActionState.None;
            
            // Update ammo status explicitly after equipping
            UpdateAmmoStatus(); 
        }

        private void DisableAim()
        {
            if (GetActiveItem().OnAimReleased()) _aimState = FPSAimState.None;
        }
        
        private bool CanFire()
        {
            // Check with new player state manager first if available
            if (playerStateManager != null)
            {
                // Use the centralized firing permission system
                if (!playerStateManager.CanPlayerFire())
                {
                    return false;
                }
                
                // Additional explicit check for building states
                var currentState = playerStateManager.GetCurrentState();
                if (currentState == PlayerStateManager.PlayerState.Building || 
                    currentState == PlayerStateManager.PlayerState.BuildingMenu)
                {
                    return false;
                }
            }
            
            // Legacy check with InputStateManager
            if (inputStateManager != null && inputStateManager.ShouldBlockWeaponFiring())
            {
                return false;
            }
            
            // Also check action state
            return !HasActiveAction() && !IsSprinting();
        }

        private void OnFirePressed()
        {
            // Double-check state before firing
            if (_instantiatedWeapons.Count == 0 || !CanFire()) return;
            
            // Additional check for building mode using playerStateManager
            if (playerStateManager != null)
            {
                var state = playerStateManager.GetCurrentState();
                if (state == PlayerStateManager.PlayerState.Building || 
                    state == PlayerStateManager.PlayerState.BuildingMenu)
                {
                    return;
                }
            }
            
            // Check if the weapon itself allows firing (handles weapon-specific conditions)
            var weapon = GetActiveItem();
            if (weapon is Demo.Scripts.Runtime.Item.Weapon weaponItem && !weaponItem.CanFire())
            {
                return;
            }
            
            GetActiveItem().OnFirePressed();
        }

        private void OnFireReleased()
        {
            if (_instantiatedWeapons.Count == 0) return;
            GetActiveItem().OnFireReleased();
        }

        private FPSItem GetActiveItem()
        {
            if (_instantiatedWeapons.Count == 0) return null;
            return _instantiatedWeapons[_activeWeaponIndex];
        }
        
        private void OnSlideStarted()
        {
            _animator.CrossFade(_slideHash, 0.2f);
        }
        
        private void OnSprintStarted()
        {
            OnFireReleased();
            DisableAim();

            _aimState = FPSAimState.None;

            _userInput.SetValue(FPSANames.StabilizationWeight, 0f);
            _userInput.SetValue("LookLayerWeight", 0.3f);
        }

        private void OnSprintEnded()
        {
            _userInput.SetValue(FPSANames.StabilizationWeight, 1f);
            _userInput.SetValue("LookLayerWeight", 1f);
        }

        private void OnCrouch()
        {
            PlayTransitionMotion(settings.crouchingMotion);
        }

        private void OnUncrouch()
        {
            PlayTransitionMotion(settings.crouchingMotion);
        }
        
        private bool _isLeaning;

        public void OnChangeWeapon()
        {
            if (_movementComponent.PoseState == FPSPoseState.Prone) return;
            if (HasActiveAction() || _instantiatedWeapons.Count == 0) return;

            var currentCategory = playerLoadout.GetCategory(playerLoadout.currentCategoryIndex);
            if (currentCategory == null) return;

            var targetItem = currentCategory.GetCurrentItem();
            if (targetItem == null) return;

            int newIndex = _instantiatedWeapons.FindIndex(w => w.GetType() == targetItem.GetType());
            if (newIndex == -1) return;

            StartWeaponChange(newIndex);
        }

        public void StartWeaponChange(int newIndex)
        {
            if (newIndex == _activeWeaponIndex || newIndex < 0 || newIndex >= _instantiatedWeapons.Count)
            {
                return;
            }

            // Store the current index before changing
            _previousWeaponIndex = _activeWeaponIndex;
            
            // Call Unequip on the currently active weapon *before* changing the index
            var currentItem = GetActiveItem();
            if (currentItem != null)
            {
                UnequipWeapon(); // This now calls OnUnEquip where state saving should happen
                OnFireReleased(); // Stop firing actions
            }
            else
            {
                // If GetActiveItem failed somehow, reset action state anyway
                 _actionState = FPSActionState.None;
            }
            
            // Set the new active index
            _activeWeaponIndex = newIndex;
            
            // Use Invoke to delay equipping the new weapon
            Invoke(nameof(EquipWeapon), settings.equipDelay);
        }
        
        private void UpdateLookInput()
        {
            float scale = _userInput.GetValue<float>(_sensitivityMultiplierPropertyIndex);
            
            float deltaMouseX = _lookDeltaInput.x * settings.sensitivity * scale;
            float deltaMouseY = -_lookDeltaInput.y * settings.sensitivity * scale;
            
            _playerInput.y += deltaMouseY;
            _playerInput.x += deltaMouseX;
            
            if (_recoilPattern != null)
            {
                _playerInput += _recoilPattern.GetRecoilDelta();
                deltaMouseX += _recoilPattern.GetRecoilDelta().x;
            }
            
            float proneWeight = _animator.GetFloat(_proneWeightHash);
            Vector2 pitchClamp = Vector2.Lerp(new Vector2(-90f, 90f), new Vector2(-30, 0f), proneWeight);

            _playerInput.y = Mathf.Clamp(_playerInput.y, pitchClamp.x, pitchClamp.y);
            
            transform.rotation *= Quaternion.Euler(0f, deltaMouseX, 0f);
            
            _userInput.SetValue(FPSANames.MouseDeltaInput, new Vector4(deltaMouseX, deltaMouseY));
            _userInput.SetValue(FPSANames.MouseInput, new Vector4(_playerInput.x, _playerInput.y));
        }

        private void OnMovementUpdated()
        {
            float playablesWeight = 1f - _animator.GetFloat(_fullBodyWeightHash);
            _userInput.SetValue(FPSANames.PlayablesWeight, playablesWeight);
        }

        private void Update()
        {
            Time.timeScale = settings.timeScale;
            UpdateLookInput();
            OnMovementUpdated();
        }

#if ENABLE_INPUT_SYSTEM
        public void OnReload()
        {
            // Get the current item to avoid null reference issues
            var currentItem = GetActiveItem();
            if (currentItem == null) return;
            
            // Check if item is a weapon and ensure it can fire (is in a valid state)
            if (currentItem is Demo.Scripts.Runtime.Item.Weapon weapon)
            {
                // Make sure the weapon is operational or idle when attempting reload
                if (!weapon.IsOperational && weapon.CurrentState != WeaponState.Reloading)
                {
                    // Force weapon back to idle state if it's not already reloading
                    weapon.SetWeaponState(WeaponState.Idle);
                }
            }
            
            // If executing an action or out of ammo, don't reload
            if (HasActiveAction() || IsOutOfAmmo || !currentItem.OnReload()) return;
            
            _actionState = FPSActionState.PlayingAnimation;
        }

        public void OnThrowGrenade()
        {
            if (IsSprinting()|| HasActiveAction() || !GetActiveItem().OnGrenadeThrow()) return;
            _actionState = FPSActionState.PlayingAnimation;
        }
        
        public void OnFire(InputValue value)
        {
            // Remove the Send Message incompatible parameter
            OnFire();
        }

        // Add new method for Send Message compatibility
        public void OnFire()
        {
            // Check if player can fire through our centralized state system
            if (!CanFire())
            {
                return;
            }
            
            // Double-check building states specifically (to be extra safe)
            if (playerStateManager != null)
            {
                var state = playerStateManager.GetCurrentState();
                if (state == PlayerStateManager.PlayerState.Building || 
                    state == PlayerStateManager.PlayerState.BuildingMenu)
                {
                    return;
                }
            }
            
            // Only block shooting if the hotbar is both active and visible (alpha > 0)
            if (gameplayHUD != null && gameplayHUD.IsHotbarActive() && gameplayHUD.hotbarCanvasGroup.alpha > 0) return;
            
            var currentItem = GetActiveItem();
            
            // Don't try to process hotbar selection via fire button - just fire if we have a weapon
            if (currentItem is Demo.Scripts.Runtime.Item.Weapon || currentItem is ConsumableItem)
            {
                // Handle consumable items - call their primary use method
                if (currentItem is ConsumableItem consumable)
                {
                    consumable.OnPrimaryUse();
                    return;
                }
                
                // Handle regular weapon firing - direct call based on mouse button state
                if (Input.GetMouseButton(0))
                {
                    OnFirePressed();
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    OnFireReleased();
                }
            }
        }

        public void OnAim(InputValue value)
        {
            // Remove the Send Message incompatible parameter
            OnAim();
        }

        // Add new method for Send Message compatibility
        public void OnAim()
        {
            if (IsSprinting()) return;

            var currentItem = GetActiveItem();
            if (currentItem is ConsumableItem consumable)
            {
                if (Input.GetMouseButton(1))
                {
                    consumable.OnSecondaryUse();
                }
                return;
            }

            //change here to right click to exit hotbar mode without aiming down sights, same thing for cancelling buildings, create variable for blocking aiming
            if (Input.GetMouseButton(1) && !IsAiming())
            {
                if (GetActiveItem().OnAimPressed()) _aimState = FPSAimState.Aiming;
                PlayTransitionMotion(settings.aimingMotion);
                return;
            }


            if (Input.GetMouseButtonUp(1) && IsAiming())
            {
                DisableAim();
                PlayTransitionMotion(settings.aimingMotion);
            }
        }

        public void OnLook(InputValue value)
        {
            _lookDeltaInput = value.Get<Vector2>();
        }

        public void OnLean(InputValue value)
        {
            _userInput.SetValue(FPSANames.LeanInput, value.Get<float>() * settings.leanAngle);
            PlayTransitionMotion(settings.leanMotion);
        }

        public void OnCycleScope()
        {
            if (!IsAiming()) return;
            
            GetActiveItem().OnCycleScope();
            PlayTransitionMotion(settings.aimingMotion);
        }

        public void OnChangeFireMode()
        {
            GetActiveItem().OnChangeFireMode();
        }

        public void OnToggleAttachmentEditing()
        {
            if (HasActiveAction() && _actionState != FPSActionState.AttachmentEditing) return;
            
            if (_actionState != FPSActionState.AttachmentEditing)
            {
                inputStateManager.SetState(InputState.AttachmentMode);
                _animator.CrossFade(_inspectStartHash, 0.2f);
            }
            else
            {
                inputStateManager.SetState(InputState.Normal);
                _animator.CrossFade(_inspectEndHash, 0.3f);
            }
        }

        // Replace the individual attachment methods with the original DigitAxis handler
        public void OnDigitAxis(InputValue value)
        {
            if (_actionState != FPSActionState.AttachmentEditing) return;
            
            float digit = value.Get<float>();
            if (digit > 0)
            {
                int attachmentIndex = Mathf.RoundToInt(digit);
                HandleAttachmentModeChanged(attachmentIndex);
            }
        }

        public void OnSelectPrimary(InputValue value)
        {
            if (value.isPressed) playerLoadout.SelectCategory(0);
        }

        public void OnSelectSecondary(InputValue value)
        {
            if (value.isPressed) playerLoadout.SelectCategory(1);
        }

        public void OnSelectThrowables(InputValue value)
        {
            if (value.isPressed) playerLoadout.SelectCategory(2);
        }

        public void OnSelectSpecialEquipment(InputValue value)
        {
            if (value.isPressed) playerLoadout.SelectCategory(3);
        }

        public void OnSelectMedical(InputValue value)
        {
            if (value.isPressed) playerLoadout.SelectCategory(4);
        }

        public void OnSelectTools(InputValue value)
        {
            if (value.isPressed) playerLoadout.SelectCategory(5);
        }
#endif

        /*
        public void OnUseConsumable(InputValue value)
        {
            if (IsSprinting() || HasActiveAction()) return;
            
            var currentItem = GetActiveItem();
            if (currentItem is ConsumableItem consumable)
            {
                consumable.OnPrimaryUse();
            }
        }

        public void OnAlternateUseConsumable(InputValue value)
        {
            if (IsSprinting() || HasActiveAction()) return;
            
            var currentItem = GetActiveItem();
            if (currentItem is ConsumableItem consumable)
            {
                consumable.OnSecondaryUse();
            }
        }
        */

        // New method to update ammo status
        public void UpdateAmmoStatus()
        {
            var playerLoadout = GetComponent<PlayerLoadout>();
            if (playerLoadout != null)
            {
                // Check if current weapon has any magazines with ammo
                IsOutOfAmmo = !playerLoadout.CurrentWeaponHasAmmo();
            }
        }
    }
}