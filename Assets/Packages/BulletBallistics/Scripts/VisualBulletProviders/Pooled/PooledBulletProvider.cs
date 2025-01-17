using UnityEngine;
using UnityEngine.Pool;

namespace Ballistics
{
    [CreateAssetMenu(fileName = "Pooled Bullet Provider", menuName = "Ballistics/Visual Bullet Providers/Pooled Bullet Provider", order = 0)]
    public class PooledBulletProvider : VisualBulletProviderObject
    {
        public PooledBullet VisualBulletPrefab;
        public int InitialPoolSize = 0;
        public int MaxPoolSize = 1000;
        private ObjectPool<PooledBullet> BulletPool;

        public override IVisualBullet GetVisualBullet()
        {
            return BulletPool.Get();
        }

        public override void Initialize()
        {
            BulletPool = new(
                () => {
                    var instance = Instantiate(VisualBulletPrefab);
                    instance.SetSourcePool(BulletPool);
                    DontDestroyOnLoad(instance.gameObject);
                    return instance;
                },
                (instance) => {
                    instance.PoolOnGet();
                    instance.gameObject.SetActive(true);
                },
                (instance) => {
                    instance.PoolOnRelease();
                    instance.gameObject.SetActive(false);
                },
                (instance) => {
                    instance.PoolOnDestroy();
                    Destroy(instance.gameObject);
                },
                true,
                InitialPoolSize,
                MaxPoolSize);
        }

        public void Clear()
        {
            BulletPool.Clear();
        }
    }
}