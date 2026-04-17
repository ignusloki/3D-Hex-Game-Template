using System.Collections.Generic;

public enum HexFogKnowledgeState
{
    Unseen,
    Remembered,
    Visible
}

public sealed class HexFogTileKnowledge
{
    public HexFogTileKnowledge(HexCoordinates coordinates)
    {
        Coordinates = coordinates;
        KnowledgeState = HexFogKnowledgeState.Unseen;
    }

    public HexCoordinates Coordinates { get; }
    public HexFogKnowledgeState KnowledgeState { get; internal set; }
    public bool HasDiscoveredTerrain { get; internal set; }
    public bool IsAlwaysKnownSpecial { get; internal set; }
}

public sealed class HexFogUpdateResult
{
    public static HexFogUpdateResult Empty { get; } = new(
        new HashSet<HexCoordinates>(),
        new HashSet<HexCoordinates>(),
        new HashSet<HexCoordinates>(),
        new HashSet<HexCoordinates>(),
        new HashSet<HexCoordinates>(),
        new HashSet<HexCoordinates>(),
        new HashSet<HexCoordinates>());

    public HexFogUpdateResult(
        HashSet<HexCoordinates> visibleNow,
        HashSet<HexCoordinates> enteredVisibility,
        HashSet<HexCoordinates> leftVisibility,
        HashSet<HexCoordinates> discoveredThisUpdate,
        HashSet<HexCoordinates> discoveredTiles,
        HashSet<HexCoordinates> rememberedTiles,
        HashSet<HexCoordinates> alwaysKnownTiles)
    {
        VisibleNow = visibleNow;
        EnteredVisibility = enteredVisibility;
        LeftVisibility = leftVisibility;
        DiscoveredThisUpdate = discoveredThisUpdate;
        DiscoveredTiles = discoveredTiles;
        RememberedTiles = rememberedTiles;
        AlwaysKnownTiles = alwaysKnownTiles;
    }

    public IReadOnlyCollection<HexCoordinates> VisibleNow { get; }
    public IReadOnlyCollection<HexCoordinates> EnteredVisibility { get; }
    public IReadOnlyCollection<HexCoordinates> LeftVisibility { get; }
    public IReadOnlyCollection<HexCoordinates> DiscoveredThisUpdate { get; }
    public IReadOnlyCollection<HexCoordinates> DiscoveredTiles { get; }
    public IReadOnlyCollection<HexCoordinates> RememberedTiles { get; }
    public IReadOnlyCollection<HexCoordinates> AlwaysKnownTiles { get; }
}

public sealed class HexFogOfWarState
{
    private readonly Dictionary<HexCoordinates, HexFogTileKnowledge> tileKnowledge = new();
    private readonly HashSet<HexCoordinates> currentVisible = new();
    private readonly HashSet<HexCoordinates> alwaysKnownTiles = new();

    private HexGridData gridData;

    public bool IsInitialized => gridData != null && tileKnowledge.Count > 0;

    public void Initialize(HexGridData gridData, IEnumerable<HexCoordinates> alwaysKnownCoordinates)
    {
        this.gridData = gridData;
        tileKnowledge.Clear();
        currentVisible.Clear();
        alwaysKnownTiles.Clear();

        if (gridData == null)
        {
            return;
        }

        foreach (HexTileData tile in gridData.Tiles)
        {
            if (tile == null)
            {
                continue;
            }

            tileKnowledge[tile.Coordinates] = new HexFogTileKnowledge(tile.Coordinates);
        }

        SetAlwaysKnownTiles(alwaysKnownCoordinates);
    }

    public void SetAlwaysKnownTiles(IEnumerable<HexCoordinates> coordinates)
    {
        alwaysKnownTiles.Clear();
        foreach (HexFogTileKnowledge knowledge in tileKnowledge.Values)
        {
            knowledge.IsAlwaysKnownSpecial = false;
        }

        if (coordinates == null)
        {
            return;
        }

        foreach (HexCoordinates coordinatesValue in coordinates)
        {
            if (!tileKnowledge.TryGetValue(coordinatesValue, out HexFogTileKnowledge knowledge))
            {
                continue;
            }

            alwaysKnownTiles.Add(coordinatesValue);
            knowledge.IsAlwaysKnownSpecial = true;
        }
    }

    public HexFogUpdateResult UpdateVisibility(HexCoordinates caravanCoordinates, int visionRadius)
    {
        if (!IsInitialized)
        {
            return HexFogUpdateResult.Empty;
        }

        HashSet<HexCoordinates> nextVisible = BuildVisibleSet(caravanCoordinates, visionRadius);
        HashSet<HexCoordinates> enteredVisibility = new(nextVisible);
        enteredVisibility.ExceptWith(currentVisible);

        HashSet<HexCoordinates> leftVisibility = new(currentVisible);
        leftVisibility.ExceptWith(nextVisible);

        foreach (HexCoordinates coordinates in leftVisibility)
        {
            if (tileKnowledge.TryGetValue(coordinates, out HexFogTileKnowledge knowledge) && knowledge.HasDiscoveredTerrain)
            {
                knowledge.KnowledgeState = HexFogKnowledgeState.Remembered;
            }
        }

        HashSet<HexCoordinates> discoveredThisUpdate = new();
        foreach (HexCoordinates coordinates in nextVisible)
        {
            if (!tileKnowledge.TryGetValue(coordinates, out HexFogTileKnowledge knowledge))
            {
                continue;
            }

            if (!knowledge.HasDiscoveredTerrain)
            {
                knowledge.HasDiscoveredTerrain = true;
                discoveredThisUpdate.Add(coordinates);
            }

            knowledge.KnowledgeState = HexFogKnowledgeState.Visible;
        }

        foreach (HexFogTileKnowledge knowledge in tileKnowledge.Values)
        {
            if (!nextVisible.Contains(knowledge.Coordinates) && !knowledge.HasDiscoveredTerrain)
            {
                knowledge.KnowledgeState = HexFogKnowledgeState.Unseen;
            }
        }

        currentVisible.Clear();
        currentVisible.UnionWith(nextVisible);

        return new HexFogUpdateResult(
            new HashSet<HexCoordinates>(currentVisible),
            enteredVisibility,
            leftVisibility,
            discoveredThisUpdate,
            GatherDiscoveredTiles(),
            GatherRememberedTiles(),
            new HashSet<HexCoordinates>(alwaysKnownTiles));
    }

    public HexFogUpdateResult RevealAll()
    {
        if (!IsInitialized)
        {
            return HexFogUpdateResult.Empty;
        }

        HashSet<HexCoordinates> nextVisible = new(tileKnowledge.Keys);
        HashSet<HexCoordinates> enteredVisibility = new(nextVisible);
        enteredVisibility.ExceptWith(currentVisible);
        HashSet<HexCoordinates> discoveredThisUpdate = new();

        foreach (HexFogTileKnowledge knowledge in tileKnowledge.Values)
        {
            if (!knowledge.HasDiscoveredTerrain)
            {
                knowledge.HasDiscoveredTerrain = true;
                discoveredThisUpdate.Add(knowledge.Coordinates);
            }

            knowledge.KnowledgeState = HexFogKnowledgeState.Visible;
        }

        currentVisible.Clear();
        currentVisible.UnionWith(nextVisible);

        return new HexFogUpdateResult(
            new HashSet<HexCoordinates>(currentVisible),
            enteredVisibility,
            new HashSet<HexCoordinates>(),
            discoveredThisUpdate,
            GatherDiscoveredTiles(),
            new HashSet<HexCoordinates>(),
            new HashSet<HexCoordinates>(alwaysKnownTiles));
    }

    public HexFogKnowledgeState GetKnowledgeState(HexCoordinates coordinates)
    {
        return tileKnowledge.TryGetValue(coordinates, out HexFogTileKnowledge knowledge)
            ? knowledge.KnowledgeState
            : HexFogKnowledgeState.Unseen;
    }

    public bool IsCurrentlyVisible(HexCoordinates coordinates)
    {
        return currentVisible.Contains(coordinates);
    }

    public bool HasBeenDiscovered(HexCoordinates coordinates)
    {
        return tileKnowledge.TryGetValue(coordinates, out HexFogTileKnowledge knowledge) && knowledge.HasDiscoveredTerrain;
    }

    public bool IsRemembered(HexCoordinates coordinates)
    {
        return GetKnowledgeState(coordinates) == HexFogKnowledgeState.Remembered;
    }

    public bool IsAlwaysKnownSpecial(HexCoordinates coordinates)
    {
        return alwaysKnownTiles.Contains(coordinates);
    }

    public bool ShouldShowTerrain(HexCoordinates coordinates)
    {
        return HasBeenDiscovered(coordinates);
    }

    public bool ShouldShowSpecialTile(HexCoordinates coordinates)
    {
        return IsAlwaysKnownSpecial(coordinates) || HasBeenDiscovered(coordinates) || IsCurrentlyVisible(coordinates);
    }

    public bool ShouldShowObstacle(HexCoordinates coordinates)
    {
        return IsCurrentlyVisible(coordinates);
    }

    public IReadOnlyCollection<HexCoordinates> GetCurrentlyVisibleTiles()
    {
        return new HashSet<HexCoordinates>(currentVisible);
    }

    public IReadOnlyCollection<HexCoordinates> GetDiscoveredTiles()
    {
        return GatherDiscoveredTiles();
    }

    public IReadOnlyCollection<HexCoordinates> GetRememberedTiles()
    {
        return GatherRememberedTiles();
    }

    private HashSet<HexCoordinates> BuildVisibleSet(HexCoordinates caravanCoordinates, int visionRadius)
    {
        HashSet<HexCoordinates> visibleTiles = new();
        int clampedRadius = visionRadius < 0 ? 0 : visionRadius;

        foreach (HexFogTileKnowledge knowledge in tileKnowledge.Values)
        {
            if (caravanCoordinates.DistanceTo(knowledge.Coordinates) <= clampedRadius)
            {
                visibleTiles.Add(knowledge.Coordinates);
            }
        }

        return visibleTiles;
    }

    private HashSet<HexCoordinates> GatherDiscoveredTiles()
    {
        HashSet<HexCoordinates> discoveredTiles = new();
        foreach (HexFogTileKnowledge knowledge in tileKnowledge.Values)
        {
            if (knowledge.HasDiscoveredTerrain)
            {
                discoveredTiles.Add(knowledge.Coordinates);
            }
        }

        return discoveredTiles;
    }

    private HashSet<HexCoordinates> GatherRememberedTiles()
    {
        HashSet<HexCoordinates> rememberedTiles = new();
        foreach (HexFogTileKnowledge knowledge in tileKnowledge.Values)
        {
            if (knowledge.KnowledgeState == HexFogKnowledgeState.Remembered)
            {
                rememberedTiles.Add(knowledge.Coordinates);
            }
        }

        return rememberedTiles;
    }
}
