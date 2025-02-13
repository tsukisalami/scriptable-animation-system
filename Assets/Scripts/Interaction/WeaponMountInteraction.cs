using UnityEngine;

[RequireComponent(typeof(MountableWeapon))]
public class WeaponMountInteraction : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private LayerMask playerLayer;
    
    private MountableWeapon mountableWeapon;
    private bool showDebugSphere = true;
    
    private void Start()
    {
        mountableWeapon = GetComponent<MountableWeapon>();
        if (mountableWeapon == null)
        {
            Debug.LogError("WeaponMountInteraction: No MountableWeapon component found!", this);
            enabled = false;
            return;
        }

        // Validate layer mask
        if (playerLayer.value == 0)
        {
            Debug.LogError("WeaponMountInteraction: Player Layer not set! Please set it in the inspector.", this);
            showDebugSphere = true;
        }
        
        Debug.Log($"WeaponMountInteraction initialized with player layer mask: {playerLayer.value}");
    }
    
    private void Update()
    {
        if (!Input.GetKeyDown(interactKey)) return;
        
        if (mountableWeapon.IsMounted())
        {
            Debug.Log("WeaponMountInteraction: Unmounting weapon");
            mountableWeapon.Unmount();
            return;
        }
        
        Debug.Log($"WeaponMountInteraction: Checking for players within {interactionDistance} units");
        var colliders = Physics.OverlapSphere(transform.position, interactionDistance, playerLayer);
        
        if (colliders.Length == 0)
        {
            // More detailed debug info
            var allColliders = Physics.OverlapSphere(transform.position, interactionDistance);
            Debug.Log($"WeaponMountInteraction: No players found in range. Found {allColliders.Length} total colliders:");
            foreach (var col in allColliders)
            {
                Debug.Log($"Found object: {col.gameObject.name} on layer: {LayerMask.LayerToName(col.gameObject.layer)}");
            }
            return;
        }

        foreach (var col in colliders)
        {
            Debug.Log($"WeaponMountInteraction: Found player object {col.gameObject.name} on layer {LayerMask.LayerToName(col.gameObject.layer)}");
            mountableWeapon.Mount(col.gameObject);
            break;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugSphere) return;
        
        // Always show the interaction sphere in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Draw the mount position if available
        var weapon = GetComponent<MountableWeapon>();
        if (weapon != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Warn if player layer mask is not set correctly
        if (playerLayer.value == 0)
        {
            Debug.LogWarning("WeaponMountInteraction: Player Layer is not set!", this);
        }
    }
#endif
} 