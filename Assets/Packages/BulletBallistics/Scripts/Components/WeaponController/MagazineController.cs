using UnityEngine;

namespace Ballistics
{
    [AddComponentMenu("Ballistics/Weapon Controller/Base Magazine Controller")]
    public class MagazineController : MonoBehaviour
    {
        public virtual bool IsBulletAvailable() { return true; }
        public virtual void BulletFired() { }
    }
}
