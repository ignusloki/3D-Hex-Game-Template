public partial class PlayerController
{
    public void ActivateGameplaySession()
    {
        isGameplaySessionActive = true;
        runState.ResetOutcome();
        SetRunPhase(HexRunPhase.AwaitingPlayerInput, string.Empty);
        SyncRunStateActContext();
        SyncRunStateCoordinates();
        hudDocumentController?.SetGameplayModalState(false);
        gameplayUiRootController?.SetLayerVisible(HexGameplayUiLayerId.Hud, true);
        gameplayUiRootController?.SetLayerVisible(HexGameplayUiLayerId.Context, true);
        gameplayUiRootController?.SetLayerInteractive(HexGameplayUiLayerId.Hud, false);
        gameplayUiRootController?.SetLayerInteractive(HexGameplayUiLayerId.Context, false);
        RefreshTileDetails(currentTile);
        hudPresenter.ShowCaravanIdle(currentTile);
        PlayCurrentActMusic();
    }

    private void EndRunAsVictory()
    {
        if (isRunOver)
        {
            return;
        }

        if (TryBeginActTransition())
        {
            return;
        }

        SyncRunStateActContext();
        runState.SetResources(caravanResources.ToSnapshot());
        runState.Complete(HexRunOutcome.Victory, "Victory");
        HexActTransitionService.ResetRunSession();
        isRunOver = true;
        ClearHighlights();
        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;
        travelTimePresenter.Reset();
        pitstopEventController?.HideActiveModal();
        HideMockQuestMarkerModal();
        RefreshTileDetails(currentTile);
        hudPresenter.ShowVictory(goalTile, caravanResources.ToSnapshot());
        runEndModalPresenter?.ShowVictory(ReturnToMainMenuAfterFade);
    }

    private bool TryBeginActTransition()
    {
        if (!HexActTransitionService.CanAdvanceFromCurrentAct())
        {
            return false;
        }

        HexActTransitionDisplayData displayData = HexActTransitionService.BuildTransitionDisplayData(caravanResources.ToSnapshot());
        PrepareForActTransitionModalState();
        hudPresenter.ShowHint($"Act {HexActTransitionService.GetCurrentActNumber()} complete. Preparing the next crossing.");
        actTransitionModalPresenter?.ShowTransition(displayData, ContinueToNextAct);
        return true;
    }

    private void EndRunAsDefeat(string defeatReason)
    {
        if (isRunOver)
        {
            return;
        }

        SyncRunStateActContext();
        runState.SetResources(caravanResources.ToSnapshot());
        runState.Complete(HexRunOutcome.Defeat, defeatReason);
        HexActTransitionService.ResetRunSession();
        isRunOver = true;
        ClearHighlights();
        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;
        travelTimePresenter.Reset();
        pitstopEventController?.HideActiveModal();
        HideMockQuestMarkerModal();
        RefreshTileDetails(currentTile);
        hudPresenter.ShowDefeat(currentTile, defeatReason);
        runEndModalPresenter?.ShowDefeat(RetryCurrentScene, ReturnToMainMenuWithFade);
    }

    private void PrepareForActTransitionModalState()
    {
        SetRunPhase(HexRunPhase.ActTransition, "ActTransition");
        runState.SetResources(caravanResources.ToSnapshot());
        SyncRunStateActContext();
        isRunOver = true;
        ClearHighlights();
        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;
        travelTimePresenter.Reset();
        pitstopEventController?.HideActiveModal();
        HideMockQuestMarkerModal();
        RefreshTileDetails(currentTile);
    }

    private void RevealGameplayAfterSceneLoad()
    {
        globalTransitionController ??= HexGlobalUiTransitionController.ResolveShared(this);
        globalTransitionController?.EnsureInitialized();
        if (globalTransitionController == null || !globalTransitionController.PlayFadeFromBlack())
        {
            globalTransitionController?.HideBlackoutImmediate();
        }
    }

    private void ReturnToMainMenuWithFade()
    {
        HexGameBootstrap.ReloadActiveSceneToMainMenu(this, resetRunSession: true);
    }

    private void ReturnToMainMenuAfterFade()
    {
        HexGameBootstrap.LoadActiveSceneToMainMenu(resetRunSession: true);
    }

    private void RetryCurrentScene()
    {
        HexGameBootstrap.ReloadActiveSceneToGameplay(this, resetRunSession: true);
    }

    private void ContinueToNextAct(HexBoonDefinition selectedBoon)
    {
        if (!HexActTransitionService.TryAdvanceToNextAct(caravanResources.ToSnapshot(), selectedBoon))
        {
            return;
        }

        SyncRunStateActContext();
        runState.SetResources(HexActTransitionService.GetStartingResources(caravanResources.ToSnapshot()));
        SetRunPhase(HexRunPhase.PreparingGameplay, "LoadingNextAct");
        LoadCurrentSceneBehindFade();
    }

    private void LoadCurrentSceneBehindFade()
    {
        HexGameBootstrap.ReloadActiveSceneToGameplay(this, resetRunSession: false);
    }
}
