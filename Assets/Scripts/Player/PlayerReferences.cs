using UnityEngine;

public class PlayerReferences : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerHealth health;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;

    [Header("Important Objects")]
    [SerializeField] private GameObject hipsObject;

    // Public accessors
    public PlayerHealth Health => health;
    public CharacterController CharacterController => characterController;
    public Animator Animator => animator;
    public GameObject HipsObject => hipsObject;

    private void Awake()
    {
        // Auto-find components if not manually assigned
        if (health == null)
            health = GetComponent<PlayerHealth>();
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (hipsObject == null)
        {
            Transform hipsTransform = transform.Find("Hips");
            if (hipsTransform != null)
            {
                hipsObject = hipsTransform.gameObject;
            }
        }

        // Validate required components
        if (health == null)
            Debug.LogError("PlayerReferences: PlayerHealth component not found!", this);
        if (characterController == null)
            Debug.LogError("PlayerReferences: CharacterController component not found!", this);
        if (animator == null)
            Debug.LogError("PlayerReferences: Animator component not found!", this);
        if (hipsObject == null)
            Debug.LogError("PlayerReferences: Hips object not found!", this);
    }

    private void OnValidate()
    {
        // Auto-find components in editor if not assigned
        if (health == null)
            health = GetComponent<PlayerHealth>();
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (hipsObject == null)
        {
            Transform hipsTransform = transform.Find("Hips");
            if (hipsTransform != null)
                hipsObject = hipsTransform.gameObject;
        }
    }
} 