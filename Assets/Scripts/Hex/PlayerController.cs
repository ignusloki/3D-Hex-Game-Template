using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public Text resourcesText;
    public Text travelTimeText;
    public Text selectionStatusText;
    public Text tileDetailsText;
    public Text hintText;
    public Transform caravanVisual;
    public Transform goalVisual;
    [Min(1)] public int startingFood = 30;
    [Min(1)] public int startingMorale = 3;
    [Min(0)] public int startingGold = 3;
    [Min(0.1f)] public float caravanHeight = 0.65f;
    [Min(0.1f)] public float caravanScale = 0.45f;
    [Min(0.1f)] public float goalHeight = 0.5f;
    [Min(0.1f)] public float goalScale = 0.3f;
    public Color caravanColor = new(0.95f, 0.76f, 0.29f, 1f);
    public Color goalColor = new(0.25f, 0.87f, 0.44f, 1f);
    public Color outOfRangeSelectionColor = new(0.42f, 0.65f, 0.95f, 1f);

    private HexTileInputService inputService;
    private HexPathHighlighter pathHighlighter;
    private HexTravelTimePresenter travelTimePresenter;
    private HexHudPresenter hudPresenter;
    private MapGenerator mapGenerator;
    private PitstopSpawner pitstopSpawner;
    private HexFogOfWarController fogOfWarController;
    private HexObstacleController obstacleController;
    private readonly CaravanResourceState caravanResources = new();

    private HexagonTile currentTile;
    private HexagonTile goalTile;
    private HexagonTile selectedTile;
    private IReadOnlyList<HexTileData> previewPath;
    private bool caravanSelectionActive;
    private bool isReady;
    private bool isRunOver;

    private void Awake()
    {
        EnsureRuntimeReferences();
        travelTimePresenter.Reset();
        hudPresenter.ResetTileDetails();
    }

    private void OnEnable()
    {
        EnsureRuntimeReferences();
    }

    private void OnValidate()
    {
        AutoAssignTextReferences();
        startingFood = Mathf.Max(1, startingFood);
        startingMorale = Mathf.Max(1, startingMorale);
        startingGold = Mathf.Max(0, startingGold);
        caravanHeight = Mathf.Max(0.1f, caravanHeight);
        caravanScale = Mathf.Max(0.1f, caravanScale);
        goalHeight = Mathf.Max(0.1f, goalHeight);
        goalScale = Mathf.Max(0.1f, goalScale);
    }

    private IEnumerator Start()
    {
        EnsureRuntimeReferences();

        while (mapGenerator == null || mapGenerator.GridData == null)
        {
            EnsureRuntimeReferences();
            mapGenerator = FindAnyObjectByType<MapGenerator>();
            yield return null;
        }

        pitstopSpawner = FindAnyObjectByType<PitstopSpawner>();
        while (pitstopSpawner != null && !pitstopSpawner.IsSpawnComplete)
        {
            EnsureRuntimeReferences();
            yield return null;
        }

        if (!TrySpawnCaravan())
        {
            yield break;
        }

        if (!TryAssignGoal())
        {
            yield break;
        }

        InitializeFogOfWar();
        HexFogUpdateResult initialFogUpdate = RefreshFogOfWar();
        InitializeObstacleSystem(initialFogUpdate);

        caravanResources.Initialize(startingFood, startingMorale, startingGold);
        UpdateResourcesText();
        isReady = true;
        RefreshTileDetails(currentTile);
        hudPresenter.ShowCaravanIdle(currentTile);
    }

    private void Update()
    {
        EnsureRuntimeReferences();

        if (!isReady || isRunOver)
        {
            return;
        }

        if (inputService.TryGetClickedTile(Camera.main, Input.mousePosition, out HexagonTile tile))
        {
            HandleTileClick(tile);
        }
    }

    private void HandleTileClick(HexagonTile clickedTile)
    {
        if (clickedTile == null || !(clickedTile.TileData?.IsPassable ?? clickedTile.canTravelThrough))
        {
            return;
        }

        if (selectedTile == clickedTile)
        {
            HandleRepeatedSelection(clickedTile);
            return;
        }

        ClearHighlights();

        if (clickedTile == currentTile)
        {
            selectedTile = clickedTile;
            RefreshTileDetails(clickedTile);
            caravanSelectionActive = true;
            previewPath = null;
            travelTimePresenter.Reset();
            hudPresenter.ShowCaravanSelected(currentTile);
            RefreshHighlights();
            return;
        }

        selectedTile = clickedTile;
        RefreshTileDetails(clickedTile);

        if (!caravanSelectionActive)
        {
            previewPath = null;
            travelTimePresenter.Reset();
            hudPresenter.ShowInspectingTile(clickedTile);
            RefreshHighlights();
            return;
        }

        if (!IsWithinImmediateMovementRange(clickedTile))
        {
            previewPath = null;
            travelTimePresenter.Reset();
            hudPresenter.ShowOutOfRangeDestination(clickedTile);
            RefreshHighlights();
            return;
        }

        previewPath = mapGenerator.FindPath(currentTile.Coordinates, clickedTile.Coordinates);
        if (previewPath == null)
        {
            travelTimePresenter.Reset();
            hudPresenter.ShowUnreachableDestination(clickedTile);
        }
        else
        {
            travelTimePresenter.ShowPath(previewPath);
            int moveCost = HexPathMetrics.GetTravelCost(previewPath);
            if (moveCost > caravanResources.Food)
            {
                hudPresenter.ShowInsufficientResources(clickedTile, moveCost, caravanResources.Food);
            }
            else
            {
                hudPresenter.ShowDestinationPreview(clickedTile, previewPath);
            }
        }

        RefreshHighlights();
    }

    private void HandleRepeatedSelection(HexagonTile clickedTile)
    {
        if (clickedTile == currentTile)
        {
            ClearSelection();
            return;
        }

        if (caravanSelectionActive && !IsWithinImmediateMovementRange(clickedTile))
        {
            travelTimePresenter.Reset();
            hudPresenter.ShowOutOfRangeDestination(clickedTile);
            return;
        }

        if (!caravanSelectionActive || previewPath == null)
        {
            hudPresenter.ShowInspectingTile(clickedTile);
            return;
        }

        int moveCost = HexPathMetrics.GetTravelCost(previewPath);
        if (moveCost > caravanResources.Food)
        {
            hudPresenter.ShowInsufficientResources(clickedTile, moveCost, caravanResources.Food);
            return;
        }

        CommitMove(clickedTile, moveCost);
    }

    private void CommitMove(HexagonTile destinationTile, int moveCost)
    {
        ClearHighlights();

        currentTile.TileData?.SetOccupied(false);
        currentTile = destinationTile;
        currentTile.TileData?.SetOccupied(true);
        caravanResources.Spend(CaravanResourceType.Food, moveCost);
        UpdateResourcesText();

        AttachCaravanToTile(currentTile);
        HexFogUpdateResult fogUpdate = RefreshFogOfWar();

        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;

        travelTimePresenter.Reset();
        if (caravanResources.IsDefeated)
        {
            RefreshTileDetails(currentTile);
            EndRunAsDefeat(caravanResources.GetDefeatReason());
            return;
        }

        HexObstacleTurnResult obstacleTurnResult = ProcessObstacleTurn(fogUpdate);
        RefreshTileDetails(currentTile);
        if (caravanResources.IsDefeated)
        {
            EndRunAsDefeat(caravanResources.GetDefeatReason());
            return;
        }

        if (goalTile != null && currentTile == goalTile)
        {
            EndRunAsVictory();
            return;
        }

        if (obstacleTurnResult.ContactResult.HasContact)
        {
            hudPresenter.ShowObstacleEncounter(currentTile, obstacleTurnResult.ContactResult, caravanResources.ToSnapshot());
        }
        else
        {
            hudPresenter.ShowMoveComplete(currentTile, moveCost, caravanResources.ToSnapshot());
        }
        RefreshHighlights();
    }

    private void ClearSelection()
    {
        ClearHighlights();

        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;

        travelTimePresenter.Reset();
        hudPresenter.ResetTileDetails();
        hudPresenter.ShowCaravanIdle(currentTile);
        RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        pathHighlighter.Reset(currentTile, selectedTile, previewPath, mapGenerator);

        if (caravanSelectionActive && currentTile != null)
        {
            pathHighlighter.HighlightEndpoint(currentTile);
        }

        if (selectedTile != null && selectedTile != currentTile)
        {
            pathHighlighter.HighlightEndpoint(selectedTile, ResolveSelectionHighlightColor(selectedTile));
        }

        if (caravanSelectionActive && previewPath != null && currentTile != null && selectedTile != null)
        {
            pathHighlighter.HighlightPath(previewPath, currentTile, selectedTile, mapGenerator);
        }
    }

    private void ClearHighlights()
    {
        pathHighlighter.Reset(currentTile, selectedTile, previewPath, mapGenerator);
    }

    private bool TrySpawnCaravan()
    {
        if (!mapGenerator.TryGetStartTileView(out currentTile) || currentTile == null)
        {
            Debug.LogError("PlayerController could not resolve the configured start tile.", this);
            return false;
        }

        if (!(currentTile.TileData?.IsAvailable ?? false))
        {
            Debug.LogError($"PlayerController start tile {currentTile.Coordinates} is not available for caravan spawn.", this);
            return false;
        }

        currentTile.TileData?.SetOccupied(true);
        EnsureCaravanVisual();
        AttachCaravanToTile(currentTile);
        RefreshTileDetails(currentTile);
        return true;
    }

    private bool TryAssignGoal()
    {
        if (!mapGenerator.TryGetGoalTileView(out goalTile) || goalTile == null)
        {
            Debug.LogError("PlayerController could not resolve the configured goal tile.", this);
            return false;
        }

        if (!(goalTile.TileData?.IsPassable ?? false))
        {
            Debug.LogError($"PlayerController goal tile {goalTile.Coordinates} is not passable.", this);
            return false;
        }

        EnsureGoalVisual();
        AttachGoalToTile(goalTile);
        return true;
    }

    private void EnsureCaravanVisual()
    {
        if (caravanVisual == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Caravan Placeholder";
            cube.transform.localScale = Vector3.one * caravanScale;

            Collider cubeCollider = cube.GetComponent<Collider>();
            if (cubeCollider != null)
            {
                Destroy(cubeCollider);
            }

            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color = caravanColor;
            }

            caravanVisual = cube.transform;
        }

        caravanVisual.localScale = Vector3.one * caravanScale;

        if (caravanVisual.TryGetComponent<Collider>(out Collider collider))
        {
            collider.enabled = false;
        }
    }

    private void EnsureGoalVisual()
    {
        if (goalVisual == null)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Goal Marker";
            sphere.transform.localScale = Vector3.one * goalScale;

            Collider sphereCollider = sphere.GetComponent<Collider>();
            if (sphereCollider != null)
            {
                Destroy(sphereCollider);
            }

            Renderer sphereRenderer = sphere.GetComponent<Renderer>();
            if (sphereRenderer != null)
            {
                sphereRenderer.material.color = goalColor;
            }

            goalVisual = sphere.transform;
        }

        goalVisual.localScale = Vector3.one * goalScale;

        if (goalVisual.TryGetComponent<Collider>(out Collider collider))
        {
            collider.enabled = false;
        }
    }

    private void AttachCaravanToTile(HexagonTile tile)
    {
        if (caravanVisual == null || tile == null)
        {
            return;
        }

        caravanVisual.SetParent(tile.transform, false);
        caravanVisual.localPosition = new Vector3(0f, caravanHeight, 0f);
        caravanVisual.localRotation = Quaternion.identity;
        caravanVisual.localScale = Vector3.one * caravanScale;
    }

    private void AttachGoalToTile(HexagonTile tile)
    {
        if (goalVisual == null || tile == null)
        {
            return;
        }

        goalVisual.SetParent(tile.transform, false);
        goalVisual.localPosition = new Vector3(0f, goalHeight, 0f);
        goalVisual.localRotation = Quaternion.identity;
        goalVisual.localScale = Vector3.one * goalScale;
    }

    private void UpdateResourcesText()
    {
        if (resourcesText != null)
        {
            resourcesText.text =
                $"Food: {Mathf.Max(caravanResources.Food, 0)}  Morale: {Mathf.Max(caravanResources.Morale, 0)}  Gold: {Mathf.Max(caravanResources.Gold, 0)}";
        }
    }

    private void EndRunAsVictory()
    {
        isRunOver = true;
        ClearHighlights();
        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;
        travelTimePresenter.Reset();
        RefreshTileDetails(currentTile);
        hudPresenter.ShowVictory(goalTile, caravanResources.ToSnapshot());
    }

    private void EndRunAsDefeat(string defeatReason)
    {
        isRunOver = true;
        ClearHighlights();
        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;
        travelTimePresenter.Reset();
        RefreshTileDetails(currentTile);
        hudPresenter.ShowDefeat(currentTile, defeatReason);
    }

    private void RefreshTileDetails(HexagonTile tile)
    {
        if (tile == null)
        {
            hudPresenter.ResetTileDetails();
            return;
        }

        pitstopSpawner ??= FindAnyObjectByType<PitstopSpawner>();
        PitstopSite pitstopSite = null;
        if (pitstopSpawner != null)
        {
            pitstopSpawner.TryGetPitstop(tile.Coordinates, out pitstopSite);
        }

        obstacleController ??= FindAnyObjectByType<HexObstacleController>();
        HexObstacleInstance visibleObstacle = null;
        if (obstacleController != null)
        {
            obstacleController.TryGetVisibleObstacle(tile.Coordinates, out visibleObstacle);
        }

        hudPresenter.ShowTileDetails(tile, pitstopSite, visibleObstacle);
    }

    private void InitializeFogOfWar()
    {
        fogOfWarController ??= GetComponent<HexFogOfWarController>() ?? gameObject.AddComponent<HexFogOfWarController>();
        fogOfWarController.Initialize(mapGenerator, BuildAlwaysKnownCoordinates());
    }

    private HexFogUpdateResult RefreshFogOfWar()
    {
        if (fogOfWarController == null || currentTile == null)
        {
            return HexFogUpdateResult.Empty;
        }

        return fogOfWarController.RefreshVisibility(currentTile.Coordinates, BuildAlwaysKnownCoordinates());
    }

    private IEnumerable<HexCoordinates> BuildAlwaysKnownCoordinates()
    {
        HashSet<HexCoordinates> alwaysKnownCoordinates = new();

        if (goalTile != null)
        {
            alwaysKnownCoordinates.Add(goalTile.Coordinates);
        }
        else if (mapGenerator != null)
        {
            alwaysKnownCoordinates.Add(mapGenerator.GoalCoordinates);
        }

        if (pitstopSpawner != null)
        {
            foreach (HexCoordinates coordinates in pitstopSpawner.SpawnedSites.Keys)
            {
                alwaysKnownCoordinates.Add(coordinates);
            }
        }

        return alwaysKnownCoordinates;
    }

    private Color ResolveSelectionHighlightColor(HexagonTile tile)
    {
        if (tile == null || currentTile == null || tile == currentTile)
        {
            return new Color(0f, 1f, 0f, 1f);
        }

        bool isWithinImmediateMovementRange = IsWithinImmediateMovementRange(tile);
        return isWithinImmediateMovementRange
            ? new Color(0f, 1f, 0f, 1f)
            : outOfRangeSelectionColor;
    }

    private bool IsWithinImmediateMovementRange(HexagonTile tile)
    {
        return tile != null && currentTile != null && currentTile.Coordinates.DistanceTo(tile.Coordinates) <= 1;
    }

    private void AutoAssignTextReferences()
    {
        resourcesText = FindTextReference(resourcesText, "Resources Text");
        travelTimeText = FindTextReference(travelTimeText, "Travel Time Text");
        selectionStatusText = FindTextReference(selectionStatusText, "Selection Status Text");
        tileDetailsText = FindTextReference(tileDetailsText, "Tile Details Text");
        hintText = FindTextReference(hintText, "Hint Text");
    }

    private static Text FindTextReference(Text currentValue, string objectName)
    {
        if (currentValue != null)
        {
            return currentValue;
        }

        GameObject textObject = GameObject.Find(objectName);
        return textObject != null ? textObject.GetComponent<Text>() : null;
    }

    private void EnsureRuntimeReferences()
    {
        AutoAssignTextReferences();

        inputService ??= new HexTileInputService();
        pathHighlighter ??= new HexPathHighlighter();
        travelTimePresenter ??= new HexTravelTimePresenter(travelTimeText);
        hudPresenter ??= new HexHudPresenter(selectionStatusText, tileDetailsText, hintText);
        mapGenerator ??= FindAnyObjectByType<MapGenerator>();
        pitstopSpawner ??= FindAnyObjectByType<PitstopSpawner>();
        fogOfWarController ??= GetComponent<HexFogOfWarController>() ?? gameObject.AddComponent<HexFogOfWarController>();
        obstacleController ??= FindAnyObjectByType<HexObstacleController>();
    }

    private void InitializeObstacleSystem(HexFogUpdateResult initialFogUpdate)
    {
        if (obstacleController == null)
        {
            return;
        }

        obstacleController.Initialize(mapGenerator, pitstopSpawner, fogOfWarController);
        obstacleController.SyncVisibility(initialFogUpdate);
    }

    private HexObstacleTurnResult ProcessObstacleTurn(HexFogUpdateResult fogUpdate)
    {
        if (obstacleController == null)
        {
            return HexObstacleTurnResult.Empty;
        }

        HexObstacleTurnResult turnResult = obstacleController.ProcessTurn(fogUpdate, currentTile.Coordinates, caravanResources.ToSnapshot());
        if (!turnResult.ContactResult.HasContact)
        {
            return turnResult;
        }

        caravanResources.Spend(turnResult.ContactResult.AffectedResource, turnResult.ContactResult.AmountDrained);
        UpdateResourcesText();
        return turnResult;
    }
}
