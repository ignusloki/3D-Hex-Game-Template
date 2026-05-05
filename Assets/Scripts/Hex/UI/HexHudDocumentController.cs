using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class HexHudDocumentController : MonoBehaviour, IHexHudView, IHexTravelTimeView
{
    private enum InspectorMode
    {
        Empty,
        Biome,
        Pitstop,
        Generic
    }

    private const string LayoutResourcePath = "UI/Hud/HexMainHud";
    private const string StyleSheetResourcePath = "UI/Hud/HexMainHudStyles";
    private const string ContextLayoutResourcePath = "UI/Context/HexGameplayContext";
    private const string ContextStyleSheetResourcePath = "UI/Context/HexGameplayContextStyles";
    private const string ContextMountName = "hud-context-mount";
    private const string FoodIconResourcePath = "UI/Hud/Icons/resource-food";
    private const string MoraleIconResourcePath = "UI/Hud/Icons/resource-morale";
    private const string GoldIconResourcePath = "UI/Hud/Icons/resource-gold";
    private const string DiagnosticScope = "HUD";

    [Header("Resources")]
    [SerializeField] private string runContextLabel = "Act 1";
    [SerializeField] private VisualTreeAsset layoutAsset;
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private VisualTreeAsset contextLayoutAsset;
    [SerializeField] private StyleSheet contextStyleSheet;
    [SerializeField] private Texture2D foodIcon;
    [SerializeField] private Texture2D moraleIcon;
    [SerializeField] private Texture2D goldIcon;

    private HexGameplayUiRootController gameplayUiRootController;
    private VisualElement hudLayer;
    private VisualElement contextMount;
    private TemplateContainer hudTree;
    private TemplateContainer contextTree;
    private VisualElement topBar;
    private VisualElement centerStatusPocket;
    private Label runContextLabelElement;
    private Label foodValueLabel;
    private Label moraleValueLabel;
    private Label goldValueLabel;
    private Label boonStatusLabel;
    private Label selectionStatusLabel;
    private Label travelTimeLabel;
    private Label hintLabel;
    private VisualElement unifiedInspectorPanel;
    private VisualElement unifiedInspectorEmptyState;
    private VisualElement unifiedInspectorBiomeState;
    private VisualElement unifiedInspectorPitstopState;
    private VisualElement unifiedInspectorGenericState;
    private Label unifiedInspectorBiomeTitle;
    private Label unifiedInspectorBiomeHexValue;
    private Label unifiedInspectorBiomeValue;
    private Label unifiedInspectorBiomeTravelCostValue;
    private Label unifiedInspectorBiomeDescription;
    private Label unifiedInspectorPitstopTitle;
    private Label unifiedInspectorPitstopType;
    private Label unifiedInspectorPitstopHexValue;
    private Label unifiedInspectorPitstopTerrainValue;
    private Label unifiedInspectorPitstopTravelCostValue;
    private Label unifiedInspectorPitstopWorkingValue;
    private Label unifiedInspectorPitstopDestroyedValue;
    private Label unifiedInspectorPitstopRefuelValue;
    private Label unifiedInspectorPitstopRepeatableValue;
    private Label unifiedInspectorPitstopVisitedValue;
    private Label unifiedInspectorPitstopDescription;
    private Label unifiedInspectorGenericTitle;
    private Label unifiedInspectorGenericSubtitle;
    private Label unifiedInspectorGenericHexValue;
    private Label unifiedInspectorGenericTerrainValue;
    private Label unifiedInspectorGenericTravelCostValue;
    private Label unifiedInspectorGenericDescription;
    private VisualElement foodIconElement;
    private VisualElement moraleIconElement;
    private VisualElement goldIconElement;
    private bool isGameplayModalActive;
    private bool isInitialized;
    private int lastResolvedActNumber = -1;
    private InspectorMode inspectorMode = InspectorMode.Empty;

    public bool IsReady => isInitialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        layoutAsset ??= Resources.Load<VisualTreeAsset>(LayoutResourcePath);
        styleSheet ??= Resources.Load<StyleSheet>(StyleSheetResourcePath);
        contextLayoutAsset ??= Resources.Load<VisualTreeAsset>(ContextLayoutResourcePath);
        contextStyleSheet ??= Resources.Load<StyleSheet>(ContextStyleSheetResourcePath);
        foodIcon ??= Resources.Load<Texture2D>(FoodIconResourcePath);
        moraleIcon ??= Resources.Load<Texture2D>(MoraleIconResourcePath);
        goldIcon ??= Resources.Load<Texture2D>(GoldIconResourcePath);

        if (layoutAsset == null || styleSheet == null || contextLayoutAsset == null || contextStyleSheet == null)
        {
            Debug.LogError("HexHudDocumentController could not load the UI Toolkit HUD/context assets from Resources.", this);
            return;
        }

        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this);
        if (gameplayUiRootController == null)
        {
            Debug.LogError("HexHudDocumentController could not resolve the shared gameplay UI root.", this);
            return;
        }

        gameplayUiRootController.EnsureInitialized();
        hudLayer = gameplayUiRootController.RequestLayer(HexGameplayUiLayerId.Hud, nameof(HexHudDocumentController), true, this);
        if (hudLayer == null)
        {
            Debug.LogError("HexHudDocumentController could not bind to the shared HUD layer.", this);
            return;
        }

        contextMount = gameplayUiRootController.RequestLayerMount(
            HexGameplayUiLayerId.Context,
            ContextMountName,
            nameof(HexHudDocumentController),
            true,
            this);
        if (contextMount == null)
        {
            Debug.LogError("HexHudDocumentController could not bind to the shared context layer.", this);
            return;
        }

        hudTree = layoutAsset.CloneTree();
        hudTree.styleSheets.Add(styleSheet);
        StretchToFill(hudTree);
        hudTree.pickingMode = PickingMode.Ignore;
        hudLayer.Add(hudTree);

        contextTree = contextLayoutAsset.CloneTree();
        contextTree.styleSheets.Add(contextStyleSheet);
        StretchToFill(contextTree);
        contextTree.pickingMode = PickingMode.Ignore;
        contextMount.Add(contextTree);

        VisualElement root = hudTree.Q<VisualElement>("hud-root");
        if (root == null)
        {
            Debug.LogError("HexHudDocumentController could not find the HUD root inside the cloned UXML.", this);
            return;
        }

        root.pickingMode = PickingMode.Ignore;
        VisualElement contextRoot = contextTree.Q<VisualElement>("gameplay-context-root");
        if (contextRoot == null)
        {
            Debug.LogError("HexHudDocumentController could not find the gameplay context root inside the cloned UXML.", this);
            return;
        }

        contextRoot.pickingMode = PickingMode.Ignore;

        runContextLabelElement = hudTree.Q<Label>("run-context-label");
        topBar = hudTree.Q<VisualElement>("top-bar");
        centerStatusPocket = hudTree.Q<VisualElement>("center-status-pocket");
        foodValueLabel = hudTree.Q<Label>("resource-food-value");
        moraleValueLabel = hudTree.Q<Label>("resource-morale-value");
        goldValueLabel = hudTree.Q<Label>("resource-gold-value");
        boonStatusLabel = hudTree.Q<Label>("boon-status");
        selectionStatusLabel = hudTree.Q<Label>("selection-status");
        travelTimeLabel = hudTree.Q<Label>("travel-time");
        hintLabel = hudTree.Q<Label>("hint-text");
        unifiedInspectorPanel = contextTree.Q<VisualElement>("UnifiedInspectorPanel");
        unifiedInspectorEmptyState = contextTree.Q<VisualElement>("UnifiedInspectorEmptyState");
        unifiedInspectorBiomeState = contextTree.Q<VisualElement>("UnifiedInspectorBiomeState");
        unifiedInspectorPitstopState = contextTree.Q<VisualElement>("UnifiedInspectorPitstopState");
        unifiedInspectorGenericState = contextTree.Q<VisualElement>("UnifiedInspectorGenericState");
        unifiedInspectorBiomeTitle = contextTree.Q<Label>("UnifiedInspectorBiomeTitle");
        unifiedInspectorBiomeHexValue = contextTree.Q<Label>("UnifiedInspectorBiomeHexValue");
        unifiedInspectorBiomeValue = contextTree.Q<Label>("UnifiedInspectorBiomeValue");
        unifiedInspectorBiomeTravelCostValue = contextTree.Q<Label>("UnifiedInspectorBiomeTravelCostValue");
        unifiedInspectorBiomeDescription = contextTree.Q<Label>("UnifiedInspectorBiomeDescription");
        unifiedInspectorPitstopTitle = contextTree.Q<Label>("UnifiedInspectorPitstopTitle");
        unifiedInspectorPitstopType = contextTree.Q<Label>("UnifiedInspectorPitstopType");
        unifiedInspectorPitstopHexValue = contextTree.Q<Label>("UnifiedInspectorPitstopHexValue");
        unifiedInspectorPitstopTerrainValue = contextTree.Q<Label>("UnifiedInspectorPitstopTerrainValue");
        unifiedInspectorPitstopTravelCostValue = contextTree.Q<Label>("UnifiedInspectorPitstopTravelCostValue");
        unifiedInspectorPitstopWorkingValue = contextTree.Q<Label>("UnifiedInspectorPitstopWorkingValue");
        unifiedInspectorPitstopDestroyedValue = contextTree.Q<Label>("UnifiedInspectorPitstopDestroyedValue");
        unifiedInspectorPitstopRefuelValue = contextTree.Q<Label>("UnifiedInspectorPitstopRefuelValue");
        unifiedInspectorPitstopRepeatableValue = contextTree.Q<Label>("UnifiedInspectorPitstopRepeatableValue");
        unifiedInspectorPitstopVisitedValue = contextTree.Q<Label>("UnifiedInspectorPitstopVisitedValue");
        unifiedInspectorPitstopDescription = contextTree.Q<Label>("UnifiedInspectorPitstopDescription");
        unifiedInspectorGenericTitle = contextTree.Q<Label>("UnifiedInspectorGenericTitle");
        unifiedInspectorGenericSubtitle = contextTree.Q<Label>("UnifiedInspectorGenericSubtitle");
        unifiedInspectorGenericHexValue = contextTree.Q<Label>("UnifiedInspectorGenericHexValue");
        unifiedInspectorGenericTerrainValue = contextTree.Q<Label>("UnifiedInspectorGenericTerrainValue");
        unifiedInspectorGenericTravelCostValue = contextTree.Q<Label>("UnifiedInspectorGenericTravelCostValue");
        unifiedInspectorGenericDescription = contextTree.Q<Label>("UnifiedInspectorGenericDescription");
        foodIconElement = hudTree.Q<VisualElement>("resource-food-icon");
        moraleIconElement = hudTree.Q<VisualElement>("resource-morale-icon");
        goldIconElement = hudTree.Q<VisualElement>("resource-gold-icon");

        ApplyIcon(foodIconElement, foodIcon);
        ApplyIcon(moraleIconElement, moraleIcon);
        ApplyIcon(goldIconElement, goldIcon);
        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Hud, true);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Hud, false);
        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Context, true);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Context, false);
        isInitialized = true;
        LogDiagnostic(
            $"Bound HUD into shared root. topBarFound={topBar != null} contextMounted={contextMount != null} unifiedInspectorFound={unifiedInspectorPanel != null}.");
        RefreshRunContext();
        SetResources(0, 0, 0);
        SetStatusText("Select a departure tile.");
        SetTravelTimeText("Travel Time: --");
        SetHintText(string.Empty);
        ShowInspectorEmptyState();
    }

    public void RefreshRunContext()
    {
        int currentActNumber = HexActTransitionService.GetCurrentActNumber();
        string nextLabel = $"Act {currentActNumber}";
        if (currentActNumber == lastResolvedActNumber && string.Equals(runContextLabel, nextLabel, System.StringComparison.Ordinal))
        {
            return;
        }

        lastResolvedActNumber = currentActNumber;
        runContextLabel = nextLabel;
        if (runContextLabelElement != null)
        {
            runContextLabelElement.text = runContextLabel;
        }

        LogDiagnostic($"RefreshRunContext -> '{runContextLabel}'.");
    }

    public void SetResources(int food, int morale, int gold, string boonLine = null)
    {
        if (!isInitialized)
        {
            return;
        }

        SetLabelText(foodValueLabel, Mathf.Max(food, 0).ToString());
        SetLabelText(moraleValueLabel, Mathf.Max(morale, 0).ToString());
        SetLabelText(goldValueLabel, Mathf.Max(gold, 0).ToString());

        if (boonStatusLabel == null)
        {
            return;
        }

        boonStatusLabel.text = string.IsNullOrWhiteSpace(boonLine) ? string.Empty : boonLine.Trim();
        RefreshBoonStatusVisibility();
        LogDiagnostic($"SetResources food={food} morale={morale} gold={gold} boonVisible={!string.IsNullOrWhiteSpace(boonLine)}.", true);
    }

    public void SetStatusText(string value)
    {
        SetLabelText(selectionStatusLabel, value);
        LogDiagnostic($"SetStatusText value='{value}'.", true);
    }

    public void SetHintText(string value)
    {
        SetLabelText(hintLabel, value);
        if (hintLabel != null)
        {
            hintLabel.style.display = string.IsNullOrWhiteSpace(value) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        LogDiagnostic($"SetHintText visible={!string.IsNullOrWhiteSpace(value)} value='{value}'.", true);
    }

    public void ShowInspectorEmptyState()
    {
        inspectorMode = InspectorMode.Empty;
        RefreshUnifiedInspectorState();
        LogDiagnostic("ShowInspectorEmptyState.");
    }

    public void ShowBiomeInspector(HexInspectorBiomeDisplayData data)
    {
        inspectorMode = InspectorMode.Biome;
        SetLabelText(unifiedInspectorBiomeTitle, data.Biome);
        SetLabelText(unifiedInspectorBiomeHexValue, data.Hex);
        SetLabelText(unifiedInspectorBiomeValue, data.Biome);
        SetLabelText(unifiedInspectorBiomeTravelCostValue, data.TravelCost);
        SetLabelText(unifiedInspectorBiomeDescription, data.Description);
        RefreshUnifiedInspectorState();
        LogDiagnostic($"ShowBiomeInspector biome='{data.Biome}' hex='{data.Hex}'.", true);
    }

    public void ShowPitstopInspector(HexInspectorPitstopDisplayData data)
    {
        inspectorMode = InspectorMode.Pitstop;
        SetLabelText(unifiedInspectorPitstopTitle, data.Title);
        SetLabelText(unifiedInspectorPitstopType, data.Type);
        SetLabelText(unifiedInspectorPitstopHexValue, data.Hex);
        SetLabelText(unifiedInspectorPitstopTerrainValue, data.Terrain);
        SetLabelText(unifiedInspectorPitstopTravelCostValue, data.TravelCost);
        SetStatusValue(unifiedInspectorPitstopWorkingValue, data.Working);
        SetStatusValue(unifiedInspectorPitstopDestroyedValue, data.Destroyed);
        SetStatusValue(unifiedInspectorPitstopRefuelValue, data.Refuel);
        SetStatusValue(unifiedInspectorPitstopRepeatableValue, data.Repeatable);
        SetStatusValue(unifiedInspectorPitstopVisitedValue, data.Visited);
        SetLabelText(unifiedInspectorPitstopDescription, data.Description);
        RefreshUnifiedInspectorState();
        LogDiagnostic($"ShowPitstopInspector title='{data.Title}' hex='{data.Hex}'.", true);
    }

    public void ShowGenericInspector(HexInspectorGenericDisplayData data)
    {
        inspectorMode = InspectorMode.Generic;
        SetLabelText(unifiedInspectorGenericTitle, data.Title);
        SetLabelText(unifiedInspectorGenericSubtitle, data.Subtitle);
        SetLabelText(unifiedInspectorGenericHexValue, data.Hex);
        SetLabelText(unifiedInspectorGenericTerrainValue, data.Terrain);
        SetLabelText(unifiedInspectorGenericTravelCostValue, data.TravelCost);
        SetLabelText(unifiedInspectorGenericDescription, data.Description);
        RefreshUnifiedInspectorState();
        LogDiagnostic($"ShowGenericInspector title='{data.Title}' hex='{data.Hex}'.", true);
    }

    public void SetTravelTimeText(string value)
    {
        SetLabelText(travelTimeLabel, value);
        if (travelTimeLabel != null)
        {
            travelTimeLabel.style.display = ShouldShowTravelTime(value) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        LogDiagnostic($"SetTravelTimeText visible={ShouldShowTravelTime(value)} value='{value}'.", true);
    }

    public void SetGameplayModalState(bool active)
    {
        isGameplayModalActive = active;

        if (topBar != null)
        {
            topBar.EnableInClassList("modal-muted", active);
        }

        if (centerStatusPocket != null)
        {
            centerStatusPocket.style.display = active ? DisplayStyle.None : DisplayStyle.Flex;
        }

        RefreshBoonStatusVisibility();
        RefreshUnifiedInspectorState();
        LogDiagnostic($"SetGameplayModalState active={active}.");
    }

    private static void ApplyIcon(VisualElement target, Texture2D texture)
    {
        if (target == null)
        {
            return;
        }

        Label glyphLabel = target.Q<Label>();
        if (texture == null)
        {
            if (glyphLabel != null)
            {
                glyphLabel.style.display = DisplayStyle.Flex;
            }

            return;
        }

        target.style.backgroundImage = new StyleBackground(texture);
        if (glyphLabel != null)
        {
            glyphLabel.style.display = DisplayStyle.None;
        }
    }

    private static void StretchToFill(VisualElement element)
    {
        if (element == null)
        {
            return;
        }

        element.style.position = Position.Absolute;
        element.style.left = 0f;
        element.style.top = 0f;
        element.style.right = 0f;
        element.style.bottom = 0f;
    }

    private static void SetLabelText(Label target, string value)
    {
        if (target != null)
        {
            target.text = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }
    }

    private void RefreshBoonStatusVisibility()
    {
        if (boonStatusLabel == null)
        {
            return;
        }

        boonStatusLabel.EnableInClassList("modal-muted", isGameplayModalActive);
        bool hasBoonStatus = !string.IsNullOrWhiteSpace(boonStatusLabel.text);
        boonStatusLabel.style.display = !isGameplayModalActive && hasBoonStatus ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void RefreshUnifiedInspectorState()
    {
        if (unifiedInspectorPanel == null)
        {
            return;
        }

        bool shouldDisplayPanel = !isGameplayModalActive;
        bool showEmptyState = inspectorMode == InspectorMode.Empty;
        bool showBiomeState = inspectorMode == InspectorMode.Biome;
        bool showPitstopState = inspectorMode == InspectorMode.Pitstop;
        bool showGenericState = inspectorMode == InspectorMode.Generic;

        unifiedInspectorPanel.style.display = shouldDisplayPanel ? DisplayStyle.Flex : DisplayStyle.None;

        if (unifiedInspectorEmptyState != null)
        {
            unifiedInspectorEmptyState.style.display = showEmptyState ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (unifiedInspectorBiomeState != null)
        {
            unifiedInspectorBiomeState.style.display = showBiomeState ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (unifiedInspectorPitstopState != null)
        {
            unifiedInspectorPitstopState.style.display = showPitstopState ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (unifiedInspectorGenericState != null)
        {
            unifiedInspectorGenericState.style.display = showGenericState ? DisplayStyle.Flex : DisplayStyle.None;
        }

        LogDiagnostic(
            $"RefreshUnifiedInspectorState panel={shouldDisplayPanel} mode={inspectorMode}.",
            true);
    }

    private static void SetStatusValue(Label target, string value)
    {
        SetLabelText(target, value);
        target?.EnableInClassList("inspector-fact-value--positive", IsPositiveValue(value));
    }

    private static bool IsPositiveValue(string value)
    {
        return string.Equals(value?.Trim(), "Yes", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldShowTravelTime(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && !value.Contains("--");
    }

    private void LogDiagnostic(string message, bool verbose = false)
    {
        gameplayUiRootController?.LogDiagnostic(DiagnosticScope, message, this, verbose);
    }
}
