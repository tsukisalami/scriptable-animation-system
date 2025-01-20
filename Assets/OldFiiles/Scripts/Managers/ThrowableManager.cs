using UnityEngine;
using System.Collections.Generic;
using Ballistics;
using UnityEngine.Events;

public class ThrowableManager : MonoBehaviour
{
    [SerializeField] public enum ThrowableType { Frag, Flashbang, Impact, C4, Smoke }
    [SerializeField] public ThrowableType throwableType;

    [SerializeField] public float weight = 0.15f;
    [SerializeField] public float fuseTime = 5f;
    [SerializeField] public float explosionRadius = 10f;
    [SerializeField] public float explosionForce = 10f;
    [SerializeField] public GameObject explosionEffect;

    [Header("Spawn")]
    [SerializeField] public List<Transform> fragmentSpawns = new List<Transform>();

    private Ballistics.Weapon projectileManager;

    private Rigidbody rb;
    private bool isThrown = false;
    private bool isExploded = false;
    private float countdown;

    private void Start()
    {
        countdown = fuseTime;
        GetProjectileManager();
    }

        private void GetProjectileManager()
    {
        projectileManager = GetComponent<Ballistics.Weapon>();
        if (projectileManager == null)
        {
            Debug.LogError("No projectileManager assigned.");
        }
    }

    void Update()
    {
        if (isThrown && throwableType == ThrowableType.Frag)
        {
            countdown -= Time.deltaTime;
            if (countdown <= 0f && !isExploded)
            {
                Explode();
                isExploded = true;
            }
        }
    }

    private void Explode()
    {
        Debug.Log("Exploded");
        switch (throwableType)
        {
            case ThrowableType.Frag:
                Debug.Log("Frag grenade explosion method called");
                HandleFragGrenadeExplosion();
                break;
            case ThrowableType.Flashbang:
                HandleFlashbangExplosion();
                break;
            case ThrowableType.Impact:
                HandleImpactGrenadeExplosion();
                break;
        }
        Destroy(gameObject);
    }

    public void Throw()
    {
        Debug.Log("Throw method sent");
        isThrown = true;
    }

    private void HandleFragGrenadeExplosion()
    {
        foreach (Transform spawnPoint in fragmentSpawns)
        {
            //projectileManager.Shoot(spawnPoint.forward);
        }

        Instantiate(explosionEffect, transform.position, transform.rotation);

        // Destroy all objects in the explosion radius
        Collider[] collidersToDestroy = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider objectInRange in collidersToDestroy)
        {
            Destructible destructible = objectInRange.GetComponent<Destructible>();
            {
                if (destructible!= null)
                {
                    destructible.Destroy();
                }
            }
        }

        // Move all objects in the explosion radius
        Collider[] collidersToMove = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider objectInRange in collidersToMove)
        {
            Rigidbody rb = objectInRange.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);

            }
        }
    }

    private void HandleFlashbangExplosion()
    {
        Instantiate(explosionEffect, transform.position, transform.rotation);
        // Handle flashbang effects (e.g., blinding, sound)
    }

    private void HandleImpactGrenadeExplosion()
    {
        Instantiate(explosionEffect, transform.position, transform.rotation);

        Collider[] collidersToDestroy = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider objectInRange in collidersToDestroy)
        {
            Rigidbody rb = objectInRange.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (throwableType == ThrowableType.Impact && isThrown)
        {
            Explode();
        }
    }
}
