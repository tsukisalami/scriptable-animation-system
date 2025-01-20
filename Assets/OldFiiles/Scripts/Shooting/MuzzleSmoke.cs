using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuzzleSmoke : MonoBehaviour
{
    public ParticleSystem smokeEffect;
    public int maxShotsBeforeSmoke = 4;
    public float coolDownRate = 1f;
    public float smokeDuration = 2f;

    // Customizable parameters for intensity
    public float minEmission = 10f;
    public float maxEmission = 30f;
    public float minStartSize = 0.1f;
    public float maxStartSize = 0.5f;

    private int shotsFired = 0;
    private float lastShotTime = 0f;
    private bool isSmoking = false;

    void Update()
    {
        if (shotsFired > 0 && Time.time - lastShotTime > coolDownRate)
        {
            shotsFired--;
            lastShotTime = Time.time;
        }

        if (shotsFired >= maxShotsBeforeSmoke && !isSmoking)
        {
            StartCoroutine(TriggerSmoke());
        }
    }

    public void OnShotFired()
    {
        shotsFired++;
        lastShotTime = Time.time;
    }

    private IEnumerator TriggerSmoke()
    {
        isSmoking = true;

        // Adjust smoke based on the intensity (customize intensity here)
        var emission = smokeEffect.emission;
        var emissionRate = Mathf.Lerp(minEmission, maxEmission, (float)shotsFired / maxShotsBeforeSmoke);
        emission.rateOverTime = emissionRate;

        var main = smokeEffect.main;
        main.startSize = Mathf.Lerp(minStartSize, maxStartSize, (float)shotsFired / maxShotsBeforeSmoke);

        smokeEffect.Play();
        yield return new WaitForSeconds(smokeDuration);
        smokeEffect.Stop();
        isSmoking = false;
    }
}
