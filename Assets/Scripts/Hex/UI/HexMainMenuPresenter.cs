using System;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class HexMainMenuPresenter : MonoBehaviour
{
    private const string LayoutResourcePath = "UI/Menu/HexMainMenu";
    private const string StyleSheetResourcePath = "UI/Menu/HexMainMenuStyles";
    private const string LogoResourcePath = "UI/Menu/Logo";
    private const string MenuMountName = "main-menu-mount";
    private const string RimouskiFontEditorAssetPath = "Assets/Art/Fonts/rimouski sb.otf";
    private const string LogoEditorAssetPath = "Assets/Art/Logo/Logo.png";

    [SerializeField] private VisualTreeAsset layoutAsset;
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private Texture2D logoTexture;
    [SerializeField] private Font buttonFont;
    [SerializeField] private HexMainMenuTransitionService transitionService;
    [SerializeField] private HexAudioSystem audioSystem;

    private HexGameplayUiRootController gameplayUiRootController;
    private HexHudDocumentController hudDocumentController;
    private VisualElement menuMount;
    private VisualElement menuRoot;
    private VisualElement vignetteElement;
    private Image logoImage;
    private Button newGameButton;
    private Button quitButton;
    private Action newGameRequested;
    private bool isInitialized;
    private bool isOpen;
    private bool isTransitioning;

    public bool IsOpen => isOpen;

    private void OnEnable()
    {
        if (!isInitialized)
        {
            EnsureInitialized();
        }
    }

    public void InitializeForBootstrap()
    {
        EnsureInitialized();
    }

    public void ShowBootMenu(Action onNewGameRequested)
    {
        EnsureInitialized();
        if (!isInitialized)
        {
            onNewGameRequested?.Invoke();
            return;
        }

        newGameRequested = () =>
        {
            bool startedGameplay = false;
            void StartGameplayOnce()
            {
                if (startedGameplay)
                {
                    return;
                }

                startedGameplay = true;
                HideForGameplay();
                onNewGameRequested?.Invoke();
            }

            transitionService ??= GetComponent<HexMainMenuTransitionService>() ?? gameObject.AddComponent<HexMainMenuTransitionService>();
            bool started = transitionService.PlayMainMenuToActOne(StartGameplayOnce);
            if (!started)
            {
                StartGameplayOnce();
            }
        };
        SetMenuInteractionEnabled(false);
        isTransitioning = true;

        bool revealCompleted = false;
        void CompleteReveal()
        {
            if (revealCompleted)
            {
                return;
            }

            revealCompleted = true;
            isTransitioning = false;
            SetMenuInteractionEnabled(true);
        }

        transitionService ??= GetComponent<HexMainMenuTransitionService>() ?? gameObject.AddComponent<HexMainMenuTransitionService>();
        bool started = transitionService.PlayStartupToMainMenu(
            () => SetMenuVisibility(true, showGameplayLayers: false),
            CompleteReveal);
        if (!started)
        {
            CompleteReveal();
        }
    }

    public void HideForGameplay()
    {
        SetMenuVisibility(false, showGameplayLayers: true);
        newGameRequested = null;
        isTransitioning = false;
    }

    public void ShowMenu(Action onNewGameRequested, bool showGameplayLayers, bool enableInteraction)
    {
        EnsureInitialized();
        if (!isInitialized)
        {
            onNewGameRequested?.Invoke();
            return;
        }

        newGameRequested = onNewGameRequested;
        isTransitioning = false;
        SetMenuVisibility(true, showGameplayLayers);
        SetMenuInteractionEnabled(enableInteraction);
    }

    public void SetMenuVisibleImmediate(bool visible, bool showGameplayLayers)
    {
        EnsureInitialized();
        if (!isInitialized)
        {
            return;
        }

        SetMenuVisibility(visible, showGameplayLayers);
        SetMenuInteractionEnabled(visible);
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        layoutAsset ??= Resources.Load<VisualTreeAsset>(LayoutResourcePath);
        styleSheet ??= Resources.Load<StyleSheet>(StyleSheetResourcePath);
        logoTexture ??= Resources.Load<Texture2D>(LogoResourcePath);
        buttonFont ??= LoadRimouskiFont();
        logoTexture ??= LoadEditorLogoTexture();
        if (layoutAsset == null || styleSheet == null)
        {
            Debug.LogError("HexMainMenuPresenter could not load the Main Menu UI Toolkit assets.", this);
            return;
        }

        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this);
        if (gameplayUiRootController == null)
        {
            Debug.LogError("HexMainMenuPresenter could not resolve the shared gameplay UI root.", this);
            return;
        }

        gameplayUiRootController.EnsureInitialized();
        menuMount = gameplayUiRootController.RequestLayerMount(
            HexGameplayUiLayerId.Menu,
            MenuMountName,
            nameof(HexMainMenuPresenter),
            false,
            this);
        if (menuMount == null)
        {
            Debug.LogError("HexMainMenuPresenter could not bind to the shared menu layer.", this);
            return;
        }

        menuMount.Clear();
        menuMount.styleSheets.Clear();
        menuMount.styleSheets.Add(styleSheet);
        layoutAsset.CloneTree(menuMount);

        menuRoot = menuMount.Q<VisualElement>("MainMenuRoot");
        vignetteElement = menuMount.Q<VisualElement>("MainMenuVignette");
        logoImage = menuMount.Q<Image>("MainMenuLogo");
        newGameButton = menuMount.Q<Button>("NewGameButton");
        quitButton = menuMount.Q<Button>("QuitButton");

        if (menuRoot != null)
        {
            menuRoot.pickingMode = PickingMode.Position;
            menuRoot.style.display = DisplayStyle.None;
        }

        if (logoImage != null)
        {
            logoImage.scaleMode = ScaleMode.ScaleToFit;
            logoImage.image = logoTexture;
        }

        ApplyButtonFont(newGameButton);
        ApplyButtonFont(quitButton);
        ApplyRuntimeVignette();

        if (newGameButton != null)
        {
            HexAudioUiBinder.BindButton(newGameButton, this);
            newGameButton.clicked += HandleNewGameClicked;
        }

        if (quitButton != null)
        {
            HexAudioUiBinder.BindButton(quitButton, this);
            quitButton.clicked += HandleQuitClicked;
        }

        hudDocumentController ??= GetComponent<HexHudDocumentController>() ?? FindAnyObjectByType<HexHudDocumentController>();
        transitionService ??= GetComponent<HexMainMenuTransitionService>() ?? gameObject.AddComponent<HexMainMenuTransitionService>();
        if (audioSystem == null)
        {
            audioSystem = HexAudioSystem.ResolveShared(this);
        }

        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Menu, false);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Menu, false);
        isInitialized = true;
        LogDebug($"Initialized Main Menu. rootFound={menuRoot != null} logoFound={logoTexture != null} newGameFound={newGameButton != null} quitFound={quitButton != null}.");
    }

    private void HandleNewGameClicked()
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        SetMenuInteractionEnabled(false);

        Action callback = newGameRequested;
        callback?.Invoke();
    }

    private void HandleQuitClicked()
    {
        if (isTransitioning)
        {
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetMenuVisibility(bool visible, bool showGameplayLayers)
    {
        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Hud, showGameplayLayers);
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Context, showGameplayLayers);
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Menu, visible);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Hud, false);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Context, false);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Menu, visible);
        }

        hudDocumentController ??= GetComponent<HexHudDocumentController>() ?? FindAnyObjectByType<HexHudDocumentController>();
        hudDocumentController?.SetGameplayModalState(visible);

        if (menuRoot != null)
        {
            menuRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (visible)
        {
            PlayMusic(HexMusicStage.MainMenu);
        }

        isOpen = visible;
        LogDebug($"SetMenuVisibility visible={visible} showGameplayLayers={showGameplayLayers}.");
    }

    public void SetMenuInteractionEnabled(bool enabled)
    {
        if (newGameButton != null)
        {
            newGameButton.SetEnabled(enabled);
            newGameButton.pickingMode = enabled ? PickingMode.Position : PickingMode.Ignore;
        }

        if (quitButton != null)
        {
            quitButton.SetEnabled(enabled);
            quitButton.pickingMode = enabled ? PickingMode.Position : PickingMode.Ignore;
        }
    }

    private void ApplyButtonFont(VisualElement element)
    {
        if (element == null || buttonFont == null)
        {
            return;
        }

        element.style.unityFont = new StyleFont(buttonFont);
        element.style.unityFontDefinition = FontDefinition.FromFont(buttonFont);
    }

    private void ApplyRuntimeVignette()
    {
        if (vignetteElement == null)
        {
            return;
        }

        Texture2D vignetteTexture = CreateVignetteTexture(256, 0.34f, 0.78f, 0.50f);
        vignetteElement.style.backgroundImage = new StyleBackground(vignetteTexture);
    }

    private static Texture2D CreateVignetteTexture(int size, float innerRadius, float outerRadius, float maxAlpha)
    {
        int resolvedSize = Mathf.Max(16, size);
        Texture2D texture = new(resolvedSize, resolvedSize, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.DontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color32[] pixels = new Color32[resolvedSize * resolvedSize];
        float center = (resolvedSize - 1) * 0.5f;
        float safeOuter = Mathf.Max(innerRadius + 0.001f, outerRadius);
        for (int y = 0; y < resolvedSize; y++)
        {
            for (int x = 0; x < resolvedSize; x++)
            {
                float normalizedX = (x - center) / center;
                float normalizedY = (y - center) / center;
                float distance = Mathf.Sqrt((normalizedX * normalizedX) + (normalizedY * normalizedY));
                float t = Mathf.InverseLerp(innerRadius, safeOuter, distance);
                t = t * t * (3f - (2f * t));
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(t * maxAlpha) * 255f);
                pixels[(y * resolvedSize) + x] = new Color32(0, 0, 0, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        return texture;
    }

    private static Font LoadRimouskiFont()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(RimouskiFontEditorAssetPath);
#else
        return null;
#endif
    }

    private static Texture2D LoadEditorLogoTexture()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(LogoEditorAssetPath);
#else
        return null;
#endif
    }

    private void LogDebug(string message)
    {
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this, createIfMissing: false);
        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.LogDiagnostic("MainMenu", message, this);
            return;
        }

        Debug.Log($"[GameplayUI:MainMenu] {message}", this);
    }

    private void PlayMusic(HexMusicStage stage)
    {
        if (audioSystem == null)
        {
            audioSystem = HexAudioSystem.ResolveShared(this);
        }

        if (audioSystem != null)
        {
            audioSystem.PlayMusic(stage);
        }
    }

}
