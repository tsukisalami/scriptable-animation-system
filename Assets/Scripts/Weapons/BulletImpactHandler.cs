using UnityEngine;
using Ballistics;

public class BulletImpactHandler : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // Check if we hit a body part
        BodyPartHitbox hitbox = collision.gameObject.GetComponent<BodyPartHitbox>();
        if (hitbox != null)
        {
            // Get the bullet's damage from your bullet system
            float damage = 45f; // Replace with actual bullet damage
            
            // Apply damage to the hitbox
            hitbox.ApplyDamage(damage);
        }
    }
}