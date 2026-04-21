public interface IHexHudView
{
    void SetStatusText(string value);
    void SetTileDetailsText(string value);
    void SetHintText(string value);
    void SetPitstopInfoText(string value);
}

public interface IHexTravelTimeView
{
    void SetTravelTimeText(string value);
}
