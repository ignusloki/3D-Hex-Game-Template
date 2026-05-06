using System;
using System.Collections.Generic;
using UnityEngine;
using UIE = UnityEngine.UIElements;

internal sealed class HexVictoryOverlayDocumentController
{
    private const string LayoutResourcePath = "UI/Modal/HexVictoryOverlay";
    private const string StyleSheetResourcePath = "UI/Modal/HexVictoryOverlayStyles";
    private const string SimpleModalFadeProfileResourcePath = "UI/Transitions/SimpleModalFade";
    private const string VictoryHeroResourcePath = "UI/Victory/S_2";
    private const string ModalMountName = "victory-overlay-mount";
    private const string RimouskiFontEditorAssetPath = "Assets/Art/Fonts/rimouski sb.otf";
    private const string ScrimTransitionTargetKey = "scrim";
    private const string ShellTransitionTargetKey = "shell";

    private readonly MonoBehaviour owner;

    private UIE.VisualTreeAsset layoutAsset;
    private UIE.StyleSheet styleSheet;
    private HexUiTransitionProfile simpleModalFadeProfile;
    private HexUiTransitionProfile activeTransitionProfile;
    private Font rimouskiFont;
    private Texture2D heroTexture;
    private HexGameplayUiRootController gameplayUiRootController;
    private UIE.VisualElement modalMount;
    private UIE.VisualElement modalRoot;
    private UIE.VisualElement scrimElement;
    private UIE.VisualElement pageElement;
    private UIE.Label titleLabel;
    private UIE.Button continueButton;
    private UIE.Image heroImage;
    private HexUiTransitionPlayer transitionPlayer;
    private HexHudDocumentController hudDocumentController;
    private HexGlobalUiTransitionController globalTransitionController;
    private Action continueRequested;
    private bool isInitialized;
    private bool isOpen;
    private bool areTransitionControlsLocked;
    private bool isContinueTransitioning;

    public HexVictoryOverlayDocumentController(MonoBehaviour owner)
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
        simpleModalFadeProfile ??= Resources.Load<HexUiTransitionProfile>(SimpleModalFadeProfileResourcePath);
        rimouskiFont ??= LoadRimouskiFont();
        heroTexture ??= ResolveVictoryHeroTexture();
        if (layoutAsset == null || styleSheet == null)
        {
            Debug.LogError("HexVictoryOverlayDocumentController could not load Victory UI Toolkit assets.", owner);
            return;
        }

        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(owner);
        if (gameplayUiRootController == null)
        {
            Debug.LogError("HexVictoryOverlayDocumentController could not resolve the shared gameplay UI root.", owner);
            return;
        }

        gameplayUiRootController.EnsureInitialized();
        modalMount = gameplayUiRootController.RequestLayerMount(
            HexGameplayUiLayerId.Modal,
            ModalMountName,
            "VictoryOverlay",
            false,
            owner);
        if (modalMount == null)
        {
            Debug.LogError("HexVictoryOverlayDocumentController could not bind to the shared modal layer.", owner);
            return;
        }

        modalMount.Clear();
        modalMount.styleSheets.Clear();
        modalMount.styleSheets.Add(styleSheet);
        layoutAsset.CloneTree(modalMount);

        modalRoot = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "VictoryOverlay");
        scrimElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "VictoryScrim");
        pageElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "VictoryPage");
        titleLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "VictoryTitle");
        continueButton = UIE.UQueryExtensions.Q<UIE.Button>(modalMount, "VictoryContinueButton");
        heroImage = UIE.UQueryExtensions.Q<UIE.Image>(modalMount, "VictoryHeroImage");

        ApplyRimouskiFont(titleLabel);
        ApplyRimouskiFont(continueButton);
        ApplyHeroImage();

        if (continueButton != null)
        {
            HexAudioUiBinder.BindButton(continueButton, owner);
            continueButton.clicked += HandleContinueClicked;
        }

        if (modalRoot != null)
        {
            modalRoot.style.display = UIE.DisplayStyle.None;
            transitionPlayer = new HexUiTransitionPlayer(modalRoot);
        }

        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        globalTransitionController ??= HexGlobalUiTransitionController.ResolveShared(owner);
        globalTransitionController?.EnsureInitialized();
        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Modal, false);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Modal, false);
        isInitialized = true;
        LogDebug($"Initialized Victory overlay. rootFound={modalRoot != null} pageFound={pageElement != null} heroFound={heroTexture != null}.");
    }

    public void Show(Action onContinueRequested)
    {
        EnsureInitialized();
        if (!isInitialized || modalRoot == null)
        {
            return;
        }

        continueRequested = onContinueRequested;
        SetOverlayVisibility(true);
        PrepareOpenTransitionInitialState();
        StartOpenTransition();
        LogDebug("Show Victory overlay.");
    }

    public void Hide()
    {
        StopOpenTransition();
        isContinueTransitioning = false;
        continueRequested = null;
        SetOverlayVisibility(false);
        LogDebug("Hide Victory overlay.");
    }

    private void ApplyHeroImage()
    {
        if (heroImage == null)
        {
            return;
        }

        heroImage.scaleMode = ScaleMode.ScaleAndCrop;
        heroImage.image = heroTexture;
        heroImage.style.display = heroTexture != null ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
    }

    private void HandleContinueClicked()
    {
        if (areTransitionControlsLocked || isContinueTransitioning)
        {
            return;
        }

        LogDebug($"HandleContinueClicked callbackAssigned={continueRequested != null}.");
        Action callback = continueRequested;
        continueRequested = null;
        isContinueTransitioning = true;
        SetTransitionInteractionLock(true);

        globalTransitionController ??= HexGlobalUiTransitionController.ResolveShared(owner);
        globalTransitionController?.EnsureInitialized();
        bool started = globalTransitionController != null
            && globalTransitionController.PlayFadeToBlack(
                () => InvokeContinueCallback(callback),
                null,
                fadeBackOut: false);
        if (!started)
        {
            LogDebug("Global fade-to-black was unavailable. Continuing immediately.");
            InvokeContinueCallback(callback);
        }
    }

    private void InvokeContinueCallback(Action callback)
    {
        Hide();
        callback?.Invoke();
    }

    private void PrepareOpenTransitionInitialState()
    {
        StopOpenTransition();
        SetButtonInteractionEnabled(false);
        SetElementOpacity(scrimElement, 0f);
        SetElementOpacity(pageElement, 0f);
    }

    private void StartOpenTransition()
    {
        if (transitionPlayer == null || pageElement == null)
        {
            CompleteOpenTransition();
            return;
        }

        ReleaseTransitionProfile();
        activeTransitionProfile = HexUiTransitionProfile.CreateRuntimeProfile(
            simpleModalFadeProfile,
            "victory-simple-modal-fade",
            HexUiTransitionEasing.EaseOut,
            CreateDefaultSimpleModalFadeTracks());

        HexUiTransitionTargetSet targets = new HexUiTransitionTargetSet()
            .Register(ScrimTransitionTargetKey, scrimElement)
            .Register(ShellTransitionTargetKey, pageElement);

        bool started = transitionPlayer.Play(
            activeTransitionProfile,
            targets,
            CompleteOpenTransition,
            SetTransitionInteractionLock,
            message => LogDebug(message, true));
        if (!started)
        {
            CompleteOpenTransition();
        }
    }

    private void StopOpenTransition()
    {
        transitionPlayer?.Cancel(applyEndState: false);
        SetTransitionInteractionLock(false);
        ReleaseTransitionProfile();
    }

    private void CompleteOpenTransition()
    {
        SetElementOpacity(scrimElement, 1f);
        SetElementOpacity(pageElement, 1f);
        SetTransitionInteractionLock(false);
        ReleaseTransitionProfile();
    }

    private static IReadOnlyList<HexUiTransitionFadeTrack> CreateDefaultSimpleModalFadeTracks()
    {
        return new[]
        {
            new HexUiTransitionFadeTrack
            {
                targetKey = ScrimTransitionTargetKey,
                startTime = 0f,
                duration = 0.12f,
                startOpacity = 0f,
                endOpacity = 1f,
                isRequired = true
            },
            new HexUiTransitionFadeTrack
            {
                targetKey = ShellTransitionTargetKey,
                startTime = 0.04f,
                duration = 0.16f,
                startOpacity = 0f,
                endOpacity = 1f,
                isRequired = true
            }
        };
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

    private void SetTransitionInteractionLock(bool locked)
    {
        areTransitionControlsLocked = locked;
        SetButtonInteractionEnabled(!locked);
    }

    private void SetButtonInteractionEnabled(bool enabled)
    {
        if (continueButton == null)
        {
            return;
        }

        continueButton.SetEnabled(enabled);
        continueButton.pickingMode = enabled ? UIE.PickingMode.Position : UIE.PickingMode.Ignore;
        continueButton.focusable = enabled;
    }

    private static void SetElementOpacity(UIE.VisualElement element, float opacity)
    {
        if (element != null)
        {
            element.style.opacity = Mathf.Clamp01(opacity);
        }
    }

    private void ReleaseTransitionProfile()
    {
        if (activeTransitionProfile != null)
        {
            UnityEngine.Object.Destroy(activeTransitionProfile);
            activeTransitionProfile = null;
        }
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

    private static Texture2D ResolveVictoryHeroTexture()
    {
        return Resources.Load<Texture2D>(VictoryHeroResourcePath);
    }

    private void LogDebug(string message, bool verbose = false)
    {
        if (owner is HexRunEndModalPresenter runEndPresenter)
        {
            runEndPresenter.LogDebug(message, verbose);
            return;
        }

        Debug.Log($"[GameplayUI:VictoryOverlay] {message}", owner);
    }
}
