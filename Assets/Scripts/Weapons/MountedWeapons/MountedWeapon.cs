using UnityEngine;

public class MountedWeapon : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode useKey = KeyCode.E;
    
    [Header("Weapon Movement")]
    [SerializeField] private float horizontalRotationLimit = 45f; // Degrees left/right
    [SerializeField] private float verticalRotationLimit = 30f;   // Degrees up/down
    [SerializeField] private float rotationSpeed = 2f;
    
    [Header("References")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform playerMountPosition;
    
    private bool isOccupied;
    private GameObject currentUser;
    private Vector3 originalRotation;
    private float currentHorizontalAngle;
    private float currentVerticalAngle;

    private void Start()
    {
        originalRotation = weaponPivot.localEulerAngles;
    }

    private void Update()
    {
        if (!isOccupied)
        {
            CheckForInteraction();
            return;
        }

        HandleWeaponControl();
    }

    private void CheckForInteraction()
    {
        // Check if player is in range and presses use key
        if (Input.GetKeyDown(useKey))
        {
            GameObject nearbyPlayer = FindNearbyPlayer();
            if (nearbyPlayer != null)
            {
                MountPlayer(nearbyPlayer);
            }
        }
    }

    private GameObject FindNearbyPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(playerMountPosition.position, player.transform.position);
            if (distance <= interactionDistance)
            {
                return player;
            }
        }
        return null;
    }

    private void MountPlayer(GameObject player)
    {
        currentUser = player;
        isOccupied = true;
        
        // Disable player's movement (you'll need to implement this through your own system)
        // player.DisableMovement();
        
        // Position and parent player at mount position
        player.transform.position = playerMountPosition.position;
        player.transform.rotation = playerMountPosition.rotation;
        player.transform.parent = playerMountPosition;
    }

    private void HandleWeaponControl()
    {
        if (Input.GetKeyDown(useKey))
        {
            DismountPlayer();
            return;
        }

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

        // Update angles with clamping
        currentHorizontalAngle = Mathf.Clamp(currentHorizontalAngle + mouseX, -horizontalRotationLimit, horizontalRotationLimit);
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle - mouseY, -verticalRotationLimit, verticalRotationLimit);

        // Apply rotation (using Z-axis for vertical movement)
        weaponPivot.localEulerAngles = new Vector3(
            originalRotation.x,
            originalRotation.y + currentHorizontalAngle,
            originalRotation.z + currentVerticalAngle
        );

        // Keep player level while rotating horizontally
        if (currentUser != null)
        {
            Vector3 playerEulerAngles = currentUser.transform.localEulerAngles;
            playerEulerAngles.x = 0;
            playerEulerAngles.z = 0;
            currentUser.transform.localEulerAngles = playerEulerAngles;
        }
    }

    private void DismountPlayer()
    {
        if (currentUser != null)
        {
            // Unparent the player before resetting
            currentUser.transform.parent = null;
            
            // Re-enable player's movement (you'll need to implement this through your own system)
            // currentUser.EnableMovement();
            
            // Reset weapon state
            isOccupied = false;
            currentUser = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Skip if playerMountPosition is not set
        if (playerMountPosition == null) return;
        
        // Visualize interaction range in editor centered on mount position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerMountPosition.position, interactionDistance);
    }
}