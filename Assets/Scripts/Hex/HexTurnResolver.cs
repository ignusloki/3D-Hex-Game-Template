using System.Collections.Generic;

public enum HexTurnResolutionOutcome
{
    None,
    Resolved,
    PitstopChoice,
    GoalReached,
    Defeat
}

public enum HexTurnDefeatSource
{
    None,
    ImmediateNemesisContact,
    NemesisTurn,
    Resources
}

public readonly struct HexTurnResolutionRequest
{
    public HexTurnResolutionRequest(
        HexagonTile currentTile,
        HexagonTile destinationTile,
        HexagonTile goalTile,
        int moveCost,
        CaravanResourceState resources,
        IEnumerable<HexCoordinates> alwaysKnownCoordinates,
        HexFogOfWarController fogOfWarController,
        HexObstacleController obstacleController,
        HexNemesisController nemesisController,
        PitstopEventController pitstopEventController,
        PitstopSpawner pitstopSpawner,
        HexBoonRuntimeState boonRuntime)
    {
        CurrentTile = currentTile;
        DestinationTile = destinationTile;
        GoalTile = goalTile;
        MoveCost = moveCost;
        Resources = resources;
        AlwaysKnownCoordinates = alwaysKnownCoordinates;
        FogOfWarController = fogOfWarController;
        ObstacleController = obstacleController;
        NemesisController = nemesisController;
        PitstopEventController = pitstopEventController;
        PitstopSpawner = pitstopSpawner;
        BoonRuntime = boonRuntime;
    }

    public HexagonTile CurrentTile { get; }
    public HexagonTile DestinationTile { get; }
    public HexagonTile GoalTile { get; }
    public int MoveCost { get; }
    public CaravanResourceState Resources { get; }
    public IEnumerable<HexCoordinates> AlwaysKnownCoordinates { get; }
    public HexFogOfWarController FogOfWarController { get; }
    public HexObstacleController ObstacleController { get; }
    public HexNemesisController NemesisController { get; }
    public PitstopEventController PitstopEventController { get; }
    public PitstopSpawner PitstopSpawner { get; }
    public HexBoonRuntimeState BoonRuntime { get; }
}

public sealed class HexTurnResolutionResult
{
    public HexagonTile PreviousTile { get; set; }
    public HexagonTile CurrentTile { get; set; }
    public HexCoordinates PreviousCoordinates { get; set; }
    public HexCoordinates CurrentCoordinates { get; set; }
    public int MoveCost { get; set; }
    public HexFogUpdateResult FogUpdate { get; set; } = HexFogUpdateResult.Empty;
    public HexObstacleTurnResult ObstacleTurnResult { get; set; } = HexObstacleTurnResult.Empty;
    public HexNemesisTurnResult NemesisTurnResult { get; set; } = HexNemesisTurnResult.Empty;
    public PitstopEventResult PitstopEventResult { get; set; } = PitstopEventResult.Empty;
    public HexTurnResolutionOutcome Outcome { get; set; } = HexTurnResolutionOutcome.None;
    public HexTurnDefeatSource DefeatSource { get; set; } = HexTurnDefeatSource.None;
    public string DefeatReason { get; set; } = string.Empty;
    public string PitstopBoonHint { get; set; } = string.Empty;
    public bool ResourcesChanged { get; set; }
    public bool BoonStateChanged { get; set; }
    public bool Moved => PreviousTile != null && CurrentTile != null && PreviousTile != CurrentTile;
    public bool HasDeferredNemesisPitstopDestruction => NemesisTurnResult?.DeferredPitstopDestructions.Count > 0;
}

public sealed class HexTurnResolver
{
    public HexTurnResolutionResult ResolveMove(HexTurnResolutionRequest request)
    {
        HexTurnResolutionResult result = new()
        {
            PreviousTile = request.CurrentTile,
            CurrentTile = request.DestinationTile,
            MoveCost = request.MoveCost
        };

        if (request.CurrentTile == null || request.DestinationTile == null || request.Resources == null)
        {
            result.CurrentTile = request.CurrentTile;
            return result;
        }

        result.PreviousCoordinates = request.CurrentTile.Coordinates;
        result.CurrentCoordinates = request.DestinationTile.Coordinates;

        request.CurrentTile.TileData?.SetOccupied(false);
        request.DestinationTile.TileData?.SetOccupied(true);
        request.Resources.Spend(CaravanResourceType.Food, request.MoveCost);
        result.ResourcesChanged = true;

        result.FogUpdate = RefreshFogOfWar(request);

        if (request.NemesisController != null
            && request.NemesisController.TryResolveImmediateCaravanContact(
                result.CurrentCoordinates,
                out string immediateContactDefeatReason))
        {
            result.Outcome = HexTurnResolutionOutcome.Defeat;
            result.DefeatSource = HexTurnDefeatSource.ImmediateNemesisContact;
            result.DefeatReason = immediateContactDefeatReason;
            return result;
        }

        result.ObstacleTurnResult = ProcessObstacleTurn(request, result.FogUpdate, result);
        result.NemesisTurnResult = ProcessNemesisTurn(request, result.PreviousCoordinates, result.CurrentCoordinates);

        if (result.NemesisTurnResult.CausedDefeat)
        {
            result.Outcome = HexTurnResolutionOutcome.Defeat;
            result.DefeatSource = HexTurnDefeatSource.NemesisTurn;
            result.DefeatReason = result.NemesisTurnResult.DefeatReason;
            return result;
        }

        if (request.GoalTile != null && request.DestinationTile == request.GoalTile)
        {
            result.Outcome = HexTurnResolutionOutcome.GoalReached;
            return result;
        }

        result.PitstopEventResult = ProcessPitstopArrival(request, result);
        result.PitstopBoonHint = ProcessPitstopRecharge(request, result);

        if (request.Resources.IsDefeated)
        {
            result.Outcome = HexTurnResolutionOutcome.Defeat;
            result.DefeatSource = HexTurnDefeatSource.Resources;
            result.DefeatReason = request.Resources.GetDefeatReason();
            return result;
        }

        result.Outcome = result.PitstopEventResult.RequiresChoice
            ? HexTurnResolutionOutcome.PitstopChoice
            : HexTurnResolutionOutcome.Resolved;
        return result;
    }

    private static HexFogUpdateResult RefreshFogOfWar(HexTurnResolutionRequest request)
    {
        if (request.FogOfWarController == null)
        {
            return HexFogUpdateResult.Empty;
        }

        return request.FogOfWarController.RefreshVisibility(
            request.DestinationTile.Coordinates,
            request.AlwaysKnownCoordinates);
    }

    private static HexObstacleTurnResult ProcessObstacleTurn(
        HexTurnResolutionRequest request,
        HexFogUpdateResult fogUpdate,
        HexTurnResolutionResult resolutionResult)
    {
        if (request.ObstacleController == null)
        {
            return HexObstacleTurnResult.Empty;
        }

        HexObstacleTurnResult turnResult = request.ObstacleController.ProcessTurn(
            fogUpdate,
            request.DestinationTile.Coordinates,
            request.Resources.ToSnapshot());
        if (!turnResult.ContactResult.HasContact)
        {
            return turnResult;
        }

        if (request.BoonRuntime != null
            && request.BoonRuntime.TryConsumeObstacleIgnore(out HexBoonChargeChangeResult chargeChange))
        {
            turnResult.ContactPenaltyIgnored = true;
            turnResult.ContactPenaltyIgnoreNote = chargeChange.Message;
            resolutionResult.BoonStateChanged = true;
            return turnResult;
        }

        request.Resources.Spend(
            turnResult.ContactResult.AffectedResource,
            turnResult.ContactResult.AmountDrained);
        resolutionResult.ResourcesChanged = true;
        return turnResult;
    }

    private static HexNemesisTurnResult ProcessNemesisTurn(
        HexTurnResolutionRequest request,
        HexCoordinates previousCoordinates,
        HexCoordinates currentCoordinates)
    {
        if (request.NemesisController == null)
        {
            return HexNemesisTurnResult.Empty;
        }

        return request.NemesisController.ProcessCaravanMove(previousCoordinates, currentCoordinates);
    }

    private static PitstopEventResult ProcessPitstopArrival(
        HexTurnResolutionRequest request,
        HexTurnResolutionResult resolutionResult)
    {
        if (request.PitstopEventController == null)
        {
            return PitstopEventResult.Empty;
        }

        PitstopEventResult result = request.PitstopEventController.ProcessArrival(
            request.DestinationTile.Coordinates,
            request.Resources);
        if (result == null || !result.Triggered || !result.EffectsApplied)
        {
            return result ?? PitstopEventResult.Empty;
        }

        resolutionResult.ResourcesChanged = true;
        return result;
    }

    private static string ProcessPitstopRecharge(
        HexTurnResolutionRequest request,
        HexTurnResolutionResult resolutionResult)
    {
        if (request.BoonRuntime == null
            || request.PitstopSpawner == null
            || !request.PitstopSpawner.TryGetPitstop(request.DestinationTile.Coordinates, out PitstopSite site)
            || site == null
            || site.IsDestroyed)
        {
            return string.Empty;
        }

        if (!request.BoonRuntime.TryRecharge(HexBoonRechargeTrigger.PitstopArrival, out HexBoonChargeChangeResult chargeChange))
        {
            return string.Empty;
        }

        resolutionResult.BoonStateChanged = true;
        return chargeChange.Message;
    }
}
