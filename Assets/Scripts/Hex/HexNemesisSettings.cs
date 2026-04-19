using UnityEngine;

[System.Serializable]
public sealed class HexNemesisSettings
{
    public bool enableNemesis;
    public HexNemesisArchetype activeArchetype = HexNemesisArchetype.Hunter;
    public HexNemesisUnusedCornerChoice cornerChoice = HexNemesisUnusedCornerChoice.FirstUnusedCorner;
    [Header("Hunter")]
    public bool enableHunterVisibilityModifiers = true;
    [Header("Debug")]
    public bool enableDebugLogging;
    public HexNemesisArchetypeProfile hunterProfile;
    public HexNemesisArchetypeProfile echoProfile;
    public HexNemesisArchetypeProfile corruptorProfile;

    public void Validate()
    {
        hunterProfile ??= Resources.Load<HexNemesisArchetypeProfile>("NemesisProfiles/HunterNemesisProfile");
        echoProfile ??= Resources.Load<HexNemesisArchetypeProfile>("NemesisProfiles/EchoNemesisProfile");
        corruptorProfile ??= Resources.Load<HexNemesisArchetypeProfile>("NemesisProfiles/CorruptorNemesisProfile");

        hunterProfile?.Validate();
        echoProfile?.Validate();
        corruptorProfile?.Validate();
    }

    public HexNemesisArchetypeProfile GetProfile(HexNemesisArchetype archetype)
    {
        return archetype switch
        {
            HexNemesisArchetype.Hunter => hunterProfile,
            HexNemesisArchetype.Echo => echoProfile,
            HexNemesisArchetype.Corruptor => corruptorProfile,
            _ => null
        };
    }
}
