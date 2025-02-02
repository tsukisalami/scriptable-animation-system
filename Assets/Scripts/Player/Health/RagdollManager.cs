using UnityEngine;
using System.Collections;

public class RagdollManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform hipsBone; // Assign the hips/pelvis bone in inspector

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private bool isRagdollActive = false;
    private Vector3 lastHipsPosition;

    private void Awake()
    {
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        if (hipsBone == null)
        {
            Debug.LogError("[RagdollManager] Hips bone reference is missing! Player revival position will be incorrect.");
            enabled = false;
            return;
        }

        SetRagdollState(false);
    }

    private void Start()
    {
        if (playerHealth == null || animator == null || characterController == null)
        {
            Debug.LogError("[RagdollManager] Missing required components!");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        playerHealth.OnHealthStateChanged.AddListener(HandleHealthStateChanged);
        playerHealth.OnPlayerRevived.AddListener(HandlePlayerRevived);
    }

    private void OnDisable()
    {
        playerHealth.OnHealthStateChanged.RemoveListener(HandleHealthStateChanged);
        playerHealth.OnPlayerRevived.RemoveListener(HandlePlayerRevived);
    }

    private void HandleHealthStateChanged(PlayerHealth.HealthState newState)
    {
        if (newState == PlayerHealth.HealthState.Downed || newState == PlayerHealth.HealthState.Dead)
        {
            EnableRagdoll();
        }
    }

    private void HandlePlayerRevived()
    {
        DisableRagdoll();
    }

    private void EnableRagdoll()
    {
        if (isRagdollActive) return;

        Debug.Log("[RagdollManager] Enabling ragdoll physics");
        
        // Store velocity before disabling character controller
        Vector3 currentVelocity = characterController.velocity;
        
        characterController.enabled = false;
        animator.enabled = false;
        StartCoroutine(EnableRagdollPhysics(currentVelocity));
    }

    private IEnumerator EnableRagdollPhysics(Vector3 initialVelocity)
    {
        yield return new WaitForFixedUpdate();
        SetRagdollState(true);
        
        // Apply velocity to all rigidbodies
        foreach (var rb in ragdollRigidbodies)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = initialVelocity;
            }
        }
        
        isRagdollActive = true;
    }

    private void DisableRagdoll()
    {
        if (!isRagdollActive) return;

        Debug.Log("[RagdollManager] Disabling ragdoll physics");

        // Update player position to where the ragdoll ended up
        if (hipsBone != null)
        {
            // Move the root object to the hips position, but keep the y position slightly above ground
            Vector3 newPosition = lastHipsPosition;
            newPosition.y += characterController.height * 0.5f;
            transform.position = newPosition;
        }
        
        characterController.enabled = true;
        animator.enabled = true;
        SetRagdollState(false);
        isRagdollActive = false;
    }

    private void SetRagdollState(bool state)
    {
        foreach (var rb in ragdollRigidbodies)
        {
            if (rb != null)
            {
                if (!state && !rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = !state;
                rb.useGravity = state;
            }
        }

        foreach (var collider in ragdollColliders)
        {
            if (collider != null && collider.GetComponent<CharacterController>() == null)
            {
                collider.enabled = state;
            }
        }
    }

    private void Update()
    {
        // Track hips position while ragdolled
        if (isRagdollActive && hipsBone != null)
        {
            lastHipsPosition = hipsBone.position;
        }
    }

    private void OnValidate()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (animator == null) animator = GetComponent<Animator>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
    }
} 