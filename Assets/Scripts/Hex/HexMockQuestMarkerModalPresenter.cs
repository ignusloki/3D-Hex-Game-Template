using System;
using UnityEngine;

public sealed class HexMockQuestMarkerModalPresenter : MonoBehaviour
{
    private HexSimpleActionModalDocumentController documentController;
    private HexGameplayUiRootController gameplayUiRootController;

    public bool IsOpen => documentController != null && documentController.IsOpen;

    public void Show(string title, string body, Action onCloseRequested)
    {
        LogDebug($"Show title='{title}'.");
        EnsureView();
        documentController?.Show(
            string.IsNullOrWhiteSpace(title) ? "Quest Marker" : title.Trim(),
            string.IsNullOrWhiteSpace(body) ? "Mock quest marker." : body.Trim(),
            "Close",
            onCloseRequested);
    }

    public void Hide()
    {
        LogDebug("Hide requested.");
        documentController?.Hide();
    }

    private void EnsureView()
    {
        documentController ??= new HexSimpleActionModalDocumentController(
            this,
            "quest-marker-modal-mount",
            "QuestModal");
        documentController.EnsureInitialized();
    }

    internal void LogDebug(string message, bool verbose = false)
    {
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this, createIfMissing: false);
        gameplayUiRootController?.EnsureInitialized();
        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.LogDiagnostic("QuestModal", message, this, verbose);
            return;
        }

        Debug.Log($"[GameplayUI:QuestModal] {message}", this);
    }
}
