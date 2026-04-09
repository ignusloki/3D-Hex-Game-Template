using UnityEngine;

public sealed class PitstopSite : MonoBehaviour
{
    [field: SerializeField] public PitstopKind Kind { get; private set; }
    [field: SerializeField] public int Row { get; private set; }
    [field: SerializeField] public int Column { get; private set; }
    [field: SerializeField] public bool Visited { get; private set; }
    [field: SerializeField] public bool HasRefuelPoint { get; private set; } = true;
    [field: SerializeField] public string SpecialEventDescription { get; private set; } = "placeholder";

    public HexCoordinates Coordinates => new(Row, Column);

    public void Initialize(PitstopKind kind, HexCoordinates coordinates)
    {
        Kind = kind;
        Row = coordinates.Row;
        Column = coordinates.Column;
        Visited = false;
        HasRefuelPoint = true;
        SpecialEventDescription = "placeholder";
    }

    public void MarkVisited()
    {
        Visited = true;
    }
}
