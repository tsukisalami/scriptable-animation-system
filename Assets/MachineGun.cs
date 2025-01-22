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

    // Private variables for movement control
    private float currentHorizontalAngle = 0f;
    private float currentVerticalAngle = 0f;
    private float targetHorizontalAngle;
    private float targetVerticalAngle;
    private float nextFireTime;
    private float fireInterval;

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
        // Check if we're close to target angles
        float horizontalDistance = Mathf.Abs(targetHorizontalAngle - currentHorizontalAngle);
        float verticalDistance = Mathf.Abs(targetVerticalAngle - currentVerticalAngle);

        if (horizontalDistance < 1f && verticalDistance < 1f)
        {
            SetNewTargetAngles();
        }

        // Update current angles
        currentHorizontalAngle = Mathf.MoveTowards(currentHorizontalAngle, targetHorizontalAngle, 
            horizontalSweepSpeed * Time.deltaTime);
        currentVerticalAngle = Mathf.MoveTowards(currentVerticalAngle, targetVerticalAngle, 
            verticalSweepSpeed * Time.deltaTime);

        // Apply rotation
        Vector3 newRotation = gunMount.localEulerAngles;
        newRotation.y = currentHorizontalAngle;
        newRotation.x = currentVerticalAngle;
        gunMount.localEulerAngles = newRotation;
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

        if (Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireInterval;
        }
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