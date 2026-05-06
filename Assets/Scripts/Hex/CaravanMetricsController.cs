using UnityEngine;

public sealed class CaravanMetricsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [Header("Resources")]
    [Min(0)] [SerializeField] private int food = 20;
    [Min(0)] [SerializeField] private int morale = 3;
    [Min(0)] [SerializeField] private int gold = 3;
    [Min(0.05f)] [SerializeField] private float applyDelaySeconds = 0.3f;
    [Header("Runtime Selection")]
    [TextArea(3, 8)] [SerializeField] private string selectedBoonsSummary = "Boons: None";
    [Header("Debug Preview")]
    [SerializeField] private HexNemesisArchetype debugAct2ToAct3Family = HexNemesisArchetype.Hunter;

    private bool hasPresentedValues;
    private bool hasPendingInspectorApply;
    private int lastPresentedFood;
    private int lastPresentedMorale;
    private int lastPresentedGold;
    private float lastInspectorEditTime;
    private string lastPresentedSelectedBoonsSummary = string.Empty;
    private HexHudDocumentController hudDocumentController;
    private HexHudPresenter hudPresenter;
    private HexRunEndModalPresenter runEndModalPresenter;
    private HexActTransitionModalPresenter actTransitionModalPresenter;
    private HexMockQuestMarkerController mockQuestMarkerController;
    private PitstopEventController pitstopEventController;
    private HexGlobalUiTransitionController globalTransitionController;
    private CaravanResourceSnapshot debugActTransitionResources;

    public CaravanResourceSnapshot GetConfiguredSnapshot()
    {
        return new CaravanResourceSnapshot(
            Mathf.Max(0, food),
            Mathf.Max(0, morale),
            Mathf.Max(0, gold));
    }

    public void InitializeRuntimeSnapshot(CaravanResourceSnapshot snapshot)
    {
        ApplySnapshot(snapshot);
        RefreshSelectedBoonsSummary();
    }

    private void Awake()
    {
        ResolvePlayerController();
    }

    private void OnEnable()
    {
        ResolvePlayerController();
    }

    private void OnValidate()
    {
        ResolvePlayerController();
        food = Mathf.Max(0, food);
        morale = Mathf.Max(0, morale);
        gold = Mathf.Max(0, gold);
        applyDelaySeconds = Mathf.Max(0.05f, applyDelaySeconds);
        RefreshSelectedBoonsSummary();

        if (Application.isPlaying && playerController != null && playerController.HasInitializedCaravanResources)
        {
            hasPendingInspectorApply = true;
            lastInspectorEditTime = Time.realtimeSinceStartup;
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            RefreshSelectedBoonsSummary();
            return;
        }

        ResolvePlayerController();
        if (playerController == null || !playerController.HasInitializedCaravanResources)
        {
            RefreshSelectedBoonsSummary();
            return;
        }

        RefreshSelectedBoonsSummary();

        CaravanResourceSnapshot currentSnapshot = playerController.GetCurrentResources();
        if (!hasPresentedValues)
        {
            ApplySnapshot(currentSnapshot);
            return;
        }

        if (hasPendingInspectorApply)
        {
            if (Time.realtimeSinceStartup - lastInspectorEditTime >= applyDelaySeconds)
            {
                hasPendingInspectorApply = false;
                playerController.OverrideCaravanResources(food, morale, gold);
                ApplySnapshot(playerController.GetCurrentResources());
            }

            return;
        }

        if (!SnapshotsMatch(currentSnapshot))
        {
            ApplySnapshot(currentSnapshot);
        }
    }

    private void ResolvePlayerController()
    {
        playerController ??= GetComponentInParent<PlayerController>();
    }

    [ContextMenu("Debug/Open Act 2 -> Act 3 Boon Screen")]
    public void DebugOpenAct2ToAct3BoonPreview()
    {
        DebugOpenAct2ToAct3BoonPreview(debugAct2ToAct3Family);
    }

    [ContextMenu("Debug/Open Victory Modal")]
    public void DebugOpenVictoryModal()
    {
        if (!EnsurePlayMode("[RunEndPreview]", "opening the victory modal preview"))
        {
            return;
        }

        ResolveDebugReferences();
        CaravanResourceSnapshot previewResources = ResolveDebugPreviewResources();
        hudPresenter?.ShowVictory(null, previewResources);
        runEndModalPresenter?.ShowVictory(RetryCurrentScene);
    }

    [ContextMenu("Debug/Open Defeat Modal")]
    public void DebugOpenDefeatModal()
    {
        if (!EnsurePlayMode("[RunEndPreview]", "opening the defeat modal preview"))
        {
            return;
        }

        ResolveDebugReferences();
        string previewReason = "Debug defeat preview";
        hudPresenter?.ShowDefeat(null, previewReason);
        runEndModalPresenter?.ShowDefeat(RetryCurrentScene);
    }

    [ContextMenu("Debug/Open Mock Quest Modal")]
    public void DebugOpenMockQuestModal()
    {
        if (!HexDevelopmentContentGate.AllowsDevelopmentOnlyContent)
        {
            Debug.LogWarning("[QuestModalPreview] Mock quest modal previews are disabled outside development builds.", this);
            return;
        }

        if (!EnsurePlayMode("[QuestModalPreview]", "opening the mock quest modal preview"))
        {
            return;
        }

        ResolveDebugReferences();
        HexMockQuestMarkerModalPresenter previewPresenter =
            GetComponent<HexMockQuestMarkerModalPresenter>() ?? gameObject.AddComponent<HexMockQuestMarkerModalPresenter>();
        previewPresenter.Show(
            "Quest Marker",
            "This is a mock quest marker placeholder.\n\nLocation: Debug preview",
            null);
    }

    public void DebugOpenAct2ToAct3BoonPreview(HexNemesisArchetype lockedFamily)
    {
        if (!EnsurePlayMode("[ActTransitionPreview]", "opening the Act 2 -> Act 3 boon preview"))
        {
            return;
        }

        ResolveDebugReferences();
        if (actTransitionModalPresenter == null)
        {
            Debug.LogWarning("[ActTransitionPreview] Missing HexActTransitionModalPresenter. Unable to open the boon preview.", this);
            return;
        }

        HexNemesisArchetype resolvedFamily = lockedFamily == HexNemesisArchetype.None
            ? HexNemesisArchetype.Hunter
            : lockedFamily;
        CaravanResourceSnapshot previewResources = ResolveDebugPreviewResources();
        debugActTransitionResources = previewResources;

        HexActTransitionService.DebugConfigureRunSession(2, previewResources, resolvedFamily);
        HexActTransitionDisplayData displayData = HexActTransitionService.BuildTransitionDisplayData(previewResources);
        hudDocumentController?.RefreshRunContext();
        PrepareDebugModalState();
        hudPresenter?.ShowHint($"Debug preview: Act 2 complete. Inspecting {resolvedFamily} family boon options.");

        if (displayData.RequiresBoonSelection)
        {
            actTransitionModalPresenter.ShowTransitionSelection(displayData, ContinueDebugActTransition);
        }
        else
        {
            actTransitionModalPresenter.ShowTransition(displayData, ContinueDebugActTransition);
            Debug.LogWarning(
                $"[ActTransitionPreview] No boon options were available for the {resolvedFamily} family. Showing the intermission screen instead.",
                this);
        }

        Debug.Log(
            $"[ActTransitionPreview] Opened Act 2 -> Act 3 preview. family={resolvedFamily} options={displayData.BoonOptions.Length} food={previewResources.Food} morale={previewResources.Morale} gold={previewResources.Gold}.",
            this);
    }

    private void ResolveDebugReferences()
    {
        ResolvePlayerController();
        GameObject playerObject = playerController != null ? playerController.gameObject : null;
        hudDocumentController ??= playerObject != null
            ? playerObject.GetComponent<HexHudDocumentController>()
            : FindAnyObjectByType<HexHudDocumentController>();
        hudDocumentController?.EnsureInitialized();
        hudPresenter ??= hudDocumentController != null ? new HexHudPresenter(hudDocumentController) : null;
        runEndModalPresenter ??= playerObject != null
            ? playerObject.GetComponent<HexRunEndModalPresenter>() ?? playerObject.AddComponent<HexRunEndModalPresenter>()
            : FindAnyObjectByType<HexRunEndModalPresenter>();
        actTransitionModalPresenter ??= playerObject != null
            ? playerObject.GetComponent<HexActTransitionModalPresenter>() ?? playerObject.AddComponent<HexActTransitionModalPresenter>()
            : FindAnyObjectByType<HexActTransitionModalPresenter>();
        if (HexDevelopmentContentGate.AllowsDevelopmentOnlyContent)
        {
            mockQuestMarkerController ??= playerObject != null
                ? playerObject.GetComponent<HexMockQuestMarkerController>()
                : FindAnyObjectByType<HexMockQuestMarkerController>();
        }
        pitstopEventController ??= FindAnyObjectByType<PitstopEventController>();
        globalTransitionController ??= HexGlobalUiTransitionController.ResolveShared(this);
    }

    private CaravanResourceSnapshot ResolveDebugPreviewResources()
    {
        if (playerController != null && playerController.HasInitializedCaravanResources)
        {
            return playerController.GetCurrentResources();
        }

        return HexActTransitionService.GetStartingResources(GetConfiguredSnapshot());
    }

    private void PrepareDebugModalState()
    {
        pitstopEventController?.HideActiveModal();
        if (HexDevelopmentContentGate.AllowsDevelopmentOnlyContent)
        {
            mockQuestMarkerController?.HideActiveModal();
        }
        runEndModalPresenter?.Hide();
    }

    private static void RetryCurrentScene()
    {
        HexGameBootstrap.ReloadActiveSceneToGameplay(null, resetRunSession: true);
    }

    private static bool EnsurePlayMode(string scope, string action)
    {
        if (Application.isPlaying)
        {
            return true;
        }

        Debug.LogWarning($"{scope} Enter Play Mode before {action}.");
        return false;
    }

    private void ContinueDebugActTransition(HexBoonDefinition selectedBoon)
    {
        if (!HexActTransitionService.TryAdvanceToNextAct(debugActTransitionResources, selectedBoon))
        {
            return;
        }

        LoadCurrentSceneBehindFade();
    }

    private void LoadCurrentSceneBehindFade()
    {
        HexGameBootstrap.ReloadActiveSceneToGameplay(this, resetRunSession: false);
    }

    private bool SnapshotsMatch(CaravanResourceSnapshot snapshot)
    {
        return snapshot.Food == lastPresentedFood
            && snapshot.Morale == lastPresentedMorale
            && snapshot.Gold == lastPresentedGold;
    }

    private void ApplySnapshot(CaravanResourceSnapshot snapshot)
    {
        food = Mathf.Max(0, snapshot.Food);
        morale = Mathf.Max(0, snapshot.Morale);
        gold = Mathf.Max(0, snapshot.Gold);
        lastPresentedFood = food;
        lastPresentedMorale = morale;
        lastPresentedGold = gold;
        hasPresentedValues = true;
    }

    private void RefreshSelectedBoonsSummary()
    {
        string nextSummary = BuildSelectedBoonsSummary();
        if (string.Equals(nextSummary, lastPresentedSelectedBoonsSummary, System.StringComparison.Ordinal))
        {
            return;
        }

        selectedBoonsSummary = nextSummary;
        lastPresentedSelectedBoonsSummary = nextSummary;
    }

    private static string BuildSelectedBoonsSummary()
    {
        System.Collections.Generic.IReadOnlyList<HexBoonDefinition> selectedBoons = HexBoonSelectionService.GetSelectedBoonDefinitions();
        int currentAct = HexActTransitionService.GetCurrentActNumber();
        HexNemesisArchetype lockedFamily = HexActTransitionService.GetLockedBoonFamily();

        System.Text.StringBuilder summary = new();
        summary.Append($"Act: {currentAct}");
        if (lockedFamily != HexNemesisArchetype.None)
        {
            summary.Append($"\nLocked Family: {lockedFamily}");
        }

        summary.Append("\nBoons:");
        if (selectedBoons == null || selectedBoons.Count == 0)
        {
            summary.Append("\n- None");
            return summary.ToString();
        }

        for (int index = 0; index < selectedBoons.Count; index++)
        {
            HexBoonDefinition boon = selectedBoons[index];
            if (boon == null)
            {
                continue;
            }

            boon.Validate();
            summary.Append($"\n- {boon.GetResolvedDisplayName()} [{boon.archetypeFamily}]");
        }

        return summary.ToString();
    }
}
