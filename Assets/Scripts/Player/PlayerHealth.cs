using UnityEngine;
using UnityEngine.Events;
using Ballistics;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public enum HealthState
    {
        Healthy,    // 70-100
        Weak,       // 30-69
        Critical,   // 1-29
        Downed,     // 0
        Dead        // permanent death
    }

    [Header("Settings")]
    [SerializeField] private PlayerHealthSettings healthSettings;
    [SerializeField] private LivingEntity livingEntity;

    [Header("Debug Values")]
    [SerializeField] private float currentHealth;
    [SerializeField] private HealthState healthState;
    [SerializeField] private float speedReduction;
    [SerializeField] private float pain;

    public float SpeedReductionFactor { get => speedReduction; private set => speedReduction = value; }
    public float PainFactor { get => pain; private set => pain = value; }
    public HealthState CurrentHealthState { get => healthState; private set => healthState = value; }
    public bool IsRevivalCooldownActive { get; private set; }

    // Events
    public UnityEvent<HealthState> OnHealthStateChanged = new UnityEvent<HealthState>();
    public UnityEvent OnPlayerDowned = new UnityEvent();
    public UnityEvent OnPlayerDied = new UnityEvent();
    public UnityEvent OnPlayerRevived = new UnityEvent();

    private void OnValidate()
    {
        if (healthSettings == null)
        {
            Debug.LogError("PlayerHealthSettings is not assigned to PlayerHealth component!");
        }
        
        if (livingEntity == null)
        {
            livingEntity = GetComponent<LivingEntity>();
        }
    }

    private void Awake()
    {
        if (livingEntity == null)
        {
            livingEntity = GetComponent<LivingEntity>();
        }
    }

    private void Start()
    {
        if (healthSettings == null)
        {
            Debug.LogError("PlayerHealthSettings is not assigned! Player health system will not function correctly.");
            return;
        }

        currentHealth = healthSettings.maxHealth;
        // Initialize the living entity's health as well
        if (livingEntity != null)
        {
            livingEntity.ApplyDamage(0); // Reset health to full
        }
        UpdateHealthState();
    }

    private void OnEnable()
    {
        if (livingEntity != null)
        {
            livingEntity.OnDamaged.AddListener(HandleDamageReceived);
        }
        else
        {
            Debug.LogError("LivingEntity component not found on player!");
        }
    }

    private void OnDisable()
    {
        if (livingEntity != null)
        {
            livingEntity.OnDamaged.RemoveListener(HandleDamageReceived);
        }
    }

    private void Update()
    {
        if (CurrentHealthState == HealthState.Downed && Input.GetKeyDown(healthSettings.reviveKey))
        {
            TryRevivePlayer();
        }
    }

    private void HandleDamageReceived(DamageInfo damageInfo)
    {
        if (CurrentHealthState == HealthState.Dead || CurrentHealthState == HealthState.Downed)
            return;

        // Only log if actual damage was taken (not healing)
        if (damageInfo.DamageAmount > 0)
        {
            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damageInfo.DamageAmount);
            Debug.Log($"Health: {previousHealth:F0} -> {currentHealth:F0} (Damage: {damageInfo.DamageAmount:F1})");
        }
        else
        {
            currentHealth = Mathf.Max(0, currentHealth - damageInfo.DamageAmount);
        }
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            HandleZeroHealth();
        }
        
        UpdateHealthState();
    }

    private void HandleZeroHealth()
    {
        if (Random.value < healthSettings.instantDeathChance)
        {
            CurrentHealthState = HealthState.Dead;
            OnPlayerDied.Invoke();
        }
        else
        {
            CurrentHealthState = HealthState.Downed;
            OnPlayerDowned.Invoke();
        }
    }

    private void TryRevivePlayer()
    {
        if (CurrentHealthState != HealthState.Downed)
            return;

        float healAmount = healthSettings.reviveHealth - currentHealth;
        currentHealth = healthSettings.reviveHealth;
        livingEntity.ApplyDamage(-healAmount);
        
        IsRevivalCooldownActive = true;
        CurrentHealthState = HealthState.Critical;
        OnPlayerRevived.Invoke();
        
        Debug.Log($"Player revived. Healing to {healthSettings.healthRegenerationTarget:F0} health over {healthSettings.healthRegenerationDuration:F1} seconds");
        
        StartCoroutine(RevivalCooldownRoutine());
        StartCoroutine(HealthRegenerationRoutine());
    }

    private IEnumerator RevivalCooldownRoutine()
    {
        yield return new WaitForSeconds(healthSettings.revivalCooldownDuration);
        IsRevivalCooldownActive = false;
    }

    private IEnumerator HealthRegenerationRoutine()
    {
        float startTime = Time.time;
        float startHealth = currentHealth;

        while (Time.time - startTime < healthSettings.healthRegenerationDuration && currentHealth < healthSettings.healthRegenerationTarget)
        {
            float progress = (Time.time - startTime) / healthSettings.healthRegenerationDuration;
            float newHealth = Mathf.Lerp(startHealth, healthSettings.healthRegenerationTarget, progress);
            float healAmount = newHealth - currentHealth;
            
            if (healAmount > 0)
            {
                currentHealth = newHealth;
                livingEntity.ApplyDamage(-healAmount);
                UpdateHealthState();
            }
            
            yield return null;
        }

        float finalHealAmount = healthSettings.healthRegenerationTarget - currentHealth;
        if (finalHealAmount > 0)
        {
            currentHealth = healthSettings.healthRegenerationTarget;
            livingEntity.ApplyDamage(-finalHealAmount);
            UpdateHealthState();
        }
    }

    private void UpdateHealthState()
    {
        HealthState previousState = CurrentHealthState;

        if (currentHealth >= 70)
        {
            CurrentHealthState = HealthState.Healthy;
            SpeedReductionFactor = 1f;
            PainFactor = 0f;
        }
        else if (currentHealth >= 30)
        {
            CurrentHealthState = HealthState.Weak;
            float t = (currentHealth - 30f) / (69f - 30f);
            SpeedReductionFactor = Mathf.Lerp(0.7f, 1f, t);
            PainFactor = Mathf.Lerp(0.6f, 0f, t);
        }
        else if (currentHealth > 0)
        {
            CurrentHealthState = HealthState.Critical;
            SpeedReductionFactor = 0.5f;
            float t = (currentHealth - 29f) / (1f - 29f);
            PainFactor = Mathf.Lerp(0.6f, 1f, t);
        }
        else
        {
            SpeedReductionFactor = 0f;
            PainFactor = 1f;
        }

        if (previousState != CurrentHealthState)
        {
            OnHealthStateChanged.Invoke(CurrentHealthState);
            Debug.Log($"State: {previousState} -> {CurrentHealthState} [Speed: {SpeedReductionFactor:F2}, Pain: {PainFactor:F2}]");
        }
    }

    // Helper method to get health as an integer for UI display
    public int GetHealthAsInt()
    {
        return Mathf.RoundToInt(currentHealth);
    }
} 