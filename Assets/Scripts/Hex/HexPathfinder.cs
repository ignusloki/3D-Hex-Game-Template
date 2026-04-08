using System.Collections.Generic;

public sealed class HexPathfinder
{
    private readonly HexGridData gridData;

    public HexPathfinder(HexGridData gridData)
    {
        this.gridData = gridData;
    }

    public IReadOnlyList<HexTileData> FindPath(HexCoordinates start, HexCoordinates goal)
    {
        if (!TryGetTraversableEndpoints(start, goal, out HexTileData startTile, out HexTileData goalTile))
        {
            return null;
        }

        HexPriorityQueue<HexCoordinates> openSet = new();
        Dictionary<HexCoordinates, HexCoordinates> cameFrom = new();
        Dictionary<HexCoordinates, int> gScore = new()
        {
            [start] = 0
        };

        openSet.Enqueue(start, 0f);

        while (openSet.Count > 0)
        {
            (HexCoordinates current, float priority) = openSet.Dequeue();
            float expectedPriority = gScore[current] + current.DistanceTo(goal);
            if (priority > expectedPriority)
            {
                continue;
            }

            if (current.Equals(goal))
            {
                return ReconstructPath(startTile, goalTile, cameFrom);
            }

            foreach (HexTileData neighbor in gridData.GetNeighbors(current))
            {
                if (!CanTraverse(neighbor, goal))
                {
                    continue;
                }

                int tentativeScore = gScore[current] + neighbor.TravelCost;
                if (gScore.TryGetValue(neighbor.Coordinates, out int existingScore) && tentativeScore >= existingScore)
                {
                    continue;
                }

                cameFrom[neighbor.Coordinates] = current;
                gScore[neighbor.Coordinates] = tentativeScore;
                float estimatedPriority = tentativeScore + neighbor.Coordinates.DistanceTo(goal);
                openSet.Enqueue(neighbor.Coordinates, estimatedPriority);
            }
        }

        return null;
    }

    public IReadOnlyList<HexTileData> GetReachableTiles(HexCoordinates start, int movementBudget)
    {
        if (!gridData.TryGetTile(start, out HexTileData startTile) || !startTile.IsPassable)
        {
            return new List<HexTileData>();
        }

        HexPriorityQueue<HexCoordinates> frontier = new();
        Dictionary<HexCoordinates, int> costs = new()
        {
            [start] = 0
        };

        frontier.Enqueue(start, 0f);

        while (frontier.Count > 0)
        {
            (HexCoordinates current, float priority) = frontier.Dequeue();
            if (priority > costs[current])
            {
                continue;
            }

            foreach (HexTileData neighbor in gridData.GetNeighbors(current))
            {
                if (!neighbor.IsAvailable)
                {
                    continue;
                }

                int travelCost = costs[current] + neighbor.TravelCost;
                if (travelCost > movementBudget)
                {
                    continue;
                }

                if (costs.TryGetValue(neighbor.Coordinates, out int existingCost) && travelCost >= existingCost)
                {
                    continue;
                }

                costs[neighbor.Coordinates] = travelCost;
                frontier.Enqueue(neighbor.Coordinates, travelCost);
            }
        }

        List<HexTileData> reachableTiles = new();
        foreach (KeyValuePair<HexCoordinates, int> entry in costs)
        {
            HexCoordinates coordinates = entry.Key;
            if (coordinates.Equals(start))
            {
                continue;
            }

            if (gridData.TryGetTile(coordinates, out HexTileData tile))
            {
                reachableTiles.Add(tile);
            }
        }

        return reachableTiles;
    }

    private bool TryGetTraversableEndpoints(HexCoordinates start, HexCoordinates goal, out HexTileData startTile, out HexTileData goalTile)
    {
        startTile = null;
        goalTile = null;

        if (!gridData.TryGetTile(start, out startTile) || !gridData.TryGetTile(goal, out goalTile))
        {
            return false;
        }

        return startTile.IsPassable && goalTile.IsPassable;
    }

    private bool CanTraverse(HexTileData tile, HexCoordinates goal)
    {
        return tile.IsPassable && (tile.IsAvailable || tile.Coordinates.Equals(goal));
    }

    private IReadOnlyList<HexTileData> ReconstructPath(HexTileData startTile, HexTileData goalTile, Dictionary<HexCoordinates, HexCoordinates> cameFrom)
    {
        List<HexTileData> path = new();
        HexTileData current = goalTile;
        path.Add(current);

        while (!current.Coordinates.Equals(startTile.Coordinates))
        {
            if (!cameFrom.TryGetValue(current.Coordinates, out HexCoordinates previousCoordinates))
            {
                return null;
            }

            if (!gridData.TryGetTile(previousCoordinates, out current))
            {
                return null;
            }

            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
