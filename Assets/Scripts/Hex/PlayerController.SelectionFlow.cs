using System.Collections.Generic;
using UnityEngine;

public partial class PlayerController
{
    private void Update()
    {
        EnsureRuntimeReferences();

        if (!CanHandleGameplayInput())
        {
            return;
        }

        if (inputService.TryGetClickedTile(Camera.main, Input.mousePosition, out HexagonTile tile))
        {
            LogUiDiagnostic("TileDetails", $"Tile click detected at {FormatCoordinates(tile.Coordinates)}.");
            HandleTileClick(tile);
        }
    }

    private bool CanHandleGameplayInput()
    {
        return isReady
            && isGameplaySessionActive
            && !(mainMenuPresenter != null && mainMenuPresenter.IsOpen)
            && !isRunOver
            && !(runEndModalPresenter != null && runEndModalPresenter.IsOpen)
            && !(actTransitionModalPresenter != null && actTransitionModalPresenter.IsOpen)
            && !(pitstopEventController != null && pitstopEventController.IsChoiceModalOpen)
            && !IsMockQuestMarkerModalOpen();
    }

    private void HandleTileClick(HexagonTile clickedTile)
    {
        if (clickedTile == null || !(clickedTile.TileData?.IsPassable ?? clickedTile.canTravelThrough))
        {
            LogUiDiagnostic("TileDetails", "Ignored tile click because the tile was null or not passable.");
            return;
        }

        LogUiDiagnostic(
            "TileDetails",
            $"HandleTileClick tile={FormatCoordinates(clickedTile.Coordinates)} current={(currentTile != null ? FormatCoordinates(currentTile.Coordinates) : "none")} selected={(selectedTile != null ? FormatCoordinates(selectedTile.Coordinates) : "none")} caravanSelectionActive={caravanSelectionActive}.");
        PlayGameplaySfx(ResolveTileSelectionSfx(clickedTile));

        if (selectedTile == clickedTile)
        {
            LogUiDiagnostic("TileDetails", $"Repeated tile selection at {FormatCoordinates(clickedTile.Coordinates)}.");
            HandleRepeatedSelection(clickedTile);
            return;
        }

        ClearHighlights();

        if (clickedTile == currentTile)
        {
            selectedTile = clickedTile;
            RefreshTileDetails(clickedTile);
            caravanSelectionActive = true;
            previewPath = null;
            travelTimePresenter.Reset();
            hudPresenter.ShowCaravanSelected(currentTile);
            RefreshHighlights();
            return;
        }

        selectedTile = clickedTile;
        RefreshTileDetails(clickedTile);

        if (!caravanSelectionActive)
        {
            previewPath = null;
            travelTimePresenter.Reset();
            hudPresenter.ShowInspectingTile(clickedTile);
            RefreshHighlights();
            return;
        }

        if (!IsWithinImmediateMovementRange(clickedTile))
        {
            previewPath = null;
            travelTimePresenter.Reset();
            hudPresenter.ShowOutOfRangeDestination(clickedTile);
            RefreshHighlights();
            return;
        }

        if (nemesisController != null && nemesisController.IsBlockedDestination(clickedTile.Coordinates))
        {
            previewPath = null;
            travelTimePresenter.Reset();
            hudPresenter.ShowNemesisBlockedDestination(clickedTile);
            RefreshHighlights();
            return;
        }

        previewPath = mapGenerator.FindPath(currentTile.Coordinates, clickedTile.Coordinates);
        if (previewPath == null)
        {
            travelTimePresenter.Reset();
            hudPresenter.ShowUnreachableDestination(clickedTile);
        }
        else
        {
            travelTimePresenter.ShowPath(previewPath);
            int moveCost = GetMoveCostForSelection(clickedTile, previewPath);
            if (moveCost > caravanResources.Food)
            {
                hudPresenter.ShowInsufficientResources(clickedTile, moveCost, caravanResources.Food);
            }
            else
            {
                hudPresenter.ShowDestinationPreview(clickedTile, previewPath);
            }
        }

        RefreshHighlights();
    }

    private void HandleRepeatedSelection(HexagonTile clickedTile)
    {
        if (clickedTile == currentTile)
        {
            ClearSelection();
            return;
        }

        if (caravanSelectionActive && !IsWithinImmediateMovementRange(clickedTile))
        {
            travelTimePresenter.Reset();
            hudPresenter.ShowOutOfRangeDestination(clickedTile);
            return;
        }

        if (caravanSelectionActive && nemesisController != null && nemesisController.IsBlockedDestination(clickedTile.Coordinates))
        {
            travelTimePresenter.Reset();
            hudPresenter.ShowNemesisBlockedDestination(clickedTile);
            return;
        }

        if (!caravanSelectionActive || previewPath == null)
        {
            hudPresenter.ShowInspectingTile(clickedTile);
            return;
        }

        int moveCost = GetMoveCostForSelection(clickedTile, previewPath);
        CommitMove(clickedTile, moveCost);
    }

    private bool IsWithinImmediateMovementRange(HexagonTile tile)
    {
        return tile != null && currentTile != null && currentTile.Coordinates.DistanceTo(tile.Coordinates) <= 1;
    }

    private int GetMoveCostForSelection(HexagonTile destinationTile, IReadOnlyList<HexTileData> path)
    {
        if (destinationTile == null)
        {
            return 0;
        }

        // Reaching the exit is treated as a special end-of-map move rather than a normal terrain-cost tile.
        if (goalTile != null && destinationTile == goalTile)
        {
            return 1;
        }

        return HexPathMetrics.GetTravelCost(path);
    }

    private HexSfxId ResolveTileSelectionSfx(HexagonTile tile)
    {
        return HasVisibleObstacle(tile) ? HexSfxId.ObstacleSelect : HexSfxId.HexSelect;
    }

    private bool HasVisibleObstacle(HexagonTile tile)
    {
        if (tile == null)
        {
            return false;
        }

        obstacleController ??= FindAnyObjectByType<HexObstacleController>();
        return obstacleController != null
            && obstacleController.TryGetVisibleObstacle(tile.Coordinates, out HexObstacleInstance obstacle)
            && obstacle != null;
    }
}
