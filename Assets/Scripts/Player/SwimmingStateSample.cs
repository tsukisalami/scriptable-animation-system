using UnityEngine;
using UnityEngine.InputSystem;
using Ballistics; // Add namespace for LivingEntity

/// <summary>
/// Sample implementation showing how swimming mechanics could be integrated with PlayerStateManager.
/// This demonstrates how a completely different movement mechanic can be implemented using the state system.
/// </summary>
public class SwimmingStateSample : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStateManager playerStateManager;
    [SerializeField] private CharacterController characterController;
    
    [Header("Swim Settings")]
    [SerializeField] private float swimSpeed = 2f;
    [SerializeField] private float swimUpSpeed = 1.5f;
    [SerializeField] private float swimDownSpeed = 1.5f;
    [SerializeField] private float waterDrag = 2f;
    [SerializeField] private float underwaterFogDensity = 0.05f;
    [SerializeField] private Color underwaterFogColor = new Color(0, 0.4f, 0.7f);
    [SerializeField] private GameObject swimUI; // UI elements shown when swimming
    [SerializeField] private float oxygenCapacity = 100f; // How much oxygen the player can hold
    [SerializeField] private float oxygenDepletionRate = 5f; // Oxygen lost per second
    
    // State tracking
    private bool isSwimming = false;
    private bool isUnderwater = false;
    private float currentOxygen;
    private Vector2 moveInput;
    private float verticalInput;
    private Vector3 swimVelocity;
    
    // Cache original scene settings
    private float originalFogDensity;
    private Color originalFogColor;
    private bool originalFogEnabled;
    
    private void Start()
    {
        // Find PlayerStateManager if not already assigned
        if (playerStateManager == null)
        {
            playerStateManager = GetComponent<PlayerStateManager>();
            
            if (playerStateManager == null)
            {
                Debug.LogError("SwimmingStateSample: PlayerStateManager not found! This component requires PlayerStateManager.");
                enabled = false;
                return;
            }
        }
        
        // Get character controller if not assigned
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
            
            if (characterController == null)
            {
                Debug.LogError("SwimmingStateSample: CharacterController not found! This component requires CharacterController.");
                enabled = false;
                return;
            }
        }
        
        // Subscribe to state changes
        playerStateManager.OnStateChanged += HandlePlayerStateChanged;
        
        // Initialize oxygen to full
        currentOxygen = oxygenCapacity;
        
        // Hide swim UI initially
        if (swimUI != null)
        {
            swimUI.SetActive(false);
        }
    }
    
    private void OnDestroy()
    {
        if (playerStateManager != null)
        {
            playerStateManager.OnStateChanged -= HandlePlayerStateChanged;
        }
        
        // Restore fog settings if we're still swimming when destroyed
        if (isSwimming && isUnderwater)
        {
            RestoreEnvironmentSettings();
        }
    }
    
    private void HandlePlayerStateChanged(PlayerStateManager.PlayerState newState, PlayerStateManager.PlayerState oldState)
    {
        // Handle exiting swim state from an external source
        if (oldState == PlayerStateManager.PlayerState.Vehicle && newState != PlayerStateManager.PlayerState.Vehicle && isSwimming)
        {
            ExitSwimState();
        }
    }
    
    // Called when player enters water trigger volume
    public void EnterWater(bool fullySubmerged)
    {
        if (isSwimming) return;
        
        // Enter swimming state
        isSwimming = true;
        isUnderwater = fullySubmerged;
        
        // Capture original environment settings
        CaptureEnvironmentSettings();
        
        // Apply underwater effects if submerged
        if (fullySubmerged)
        {
            ApplyUnderwaterEffects();
        }
        
        // Tell the state manager we're in a "vehicle" (water is treated as a special vehicle)
        playerStateManager.SetState(PlayerStateManager.PlayerState.Vehicle);
        
        // Show swim UI
        if (swimUI != null)
        {
            swimUI.SetActive(true);
        }
        
        // Reset swim velocity
        swimVelocity = Vector3.zero;
        
        Debug.Log("Entered swimming state: " + (fullySubmerged ? "underwater" : "on surface"));
    }
    
    // Called when player leaves water trigger volume
    public void ExitWater()
    {
        ExitSwimState();
    }
    
    private void ExitSwimState()
    {
        if (!isSwimming) return;
        
        isSwimming = false;
        isUnderwater = false;
        
        // Restore environment settings
        RestoreEnvironmentSettings();
        
        // Return to normal state
        playerStateManager.SetState(PlayerStateManager.PlayerState.Normal);
        
        // Hide swim UI
        if (swimUI != null)
        {
            swimUI.SetActive(false);
        }
        
        Debug.Log("Exited swimming state");
    }
    
    // Called when player transitions between surface/underwater
    public void SetSubmersion(bool isSubmerged)
    {
        if (!isSwimming) return;
        
        // Only process changes in submersion
        if (isSubmerged == isUnderwater) return;
        
        isUnderwater = isSubmerged;
        
        if (isSubmerged)
        {
            ApplyUnderwaterEffects();
            Debug.Log("Went underwater");
        }
        else
        {
            RestoreEnvironmentSettings();
            // Refill oxygen when surfacing
            currentOxygen = oxygenCapacity;
            Debug.Log("Surfaced from underwater");
        }
    }
    
    private void CaptureEnvironmentSettings()
    {
        // Capture fog settings before changing them
        originalFogEnabled = RenderSettings.fog;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogColor = RenderSettings.fogColor;
    }
    
    private void ApplyUnderwaterEffects()
    {
        // Apply underwater fog
        RenderSettings.fog = true;
        RenderSettings.fogDensity = underwaterFogDensity;
        RenderSettings.fogColor = underwaterFogColor;
        
        // Could also add underwater post-processing effects here
    }
    
    private void RestoreEnvironmentSettings()
    {
        // Restore original fog settings
        RenderSettings.fog = originalFogEnabled;
        RenderSettings.fogDensity = originalFogDensity;
        RenderSettings.fogColor = originalFogColor;
    }
    
    // Input handling for swimming
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
    
    public void OnJump(InputValue value)
    {
        // In swimming, jump is used to swim up
        verticalInput = value.isPressed ? 1.0f : 0.0f;
    }
    
    public void OnCrouch(InputValue value)
    {
        // In swimming, crouch is used to swim down
        verticalInput = value.isPressed ? -1.0f : 0.0f;
    }
    
    private void Update()
    {
        if (!isSwimming) return;
        
        // Handle oxygen depletion when underwater
        if (isUnderwater)
        {
            currentOxygen -= oxygenDepletionRate * Time.deltaTime;
            
            if (currentOxygen <= 0)
            {
                currentOxygen = 0;
                // Start drowning damage
                TakeDrowningDamage();
            }
            
            // Update oxygen UI
            UpdateOxygenUI();
        }
        
        // Handle swimming movement
        HandleSwimMovement();
    }
    
    private void HandleSwimMovement()
    {
        // Get camera transform for movement direction
        Transform cameraTransform = Camera.main.transform;
        
        // Calculate move direction relative to camera
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        moveDirection = cameraTransform.TransformDirection(moveDirection);
        moveDirection.y = 0; // Remove any vertical component from camera direction
        moveDirection.Normalize();
        
        // Add vertical movement (from jump/crouch buttons)
        Vector3 verticalDirection = Vector3.up * verticalInput;
        
        // Calculate target velocity
        Vector3 targetVelocity = moveDirection * swimSpeed;
        
        // Apply vertical movement
        if (verticalInput > 0)
            targetVelocity += Vector3.up * swimUpSpeed;
        else if (verticalInput < 0)
            targetVelocity += Vector3.down * swimDownSpeed;
        
        // Apply water drag (smooth transition to target velocity)
        swimVelocity = Vector3.Lerp(swimVelocity, targetVelocity, Time.deltaTime * waterDrag);
        
        // Move the character
        characterController.Move(swimVelocity * Time.deltaTime);
    }
    
    private void TakeDrowningDamage()
    {
        // Apply drowning damage to player
        var playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // Get the LivingEntity component directly if possible
            var livingEntity = GetComponent<LivingEntity>();
            if (livingEntity != null)
            {
                // Apply damage directly to the LivingEntity component
                livingEntity.ApplyDamage(5f * Time.deltaTime);
            }
            else
            {
                // Use HandleHit as an alternative if LivingEntity is not accessible
                playerHealth.HandleHit(5f * Time.deltaTime, PlayerHealth.HitLocation.Chest);
            }
        }
    }
    
    private void UpdateOxygenUI()
    {
        // Update oxygen UI here
        // For a progress bar:
        // oxygenBar.fillAmount = currentOxygen / oxygenCapacity;
        
        Debug.Log($"Oxygen: {currentOxygen:F1}/{oxygenCapacity}");
    }
}

/// <summary>
/// A simple trigger volume to detect when player enters water
/// </summary>
public class WaterVolume : MonoBehaviour
{
    [SerializeField] private float surfaceHeight = 0.5f; // Height of water surface relative to the volume's Y position
    
    private void OnTriggerEnter(Collider other)
    {
        var swimmingController = other.GetComponent<SwimmingStateSample>();
        if (swimmingController == null) return;
        
        // Determine if player is fully submerged based on player height vs. water surface height
        bool fullySubmerged = IsPlayerSubmerged(other.transform.position);
        swimmingController.EnterWater(fullySubmerged);
    }
    
    private void OnTriggerExit(Collider other)
    {
        var swimmingController = other.GetComponent<SwimmingStateSample>();
        if (swimmingController != null)
        {
            swimmingController.ExitWater();
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        var swimmingController = other.GetComponent<SwimmingStateSample>();
        if (swimmingController == null) return;
        
        // Check if submersion state has changed
        bool fullySubmerged = IsPlayerSubmerged(other.transform.position);
        swimmingController.SetSubmersion(fullySubmerged);
    }
    
    private bool IsPlayerSubmerged(Vector3 playerPosition)
    {
        // Calculate water surface world position
        float waterSurfaceY = transform.position.y + surfaceHeight;
        
        // Player is submerged if below water surface (add offset for eyes/camera)
        return playerPosition.y + 1.6f < waterSurfaceY; // 1.6m is approximate eye height
    }
} 