using UnityEngine;
using Ballistics;

public class Bandage : ConsumableItem
{
    [Header("Bandage Settings")]
    [SerializeField] private float healAmount = 25f;  // For future healing implementation
    [SerializeField] private float useTime = 3f;      // For future animation timing
    
    private PlayerHealth _playerHealth;
    private LivingEntity _livingEntity;
    
    public override void OnEquip(GameObject parent)
    {
        base.OnEquip(parent);
        _playerHealth = parent.GetComponent<PlayerHealth>();
        _livingEntity = parent.GetComponent<LivingEntity>();
        
        if (_playerHealth == null)
        {
            Debug.LogError("Bandage: No PlayerHealth component found on parent!");
        }
        if (_livingEntity == null)
        {
            Debug.LogError("Bandage: No LivingEntity component found on parent!");
        }
    }
    
    public override bool OnPrimaryUse()
    {
        if (_playerHealth == null || _livingEntity == null) return false;
        
        // Stop bleeding
        _playerHealth.StopBleeding();
        
        // Apply healing through LivingEntity
        _livingEntity.ApplyDamage(-healAmount); // Negative damage = healing
        
        Debug.Log($"Using bandage: Stopped bleeding and healed for {healAmount}");
        return base.OnPrimaryUse();  // This will handle the animation and consumption
    }
    
    public override bool OnSecondaryUse()
    {
        // Could be used for:
        // - Healing others
        // - Checking remaining bandages
        // - Different bandaging method
        Debug.Log("Bandage secondary use - Not implemented yet");
        return false;
    }
    
    protected override void OnUseComplete()
    {
        // Will be called when the use animation completes
        // Could be used to apply healing effects
        Debug.Log("Bandage use complete");
    }
} 