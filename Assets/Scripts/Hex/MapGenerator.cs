/// Author: Mohammed Marzouq
/// Date: 19 Sep 2024
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public GameObject[] forestTiles;
    public GameObject[] grassTiles;
    public GameObject[] mountainTiles;
    public GameObject[] waterTiles;
    public GameObject[] desertTiles;
    public HexScriptableObject[] hexProperties;
    [SerializeField] private HexMapSizePreset mapSize = HexMapSizePreset.TenByTen;
    [SerializeField] private HexBiomeGenerationSettings biomeGenerationSettings = new();
    [SerializeField] private HexSpecialTileSettings specialTileSettings = new();
    [Header("Terrain Landmarks")]
    [SerializeField] private HexTerrainLandmarkPlacementRequest[] terrainLandmarkRequests = System.Array.Empty<HexTerrainLandmarkPlacementRequest>();
    [Header("Map Objects")]
    [SerializeField] private HexMapObjectPlacementRequest[] mapObjectPlacementRequests = System.Array.Empty<HexMapObjectPlacementRequest>();
    [Header("Debug")]
    [SerializeField] private bool enableGenerationDebugLogging;
    [SerializeField] private bool enableVerboseGenerationPhaseLogging;

    private readonly HexBiomeMapGenerator biomeMapGenerator = new();
    private Dictionary<Biome, HexScriptableObject> hexsDictionary;
    [Min(0.01f)] public float X1stTileSpacing = 1f; // Horizontal space for first tile
    [Min(0.01f)] public float XTileSpacing = 1f; // Horizontal space between tiles
    [Min(0.01f)] public float YTileSpacing = .8870f; // Vertical space between tiles
    public float rotation = 30f;

    private HexagonTile[,] tiles; // 2D array to hold the instantiated tiles
    private Dictionary<HexCoordinates, HexagonTile> tileViews = new();
    private HexGridData gridData;
    private HexPathfinder pathfinder;
    private int runtimeGenerationVariant;

    public int Rows => (int)mapSize;
    public int Columns => (int)mapSize;
    public HexGridData GridData => gridData;
    public HexPathfinder Pathfinder => pathfinder;
    public HexCoordinates StartCoordinates { get; private set; }
    public HexCoordinates GoalCoordinates { get; private set; }
    public HexMapPlacementReservations PlacementReservations { get; private set; } = new();
    public HexMapObjectPlacementCollection MapObjectPlacements { get; private set; } = new();
    public HexMapGenerationModifiers ActiveGenerationModifiers { get; private set; } = HexMapGenerationModifiers.None;

    void Start() {
        if (!GenerateMap())
        {
            return;
        }
    }

    private void OnValidate()
    {
        X1stTileSpacing = Mathf.Max(0.01f, X1stTileSpacing);
        XTileSpacing = Mathf.Max(0.01f, XTileSpacing);
        YTileSpacing = Mathf.Max(0.01f, YTileSpacing);
        biomeGenerationSettings ??= new HexBiomeGenerationSettings();
        biomeGenerationSettings.Validate();
        specialTileSettings ??= new HexSpecialTileSettings();
        terrainLandmarkRequests ??= System.Array.Empty<HexTerrainLandmarkPlacementRequest>();
        for (int index = 0; index < terrainLandmarkRequests.Length; index++)
        {
            terrainLandmarkRequests[index]?.Validate();
        }

        mapObjectPlacementRequests ??= System.Array.Empty<HexMapObjectPlacementRequest>();
        for (int index = 0; index < mapObjectPlacementRequests.Length; index++)
        {
            mapObjectPlacementRequests[index]?.Validate();
        }
    }

    private GameObject[] GetBiomePrefabs(Biome chosen) {
        switch (chosen) {
            case Biome.desert:
                return desertTiles;

            case Biome.forest:
                return forestTiles;

            case Biome.grass:
                return grassTiles;
            
            case Biome.mountain:
                return mountainTiles;

            case Biome.water:
                return waterTiles;

            default:
                return forestTiles;
        }
    }

    private GameObject PickBiomePrefab(Biome biome)
    {
        GameObject[] biomePrefabs = GetBiomePrefabs(biome);
        if (biomePrefabs == null || biomePrefabs.Length == 0)
        {
            return null;
        }

        return biomePrefabs[Random.Range(0, biomePrefabs.Length)];
    }

    private HexScriptableObject GetHexProperties(Biome biome)
    {
        if (hexsDictionary.TryGetValue(biome, out HexScriptableObject properties))
        {
            return properties;
        }

        Debug.LogWarning($"No HexScriptableObject configured for biome '{biome}'.");
        return null;
    }

    public bool GenerateMap()
    {
        LoadScriptableObjects();
        if (!ValidateConfiguration())
        {
            return false;
        }

        GenerateTilesAndAssignNeighbors();
        return true;
    }

    public IEnumerator RegenerateMapCoroutine(bool advanceVariantSeed)
    {
        if (advanceVariantSeed)
        {
            runtimeGenerationVariant++;
        }

        ClearGeneratedMap();
        if (Application.isPlaying)
        {
            yield return null;
        }

        if (!GenerateMap())
        {
            Debug.LogError("MapGenerator failed to regenerate the map.", this);
        }
    }

    // Generates hexagonal tiles and assigns their neighbors
    private void GenerateTilesAndAssignNeighbors()
    {
        tiles = new HexagonTile[Rows, Columns];
        tileViews = new Dictionary<HexCoordinates, HexagonTile>();
        gridData = new HexGridData(Rows, Columns);
        int originalSeed = biomeGenerationSettings.seed;
        bool shouldOffsetFixedSeed = !biomeGenerationSettings.useRandomSeed && runtimeGenerationVariant > 0;
        int? fixedSeedOverride = shouldOffsetFixedSeed
            ? unchecked(originalSeed + (runtimeGenerationVariant * 7919))
            : null;
        HexMapGenerationModifiers generationModifiers = ResolveGenerationModifiers();
        ActiveGenerationModifiers = generationModifiers;
        HexMapGenerationContext generationContext = new(
            Rows,
            Columns,
            biomeGenerationSettings,
            specialTileSettings,
            generationModifiers,
            fixedSeedOverride,
            enableGenerationDebugLogging,
            enableVerboseGenerationPhaseLogging);

        HexBiomeMapResult biomeMapResult = biomeMapGenerator.Generate(generationContext);
        Biome[,] biomeMap = biomeMapResult.BiomeMap;
        StartCoordinates = biomeMapResult.StartCoordinates;
        GoalCoordinates = biomeMapResult.GoalCoordinates;
        PlacementReservations = biomeMapResult.PlacementReservations?.Clone() ?? new HexMapPlacementReservations();

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                HexCoordinates coordinates = new(row, col);
                Biome chosenBiome = biomeMap[row, col];
                HexTileData tileData = new(coordinates, chosenBiome, GetHexProperties(chosenBiome));
                gridData.SetTile(tileData);

                Vector3 spawnPosition = CalculateTilePosition(row, col, YTileSpacing);
                HexagonTile tile = InstantiateTile(spawnPosition, tileData);
                tiles[row, col] = tile;

                if (tile != null)
                {
                    tileViews[coordinates] = tile;
                }
            }
        }

        pathfinder = new HexPathfinder(gridData);
        AssignNeighbors();
        if (biomeMapGenerator.LastGenerationMetQualityThreshold)
        {
            Debug.Log(
                $"Generated biome map with seed {biomeMapGenerator.LastResolvedSeed} after {biomeMapGenerator.LastGenerationAttempts} attempt(s). " +
                $"Dominant biome: {biomeMapGenerator.LastQualityReport?.DominantBiome} ({biomeMapGenerator.LastQualityReport?.DominantBiomeRatio:P0}). " +
                $"Modifiers: {generationContext.Modifiers.GetDebugSummary()}.",
                this);
        }
        else
        {
            Debug.LogWarning(
                $"Generated best-effort biome map with seed {biomeMapGenerator.LastResolvedSeed} after {biomeMapGenerator.LastGenerationAttempts} attempt(s). " +
                $"Quality score: {biomeMapGenerator.LastQualityReport?.Score:F2}. " +
                $"Modifiers: {generationContext.Modifiers.GetDebugSummary()}.",
                this);
        }
    }

    private HexMapGenerationModifiers ResolveGenerationModifiers()
    {
        HexMapGenerationModifiers boonModifiers = HexBoonSelectionService.GetMapGenerationModifiers();
        HexMapGenerationModifiers sceneLandmarkModifiers = new(
            0,
            HexBoonMapPlacementBand.Default,
            terrainLandmarkRequests: terrainLandmarkRequests,
            mapObjectPlacementRequests: mapObjectPlacementRequests);
        return boonModifiers.Combine(sceneLandmarkModifiers);
    }

    private void ClearGeneratedMap()
    {
        List<GameObject> childObjects = new();
        foreach (Transform child in transform)
        {
            childObjects.Add(child.gameObject);
        }

        for (int index = 0; index < childObjects.Count; index++)
        {
            if (Application.isPlaying)
            {
                Destroy(childObjects[index]);
            }
            else
            {
                DestroyImmediate(childObjects[index]);
            }
        }

        tiles = null;
        tileViews = new Dictionary<HexCoordinates, HexagonTile>();
        gridData = null;
        pathfinder = null;
        PlacementReservations = new HexMapPlacementReservations();
        MapObjectPlacements = new HexMapObjectPlacementCollection();
        ActiveGenerationModifiers = HexMapGenerationModifiers.None;
    }

    public void ApplyMapObjectPlacementPlan(HexMapObjectPlacementPlanResult placementPlan)
    {
        if (placementPlan == null)
        {
            MapObjectPlacements = new HexMapObjectPlacementCollection();
            return;
        }

        MapObjectPlacements = placementPlan.Placements?.Clone() ?? new HexMapObjectPlacementCollection();
        PlacementReservations = placementPlan.PlacementReservations?.Clone() ?? PlacementReservations ?? new HexMapPlacementReservations();
    }

    // Calculates the position of a tile based on its row and column
    private Vector3 CalculateTilePosition(int row, int col, float rowOffset) {
        
        float posX = col + X1stTileSpacing;
        float posY = row * rowOffset;

        if (row % 2 == 1) // Adjust for hexagonal staggering
        {
            posX += XTileSpacing / 2;
        }

        return new Vector3(posX, 0, posY);
    }

    // Instantiates a tile prefab at the given position
    private HexagonTile InstantiateTile(Vector3 position, HexTileData tileData)
    {
        GameObject[] biomePrefabs = GetBiomePrefabs(tileData.Biome);
        if (biomePrefabs == null || biomePrefabs.Length == 0)
        {
            Debug.LogError($"No prefabs configured for biome '{tileData.Biome}'.");
            return null;
        }

        GameObject tileObject = Instantiate(biomePrefabs[Random.Range(0, biomePrefabs.Length)], position, Quaternion.Euler(0, rotation, 0));
        tileObject.name = tileData.Coordinates.ToString();
        tileObject.transform.SetParent(transform);

        HexagonTile tile = tileObject.GetComponent<HexagonTile>();
        if (tile == null)
        {
            Debug.LogError($"Spawned tile '{tileObject.name}' is missing a HexagonTile component.");
            return null;
        }

        tile.Initialize(tileData);
        return tile;
    }

    // Assigns neighbors to each tile based on hexagonal grid logic
    private void AssignNeighbors()
    {
        for (int x = 0; x < Rows; x++)
        {
            for (int y = 0; y < Columns; y++)
            {
                HexagonTile tile = tiles[x, y];
                if (tile == null)
                {
                    continue;
                }

                tile.ClearNeighbors();
                foreach (HexTileData neighborData in gridData.GetNeighbors(tile.Coordinates))
                {
                    if (!neighborData.IsPassable)
                    {
                        continue;
                    }

                    if (tileViews.TryGetValue(neighborData.Coordinates, out HexagonTile neighborView))
                    {
                        tile.neighbors.Add(neighborView);
                    }
                }
            }
        }
    }

    public bool TryGetTileData(HexCoordinates coordinates, out HexTileData tileData)
    {
        tileData = null;
        return gridData != null && gridData.TryGetTile(coordinates, out tileData);
    }

    public bool TryGetTileView(HexCoordinates coordinates, out HexagonTile tile)
    {
        return tileViews.TryGetValue(coordinates, out tile);
    }

    public bool TryGetStartTileView(out HexagonTile tile)
    {
        return TryGetTileView(StartCoordinates, out tile);
    }

    public bool TryGetGoalTileView(out HexagonTile tile)
    {
        return TryGetTileView(GoalCoordinates, out tile);
    }

    public bool TryApplyBiomeOverride(HexCoordinates coordinates, Biome biome)
    {
        if (gridData == null || !gridData.TryGetTile(coordinates, out HexTileData tileData))
        {
            return false;
        }

        HexScriptableObject properties = GetHexProperties(biome);
        if (properties == null)
        {
            return false;
        }

        tileData.ApplyBiome(biome, properties);
        if (tileViews.TryGetValue(coordinates, out HexagonTile tileView) && tileView != null)
        {
            tileView.ApplyRuntimeBiomeVisual(PickBiomePrefab(biome), properties);
        }

        AssignNeighbors();
        return true;
    }

    public IReadOnlyList<HexTileData> FindPath(HexCoordinates start, HexCoordinates goal)
    {
        return pathfinder?.FindPath(start, goal);
    }

    public IReadOnlyList<HexTileData> GetReachableTiles(HexCoordinates start, int movementBudget)
    {
        if (movementBudget < 0)
        {
            return new List<HexTileData>();
        }

        return pathfinder?.GetReachableTiles(start, movementBudget) ?? new List<HexTileData>();
    }

    private bool ValidateConfiguration()
    {
        bool isValid = true;

        if (hexProperties == null || hexProperties.Length == 0)
        {
            Debug.LogError("MapGenerator requires at least one HexScriptableObject configuration.", this);
            isValid = false;
        }

        foreach (Biome biome in System.Enum.GetValues(typeof(Biome)))
        {
            isValid &= ValidateBiomeSetup(biome);
            isValid &= ValidateBiomeProperties(biome);
        }

        return isValid;
    }

    private bool ValidateBiomeSetup(Biome biome)
    {
        GameObject[] biomePrefabs = GetBiomePrefabs(biome);
        if (biomePrefabs == null || biomePrefabs.Length == 0)
        {
            Debug.LogError($"MapGenerator has no prefabs configured for biome '{biome}'.", this);
            return false;
        }

        return true;
    }

    private bool ValidateBiomeProperties(Biome biome)
    {
        if (GetHexProperties(biome) == null)
        {
            Debug.LogError($"MapGenerator has no HexScriptableObject configured for biome '{biome}'.", this);
            return false;
        }

        return true;
    }

    private void LoadScriptableObjects() {
        HexScriptableObject[] loadedHexProperties = Resources.LoadAll<HexScriptableObject>("Scriptable Object"); // Assumes your items are in a Resources/Items folder
        if (loadedHexProperties != null && loadedHexProperties.Length > 0)
        {
            hexProperties = loadedHexProperties;
        }

        InitializeDictionary();
    }

    private void InitializeDictionary() {

        hexsDictionary = new Dictionary<Biome, HexScriptableObject>();
        if (hexProperties == null)
        {
            return;
        }

        foreach (HexScriptableObject hex in hexProperties) {

            if (hex != null) {
                if (!hexsDictionary.ContainsKey(hex.type)) {
                    hexsDictionary.Add(hex.type, hex);
                } else {
                    Debug.LogWarning($"Duplicate hex '{hex.type}' found. Skipping item '{hex.type}'.");
                }
            } else {
                Debug.LogWarning("Hex is null. Skipping item.");
            }
        }
    }

}
