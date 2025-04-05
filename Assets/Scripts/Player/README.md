# Player State Manager

A robust, centralized solution for managing player states, input restrictions, and UI behaviors.

## Overview

The `PlayerStateManager` provides a single point of truth for determining the player's current state (normal gameplay, building, menu, etc.) and controlling all related settings. It handles:

- Cursor visibility and locking
- Camera movement restrictions
- Input action enabling/disabling
- Movement restrictions
- Firing restrictions
- Interaction restrictions
- UI behavior coordination

## Getting Started

### Setup

1. Add the `PlayerStateManager` component to your player GameObject
2. Set up the state settings in the inspector for each state
3. Reference `PlayerStateManager` in components that need to check or change player state

### Basic Usage

To change the player's state:

```csharp
// Get a reference to PlayerStateManager
private PlayerStateManager playerStateManager;

// Switch to building menu state
playerStateManager.SetState(PlayerStateManager.PlayerState.BuildingMenu);

// Switch back to normal gameplay
playerStateManager.SetState(PlayerStateManager.PlayerState.Normal);

// Revert to previous state
playerStateManager.RevertToPreviousState();
```

To check the current state:

```csharp
// Check if player can fire weapon
if (playerStateManager.CanPlayerFire())
{
    FireWeapon();
}

// Or check the exact state
if (playerStateManager.GetCurrentState() == PlayerStateManager.PlayerState.Building)
{
    // Do something specific to building state
}
```

### State Change Events

Subscribe to state change events:

```csharp
private void Start()
{
    playerStateManager.OnStateChanged += HandlePlayerStateChanged;
}

private void OnDestroy()
{
    playerStateManager.OnStateChanged -= HandlePlayerStateChanged;
}

private void HandlePlayerStateChanged(PlayerStateManager.PlayerState newState, PlayerStateManager.PlayerState oldState)
{
    // Handle state change
    if (newState == PlayerStateManager.PlayerState.Hotbar)
    {
        ShowHotbarUI();
    }
    else if (oldState == PlayerStateManager.PlayerState.Hotbar)
    {
        HideHotbarUI();
    }
}
```

## Adding New Player States

To add a new player state:

1. Add a new value to the `PlayerState` enum in `PlayerStateManager.cs`
2. Add a new `StateSettings` field in `PlayerStateManager.cs`
3. Initialize default settings in the `CreateDefaultStateSettings` method
4. Add the state to the `stateConfigs` dictionary in `InitializeStateConfigurations`
5. Update any relevant helper methods (like `CanPlayerFire()`)

## Integration with Legacy Systems

For backward compatibility, `PlayerStateManager` includes a bridge to the legacy `InputStateManager` system. If you need to communicate with legacy components:

```csharp
// Use the PlayerStateManager's converter to get the equivalent legacy state
InputState legacyState = ConvertToLegacyInputState(currentState);

// Then raise the event through the inventory events system
inventoryEvents.RaiseInputStateChanged(legacyState);
```

## Sample Implementations

Check out the sample implementations to see how to integrate different mechanics:

- `ActionStateSample.cs` - Mounted weapon integration
- `SwimmingStateSample.cs` - Swimming mechanics
- `BuildSystem.cs` - Building placement system

## Best Practices

1. **Always handle state cleanup**: When transitioning away from a state, make sure to clean up any UI elements or effects.

2. **Subscribe to state changes**: Rather than checking the state every frame, subscribe to the `OnStateChanged` event.

3. **Use helper methods**: Use the provided helper methods like `CanPlayerFire()` instead of directly checking the state.

4. **Set appropriate defaults**: Make sure the default settings for each state are sensible to avoid unexpected behavior.

5. **Consider state history**: The `RevertToPreviousState()` method can be useful for temporary states like menus or tooltips. 