using System.Collections;
using UnityEngine;

public class LightController : MonoBehaviour
{
    [SerializeField] private GameObject areaLight;
    [SerializeField] private bool isOn = true;
    [SerializeField] public Generator responsibleGenerator;
    [SerializeField] private float onIntensity = 1f;
    [SerializeField] private float offIntensity = 0f;
    [SerializeField] private float range = 10f;
    [SerializeField] private Color lightColor = Color.white;
    [SerializeField] private float fadeInSpeed = 1f;
    [SerializeField] private float fadeOutSpeed = 1f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Light lightComponent;
    private Coroutine currentCoroutine;
    

    private void Start()
    {
        lightComponent = areaLight.GetComponent<Light>();
        lightComponent.color = lightColor;
        lightComponent.range = range;


        if (responsibleGenerator != null)
        {
            Generator generator = responsibleGenerator.GetComponent<Generator>();
            if (generator != null)
            {
                Debug.Log("generator component was found");
                if (generator.isOn)
                {
                    TurnOn();
                }
                else 
                {
                    TurnOff();
                }
            }
        }
        else //If the light is not part of a generator group
        {
            if (isOn)
            {
                TurnOn();
            }
            else 
            {
                TurnOff();
            }
        }
    }

    public void TurnOn()
    {
        if (lightComponent != null)
        {
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }
            currentCoroutine = StartCoroutine(FadeLight(onIntensity, fadeInSpeed, fadeInCurve));
        }
        isOn = true;
        Debug.Log("Light turned on.");
    }

    public void TurnOff()
    {
        if (lightComponent != null)
        {
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }
            currentCoroutine = StartCoroutine(FadeLight(offIntensity, fadeOutSpeed, fadeOutCurve));
        }
        isOn = false;
        Debug.Log("Light turned off.");
    }

    private IEnumerator FadeLight(float targetIntensity, float duration, AnimationCurve curve)
    {
        float startIntensity = lightComponent.intensity;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            lightComponent.intensity = Mathf.Lerp(startIntensity, targetIntensity, curve.Evaluate(t));
            yield return null;
        }

        lightComponent.intensity = targetIntensity;
    }
}
