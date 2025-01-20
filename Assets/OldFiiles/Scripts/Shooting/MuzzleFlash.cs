using System.Collections;
using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    [Header("Muzzle Flash Settings")]
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private float baseIntensity = 1.0f;
    [SerializeField] private float flashDuration = 0.1f;

    private Light flashLight;
    private ParticleSystem flashParticle;
    private Transform muzzlePoint;

    private void Start()
    {
        // Instantiate the muzzle flash prefab at the weapon's muzzle point
        if (muzzleFlashPrefab != null)
        {
            GameObject flashInstance = Instantiate(muzzleFlashPrefab, transform);
            flashParticle = flashInstance.GetComponent<ParticleSystem>();
            flashLight = flashInstance.GetComponent<Light>();

            if (flashParticle != null)
            {
                flashParticle.Stop();
            }

            if (flashLight != null)
            {
                flashLight.enabled = false;
            }

            muzzlePoint = flashInstance.transform;
        }
        else
        {
            Debug.LogError("Muzzle flash prefab is not assigned.");
        }
    }

    public void TriggerMuzzleFlash(float intensityModifier)
    {
        if (flashParticle != null)
        {
            flashParticle.Play();
        }

        if (flashLight != null)
        {
            StartCoroutine(FlashLightCoroutine(intensityModifier));
        }
    }

    private IEnumerator FlashLightCoroutine(float intensityModifier)
    {
        flashLight.intensity = baseIntensity * intensityModifier;
        flashLight.enabled = true;

        yield return new WaitForSeconds(flashDuration);

        flashLight.enabled = false;
    }
}
