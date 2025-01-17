using UnityEngine;

namespace Ballistics
{
    [AddComponentMenu("Ballistics/Weapon Controller/Base Spread Controller")]
    public class SpreadController : MonoBehaviour
    {
        public virtual Vector3 CalculateShootDirection(Weapon weapon) { return weapon.BulletSpawnPoint.forward; }
        public virtual void BulletFired() { }
    }
}
