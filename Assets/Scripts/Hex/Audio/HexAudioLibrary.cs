using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HexAudioLibrary", menuName = "Hex/Audio/Audio Library")]
public sealed class HexAudioLibrary : ScriptableObject
{
    [SerializeField] private List<HexMusicEntry> musicEntries = new();
    [SerializeField] private List<HexSfxEntry> sfxEntries = new();

    public IReadOnlyList<HexMusicEntry> MusicEntries => musicEntries;
    public IReadOnlyList<HexSfxEntry> SfxEntries => sfxEntries;

    public bool TryGetMusic(HexMusicStage stage, out HexMusicEntry entry)
    {
        for (int index = 0; index < musicEntries.Count; index++)
        {
            HexMusicEntry candidate = musicEntries[index];
            if (candidate != null && candidate.stage == stage)
            {
                entry = candidate;
                return true;
            }
        }

        entry = null;
        return false;
    }

    public bool TryGetSfx(HexSfxId id, out HexSfxEntry entry)
    {
        for (int index = 0; index < sfxEntries.Count; index++)
        {
            HexSfxEntry candidate = sfxEntries[index];
            if (candidate != null && candidate.id == id)
            {
                entry = candidate;
                return true;
            }
        }

        entry = null;
        return false;
    }
}
