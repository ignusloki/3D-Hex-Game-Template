using System.Collections.Generic;

public sealed class HexTravelTimePresenter
{
    private readonly IHexTravelTimeView runtimeView;

    public HexTravelTimePresenter(IHexTravelTimeView runtimeView)
    {
        this.runtimeView = runtimeView;
    }

    public void ShowPath(IReadOnlyList<HexTileData> path)
    {
        if (runtimeView == null)
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
        runtimeView?.SetTravelTimeText(value);
    }
}
