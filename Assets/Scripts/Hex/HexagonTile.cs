/// Author: Mohammed Marzouq
/// Date: 19 Sep 2024
using UnityEngine;
using Pathing;
using System.Collections.Generic;

[RequireComponent(typeof(Renderer))]
public class HexagonTile : MonoBehaviour, IAStarNode {
    public int travelCost; // Cost to travel through this tile
    public bool canTravelThrough = true; // Whether the tile can be traversed

    // Materials used for different states of the tiles
    private Material originalMaterial; // Original material of the tile
    private Material highlightMaterial; // Material used for highlighting tiles
    private Renderer tileRenderer;

    // List of neighboring tiles
    public List<HexagonTile> neighbors = new();

    // Provides a collection of neighboring tiles cast to IAStarNode
    public IEnumerable<IAStarNode> Neighbours => neighbors;
    [SerializeField] private HexScriptableObject properties;
    [SerializeField] private int row;
    [SerializeField] private int column;

    public HexCoordinates Coordinates => new(row, column);
    public HexTileData TileData { get; private set; }

    private void Awake() {
        tileRenderer = GetComponent<Renderer>();
        originalMaterial = tileRenderer.material; // Fetch and store the original material on creation
        ApplyProperties();
    }

    private void OnValidate()
    {
        neighbors ??= new List<HexagonTile>();

        if (properties != null)
        {
            travelCost = properties.travelCost;
            canTravelThrough = properties.passable;
        }
    }

    public void Initialize(HexTileData tileData)
    {
        TileData = tileData;
        properties = tileData?.Properties;
        row = tileData?.Coordinates.Row ?? 0;
        column = tileData?.Coordinates.Column ?? 0;
        ApplyProperties();
    }

    public void ClearNeighbors()
    {
        neighbors.Clear();
    }

    private void ApplyProperties()
    {
        if (properties == null)
        {
            travelCost = TileData?.TravelCost ?? 0;
            canTravelThrough = TileData?.IsPassable ?? true;
            return;
        }

        travelCost = TileData?.TravelCost ?? properties.travelCost;
        canTravelThrough = TileData?.IsPassable ?? properties.passable;
    }

    // Method to highlight the road on the path
    public void HighlightedRoad()
    {
        SetTileMaterial(new Color(1f, 0.25f, 0f, 1f));
    }


    // Mark this tile as start or end
    public void SelectAsStartOrEnd()
    {
        SetTileMaterial(new Color(0f, 1f, 0f, 1f));
    }

    // Helper method to set material properties based on the provided color
    private void SetTileMaterial(Color color)
    {
        highlightMaterial = Instantiate(originalMaterial);
        highlightMaterial.SetColor("_EmissionColor", color);
        highlightMaterial.EnableKeyword("_EMISSION");
        tileRenderer.material = highlightMaterial;
    }

    // Resets the tile's material to its original state
    public void ResetMaterial()
    {
        tileRenderer.material = originalMaterial;
    }

    public float EstimatedCostTo(IAStarNode other)
    {
        return Coordinates.DistanceTo(((HexagonTile)other).Coordinates);
    }

    public float CostTo(IAStarNode next)
    {
        HexagonTile nextTile = (HexagonTile)next;
        return nextTile.TileData?.TravelCost ?? nextTile.travelCost;
    }

}
