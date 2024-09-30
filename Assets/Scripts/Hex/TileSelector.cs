/// Author: Mohammed Marzouq
/// Date: 19 Sep 2024
using UnityEngine;
using Pathing;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class TileSelector : MonoBehaviour
{
    // Public reference to the UI Text component to display travel time
    public Text travelTimeText;

    // Stores the starting tile for the path
    private HexagonTile startTile;
    // Stores the ending tile for the path
    private HexagonTile endTile;
    // Flag to check if the next tile to select is the start tile
    private bool selectingStart = true;
    // List to store the calculated path
    private IList<IAStarNode> path;

    // Update is called once per frame
    void Update()
    {
        DetectTileSelection();
    }

    // Detects tile selection based on mouse input
    private void DetectTileSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {
                HexagonTile tile = hit.collider.GetComponent<HexagonTile>();
                if (tile != null && tile.canTravelThrough) // Ensure the tile is traversable
                {
                    HandleTileSelection(tile);
                }
            }
        }
    }

    // Handles the tile selection logic for both start and end tiles
    private void HandleTileSelection(HexagonTile tile)
    {
        if (selectingStart)
        {
            ResetHighlightedTiles(startTile);
            startTile = tile;
            tile.SelectAsStartOrEnd();
            selectingStart = false;
        }
        else
        {
            endTile?.ResetMaterial();
            endTile = tile;
            tile.SelectAsStartOrEnd();
            selectingStart = true;
            FindAndHighlightPath();
        }
    }

    // Finds and highlights the path between selected tiles
    public void FindAndHighlightPath()
    {
        path = AStar.GetPath(startTile, endTile);
        if (path != null)
        {
            HighlightPath();
            UpdateTravelTimeText();
        }
        else
        {
            Debug.Log("No path found or one of the tiles is null.");
        }
    }

    // Highlights each tile in the path except the start and end tiles
    private void HighlightPath()
    {
        foreach (HexagonTile tile in path.Cast<HexagonTile>().Where(t => t != startTile && t != endTile))
        {
            tile.HighlightedRoad();
        }
    }

    // Updates the travel time text in the UI
    private void UpdateTravelTimeText()
    {
        int totalTravelTime = path.Cast<HexagonTile>().Sum(tile => tile.travelCost);
        travelTimeText.text = "Travel Time: " + totalTravelTime + " days";
    }

    // Resets the highlighted tiles and clears the path
    public void ResetHighlightedTiles(HexagonTile tile)
    {
        tile?.ResetMaterial();
        startTile = null;
        endTile = null;
        path?.ToList().ForEach(t => ((HexagonTile)t).ResetMaterial());
        path = null;
        travelTimeText.text = "Travel Time: 0 days";
    }
}
