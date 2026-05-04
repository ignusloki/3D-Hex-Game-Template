using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerController : MonoBehaviour
{
    private const string CurrentTileSelectionPrefabPath = "Assets/Prefabs/Selection Hex.prefab";

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
    public Color selectedHexColor = new(0.12f, 0.78f, 1f, 0.72f);
    [Min(0.01f)] public float selectedHexMarkerRadius = 0.58f;
    [Min(0.01f)] public float selectedHexMarkerScale = 1.2f;
    [Min(0f)] public float selectedHexMarkerYOffset = 0.1f;
    public GameObject currentTileSelectionPrefab;
    [Min(0.01f)] public float currentTileSelectionScale = 1f;
    [Min(0f)] public float currentTileSelectionYOffset = 0.12f;
    public Color outOfRangeSelectionColor = new(0.42f, 0.65f, 0.95f, 1f);

    private HexTileInputService inputService;
    private HexPathHighlighter pathHighlighter;
    private HexSelectionMarker selectionMarker;
    private GameObject currentTileSelectionInstance;
    private HexTravelTimePresenter travelTimePresenter;
    private HexHudPresenter hudPresenter;
    private HexGameplayUiRootController gameplayUiRootController;
    private HexHudDocumentController hudDocumentController;
    private MapGenerator mapGenerator;
    private CaravanMetricsController caravanMetricsController;
    private PitstopSpawner pitstopSpawner;
    private PitstopEventController pitstopEventController;
    private HexFogOfWarController fogOfWarController;
    private HexObstacleController obstacleController;
    private HexNemesisController nemesisController;
    private HexRunEndModalPresenter runEndModalPresenter;
    private HexActTransitionModalPresenter actTransitionModalPresenter;
    private HexMockQuestMarkerController mockQuestMarkerController;
    private HexGlobalUiTransitionController globalTransitionController;
    private HexMainMenuPresenter mainMenuPresenter;
    private HexAudioSystem audioSystem;
    private HexNemesisTurnResult pendingDeferredNemesisResult;
    private HexBoonRuntimeState boonRuntime;
    private readonly CaravanResourceState caravanResources = new();

    private HexagonTile currentTile;
    private HexagonTile goalTile;
    private HexagonTile selectedTile;
    private IReadOnlyList<HexTileData> previewPath;
    private bool caravanSelectionActive;
    private bool isReady;
    private bool isGameplaySessionActive;
    private bool isRunOver;
    private bool resourcesInitialized;
    private bool didWarnMissingCurrentTileSelectionPrefab;
    private string pendingPitstopBoonHint;

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

    private void OnDestroy()
    {
        selectionMarker?.Destroy();
        selectionMarker = null;
        DestroyCurrentTileSelectionInstance();
    }

    private void OnValidate()
    {
        hudDocumentController = GetComponent<HexHudDocumentController>();
        startingFood = Mathf.Max(1, startingFood);
        startingMorale = Mathf.Max(1, startingMorale);
        startingGold = Mathf.Max(0, startingGold);
        caravanHeight = Mathf.Max(0.1f, caravanHeight);
        caravanScale = Mathf.Max(0.1f, caravanScale);
        goalHeight = Mathf.Max(0.1f, goalHeight);
        goalScale = Mathf.Max(0.1f, goalScale);
        selectedHexColor.a = Mathf.Clamp01(selectedHexColor.a);
        selectedHexMarkerRadius = Mathf.Max(0.01f, selectedHexMarkerRadius);
        selectedHexMarkerScale = Mathf.Max(0.01f, selectedHexMarkerScale);
        selectedHexMarkerYOffset = Mathf.Max(0f, selectedHexMarkerYOffset);
        currentTileSelectionScale = Mathf.Max(0.01f, currentTileSelectionScale);
        currentTileSelectionYOffset = Mathf.Max(0f, currentTileSelectionYOffset);
        outOfRangeSelectionColor.a = 1f;

#if UNITY_EDITOR
        currentTileSelectionPrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>(CurrentTileSelectionPrefabPath);
#endif
    }

    private IEnumerator Start()
    {
        if (HexGameBootstrap.HasActiveBootController)
        {
            yield break;
        }

        yield return PrepareGameplaySessionForBoot();
        bool startGameplayDirectly = HexGameBootstrap.ConsumeGameplayOnNextSceneLoadRequest()
            || HexActTransitionService.HasActiveRunSession();

        if (!startGameplayDirectly && mainMenuPresenter != null)
        {
            mainMenuPresenter.ShowBootMenu(ActivateGameplaySession);
        }
        else
        {
            ActivateGameplaySession();
            RevealGameplayAfterSceneLoad();
        }
    }

    public IEnumerator PrepareGameplaySessionForBoot()
    {
        if (isReady)
        {
            yield break;
        }

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

        InitializePitstopEvents();

        if (!TrySpawnCaravan())
        {
            yield break;
        }

        if (!TryAssignGoal())
        {
            yield break;
        }

        InitializeFogOfWar();
        InitializeBoonSystem();
        HexFogUpdateResult initialFogUpdate = RefreshFogOfWar();
        InitializeObstacleSystem(initialFogUpdate);
        InitializeNemesisSystem();
        InitializeMockQuestMarkerSystem();

        CaravanResourceSnapshot configuredStartingResources = caravanMetricsController != null
            ? caravanMetricsController.GetConfiguredSnapshot()
            : new CaravanResourceSnapshot(startingFood, startingMorale, startingGold);
        CaravanResourceSnapshot startingResources = HexActTransitionService.GetStartingResources(configuredStartingResources);

        caravanResources.Initialize(startingResources.Food, startingResources.Morale, startingResources.Gold);
        resourcesInitialized = true;
        UpdateResourcesText();
        caravanMetricsController?.InitializeRuntimeSnapshot(caravanResources.ToSnapshot());
        isReady = true;
        RefreshTileDetails(currentTile);
        hudPresenter.ShowCaravanIdle(currentTile);
    }

    private void Update()
    {
        EnsureRuntimeReferences();

        if (!isReady
            || !isGameplaySessionActive
            || (mainMenuPresenter != null && mainMenuPresenter.IsOpen)
            || isRunOver
            || (runEndModalPresenter != null && runEndModalPresenter.IsOpen)
            || (actTransitionModalPresenter != null && actTransitionModalPresenter.IsOpen)
            || (pitstopEventController != null && pitstopEventController.IsChoiceModalOpen)
            || (mockQuestMarkerController != null && mockQuestMarkerController.IsModalOpen))
        {
            return;
        }

        if (inputService.TryGetClickedTile(Camera.main, Input.mousePosition, out HexagonTile tile))
        {
            LogUiDiagnostic("TileDetails", $"Tile click detected at {FormatCoordinates(tile.Coordinates)}.");
            HandleTileClick(tile);
        }
    }

    private void HandleTileClick(HexagonTile clickedTile)
    {
        if (clickedTile == null || !(clickedTile.TileData?.IsPassable ?? clickedTile.canTravelThrough))
        {
            LogUiDiagnostic("TileDetails", "Ignored tile click because the tile was null or not passable.");
            return;
        }

        LogUiDiagnostic(
            "TileDetails",
            $"HandleTileClick tile={FormatCoordinates(clickedTile.Coordinates)} current={(currentTile != null ? FormatCoordinates(currentTile.Coordinates) : "none")} selected={(selectedTile != null ? FormatCoordinates(selectedTile.Coordinates) : "none")} caravanSelectionActive={caravanSelectionActive}.");
        PlayGameplaySfx(ResolveTileSelectionSfx(clickedTile));

        if (selectedTile == clickedTile)
        {
            LogUiDiagnostic("TileDetails", $"Repeated tile selection at {FormatCoordinates(clickedTile.Coordinates)}.");
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

        if (nemesisController != null && nemesisController.IsBlockedDestination(clickedTile.Coordinates))
        {
            previewPath = null;
            travelTimePresenter.Reset();
            hudPresenter.ShowNemesisBlockedDestination(clickedTile);
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
            int moveCost = GetMoveCostForSelection(clickedTile, previewPath);
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

        if (caravanSelectionActive && nemesisController != null && nemesisController.IsBlockedDestination(clickedTile.Coordinates))
        {
            travelTimePresenter.Reset();
            hudPresenter.ShowNemesisBlockedDestination(clickedTile);
            return;
        }

        if (!caravanSelectionActive || previewPath == null)
        {
            hudPresenter.ShowInspectingTile(clickedTile);
            return;
        }

        int moveCost = GetMoveCostForSelection(clickedTile, previewPath);
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
        HexCoordinates previousCoordinates = currentTile.Coordinates;

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
        if (nemesisController != null && nemesisController.TryResolveImmediateCaravanContact(currentTile.Coordinates, out string immediateContactDefeatReason))
        {
            RefreshTileDetails(currentTile);
            EndRunAsDefeat(immediateContactDefeatReason);
            return;
        }

        HexObstacleTurnResult obstacleTurnResult = ProcessObstacleTurn(fogUpdate);
        PlayGameplaySfx(obstacleTurnResult.ContactResult.HasContact ? HexSfxId.ObstacleTravel : HexSfxId.CaravanMove);
        HexNemesisTurnResult nemesisTurnResult = ProcessNemesisTurn(previousCoordinates, currentTile.Coordinates);
        if (nemesisTurnResult.Moved)
        {
            PlayGameplaySfx(HexSfxId.NemesisMove);
        }

        RefreshTileDetails(currentTile);

        if (nemesisTurnResult.CausedDefeat)
        {
            EndRunAsDefeat(nemesisTurnResult.DefeatReason);
            return;
        }

        if (goalTile != null && currentTile == goalTile)
        {
            EndRunAsVictory();
            return;
        }

        PitstopEventResult pitstopEventResult = ProcessPitstopArrival();
        string pitstopBoonHint = ProcessPitstopRecharge();
        bool hasDeferredNemesisPitstopDestruction = nemesisTurnResult.DeferredPitstopDestructions.Count > 0;

        if (caravanResources.IsDefeated)
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            EndRunAsDefeat(caravanResources.GetDefeatReason());
            return;
        }

        if (mockQuestMarkerController != null && mockQuestMarkerController.TryProcessArrival(currentTile.Coordinates))
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            RefreshHighlights();
            return;
        }

        if (pitstopEventResult.RequiresChoice)
        {
            pendingPitstopBoonHint = pitstopBoonHint;
            pendingDeferredNemesisResult = hasDeferredNemesisPitstopDestruction ? nemesisTurnResult : null;
            pitstopEventController.PresentChoice(pitstopEventResult, caravanResources, HandlePitstopChoiceResolved);
        }
        else if (pitstopEventResult.Triggered || (pitstopEventResult.Site != null && pitstopEventResult.Site.Visited))
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            hudPresenter.ShowPitstopEvent(currentTile, pitstopEventResult, caravanResources.ToSnapshot());
            if (!string.IsNullOrWhiteSpace(pitstopBoonHint))
            {
                hudPresenter.ShowHint(pitstopBoonHint);
            }
        }
        else if (obstacleTurnResult.ContactResult.HasContact)
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            hudPresenter.ShowObstacleEncounter(currentTile, obstacleTurnResult.ContactResult, caravanResources.ToSnapshot());
            if (!string.IsNullOrWhiteSpace(obstacleTurnResult.ContactPenaltyIgnoreNote))
            {
                hudPresenter.ShowHint(obstacleTurnResult.ContactPenaltyIgnoreNote);
            }
        }
        else if (nemesisTurnResult.Active && (nemesisTurnResult.Acted || nemesisTurnResult.DestroyedPitstops.Count > 0))
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            hudPresenter.ShowNemesisUpdate(currentTile, nemesisTurnResult);
        }
        else
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            hudPresenter.ShowMoveComplete(currentTile, moveCost, caravanResources.ToSnapshot());
        }
        RefreshHighlights();
    }

    private void HandlePitstopChoiceResolved(PitstopEventResult eventResult)
    {
        FinalizeDeferredNemesisPitstopDestructionIfNeeded(pendingDeferredNemesisResult);
        pendingDeferredNemesisResult = null;

        UpdateResourcesText();
        RefreshTileDetails(currentTile);

        if (caravanResources.IsDefeated)
        {
            EndRunAsDefeat(caravanResources.GetDefeatReason());
            return;
        }

        if (eventResult != null && eventResult.Site != null)
        {
            hudPresenter.ShowPitstopChoiceResolved(currentTile, eventResult, caravanResources.ToSnapshot());
        }
        else
        {
            hudPresenter.ShowCaravanIdle(currentTile);
        }

        if (!string.IsNullOrWhiteSpace(pendingPitstopBoonHint))
        {
            hudPresenter.ShowHint(pendingPitstopBoonHint);
        }

        pendingPitstopBoonHint = string.Empty;
    }

    private void FinalizeDeferredNemesisPitstopDestructionIfNeeded(HexNemesisTurnResult turnResult)
    {
        if (turnResult == null || turnResult.DeferredPitstopDestructions.Count == 0 || nemesisController == null)
        {
            return;
        }

        nemesisController.FinalizeDeferredPitstopDestructions(turnResult);
        if (currentTile != null)
        {
            RefreshTileDetails(currentTile);
        }
    }

    private void ClearSelection()
    {
        ClearHighlights();

        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;

        travelTimePresenter.Reset();
        RefreshTileDetails(currentTile);
        hudPresenter.ShowCaravanIdle(currentTile);
        RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        pathHighlighter.Reset(currentTile, selectedTile, previewPath, mapGenerator);
        RefreshSelectionMarker();

        if (caravanSelectionActive && previewPath != null && currentTile != null && selectedTile != null)
        {
            pathHighlighter.HighlightPath(previewPath, currentTile, selectedTile, mapGenerator);
        }
    }

    private void ClearHighlights()
    {
        pathHighlighter.Reset(currentTile, selectedTile, previewPath, mapGenerator);
        selectionMarker?.Hide();
        HideCurrentTileSelectionInstance();
    }

    private void RefreshSelectionMarker()
    {
        bool shouldShowCurrentTileSelection = caravanSelectionActive && currentTile != null;
        if (shouldShowCurrentTileSelection)
        {
            ShowCurrentTileSelectionInstance(currentTile);
        }
        else
        {
            HideCurrentTileSelectionInstance();
        }

        if (selectedTile == null)
        {
            selectionMarker?.Hide();
            return;
        }

        if (currentTile != null && selectedTile == currentTile)
        {
            selectionMarker?.Hide();
            return;
        }

        selectionMarker ??= new HexSelectionMarker();
        selectionMarker.Show(
            selectedTile,
            ResolveSelectionMarkerColor(selectedTile),
            selectedHexMarkerRadius,
            selectedHexMarkerScale,
            selectedHexMarkerYOffset);
    }

    private void ShowCurrentTileSelectionInstance(HexagonTile tile)
    {
        if (tile == null)
        {
            HideCurrentTileSelectionInstance();
            return;
        }

        if (currentTileSelectionPrefab == null)
        {
            WarnMissingCurrentTileSelectionPrefabOnce();
            return;
        }

        if (currentTileSelectionInstance == null)
        {
            currentTileSelectionInstance = InstantiateCurrentTileSelectionPrefab();
            if (currentTileSelectionInstance == null)
            {
                return;
            }

            currentTileSelectionInstance.name = "Current Tile Selection Marker";
            DisableSelectionMarkerColliders(currentTileSelectionInstance);
        }

        Transform markerTransform = currentTileSelectionInstance.transform;
        markerTransform.SetParent(tile.transform, false);
        markerTransform.localPosition = new Vector3(0f, currentTileSelectionYOffset, 0f);
        markerTransform.localRotation = Quaternion.identity;
        markerTransform.localScale = Vector3.one * Mathf.Max(0.01f, currentTileSelectionScale);
        currentTileSelectionInstance.SetActive(true);
    }

    private GameObject InstantiateCurrentTileSelectionPrefab()
    {
#if UNITY_EDITOR
        GameObject editorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CurrentTileSelectionPrefabPath);
        if (editorPrefab != null)
        {
            UnityEngine.Object editorInstance = PrefabUtility.InstantiatePrefab(editorPrefab);
            GameObject editorInstanceGameObject = ResolveInstantiatedSelectionGameObject(editorInstance);
            if (editorInstanceGameObject != null)
            {
                return editorInstanceGameObject;
            }
        }
#endif

        if (currentTileSelectionPrefab == null)
        {
            WarnMissingCurrentTileSelectionPrefabOnce();
            return null;
        }

        UnityEngine.Object instance = UnityEngine.Object.Instantiate((UnityEngine.Object)currentTileSelectionPrefab);
        GameObject instanceGameObject = ResolveInstantiatedSelectionGameObject(instance);
        if (instanceGameObject != null)
        {
            return instanceGameObject;
        }

        Debug.LogWarning(
            $"PlayerController could not instantiate '{CurrentTileSelectionPrefabPath}' as a GameObject. Falling back to no caravan tile selection marker.",
            this);

        if (instance != null)
        {
            Destroy(instance);
        }

        return null;
    }

    private static GameObject ResolveInstantiatedSelectionGameObject(UnityEngine.Object instance)
    {
        return instance switch
        {
            GameObject gameObject => gameObject,
            Component component => component.gameObject,
            _ => null
        };
    }

    private void HideCurrentTileSelectionInstance()
    {
        if (currentTileSelectionInstance != null)
        {
            currentTileSelectionInstance.SetActive(false);
        }
    }

    private void DestroyCurrentTileSelectionInstance()
    {
        if (currentTileSelectionInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(currentTileSelectionInstance);
        }
        else
        {
            DestroyImmediate(currentTileSelectionInstance);
        }

        currentTileSelectionInstance = null;
    }

    private void WarnMissingCurrentTileSelectionPrefabOnce()
    {
        if (didWarnMissingCurrentTileSelectionPrefab)
        {
            return;
        }

        didWarnMissingCurrentTileSelectionPrefab = true;
        Debug.LogWarning(
            $"PlayerController cannot show the caravan tile selection marker because '{CurrentTileSelectionPrefabPath}' is not assigned.",
            this);
    }

    private static void DisableSelectionMarkerColliders(GameObject markerRoot)
    {
        if (markerRoot == null)
        {
            return;
        }

        foreach (Collider collider in markerRoot.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }
    }

    private Color ResolveSelectionMarkerColor(HexagonTile tile)
    {
        if (!caravanSelectionActive || tile == null || currentTile == null)
        {
            return selectedHexColor;
        }

        return IsWithinImmediateMovementRange(tile)
            ? selectedHexColor
            : outOfRangeSelectionColor;
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
        string boonLine = boonRuntime != null && boonRuntime.HasActiveBoon
            ? boonRuntime.GetStatusLine()
            : string.Empty;

        hudDocumentController?.SetResources(
            caravanResources.Food,
            caravanResources.Morale,
            caravanResources.Gold,
            boonLine);
    }

    private void EndRunAsVictory()
    {
        if (isRunOver)
        {
            return;
        }

        if (TryBeginActTransition())
        {
            return;
        }

        HexActTransitionService.ResetRunSession();
        isRunOver = true;
        ClearHighlights();
        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;
        travelTimePresenter.Reset();
        pitstopEventController?.HideActiveModal();
        mockQuestMarkerController?.HideActiveModal();
        RefreshTileDetails(currentTile);
        hudPresenter.ShowVictory(goalTile, caravanResources.ToSnapshot());
        runEndModalPresenter?.ShowVictory(ReturnToMainMenuAfterFade);
    }

    private bool TryBeginActTransition()
    {
        if (!HexActTransitionService.CanAdvanceFromCurrentAct())
        {
            return false;
        }

        HexActTransitionDisplayData displayData = HexActTransitionService.BuildTransitionDisplayData(caravanResources.ToSnapshot());
        PrepareForActTransitionModalState();
        hudPresenter.ShowHint($"Act {HexActTransitionService.GetCurrentActNumber()} complete. Preparing the next crossing.");
        actTransitionModalPresenter?.ShowTransition(displayData, ContinueToNextAct);
        return true;
    }

    private void EndRunAsDefeat(string defeatReason)
    {
        if (isRunOver)
        {
            return;
        }

        HexActTransitionService.ResetRunSession();
        isRunOver = true;
        ClearHighlights();
        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;
        travelTimePresenter.Reset();
        pitstopEventController?.HideActiveModal();
        mockQuestMarkerController?.HideActiveModal();
        RefreshTileDetails(currentTile);
        hudPresenter.ShowDefeat(currentTile, defeatReason);
        runEndModalPresenter?.ShowDefeat(RetryCurrentScene, ReturnToMainMenuWithFade);
    }

    private void RefreshTileDetails(HexagonTile tile)
    {
        if (tile == null)
        {
            LogUiDiagnostic("TileDetails", "RefreshTileDetails reset because tile was null.");
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

        string nemesisDetails = nemesisController != null ? nemesisController.GetTileDetails(tile.Coordinates) : string.Empty;

        LogUiDiagnostic(
            "TileDetails",
            $"RefreshTileDetails tile={FormatCoordinates(tile.Coordinates)} pitstop={(pitstopSite != null ? pitstopSite.Kind.ToString() : "none")} obstacle={(visibleObstacle?.Definition?.displayName ?? "none")} hasNemesisDetails={!string.IsNullOrWhiteSpace(nemesisDetails)}.");

        hudPresenter.ShowTileDetails(tile, pitstopSite, visibleObstacle, nemesisDetails);
    }

    private void PrepareForActTransitionModalState()
    {
        isRunOver = true;
        ClearHighlights();
        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;
        travelTimePresenter.Reset();
        pitstopEventController?.HideActiveModal();
        mockQuestMarkerController?.HideActiveModal();
        RefreshTileDetails(currentTile);
    }

    private void InitializeFogOfWar()
    {
        fogOfWarController ??= GetComponent<HexFogOfWarController>() ?? gameObject.AddComponent<HexFogOfWarController>();
        fogOfWarController.Initialize(mapGenerator, BuildAlwaysKnownCoordinates());
    }

    private void InitializeBoonSystem()
    {
        boonRuntime = HexBoonRuntimeState.FromCurrentSelection();
        pendingPitstopBoonHint = string.Empty;
        if (fogOfWarController != null)
        {
            fogOfWarController.SetVisionRadiusBonus(boonRuntime?.GetVisibilityRadiusBonus() ?? 0);
        }
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

    private bool IsWithinImmediateMovementRange(HexagonTile tile)
    {
        return tile != null && currentTile != null && currentTile.Coordinates.DistanceTo(tile.Coordinates) <= 1;
    }

    private int GetMoveCostForSelection(HexagonTile destinationTile, IReadOnlyList<HexTileData> path)
    {
        if (destinationTile == null)
        {
            return 0;
        }

        // Reaching the exit is treated as a special end-of-map move rather than a normal terrain-cost tile.
        if (goalTile != null && destinationTile == goalTile)
        {
            return 1;
        }

        return HexPathMetrics.GetTravelCost(path);
    }

    private static string FormatCoordinates(HexCoordinates coordinates)
    {
        return $"{coordinates.Row},{coordinates.Column}";
    }

    private void LogUiDiagnostic(string scope, string message, bool verbose = false)
    {
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this, createIfMissing: false);
        gameplayUiRootController?.LogDiagnostic(scope, message, this, verbose);
    }

    private void EnsureRuntimeReferences()
    {
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this);
        gameplayUiRootController?.EnsureInitialized();
        hudDocumentController ??= GetComponent<HexHudDocumentController>() ?? gameObject.AddComponent<HexHudDocumentController>();
        hudDocumentController?.EnsureInitialized();

        inputService ??= new HexTileInputService();
        pathHighlighter ??= new HexPathHighlighter();
        travelTimePresenter ??= new HexTravelTimePresenter(hudDocumentController);
        hudPresenter ??= new HexHudPresenter(hudDocumentController);
        mapGenerator ??= FindAnyObjectByType<MapGenerator>();
        caravanMetricsController ??= GetComponentInChildren<CaravanMetricsController>(true);
        pitstopSpawner ??= FindAnyObjectByType<PitstopSpawner>();
        fogOfWarController ??= GetComponent<HexFogOfWarController>() ?? gameObject.AddComponent<HexFogOfWarController>();
        obstacleController ??= FindAnyObjectByType<HexObstacleController>();
        pitstopEventController ??= FindAnyObjectByType<PitstopEventController>();
        nemesisController ??= FindAnyObjectByType<HexNemesisController>();
        runEndModalPresenter ??= GetComponent<HexRunEndModalPresenter>() ?? gameObject.AddComponent<HexRunEndModalPresenter>();
        actTransitionModalPresenter ??= GetComponent<HexActTransitionModalPresenter>() ?? gameObject.AddComponent<HexActTransitionModalPresenter>();
        mockQuestMarkerController ??= GetComponent<HexMockQuestMarkerController>() ?? gameObject.AddComponent<HexMockQuestMarkerController>();
        globalTransitionController ??= HexGlobalUiTransitionController.ResolveShared(this);
        mainMenuPresenter ??= GetComponent<HexMainMenuPresenter>()
            ?? FindAnyObjectByType<HexMainMenuPresenter>()
            ?? gameObject.AddComponent<HexMainMenuPresenter>();
        if (audioSystem == null)
        {
            audioSystem = HexAudioSystem.ResolveShared(this);
        }
    }

    public void ActivateGameplaySession()
    {
        isGameplaySessionActive = true;
        hudDocumentController?.SetGameplayModalState(false);
        gameplayUiRootController?.SetLayerVisible(HexGameplayUiLayerId.Hud, true);
        gameplayUiRootController?.SetLayerVisible(HexGameplayUiLayerId.Context, true);
        gameplayUiRootController?.SetLayerInteractive(HexGameplayUiLayerId.Hud, false);
        gameplayUiRootController?.SetLayerInteractive(HexGameplayUiLayerId.Context, false);
        RefreshTileDetails(currentTile);
        hudPresenter.ShowCaravanIdle(currentTile);
        PlayCurrentActMusic();
    }

    private void RevealGameplayAfterSceneLoad()
    {
        globalTransitionController ??= HexGlobalUiTransitionController.ResolveShared(this);
        globalTransitionController?.EnsureInitialized();
        if (globalTransitionController == null || !globalTransitionController.PlayFadeFromBlack())
        {
            globalTransitionController?.HideBlackoutImmediate();
        }
    }

    private void ReturnToMainMenuWithFade()
    {
        HexGameBootstrap.ReloadActiveSceneToMainMenu(this, resetRunSession: true);
    }

    private void ReturnToMainMenuAfterFade()
    {
        HexGameBootstrap.LoadActiveSceneToMainMenu(resetRunSession: true);
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

    private void InitializePitstopEvents()
    {
        if (pitstopEventController == null || pitstopSpawner == null)
        {
            return;
        }

        pitstopEventController.Initialize(pitstopSpawner);
    }

    private void InitializeNemesisSystem()
    {
        if (nemesisController == null)
        {
            return;
        }

        HexNemesisArchetype sceneArchetype = nemesisController.ActiveArchetype;
        bool sceneNemesisEnabled = nemesisController.IsEnabled;
        HexNemesisArchetype resolvedArchetype = HexActTransitionService.ResolveNemesisArchetypeForCurrentAct(
            sceneArchetype == HexNemesisArchetype.None ? HexNemesisArchetype.Hunter : sceneArchetype);
        bool enableNemesis = HexActTransitionService.ResolveNemesisEnabledForCurrentAct(sceneNemesisEnabled);

        nemesisController.ConfigureArchetype(resolvedArchetype, enableNemesis);
        nemesisController.ConfigureRuntimeModifiers(
            boonRuntime != null
                ? boonRuntime.GetNemesisRuntimeModifiers(resolvedArchetype)
                : HexNemesisRuntimeModifiers.None);

        nemesisController.Initialize(mapGenerator, pitstopSpawner, obstacleController, currentTile.Coordinates);
    }

    private void InitializeMockQuestMarkerSystem()
    {
        if (mockQuestMarkerController == null || mapGenerator == null)
        {
            return;
        }

        mockQuestMarkerController.Initialize(mapGenerator);
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

        if (boonRuntime != null && boonRuntime.TryConsumeObstacleIgnore(out HexBoonChargeChangeResult chargeChange))
        {
            turnResult.ContactPenaltyIgnored = true;
            turnResult.ContactPenaltyIgnoreNote = chargeChange.Message;
            UpdateResourcesText();
            return turnResult;
        }

        caravanResources.Spend(turnResult.ContactResult.AffectedResource, turnResult.ContactResult.AmountDrained);
        UpdateResourcesText();
        return turnResult;
    }

    private PitstopEventResult ProcessPitstopArrival()
    {
        if (pitstopEventController == null || currentTile == null)
        {
            return PitstopEventResult.Empty;
        }

        PitstopEventResult result = pitstopEventController.ProcessArrival(currentTile.Coordinates, caravanResources);
        if (result == null || !result.Triggered || !result.EffectsApplied)
        {
            return result ?? PitstopEventResult.Empty;
        }

        UpdateResourcesText();
        return result;
    }

    private string ProcessPitstopRecharge()
    {
        if (boonRuntime == null
            || currentTile == null
            || pitstopSpawner == null
            || !pitstopSpawner.TryGetPitstop(currentTile.Coordinates, out PitstopSite site)
            || site == null
            || site.IsDestroyed)
        {
            return string.Empty;
        }

        if (!boonRuntime.TryRecharge(HexBoonRechargeTrigger.PitstopArrival, out HexBoonChargeChangeResult chargeChange))
        {
            return string.Empty;
        }

        UpdateResourcesText();
        return chargeChange.Message;
    }

    private HexNemesisTurnResult ProcessNemesisTurn(HexCoordinates previousCoordinates, HexCoordinates currentCoordinates)
    {
        if (nemesisController == null)
        {
            return HexNemesisTurnResult.Empty;
        }

        return nemesisController.ProcessCaravanMove(previousCoordinates, currentCoordinates);
    }

    public bool HasInitializedCaravanResources => resourcesInitialized;

    public CaravanResourceSnapshot GetCurrentResources()
    {
        return caravanResources.ToSnapshot();
    }

    public void OverrideCaravanResources(int food, int morale, int gold)
    {
        caravanResources.Initialize(food, morale, gold);
        resourcesInitialized = true;
        UpdateResourcesText();

        if (currentTile != null)
        {
            RefreshTileDetails(currentTile);
        }

        if (!isRunOver && caravanResources.IsDefeated)
        {
            EndRunAsDefeat(caravanResources.GetDefeatReason());
        }
    }

    private void RetryCurrentScene()
    {
        HexGameBootstrap.ReloadActiveSceneToGameplay(this, resetRunSession: true);
    }

    private void ContinueToNextAct(HexBoonDefinition selectedBoon)
    {
        if (!HexActTransitionService.TryAdvanceToNextAct(caravanResources.ToSnapshot(), selectedBoon))
        {
            return;
        }

        LoadCurrentSceneBehindFade();
    }

    private void LoadCurrentSceneBehindFade()
    {
        HexGameBootstrap.ReloadActiveSceneToGameplay(this, resetRunSession: false);
    }

    private void PlayCurrentActMusic()
    {
        if (audioSystem == null)
        {
            audioSystem = HexAudioSystem.ResolveShared(this);
        }

        if (audioSystem != null)
        {
            audioSystem.PlayMusicForCurrentAct();
        }
    }

    private void PlayGameplaySfx(HexSfxId id)
    {
        if (audioSystem == null)
        {
            audioSystem = HexAudioSystem.ResolveShared(this);
        }

        if (audioSystem != null)
        {
            audioSystem.PlayGameplaySfx(id);
        }
    }

    private HexSfxId ResolveTileSelectionSfx(HexagonTile tile)
    {
        return HasVisibleObstacle(tile) ? HexSfxId.ObstacleSelect : HexSfxId.HexSelect;
    }

    private bool HasVisibleObstacle(HexagonTile tile)
    {
        if (tile == null)
        {
            return false;
        }

        obstacleController ??= FindAnyObjectByType<HexObstacleController>();
        return obstacleController != null
            && obstacleController.TryGetVisibleObstacle(tile.Coordinates, out HexObstacleInstance obstacle)
            && obstacle != null;
    }
}

public sealed class HexMockQuestMarkerController : MonoBehaviour
{
    [Header("Mock Content")]
    [SerializeField] private string fallbackTitle = "Quest Marker";
    [TextArea(3, 6)] [SerializeField] private string placeholderBody =
        "This is a mock quest marker.\n\nReplace this placeholder with a real quest flow later.";

    private readonly Dictionary<HexCoordinates, HexMapObjectPlacement> questMarkersByCoordinate = new();
    private readonly HashSet<HexCoordinates> resolvedQuestMarkers = new();

    private MapGenerator mapGenerator;
    private HexMockQuestMarkerModalPresenter modalPresenter;
    private HexCoordinates? activeQuestMarker;

    public bool IsModalOpen => modalPresenter != null && modalPresenter.IsOpen;

    public void Initialize(MapGenerator generator)
    {
        mapGenerator = generator;
        modalPresenter ??= GetComponent<HexMockQuestMarkerModalPresenter>() ?? gameObject.AddComponent<HexMockQuestMarkerModalPresenter>();
        RebuildQuestMarkers();
    }

    public bool TryProcessArrival(HexCoordinates coordinates)
    {
        if (IsModalOpen
            || resolvedQuestMarkers.Contains(coordinates)
            || !questMarkersByCoordinate.TryGetValue(coordinates, out HexMapObjectPlacement placement))
        {
            return false;
        }

        activeQuestMarker = coordinates;
        modalPresenter.Show(
            ResolveTitle(placement),
            ResolveBody(placement),
            HandleDialogClosed);
        return true;
    }

    public void HideActiveModal()
    {
        activeQuestMarker = null;
        modalPresenter?.Hide();
    }

    private void HandleDialogClosed()
    {
        if (activeQuestMarker.HasValue)
        {
            resolvedQuestMarkers.Add(activeQuestMarker.Value);
        }

        activeQuestMarker = null;
    }

    private void RebuildQuestMarkers()
    {
        questMarkersByCoordinate.Clear();
        if (mapGenerator?.MapObjectPlacements == null)
        {
            return;
        }

        List<HexMapObjectPlacement> questMarkers = mapGenerator.MapObjectPlacements.GetByType(HexMapObjectType.QuestMarker);
        for (int index = 0; index < questMarkers.Count; index++)
        {
            HexMapObjectPlacement placement = questMarkers[index];
            questMarkersByCoordinate[placement.Coordinates] = placement;
        }
    }

    private string ResolveTitle(HexMapObjectPlacement placement)
    {
        return string.IsNullOrWhiteSpace(placement.DisplayName) ? fallbackTitle : placement.DisplayName;
    }

    private string ResolveBody(HexMapObjectPlacement placement)
    {
        string baseBody = string.IsNullOrWhiteSpace(placeholderBody)
            ? "This is a mock quest marker placeholder."
            : placeholderBody.Trim();

        return $"{baseBody}\n\nLocation: {placement.Coordinates}";
    }
}
