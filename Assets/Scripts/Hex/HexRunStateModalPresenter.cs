using System;
using UnityEngine;

public sealed class HexActTransitionModalPresenter : MonoBehaviour
{
    private HexActTransitionModalDocumentController documentController;
    private HexGameplayUiRootController gameplayUiRootController;

    public bool IsOpen => documentController != null && documentController.IsOpen;

    public void ShowTransition(string title, string body, string continueButtonLabel, Action onContinueRequested)
    {
        ShowTransition(
            new HexActTransitionDisplayData(
                title,
                body,
                string.Empty,
                string.Empty,
                continueButtonLabel,
                continueButtonLabel,
                Array.Empty<HexBoonDefinition>()),
            _ => onContinueRequested?.Invoke());
    }

    public void ShowTransition(HexActTransitionDisplayData displayData, Action<HexBoonDefinition> onContinueRequested)
    {
        EnsureView();
        documentController?.Show(displayData, onContinueRequested, openSelectionImmediately: false);
    }

    public void ShowTransitionSelection(HexActTransitionDisplayData displayData, Action<HexBoonDefinition> onContinueRequested)
    {
        EnsureView();
        documentController?.Show(displayData, onContinueRequested, openSelectionImmediately: true);
    }

    public void Hide()
    {
        documentController?.Hide();
    }

    private void EnsureView()
    {
        documentController ??= new HexActTransitionModalDocumentController(this);
        documentController.EnsureInitialized();
    }

    internal void LogDebug(string message, bool verbose = false)
    {
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this, createIfMissing: false);
        gameplayUiRootController?.EnsureInitialized();
        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.LogDiagnostic("TransitionModal", message, this, verbose);
            return;
        }

        Debug.Log($"[GameplayUI:TransitionModal] {message}", this);
    }
}
