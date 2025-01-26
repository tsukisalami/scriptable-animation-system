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

    public enum HitLocation
    {
        Head,
        Body,
        Limb
    }

    [Header("Settings")]
    [SerializeField] private PlayerHealthSettings healthSettings;
    [SerializeField] private LivingEntity livingEntity;

    [Header("Debug Values")]
    [SerializeField] private float currentHealth;
    [SerializeField] private HealthState healthState;
    [SerializeField] private float speedReduction;
    [SerializeField] private float pain;
    [SerializeField] private bool isBleeding;
    [SerializeField] private float downedTimeRemaining;

    // Public properties
    public float SpeedReductionFactor { get => speedReduction; private set => speedReduction = value; }
    public float PainFactor { get => pain; private set => pain = value; }
    public HealthState CurrentHealthState { get => healthState; private set => healthState = value; }
    public bool IsRevivalCooldownActive { get; private set; }
    public bool IsBleeding { get => isBleeding; private set => isBleeding = value; }
    public float DownedTimeRemaining { get => downedTimeRemaining; private set => downedTimeRemaining = value; }
    public bool IsMorphineActive { get; private set; }

    // Events
    public UnityEvent<HealthState> OnHealthStateChanged = new UnityEvent<HealthState>();
    public UnityEvent OnPlayerDowned = new UnityEvent();
    public UnityEvent OnPlayerDied = new UnityEvent();
    public UnityEvent OnPlayerRevived = new UnityEvent();
    public UnityEvent<bool> OnBleedingChanged = new UnityEvent<bool>();

    private Coroutine bleedingCoroutine;
    private Coroutine morphineCoroutine;
    private Coroutine randomReviveCoroutine;

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
        if (CurrentHealthState == HealthState.Downed)
        {
            downedTimeRemaining -= Time.deltaTime;
            if (downedTimeRemaining <= 0)
            {
                HandleZeroHealth();
            }
        }

        if (CurrentHealthState == HealthState.Downed && Input.GetKeyDown(healthSettings.reviveKey))
        {
            TryRevivePlayer();
        }
    }

    public void HandleHit(float baseDamage, HitLocation hitLocation)
    {
        if (CurrentHealthState == HealthState.Dead || CurrentHealthState == HealthState.Downed)
            return;

        float damageMultiplier = hitLocation switch
        {
            HitLocation.Head => healthSettings.headMultiplier,
            HitLocation.Body => healthSettings.bodyMultiplier,
            HitLocation.Limb => healthSettings.limbMultiplier,
            _ => 1f
        };

        float finalDamage = baseDamage * damageMultiplier;
        
        // Start bleeding on any hit
        if (!isBleeding)
        {
            StartBleeding();
        }

        livingEntity.ApplyDamage(finalDamage);
    }

    private void HandleDamageReceived(DamageInfo damageInfo)
    {
        if (CurrentHealthState == HealthState.Dead || CurrentHealthState == HealthState.Downed)
            return;

        if (damageInfo.DamageAmount > 0)
        {
            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damageInfo.DamageAmount);
            Debug.Log($"Health: {previousHealth:F0} -> {currentHealth:F0} (Damage: {damageInfo.DamageAmount:F1})");
        }
        else
        {
            currentHealth = Mathf.Min(healthSettings.maxHealth, currentHealth - damageInfo.DamageAmount);
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
            if (randomReviveCoroutine != null)
                StopCoroutine(randomReviveCoroutine);
        }
        else
        {
            CurrentHealthState = HealthState.Downed;
            downedTimeRemaining = healthSettings.downedStateDuration;
            OnPlayerDowned.Invoke();
            
            // Start random revive chance
            if (randomReviveCoroutine != null)
                StopCoroutine(randomReviveCoroutine);
            randomReviveCoroutine = StartCoroutine(RandomReviveRoutine());
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

    private void StartBleeding()
    {
        if (bleedingCoroutine != null)
            StopCoroutine(bleedingCoroutine);
        
        isBleeding = true;
        OnBleedingChanged.Invoke(true);
        bleedingCoroutine = StartCoroutine(BleedingRoutine());
    }

    public void StopBleeding()
    {
        if (bleedingCoroutine != null)
        {
            StopCoroutine(bleedingCoroutine);
            bleedingCoroutine = null;
        }
        
        isBleeding = false;
        OnBleedingChanged.Invoke(false);
    }

    private IEnumerator BleedingRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(healthSettings.bleedTickRate);
            if (CurrentHealthState != HealthState.Dead && CurrentHealthState != HealthState.Downed)
            {
                livingEntity.ApplyDamage(healthSettings.bleedDamagePerSecond * healthSettings.bleedTickRate);
            }
        }
    }

    public void ApplyMorphine()
    {
        if (morphineCoroutine != null)
            StopCoroutine(morphineCoroutine);
        
        IsMorphineActive = true;
        IsRevivalCooldownActive = false;
        morphineCoroutine = StartCoroutine(MorphineEffectRoutine());
    }

    private IEnumerator MorphineEffectRoutine()
    {
        float startTime = Time.time;
        
        while (Time.time - startTime < healthSettings.morphineDuration)
        {
            // Apply health regeneration
            if (currentHealth < healthSettings.maxHealth)
            {
                float healAmount = healthSettings.morphineHealthRegenerationRate * Time.deltaTime;
                livingEntity.ApplyDamage(-healAmount);
            }
            
            yield return null;
        }
        
        IsMorphineActive = false;
    }

    private IEnumerator RandomReviveRoutine()
    {
        if (Random.value < healthSettings.randomReviveChance)
        {
            float reviveTime = Random.Range(healthSettings.randomReviveMinTime, healthSettings.randomReviveMaxTime);
            yield return new WaitForSeconds(reviveTime);
            
            if (CurrentHealthState == HealthState.Downed)
            {
                TryRevivePlayer();
            }
        }
    }

    public void GiveUp()
    {
        if (CurrentHealthState == HealthState.Downed)
        {
            CurrentHealthState = HealthState.Dead;
            OnPlayerDied.Invoke();
            if (randomReviveCoroutine != null)
                StopCoroutine(randomReviveCoroutine);
        }
    }

    // Helper method to get health as an integer for UI display
    public int GetHealthAsInt()
    {
        return Mathf.RoundToInt(currentHealth);
    }
} 