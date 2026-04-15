using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PitstopSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject millPrefab;
    [SerializeField] private GameObject wallTowerPrefab;
    [SerializeField] private GameObject mansionPrefab;

    [Header("Placement")]
    [SerializeField] private PitstopPlacementSettings placementSettings = new();
    [Min(0f)] [SerializeField] private float visualHeight = 0.08f;
    [Min(0.1f)] [SerializeField] private float visualScale = 1.2f;
    [SerializeField] private bool randomizeRotation = true;

    private readonly HexMapObjectPlacementPass placementPass = new();
    private readonly Dictionary<HexCoordinates, PitstopSite> spawnedSites = new();
    private const int DetailedRetryLogInterval = 10;
    private MapGenerator mapGenerator;

    public IReadOnlyDictionary<HexCoordinates, PitstopSite> SpawnedSites => spawnedSites;
    public bool IsSpawnComplete { get; private set; }

    private void Awake()
    {
        mapGenerator = FindAnyObjectByType<MapGenerator>();
    }

    private void OnValidate()
    {
        placementSettings ??= new PitstopPlacementSettings();
        placementSettings.Validate();
        visualHeight = Mathf.Max(0f, visualHeight);
        visualScale = Mathf.Max(0.1f, visualScale);
        AutoAssignPrefabs();
    }

    private IEnumerator Start()
    {
        AutoAssignPrefabs();
        IsSpawnComplete = false;

        while (mapGenerator == null || mapGenerator.GridData == null)
        {
            mapGenerator = FindAnyObjectByType<MapGenerator>();
            yield return null;
        }

        yield return SpawnPitstopsUntilValidLayout();
        IsSpawnComplete = true;
    }

    public bool TryGetPitstop(HexCoordinates coordinates, out PitstopSite site)
    {
        return spawnedSites.TryGetValue(coordinates, out site);
    }

    private IEnumerator SpawnPitstopsUntilValidLayout()
    {
        if (mapGenerator == null || mapGenerator.GridData == null || mapGenerator.Pathfinder == null)
        {
            Debug.LogError("PitstopSpawner requires a generated map and pathfinder.", this);
            IsSpawnComplete = true;
            yield break;
        }

        HexMapObjectPlacementPlanResult bestFallbackPlan = HexMapObjectPlacementPlanResult.Empty;
        int mapAttempt = 0;
        while (true)
        {
            spawnedSites.Clear();
            HexMapGenerationModifiers generationModifiers = HexBoonSelectionService.GetMapGenerationModifiers();

            HexMapObjectPlacementPlanResult placementPlan = placementPass.PlanPitstops(
                mapGenerator.GridData,
                mapGenerator.Pathfinder,
                mapGenerator.StartCoordinates,
                mapGenerator.GoalCoordinates,
                placementSettings,
                generationModifiers,
                mapGenerator.PlacementReservations,
                new System.Random());

            if (placementPlan.Score > bestFallbackPlan.Score)
            {
                bestFallbackPlan = placementPlan;
            }

            List<HexMapObjectPlacement> pitstopPlacements = placementPlan.Placements.GetByType(HexMapObjectType.Pitstop);
            if (placementPlan.IsValid && pitstopPlacements.Count > 0)
            {
                mapGenerator.ApplyMapObjectPlacementPlan(placementPlan);
                SpawnPitstopLayout(pitstopPlacements);
                string diagnosticsSuffix = placementSettings.enableDebugLogging
                    ? $" {placementPlan.DiagnosticsSummary}"
                    : string.Empty;
                Debug.Log($"Spawned {spawnedSites.Count} pitstops across the map. {placementPlan.Summary} Attempts: {placementPlan.AttemptsUsed}. Map rerolls: {mapAttempt}.{diagnosticsSuffix}", this);
                yield break;
            }

            bool canRegenerateMap = placementSettings.CanRetryMap(mapAttempt);
            if (!canRegenerateMap)
            {
                break;
            }

            mapAttempt++;
            if (mapAttempt == 1 || mapAttempt % DetailedRetryLogInterval == 0)
            {
                string retryMode = placementSettings.retryMapUntilValidLayoutWithoutLimit
                    ? "Retry mode: unlimited."
                    : $"Retry mode: capped at {placementSettings.maxMapRegenerationAttempts} rerolls.";

                Debug.LogWarning(
                    $"Pitstop layout attempt failed on map variant {mapAttempt}. Regenerating map and retrying pitstop placement. " +
                    $"Planner summary: {placementPlan.Summary} {(placementSettings.enableDebugLogging ? placementPlan.DiagnosticsSummary + " " : string.Empty)}{retryMode}",
                    this);
            }

            yield return mapGenerator.RegenerateMapCoroutine(true);
            while (mapGenerator.GridData == null || mapGenerator.Pathfinder == null)
            {
                yield return null;
            }
        }

        List<HexMapObjectPlacement> bestFallbackPitstops = bestFallbackPlan.Placements.GetByType(HexMapObjectType.Pitstop);
        if (bestFallbackPitstops.Count > 0 && placementSettings.keepBestLayoutIfAllAttemptsFail)
        {
            mapGenerator.ApplyMapObjectPlacementPlan(bestFallbackPlan);
            SpawnPitstopLayout(bestFallbackPitstops);
            Debug.LogWarning(
                $"Spawned {spawnedSites.Count} pitstops using the best available layout after {mapAttempt} map rerolls. " +
                $"{bestFallbackPlan.Summary} Attempts: {bestFallbackPlan.AttemptsUsed}. {(placementSettings.enableDebugLogging ? bestFallbackPlan.DiagnosticsSummary : string.Empty)}",
                this);
            yield break;
        }

        Debug.LogError(
            $"PitstopSpawner could not generate a valid layout after {mapAttempt} map rerolls and {placementSettings.maxGenerationAttempts} planner attempts per map. " +
            $"Relax the rules or increase the reroll limit.",
            this);
    }

    private void SpawnPitstopLayout(IReadOnlyList<HexMapObjectPlacement> placements)
    {
        for (int index = 0; index < placements.Count; index++)
        {
            HexMapObjectPlacement placement = placements[index];
            HexCoordinates coordinates = placement.Coordinates;
            if (!mapGenerator.TryGetTileView(coordinates, out HexagonTile tileView) || tileView == null)
            {
                continue;
            }

            if (!placement.TryGetPitstopKind(out PitstopKind kind))
            {
                Debug.LogWarning($"Map object placement at {coordinates} was not a valid pitstop variant.", this);
                continue;
            }

            GameObject prefab = GetPrefab(kind);
            if (prefab == null)
            {
                Debug.LogWarning($"PitstopSpawner has no prefab assigned for {kind}.", this);
                continue;
            }

            GameObject instance = Instantiate(prefab, tileView.transform);
            instance.name = $"Pitstop_{kind}_{coordinates}";
            instance.transform.localPosition = new Vector3(0f, visualHeight, 0f);
            instance.transform.localRotation = Quaternion.Euler(0f, randomizeRotation ? Random.Range(0, 6) * 60f : 0f, 0f);
            instance.transform.localScale = Vector3.one * visualScale;
            DisableColliders(instance);

            PitstopSite site = instance.GetComponent<PitstopSite>();
            if (site == null)
            {
                site = instance.AddComponent<PitstopSite>();
            }

            site.Initialize(kind, coordinates);
            spawnedSites[coordinates] = site;
        }
    }

    private GameObject GetPrefab(PitstopKind kind)
    {
        return kind switch
        {
            PitstopKind.Mill => millPrefab,
            PitstopKind.WallTower => wallTowerPrefab,
            PitstopKind.Mansion => mansionPrefab,
            _ => null
        };
    }

    private static void DisableColliders(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }
    }

    private void AutoAssignPrefabs()
    {
#if UNITY_EDITOR
        millPrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Kenny/FBX format/unit-mill.fbx");
        wallTowerPrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Kenny/FBX format/unit-wall-tower.fbx");
        mansionPrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Kenny/FBX format/unit-mansion.fbx");
#endif
    }
}
