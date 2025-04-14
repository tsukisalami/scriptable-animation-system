using UnityEngine;
using System.Collections.Generic;
using Ballistics;

// Static class to add extension methods to the original MagazineData class
public static class MagazineExtensions
{
    // Extension method to fill a magazine with default bullets
    public static void FillMagazineWithDefaultBullets(this MagazineData magazineData, List<BulletInfo> bulletsInMag)
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
    
    // Extension method to use a bullet from the magazine
    public static void UseBullet(this MagazineData magazineData, List<BulletInfo> bulletsInMag)
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
    
    // Extension method to get the speed of the currently loaded bullet
    public static float GetCurrentBulletSpeed(this MagazineData magazineData, List<BulletInfo> bulletsInMag)
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

    // Extension method to get the lifetime of the currently loaded bullet
    public static float GetCurrentBulletLifeTime(this MagazineData magazineData, List<BulletInfo> bulletsInMag)
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
} 