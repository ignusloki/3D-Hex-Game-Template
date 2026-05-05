public partial class PlayerController
{
    private void CommitMove(HexagonTile destinationTile, int moveCost)
    {
        runState.SetPhase(HexRunPhase.ResolvingMove);
        runState.SetPendingModalContext(string.Empty);
        ClearHighlights();
        HexCoordinates previousCoordinates = currentTile.Coordinates;

        currentTile.TileData?.SetOccupied(false);
        currentTile = destinationTile;
        currentTile.TileData?.SetOccupied(true);
        SyncRunStateCoordinates();
        caravanResources.Spend(CaravanResourceType.Food, moveCost);
        UpdateResourcesText();

        AttachCaravanToTile(currentTile);
        HexFogUpdateResult fogUpdate = RefreshFogOfWar();

        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;

        travelTimePresenter.Reset();
        if (nemesisController != null && nemesisController.TryResolveImmediateCaravanContact(currentTile.Coordinates, out string immediateContactDefeatReason))
        {
            RefreshTileDetails(currentTile);
            EndRunAsDefeat(immediateContactDefeatReason);
            return;
        }

        HexObstacleTurnResult obstacleTurnResult = ProcessObstacleTurn(fogUpdate);
        PlayGameplaySfx(obstacleTurnResult.ContactResult.HasContact ? HexSfxId.ObstacleTravel : HexSfxId.CaravanMove);
        HexNemesisTurnResult nemesisTurnResult = ProcessNemesisTurn(previousCoordinates, currentTile.Coordinates);
        if (nemesisTurnResult.Moved)
        {
            PlayGameplaySfx(HexSfxId.NemesisMove);
        }

        RefreshTileDetails(currentTile);

        if (nemesisTurnResult.CausedDefeat)
        {
            EndRunAsDefeat(nemesisTurnResult.DefeatReason);
            return;
        }

        if (goalTile != null && currentTile == goalTile)
        {
            EndRunAsVictory();
            return;
        }

        PitstopEventResult pitstopEventResult = ProcessPitstopArrival();
        string pitstopBoonHint = ProcessPitstopRecharge();
        bool hasDeferredNemesisPitstopDestruction = nemesisTurnResult.DeferredPitstopDestructions.Count > 0;

        if (caravanResources.IsDefeated)
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            EndRunAsDefeat(caravanResources.GetDefeatReason());
            return;
        }

        if (ShouldUseMockQuestMarkers
            && mockQuestMarkerController != null
            && mockQuestMarkerController.TryProcessArrival(currentTile.Coordinates))
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            runState.SetPhase(HexRunPhase.AwaitingPlayerInput);
            runState.SetPendingModalContext("QuestMarker");
            RefreshHighlights();
            return;
        }

        if (pitstopEventResult.RequiresChoice)
        {
            pendingPitstopBoonHint = pitstopBoonHint;
            pendingDeferredNemesisResult = hasDeferredNemesisPitstopDestruction ? nemesisTurnResult : null;
            runState.SetPhase(HexRunPhase.PitstopChoice);
            runState.SetPendingModalContext("Pitstop");
            pitstopEventController.PresentChoice(pitstopEventResult, caravanResources, HandlePitstopChoiceResolved);
        }
        else if (pitstopEventResult.Triggered || (pitstopEventResult.Site != null && pitstopEventResult.Site.Visited))
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            hudPresenter.ShowPitstopEvent(currentTile, pitstopEventResult, caravanResources.ToSnapshot());
            if (!string.IsNullOrWhiteSpace(pitstopBoonHint))
            {
                hudPresenter.ShowHint(pitstopBoonHint);
            }
        }
        else if (obstacleTurnResult.ContactResult.HasContact)
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            hudPresenter.ShowObstacleEncounter(currentTile, obstacleTurnResult.ContactResult, caravanResources.ToSnapshot());
            if (!string.IsNullOrWhiteSpace(obstacleTurnResult.ContactPenaltyIgnoreNote))
            {
                hudPresenter.ShowHint(obstacleTurnResult.ContactPenaltyIgnoreNote);
            }
        }
        else if (nemesisTurnResult.Active && (nemesisTurnResult.Acted || nemesisTurnResult.DestroyedPitstops.Count > 0))
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            hudPresenter.ShowNemesisUpdate(currentTile, nemesisTurnResult);
        }
        else
        {
            FinalizeDeferredNemesisPitstopDestructionIfNeeded(nemesisTurnResult);
            hudPresenter.ShowMoveComplete(currentTile, moveCost, caravanResources.ToSnapshot());
        }

        if (!isRunOver && runState.Phase == HexRunPhase.ResolvingMove)
        {
            runState.SetPhase(HexRunPhase.AwaitingPlayerInput);
            runState.SetPendingModalContext(string.Empty);
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

        if (eventResult != null && eventResult.Site != null)
        {
            hudPresenter.ShowPitstopChoiceResolved(currentTile, eventResult, caravanResources.ToSnapshot());
        }
        else
        {
            hudPresenter.ShowCaravanIdle(currentTile);
        }

        if (!string.IsNullOrWhiteSpace(pendingPitstopBoonHint))
        {
            hudPresenter.ShowHint(pendingPitstopBoonHint);
        }

        pendingPitstopBoonHint = string.Empty;
        SyncRunStateFromExistingModalState();
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

    private HexObstacleTurnResult ProcessObstacleTurn(HexFogUpdateResult fogUpdate)
    {
        if (obstacleController == null)
        {
            return HexObstacleTurnResult.Empty;
        }

        HexObstacleTurnResult turnResult = obstacleController.ProcessTurn(fogUpdate, currentTile.Coordinates, caravanResources.ToSnapshot());
        if (!turnResult.ContactResult.HasContact)
        {
            return turnResult;
        }

        if (boonRuntime != null && boonRuntime.TryConsumeObstacleIgnore(out HexBoonChargeChangeResult chargeChange))
        {
            turnResult.ContactPenaltyIgnored = true;
            turnResult.ContactPenaltyIgnoreNote = chargeChange.Message;
            UpdateResourcesText();
            return turnResult;
        }

        caravanResources.Spend(turnResult.ContactResult.AffectedResource, turnResult.ContactResult.AmountDrained);
        UpdateResourcesText();
        return turnResult;
    }

    private PitstopEventResult ProcessPitstopArrival()
    {
        if (pitstopEventController == null || currentTile == null)
        {
            return PitstopEventResult.Empty;
        }

        PitstopEventResult result = pitstopEventController.ProcessArrival(currentTile.Coordinates, caravanResources);
        if (result == null || !result.Triggered || !result.EffectsApplied)
        {
            return result ?? PitstopEventResult.Empty;
        }

        UpdateResourcesText();
        return result;
    }

    private string ProcessPitstopRecharge()
    {
        if (boonRuntime == null
            || currentTile == null
            || pitstopSpawner == null
            || !pitstopSpawner.TryGetPitstop(currentTile.Coordinates, out PitstopSite site)
            || site == null
            || site.IsDestroyed)
        {
            return string.Empty;
        }

        if (!boonRuntime.TryRecharge(HexBoonRechargeTrigger.PitstopArrival, out HexBoonChargeChangeResult chargeChange))
        {
            return string.Empty;
        }

        UpdateResourcesText();
        return chargeChange.Message;
    }

    private HexNemesisTurnResult ProcessNemesisTurn(HexCoordinates previousCoordinates, HexCoordinates currentCoordinates)
    {
        if (nemesisController == null)
        {
            return HexNemesisTurnResult.Empty;
        }

        return nemesisController.ProcessCaravanMove(previousCoordinates, currentCoordinates);
    }
}
