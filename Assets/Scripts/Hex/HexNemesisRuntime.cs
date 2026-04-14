using System.Collections.Generic;

public readonly struct HexNemesisPitstopDestructionInfo
{
    public HexNemesisPitstopDestructionInfo(HexCoordinates coordinates, PitstopSite site)
    {
        Coordinates = coordinates;
        Site = site;
    }

    public HexCoordinates Coordinates { get; }
    public PitstopSite Site { get; }
}

public sealed class HexNemesisTurnResult
{
    public static HexNemesisTurnResult Empty { get; } = new();

    public bool Active { get; set; }
    public HexNemesisArchetype Archetype { get; set; } = HexNemesisArchetype.None;
    public HexNemesisPhase Phase { get; set; } = HexNemesisPhase.Inactive;
    public bool Acted { get; set; }
    public bool Moved { get; set; }
    public bool PhaseChanged { get; set; }
    public bool CausedDefeat { get; set; }
    public string DefeatReason { get; set; } = string.Empty;
    public HexCoordinates? PreviousCoordinates { get; set; }
    public HexCoordinates? CurrentCoordinates { get; set; }
    public HexCoordinates? EchoBlockedCoordinates { get; set; }
    public List<HexCoordinates> NewlyCorruptedHexes { get; } = new();
    public List<HexNemesisPitstopDestructionInfo> DestroyedPitstops { get; } = new();
    public List<HexNemesisPitstopDestructionInfo> DeferredPitstopDestructions { get; } = new();
}
