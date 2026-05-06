using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HexRunSessionController : MonoBehaviour
{
    private const string RuntimeObjectName = "HexRunSessionController";

    private static HexRunSessionController activeController;

    private readonly HexRunState runState = new();
    private bool hasActiveRunSession;

    public HexRunState RunState => runState;
    public bool HasActiveRunSession => hasActiveRunSession;
    public HexRunStateSnapshot CurrentSnapshot => runState.ToSnapshot();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        activeController = null;
    }

    public static HexRunSessionController Resolve(MonoBehaviour owner = null, bool createIfMissing = true)
    {
        if (activeController != null)
        {
            return activeController;
        }

        activeController = FindAnyObjectByType<HexRunSessionController>();
        if (activeController != null || !createIfMissing)
        {
            return activeController;
        }

        GameObject runtimeObject = new(RuntimeObjectName);
        activeController = runtimeObject.AddComponent<HexRunSessionController>();
        return activeController;
    }

    public static bool HasActiveSession()
    {
        return activeController != null && activeController.HasActiveRunSession;
    }

    public static void ResetActiveSession()
    {
        activeController?.ResetRun();
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

    public void StartRun(CaravanResourceSnapshot startingResources)
    {
        hasActiveRunSession = true;
        runState.InitializeSession(
            1,
            startingResources,
            HexNemesisArchetype.None,
            null,
            HexRunPhase.PreparingGameplay);
    }

    public void ConfigureRun(
        int currentActNumber,
        CaravanResourceSnapshot currentResources,
        HexNemesisArchetype lockedBoonFamily,
        IReadOnlyList<HexBoonDefinition> selectedBoons)
    {
        hasActiveRunSession = true;
        runState.InitializeSession(
            Mathf.Max(1, currentActNumber),
            currentResources,
            currentActNumber >= 2 ? lockedBoonFamily : HexNemesisArchetype.None,
            selectedBoons,
            HexRunPhase.PreparingGameplay);
    }

    public void ResetRun()
    {
        hasActiveRunSession = false;
        runState.InitializeSession(
            1,
            default,
            HexNemesisArchetype.None,
            null,
            HexRunPhase.MainMenu);
    }

    public CaravanResourceSnapshot GetStartingResources(CaravanResourceSnapshot defaultResources)
    {
        if (!hasActiveRunSession)
        {
            StartRun(defaultResources);
        }

        return runState.Resources;
    }

    public bool TryAdvanceToNextAct(CaravanResourceSnapshot currentResources, HexBoonDefinition selectedBoon)
    {
        if (!hasActiveRunSession)
        {
            StartRun(currentResources);
        }

        HexRunStateSnapshot snapshot = runState.ToSnapshot();
        if (!HexActTransitionService.TryBuildAdvanceResult(
                snapshot,
                currentResources,
                selectedBoon,
                out HexActTransitionAdvanceResult advanceResult))
        {
            return false;
        }

        hasActiveRunSession = true;
        runState.InitializeSession(
            advanceResult.NextActNumber,
            advanceResult.NextResources,
            advanceResult.LockedBoonFamily,
            advanceResult.SelectedBoons,
            HexRunPhase.PreparingGameplay);
        return true;
    }
}
