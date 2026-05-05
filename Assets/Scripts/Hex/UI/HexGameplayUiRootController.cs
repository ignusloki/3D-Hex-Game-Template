using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
    [SerializeField] private bool enableVerboseRuntimeDiagnostics;

    private static HexGameplayUiRootController sharedInstance;
    private readonly Dictionary<HexGameplayUiLayerId, VisualElement> layers = new();
    private GameObject uiRootObject;
    private UIDocument document;
    private VisualElement rootElement;
    private bool isInitialized;

    public bool IsReady => isInitialized;
    public VisualElement RootElement => rootElement;
    public VisualElement HudLayer => GetLayer(HexGameplayUiLayerId.Hud);
    public VisualElement ContextLayer => GetLayer(HexGameplayUiLayerId.Context);
    public VisualElement MenuLayer => GetLayer(HexGameplayUiLayerId.Menu);
    public VisualElement ModalLayer => GetLayer(HexGameplayUiLayerId.Modal);
    public VisualElement GlobalTransitionLayer => GetLayer(HexGameplayUiLayerId.GlobalTransition);
    public VisualElement DebugLayer => GetLayer(HexGameplayUiLayerId.Debug);

    public static HexGameplayUiRootController ResolveShared(MonoBehaviour owner, bool createIfMissing = true)
    {
        if (sharedInstance != null)
        {
            return sharedInstance;
        }

        sharedInstance = Object.FindAnyObjectByType<HexGameplayUiRootController>();
        if (sharedInstance != null)
        {
            return sharedInstance;
        }

        if (!createIfMissing || owner == null)
        {
            return null;
        }

        sharedInstance =
            owner.GetComponentInParent<HexGameplayUiRootController>()
            ?? owner.GetComponent<HexGameplayUiRootController>()
            ?? owner.gameObject.AddComponent<HexGameplayUiRootController>();
        return sharedInstance;
    }

    private void OnEnable()
    {
        if (sharedInstance == null)
        {
            sharedInstance = this;
        }
    }

    private void OnDestroy()
    {
        if (sharedInstance == this)
        {
            sharedInstance = null;
        }
    }

    public void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        if (sharedInstance == null)
        {
            sharedInstance = this;
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
        RegisterLayer(HexGameplayUiLayerId.Menu, rootElement.Q<VisualElement>("gameplay-ui-menu-layer"));
        RegisterLayer(HexGameplayUiLayerId.Modal, rootElement.Q<VisualElement>("gameplay-ui-modal-layer"));
        RegisterLayer(HexGameplayUiLayerId.GlobalTransition, rootElement.Q<VisualElement>("gameplay-ui-global-transition-layer"));
        RegisterLayer(HexGameplayUiLayerId.Debug, rootElement.Q<VisualElement>("gameplay-ui-debug-layer"));

        SetLayerInteractive(HexGameplayUiLayerId.Hud, false);
        SetLayerInteractive(HexGameplayUiLayerId.Context, false);
        SetLayerInteractive(HexGameplayUiLayerId.Menu, false);
        SetLayerInteractive(HexGameplayUiLayerId.Modal, false);
        SetLayerInteractive(HexGameplayUiLayerId.GlobalTransition, false);
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

    public VisualElement RequestLayerMount(
        HexGameplayUiLayerId layerId,
        string mountName,
        string consumerName,
        bool clearChildren = false,
        Object context = null)
    {
        if (string.IsNullOrWhiteSpace(mountName))
        {
            LogDiagnostic("Root", $"Mount request failed. layer={layerId} consumer={consumerName} mountName was empty.", context ? context : this);
            return null;
        }

        VisualElement layer = RequestLayer(layerId, consumerName, false, context);
        if (layer == null)
        {
            return null;
        }

        VisualElement mount = layer.Q<VisualElement>(mountName);
        if (mount == null)
        {
            mount = new VisualElement
            {
                name = mountName,
                pickingMode = PickingMode.Ignore
            };
            mount.AddToClassList("gameplay-ui-mount");
            layer.Add(mount);
            LogDiagnostic("Root", $"Created mount '{mountName}' on layer={layerId} for consumer={consumerName}.", context ? context : this);
        }

        if (clearChildren)
        {
            mount.Clear();
            LogDiagnostic("Root", $"Cleared mount '{mountName}' on layer={layerId} for consumer={consumerName}.", context ? context : this, true);
        }

        return mount;
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

    public void LogDiagnostic(string scope, string message, Object context = null, bool verbose = false)
    {
        if (!enableRuntimeDiagnostics)
        {
            return;
        }

        if (verbose && !enableVerboseRuntimeDiagnostics)
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
    Menu,
    Modal,
    GlobalTransition,
    Debug
}
