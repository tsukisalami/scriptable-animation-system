using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ballistics
{
    /// Basic reloading controller
    [AddComponentMenu("Ballistics/Weapon Controller/Default Magazine Controller")]
    public class DefaultMagazineController : MagazineController
    {
        [Tooltip("Bullets contained in each magazine.")]
        public int BulletsPerMagazine;

        [Tooltip("Magazines available on startup")]
        public int InitialMagazineCount;

        public int CurrentBullets { get; private set; }
        public int StoredBullets { get; private set; }

        public UnityEvent OnMagazineEmpty;

        void Awake()
        {
            Initialize();
            Reload();
        }

        public override bool IsBulletAvailable()
        {
            return CurrentBullets > 0;
        }

        public override void BulletFired()
        {
            var before = CurrentBullets;
            CurrentBullets = Mathf.Max(before - 1, 0);
            if (before == 1)
                OnMagazineEmpty.Invoke();
        }

        public void Initialize()
        {
            StoredBullets = BulletsPerMagazine * InitialMagazineCount;
        }

        public bool Reload()
        {
            var load = Mathf.Min(BulletsPerMagazine - CurrentBullets, StoredBullets);
            CurrentBullets += load;
            StoredBullets -= load;
            return load > 0;
        }
    }
}
