using System.Collections.Generic;
using System.Text;

public enum HexMapPlacementReservationLayer
{
    StartGoal,
    TerrainLandmark,
    Pitstop,
    MapObject
}

public readonly struct HexMapPlacementReservation
{
    public HexMapPlacementReservation(HexCoordinates coordinates, HexMapPlacementReservationLayer layer, string sourceId)
    {
        Coordinates = coordinates;
        Layer = layer;
        SourceId = string.IsNullOrWhiteSpace(sourceId) ? layer.ToString() : sourceId.Trim();
    }

    public HexCoordinates Coordinates { get; }
    public HexMapPlacementReservationLayer Layer { get; }
    public string SourceId { get; }
}

public sealed class HexMapPlacementReservations
{
    private readonly Dictionary<HexCoordinates, List<HexMapPlacementReservation>> reservationsByCoordinate = new();

    public int Count
    {
        get
        {
            int count = 0;
            foreach (KeyValuePair<HexCoordinates, List<HexMapPlacementReservation>> pair in reservationsByCoordinate)
            {
                count += pair.Value.Count;
            }

            return count;
        }
    }

    public bool IsReserved(HexCoordinates coordinates)
    {
        return reservationsByCoordinate.TryGetValue(coordinates, out List<HexMapPlacementReservation> reservations)
            && reservations != null
            && reservations.Count > 0;
    }

    public bool IsReserved(HexCoordinates coordinates, HexMapPlacementReservationLayer layer)
    {
        if (!reservationsByCoordinate.TryGetValue(coordinates, out List<HexMapPlacementReservation> reservations))
        {
            return false;
        }

        for (int index = 0; index < reservations.Count; index++)
        {
            if (reservations[index].Layer == layer)
            {
                return true;
            }
        }

        return false;
    }

    public void Reserve(HexCoordinates coordinates, HexMapPlacementReservationLayer layer, string sourceId)
    {
        if (!reservationsByCoordinate.TryGetValue(coordinates, out List<HexMapPlacementReservation> reservations))
        {
            reservations = new List<HexMapPlacementReservation>();
            reservationsByCoordinate.Add(coordinates, reservations);
        }

        for (int index = 0; index < reservations.Count; index++)
        {
            if (reservations[index].Layer == layer && reservations[index].SourceId == sourceId)
            {
                return;
            }
        }

        reservations.Add(new HexMapPlacementReservation(coordinates, layer, sourceId));
    }

    public bool TryGetReservations(HexCoordinates coordinates, out IReadOnlyList<HexMapPlacementReservation> reservations)
    {
        if (reservationsByCoordinate.TryGetValue(coordinates, out List<HexMapPlacementReservation> list)
            && list != null
            && list.Count > 0)
        {
            reservations = list;
            return true;
        }

        reservations = null;
        return false;
    }

    public int CountReservations(HexMapPlacementReservationLayer layer)
    {
        int count = 0;
        foreach (KeyValuePair<HexCoordinates, List<HexMapPlacementReservation>> pair in reservationsByCoordinate)
        {
            List<HexMapPlacementReservation> reservations = pair.Value;
            for (int index = 0; index < reservations.Count; index++)
            {
                if (reservations[index].Layer == layer)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public string DescribeReservations(HexCoordinates coordinates)
    {
        if (!TryGetReservations(coordinates, out IReadOnlyList<HexMapPlacementReservation> reservations))
        {
            return "none";
        }

        StringBuilder summary = new();
        for (int index = 0; index < reservations.Count; index++)
        {
            if (index > 0)
            {
                summary.Append(", ");
            }

            summary.Append(reservations[index].Layer);
            summary.Append(':');
            summary.Append(reservations[index].SourceId);
        }

        return summary.ToString();
    }

    public HexMapPlacementReservations Clone()
    {
        HexMapPlacementReservations clone = new();
        foreach (KeyValuePair<HexCoordinates, List<HexMapPlacementReservation>> pair in reservationsByCoordinate)
        {
            for (int index = 0; index < pair.Value.Count; index++)
            {
                HexMapPlacementReservation reservation = pair.Value[index];
                clone.Reserve(reservation.Coordinates, reservation.Layer, reservation.SourceId);
            }
        }

        return clone;
    }
}

public sealed class HexBiomeMapResult
{
    public HexBiomeMapResult(
        Biome[,] biomeMap,
        HexCoordinates startCoordinates,
        HexCoordinates goalCoordinates,
        HexMapPlacementReservations placementReservations = null)
    {
        BiomeMap = biomeMap;
        StartCoordinates = startCoordinates;
        GoalCoordinates = goalCoordinates;
        PlacementReservations = placementReservations?.Clone() ?? new HexMapPlacementReservations();
    }

    public Biome[,] BiomeMap { get; }
    public HexCoordinates StartCoordinates { get; }
    public HexCoordinates GoalCoordinates { get; }
    public HexMapPlacementReservations PlacementReservations { get; }
}
