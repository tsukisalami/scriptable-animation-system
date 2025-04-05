using UnityEngine;
using UnityEngine.InputSystem;
using Ballistics; // Add namespace for LivingEntity

/// <summary>
/// This is a sample implementation showing how to integrate new gameplay mechanics 
/// with the PlayerStateManager. This example handles interactions with a mounted weapon.
/// </summary>
public class ActionStateSample : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStateManager playerStateManager;
    
    [Header("Sample Gun Settings")]
    [SerializeField] private Transform mountedWeaponPivot;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float maxPitch = 20f;
    [SerializeField] private float minPitch = -5f;
    
    // State tracking
    private bool isUsingMountedWeapon = false;
    private Vector2 lookInput;
    
    private void Start()
    {
        // Find PlayerStateManager if not already assigned
        if (playerStateManager == null)
        {
            playerStateManager = GetComponent<PlayerStateManager>();
            
            if (playerStateManager == null)
            {
                Debug.LogError("ActionStateSample: PlayerStateManager not found! This component requires PlayerStateManager.");
                enabled = false;
                return;
            }
        }
        
        // Subscribe to state changes to detect when we exit vehicle state
        playerStateManager.OnStateChanged += HandlePlayerStateChanged;
    }
    
    private void OnDestroy()
    {
        if (playerStateManager != null)
        {
            playerStateManager.OnStateChanged -= HandlePlayerStateChanged;
        }
    }
    
    private void HandlePlayerStateChanged(PlayerStateManager.PlayerState newState, PlayerStateManager.PlayerState oldState)
    {
        // If we were in vehicle state and changed to something else, clean up
        if (oldState == PlayerStateManager.PlayerState.Vehicle && isUsingMountedWeapon)
        {
            ExitMountedWeapon();
        }
    }
    
    // Called from mounted weapon interactable
    public void InteractWithMountedWeapon()
    {
        if (isUsingMountedWeapon) return;
        
        // Enter mounted weapon state
        isUsingMountedWeapon = true;
        
        // Tell the state manager we're in a vehicle
        playerStateManager.SetState(PlayerStateManager.PlayerState.Vehicle);
        
        // The PlayerStateManager will handle cursor locking, input restrictions, etc.
        
        // Enable any special weapon UI
        Debug.Log("Mounted weapon UI enabled");
    }
    
    // Called when player presses the exit key or by state manager
    public void ExitMountedWeapon()
    {
        if (!isUsingMountedWeapon) return;
        
        isUsingMountedWeapon = false;
        
        // Return to normal state (only if we're still in Vehicle state)
        if (playerStateManager.GetCurrentState() == PlayerStateManager.PlayerState.Vehicle)
        {
            playerStateManager.SetState(PlayerStateManager.PlayerState.Normal);
        }
        
        // Disable any special weapon UI
        Debug.Log("Mounted weapon UI disabled");
    }
    
    // Input handling while using mounted weapon
    public void OnLook(InputValue value)
    {
        // Only process look input if we're using the mounted weapon
        if (!isUsingMountedWeapon) return;
        
        lookInput = value.Get<Vector2>();
    }
    
    public void OnFire(InputValue value)
    {
        // Only process fire input if we're using the mounted weapon
        if (!isUsingMountedWeapon) return;
        
        if (value.isPressed)
        {
            FireMountedWeapon();
        }
    }
    
    public void OnExit(InputValue value)
    {
        // Only process exit input if we're using the mounted weapon
        if (!isUsingMountedWeapon) return;
        
        if (value.isPressed)
        {
            ExitMountedWeapon();
        }
    }
    
    // Sample mounted weapon controls
    private void Update()
    {
        if (!isUsingMountedWeapon || mountedWeaponPivot == null) return;
        
        // Rotate mounted weapon based on look input
        float yaw = lookInput.x * rotationSpeed;
        float pitch = -lookInput.y * rotationSpeed;
        
        // Apply rotation to the weapon pivot
        mountedWeaponPivot.Rotate(Vector3.up, yaw, Space.World);
        
        // For pitch, we need to clamp the values
        Vector3 currentEuler = mountedWeaponPivot.localEulerAngles;
        float currentPitch = currentEuler.x;
        
        // Convert from 0-360 to -180/180 for easier clamping
        if (currentPitch > 180f)
            currentPitch -= 360f;
        
        // Apply new pitch with clamping
        float newPitch = Mathf.Clamp(currentPitch + pitch, minPitch, maxPitch);
        mountedWeaponPivot.localEulerAngles = new Vector3(newPitch, currentEuler.y, currentEuler.z);
    }
    
    private void FireMountedWeapon()
    {
        // Sample implementation of firing logic
        Debug.Log("Mounted weapon fired!");
        
        // Add particle effects, sound, etc.
        
        // Add raycast for hit detection
        if (Physics.Raycast(mountedWeaponPivot.position, mountedWeaponPivot.forward, out RaycastHit hit, 100f))
        {
            Debug.Log($"Hit {hit.collider.gameObject.name} at distance {hit.distance}");
            
            // Option 1: Apply damage to PlayerHealth if it has one
            var playerHealth = hit.collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Properly handle the hit with the right method - PlayerHealth expects to be called with HandleHit
                playerHealth.HandleHit(25f, PlayerHealth.HitLocation.Chest);
                return;
            }
            
            // Option 2: Apply damage to LivingEntity directly if it has one
            var livingEntity = hit.collider.GetComponent<LivingEntity>();
            if (livingEntity != null)
            {
                livingEntity.ApplyDamage(25f);
            }
        }
    }
}

/// <summary>
/// This is a sample interaction component that allows players to enter a mounted weapon
/// </summary>
public class MountedWeaponInteractable : MonoBehaviour
{
    [SerializeField] private ActionStateSample mountedWeaponController;
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        var playerStateManager = other.GetComponent<PlayerStateManager>();
        if (playerStateManager == null) return;
        
        // Only allow interaction if the player is in normal state
        if (playerStateManager.GetCurrentState() == PlayerStateManager.PlayerState.Normal)
        {
            // Show interaction prompt
            Debug.Log("Press E to use mounted weapon");
        }
    }
    
    public void Interact()
    {
        if (mountedWeaponController != null)
        {
            mountedWeaponController.InteractWithMountedWeapon();
        }
    }
} 