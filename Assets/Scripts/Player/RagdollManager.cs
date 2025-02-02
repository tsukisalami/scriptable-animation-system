using UnityEngine;
using System.Collections;

public class RagdollManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private bool isRagdollActive = false;

    private void Awake()
    {
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
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
        
        characterController.enabled = false;
        animator.enabled = false;
        StartCoroutine(EnableRagdollPhysics());
    }

    private IEnumerator EnableRagdollPhysics()
    {
        yield return new WaitForFixedUpdate();
        SetRagdollState(true);
        isRagdollActive = true;
    }

    private void DisableRagdoll()
    {
        if (!isRagdollActive) return;

        Debug.Log("[RagdollManager] Disabling ragdoll physics");
        
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

    private void OnValidate()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (animator == null) animator = GetComponent<Animator>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
    }
} 