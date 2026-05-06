using UnityEngine;

[DisallowMultipleComponent]
public sealed class HexGameFlowController : MonoBehaviour
{
    private const string RuntimeObjectName = "HexGameFlowController";

    private static HexGameFlowController activeController;

    private HexRunSessionController runSessionController;
    private HexMainMenuPresenter mainMenuPresenter;
    private HexRunEndModalPresenter runEndModalPresenter;
    private HexActTransitionModalPresenter actTransitionModalPresenter;
    private PitstopEventController pitstopEventController;
    private HexMockQuestMarkerController mockQuestMarkerController;
    private HexGlobalUiTransitionController globalTransitionController;

    public HexRunPhase CurrentPhase => ResolveRunState()?.Phase ?? HexRunPhase.Booting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        activeController = null;
    }

    public static HexGameFlowController Resolve(MonoBehaviour owner = null, bool createIfMissing = true)
    {
        if (activeController != null)
        {
            return activeController;
        }

        activeController = FindAnyObjectByType<HexGameFlowController>();
        if (activeController != null || !createIfMissing)
        {
            return activeController;
        }

        HexRunSessionController sessionController = HexRunSessionController.Resolve(owner, createIfMissing);
        if (sessionController != null)
        {
            activeController = sessionController.GetComponent<HexGameFlowController>()
                ?? sessionController.gameObject.AddComponent<HexGameFlowController>();
            return activeController;
        }

        GameObject runtimeObject = new(RuntimeObjectName);
        activeController = runtimeObject.AddComponent<HexGameFlowController>();
        return activeController;
    }

    private void Awake()
    {
        if (activeController != null && activeController != this)
        {
            Destroy(gameObject);
            return;
        }

        activeController = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (activeController == this)
        {
            activeController = null;
        }
    }

    public void ConfigureSceneReferences(
        HexMainMenuPresenter mainMenuPresenter,
        HexRunEndModalPresenter runEndModalPresenter,
        HexActTransitionModalPresenter actTransitionModalPresenter,
        PitstopEventController pitstopEventController,
        HexMockQuestMarkerController mockQuestMarkerController,
        HexGlobalUiTransitionController globalTransitionController)
    {
        this.mainMenuPresenter = mainMenuPresenter;
        this.runEndModalPresenter = runEndModalPresenter;
        this.actTransitionModalPresenter = actTransitionModalPresenter;
        this.pitstopEventController = pitstopEventController;
        this.mockQuestMarkerController = mockQuestMarkerController;
        this.globalTransitionController = globalTransitionController;
    }

    public void SetPhase(HexRunPhase phase, string pendingModalContext = null)
    {
        HexRunState runState = ResolveRunState();
        if (runState == null)
        {
            return;
        }

        runState.SetPhase(phase);
        if (pendingModalContext != null)
        {
            runState.SetPendingModalContext(pendingModalContext);
        }
    }

    public HexRunPhase SyncPhaseFromRuntimeState(bool isReady, bool isGameplaySessionActive, bool isRunOver)
    {
        HexRunState runState = ResolveRunState();
        if (runState == null)
        {
            return HexRunPhase.Booting;
        }

        EnsureSceneReferences();

        if (!isReady)
        {
            SetPhase(HexRunPhase.PreparingGameplay, string.Empty);
            return runState.Phase;
        }

        if (IsMainMenuOpen())
        {
            SetPhase(HexRunPhase.MainMenu, "MainMenu");
            return runState.Phase;
        }

        if (!isGameplaySessionActive)
        {
            SetPhase(HexRunPhase.MainMenu, string.Empty);
            return runState.Phase;
        }

        if (IsGlobalTransitionBlocking())
        {
            SetPhase(HexRunPhase.PreparingGameplay, "GlobalTransition");
            return runState.Phase;
        }

        if (IsRunEndModalOpen())
        {
            if (runState.Outcome == HexRunOutcome.Victory)
            {
                SetPhase(HexRunPhase.Victory, "Victory");
            }
            else if (runState.Outcome == HexRunOutcome.Defeat)
            {
                SetPhase(HexRunPhase.Defeat, "Defeat");
            }
            else
            {
                runState.SetPendingModalContext("RunEnd");
            }

            return runState.Phase;
        }

        if (IsActTransitionModalOpen())
        {
            SetPhase(HexRunPhase.ActTransition, "ActTransition");
            return runState.Phase;
        }

        if (IsPitstopModalOpen())
        {
            SetPhase(HexRunPhase.PitstopChoice, "Pitstop");
            return runState.Phase;
        }

        if (IsQuestModalOpen())
        {
            if (runState.Phase == HexRunPhase.ResolvingMove)
            {
                runState.SetPhase(HexRunPhase.AwaitingPlayerInput);
            }

            runState.SetPendingModalContext("QuestMarker");
            return runState.Phase;
        }

        if (isRunOver || runState.HasFinished)
        {
            return runState.Phase;
        }

        SetPhase(HexRunPhase.AwaitingPlayerInput, string.Empty);
        return runState.Phase;
    }

    public bool CanHandleGameplayInput(bool isReady, bool isGameplaySessionActive, bool isRunOver)
    {
        HexRunPhase phase = SyncPhaseFromRuntimeState(isReady, isGameplaySessionActive, isRunOver);
        return isReady
            && isGameplaySessionActive
            && !isRunOver
            && phase == HexRunPhase.AwaitingPlayerInput
            && !HasBlockingModalOpen();
    }

    public bool IsCameraInputBlocked()
    {
        EnsureSceneReferences();
        HexRunPhase phase = CurrentPhase;
        return IsGlobalTransitionBlocking()
            || IsMainMenuOpen()
            || HasBlockingModalOpen()
            || phase == HexRunPhase.MainMenu
            || phase == HexRunPhase.PreparingGameplay
            || phase == HexRunPhase.PitstopChoice
            || phase == HexRunPhase.ActTransition
            || phase == HexRunPhase.Victory
            || phase == HexRunPhase.Defeat;
    }

    private HexRunState ResolveRunState()
    {
        if (runSessionController == null)
        {
            runSessionController = HexRunSessionController.Resolve(this);
        }

        return runSessionController != null ? runSessionController.RunState : null;
    }

    private void EnsureSceneReferences()
    {
        if (mainMenuPresenter == null)
        {
            mainMenuPresenter = FindAnyObjectByType<HexMainMenuPresenter>();
        }

        if (runEndModalPresenter == null)
        {
            runEndModalPresenter = FindAnyObjectByType<HexRunEndModalPresenter>();
        }

        if (actTransitionModalPresenter == null)
        {
            actTransitionModalPresenter = FindAnyObjectByType<HexActTransitionModalPresenter>();
        }

        if (pitstopEventController == null)
        {
            pitstopEventController = FindAnyObjectByType<PitstopEventController>();
        }

        if (globalTransitionController == null)
        {
            globalTransitionController = HexGlobalUiTransitionController.ResolveShared(this, createIfMissing: false);
        }

        if (HexDevelopmentContentGate.AllowsDevelopmentOnlyContent)
        {
            if (mockQuestMarkerController == null)
            {
                mockQuestMarkerController = FindAnyObjectByType<HexMockQuestMarkerController>();
            }
        }
        else
        {
            mockQuestMarkerController = null;
        }
    }

    private bool HasBlockingModalOpen()
    {
        return IsRunEndModalOpen()
            || IsActTransitionModalOpen()
            || IsPitstopModalOpen()
            || IsQuestModalOpen();
    }

    private bool IsMainMenuOpen()
    {
        return mainMenuPresenter != null && mainMenuPresenter.IsOpen;
    }

    private bool IsRunEndModalOpen()
    {
        return runEndModalPresenter != null && runEndModalPresenter.IsOpen;
    }

    private bool IsActTransitionModalOpen()
    {
        return actTransitionModalPresenter != null && actTransitionModalPresenter.IsOpen;
    }

    private bool IsPitstopModalOpen()
    {
        return pitstopEventController != null && pitstopEventController.IsChoiceModalOpen;
    }

    private bool IsQuestModalOpen()
    {
        return HexDevelopmentContentGate.AllowsDevelopmentOnlyContent
            && mockQuestMarkerController != null
            && mockQuestMarkerController.IsModalOpen;
    }

    private bool IsGlobalTransitionBlocking()
    {
        return globalTransitionController != null && globalTransitionController.IsTransitioning;
    }
}
