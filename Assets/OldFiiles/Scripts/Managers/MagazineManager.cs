using System.Collections.Generic;
using UnityEngine;
using Ballistics;

public class MagazineManager : MonoBehaviour
{
    [SerializeField] public MagazineData magazineData; // Reference to the MagazineData scriptable object
    [SerializeField] public List<BulletInfo> bulletsInMag = new List<BulletInfo>(); // List of bullets in the magazine

    private void Awake()
    {
        // Fill the magazine with default bullets
        FillMagazineWithDefaultBullets();
    }

    // Fill the magazine with default bullets
    public void FillMagazineWithDefaultBullets()
    {
        bulletsInMag.Clear(); // Clear any existing bullets

        // Add bullet types to the magazine
        for (int i = 0; i < magazineData.ammoCount; i++)
        {
            foreach (BulletInfo bulletType in magazineData.bulletTypes)
            {
                // Add a new bullet of this type to the magazine
                bulletsInMag.Add(bulletType);
            }
        }
    }

    // Use the currently loaded bullet
    public void UseBullet()
    {
        if (bulletsInMag.Count > 0)
        {
            // Remove the first bullet in the list
            bulletsInMag.RemoveAt(0);
        }
        else
        {
            Debug.LogError("No bullets in the magazine!");
        }
    }

    // Get the speed of the currently loaded bullet
    public float GetCurrentBulletSpeed()
    {
        if (bulletsInMag.Count > 0)
        {
            return bulletsInMag[0].Speed; // Return the speed of the first bullet
        }
        else
        {
            Debug.LogError("No bullets in the magazine!");
            return 0f; // Return 0 if there are no bullets
        }
    }

    // Get the lifetime of the currently loaded bullet
    public float GetCurrentBulletLifeTime()
    {
        if (bulletsInMag.Count > 0)
        {
            return bulletsInMag[0].Lifetime; // Return the lifetime of the first bullet
        }
        else
        {
            Debug.LogError("No bullets in the magazine!");
            return 0f; // Return 0 if there are no bullets
        }
    }

    // Get the reload time for the currently loaded magazine
    public float GetReloadTime()
    {
        return magazineData.reloadTime; // Return the reload time from MagazineData scriptable object
    }
}
