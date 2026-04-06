using UnityEngine;

/// <summary>
/// Maps a SoundId to one or more AudioClips.
/// When multiple clips are assigned, one is chosen at random to avoid repetition.
/// </summary>
[System.Serializable]
public class SoundEntry
{
    public SoundId id;

    [Tooltip("One or more clips — a random clip is chosen each play")]
    public AudioClip[] clips;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0.8f, 1.2f)]
    public float pitchMin = 0.95f;

    [Range(0.8f, 1.2f)]
    public float pitchMax = 1.05f;

    [System.NonSerialized] private int _lastClipIndex = -1;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;

        if (clips.Length == 1)
        {
            _lastClipIndex = 0;
            return clips[0];
        }

        int index = Random.Range(0, clips.Length);
        if (index == _lastClipIndex)
            index = (index + 1) % clips.Length;

        _lastClipIndex = index;
        return clips[index];
    }

    public float GetRandomPitch()
    {
        // Existing serialized assets may have legacy zero values.
        // pitch <= 0 can make playback effectively silent, so sanitize here.
        float min = pitchMin;
        float max = pitchMax;

        if (float.IsNaN(min) || float.IsInfinity(min)) min = 1f;
        if (float.IsNaN(max) || float.IsInfinity(max)) max = 1f;

        if (min <= 0f) min = 1f;
        if (max <= 0f) max = 1f;

        if (min > max)
        {
            float tmp = min;
            min = max;
            max = tmp;
        }

        return Random.Range(min, max);
    }
}
