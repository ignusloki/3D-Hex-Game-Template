using System;

public sealed class HexRunUiReporter
{
    private HexHudPresenter hudPresenter;
    private PitstopEventController pitstopEventController;
    private HexRunEndModalPresenter runEndModalPresenter;
    private HexActTransitionModalPresenter actTransitionModalPresenter;

    public void Configure(
        HexHudPresenter hudPresenter,
        PitstopEventController pitstopEventController,
        HexRunEndModalPresenter runEndModalPresenter,
        HexActTransitionModalPresenter actTransitionModalPresenter)
    {
        this.hudPresenter = hudPresenter;
        this.pitstopEventController = pitstopEventController;
        this.runEndModalPresenter = runEndModalPresenter;
        this.actTransitionModalPresenter = actTransitionModalPresenter;
    }

    public bool PresentPitstopChoice(
        HexTurnResolutionResult turnResult,
        CaravanResourceState resources,
        Action<PitstopEventResult> onResolved)
    {
        if (turnResult?.PitstopEventResult == null
            || !turnResult.PitstopEventResult.RequiresChoice
            || pitstopEventController == null)
        {
            return false;
        }

        pitstopEventController.PresentChoice(turnResult.PitstopEventResult, resources, onResolved);
        return true;
    }

    public void ReportTurnResolved(
        HexTurnResolutionResult turnResult,
        HexagonTile currentTile,
        CaravanResourceSnapshot resources)
    {
        if (turnResult == null || hudPresenter == null)
        {
            return;
        }

        PitstopEventResult pitstopEventResult = turnResult.PitstopEventResult;
        HexObstacleTurnResult obstacleTurnResult = turnResult.ObstacleTurnResult;
        HexNemesisTurnResult nemesisTurnResult = turnResult.NemesisTurnResult;

        if (pitstopEventResult.Triggered || (pitstopEventResult.Site != null && pitstopEventResult.Site.Visited))
        {
            hudPresenter.ShowPitstopEvent(currentTile, pitstopEventResult, resources);
            ShowHintIfPresent(turnResult.PitstopBoonHint);
            return;
        }

        if (obstacleTurnResult.ContactResult.HasContact)
        {
            hudPresenter.ShowObstacleEncounter(currentTile, obstacleTurnResult.ContactResult, resources);
            ShowHintIfPresent(obstacleTurnResult.ContactPenaltyIgnoreNote);
            return;
        }

        if (nemesisTurnResult.Active
            && (nemesisTurnResult.Acted || nemesisTurnResult.DestroyedPitstops.Count > 0))
        {
            hudPresenter.ShowNemesisUpdate(currentTile, nemesisTurnResult);
            return;
        }

        hudPresenter.ShowMoveComplete(currentTile, turnResult.MoveCost, resources);
    }

    public void ReportPitstopChoiceResolved(
        HexagonTile currentTile,
        PitstopEventResult eventResult,
        CaravanResourceSnapshot resources,
        string boonHint)
    {
        if (hudPresenter == null)
        {
            return;
        }

        if (eventResult != null && eventResult.Site != null)
        {
            hudPresenter.ShowPitstopChoiceResolved(currentTile, eventResult, resources);
        }
        else
        {
            hudPresenter.ShowCaravanIdle(currentTile);
        }

        ShowHintIfPresent(boonHint);
    }

    public void ReportActTransition(
        HexActTransitionDisplayData displayData,
        int currentActNumber,
        Action<HexBoonDefinition> onContinueRequested)
    {
        hudPresenter?.ShowHint($"Act {currentActNumber} complete. Preparing the next crossing.");
        actTransitionModalPresenter?.ShowTransition(displayData, onContinueRequested);
    }

    public void ReportVictory(
        HexagonTile goalTile,
        CaravanResourceSnapshot resources,
        Action onContinueRequested)
    {
        hudPresenter?.ShowVictory(goalTile, resources);
        runEndModalPresenter?.ShowVictory(onContinueRequested);
    }

    public void ReportDefeat(
        HexagonTile currentTile,
        string defeatReason,
        Action onRetryRequested,
        Action onReturnToTitleRequested)
    {
        hudPresenter?.ShowDefeat(currentTile, defeatReason);
        runEndModalPresenter?.ShowDefeat(onRetryRequested, onReturnToTitleRequested);
    }

    private void ShowHintIfPresent(string hint)
    {
        if (!string.IsNullOrWhiteSpace(hint))
        {
            hudPresenter?.ShowHint(hint);
        }
    }
}
