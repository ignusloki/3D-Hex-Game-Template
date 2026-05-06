using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class HexGameBootstrap : MonoBehaviour
{
    private enum BootDestination
    {
        MainMenu,
        Gameplay
    }

    [Header("Startup Resolution")]
    [SerializeField] private bool forceStartupResolution = true;
    [SerializeField] private Vector2Int targetResolution = new(1920, 1080);
    [SerializeField] private FullScreenMode startupFullScreenMode = FullScreenMode.FullScreenWindow;

    [Header("Boot Flow")]
    [SerializeField] private bool controlSceneBoot = true;
    [SerializeField] private bool enableBootDebugLogging;

    [Header("Return To Menu Input")]
    [SerializeField] private bool enableHoldReturnToMenu = true;
    [SerializeField, Min(0.1f)] private float returnToMenuHoldSeconds = 5f;
    [SerializeField] private bool enableReturnToMenuInputDebugLogging = true;

    private const KeyCode ReturnToMenuKey = KeyCode.R;

    [Header("Scene References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private PitstopSpawner pitstopSpawner;
    [SerializeField] private HexGameplayUiRootController gameplayUiRootController;
    [SerializeField] private HexGlobalUiTransitionController globalTransitionController;
    [SerializeField] private HexMainMenuPresenter mainMenuPresenter;
    [SerializeField] private HexMainMenuTransitionService mainMenuTransitionService;
    [SerializeField] private HexAudioSystem audioSystem;

    private static HexGameBootstrap activeBootstrap;
    private static BootDestination? requestedNextBootDestination;
    private bool isBooting;
    private bool isStartingGameplay;
    private bool isReturningToMenuFromInput;
    private float returnToMenuHoldTimer;
    private string lastReturnToMenuInputBlockReason = string.Empty;
    private bool didLogReturnToMenuKeyDown;

    public static bool HasActiveBootController => activeBootstrap != null && activeBootstrap.controlSceneBoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        activeBootstrap = null;
        requestedNextBootDestination = null;
    }

    public static void RequestGameplayOnNextSceneLoad()
    {
        requestedNextBootDestination = BootDestination.Gameplay;
    }

    public static void RequestMainMenuOnNextSceneLoad()
    {
        requestedNextBootDestination = BootDestination.MainMenu;
    }

    public static bool ConsumeGameplayOnNextSceneLoadRequest()
    {
        if (requestedNextBootDestination != BootDestination.Gameplay)
        {
            return false;
        }

        requestedNextBootDestination = null;
        return true;
    }

    public static void ReloadActiveSceneToGameplay(MonoBehaviour owner, bool resetRunSession)
    {
        ReloadActiveScene(owner, BootDestination.Gameplay, resetRunSession, playFadeToBlack: true);
    }

    public static void ReloadActiveSceneToMainMenu(MonoBehaviour owner, bool resetRunSession, bool playFadeToBlack = true)
    {
        ReloadActiveScene(owner, BootDestination.MainMenu, resetRunSession, playFadeToBlack);
    }

    public static void LoadActiveSceneToMainMenu(bool resetRunSession)
    {
        LoadActiveScene(BootDestination.MainMenu, resetRunSession);
    }

    private void Awake()
    {
        activeBootstrap = this;
        ApplyStartupResolution();
        ResolveReferences();
        PrepareBootBlackout();
    }

    private void OnValidate()
    {
        targetResolution = new Vector2Int(Mathf.Max(1, targetResolution.x), Mathf.Max(1, targetResolution.y));
        returnToMenuHoldSeconds = Mathf.Max(0.1f, returnToMenuHoldSeconds);
    }

    private IEnumerator Start()
    {
        if (!controlSceneBoot)
        {
            yield break;
        }

        if (isBooting)
        {
            yield break;
        }

        isBooting = true;
        BootDestination destination = ResolveBootDestination();

        PrepareBootBlackout();
        yield return WaitForMapReadiness();

        if (playerController != null)
        {
            yield return playerController.PrepareGameplaySessionForBoot();
        }

        if (destination == BootDestination.Gameplay)
        {
            RevealGameplayAfterBoot();
        }
        else
        {
            RevealMainMenuAfterBoot();
        }

        isBooting = false;
    }

    private void OnDestroy()
    {
        if (activeBootstrap == this)
        {
            activeBootstrap = null;
        }
    }

    private void Update()
    {
        ProcessReturnToMenuHoldInput();
    }

    private void ApplyStartupResolution()
    {
        if (!forceStartupResolution)
        {
            return;
        }

        int width = Mathf.Max(1, targetResolution.x);
        int height = Mathf.Max(1, targetResolution.y);
        Screen.SetResolution(width, height, startupFullScreenMode);
        LogDebug($"Applied startup resolution {width}x{height} mode={startupFullScreenMode}.");
    }

    private void ResolveReferences()
    {
        playerController ??= GetComponent<PlayerController>() ?? FindAnyObjectByType<PlayerController>();
        mapGenerator ??= FindAnyObjectByType<MapGenerator>();
        pitstopSpawner ??= FindAnyObjectByType<PitstopSpawner>();
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this);
        gameplayUiRootController?.EnsureInitialized();
        globalTransitionController ??= HexGlobalUiTransitionController.ResolveShared(this);
        globalTransitionController?.EnsureInitialized();
        if (audioSystem == null)
        {
            audioSystem = HexAudioSystem.ResolveShared(this);
        }

        if (audioSystem != null)
        {
            audioSystem.EnsureInitialized();
        }
        mainMenuPresenter ??= GetComponent<HexMainMenuPresenter>()
            ?? FindAnyObjectByType<HexMainMenuPresenter>()
            ?? gameObject.AddComponent<HexMainMenuPresenter>();
        mainMenuPresenter.InitializeForBootstrap();
        mainMenuTransitionService ??= GetComponent<HexMainMenuTransitionService>()
            ?? FindAnyObjectByType<HexMainMenuTransitionService>()
            ?? gameObject.AddComponent<HexMainMenuTransitionService>();
    }

    private void PrepareBootBlackout()
    {
        if (!controlSceneBoot)
        {
            return;
        }

        ResolveReferences();
        globalTransitionController?.ShowBlackoutImmediate();
        mainMenuPresenter?.SetMenuVisibleImmediate(false, showGameplayLayers: false);

        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Hud, false);
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Context, false);
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Modal, false);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Hud, false);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Context, false);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Modal, false);
        }
    }

    private BootDestination ResolveBootDestination()
    {
        if (requestedNextBootDestination.HasValue)
        {
            BootDestination destination = requestedNextBootDestination.Value;
            requestedNextBootDestination = null;
            return destination;
        }

        return HexActTransitionService.HasActiveRunSession()
            ? BootDestination.Gameplay
            : BootDestination.MainMenu;
    }

    private IEnumerator WaitForMapReadiness()
    {
        while (mapGenerator == null || mapGenerator.GridData == null)
        {
            ResolveReferences();
            yield return null;
        }

        while (pitstopSpawner != null && !pitstopSpawner.IsSpawnComplete)
        {
            ResolveReferences();
            yield return null;
        }
    }

    private void RevealMainMenuAfterBoot()
    {
        if (mainMenuPresenter == null)
        {
            RevealGameplayAfterBoot();
            return;
        }

        mainMenuPresenter.SetMenuInteractionEnabled(false);
        bool started = mainMenuTransitionService != null
            && mainMenuTransitionService.PlayStartupToMainMenu(
                () => mainMenuPresenter.ShowMenu(HandleNewGameRequested, showGameplayLayers: false, enableInteraction: false),
                () => mainMenuPresenter.SetMenuInteractionEnabled(true));

        if (!started)
        {
            mainMenuPresenter.ShowMenu(HandleNewGameRequested, showGameplayLayers: false, enableInteraction: true);
        }
    }

    private void RevealGameplayAfterBoot()
    {
        mainMenuPresenter?.HideForGameplay();
        playerController?.ActivateGameplaySession();

        if (globalTransitionController == null || !globalTransitionController.PlayFadeFromBlack())
        {
            globalTransitionController?.HideBlackoutImmediate();
        }
    }

    private void HandleNewGameRequested()
    {
        if (isStartingGameplay)
        {
            return;
        }

        isStartingGameplay = true;
        mainMenuPresenter?.SetMenuInteractionEnabled(false);
        bool started = mainMenuTransitionService != null
            && mainMenuTransitionService.PlayMainMenuToActOne(
                () =>
                {
                    mainMenuPresenter?.HideForGameplay();
                    playerController?.ActivateGameplaySession();
                },
                () => isStartingGameplay = false);

        if (!started)
        {
            mainMenuPresenter?.HideForGameplay();
            playerController?.ActivateGameplaySession();
            isStartingGameplay = false;
        }
    }

    private void ProcessReturnToMenuHoldInput()
    {
        if (Input.GetKeyDown(ReturnToMenuKey))
        {
            LogReturnToMenuInputDebug($"R key down detected. enabled={enableHoldReturnToMenu}, gameplayActive={playerController != null && playerController.IsGameplaySessionActive}, runOver={playerController != null && playerController.IsRunOver}, menuOpen={mainMenuPresenter != null && mainMenuPresenter.IsOpen}.");
        }

        if (!ShouldProcessReturnToMenuInput(out string blockReason))
        {
            LogReturnToMenuInputBlock(blockReason);
            ResetReturnToMenuHold();
            return;
        }

        if (!Input.GetKey(ReturnToMenuKey))
        {
            if (didLogReturnToMenuKeyDown)
            {
                LogReturnToMenuInputDebug($"key released before completion at {returnToMenuHoldTimer:0.00}/{returnToMenuHoldSeconds:0.00}s.");
            }

            ResetReturnToMenuHold();
            return;
        }

        if (!didLogReturnToMenuKeyDown)
        {
            didLogReturnToMenuKeyDown = true;
            lastReturnToMenuInputBlockReason = string.Empty;
            LogReturnToMenuInputDebug($"key hold started. key={ReturnToMenuKey}, requiredSeconds={returnToMenuHoldSeconds:0.00}.");
        }

        returnToMenuHoldTimer += Time.unscaledDeltaTime;
        if (returnToMenuHoldTimer < returnToMenuHoldSeconds)
        {
            return;
        }

        isReturningToMenuFromInput = true;
        ResetReturnToMenuHold();
        LogReturnToMenuInputDebug($"hold completed via key={ReturnToMenuKey}. Triggering main menu transition.");
        ReloadActiveSceneToMainMenu(this, resetRunSession: true);
    }

    private bool ShouldProcessReturnToMenuInput(out string blockReason)
    {
        if (!controlSceneBoot)
        {
            blockReason = "controlSceneBoot=false";
            return false;
        }

        if (!enableHoldReturnToMenu)
        {
            blockReason = "enableHoldReturnToMenu=false";
            return false;
        }

        if (isBooting)
        {
            blockReason = "isBooting=true";
            return false;
        }

        if (isStartingGameplay)
        {
            blockReason = "isStartingGameplay=true";
            return false;
        }

        if (isReturningToMenuFromInput)
        {
            blockReason = "isReturningToMenuFromInput=true";
            return false;
        }

        ResolveReturnToMenuInputReferences();
        if (playerController == null)
        {
            blockReason = "playerController=null";
            return false;
        }

        if (!playerController.IsGameplaySessionActive)
        {
            blockReason = "playerController.IsGameplaySessionActive=false";
            return false;
        }

        if (playerController.IsRunOver)
        {
            blockReason = "playerController.IsRunOver=true";
            return false;
        }

        if (mainMenuPresenter != null && mainMenuPresenter.IsOpen)
        {
            blockReason = "mainMenuPresenter.IsOpen=true";
            return false;
        }

        blockReason = string.Empty;
        return true;
    }

    private void ResetReturnToMenuHold()
    {
        returnToMenuHoldTimer = 0f;
        didLogReturnToMenuKeyDown = false;
    }

    private void ResolveReturnToMenuInputReferences()
    {
        playerController ??= GetComponent<PlayerController>() ?? FindAnyObjectByType<PlayerController>();
        mainMenuPresenter ??= GetComponent<HexMainMenuPresenter>() ?? FindAnyObjectByType<HexMainMenuPresenter>();
    }

    private void LogReturnToMenuInputBlock(string blockReason)
    {
        if (!Input.GetKey(ReturnToMenuKey))
        {
            lastReturnToMenuInputBlockReason = string.Empty;
            return;
        }

        if (string.Equals(lastReturnToMenuInputBlockReason, blockReason, System.StringComparison.Ordinal))
        {
            return;
        }

        lastReturnToMenuInputBlockReason = blockReason;
        LogReturnToMenuInputDebug($"key is held but input is blocked. key={ReturnToMenuKey}, reason={blockReason}.");
    }

    private void LogReturnToMenuInputDebug(string message)
    {
        if (!enableReturnToMenuInputDebugLogging)
        {
            return;
        }

        Debug.Log($"[ReturnToMenuInput] {message}", this);
    }

    private static void ReloadActiveScene(MonoBehaviour owner, BootDestination destination, bool resetRunSession, bool playFadeToBlack)
    {
        if (!playFadeToBlack)
        {
            LoadActiveScene(destination, resetRunSession);
            return;
        }

        bool sceneLoadRequested = false;
        void LoadSceneOnce()
        {
            if (sceneLoadRequested)
            {
                return;
            }

            sceneLoadRequested = true;
            LoadActiveScene(destination, resetRunSession);
        }

        MonoBehaviour transitionOwner = owner != null ? owner : activeBootstrap;
        HexGlobalUiTransitionController transitionController = HexGlobalUiTransitionController.ResolveShared(transitionOwner);
        transitionController?.EnsureInitialized();
        bool started = transitionController != null
            && transitionController.PlayFadeToBlack(
                LoadSceneOnce,
                null,
                fadeBackOut: false);
        if (!started)
        {
            LoadSceneOnce();
        }
    }

    private static void LoadActiveScene(BootDestination destination, bool resetRunSession)
    {
        if (resetRunSession)
        {
            HexActTransitionService.ResetRunSession();
        }

        requestedNextBootDestination = destination;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LogDebug(string message)
    {
        if (!enableBootDebugLogging)
        {
            return;
        }

        Debug.Log($"[GameBootstrap] {message}", this);
    }
}
