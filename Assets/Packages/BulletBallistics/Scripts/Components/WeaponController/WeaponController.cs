using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ballistics
{
    /// Basic WeaponController, that can simulate the most common weapon types.
    /// Just pull the trigger -> 'myController.SetTrigger(true)'
    [AddComponentMenu("Ballistics/Weapon Controller/Weapon Controller")]
    public class WeaponController : MonoBehaviour
    {
        public Weapon Weapon;

        [Tooltip("Time in seconds between shots")]
        public float ShootDelay = 0.25f;

        public enum WeaponMode
        {
            [Tooltip("A single bullet is fired each shot. The trigger has to be released after each shot.")]
            SingleShot,
            [Tooltip("Each shot fires multiple bullet fragments. The trigger has to be released after each shot.")]
            Shotgun,
            [Tooltip("A single bullet is fired each shot. Holding the trigger will continuously fire bullets.")]
            FullAuto,
            [Tooltip("A single bullet is fired each shot. Holding the trigger will fire x bullets without having to release the trigger.")]
            Burst
        }
        public WeaponMode Mode = WeaponMode.SingleShot;

        [Header("Burst- / Shotgun- Mode")]
        [Tooltip("Number of bullet framents in one shotgun shell.")]
        public int BulletsPerShell = 8;

        [Tooltip("Number of bullets fired in one burst.")]
        public int BulletsPerBurst = 3;

        [Header("Controllers")]
        public SpreadController SpreadController;
        public MagazineController MagazineController;

        [Header("Zeroing")]
        public List<float> Distances;
        public int CurrentZeroingIndex {
            get {
                if (zeroingResults == null)
                    return -1;
                return Mathf.Clamp(currentZeroingIndex, -1, zeroingResults.Length - 1);
            }
            set {
                currentZeroingIndex = value;
            }
        }
        private int currentZeroingIndex = -1;
        private Zeroing.Result[] zeroingResults;

        // internal state
        private bool triggerHeld = false;
        private bool triggerReleaseRequired = false;
        private float cooldownTimer = 0;

        public void SetTrigger(bool held)
        {
            triggerHeld = held;
            CheckShoot();
        }

        private void Start()
        {
            UpdateZeroing();
            if (!SpreadController)
                SpreadController = gameObject.AddComponent<SpreadController>();
            if (!MagazineController)
                MagazineController = gameObject.AddComponent<MagazineController>();
        }

        private void Update()
        {
            CheckShoot();
            cooldownTimer -= Time.deltaTime;
        }

        private void CheckShoot()
        {
            if (!triggerHeld && triggerReleaseRequired)
                triggerReleaseRequired = false;
            if (triggerHeld && cooldownTimer <= 0 && MagazineController.IsBulletAvailable()) {
                switch (Mode) {
                    case WeaponMode.FullAuto:
                        Shoot(1, ShootDelay);
                        break;
                    case WeaponMode.Shotgun:
                        if (!triggerReleaseRequired) {
                            triggerReleaseRequired = true;
                            Shoot(BulletsPerShell, ShootDelay);
                        }
                        break;
                    case WeaponMode.Burst:
                        if (!triggerReleaseRequired) {
                            triggerReleaseRequired = true;
                            StartCoroutine(ShootBurst());
                        }
                        break;
                    case WeaponMode.SingleShot:
                        if (!triggerReleaseRequired) {
                            triggerReleaseRequired = true;
                            Shoot(1, ShootDelay);
                        }
                        break;
                }
            }
        }

        private IEnumerator ShootBurst()
        {
            var wait = new WaitForSeconds(ShootDelay);
            for (var i = BulletsPerBurst; i > 0; i--) {
                Shoot(1, i * ShootDelay);
                yield return wait;
            }
        }

        private void Shoot(int bullets, float cooldown)
        {
            for (var i = bullets; i > 0; i--)
                Weapon.Shoot(SpreadController.CalculateShootDirection(Weapon), CurrentZeroing().Angle);
            cooldownTimer = cooldown;
            SpreadController.BulletFired();
            MagazineController.BulletFired();
        }

        public void UpdateZeroing()
        {
            if (Distances.Count == 0 || !Core.Environment.EnableGravity) {
                zeroingResults = null;
                return;
            }
            var gravity = -Unity.Mathematics.math.length(Core.Environment.Gravity);
#if !BB_NO_AIR_RESISTANCE
            if (Core.Environment.EnableAirResistance) {
                using (var handle = Zeroing.ApproximateZeroingAnglesWithDrag(Distances, Weapon.BulletInfo, gravity, Core.Environment.AirDensity, Core.Environment.MaximumDeltaTime))
                    zeroingResults = handle.Get();
            } else
#endif
                zeroingResults = Zeroing.ZeroingAnglesNoDrag(Distances, Weapon.BulletInfo.Speed, gravity);
        }

        public Zeroing.Result CurrentZeroing()
        {
            if (CurrentZeroingIndex != -1)
                return zeroingResults[CurrentZeroingIndex];
            return new Zeroing.Result(0, 0);
        }
    }
}