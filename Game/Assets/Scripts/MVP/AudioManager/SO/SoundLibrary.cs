using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that holds every sound mapping for the game.
/// Create via Assets → Create → Audio → Sound Library.
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [SerializeField] private SoundEntry[] entries;

    private Dictionary<SoundId, SoundEntry> _lookup;

    public void Init()
    {
        _lookup = new Dictionary<SoundId, SoundEntry>();
        if (entries == null) return;
        foreach (var e in entries)
        {
            if (e.id == SoundId.None) continue;
            if (!_lookup.TryAdd(e.id, e))
                Debug.LogWarning($"[SoundLibrary] Duplicate SoundId: {e.id}");
        }
    }

    public bool TryGet(SoundId id, out SoundEntry entry)
    {
        if (_lookup == null) Init();
        return _lookup.TryGetValue(id, out entry);
    }
}
