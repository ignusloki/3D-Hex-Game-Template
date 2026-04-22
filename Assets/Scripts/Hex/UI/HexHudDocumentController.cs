using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class HexHudDocumentController : MonoBehaviour, IHexHudView, IHexTravelTimeView
{
    private const string LayoutResourcePath = "UI/Hud/HexMainHud";
    private const string StyleSheetResourcePath = "UI/Hud/HexMainHudStyles";
    private const string DefaultTileDetailsText = "Click a tile to inspect terrain cost.";
    private const string DefaultPitstopInfoText = "Pitstop Info\nSelect a pitstop to inspect its stop effect.";
    private const string FoodIconResourcePath = "UI/Hud/Icons/resource-food";
    private const string MoraleIconResourcePath = "UI/Hud/Icons/resource-morale";
    private const string GoldIconResourcePath = "UI/Hud/Icons/resource-gold";
    private const string DiagnosticScope = "HUD";

    [Header("Resources")]
    [SerializeField] private string runContextLabel = "Act 1";
    [SerializeField] private VisualTreeAsset layoutAsset;
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private Texture2D foodIcon;
    [SerializeField] private Texture2D moraleIcon;
    [SerializeField] private Texture2D goldIcon;

    [Header("Compatibility")]
    [SerializeField] private bool hideLegacyHudPanels = true;

    private HexGameplayUiRootController gameplayUiRootController;
    private VisualElement hudLayer;
    private TemplateContainer hudTree;
    private VisualElement topBar;
    private VisualElement centerStatusPocket;
    private Label runContextLabelElement;
    private Label foodValueLabel;
    private Label moraleValueLabel;
    private Label goldValueLabel;
    private Label boonStatusLabel;
    private Label selectionStatusLabel;
    private Label travelTimeLabel;
    private Label tileDetailsLabel;
    private Label hintLabel;
    private Label pitstopInfoLabel;
    private VisualElement tileInspectorPanel;
    private VisualElement pitstopPanel;
    private VisualElement foodIconElement;
    private VisualElement moraleIconElement;
    private VisualElement goldIconElement;
    private bool isGameplayModalActive;
    private bool isInitialized;

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
        foodIcon ??= Resources.Load<Texture2D>(FoodIconResourcePath);
        moraleIcon ??= Resources.Load<Texture2D>(MoraleIconResourcePath);
        goldIcon ??= Resources.Load<Texture2D>(GoldIconResourcePath);

        if (layoutAsset == null || styleSheet == null)
        {
            Debug.LogError("HexHudDocumentController could not load the UI Toolkit HUD assets from Resources.", this);
            return;
        }

        gameplayUiRootController ??= GetComponent<HexGameplayUiRootController>() ?? gameObject.AddComponent<HexGameplayUiRootController>();
        gameplayUiRootController.EnsureInitialized();
        hudLayer = gameplayUiRootController.RequestLayer(HexGameplayUiLayerId.Hud, nameof(HexHudDocumentController), true, this);
        if (hudLayer == null)
        {
            Debug.LogError("HexHudDocumentController could not bind to the shared HUD layer.", this);
            return;
        }

        hudTree = layoutAsset.CloneTree();
        hudTree.styleSheets.Add(styleSheet);
        hudLayer.Add(hudTree);

        VisualElement root = hudTree.Q<VisualElement>("hud-root");
        if (root == null)
        {
            Debug.LogError("HexHudDocumentController could not find the HUD root inside the cloned UXML.", this);
            return;
        }

        root.pickingMode = PickingMode.Ignore;

        runContextLabelElement = hudTree.Q<Label>("run-context-label");
        topBar = hudTree.Q<VisualElement>("top-bar");
        centerStatusPocket = hudTree.Q<VisualElement>("center-status-pocket");
        foodValueLabel = hudTree.Q<Label>("resource-food-value");
        moraleValueLabel = hudTree.Q<Label>("resource-morale-value");
        goldValueLabel = hudTree.Q<Label>("resource-gold-value");
        boonStatusLabel = hudTree.Q<Label>("boon-status");
        selectionStatusLabel = hudTree.Q<Label>("selection-status");
        travelTimeLabel = hudTree.Q<Label>("travel-time");
        tileDetailsLabel = hudTree.Q<Label>("tile-details");
        hintLabel = hudTree.Q<Label>("hint-text");
        pitstopInfoLabel = hudTree.Q<Label>("pitstop-info");
        tileInspectorPanel = hudTree.Q<VisualElement>("tile-inspector-panel");
        pitstopPanel = hudTree.Q<VisualElement>("pitstop-panel");
        foodIconElement = hudTree.Q<VisualElement>("resource-food-icon");
        moraleIconElement = hudTree.Q<VisualElement>("resource-morale-icon");
        goldIconElement = hudTree.Q<VisualElement>("resource-gold-icon");

        ApplyIcon(foodIconElement, foodIcon);
        ApplyIcon(moraleIconElement, moraleIcon);
        ApplyIcon(goldIconElement, goldIcon);
        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Hud, true);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Hud, false);
        isInitialized = true;
        LogDiagnostic(
            $"Bound HUD into shared root. topBarFound={topBar != null} tileInspectorFound={tileInspectorPanel != null} pitstopPanelFound={pitstopPanel != null}.");
        RefreshRunContext();
        SetResources(0, 0, 0);
        SetStatusText("Select a departure tile.");
        SetTravelTimeText("Travel Time: --");
        SetTileDetailsText(DefaultTileDetailsText);
        SetHintText(string.Empty);
        SetPitstopInfoText(DefaultPitstopInfoText);

        if (hideLegacyHudPanels)
        {
            HideLegacyHudPanelsNow();
        }
    }

    public void RefreshRunContext()
    {
        runContextLabel = $"Act {HexActTransitionService.GetCurrentActNumber()}";
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
        LogDiagnostic($"SetResources food={food} morale={morale} gold={gold} boonVisible={!string.IsNullOrWhiteSpace(boonLine)}.");
    }

    public void SetStatusText(string value)
    {
        SetLabelText(selectionStatusLabel, value);
        LogDiagnostic($"SetStatusText value='{value}'.");
    }

    public void SetTileDetailsText(string value)
    {
        SetLabelText(tileDetailsLabel, value);
        RefreshTileInspectorVisibility();
        LogDiagnostic($"SetTileDetailsText visible={tileInspectorPanel?.resolvedStyle.display.ToString() ?? "unknown"} value='{value}'.");
    }

    public void SetHintText(string value)
    {
        SetLabelText(hintLabel, value);
        if (hintLabel != null)
        {
            hintLabel.style.display = string.IsNullOrWhiteSpace(value) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        LogDiagnostic($"SetHintText visible={!string.IsNullOrWhiteSpace(value)} value='{value}'.");
    }

    public void SetPitstopInfoText(string value)
    {
        SetLabelText(pitstopInfoLabel, value);
        RefreshPitstopPanelVisibility();
        LogDiagnostic($"SetPitstopInfoText value='{value}'.");
    }

    public void SetTravelTimeText(string value)
    {
        SetLabelText(travelTimeLabel, value);
        if (travelTimeLabel != null)
        {
            travelTimeLabel.style.display = ShouldShowTravelTime(value) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        LogDiagnostic($"SetTravelTimeText visible={ShouldShowTravelTime(value)} value='{value}'.");
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
        RefreshTileInspectorVisibility();
        RefreshPitstopPanelVisibility();
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

    private void RefreshTileInspectorVisibility()
    {
        if (tileInspectorPanel == null)
        {
            return;
        }

        bool shouldDisplay = !isGameplayModalActive && ShouldShowTileInspector(tileDetailsLabel?.text);
        tileInspectorPanel.style.display = shouldDisplay ? DisplayStyle.Flex : DisplayStyle.None;
        LogDiagnostic($"RefreshTileInspectorVisibility visible={shouldDisplay}.");
    }

    private void RefreshPitstopPanelVisibility()
    {
        if (pitstopPanel == null)
        {
            return;
        }

        bool shouldDisplay = !isGameplayModalActive && ShouldShowPitstopPanel(pitstopInfoLabel?.text);
        pitstopPanel.style.display = shouldDisplay ? DisplayStyle.Flex : DisplayStyle.None;
        LogDiagnostic($"RefreshPitstopPanelVisibility visible={shouldDisplay}.");
    }

    private static bool ShouldShowTravelTime(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && !value.Contains("--");
    }

    private static bool ShouldShowTileInspector(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && !string.Equals(value.Trim(), DefaultTileDetailsText, System.StringComparison.Ordinal);
    }

    private static bool ShouldShowPitstopPanel(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && !string.Equals(value.Trim(), DefaultPitstopInfoText, System.StringComparison.Ordinal);
    }

    private static void HideLegacyHudPanelsNow()
    {
        HideLegacyObject("HUD Panel");
        HideLegacyObject("Pitstop Info Panel");
    }

    private static void HideLegacyObject(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            target.SetActive(false);
        }
    }

    private void LogDiagnostic(string message)
    {
        gameplayUiRootController?.LogDiagnostic(DiagnosticScope, message, this);
    }
}

[DisallowMultipleComponent]
public sealed class HexGameplayUiRootController : MonoBehaviour
{
    private const string PanelSettingsResourcePath = "UI/Hud/HexHudPanelSettings";
    private const string LayoutResourcePath = "UI/Gameplay/HexGameplayUiRoot";
    private const string StyleSheetResourcePath = "UI/Gameplay/HexGameplayUiRootStyles";

    [Header("Resources")]
    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private VisualTreeAsset layoutAsset;
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private int sortingOrder = 100;
    [SerializeField] private bool enableRuntimeDiagnostics = true;

    private readonly Dictionary<HexGameplayUiLayerId, VisualElement> layers = new();
    private GameObject uiRootObject;
    private UIDocument document;
    private VisualElement rootElement;
    private bool isInitialized;

    public bool IsReady => isInitialized;
    public VisualElement RootElement => rootElement;
    public VisualElement HudLayer => GetLayer(HexGameplayUiLayerId.Hud);
    public VisualElement ContextLayer => GetLayer(HexGameplayUiLayerId.Context);
    public VisualElement ModalLayer => GetLayer(HexGameplayUiLayerId.Modal);
    public VisualElement DebugLayer => GetLayer(HexGameplayUiLayerId.Debug);

    public void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        panelSettings ??= Resources.Load<PanelSettings>(PanelSettingsResourcePath);
        layoutAsset ??= Resources.Load<VisualTreeAsset>(LayoutResourcePath);
        styleSheet ??= Resources.Load<StyleSheet>(StyleSheetResourcePath);
        if (panelSettings == null || layoutAsset == null || styleSheet == null)
        {
            Debug.LogError("HexGameplayUiRootController could not load the shared UI Toolkit root assets from Resources.", this);
            return;
        }

        uiRootObject ??= new GameObject("Gameplay UI Root");
        if (uiRootObject.transform.parent != transform)
        {
            uiRootObject.transform.SetParent(transform, false);
        }

        uiRootObject.layer = gameObject.layer;

        document ??= uiRootObject.GetComponent<UIDocument>() ?? uiRootObject.AddComponent<UIDocument>();
        document.panelSettings = panelSettings;
        document.sortingOrder = sortingOrder;

        rootElement = document.rootVisualElement;
        rootElement.pickingMode = PickingMode.Ignore;
        rootElement.Clear();
        rootElement.styleSheets.Clear();
        rootElement.styleSheets.Add(styleSheet);
        layoutAsset.CloneTree(rootElement);

        layers.Clear();
        RegisterLayer(HexGameplayUiLayerId.Hud, rootElement.Q<VisualElement>("gameplay-ui-hud-layer"));
        RegisterLayer(HexGameplayUiLayerId.Context, rootElement.Q<VisualElement>("gameplay-ui-context-layer"));
        RegisterLayer(HexGameplayUiLayerId.Modal, rootElement.Q<VisualElement>("gameplay-ui-modal-layer"));
        RegisterLayer(HexGameplayUiLayerId.Debug, rootElement.Q<VisualElement>("gameplay-ui-debug-layer"));

        SetLayerInteractive(HexGameplayUiLayerId.Hud, false);
        SetLayerInteractive(HexGameplayUiLayerId.Context, false);
        SetLayerInteractive(HexGameplayUiLayerId.Modal, false);
        SetLayerInteractive(HexGameplayUiLayerId.Debug, false);

        isInitialized = true;
        LogDiagnostic("Root", $"Initialized shared gameplay UI root. sortingOrder={sortingOrder}.", this);
    }

    public VisualElement GetLayer(HexGameplayUiLayerId layerId)
    {
        return layers.TryGetValue(layerId, out VisualElement layer) ? layer : null;
    }

    public VisualElement RequestLayer(HexGameplayUiLayerId layerId, string consumerName, bool clearChildren, Object context = null)
    {
        EnsureInitialized();

        VisualElement layer = GetLayer(layerId);
        if (layer == null)
        {
            LogDiagnostic("Root", $"Layer request failed. layer={layerId} consumer={consumerName}.", context ? context : this);
            return null;
        }

        if (clearChildren)
        {
            layer.Clear();
            LogDiagnostic("Root", $"Cleared layer '{layerId}' for consumer={consumerName}.", context ? context : this);
        }

        LogDiagnostic(
            "Root",
            $"Layer request success. layer={layerId} consumer={consumerName} interactive={layer.pickingMode == PickingMode.Position} display={layer.resolvedStyle.display}.",
            context ? context : this);
        return layer;
    }

    public void SetLayerVisible(HexGameplayUiLayerId layerId, bool visible)
    {
        VisualElement layer = GetLayer(layerId);
        if (layer == null)
        {
            return;
        }

        layer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        LogDiagnostic("Root", $"SetLayerVisible layer={layerId} visible={visible}.", this);
    }

    public void SetLayerInteractive(HexGameplayUiLayerId layerId, bool interactive)
    {
        VisualElement layer = GetLayer(layerId);
        if (layer == null)
        {
            return;
        }

        layer.pickingMode = interactive ? PickingMode.Position : PickingMode.Ignore;
        LogDiagnostic("Root", $"SetLayerInteractive layer={layerId} interactive={interactive}.", this);
    }

    private void RegisterLayer(HexGameplayUiLayerId layerId, VisualElement layer)
    {
        if (layer == null)
        {
            Debug.LogError($"HexGameplayUiRootController could not find layer '{layerId}'.", this);
            return;
        }

        layers[layerId] = layer;
        layer.style.display = DisplayStyle.Flex;
        LogDiagnostic("Root", $"Registered layer '{layerId}'.", this);
    }

    public void LogDiagnostic(string scope, string message, Object context = null)
    {
        if (!enableRuntimeDiagnostics)
        {
            return;
        }

        Debug.Log($"[GameplayUI:{scope}] {message}", context ? context : this);
    }
}

public enum HexGameplayUiLayerId
{
    Hud,
    Context,
    Modal,
    Debug
}
