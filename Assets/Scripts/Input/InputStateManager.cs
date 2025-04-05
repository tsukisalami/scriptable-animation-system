using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Legacy input state manager, maintained for backward compatibility.
/// New code should use PlayerStateManager instead.
/// </summary>
public class InputStateManager : MonoBehaviour
{
    [SerializeField] private InventoryEvents inventoryEvents;
    private InputState currentState = InputState.Normal;
    private PlayerInput playerInput;
    private PlayerStateManager playerStateManager; // Reference to new state manager
    
    // Add a flag to track if the build menu is open (radial menu)
    private bool isBuildMenuOpen = false;
    
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerStateManager = GetComponent<PlayerStateManager>();
        
        if (playerInput != null)
        {
            // Enable all action maps by default
            foreach (var actionMap in playerInput.actions.actionMaps)
            {
                actionMap.Enable();
            }
        }
        // Start in normal state
        SetState(InputState.Normal);
    }

    public void SetState(InputState newState)
    {
        if (currentState == newState && 
            (newState != InputState.BuildMode || isBuildMenuOpen == isBuildMenuOpen)) 
            return;
        
        // Update state and notify listeners
        currentState = newState;
        
        // If we have the new state manager, try to use it instead
        if (playerStateManager != null)
        {
            // Convert to new state enum and set it
            PlayerStateManager.PlayerState newPlayerState = ConvertToPlayerState(newState, isBuildMenuOpen);
            playerStateManager.SetState(newPlayerState);
            
            // Let the state manager handle the input maps
            // We still notify via events for backward compatibility
            inventoryEvents?.RaiseInputStateChanged(newState);
            return;
        }
        
        // Legacy code path when no PlayerStateManager is available
        inventoryEvents?.RaiseInputStateChanged(newState);

        if (playerInput == null) return;

        switch (newState)
        {
            case InputState.Normal:
                // Enable all action maps in normal state
                isBuildMenuOpen = false;
                foreach (var actionMap in playerInput.actions.actionMaps)
                {
                    actionMap.Enable();
                }
                break;

            case InputState.AttachmentMode:
                // Keep both gameplay and attachment maps active
                isBuildMenuOpen = false;
                playerInput.actions.FindActionMap("Gameplay").Enable();
                playerInput.actions.FindActionMap("Attachments").Enable();
                break;

            case InputState.HotbarActive:
                // Keep both gameplay and inventory maps active
                isBuildMenuOpen = false;
                playerInput.actions.FindActionMap("Gameplay").Enable();
                playerInput.actions.FindActionMap("Inventory").Enable();
                break;

            case InputState.ActionLocked:
                // During animations, only keep essential controls
                isBuildMenuOpen = false;
                playerInput.actions.FindActionMap("Gameplay").Enable();
                break;
                
            case InputState.BuildMode:
                // Handle build mode differently based on whether the menu is open
                var gameplayMap = playerInput.actions.FindActionMap("Gameplay");
                
                // First disable all action maps
                foreach (var actionMap in playerInput.actions.actionMaps)
                {
                    actionMap.Disable();
                }
                
                // If gameplay map exists, enable it with restrictions
                if (gameplayMap != null)
                {
                    gameplayMap.Enable();
                    
                    // Always disable combat-related actions in build mode
                    DisableActionIfFound(gameplayMap, "Fire");
                    DisableActionIfFound(gameplayMap, "Aim");
                    DisableActionIfFound(gameplayMap, "Reload");
                    DisableActionIfFound(gameplayMap, "ThrowGrenade");
                    DisableActionIfFound(gameplayMap, "ChangeFireMode");
                    DisableActionIfFound(gameplayMap, "CycleScope");
                    DisableActionIfFound(gameplayMap, "Lean"); // Disable leaning so Q/E can be used for rotation
                    
                    // When build menu is open, completely disable camera movement
                    if (isBuildMenuOpen)
                    {
                        DisableActionIfFound(gameplayMap, "Look");
                        DisableActionIfFound(gameplayMap, "Mouse"); // Also disable any mouse-related actions
                    }
                    else
                    {
                        // When just placing building, allow looking around
                        EnableActionIfFound(gameplayMap, "Look");
                    }
                    
                    // Always enable these controls
                    EnableActionIfFound(gameplayMap, "Move");
                    EnableActionIfFound(gameplayMap, "Jump");
                    EnableActionIfFound(gameplayMap, "Sprint");
                    EnableActionIfFound(gameplayMap, "Crouch");
                    EnableActionIfFound(gameplayMap, "ToggleBuild");
                }
                
                // If a Building action map exists, enable it
                var buildingMap = playerInput.actions.FindActionMap("Building");
                if (buildingMap != null)
                {
                    buildingMap.Enable();
                }
                
                break;
        }
    }
    
    // Set the build menu state - called from BuildSystem
    public void SetBuildMenuOpen(bool isOpen)
    {
        // Only process if there's a change
        if (isBuildMenuOpen != isOpen)
        {
            isBuildMenuOpen = isOpen;
            
            // If we have the new state manager, update it directly
            if (playerStateManager != null)
            {
                if (currentState == InputState.BuildMode)
                {
                    // Set the appropriate building state based on menu open/closed
                    playerStateManager.SetState(isOpen ? 
                        PlayerStateManager.PlayerState.BuildingMenu : 
                        PlayerStateManager.PlayerState.Building);
                }
                return;
            }
            
            // Legacy path when no PlayerStateManager is available
            // If we're in build mode, update the state to reflect the menu change
            if (currentState == InputState.BuildMode)
            {
                // Re-apply the build mode state to update action restrictions
                SetState(InputState.BuildMode);
            }
        }
    }
    
    // Helper methods to safely enable/disable actions if they exist
    private void EnableActionIfFound(InputActionMap actionMap, string actionName)
    {
        var action = actionMap.FindAction(actionName);
        if (action != null) 
        {
            action.Enable();
        }
    }
    
    private void DisableActionIfFound(InputActionMap actionMap, string actionName)
    {
        var action = actionMap.FindAction(actionName);
        if (action != null)
        {
            action.Disable();
        }
    }

    public InputState GetCurrentState() => currentState;
    
    // Check if we're currently in a state where weapon firing should be blocked
    public bool ShouldBlockWeaponFiring()
    {
        // If we have the new state manager, use it
        if (playerStateManager != null)
        {
            return !playerStateManager.CanPlayerFire();
        }
        
        // Legacy check
        // Always block firing in build mode, whether in menu or placement phase
        if (currentState == InputState.BuildMode) return true;
        
        // Also block in other states
        return currentState == InputState.HotbarActive || 
               currentState == InputState.AttachmentMode || 
               currentState == InputState.ActionLocked;
    }
    
    // Helper method to convert old InputState to new PlayerState
    private PlayerStateManager.PlayerState ConvertToPlayerState(InputState legacyState, bool buildMenuOpen)
    {
        switch (legacyState)
        {
            case InputState.Normal:
                return PlayerStateManager.PlayerState.Normal;
            case InputState.HotbarActive:
                return PlayerStateManager.PlayerState.Hotbar;
            case InputState.AttachmentMode:
                return PlayerStateManager.PlayerState.Attachment;
            case InputState.ActionLocked:
                return PlayerStateManager.PlayerState.Animation;
            case InputState.BuildMode:
                return buildMenuOpen ? 
                    PlayerStateManager.PlayerState.BuildingMenu : 
                    PlayerStateManager.PlayerState.Building;
            default:
                return PlayerStateManager.PlayerState.Normal;
        }
    }
} 