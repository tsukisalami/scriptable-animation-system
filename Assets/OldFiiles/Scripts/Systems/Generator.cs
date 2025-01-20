using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour
{
    [SerializeField] private List<LightController> connectedLights = new List<LightController>();
    [SerializeField] private AudioClip switchSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] public bool isOn = true;

    public void Interact()
    {
        isOn = !isOn;

        foreach (var light in connectedLights)
        {
            if (isOn)
            {
                light.TurnOn();
            }
            else
            {
                light.TurnOff();
            }
        }

        PlaySwitchSound();
        Debug.Log("Generator power toggled. Current state: " + (isOn ? "On" : "Off"));
    }

    private void PlaySwitchSound()
    {
        if (audioSource != null && switchSound != null)
        {
            audioSource.PlayOneShot(switchSound);
        }
    }
}
