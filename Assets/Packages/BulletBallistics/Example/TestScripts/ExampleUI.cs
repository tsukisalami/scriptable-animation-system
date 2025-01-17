using UnityEngine;
using UnityEngine.UI;

namespace Ballistics
{
    public class ExampleUI : MonoBehaviour
    {
        public PlayerController PlayerController;
        public Text zeroingText;
        public Text BulletsText;
        public Text WeaponText;
        public Text FpsText;
        public Text TimeScaleText;
        public Text ActiveBulletsText;
        public Text Message;
        public Text CinematicMode;

        private float messageTimeout = 0;

        void Update()
        {
            var weapon = PlayerController.Weapons[PlayerController.CurrentWeaponIndex].Controller;
            CinematicMode.enabled = PlayerController.CinematicMode;
            zeroingText.text = $"Zeroing: {weapon.CurrentZeroing().Distance}";
            WeaponText.text = weapon.name;
            var magController = weapon.MagazineController as DefaultMagazineController;
            BulletsText.text = $"{magController.CurrentBullets}/{magController.StoredBullets}";
            TimeScaleText.text = Time.timeScale > .9f ? "" : "slow motion";
            FpsText.text = $"fps {(int)(Time.timeScale / Time.smoothDeltaTime)}";
            ActiveBulletsText.text = $"active: {Core.ActiveBullets}";
            if (messageTimeout >= 0) {
                messageTimeout -= Time.deltaTime;
                if (messageTimeout < 0)
                    Message.text = "";
            }
        }

        public void ShowHitMessage(DamageInfo info)
        {
            Message.text = $"Hit '{info.Context}' for {info.DamageAmount:0.00}HP!";
            messageTimeout = 2;
        }
    }
}