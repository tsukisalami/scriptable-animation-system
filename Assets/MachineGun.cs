using UnityEngine;
using System.Collections;
using Ballistics;

public class MachineGun : MonoBehaviour
{
    [Header("Machine Gun Components")]
    [Tooltip("The transform that will rotate (the gun mount)")]
    public Transform gunMount;
    private Weapon weapon;
    
    [Header("Firing Settings")]
    [Tooltip("Rate of fire in rounds per minute")]
    public float roundsPerMinute = 600f;
    [Tooltip("Whether the machine gun is currently active and can fire")]
    public bool canFire = true;

    [Header("Horizontal Swivel Settings")]
    [Tooltip("Minimum horizontal angle in degrees")]
    public float minHorizontalAngle = -45f;
    [Tooltip("Maximum horizontal angle in degrees")]
    public float maxHorizontalAngle = 45f;
    [Tooltip("How fast the gun sweeps horizontally in degrees per second")]
    public float horizontalSweepSpeed = 30f;
    
    [Header("Vertical Swivel Settings")]
    [Tooltip("Minimum vertical angle in degrees")]
    public float minVerticalAngle = -10f;
    [Tooltip("Maximum vertical angle in degrees")]
    public float maxVerticalAngle = 30f;
    [Tooltip("How fast the gun sweeps vertically in degrees per second")]
    public float verticalSweepSpeed = 15f;

    [Header("Burst Fire Settings")]
    [Tooltip("Minimum duration of pause between bursts (seconds)")]
    public float minBurstPause = 6f;
    [Tooltip("Maximum duration of pause between bursts (seconds)")]
    public float maxBurstPause = 20f;
    [Tooltip("Minimum duration of each burst (seconds)")]
    public float minBurstDuration = 0.5f;
    [Tooltip("Maximum duration of each burst (seconds)")]
    public float maxBurstDuration = 3f;

    [Header("Player Following Settings")]
    [Tooltip("Whether the gun should follow the player instead of random movement")]
    public bool followPlayer = false;
    [Tooltip("Reference to the player's transform")]
    public Transform playerTransform;
    [Tooltip("How quickly the gun tracks the player (degrees per second)")]
    public float playerTrackingSpeed = 45f;

    [Header("Raycast Settings")]
    [Tooltip("Layer mask for raycast detection")]
    public LayerMask targetLayers;
    [Tooltip("Maximum distance to detect player")]
    public float detectionRange = 50f;
    [Tooltip("How often to perform target detection (seconds)")]
    public float detectionInterval = 0.2f;
    private float nextDetectionTime;

    // Private variables for movement control
    private float currentHorizontalAngle = 0f;
    private float currentVerticalAngle = 0f;
    private float targetHorizontalAngle;
    private float targetVerticalAngle;
    private float nextFireTime;
    private float fireInterval;

    // Private variables for burst control
    private float nextBurstTime;
    private float currentBurstEndTime;
    private bool isBursting = false;

    void Start()
    {
        Debug.Log("MachineGun: Starting initialization...");
        
        // Validate components
        if (gunMount == null)
        {
            Debug.LogError("MachineGun: No gun mount assigned!");
            enabled = false;
            return;
        }

        // Get the weapon component
        weapon = GetComponent<Weapon>();
        if (weapon == null)
        {
            Debug.LogError("MachineGun: No Weapon component found!");
            enabled = false;
            return;
        }

        if (weapon.BulletSpawnPoint == null)
        {
            Debug.LogError("MachineGun: Weapon's BulletSpawnPoint is not assigned!");
            enabled = false;
            return;
        }

        // Calculate fire interval from RPM
        fireInterval = 60f / roundsPerMinute;
        
        // Initialize angles
        currentHorizontalAngle = gunMount.localEulerAngles.y;
        currentVerticalAngle = gunMount.localEulerAngles.x;
        SetNewTargetAngles();

        // Initialize burst timing
        SetNextBurstTime();

        if (followPlayer && playerTransform == null)
        {
            Debug.LogWarning("MachineGun: Follow Player is enabled but no player transform is assigned!");
            // Try to find player by tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("MachineGun: Found player transform automatically");
            }
        }

        Debug.Log("MachineGun: Initialization complete");
    }

    void Update()
    {
        if (!enabled) return;

        UpdateGunMovement();
        HandleFiring();
    }

    private void UpdateGunMovement()
    {
        if (followPlayer && playerTransform != null)
        {
            // Check if it's time for a new detection
            if (Time.time >= nextDetectionTime)
            {
                // Cast a ray towards the player to check visibility
                Vector3 directionToPlayer = playerTransform.position - gunMount.position;
                RaycastHit hit;
                bool canSeePlayer = Physics.Raycast(gunMount.position, directionToPlayer, out hit, detectionRange, targetLayers);

                if (canSeePlayer && hit.transform == playerTransform)
                {
                    // Get the look rotation towards the player
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                    
                    // Convert to local space if we have a parent
                    if (gunMount.parent != null)
                    {
                        targetRotation = Quaternion.Inverse(gunMount.parent.rotation) * targetRotation;
                    }

                    // Extract the euler angles
                    Vector3 targetEulerAngles = targetRotation.eulerAngles;

                    // Normalize angles to -180 to 180 range
                    float targetHorizontal = targetEulerAngles.y;
                    if (targetHorizontal > 180f) targetHorizontal -= 360f;
                    
                    float targetVertical = targetEulerAngles.x;
                    if (targetVertical > 180f) targetVertical -= 360f;

                    // Clamp to allowed ranges
                    targetHorizontal = Mathf.Clamp(targetHorizontal, minHorizontalAngle, maxHorizontalAngle);
                    targetVertical = Mathf.Clamp(targetVertical, minVerticalAngle, maxVerticalAngle);

                    // Smoothly rotate towards target
                    currentHorizontalAngle = Mathf.MoveTowards(currentHorizontalAngle, targetHorizontal, 
                        playerTrackingSpeed * Time.deltaTime);
                    currentVerticalAngle = Mathf.MoveTowards(currentVerticalAngle, targetVertical, 
                        playerTrackingSpeed * Time.deltaTime);
                }

                nextDetectionTime = Time.time + detectionInterval;
            }

            // Apply the current rotation
            gunMount.localRotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0);
        }
        else
        {
            // Original random movement logic
            float horizontalDistance = Mathf.Abs(targetHorizontalAngle - currentHorizontalAngle);
            float verticalDistance = Mathf.Abs(targetVerticalAngle - currentVerticalAngle);

            if (horizontalDistance < 1f && verticalDistance < 1f)
            {
                SetNewTargetAngles();
            }

            currentHorizontalAngle = Mathf.MoveTowards(currentHorizontalAngle, targetHorizontalAngle, 
                horizontalSweepSpeed * Time.deltaTime);
            currentVerticalAngle = Mathf.MoveTowards(currentVerticalAngle, targetVerticalAngle, 
                verticalSweepSpeed * Time.deltaTime);

            // Apply rotation
            gunMount.localRotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0);
        }
    }

    private void SetNewTargetAngles()
    {
        // Set new random target angles within the allowed ranges
        targetHorizontalAngle = Random.Range(minHorizontalAngle, maxHorizontalAngle);
        targetVerticalAngle = Random.Range(minVerticalAngle, maxVerticalAngle);
    }

    private void HandleFiring()
    {
        if (!canFire) return;

        // Check if it's time to start a new burst
        if (!isBursting && Time.time >= nextBurstTime)
        {
            isBursting = true;
            currentBurstEndTime = Time.time + Random.Range(minBurstDuration, maxBurstDuration);
        }

        // Check if current burst should end
        if (isBursting && Time.time >= currentBurstEndTime)
        {
            isBursting = false;
            SetNextBurstTime();
        }

        // Only fire if we're in a burst
        if (isBursting && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireInterval;
        }
    }

    private void SetNextBurstTime()
    {
        nextBurstTime = Time.time + Random.Range(minBurstPause, maxBurstPause);
    }

    private void Fire()
    {
        if (weapon != null)
        {
            try
            {
                weapon.Shoot();
                OnMachineGunFired();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"MachineGun: Error during weapon.Shoot(): {e.Message}");
            }
        }
    }

    protected virtual void OnMachineGunFired()
    {
        // Placeholder for effects (muzzle flash, sound, etc.)
    }

    // Public control methods
    public void SetCanFire(bool enabled)
    {
        canFire = enabled;
    }

    public void SetRoundsPerMinute(float rpm)
    {
        roundsPerMinute = rpm;
        fireInterval = 60f / roundsPerMinute;
    }

    public void SetHorizontalLimits(float min, float max)
    {
        minHorizontalAngle = min;
        maxHorizontalAngle = max;
    }

    public void SetVerticalLimits(float min, float max)
    {
        minVerticalAngle = min;
        maxVerticalAngle = max;
    }
} 