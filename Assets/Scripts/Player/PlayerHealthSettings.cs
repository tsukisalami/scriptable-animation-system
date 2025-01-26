using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHealthSettings", menuName = "Game/Player Health Settings")]
public class PlayerHealthSettings : ScriptableObject
{
    [Header("Health Settings")]
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public float reviveHealth = 10f;
    [SerializeField] public float healthRegenerationTarget = 30f;
    [SerializeField] public float healthRegenerationDuration = 40f;
    
    [Header("Revival Settings")]
    [SerializeField] public float revivalCooldownDuration = 15f;
    [SerializeField] public KeyCode reviveKey = KeyCode.R;
    [SerializeField] public float instantDeathChance = 0.1f;

    [Header("Damage Multipliers")]
    [SerializeField] public float headMultiplier = 2.5f;
    [SerializeField] public float bodyMultiplier = 1.0f;
    [SerializeField] public float limbMultiplier = 0.7f;

    [Header("Bleeding Settings")]
    [SerializeField] public float bleedDamagePerSecond = 1f;
    [SerializeField] public float bleedTickRate = 1f; // How often bleeding damage is applied

    [Header("Downed State Settings")]
    [SerializeField] public float downedStateDuration = 180f; // 3 minutes before death
    [SerializeField] public float randomReviveMinTime = 60f;
    [SerializeField] public float randomReviveMaxTime = 120f;
    [SerializeField] public float randomReviveChance = 0.1f;
    [SerializeField] public KeyCode callMedicKey = KeyCode.H;
    [SerializeField] public KeyCode giveUpKey = KeyCode.Space;
    
    [Header("Morphine Settings")]
    [SerializeField] public float morphineStaminaMultiplier = 1.5f;
    [SerializeField] public float morphineHealthRegenerationRate = 1f; // Health per second
    [SerializeField] public float morphineDuration = 60f; // 1 minute of effect
} 