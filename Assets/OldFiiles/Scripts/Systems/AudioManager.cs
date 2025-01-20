using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct AudioClipInfo {
    public string name; // Name of the audio clip
    public AudioClip clip; // The audio clip itself
}

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance; // Singleton instance

    [SerializeField]
    private List<AudioClipInfo> audioClips; // List of audio clips set in the Inspector
    private Dictionary<string, AudioClip> audioClipDictionary; // Dictionary for quick lookup
    private AudioSource audioSource; // AudioSource component for playing sounds

    // Awake is called when the script instance is being loaded
    private void Awake() {
        if (Instance == null) {
            Instance = this; // Assign the instance to the first created AudioManager
            DontDestroyOnLoad(gameObject); // Persist the AudioManager across scenes
        } else {
            Destroy(gameObject); // Destroy duplicate AudioManager instances
        }

        // Initialize the dictionary and add audio clips to it
        audioClipDictionary = new Dictionary<string, AudioClip>();
        foreach (var audioClipInfo in audioClips) {
            audioClipDictionary[audioClipInfo.name] = audioClipInfo.clip;
        }

        // Add an AudioSource component to this GameObject
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    // Method to play a sound by name
    public void PlaySound(string name) {
        if (audioClipDictionary.ContainsKey(name)) {
            audioSource.PlayOneShot(audioClipDictionary[name]);
        } else {
            Debug.LogWarning("AudioManager: Audio clip not found in dictionary: " + name);
        }
    }

    // Overloaded method to play a sound by name with a specified volume
    public void PlaySound(string name, float volume) {
        if (audioClipDictionary.ContainsKey(name)) {
            audioSource.PlayOneShot(audioClipDictionary[name], volume);
        } else {
            Debug.LogWarning("AudioManager: Audio clip not found in dictionary: " + name);
        }
    }
}
