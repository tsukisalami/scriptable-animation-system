using UnityEngine;
using Demo.Scripts.Runtime.Character;
using Demo.Scripts.Runtime.Item;

public class WeaponDropHandler : MonoBehaviour
{
    [Header("Layer Settings")]
    [SerializeField, Tooltip("Layer to set the weapon to when dropped")]
    private LayerMask droppedWeaponLayer;
    
    private PlayerHealth playerHealth;
    private FPSController fpsController;
    
    private FPSItem droppedWeapon;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Rigidbody[] cachedRigidbodies;
    private Collider[] cachedColliders;
    private bool[] originalKinematicStates;
    private bool[] originalColliderStates;
    
    private void Start()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (fpsController == null) fpsController = GetComponent<FPSController>();
        
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDied.AddListener(HandleWeaponDrop);
            playerHealth.OnPlayerDowned.AddListener(HandleWeaponDrop);
            playerHealth.OnPlayerRevived.AddListener(HandlePlayerRevive);
        }
    }

    private void HandleWeaponDrop()
    {
        if (fpsController == null || fpsController._instantiatedWeapons.Count == 0) return;
        
        // Get the currently active weapon
        var activeWeapon = fpsController._instantiatedWeapons[fpsController._activeWeaponIndex];
        if (activeWeapon == null) return;

        // If we're already handling this weapon, don't do it again
        if (droppedWeapon == activeWeapon) return;
        
        droppedWeapon = activeWeapon;
        
        // Store original transform data
        originalParent = droppedWeapon.transform.parent;
        originalLocalPosition = droppedWeapon.transform.localPosition;
        originalLocalRotation = droppedWeapon.transform.localRotation;
        
        // Cache all physics components
        cachedRigidbodies = droppedWeapon.GetComponentsInChildren<Rigidbody>(true);
        cachedColliders = droppedWeapon.GetComponentsInChildren<Collider>(true);
        
        // Store original states and layers
        originalKinematicStates = new bool[cachedRigidbodies.Length];
        originalColliderStates = new bool[cachedColliders.Length];
        
        for (int i = 0; i < cachedRigidbodies.Length; i++)
        {
            originalKinematicStates[i] = cachedRigidbodies[i].isKinematic;
        }
        
        for (int i = 0; i < cachedColliders.Length; i++)
        {
            originalColliderStates[i] = cachedColliders[i].enabled;
        }
        
        // Detach from parent
        droppedWeapon.transform.SetParent(null);
        
        // Get player velocity before enabling physics
        var playerVelocity = fpsController.GetComponent<CharacterController>().velocity;
        
        // Enable physics on all components
        foreach (var rb in cachedRigidbodies)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = playerVelocity;
        }
        
        // Enable colliders and set layer
        foreach (var collider in cachedColliders)
        {
            collider.enabled = true;
            if (droppedWeaponLayer != 0)
            {
                collider.gameObject.layer = (int)Mathf.Log(droppedWeaponLayer.value, 2);
            }
        }
    }
    
    private void HandlePlayerRevive()
    {
        if (droppedWeapon == null) return;
        
        // Restore physics states
        for (int i = 0; i < cachedRigidbodies.Length; i++)
        {
            var rb = cachedRigidbodies[i];
            rb.isKinematic = originalKinematicStates[i];
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        for (int i = 0; i < cachedColliders.Length; i++)
        {
            cachedColliders[i].enabled = originalColliderStates[i];
        }
        
        // Restore original transform
        droppedWeapon.transform.SetParent(originalParent);
        droppedWeapon.transform.localPosition = originalLocalPosition;
        droppedWeapon.transform.localRotation = originalLocalRotation;
        
        droppedWeapon = null;
        cachedRigidbodies = null;
        cachedColliders = null;
    }
} 