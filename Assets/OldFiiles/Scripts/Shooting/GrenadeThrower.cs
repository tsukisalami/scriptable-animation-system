using UnityEngine;

public class GrenadeThrower : MonoBehaviour
{
    /*
    public GameObject grenadePrefab;
    public Transform grenadeSpawnPoint;
    public float throwForce = 10f;

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            ThrowGrenade();
        }
    }

    void ThrowGrenade()
    {
        if (grenadePrefab != null && grenadeSpawnPoint != null)
        {
            // Instantiate the grenade at the spawn point
            GameObject grenadeInstance = Instantiate(grenadePrefab, grenadeSpawnPoint.position, grenadeSpawnPoint.rotation);

            // Get the GrenadeManager component and apply throw force
            GrenadeManager grenadeManager = grenadeInstance.GetComponent<GrenadeManager>();
            if (grenadeManager != null)
            {
                Vector3 throwDirection = grenadeSpawnPoint.forward + grenadeSpawnPoint.up * 0.2f; // Adjust for a slight upward arc
                grenadeManager.ThrowGrenade(throwDirection * throwForce);
            }
            else
            {
                Debug.LogError("GrenadePrefab does not have a GrenadeManager component.");
            }
        }
        else
        {
            Debug.LogError("GrenadePrefab or GrenadeSpawnPoint is not assigned.");
        }
    }*/
}
