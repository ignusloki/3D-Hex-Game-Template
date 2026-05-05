using System;
using System.Collections.Generic;
using UnityEngine;
using UIE = UnityEngine.UIElements;

internal sealed class HexGameOverOverlayDocumentController
{
    private const string LayoutResourcePath = "UI/Modal/HexGameOverOverlay";
    private const string StyleSheetResourcePath = "UI/Modal/HexGameOverOverlayStyles";
    private const string RimouskiFontEditorAssetPath = "Assets/Art/Fonts/rimouski sb.otf";
    private const string BackgroundEditorAssetPath = "Assets/Art/Image/Placeholder/parallax-forest.png";

    private readonly MonoBehaviour owner;

    private UIE.VisualTreeAsset layoutAsset;
    private UIE.StyleSheet styleSheet;
    private Font rimouskiFont;
    private Texture2D backgroundTexture;
    private HexGameplayUiRootController gameplayUiRootController;
    private UIE.VisualElement modalMount;
    private UIE.VisualElement modalRoot;
    private UIE.VisualElement blackoutElement;
    private UIE.VisualElement backgroundElement;
    private UIE.VisualElement scrimElement;
    private UIE.VisualElement separatorElement;
    private UIE.VisualElement panelElement;
    private UIE.Label titleLabel;
    private UIE.Label descriptionLabel;
    private UIE.Button retryButton;
    private UIE.Button returnToTitleButton;
    private HexUiTransitionPlayer transitionPlayer;
    private HexUiTransitionTargetSet transitionTargets;
    private HexUiTransitionProfile transitionProfile;
    private HexGameOverTransitionSettings transitionSettings;
    private HexHudDocumentController hudDocumentController;
    private Action retryRequested;
    private Action returnToTitleRequested;
    private bool isInitialized;
    private bool isOpen;
    private bool areTransitionControlsLocked;

    public HexGameOverOverlayDocumentController(MonoBehaviour owner)
    {
        this.owner = owner;
    }

    public bool IsOpen => isOpen;

    public void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        layoutAsset ??= Resources.Load<UIE.VisualTreeAsset>(LayoutResourcePath);
        styleSheet ??= Resources.Load<UIE.StyleSheet>(StyleSheetResourcePath);
        rimouskiFont ??= LoadRimouskiFont();
        backgroundTexture ??= LoadGameOverBackgroundTexture();
        transitionSettings = ResolveTransitionSettings();
        if (layoutAsset == null || styleSheet == null)
        {
            Debug.LogError("HexGameOverOverlayDocumentController could not load Game Over UI Toolkit assets.", owner);
            return;
        }

        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(owner);
        if (gameplayUiRootController == null)
        {
            Debug.LogError("HexGameOverOverlayDocumentController could not resolve the shared gameplay UI root.", owner);
            return;
        }

        gameplayUiRootController.EnsureInitialized();
        modalMount = gameplayUiRootController.RequestLayerMount(
            HexGameplayUiLayerId.Modal,
            "game-over-overlay-mount",
            "GameOverOverlay",
            false,
            owner);
        if (modalMount == null)
        {
            Debug.LogError("HexGameOverOverlayDocumentController could not bind to the shared modal layer.", owner);
            return;
        }

        modalMount.Clear();
        modalMount.styleSheets.Clear();
        modalMount.styleSheets.Add(styleSheet);
        layoutAsset.CloneTree(modalMount);

        modalRoot = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "GameOverOverlay");
        blackoutElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "GameOverBlackout");
        backgroundElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "GameOverBackground");
        scrimElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "GameOverScrim");
        separatorElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "GameOverSeparator");
        panelElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "GameOverPanel");
        titleLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "GameOverTitle");
        descriptionLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "GameOverDescription");
        retryButton = UIE.UQueryExtensions.Q<UIE.Button>(modalMount, "RetryButton");
        returnToTitleButton = UIE.UQueryExtensions.Q<UIE.Button>(modalMount, "ReturnToTitleButton");
        transitionPlayer = new HexUiTransitionPlayer(modalRoot);
        transitionTargets = CreateTransitionTargets();

        ApplyRimouskiFont(titleLabel);
        ApplyRimouskiFont(retryButton);
        ApplyRimouskiFont(returnToTitleButton);

        if (backgroundElement != null && backgroundTexture != null)
        {
            backgroundElement.style.backgroundImage = new UIE.StyleBackground(backgroundTexture);
        }

        if (retryButton != null)
        {
            HexAudioUiBinder.BindButton(retryButton, owner);
            retryButton.clicked += HandleRetryClicked;
        }

        if (returnToTitleButton != null)
        {
            HexAudioUiBinder.BindButton(returnToTitleButton, owner);
            returnToTitleButton.clicked += HandleReturnToTitleClicked;
        }

        if (modalRoot != null)
        {
            modalRoot.pickingMode = UIE.PickingMode.Position;
            modalRoot.style.display = UIE.DisplayStyle.None;
        }

        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Modal, false);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Modal, false);
        isInitialized = true;
        LogDebug(
            $"Initialized Game Over overlay. rootFound={modalRoot != null} backgroundFound={backgroundElement != null} retryFound={retryButton != null} returnFound={returnToTitleButton != null} backgroundTextureFound={backgroundTexture != null}.");
    }

    public void Show(Action onRetryRequested, Action onReturnToTitleRequested)
    {
        EnsureInitialized();
        if (!isInitialized || modalRoot == null)
        {
            return;
        }

        if (isOpen)
        {
            LogDebug("Show ignored because the Game Over overlay is already open.");
            return;
        }

        retryRequested = onRetryRequested;
        returnToTitleRequested = onReturnToTitleRequested;
        transitionSettings = ResolveTransitionSettings();
        if (titleLabel != null)
        {
            titleLabel.text = "Game Over";
        }

        if (retryButton != null)
        {
            retryButton.text = "Retry";
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.text = "Return to Title";
        }

        PrepareTransitionInitialState();
        SetOverlayVisibility(true);
        StartTransition();
        LogDebug($"Show Game Over overlay. retryAssigned={retryRequested != null}.");
    }

    public void Hide()
    {
        StopTransition();
        retryRequested = null;
        returnToTitleRequested = null;
        SetOverlayVisibility(false);
        LogDebug("Hide Game Over overlay.");
    }

    private void HandleRetryClicked()
    {
        if (areTransitionControlsLocked)
        {
            return;
        }

        LogDebug($"HandleRetryClicked callbackAssigned={retryRequested != null}.");
        Action callback = retryRequested;
        Hide();
        callback?.Invoke();
    }

    private void HandleReturnToTitleClicked()
    {
        if (areTransitionControlsLocked)
        {
            return;
        }

        LogDebug($"HandleReturnToTitleClicked callbackAssigned={returnToTitleRequested != null}.");
        Action callback = returnToTitleRequested;
        if (callback == null)
        {
            return;
        }

        SetTransitionInteractionLock(true);
        callback.Invoke();
    }

    private void PrepareTransitionInitialState()
    {
        StopTransition();
        SetButtonInteractionEnabled(false);
        SetElementOpacity(blackoutElement, 0f);
        SetElementOpacity(backgroundElement, 0f);
        SetElementOpacity(scrimElement, 0f);
        SetElementOpacity(titleLabel, 0f);
        SetElementOpacity(separatorElement, 0f);
        SetElementOpacity(panelElement, 0f);
        SetElementOpacity(descriptionLabel, 0f);
        SetElementOpacity(retryButton, 0f);
        SetElementOpacity(returnToTitleButton, 0f);
    }

    private void StartTransition()
    {
        if (modalRoot == null || transitionPlayer == null || transitionTargets == null)
        {
            CompleteTransition();
            return;
        }

        ReleaseTransitionProfile();
        transitionProfile = transitionSettings != null
            ? transitionSettings.CreateRuntimeProfile()
            : HexGameOverTransitionSettings.CreateDefaultRuntimeProfile();

        bool started = transitionPlayer.Play(
            transitionProfile,
            transitionTargets,
            CompleteTransition,
            SetTransitionInteractionLock,
            message => LogDebug(message));
        if (!started)
        {
            CompleteTransition();
        }
    }

    private void StopTransition()
    {
        transitionPlayer?.Cancel(applyEndState: false);
        SetTransitionInteractionLock(false);
        ReleaseTransitionProfile();
    }

    private void CompleteTransition()
    {
        SetElementOpacity(blackoutElement, 1f);
        SetElementOpacity(backgroundElement, 1f);
        SetElementOpacity(scrimElement, 1f);
        SetElementOpacity(titleLabel, 1f);
        SetElementOpacity(separatorElement, 1f);
        SetElementOpacity(panelElement, 1f);
        SetElementOpacity(descriptionLabel, 1f);
        SetElementOpacity(retryButton, 1f);
        SetElementOpacity(returnToTitleButton, 1f);
        SetTransitionInteractionLock(false);
        ReleaseTransitionProfile();
    }

    private HexUiTransitionTargetSet CreateTransitionTargets()
    {
        return new HexUiTransitionTargetSet()
            .Register(HexGameOverTransitionSettings.BlackoutTargetKey, blackoutElement)
            .Register(HexGameOverTransitionSettings.BackgroundTargetKey, backgroundElement)
            .Register(HexGameOverTransitionSettings.ScrimTargetKey, scrimElement)
            .Register(HexGameOverTransitionSettings.TitleTargetKey, titleLabel)
            .Register(HexGameOverTransitionSettings.SeparatorTargetKey, separatorElement)
            .Register(HexGameOverTransitionSettings.PanelTargetKey, panelElement)
            .Register(HexGameOverTransitionSettings.DescriptionTargetKey, descriptionLabel)
            .Register(HexGameOverTransitionSettings.RetryButtonTargetKey, retryButton)
            .Register(HexGameOverTransitionSettings.ReturnToTitleButtonTargetKey, returnToTitleButton);
    }

    private static void SetElementOpacity(UIE.VisualElement element, float opacity)
    {
        if (element != null)
        {
            element.style.opacity = Mathf.Clamp01(opacity);
        }
    }

    private void SetTransitionInteractionLock(bool locked)
    {
        areTransitionControlsLocked = locked;
        SetButtonInteractionEnabled(!locked);
    }

    private void SetButtonInteractionEnabled(bool enabled)
    {
        if (retryButton != null)
        {
            retryButton.pickingMode = enabled ? UIE.PickingMode.Position : UIE.PickingMode.Ignore;
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.pickingMode = enabled ? UIE.PickingMode.Position : UIE.PickingMode.Ignore;
        }
    }

    private void ReleaseTransitionProfile()
    {
        if (transitionProfile != null)
        {
            UnityEngine.Object.Destroy(transitionProfile);
            transitionProfile = null;
        }
    }

    private void SetOverlayVisibility(bool visible)
    {
        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        hudDocumentController?.SetGameplayModalState(visible);

        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Hud, !visible);
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Context, !visible);
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Modal, visible);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Hud, false);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Context, false);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Modal, visible);
        }

        if (modalRoot != null)
        {
            modalRoot.style.display = visible ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
        }

        isOpen = visible;
        LogDebug($"SetOverlayVisibility visible={visible}.");
    }

    private void ApplyRimouskiFont(UIE.VisualElement element)
    {
        if (element == null || rimouskiFont == null)
        {
            return;
        }

        element.style.unityFont = new UIE.StyleFont(rimouskiFont);
        element.style.unityFontDefinition = UIE.FontDefinition.FromFont(rimouskiFont);
    }

    private static Font LoadRimouskiFont()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(RimouskiFontEditorAssetPath);
#else
        return null;
#endif
    }

    private static Texture2D LoadGameOverBackgroundTexture()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundEditorAssetPath);
#else
        return null;
#endif
    }

    private static HexGameOverTransitionSettings ResolveTransitionSettings()
    {
        return UnityEngine.Object.FindAnyObjectByType<HexGameOverTransitionSettings>();
    }

    private void LogDebug(string message, bool verbose = false)
    {
        if (owner is HexRunEndModalPresenter runEndPresenter)
        {
            runEndPresenter.LogDebug(message, verbose);
            return;
        }

        Debug.Log($"[GameplayUI:GameOverOverlay] {message}", owner);
    }
}
