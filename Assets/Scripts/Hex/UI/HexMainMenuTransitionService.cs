using System;
using UnityEngine;

public sealed class HexMainMenuTransitionService : MonoBehaviour
{
    [SerializeField] private HexGlobalUiTransitionController globalTransitionController;

    public bool PlayStartupToMainMenu(Action showMainMenu, Action onCompleted = null)
    {
        return PlayMenuFlowTransition("startup-to-main-menu", showMainMenu, onCompleted);
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
