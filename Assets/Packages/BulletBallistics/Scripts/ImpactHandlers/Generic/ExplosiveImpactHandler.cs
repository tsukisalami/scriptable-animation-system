using UnityEngine;
using Unity.Mathematics;

namespace Ballistics
{
    [CreateAssetMenu(fileName = "New ExplosiveImpactHandler", menuName = "Ballistics/Impact Handler/Explosive Impact Handler", order = 2)]
    public class ExplosiveImpactHandler : ImpactHandlerObject
    {
        [Header("Explosion Effects")]
        public LayerMask explosionAffectedLayers = ~0;
        public bool showDebugSphere = false;

        public override void Initialize() { }

        public override HandledFlags HandleImpact(in ImpactInfo info, HandledFlags flags)
        {
            // Only handle explosion if the bullet is configured to explode on impact
            if (!info.BulletInfo.explodeOnImpact)
                return flags;

            // Create explosion effect if specified
            if (info.BulletInfo.explosionEffectPrefab != null)
            {
                GameObject explosionEffect = Instantiate(info.BulletInfo.explosionEffectPrefab, 
                    info.HitInfo.point, 
                    Quaternion.LookRotation(info.HitInfo.normal));
                
                // Optional: Destroy the effect after some time
                Destroy(explosionEffect, 5f);
            }

            // Find all colliders within explosion radius
            Collider[] affectedColliders = Physics.OverlapSphere(info.HitInfo.point, 
                info.BulletInfo.explosionRadius, 
                explosionAffectedLayers);

            if (showDebugSphere)
            {
                Debug.DrawLine(info.HitInfo.point, info.HitInfo.point + Vector3.up * info.BulletInfo.explosionRadius, Color.red, 5f);
                Debug.DrawLine(info.HitInfo.point, info.HitInfo.point + Vector3.right * info.BulletInfo.explosionRadius, Color.red, 5f);
                Debug.DrawLine(info.HitInfo.point, info.HitInfo.point + Vector3.forward * info.BulletInfo.explosionRadius, Color.red, 5f);
            }

            foreach (Collider col in affectedColliders)
            {
                // Calculate distance using the collider's bounds center instead of ClosestPoint
                float distance;
                
                if (col.attachedRigidbody != null)
                {
                    // For rigidbodies, use the rigidbody's position
                    distance = Vector3.Distance(info.HitInfo.point, col.attachedRigidbody.position);
                }
                else
                {
                    // For static colliders, use the collider's bounds center
                    distance = Vector3.Distance(info.HitInfo.point, col.bounds.center);
                }
                
                // Skip if outside radius
                if (distance > info.BulletInfo.explosionRadius)
                    continue;

                // Calculate damage based on distance
                float damageMultiplier = 1f - Mathf.Pow(distance / info.BulletInfo.explosionRadius, info.BulletInfo.explosionFalloff);
                float damage = info.BulletInfo.explosionDamage * damageMultiplier;

                // Apply damage if the object is damageable
                if (col.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.ApplyDamage(damage);
                }

                // Apply force if the object has a rigidbody
                if (col.attachedRigidbody != null)
                {
                    Vector3 direction = (col.bounds.center - info.HitInfo.point).normalized;
                    float force = info.BulletInfo.explosionForce * damageMultiplier;
                    col.attachedRigidbody.AddExplosionForce(force, info.HitInfo.point, info.BulletInfo.explosionRadius, 1f, ForceMode.Impulse);
                }
            }

            return flags | HandledFlags.DAMAGE | HandledFlags.PHYSICS;
        }

        public override HandledFlags HandleSurfaceInteraction(in SurfaceInteractionInfo info, HandledFlags flags)
        {
            // We don't need to handle surface interactions for explosive bullets
            return flags;
        }
    }
} 