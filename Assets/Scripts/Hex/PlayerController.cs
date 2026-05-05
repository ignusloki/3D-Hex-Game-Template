using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class PlayerController : MonoBehaviour
{
    private const string CurrentTileSelectionPrefabPath = "Assets/Prefabs/Selection Hex.prefab";
    private const int VisibilityBoonObstacleCap = 4;

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
    [Header("Debug")]
    [SerializeField] private bool enableMockQuestMarkers;
    [SerializeField] private bool allowDebugPlaceholderVisuals = true;

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
    private HexCaravanGoalVisualController caravanGoalVisualController;
    private HexGlobalUiTransitionController globalTransitionController;
    private HexMainMenuPresenter mainMenuPresenter;
    private HexAudioSystem audioSystem;
    private HexNemesisTurnResult pendingDeferredNemesisResult;
    private HexBoonRuntimeState boonRuntime;
    private readonly CaravanResourceState caravanResources = new();
    private readonly HexRunState runState = new();

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
    private int currentVisibilityRadiusBonus;
    private string pendingPitstopBoonHint;

    public bool IsGameplaySessionActive => isGameplaySessionActive;
    public bool IsRunOver => isRunOver;
    private bool ShouldUseMockQuestMarkers => HexDevelopmentContentGate.CanUseMockQuestMarkers(enableMockQuestMarkers);

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
        runState.SetPhase(HexRunPhase.PreparingGameplay);
        runState.SetPendingModalContext(string.Empty);

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

        InitializeRunState(startingResources);
        caravanResources.Initialize(startingResources.Food, startingResources.Morale, startingResources.Gold);
        resourcesInitialized = true;
        UpdateResourcesText();
        caravanMetricsController?.InitializeRuntimeSnapshot(caravanResources.ToSnapshot());
        isReady = true;
        RefreshTileDetails(currentTile);
        hudPresenter.ShowCaravanIdle(currentTile);
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
        ConfigureCaravanGoalVisualController();
        caravanVisual = caravanGoalVisualController.EnsureCaravanVisual();
    }

    private void EnsureGoalVisual()
    {
        ConfigureCaravanGoalVisualController();
        goalVisual = caravanGoalVisualController.EnsureGoalVisual();
    }

    private void AttachCaravanToTile(HexagonTile tile)
    {
        ConfigureCaravanGoalVisualController();
        caravanGoalVisualController.AttachCaravanToTile(tile);
        caravanVisual = caravanGoalVisualController.CaravanVisual;
    }

    private void AttachGoalToTile(HexagonTile tile)
    {
        ConfigureCaravanGoalVisualController();
        caravanGoalVisualController.AttachGoalToTile(tile);
        goalVisual = caravanGoalVisualController.GoalVisual;
    }

    private void ConfigureCaravanGoalVisualController()
    {
        caravanGoalVisualController ??= GetComponent<HexCaravanGoalVisualController>()
            ?? gameObject.AddComponent<HexCaravanGoalVisualController>();
        caravanGoalVisualController.Configure(
            caravanVisual,
            goalVisual,
            caravanHeight,
            caravanScale,
            goalHeight,
            goalScale,
            caravanColor,
            goalColor,
            allowDebugPlaceholderVisuals);
    }

    private void UpdateResourcesText()
    {
        if (resourcesInitialized)
        {
            runState.SetResources(caravanResources.ToSnapshot());
        }

        string boonLine = boonRuntime != null && boonRuntime.HasActiveBoon
            ? boonRuntime.GetStatusLine()
            : string.Empty;

        hudDocumentController?.SetResources(
            caravanResources.Food,
            caravanResources.Morale,
            caravanResources.Gold,
            boonLine);
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

    private void InitializeFogOfWar()
    {
        fogOfWarController ??= GetComponent<HexFogOfWarController>() ?? gameObject.AddComponent<HexFogOfWarController>();
        fogOfWarController.Initialize(mapGenerator, BuildAlwaysKnownCoordinates());
    }

    private void InitializeBoonSystem()
    {
        boonRuntime = HexBoonRuntimeState.FromCurrentSelection();
        pendingPitstopBoonHint = string.Empty;
        currentVisibilityRadiusBonus = boonRuntime?.GetVisibilityRadiusBonus() ?? 0;
        if (fogOfWarController != null)
        {
            fogOfWarController.SetVisionRadiusBonus(currentVisibilityRadiusBonus);
        }

        ApplyBoonObstacleSpawnCap();
        SyncRunStateActContext();
    }

    private void InitializeRunState(CaravanResourceSnapshot startingResources)
    {
        runState.Initialize(
            HexActTransitionService.GetCurrentActNumber(),
            startingResources,
            currentTile.Coordinates,
            goalTile.Coordinates,
            HexActTransitionService.GetLockedBoonFamily(),
            HexBoonSelectionService.GetSelectedBoonDefinitions(),
            isGameplaySessionActive ? HexRunPhase.AwaitingPlayerInput : HexRunPhase.MainMenu);
    }

    private void SyncRunStateActContext()
    {
        runState.SetCurrentActNumber(HexActTransitionService.GetCurrentActNumber());
        runState.SetLockedBoonFamily(HexActTransitionService.GetLockedBoonFamily());
        runState.SetSelectedBoons(HexBoonSelectionService.GetSelectedBoonDefinitions());
    }

    private void SyncRunStateCoordinates()
    {
        if (currentTile != null)
        {
            runState.SetCaravanCoordinates(currentTile.Coordinates);
        }
        else
        {
            runState.ClearCaravanCoordinates();
        }

        if (goalTile != null)
        {
            runState.SetGoalCoordinates(goalTile.Coordinates);
        }
        else
        {
            runState.ClearGoalCoordinates();
        }
    }

    private void SyncRunStateFromExistingModalState()
    {
        if (!isReady)
        {
            runState.SetPhase(HexRunPhase.PreparingGameplay);
            return;
        }

        if (mainMenuPresenter != null && mainMenuPresenter.IsOpen)
        {
            runState.SetPhase(HexRunPhase.MainMenu);
            runState.SetPendingModalContext("MainMenu");
            return;
        }

        if (!isGameplaySessionActive)
        {
            runState.SetPhase(HexRunPhase.MainMenu);
            runState.SetPendingModalContext(string.Empty);
            return;
        }

        if (runEndModalPresenter != null && runEndModalPresenter.IsOpen)
        {
            if (runState.Outcome == HexRunOutcome.Victory)
            {
                runState.SetPhase(HexRunPhase.Victory);
                runState.SetPendingModalContext("Victory");
            }
            else if (runState.Outcome == HexRunOutcome.Defeat)
            {
                runState.SetPhase(HexRunPhase.Defeat);
                runState.SetPendingModalContext("Defeat");
            }
            else
            {
                runState.SetPendingModalContext("RunEnd");
            }

            return;
        }

        if (actTransitionModalPresenter != null && actTransitionModalPresenter.IsOpen)
        {
            runState.SetPhase(HexRunPhase.ActTransition);
            runState.SetPendingModalContext("ActTransition");
            return;
        }

        if (pitstopEventController != null && pitstopEventController.IsChoiceModalOpen)
        {
            runState.SetPhase(HexRunPhase.PitstopChoice);
            runState.SetPendingModalContext("Pitstop");
            return;
        }

        if (IsMockQuestMarkerModalOpen())
        {
            if (runState.Phase == HexRunPhase.ResolvingMove)
            {
                runState.SetPhase(HexRunPhase.AwaitingPlayerInput);
            }

            runState.SetPendingModalContext("QuestMarker");
            return;
        }

        if (isRunOver || runState.HasFinished)
        {
            return;
        }

        runState.SetPhase(HexRunPhase.AwaitingPlayerInput);
        runState.SetPendingModalContext(string.Empty);
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
        mockQuestMarkerController = ShouldUseMockQuestMarkers
            ? GetComponent<HexMockQuestMarkerController>() ?? gameObject.AddComponent<HexMockQuestMarkerController>()
            : null;
        caravanGoalVisualController ??= GetComponent<HexCaravanGoalVisualController>() ?? gameObject.AddComponent<HexCaravanGoalVisualController>();
        globalTransitionController ??= HexGlobalUiTransitionController.ResolveShared(this);
        mainMenuPresenter ??= GetComponent<HexMainMenuPresenter>()
            ?? FindAnyObjectByType<HexMainMenuPresenter>()
            ?? gameObject.AddComponent<HexMainMenuPresenter>();
        if (audioSystem == null)
        {
            audioSystem = HexAudioSystem.ResolveShared(this);
        }
    }

    private void InitializeObstacleSystem(HexFogUpdateResult initialFogUpdate)
    {
        if (obstacleController == null)
        {
            return;
        }

        obstacleController.Initialize(mapGenerator, pitstopSpawner, fogOfWarController);
        ApplyBoonObstacleSpawnCap();
        obstacleController.SyncVisibility(initialFogUpdate);
    }

    private void ApplyBoonObstacleSpawnCap()
    {
        obstacleController?.SetRuntimeNormalMaxActiveObstacles(
            currentVisibilityRadiusBonus > 0 ? VisibilityBoonObstacleCap : -1);
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
        if (!ShouldUseMockQuestMarkers || mockQuestMarkerController == null || mapGenerator == null)
        {
            return;
        }

        mockQuestMarkerController.Initialize(mapGenerator);
    }

    private bool IsMockQuestMarkerModalOpen()
    {
        return ShouldUseMockQuestMarkers
            && mockQuestMarkerController != null
            && mockQuestMarkerController.IsModalOpen;
    }

    private void HideMockQuestMarkerModal()
    {
        if (ShouldUseMockQuestMarkers)
        {
            mockQuestMarkerController?.HideActiveModal();
        }
    }

    public bool HasInitializedCaravanResources => resourcesInitialized;

    public CaravanResourceSnapshot GetCurrentResources()
    {
        return caravanResources.ToSnapshot();
    }

    public HexRunStateSnapshot GetRunStateSnapshot()
    {
        return runState.ToSnapshot();
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

}
