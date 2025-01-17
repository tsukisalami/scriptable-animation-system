using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ballistics
{
    // very simple example, of how to make your weapon shoot.
    // for a more complex integration take a look at the WeaponController script
    public class SimpleWeaponInput : MonoBehaviour
    {
        public Weapon Weapon;

        [Tooltip("Name of the fire button (old input system).")]
        public string FireButtonName = "Fire1";

        void Awake()
        {
            // try to auto-detect weapon on GameObject
            if (Weapon == null)
                Weapon = GetComponent<Weapon>();
        }

        void Update()
        {
            if (Input.GetButtonDown(FireButtonName))
                Weapon.Shoot();
        }
    }
}
