using System;
using UnityEngine;

public sealed class HexMainMenuTransitionService : MonoBehaviour
{
    private const string StartupFadeFromBlackProfileResourcePath = "UI/Transitions/MainMenuStartupFadeFromBlack";

    [SerializeField] private HexGlobalUiTransitionController globalTransitionController;
    [SerializeField] private HexUiTransitionProfile startupFadeFromBlackProfile;

    public bool PlayStartupToMainMenu(Action showMainMenu, Action onCompleted = null)
    {
        globalTransitionController ??= HexGlobalUiTransitionController.ResolveShared(this);
        if (globalTransitionController == null)
        {
            Debug.LogWarning("[MainMenuTransition] Could not play startup reveal because no global transition controller is available.", this);
            showMainMenu?.Invoke();
            onCompleted?.Invoke();
            return false;
        }

        startupFadeFromBlackProfile ??= Resources.Load<HexUiTransitionProfile>(StartupFadeFromBlackProfileResourcePath);
        globalTransitionController.EnsureInitialized();
        globalTransitionController.ShowBlackoutImmediate();
        showMainMenu?.Invoke();
        return globalTransitionController.PlayFadeFromBlack(onCompleted, startupFadeFromBlackProfile);
    }

    public bool PlayMainMenuToActOne(Action startActOne, Action onCompleted = null)
    {
        return PlayMenuFlowTransition("main-menu-to-act-one", startActOne, onCompleted);
    }

    public bool PlayReturnToMainMenu(Action returnToMainMenu, Action onCompleted = null)
    {
        return PlayMenuFlowTransition("return-to-main-menu", returnToMainMenu, onCompleted);
    }

    private bool PlayMenuFlowTransition(string flowName, Action whileBlack, Action onCompleted)
    {
        globalTransitionController ??= HexGlobalUiTransitionController.ResolveShared(this);
        if (globalTransitionController == null)
        {
            Debug.LogWarning($"[MainMenuTransition] Could not play '{flowName}' because no global transition controller is available.", this);
            whileBlack?.Invoke();
            onCompleted?.Invoke();
            return false;
        }

        globalTransitionController.EnsureInitialized();
        return globalTransitionController.PlayFadeToBlack(
            whileBlack,
            onCompleted,
            fadeBackOut: true);
    }
}
