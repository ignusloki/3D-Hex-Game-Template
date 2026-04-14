using System.Collections.Generic;

public sealed class HexObstacleInstance
{
    public HexObstacleInstance(HexCoordinates coordinates, HexObstacleDefinition definition)
    {
        Coordinates = coordinates;
        Definition = definition;
    }

    public HexCoordinates Coordinates { get; }
    public HexObstacleDefinition Definition { get; }
}

public enum HexObstacleDespawnReason
{
    VisibilityLost,
    ConsumedByCaravan
}

public readonly struct HexObstacleDespawnInfo
{
    public HexObstacleDespawnInfo(HexObstacleInstance obstacle, HexObstacleDespawnReason reason)
    {
        Obstacle = obstacle;
        Reason = reason;
    }

    public HexObstacleInstance Obstacle { get; }
    public HexObstacleDespawnReason Reason { get; }
}

public readonly struct HexObstacleContactResult
{
    public static HexObstacleContactResult None { get; } = new(false, null, CaravanResourceType.Food, 0, false);

    public HexObstacleContactResult(
        bool hasContact,
        HexObstacleInstance obstacle,
        CaravanResourceType affectedResource,
        int amountDrained,
        bool usedFallbackResource)
    {
        HasContact = hasContact;
        Obstacle = obstacle;
        AffectedResource = affectedResource;
        AmountDrained = amountDrained;
        UsedFallbackResource = usedFallbackResource;
    }

    public bool HasContact { get; }
    public HexObstacleInstance Obstacle { get; }
    public CaravanResourceType AffectedResource { get; }
    public int AmountDrained { get; }
    public bool UsedFallbackResource { get; }
}

public sealed class HexObstacleTurnResult
{
    public static HexObstacleTurnResult Empty { get; } = new();

    public List<HexObstacleInstance> SpawnedObstacles { get; } = new();
    public List<HexObstacleDespawnInfo> DespawnedObstacles { get; } = new();
    public List<HexObstacleInstance> RevealedObstacles { get; } = new();
    public List<HexObstacleInstance> VisibleObstacles { get; } = new();
    public List<HexObstacleInstance> ActiveObstacles { get; } = new();
    public HexObstacleContactResult ContactResult { get; set; } = HexObstacleContactResult.None;
    public bool ContactPenaltyIgnored { get; set; }
    public string ContactPenaltyIgnoreNote { get; set; } = string.Empty;
    public bool SpawnRollPerformed { get; set; }
    public bool SpawnRollSucceeded { get; set; }
    public float SpawnChance { get; set; }
    public int EnteredVisibilityCount { get; set; }
}
