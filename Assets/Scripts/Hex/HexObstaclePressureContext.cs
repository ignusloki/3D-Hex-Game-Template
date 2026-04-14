using System;
using System.Collections.Generic;

public sealed class HexObstaclePressureContext
{
    public static HexObstaclePressureContext Empty { get; } = new();

    public int MinimumMaxActiveObstacles { get; set; }
    public float PressureSpawnChanceBonus { get; set; }
    public float PressureCandidateWeightBonus { get; set; }
    public int PressureInfluenceRadius { get; set; }
    public IReadOnlyCollection<HexCoordinates> PressureHexes { get; set; } = Array.Empty<HexCoordinates>();
    public IReadOnlyCollection<HexCoordinates> AdditionalProtectedHexes { get; set; } = Array.Empty<HexCoordinates>();
}
