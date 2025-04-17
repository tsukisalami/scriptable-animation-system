using UnityEngine;
using System.Collections;
using Ballistics;

public class Artillery : MonoBehaviour
{
    [Header("Artillery Components")]
    [Tooltip("The transform that will rotate (usually the upper part of the artillery)")]
    public Transform swivelRoot;
    [Tooltip("Reference to the Weapon component for firing")]
    private Weapon weapon;
    
    [Header("Firing Settings")]
    [Tooltip("Minimum time between shots")]
    public float minFireInterval = 2f;
    [Tooltip("Maximum time between shots")]
    public float maxFireInterval = 5f;
    [Tooltip("Whether the artillery is currently active and can fire")]
    public bool canFire = true;

    [Header("Swivel Settings")]
    [Tooltip("Whether the artillery should automatically rotate")]
    public bool autoSwivel = true;
    [Tooltip("Rotation speed in degrees per second")]
    public float swivelSpeed = 45f;
    [Tooltip("Minimum rotation angle")]
    public float minSwivelAngle = -45f;
    [Tooltip("Maximum rotation angle")]
    public float maxSwivelAngle = 45f;
    [Tooltip("How long to wait at the rotation limits before changing direction")]
    public float pauseAtLimits = 1f;

    // Private variables
    private bool isMovingClockwise = true;
    private float currentRotation = 0f;
    private float pauseTimer = 0f;
    private bool isPaused = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Artillery: Starting initialization...");
        
        // Validate components
        if (swivelRoot == null)
        {
            Debug.LogError("Artillery: No swivel root assigned!");
            enabled = false;
            return;
        }

        // Get the weapon component
        weapon = GetComponent<Weapon>();
        if (weapon == null)
        {
            Debug.LogError("Artillery: No Weapon component found!");
            enabled = false;
            return;
        }

        if (weapon.BulletSpawnPoint == null)
        {
            Debug.LogError("Artillery: Weapon's BulletSpawnPoint is not assigned!");
            enabled = false;
            return;
        }

        Debug.Log("Artillery: All components validated successfully");

        // Start the firing coroutine
        StartCoroutine(FireRoutine());
        Debug.Log("Artillery: FireRoutine started");
        
        // Initialize rotation
        currentRotation = swivelRoot.localEulerAngles.y;
    }

    // Update is called once per frame
    void Update()
    {
        if (autoSwivel && swivelRoot != null)
        {
            HandleSwivel();
        }
    }

    private void HandleSwivel()
    {
        if (isPaused)
        {
            pauseTimer += Time.deltaTime;
            if (pauseTimer >= pauseAtLimits)
            {
                isPaused = false;
                isMovingClockwise = !isMovingClockwise;
            }
            return;
        }

        // Calculate new rotation
        float rotationAmount = swivelSpeed * Time.deltaTime * (isMovingClockwise ? 1 : -1);
        currentRotation += rotationAmount;

        // Check bounds
        if (currentRotation >= maxSwivelAngle || currentRotation <= minSwivelAngle)
        {
            currentRotation = Mathf.Clamp(currentRotation, minSwivelAngle, maxSwivelAngle);
            isPaused = true;
            pauseTimer = 0f;
        }

        // Apply rotation
        Vector3 newRotation = swivelRoot.localEulerAngles;
        newRotation.y = currentRotation;
        swivelRoot.localEulerAngles = newRotation;
    }

    private IEnumerator FireRoutine()
    {
        Debug.Log("Artillery: FireRoutine entered");
        while (true)
        {
            if (canFire)
            {
                Debug.Log("Artillery: Attempting to fire...");
                Fire();
                float randomInterval = Random.Range(minFireInterval, maxFireInterval);
                Debug.Log($"Artillery: Waiting for {randomInterval} seconds until next shot");
                yield return new WaitForSeconds(randomInterval);
            }
            else
            {
                Debug.Log("Artillery: Cannot fire, waiting...");
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private void Fire()
    {
        if (weapon != null)
        {
            Debug.Log("Artillery: Calling weapon.Shoot()");
            try
            {
                weapon.Shoot();
                Debug.Log($"Artillery: Successfully fired at rotation: {currentRotation}");
                OnArtilleryFired();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Artillery: Error during weapon.Shoot(): {e.Message}");
            }
        }
        else
        {
            Debug.LogError("Artillery: Weapon reference is null during Fire()!");
        }
    }

    // Event handler for firing - useful for adding effects, sounds, etc.
    protected virtual void OnArtilleryFired()
    {
        // Placeholder for effects
        // Example: Spawn muzzle flash
        // Example: Play sound effect
        // Example: Camera shake
    }

    // Public methods for external control
    public void SetAutoSwivel(bool enabled)
    {
        autoSwivel = enabled;
    }

    public void SetCanFire(bool enabled)
    {
        canFire = enabled;
    }

    public void SetSwivelSpeed(float speed)
    {
        swivelSpeed = speed;
    }

    // Optional: Add method to manually control rotation
    public void SetRotation(float angle)
    {
        if (!autoSwivel && swivelRoot != null)
        {
            currentRotation = Mathf.Clamp(angle, minSwivelAngle, maxSwivelAngle);
            Vector3 newRotation = swivelRoot.localEulerAngles;
            newRotation.y = currentRotation;
            swivelRoot.localEulerAngles = newRotation;
        }
    }
}
