public partial class PlayerController
{
    private void CommitMove(HexagonTile destinationTile, int moveCost)
    {
        SetRunPhase(HexRunPhase.ResolvingMove, string.Empty);
        ClearHighlights();
        HexTurnResolutionResult turnResult = turnResolver.ResolveMove(new HexTurnResolutionRequest(
            currentTile,
            destinationTile,
            goalTile,
            moveCost,
            caravanResources,
            BuildAlwaysKnownCoordinates(),
            fogOfWarController,
            obstacleController,
            nemesisController,
            pitstopEventController,
            pitstopSpawner,
            boonRuntime));

        currentTile = turnResult.CurrentTile;
        SyncRunStateCoordinates();
        if (turnResult.ResourcesChanged || turnResult.BoonStateChanged)
        {
            UpdateResourcesText();
        }

        AttachCaravanToTile(currentTile);

        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;

        travelTimePresenter.Reset();
        if (turnResult.DefeatSource == HexTurnDefeatSource.ImmediateNemesisContact)
        {
            RefreshTileDetails(currentTile);
            EndRunAsDefeat(turnResult.DefeatReason);
            return;
        }

        HexObstacleTurnResult obstacleTurnResult = turnResult.ObstacleTurnResult;
        PlayGameplaySfx(obstacleTurnResult.ContactResult.HasContact ? HexSfxId.ObstacleTravel : HexSfxId.CaravanMove);
        HexNemesisTurnResult nemesisTurnResult = turnResult.NemesisTurnResult;
        if (nemesisTurnResult.Moved)
        {
            PlayGameplaySfx(HexSfxId.NemesisMove);
        }

        RefreshTileDetails(currentTile);

        if (nemesisTurnResult.CausedDefeat)
        {
            EndRunAsDefeat(turnResult.DefeatReason);
            return;
        }

        if (turnResult.Outcome == HexTurnResolutionOutcome.GoalReached)
        {
            EndRunAsVictory();
            return;
        }

        if (turnResult.DefeatSource == HexTurnDefeatSource.Resources)
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            EndRunAsDefeat(turnResult.DefeatReason);
            return;
        }

        if (ShouldUseMockQuestMarkers
            && mockQuestMarkerController != null
            && mockQuestMarkerController.TryProcessArrival(currentTile.Coordinates))
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            SetRunPhase(HexRunPhase.AwaitingPlayerInput, "QuestMarker");
            RefreshHighlights();
            return;
        }

        if (turnResult.PitstopEventResult.RequiresChoice)
        {
            pendingPitstopBoonHint = turnResult.PitstopBoonHint;
            pendingDeferredNemesisResult = turnResult.HasDeferredNemesisPitstopDestruction ? nemesisTurnResult : null;
            SetRunPhase(HexRunPhase.PitstopChoice, "Pitstop");
            if (!runUiReporter.PresentPitstopChoice(turnResult, caravanResources, HandlePitstopChoiceResolved))
            {
                HandlePitstopChoiceResolved(turnResult.PitstopEventResult);
            }
            return;
        }

        FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
        runUiReporter.ReportTurnResolved(turnResult, currentTile, caravanResources.ToSnapshot());

        if (!isRunOver && runState.Phase == HexRunPhase.ResolvingMove)
        {
            SetRunPhase(HexRunPhase.AwaitingPlayerInput, string.Empty);
        }

        RefreshHighlights();
    }

    private void HandlePitstopChoiceResolved(PitstopEventResult eventResult)
    {
        FinalizeDeferredNemesisPitstopDestructionIfNeeded(pendingDeferredNemesisResult);
        pendingDeferredNemesisResult = null;

        UpdateResourcesText();
        RefreshTileDetails(currentTile);

        if (caravanResources.IsDefeated)
        {
            EndRunAsDefeat(caravanResources.GetDefeatReason());
            return;
        }

        runUiReporter.ReportPitstopChoiceResolved(
            currentTile,
            eventResult,
            caravanResources.ToSnapshot(),
            pendingPitstopBoonHint);

        pendingPitstopBoonHint = string.Empty;
        SyncGameFlowStateFromRuntimeState();
    }

    private void FinalizeDeferredNemesisPitstopDestructionIfNeeded(HexNemesisTurnResult turnResult)
    {
        if (turnResult == null || turnResult.DeferredPitstopDestructions.Count == 0 || nemesisController == null)
        {
            return;
        }

        nemesisController.FinalizeDeferredPitstopDestructions(turnResult);
        if (currentTile != null)
        {
            RefreshTileDetails(currentTile);
        }
    }

}
