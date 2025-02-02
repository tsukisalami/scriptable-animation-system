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
} 