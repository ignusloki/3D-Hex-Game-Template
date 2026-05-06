using UnityEngine;

public sealed class PitstopSite : MonoBehaviour
{
    [field: SerializeField] public PitstopKind Kind { get; private set; }
    [field: SerializeField] public int Row { get; private set; }
    [field: SerializeField] public int Column { get; private set; }
    [field: SerializeField] public bool IsDestroyed { get; private set; }
    [field: SerializeField] public bool Visited { get; private set; }
    [field: SerializeField] public int VisitCount { get; private set; }
    [field: SerializeField] public string EventTitle { get; private set; } = "Pitstop";
    [field: SerializeField] public bool HasRefuelPoint { get; private set; } = true;
    [field: SerializeField] public bool Repeatable { get; private set; }
    [field: SerializeField] public string SpecialEventDescription { get; private set; } = string.Empty;

    public HexCoordinates Coordinates => new(Row, Column);

    public void Initialize(PitstopKind kind, HexCoordinates coordinates)
    {
        Kind = kind;
        Row = coordinates.Row;
        Column = coordinates.Column;
        IsDestroyed = false;
        Visited = false;
        VisitCount = 0;
        ConfigureEventMetadata(kind.ToString(), string.Empty, true, false);
    }

    public void MarkVisited()
    {
        RegisterVisit();
    }

    public void RegisterVisit()
    {
        Visited = true;
        VisitCount++;
    }

    public void ConfigureEventMetadata(string title, string description, bool hasRefuelPoint, bool repeatable)
    {
        EventTitle = string.IsNullOrWhiteSpace(title) ? Kind.ToString() : title;
        SpecialEventDescription = string.IsNullOrWhiteSpace(description) ? string.Empty : description;
        HasRefuelPoint = hasRefuelPoint;
        Repeatable = repeatable;
    }

    public void MarkDestroyed(string destroyedDescription, Color destroyedTint)
    {
        if (IsDestroyed)
        {
            return;
        }

        IsDestroyed = true;
        HasRefuelPoint = false;
        Repeatable = false;
        SpecialEventDescription = string.IsNullOrWhiteSpace(destroyedDescription)
            ? "This stop has been ruined."
            : destroyedDescription;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.material.color = destroyedTint;
        }
    }
}
