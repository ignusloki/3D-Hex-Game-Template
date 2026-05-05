using System;
using UnityEngine;

public sealed class HexRunEndModalPresenter : MonoBehaviour
{
    private HexSimpleActionModalDocumentController documentController;
    private HexVictoryOverlayDocumentController victoryDocumentController;
    private HexGameOverOverlayDocumentController gameOverDocumentController;
    private HexGameplayUiRootController gameplayUiRootController;
    private HexAudioSystem audioSystem;

    public bool IsOpen =>
        (documentController != null && documentController.IsOpen)
        || (victoryDocumentController != null && victoryDocumentController.IsOpen)
        || (gameOverDocumentController != null && gameOverDocumentController.IsOpen);

    public void ShowVictory(Action onContinueRequested)
    {
        LogDebug("ShowVictory requested.");
        documentController?.Hide();
        gameOverDocumentController?.Hide();
        PlayMusic(HexMusicStage.Victory);
        PlaySfx(HexSfxId.Victory);
        EnsureVictoryView();
        victoryDocumentController?.Show(onContinueRequested);
    }

    public void ShowDefeat(Action onRetryRequested, Action onReturnToTitleRequested = null)
    {
        LogDebug("ShowDefeat requested.");
        documentController?.Hide();
        victoryDocumentController?.Hide();
        PlayMusic(HexMusicStage.Defeat);
        EnsureGameOverView();
        gameOverDocumentController?.Show(onRetryRequested, onReturnToTitleRequested);
    }

    public void Hide()
    {
        LogDebug("Hide requested.");
        documentController?.Hide();
        victoryDocumentController?.Hide();
        gameOverDocumentController?.Hide();
    }

    private void EnsureView()
    {
        documentController ??= new HexSimpleActionModalDocumentController(
            this,
            "run-end-modal-mount",
            "RunEndModal");
        documentController.EnsureInitialized();
    }

    private void EnsureGameOverView()
    {
        gameOverDocumentController ??= new HexGameOverOverlayDocumentController(this);
        gameOverDocumentController.EnsureInitialized();
    }

    private void EnsureVictoryView()
    {
        victoryDocumentController ??= new HexVictoryOverlayDocumentController(this);
        victoryDocumentController.EnsureInitialized();
    }

    internal void LogDebug(string message, bool verbose = false)
    {
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this, createIfMissing: false);
        gameplayUiRootController?.EnsureInitialized();
        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.LogDiagnostic("RunEndModal", message, this, verbose);
            return;
        }

        Debug.Log($"[GameplayUI:RunEndModal] {message}", this);
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

    private void PlaySfx(HexSfxId id)
    {
        if (audioSystem == null)
        {
            audioSystem = HexAudioSystem.ResolveShared(this);
        }

        if (audioSystem != null)
        {
            audioSystem.PlaySfx(id);
        }
    }
}
