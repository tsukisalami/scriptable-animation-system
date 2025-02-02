using UnityEngine;
using Ballistics;

public class BulletImpactHandler : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // Get the bullet's damage from your bullet system
        float damage = 45f; // Replace with actual bullet damage

        // First try to handle it as a body part
        BodyPartHitbox hitbox = collision.gameObject.GetComponent<BodyPartHitbox>();
        if (hitbox != null)
        {
            // Apply damage with hit location info
            hitbox.ApplyDamage(damage);
            return;
        }

        // If not a body part, try to apply damage to any IDamageable object
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.ApplyDamage(damage);
        }
    }
}