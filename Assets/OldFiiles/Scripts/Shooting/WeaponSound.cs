using UnityEngine;

public class WeaponSound : MonoBehaviour
{
    [Header("Sound Effects")]
    public AudioClip shotSound;
    public AudioClip reloadSound;
    public AudioClip boltReleaseSound;
    public AudioClip fireModeSwitchSound;
    public AudioClip inspectSound;
    public AudioClip shellCasingSound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void PlayShotSound()
    {
        PlaySound(shotSound);
    }

    public void PlayReloadSound()
    {
        PlaySound(reloadSound);
    }

    public void PlayBoltReleaseSound()
    {
        PlaySound(boltReleaseSound);
    }

    public void PlayInspectSound()
    {
        PlaySound(inspectSound);
    }

    public void PlayShellCasingSound()
    {
        PlaySound(shellCasingSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
