using UnityEngine;
using Ballistics;

public class BodyPartHitbox : MonoBehaviour, IDamageable
{
    public PlayerHealth.HitLocation hitLocation;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        // Find the PlayerHealth component in parent objects
        playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError($"BodyPartHitbox on {gameObject.name} couldn't find PlayerHealth component in parents!");
        }
    }

    // Implementation of IDamageable interface
    public void ApplyDamage(float damage)
    {
        if (playerHealth != null)
        {
            playerHealth.HandleHit(damage, hitLocation);
        }
    }
    
    // Add direct collision detection
    private void OnCollisionEnter(Collision collision)
    {
        // Debug.Log($"Collision detected on {gameObject.name} with {collision.gameObject.name}");
        
        // Try to get bullet component (adjust this based on your actual bullet component)
        var bullet = collision.gameObject.GetComponent<MonoBehaviour>();
        if (bullet != null && playerHealth != null)
        {
            // Get damage from bullet (this is a placeholder - adjust for your bullet system)
            float damage = 45f;
            
            // Try different ways to get damage from bullet
            var damageProperty = bullet.GetType().GetProperty("Damage");
            if (damageProperty != null)
            {
                damage = (float)damageProperty.GetValue(bullet);
            }
            else
            {
                var damageField = bullet.GetType().GetField("damage");
                if (damageField != null)
                {
                    damage = (float)damageField.GetValue(bullet);
                }
            }
            
            // Apply damage through the PlayerHealth component
            playerHealth.HandleHit(damage, hitLocation);
        }
    }
} 