using UnityEngine;
using Ballistics;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using Demo.Scripts.Runtime.Item;
using Demo.Scripts.Runtime.Character;

public class MountableWeapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform mountPosition;
    [SerializeField] private Transform dismountPosition;
    [SerializeField] private Vector2 horizontalRotationLimits = new Vector2(-45f, 45f);
    [SerializeField] private Vector2 verticalRotationLimits = new Vector2(-20f, 60f);
    
    [Header("Weapon Components")]
    [SerializeField] private Ballistics.Weapon ballisticsWeapon;

    // Player references
    private GameObject mountedPlayer;
    private PlayerReferences playerReferences;
    private Vector2 currentRotation;
    private bool isMounted;
    
    // Animation and controller references
    private FPSAnimator playerFPSAnimator;
    private FPSAnimatorEntity weaponAnimatorEntity;
    private FPSItem previousWeapon;
    private FPSController playerFPSController;
    private Vector3 originalPlayerRotation;
    
    private void Start()
    {
        if (ballisticsWeapon == null)
            ballisticsWeapon = GetComponent<Ballistics.Weapon>();

        if (weaponPivot == null || mountPosition == null || dismountPosition == null)
        {
            Debug.LogError("MountableWeapon: WeaponPivot, MountPosition, or DismountPosition is not assigned!", this);
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
        playerFPSController = player.GetComponent<FPSController>();

        if (playerReferences == null || playerFPSController == null)
        {
            Debug.LogError("Mount failed - Required player components not found!");
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

        // Store original rotation
        originalPlayerRotation = player.transform.eulerAngles;

        // Store the current weapon and hide it
        if (playerFPSController._instantiatedWeapons.Count > 0)
        {
            previousWeapon = playerFPSController._instantiatedWeapons[playerFPSController._activeWeaponIndex];
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
        
        // Disable character controller and movement
        playerReferences.CharacterController.enabled = false;
        if (playerFPSController != null)
        {
            playerFPSController._actionState = FPSActionState.PlayingAnimation; // Prevent weapon switching and other actions
        }

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

        // Move player to dismount position
        if (mountedPlayer != null && dismountPosition != null)
        {
            mountedPlayer.transform.position = dismountPosition.position;
            mountedPlayer.transform.rotation = dismountPosition.rotation;
        }
        
        // Re-enable character controller and movement
        if (playerReferences?.CharacterController != null)
        {
            playerReferences.CharacterController.enabled = true;
        }

        // Restore FPS controller state
        if (playerFPSController != null)
        {
            playerFPSController._actionState = FPSActionState.None;
            playerFPSController.ResetActionState();
        }
        
        mountedPlayer = null;
        playerReferences = null;
        playerFPSAnimator = null;
        playerFPSController = null;
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (dismountPosition == null)
        {
            Debug.LogWarning("MountableWeapon: DismountPosition is not assigned! Players will have nowhere to go when dismounting.", this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw mount position
        if (mountPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(mountPosition.position, 0.3f);
            Gizmos.DrawLine(transform.position, mountPosition.position);
        }

        // Draw dismount position
        if (dismountPosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(dismountPosition.position, 0.3f);
            Gizmos.DrawLine(transform.position, dismountPosition.position);
        }
    }
#endif
} 