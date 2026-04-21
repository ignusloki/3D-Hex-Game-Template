using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class HexHudDocumentController : MonoBehaviour, IHexHudView, IHexTravelTimeView
{
    private const string PanelSettingsResourcePath = "UI/Hud/HexHudPanelSettings";
    private const string LayoutResourcePath = "UI/Hud/HexMainHud";
    private const string StyleSheetResourcePath = "UI/Hud/HexMainHudStyles";
    private const string DefaultTileDetailsText = "Click a tile to inspect terrain cost.";
    private const string DefaultPitstopInfoText = "Pitstop Info\nSelect a pitstop to inspect its stop effect.";
    private const string FoodIconResourcePath = "UI/Hud/Icons/resource-food";
    private const string MoraleIconResourcePath = "UI/Hud/Icons/resource-morale";
    private const string GoldIconResourcePath = "UI/Hud/Icons/resource-gold";

    [Header("Resources")]
    [SerializeField] private string runContextLabel = "Act 1";
    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private VisualTreeAsset layoutAsset;
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private Texture2D foodIcon;
    [SerializeField] private Texture2D moraleIcon;
    [SerializeField] private Texture2D goldIcon;

    [Header("Compatibility")]
    [SerializeField] private bool hideLegacyHudPanels = true;

    private UIDocument document;
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

        panelSettings ??= Resources.Load<PanelSettings>(PanelSettingsResourcePath);
        layoutAsset ??= Resources.Load<VisualTreeAsset>(LayoutResourcePath);
        styleSheet ??= Resources.Load<StyleSheet>(StyleSheetResourcePath);
        foodIcon ??= Resources.Load<Texture2D>(FoodIconResourcePath);
        moraleIcon ??= Resources.Load<Texture2D>(MoraleIconResourcePath);
        goldIcon ??= Resources.Load<Texture2D>(GoldIconResourcePath);

        if (panelSettings == null || layoutAsset == null || styleSheet == null)
        {
            Debug.LogError("HexHudDocumentController could not load the UI Toolkit HUD assets from Resources.", this);
            return;
        }

        document ??= GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
        document.panelSettings = panelSettings;
        document.sortingOrder = 300;

        VisualElement root = document.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        root.Clear();
        root.styleSheets.Clear();
        root.styleSheets.Add(styleSheet);
        layoutAsset.CloneTree(root);

        runContextLabelElement = root.Q<Label>("run-context-label");
        topBar = root.Q<VisualElement>("top-bar");
        centerStatusPocket = root.Q<VisualElement>("center-status-pocket");
        foodValueLabel = root.Q<Label>("resource-food-value");
        moraleValueLabel = root.Q<Label>("resource-morale-value");
        goldValueLabel = root.Q<Label>("resource-gold-value");
        boonStatusLabel = root.Q<Label>("boon-status");
        selectionStatusLabel = root.Q<Label>("selection-status");
        travelTimeLabel = root.Q<Label>("travel-time");
        tileDetailsLabel = root.Q<Label>("tile-details");
        hintLabel = root.Q<Label>("hint-text");
        pitstopInfoLabel = root.Q<Label>("pitstop-info");
        tileInspectorPanel = root.Q<VisualElement>("tile-inspector-panel");
        pitstopPanel = root.Q<VisualElement>("pitstop-panel");
        foodIconElement = root.Q<VisualElement>("resource-food-icon");
        moraleIconElement = root.Q<VisualElement>("resource-morale-icon");
        goldIconElement = root.Q<VisualElement>("resource-gold-icon");

        ApplyIcon(foodIconElement, foodIcon);
        ApplyIcon(moraleIconElement, moraleIcon);
        ApplyIcon(goldIconElement, goldIcon);
        isInitialized = true;
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
    }

    public void SetStatusText(string value)
    {
        SetLabelText(selectionStatusLabel, value);
    }

    public void SetTileDetailsText(string value)
    {
        SetLabelText(tileDetailsLabel, value);
        RefreshTileInspectorVisibility();
    }

    public void SetHintText(string value)
    {
        SetLabelText(hintLabel, value);
        if (hintLabel != null)
        {
            hintLabel.style.display = string.IsNullOrWhiteSpace(value) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    public void SetPitstopInfoText(string value)
    {
        SetLabelText(pitstopInfoLabel, value);
        RefreshPitstopPanelVisibility();
    }

    public void SetTravelTimeText(string value)
    {
        SetLabelText(travelTimeLabel, value);
        if (travelTimeLabel != null)
        {
            travelTimeLabel.style.display = ShouldShowTravelTime(value) ? DisplayStyle.Flex : DisplayStyle.None;
        }
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
    }

    private void RefreshPitstopPanelVisibility()
    {
        if (pitstopPanel == null)
        {
            return;
        }

        bool shouldDisplay = !isGameplayModalActive && ShouldShowPitstopPanel(pitstopInfoLabel?.text);
        pitstopPanel.style.display = shouldDisplay ? DisplayStyle.Flex : DisplayStyle.None;
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
}
