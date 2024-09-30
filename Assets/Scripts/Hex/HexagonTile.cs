/// Author: Mohammed Marzouq
/// Date: 19 Sep 2024
using UnityEngine;
using Pathing;
using System.Collections.Generic;
using System.Linq;

public class HexagonTile : MonoBehaviour, IAStarNode
{
    public int travelCost; // Cost to travel through this tile
    public bool canTravelThrough = true; // Whether the tile can be traversed

    // Materials used for different states of the tiles
    private Material originalMaterial; // Original material of the tile
    private Material highlightMaterial; // Material used for highlighting tiles

    // List of neighboring tiles
    public List<HexagonTile> neighbors;

    // Provides a collection of neighboring tiles cast to IAStarNode
    public IEnumerable<IAStarNode> Neighbours => neighbors.Cast<IAStarNode>();

    void Start()
    {
        originalMaterial = GetComponent<Renderer>().material; // Fetch and store the original material on start
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
        GetComponent<Renderer>().material = highlightMaterial;
    }

    // Resets the tile's material to its original state
    public void ResetMaterial()
    {
        GetComponent<Renderer>().material = originalMaterial;
    }

    public float EstimatedCostTo(IAStarNode other)
    {
        // Implement your heuristic here (e.g., Euclidean distance, Manhattan distance)
        return Vector3.Distance(this.transform.position, ((HexagonTile)other).transform.position);
    }

    public float CostTo(IAStarNode next)
    {
        // Return the cost based on your game’s logic, e.g., terrain difficulty
        return ((HexagonTile)next).travelCost;
    }

}

