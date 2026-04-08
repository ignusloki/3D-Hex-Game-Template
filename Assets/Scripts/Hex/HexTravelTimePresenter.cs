using System.Collections.Generic;
using UnityEngine.UI;

public sealed class HexTravelTimePresenter
{
    private readonly Text travelTimeText;

    public HexTravelTimePresenter(Text travelTimeText)
    {
        this.travelTimeText = travelTimeText;
    }

    public void ShowPath(IReadOnlyList<HexTileData> path)
    {
        if (travelTimeText == null)
        {
            return;
        }

        if (path == null || path.Count == 0)
        {
            Reset();
            return;
        }

        int totalTravelTime = HexPathMetrics.GetTravelCost(path);
        travelTimeText.text = "Travel Time: " + totalTravelTime + " days";
    }

    public void Reset()
    {
        if (travelTimeText == null)
        {
            return;
        }

        travelTimeText.text = "Travel Time: --";
    }
}
