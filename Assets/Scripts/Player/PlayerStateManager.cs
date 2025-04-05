using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

/// <summary>
/// Core player state manager that centralizes player state control.
/// Handles state transitions, input restrictions, cursor behavior, and notifications to other systems.
/// </summary>
public class PlayerStateManager : MonoBehaviour
{
    // Public state enum that all systems can reference
    public enum PlayerState
    {
        Normal,             // Regular gameplay
        Menu,               // In-game menu (inventory, options, etc.)
        Dialog,             // Talking to NPCs
        Building,           // Placing buildings
        BuildingMenu,       // Selecting building from radial menu
        Attachment,         // Modifying weapon attachments
        Animation,          // During locked animations
        Dead,               // Player is dead
        Vehicle,            // Inside a vehicle
        Hotbar,             // Hotbar selection
        Paused              // Game paused
    }

    [System.Serializable]
    public class StateSettings
    {
        public bool lockCursor = true;
        public bool showCursor = false;
        public bool allowCameraMovement = true;
        public bool allowMovement = true;
        public bool allowFiring = true;
        public bool allowWeaponSwitch = true;
        public bool allowInteractions = true;
        public bool useUIInput = false;
    }

    [Header("State Configuration")]
    [SerializeField] private Dictionary<PlayerState, StateSettings> stateConfigs = new Dictionary<PlayerState, StateSettings>();
    [SerializeField] private StateSettings normalStateSettings;
    [SerializeField] private StateSettings menuStateSettings;
    [SerializeField] private StateSettings dialogStateSettings;
    [SerializeField] private StateSettings buildingStateSettings;
    [SerializeField] private StateSettings buildingMenuStateSettings;
    [SerializeField] private StateSettings attachmentStateSettings;
    [SerializeField] private StateSettings animationStateSettings;
    [SerializeField] private StateSettings deadStateSettings;
    [SerializeField] private StateSettings vehicleStateSettings;
    [SerializeField] private StateSettings hotbarStateSettings;
    [SerializeField] private StateSettings pausedStateSettings;

    [Header("References")]
    [SerializeField] private InventoryEvents inventoryEvents;
    
    // Components
    private PlayerInput playerInput;

    // State tracking
    private PlayerState currentState = PlayerState.Normal;
    private PlayerState previousState = PlayerState.Normal;
    private Stack<PlayerState> stateHistory = new Stack<PlayerState>();
    
    // Events
    public event Action<PlayerState, PlayerState> OnStateChanged; // (newState, oldState)

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        InitializeStateConfigurations();
    }

    private void Start()
    {
        // Apply initial state
        SetState(PlayerState.Normal);
    }

    private void InitializeStateConfigurations()
    {
        // Add all the state settings to our dictionary for easy lookup
        stateConfigs[PlayerState.Normal] = normalStateSettings ?? CreateDefaultStateSettings(PlayerState.Normal);
        stateConfigs[PlayerState.Menu] = menuStateSettings ?? CreateDefaultStateSettings(PlayerState.Menu);
        stateConfigs[PlayerState.Dialog] = dialogStateSettings ?? CreateDefaultStateSettings(PlayerState.Dialog);
        stateConfigs[PlayerState.Building] = buildingStateSettings ?? CreateDefaultStateSettings(PlayerState.Building);
        stateConfigs[PlayerState.BuildingMenu] = buildingMenuStateSettings ?? CreateDefaultStateSettings(PlayerState.BuildingMenu);
        stateConfigs[PlayerState.Attachment] = attachmentStateSettings ?? CreateDefaultStateSettings(PlayerState.Attachment);
        stateConfigs[PlayerState.Animation] = animationStateSettings ?? CreateDefaultStateSettings(PlayerState.Animation);
        stateConfigs[PlayerState.Dead] = deadStateSettings ?? CreateDefaultStateSettings(PlayerState.Dead);
        stateConfigs[PlayerState.Vehicle] = vehicleStateSettings ?? CreateDefaultStateSettings(PlayerState.Vehicle);
        stateConfigs[PlayerState.Hotbar] = hotbarStateSettings ?? CreateDefaultStateSettings(PlayerState.Hotbar);
        stateConfigs[PlayerState.Paused] = pausedStateSettings ?? CreateDefaultStateSettings(PlayerState.Paused);
    }

    private StateSettings CreateDefaultStateSettings(PlayerState state)
    {
        // Create default settings based on the state type
        StateSettings settings = new StateSettings();
        
        switch (state)
        {
            case PlayerState.Normal:
                settings.lockCursor = true;
                settings.showCursor = false;
                settings.allowCameraMovement = true;
                settings.allowMovement = true;
                settings.allowFiring = true;
                settings.allowWeaponSwitch = true;
                settings.allowInteractions = true;
                settings.useUIInput = false;
                break;
                
            case PlayerState.Menu:
            case PlayerState.Paused:
                settings.lockCursor = false;
                settings.showCursor = true;
                settings.allowCameraMovement = false;
                settings.allowMovement = false;
                settings.allowFiring = false;
                settings.allowWeaponSwitch = false;
                settings.allowInteractions = false;
                settings.useUIInput = true;
                break;
                
            case PlayerState.BuildingMenu:
                settings.lockCursor = false;
                settings.showCursor = true;
                settings.allowCameraMovement = false;
                settings.allowMovement = false;
                settings.allowFiring = false;
                settings.allowWeaponSwitch = false;
                settings.allowInteractions = false;
                settings.useUIInput = true;
                break;
                
            case PlayerState.Building:
                settings.lockCursor = true;
                settings.showCursor = false;
                settings.allowCameraMovement = true;
                settings.allowMovement = true;
                settings.allowFiring = false;
                settings.allowWeaponSwitch = false;
                settings.allowInteractions = true;
                settings.useUIInput = false;
                break;
                
            case PlayerState.Attachment:
                settings.lockCursor = true;
                settings.showCursor = false;
                settings.allowCameraMovement = true;
                settings.allowMovement = false;
                settings.allowFiring = false;
                settings.allowWeaponSwitch = false;
                settings.allowInteractions = false;
                settings.useUIInput = false;
                break;
                
            case PlayerState.Animation:
                settings.lockCursor = true;
                settings.showCursor = false;
                settings.allowCameraMovement = true;
                settings.allowMovement = false;
                settings.allowFiring = false;
                settings.allowWeaponSwitch = false;
                settings.allowInteractions = false;
                settings.useUIInput = false;
                break;
                
            case PlayerState.Hotbar:
                settings.lockCursor = true;
                settings.showCursor = false;
                settings.allowCameraMovement = true;
                settings.allowMovement = true;
                settings.allowFiring = false;
                settings.allowWeaponSwitch = true;
                settings.allowInteractions = true;
                settings.useUIInput = false;
                break;
                
            default:
                settings.lockCursor = true;
                settings.showCursor = false;
                settings.allowCameraMovement = true;
                settings.allowMovement = true;
                settings.allowFiring = true;
                settings.allowWeaponSwitch = true;
                settings.allowInteractions = true;
                settings.useUIInput = false;
                break;
        }
        
        return settings;
    }

    public void SetState(PlayerState newState)
    {
        if (newState == currentState)
            return;

        PlayerState oldState = currentState;
        previousState = oldState;
        stateHistory.Push(oldState);
        currentState = newState;

        // Apply new state configuration
        ApplyStateSettings(newState);
        
        // Update InputSystem action maps
        UpdateInputMaps(newState);
        
        // Broadcast state change to other systems
        OnStateChanged?.Invoke(newState, oldState);
        
        // Update legacy input state system via InventoryEvents for backward compatibility
        if (inventoryEvents != null)
        {
            InputState legacyState = ConvertToLegacyInputState(newState);
            inventoryEvents.RaiseInputStateChanged(legacyState);
        }
        
        Debug.Log($"Player state changed from {oldState} to {newState}");
    }

    public void RevertToPreviousState()
    {
        if (stateHistory.Count > 0)
        {
            PlayerState previousState = stateHistory.Pop();
            SetState(previousState);
        }
        else
        {
            // If no history, default to normal state
            SetState(PlayerState.Normal);
        }
    }

    private void ApplyStateSettings(PlayerState state)
    {
        if (!stateConfigs.TryGetValue(state, out StateSettings settings))
        {
            Debug.LogWarning($"No settings found for state {state}, using defaults");
            settings = CreateDefaultStateSettings(state);
        }

        // Apply cursor settings
        Cursor.visible = settings.showCursor;
        Cursor.lockState = settings.lockCursor ? CursorLockMode.Locked : CursorLockMode.Confined;
    }

    private void UpdateInputMaps(PlayerState state)
    {
        if (playerInput == null) return;

        // First disable all action maps
        foreach (var actionMap in playerInput.actions.actionMaps)
        {
            actionMap.Disable();
        }

        // Get settings for the current state
        StateSettings settings = stateConfigs[state];

        // Enable the appropriate action maps based on the state
        switch (state)
        {
            case PlayerState.Normal:
                EnableActionMap("Gameplay");
                break;
                
            case PlayerState.BuildingMenu:
                EnableActionMap("Gameplay", restrictedActions: new[] { "Fire", "Aim", "Reload", "ThrowGrenade", "ChangeFireMode", "CycleScope", "Look" });
                EnableActionMap("Building");
                break;
                
            case PlayerState.Building:
                EnableActionMap("Gameplay", restrictedActions: new[] { "Fire", "Aim", "Reload", "ThrowGrenade", "ChangeFireMode", "CycleScope", "Lean" });
                EnableActionMap("Building");
                break;
                
            case PlayerState.Attachment:
                EnableActionMap("Gameplay", restrictedActions: new[] { "Fire", "Reload", "ThrowGrenade", "ChangeFireMode", "CycleScope" });
                EnableActionMap("Attachments");
                break;
                
            case PlayerState.Hotbar:
                EnableActionMap("Gameplay");
                EnableActionMap("Inventory");
                break;
                
            case PlayerState.Menu:
            case PlayerState.Paused:
                EnableActionMap("UI");
                break;
                
            case PlayerState.Animation:
                EnableActionMap("Gameplay", restrictedActions: new[] { "Fire", "Aim", "Reload", "ThrowGrenade", "ChangeFireMode", "CycleScope" });
                break;
                
            default:
                EnableActionMap("Gameplay");
                break;
        }
    }

    private void EnableActionMap(string mapName, string[] restrictedActions = null)
    {
        var actionMap = playerInput.actions.FindActionMap(mapName);
        if (actionMap != null)
        {
            actionMap.Enable();
            
            // If we have actions to restrict
            if (restrictedActions != null)
            {
                foreach (string actionName in restrictedActions)
                {
                    var action = actionMap.FindAction(actionName);
                    if (action != null)
                    {
                        action.Disable();
                    }
                }
            }
        }
    }

    // Bridge between new state system and legacy system
    private InputState ConvertToLegacyInputState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Normal:
                return InputState.Normal;
            case PlayerState.Hotbar:
                return InputState.HotbarActive;
            case PlayerState.Attachment:
                return InputState.AttachmentMode;
            case PlayerState.Animation:
                return InputState.ActionLocked;
            case PlayerState.Building:
            case PlayerState.BuildingMenu:
                return InputState.BuildMode;
            default:
                return InputState.Normal;
        }
    }

    // Helper methods for other systems to check player state
    public PlayerState GetCurrentState() => currentState;
    
    public bool CanPlayerFire() => currentState != PlayerState.Normal ? stateConfigs[currentState].allowFiring : true;
    
    public bool CanPlayerMove() => currentState != PlayerState.Normal ? stateConfigs[currentState].allowMovement : true;
    
    public bool CanPlayerInteract() => currentState != PlayerState.Normal ? stateConfigs[currentState].allowInteractions : true;
    
    public bool CanPlayerSwitchWeapons() => currentState != PlayerState.Normal ? stateConfigs[currentState].allowWeaponSwitch : true;
    
    public bool CanPlayerLook() => currentState != PlayerState.Normal ? stateConfigs[currentState].allowCameraMovement : true;
} 