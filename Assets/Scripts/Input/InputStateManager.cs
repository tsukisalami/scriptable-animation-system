using UnityEngine;
using UnityEngine.InputSystem;

public class InputStateManager : MonoBehaviour
{
    [SerializeField] private InventoryEvents inventoryEvents;
    private InputState currentState = InputState.Normal;
    private PlayerInput playerInput;
    
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
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
        if (currentState == newState) return;
        
        // Update state and notify listeners
        currentState = newState;
        inventoryEvents?.RaiseInputStateChanged(newState);

        if (playerInput == null) return;

        switch (newState)
        {
            case InputState.Normal:
                // Enable all action maps in normal state
                foreach (var actionMap in playerInput.actions.actionMaps)
                {
                    actionMap.Enable();
                }
                break;

            case InputState.AttachmentMode:
                // Keep both gameplay and attachment maps active
                playerInput.actions.FindActionMap("Gameplay").Enable();
                playerInput.actions.FindActionMap("Attachments").Enable();
                break;

            case InputState.HotbarActive:
                // Keep both gameplay and inventory maps active
                playerInput.actions.FindActionMap("Gameplay").Enable();
                playerInput.actions.FindActionMap("Inventory").Enable();
                break;

            case InputState.ActionLocked:
                // During animations, only keep essential controls
                playerInput.actions.FindActionMap("Gameplay").Enable();
                break;
        }
        
        // Log state changes for debugging
        Debug.Log($"Input State changed to: {newState}");
    }

    public InputState GetCurrentState() => currentState;
} 