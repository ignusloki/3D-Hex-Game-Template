using System;

public readonly struct HexCoordinates : IEquatable<HexCoordinates>
{
    public int Row { get; }
    public int Column { get; }

    public HexCoordinates(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public bool Equals(HexCoordinates other)
    {
        return Row == other.Row && Column == other.Column;
    }

    public override bool Equals(object obj)
    {
        return obj is HexCoordinates other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Row, Column);
    }

    public override string ToString()
    {
        return $"{Row}_{Column}";
    }

    public int DistanceTo(HexCoordinates other)
    {
        (int x, int y, int z) = ToCube();
        (int otherX, int otherY, int otherZ) = other.ToCube();

        return (Math.Abs(x - otherX) + Math.Abs(y - otherY) + Math.Abs(z - otherZ)) / 2;
    }

    private (int x, int y, int z) ToCube()
    {
        int x = Column - ((Row - (Row & 1)) / 2);
        int z = Row;
        int y = -x - z;
        return (x, y, z);
    }
}
