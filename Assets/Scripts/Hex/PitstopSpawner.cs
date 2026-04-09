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

    private readonly PitstopPlacementPlanner placementPlanner = new();
    private readonly Dictionary<HexCoordinates, PitstopSite> spawnedSites = new();
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

        SpawnPitstops();
        IsSpawnComplete = true;
    }

    public bool TryGetPitstop(HexCoordinates coordinates, out PitstopSite site)
    {
        return spawnedSites.TryGetValue(coordinates, out site);
    }

    private void SpawnPitstops()
    {
        spawnedSites.Clear();

        if (mapGenerator == null || mapGenerator.GridData == null || mapGenerator.Pathfinder == null)
        {
            Debug.LogError("PitstopSpawner requires a generated map and pathfinder.", this);
            IsSpawnComplete = true;
            return;
        }

        PitstopLayoutResult layoutResult = placementPlanner.GeneratePitstops(
            mapGenerator.GridData,
            mapGenerator.Pathfinder,
            mapGenerator.StartCoordinates,
            mapGenerator.GoalCoordinates,
            placementSettings,
            new System.Random());

        IReadOnlyList<HexCoordinates> placements = layoutResult.Coordinates;
        if (placements.Count == 0)
        {
            Debug.LogWarning("PitstopSpawner did not find any valid pitstop placements.", this);
            IsSpawnComplete = true;
            return;
        }

        List<PitstopKind> kindSequence = BuildKindSequence(placements.Count);
        for (int index = 0; index < placements.Count; index++)
        {
            HexCoordinates coordinates = placements[index];
            if (!mapGenerator.TryGetTileView(coordinates, out HexagonTile tileView) || tileView == null)
            {
                continue;
            }

            PitstopKind kind = kindSequence[index];
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

        if (layoutResult.IsValid)
        {
            Debug.Log($"Spawned {spawnedSites.Count} pitstops across the map. {layoutResult.Summary} Attempts: {layoutResult.AttemptsUsed}.", this);
        }
        else
        {
            Debug.LogWarning($"Spawned {spawnedSites.Count} pitstops using the best available layout. {layoutResult.Summary} Attempts: {layoutResult.AttemptsUsed}.", this);
        }
    }

    private List<PitstopKind> BuildKindSequence(int count)
    {
        List<PitstopKind> sequence = new(count);
        PitstopKind[] availableKinds =
        {
            PitstopKind.Mill,
            PitstopKind.WallTower,
            PitstopKind.Mansion
        };

        for (int index = 0; index < count; index++)
        {
            sequence.Add(availableKinds[index % availableKinds.Length]);
        }

        for (int index = sequence.Count - 1; index > 0; index--)
        {
            int swapIndex = Random.Range(0, index + 1);
            (sequence[index], sequence[swapIndex]) = (sequence[swapIndex], sequence[index]);
        }

        return sequence;
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
