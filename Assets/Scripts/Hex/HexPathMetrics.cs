using System.Collections.Generic;

public static class HexPathMetrics
{
    public static int GetTravelCost(IReadOnlyList<HexTileData> path)
    {
        if (path == null || path.Count <= 1)
        {
            return 0;
        }

        int totalTravelCost = 0;
        for (int i = 1; i < path.Count; i++)
        {
            totalTravelCost += path[i].TravelCost;
        }

        return totalTravelCost;
    }
}
