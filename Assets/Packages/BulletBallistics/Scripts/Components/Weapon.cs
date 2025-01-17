using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Ballistics
{
    [AddComponentMenu("Ballistics/Weapon")]
    public class Weapon : MonoBehaviour
    {
        [Header("Spawn")]
        [Tooltip("Position where bullet collistion calculations begin (e.g. camera)")]
        public Transform BulletSpawnPoint;

        [SerializeField, Tooltip("Position at which the visual bullet is created (e.g. weapon barrel). (when null, the BulletSpawnPoint will be used)")]
        private Transform visualSpawnPoint;
        public Transform VisualSpawnPoint { get => visualSpawnPoint ? visualSpawnPoint : BulletSpawnPoint; set { visualSpawnPoint = value; } }

        [Header("Bullet")]
        [SerializeField, InlineInspector, Tooltip("Visual representation of the bullet in the scene (can be null)")]
        [FormerlySerializedAs("visualBulletProvider")]
        public VisualBulletProviderObject VisualBulletProvider;

        [InlineInspector, Tooltip("All the physical information of the bullet needed for simulation")]
        public BulletInfo BulletInfo;

        [Space]
        [Tooltip("Called when the weapon is fired")]
        public UnityEvent OnShoot;

        public IVisualBullet GetVisualBullet()
        {
            var visualBullet = VisualBulletProvider?.GetVisualBullet();
#if UNITY_EDITOR
            if (Core.EnableDebug)
                visualBullet = BulletDebugProxy.Wrap(visualBullet);
#endif
            return visualBullet;
        }

        /// Shoot a bullet in the direction the BulletSpawnPoint is pointing in
        public void Shoot(float zeroingAngle = 0f)
        {
            if (zeroingAngle == 0f)
                Shoot(BulletSpawnPoint.forward);
            else
                Shoot(BulletSpawnPoint.forward, zeroingAngle);
        }

        /// Shoot a bullet from the BulletSpawnPoint in the given direction, angled upwards by the given zeroingAngle
        public void Shoot(Vector3 direction, float zeroingAngle)
        {
            Shoot(Zeroing.Apply(direction, zeroingAngle));
        }

        /// Shoot a bullet from the BulletSpawnPoint in the given direction
        public void Shoot(Vector3 direction)
        {
            var spawn = BulletSpawnPoint.position;
            Shoot(spawn, direction, visualSpawnPoint ? visualSpawnPoint.position - spawn : Vector3.zero, GetVisualBullet());
        }

        /// Shoot a bullet from a given position in a given direction
        public void Shoot(Vector3 position, Vector3 direction, Vector3 visualOffset, IVisualBullet visualBullet)
        {
            direction.Normalize();
            visualBullet.InitializeBullet(new(position, direction, InteractionFlags.NONE), visualOffset);
            // give the BulletInstance over to the BulletHandler
            Core.AddBullet(new BulletInstance(this, position, direction, BulletInfo, visualBullet));
            OnShoot.Invoke();
        }
    }
}