using UnityEngine;

/// Allow saving configuration of an AudioSource
[CreateAssetMenu(fileName = "New AudioSource Preset", menuName = "Ballistics/Preset Object/AudioSource")]
public class AudioSourcePreset : ScriptableObject, IPreset<AudioSource>
{
    public UnityEngine.Audio.AudioMixerGroup outputAudioMixerGroup;
    [Range(0, 256)] public int priority = 128;
    [Range(0f, 1f)] public float volume = 1;
    [Range(-3f, 3f)] public float pitch = 1;
    [Range(-1f, 1f)] public float panStereo = 0;
    [Range(0f, 1f)] public float spatialBlend = 1;
    [Range(0f, 1.1f)] public float reverbZoneMix = 1f;
    [Range(0f, 5f)] public float dopplerLevel = 1;
    [Range(0f, 360f)] public float spread = 1;
    public float minDistance = 1;
    public float maxDistance = 500;

    public void InitializeValues(AudioSource obj)
    {
        obj.outputAudioMixerGroup = obj.outputAudioMixerGroup;
        obj.priority = priority;
        obj.volume = volume;
        obj.pitch = pitch;
        obj.panStereo = panStereo;
        obj.spatialBlend = spatialBlend;
        obj.reverbZoneMix = reverbZoneMix;
        obj.dopplerLevel = dopplerLevel;
        obj.spread = spread;
        obj.minDistance = minDistance;
        obj.maxDistance = maxDistance;
    }

    public static void SetDefault(AudioSource obj)
    {
        obj.outputAudioMixerGroup = null;
        obj.priority = 128;
        obj.volume = 1;
        obj.pitch = 1;
        obj.panStereo = 0;
        obj.spatialBlend = 1;
        obj.reverbZoneMix = 1;
        obj.dopplerLevel = 1;
        obj.spread = 1;
        obj.minDistance = 1;
        obj.maxDistance = 500;
    }
}
