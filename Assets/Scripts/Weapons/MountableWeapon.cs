using UnityEngine;
using Ballistics;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using Demo.Scripts.Runtime.Item;

public class MountableWeapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform mountPosition;
    [SerializeField] private Vector2 horizontalRotationLimits = new Vector2(-45f, 45f);
    [SerializeField] private Vector2 verticalRotationLimits = new Vector2(-20f, 60f);
    
    [Header("Weapon Components")]
    [SerializeField] private Ballistics.Weapon ballisticsWeapon;

    // Player references
    private GameObject mountedPlayer;
    private PlayerReferences playerReferences;
    private Vector2 currentRotation;
    private bool isMounted;
    
    // Animation references
    private FPSAnimator playerFPSAnimator;
    private FPSAnimatorEntity weaponAnimatorEntity;
    private FPSItem previousWeapon;
    
    private void Start()
    {
        if (ballisticsWeapon == null)
            ballisticsWeapon = GetComponent<Ballistics.Weapon>();

        if (weaponPivot == null || mountPosition == null)
        {
            Debug.LogError("MountableWeapon: WeaponPivot or MountPosition is not assigned!", this);
            enabled = false;
        }

        // Get the weapon's animator entity
        weaponAnimatorEntity = GetComponent<FPSAnimatorEntity>();
        if (weaponAnimatorEntity == null)
        {
            Debug.LogError("MountableWeapon: No FPSAnimatorEntity found on weapon!", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // Subscribe to health state changes if player is mounted
        if (playerReferences?.Health != null)
        {
            playerReferences.Health.OnHealthStateChanged.AddListener(HandleHealthStateChanged);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from health state changes
        if (playerReferences?.Health != null)
        {
            playerReferences.Health.OnHealthStateChanged.RemoveListener(HandleHealthStateChanged);
        }
    }
    
    public void Mount(GameObject player)
    {
        if (isMounted || player == null) 
        {
            Debug.Log("Mount failed - Already mounted or null player");
            return;
        }
        
        Debug.Log($"Attempting to mount player {player.name}");

        // Get player references
        playerReferences = player.GetComponent<PlayerReferences>();
        if (playerReferences == null)
        {
            Debug.LogError("Mount failed - PlayerReferences component not found!");
            return;
        }

        // Get FPS Animator
        playerFPSAnimator = player.GetComponent<FPSAnimator>();
        if (playerFPSAnimator == null)
        {
            Debug.LogError("Mount failed - FPSAnimator component not found!");
            return;
        }

        // Verify required references
        if (playerReferences.Health == null || 
            playerReferences.CharacterController == null || 
            playerReferences.HipsObject == null)
        {
            Debug.LogError("Mount failed - Missing required player components!");
            return;
        }

        // Store the current weapon and hide it
        var fpsController = player.GetComponent<Demo.Scripts.Runtime.Character.FPSController>();
        if (fpsController != null && fpsController._instantiatedWeapons.Count > 0)
        {
            previousWeapon = fpsController._instantiatedWeapons[fpsController._activeWeaponIndex];
            if (previousWeapon != null)
            {
                previousWeapon.gameObject.SetActive(false);
            }
        }

        // Link the mounted weapon's animator profile
        playerFPSAnimator.LinkAnimatorProfile(gameObject);

        // Subscribe to health state changes
        playerReferences.Health.OnHealthStateChanged.AddListener(HandleHealthStateChanged);
        
        mountedPlayer = player;
        isMounted = true;
        
        // Position the player at the mount position
        player.transform.position = mountPosition.position;
        player.transform.rotation = mountPosition.rotation;
        
        // Disable character controller
        playerReferences.CharacterController.enabled = false;

        // Reset rotation
        currentRotation = Vector2.zero;
        Debug.Log("Mount successful");
    }
    
    public void Unmount()
    {
        if (!isMounted) return;
        
        Debug.Log("Unmounting player");

        // Restore previous weapon and its animator profile
        if (previousWeapon != null)
        {
            previousWeapon.gameObject.SetActive(true);
            if (playerFPSAnimator != null)
            {
                playerFPSAnimator.LinkAnimatorProfile(previousWeapon.gameObject);
            }
        }

        // Unsubscribe from health state changes
        if (playerReferences?.Health != null)
        {
            playerReferences.Health.OnHealthStateChanged.RemoveListener(HandleHealthStateChanged);
        }
        
        // Re-enable character controller
        if (playerReferences?.CharacterController != null)
        {
            playerReferences.CharacterController.enabled = true;
        }
        
        mountedPlayer = null;
        playerReferences = null;
        playerFPSAnimator = null;
        previousWeapon = null;
        isMounted = false;
        currentRotation = Vector2.zero;
    }

    private void HandleHealthStateChanged(PlayerHealth.HealthState newState)
    {
        // Unmount if player is downed or dead
        if (newState == PlayerHealth.HealthState.Downed || newState == PlayerHealth.HealthState.Dead)
        {
            Unmount();
        }
    }
    
    private void Update()
    {
        if (!isMounted || mountedPlayer == null) return;

        // Lock hips position to mount position
        if (playerReferences?.HipsObject != null)
        {
            playerReferences.HipsObject.transform.position = mountPosition.position;
        }
        
        // Get mouse input for weapon rotation
        var mouseX = Input.GetAxis("Mouse X");
        var mouseY = Input.GetAxis("Mouse Y");
        
        // Update rotation with clamping
        currentRotation.x += mouseX;
        currentRotation.y -= mouseY;
        
        currentRotation.x = Mathf.Clamp(currentRotation.x, horizontalRotationLimits.x, horizontalRotationLimits.y);
        currentRotation.y = Mathf.Clamp(currentRotation.y, verticalRotationLimits.x, verticalRotationLimits.y);
        
        // Apply rotation to weapon pivot
        weaponPivot.localRotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0f);
        
        // Handle firing
        if (Input.GetMouseButton(0) && ballisticsWeapon != null)
        {
            ballisticsWeapon.Shoot(weaponPivot.forward);
        }
    }
    
    public bool IsMounted() => isMounted;

    private void OnDestroy()
    {
        // Cleanup event subscription
        if (playerReferences?.Health != null)
        {
            playerReferences.Health.OnHealthStateChanged.RemoveListener(HandleHealthStateChanged);
        }
    }
} 