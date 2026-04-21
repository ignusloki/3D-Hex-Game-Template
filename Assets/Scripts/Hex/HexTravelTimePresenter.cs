using System.Collections.Generic;
using UnityEngine.UI;

public sealed class HexTravelTimePresenter
{
    private readonly Text travelTimeText;
    private readonly IHexTravelTimeView runtimeView;

    public HexTravelTimePresenter(Text travelTimeText)
    {
        this.travelTimeText = travelTimeText;
    }

    public HexTravelTimePresenter(Text travelTimeText, IHexTravelTimeView runtimeView)
    {
        this.travelTimeText = travelTimeText;
        this.runtimeView = runtimeView;
    }

    public HexTravelTimePresenter(IHexTravelTimeView runtimeView)
    {
        this.runtimeView = runtimeView;
    }

    public void ShowPath(IReadOnlyList<HexTileData> path)
    {
        if (travelTimeText == null && runtimeView == null)
        {
            return;
        }

        if (path == null || path.Count == 0)
        {
            Reset();
            return;
        }

        int totalTravelTime = HexPathMetrics.GetTravelCost(path);
        SetTravelTimeText("Travel Time: " + totalTravelTime + " days");
    }

    public void Reset()
    {
        SetTravelTimeText("Travel Time: --");
    }

    private void SetTravelTimeText(string value)
    {
        if (travelTimeText != null)
        {
            travelTimeText.text = value;
        }

        runtimeView?.SetTravelTimeText(value);
    }
}
