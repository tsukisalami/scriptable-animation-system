using UnityEngine;

namespace Ballistics
{
    [CreateAssetMenu(fileName = "NewInertProjectile", menuName = "Ballistics/Inert Projectile Info", order = 2)]
    public class InertProjectileInfo : BulletInfo
    {
        [Header("Inert Object Properties")]
        [Tooltip("How bouncy the projectile is (0 = no bounce, 1 = perfect bounce)")]
        [Range(0f, 1f)]
        public float bounciness = 0.5f;

        [Tooltip("How much velocity is lost on each bounce (0 = no loss, 1 = complete stop)")]
        [Range(0f, 1f)]
        public float bounceVelocityLoss = 0.3f;

        [Tooltip("Minimum velocity required to bounce (below this, the projectile will stop)")]
        public float minimumBounceVelocity = 1f;

        [Tooltip("How much the projectile rolls after bouncing")]
        [Range(0f, 1f)]
        public float rollFriction = 0.1f;

        [Tooltip("Maximum number of bounces before the projectile stops")]
        public int maxBounces = 3;

        private void OnValidate()
        {
            // Ensure inert projectiles can't penetrate
            explodeOnImpact = false;
        }
    }
} 