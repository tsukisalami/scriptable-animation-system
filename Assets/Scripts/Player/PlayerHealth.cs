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
        Chest,
        Limb,
        Hand,
        Foot
    }

    [Header("Settings")]
    [SerializeField] private PlayerHealthSettings healthSettings;
    [SerializeField] private LivingEntity livingEntity;
    [SerializeField] private UnityEngine.InputSystem.PlayerInput playerInput;

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
    private Coroutine healthRegenerationCoroutine; // Track the regeneration coroutine

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

        if (playerInput == null)
        {
            playerInput = GetComponentInParent<UnityEngine.InputSystem.PlayerInput>();
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
            HitLocation.Chest => healthSettings.chestMultiplier,
            HitLocation.Limb => healthSettings.limbMultiplier,
            HitLocation.Hand => healthSettings.handMultiplier,
            HitLocation.Foot => healthSettings.footMultiplier,
            _ => 1f
        };

        float finalDamage = baseDamage * damageMultiplier;
        
        // Higher chance (20%) to start bleeding on direct hits
        if (!isBleeding && Random.value < 0.2f)
        {
            StartBleeding();
        }

        livingEntity.ApplyDamage(finalDamage);
    }

    private void HandleDamageReceived(DamageInfo damageInfo)
    {
        // Don't process damage if dead (prevents state update spam)
        if (CurrentHealthState == HealthState.Dead)
            return;

        if (damageInfo.DamageAmount > 0)
        {
            // 20% chance to start bleeding when taking damage
            if (!isBleeding && Random.value < 0.2f)
            {
                StartBleeding();
            }

            // Cancel health regeneration if taking damage
            if (healthRegenerationCoroutine != null)
            {
                StopCoroutine(healthRegenerationCoroutine);
                healthRegenerationCoroutine = null;
                Debug.Log("[PlayerHealth] Regeneration cancelled due to damage");
            }

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
        else
        {
            UpdateHealthState();
        }
    }

    private void HandleZeroHealth()
    {
        Debug.Log("[PlayerHealth] HandleZeroHealth called!");
        
        HealthState previousState = CurrentHealthState;
        
        // Disable all player input
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }
        
        if (Random.value < healthSettings.instantDeathChance)
        {
            Debug.Log("[PlayerHealth] Instant death triggered!");
            CurrentHealthState = HealthState.Dead;
            OnHealthStateChanged.Invoke(HealthState.Dead);
            OnPlayerDied.Invoke();
            if (randomReviveCoroutine != null)
                StopCoroutine(randomReviveCoroutine);
        }
        else
        {
            Debug.Log("[PlayerHealth] Player downed!");
            CurrentHealthState = HealthState.Downed;
            OnHealthStateChanged.Invoke(HealthState.Downed);
            downedTimeRemaining = healthSettings.downedStateDuration;
            OnPlayerDowned.Invoke();
            
            if (randomReviveCoroutine != null)
                StopCoroutine(randomReviveCoroutine);
            randomReviveCoroutine = StartCoroutine(RandomReviveRoutine());
        }
        
        SpeedReductionFactor = 0f;
        PainFactor = 1f;
    }

    private void TryRevivePlayer()
    {
        if (CurrentHealthState != HealthState.Downed)
            return;

        // Stop any existing regeneration
        if (healthRegenerationCoroutine != null)
        {
            StopCoroutine(healthRegenerationCoroutine);
            healthRegenerationCoroutine = null;
        }

        // Re-enable player input
        if (playerInput != null)
        {
            playerInput.enabled = true;
        }

        // Force reset the health state before healing
        CurrentHealthState = HealthState.Critical;
        
        // Reset health values
        float healAmount = healthSettings.reviveHealth - currentHealth;
        currentHealth = healthSettings.reviveHealth;
        livingEntity.ApplyDamage(-healAmount);
        
        IsRevivalCooldownActive = true;
        OnPlayerRevived.Invoke();
        
        Debug.Log($"Player revived. Healing to {healthSettings.healthRegenerationTarget:F0} health over {healthSettings.healthRegenerationDuration:F1} seconds");
        
        StartCoroutine(RevivalCooldownRoutine());
        healthRegenerationCoroutine = StartCoroutine(HealthRegenerationRoutine());
    }

    private IEnumerator RevivalCooldownRoutine()
    {
        yield return new WaitForSeconds(healthSettings.revivalCooldownDuration);
        IsRevivalCooldownActive = false;
    }

    private IEnumerator HealthRegenerationRoutine()
    {
        float startTime = Time.time;
        float endTime = startTime + healthSettings.healthRegenerationDuration;
        float targetHealth = healthSettings.healthRegenerationTarget;

        while (Time.time < endTime && currentHealth < targetHealth)
        {
            // Don't continue regeneration if player dies during the process
            if (CurrentHealthState == HealthState.Dead || CurrentHealthState == HealthState.Downed)
            {
                healthRegenerationCoroutine = null;
                yield break;
            }

            // Calculate how much health should be gained this frame
            float timeProgress = (Time.time - startTime) / healthSettings.healthRegenerationDuration;
            float healthPerSecond = (targetHealth - healthSettings.reviveHealth) / healthSettings.healthRegenerationDuration;
            float healAmount = healthPerSecond * Time.deltaTime;
            
            // Only heal if we haven't reached the target
            if (currentHealth + healAmount <= targetHealth)
            {
                currentHealth += healAmount;
                livingEntity.ApplyDamage(-healAmount);
                UpdateHealthState();
            }
            
            yield return null;
        }

        // Final health update if not dead/downed
        if (CurrentHealthState != HealthState.Dead && CurrentHealthState != HealthState.Downed)
        {
            // Only apply final healing if we haven't exceeded the target
            if (currentHealth < targetHealth)
            {
                float finalHealAmount = targetHealth - currentHealth;
                if (finalHealAmount > 0)
                {
                    currentHealth = targetHealth;
                    livingEntity.ApplyDamage(-finalHealAmount);
                    UpdateHealthState();
                }
            }
        }

        healthRegenerationCoroutine = null;
    }

    private void UpdateHealthState()
    {
        // Don't update state if dead
        if (CurrentHealthState == HealthState.Dead)
        {
            return;
        }

        // Allow state updates if downed (for revival)
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
        else if (CurrentHealthState != HealthState.Downed && CurrentHealthState != HealthState.Dead)
        {
            HandleZeroHealth();
            return;
        }

        if (previousState != CurrentHealthState)
        {
            Debug.Log($"[PlayerHealth] State changed: {previousState} -> {CurrentHealthState} [Speed: {SpeedReductionFactor:F2}, Pain: {PainFactor:F2}]");
            OnHealthStateChanged.Invoke(CurrentHealthState);
        }
    }

    private void StartBleeding()
    {
        if (bleedingCoroutine != null)
            StopCoroutine(bleedingCoroutine);
        
        isBleeding = true;
        OnBleedingChanged.Invoke(true);
        bleedingCoroutine = StartCoroutine(BleedingRoutine());
        Debug.Log("[PlayerHealth] Started bleeding!");
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
                float bleedDamage = healthSettings.bleedDamagePerSecond * healthSettings.bleedTickRate;
                livingEntity.ApplyDamage(bleedDamage);
                Debug.Log($"[PlayerHealth] Bleeding tick: -{bleedDamage:F1} health");
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

    public void OnSuicide()
    {
        if (CurrentHealthState == HealthState.Dead || CurrentHealthState == HealthState.Downed)
            return;

        // Set health to 0 and trigger the death pipeline
        currentHealth = 0;
        livingEntity.ApplyDamage(float.MaxValue); // Ensure the living entity is also "dead"
        HandleZeroHealth();
    }

    // Helper method to get health as an integer for UI display
    public int GetHealthAsInt()
    {
        return Mathf.RoundToInt(currentHealth);
    }
} 