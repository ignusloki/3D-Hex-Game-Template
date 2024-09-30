/// Author: Mohammed Marzouq
/// Date: 19 Sep 2024
using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public GameObject[] tilePrefabs; // Array of tile prefabs to instantiate
    public int Rows = 5; // Number of rows in the grid
    public int Columns = 5; // Number of columns in the grid
    public float XTileSpacing = 1f; // Horizontal space between tiles
    public float YTileSpacing = 0.75f; // Vertical space between tiles

    private HexagonTile[,] tiles; // 2D array to hold the instantiated tiles

    // Start is called before the first frame update
    void Start()
    {
        GenerateTilesAndAssignNeighbors();
    }

    // Generates hexagonal tiles and assigns their neighbors
    private void GenerateTilesAndAssignNeighbors()
    {
        tiles = new HexagonTile[Rows, Columns];
        float rowOffset = 0;

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                Vector3 spawnPosition = CalculateTilePosition(row, col, rowOffset);
                HexagonTile tile = InstantiateTile(spawnPosition);
                tiles[row, col] = tile;
            }
            rowOffset += YTileSpacing;
        }

        AssignNeighbors();
    }

    // Calculates the position of a tile based on its row and column
    private Vector3 CalculateTilePosition(int row, int col, float rowOffset)
    {
        float posX = col + XTileSpacing;
        float posY = rowOffset;

        if (row % 2 == 1) // Adjust for hexagonal staggering
        {
            posX += XTileSpacing / 2;
        }

        return new Vector3(posX, 0, posY);
    }

    // Instantiates a tile prefab at the given position
    private HexagonTile InstantiateTile(Vector3 position)
    {
        GameObject tileObject = Instantiate(tilePrefabs[Random.Range(0, tilePrefabs.Length)], position, Quaternion.Euler(0, 90, 0));
        tileObject.transform.SetParent(transform);
        return tileObject.GetComponent<HexagonTile>();
    }

    // Assigns neighbors to each tile based on hexagonal grid logic
    private void AssignNeighbors()
    {
        for (int x = 0; x < Rows; x++)
        {
            for (int y = 0; y < Columns; y++)
            {
                HexagonTile tile = tiles[x, y];
                List<Vector2Int> neighborCoords = GetNeighborCoordinates(x, y);
                AddValidNeighbors(tile, neighborCoords);
            }
        }
    }

    // Returns a list of potential neighbor coordinates based on even or odd row logic
    private List<Vector2Int> GetNeighborCoordinates(int x, int y)
    {
        bool evenRow = x % 2 == 0;
        return new List<Vector2Int>
        {
            new Vector2Int(x, y + 1),
            new Vector2Int(x, y - 1),
            new Vector2Int(x - 1, evenRow ? y : y + 1),
            new Vector2Int(x + 1, evenRow ? y : y + 1),
            new Vector2Int(x - 1, evenRow ? y - 1 : y),
            new Vector2Int(x + 1, evenRow ? y - 1 : y)
        };
    }

    // Adds valid neighbors to a tile, ensuring they can be traveled through
    private void AddValidNeighbors(HexagonTile tile, List<Vector2Int> neighborCoords)
    {
        foreach (Vector2Int coord in neighborCoords)
        {
            if (coord.x >= 0 && coord.x < Rows && coord.y >= 0 && coord.y < Columns && tiles[coord.x, coord.y].canTravelThrough)
            {
                tile.neighbors.Add(tiles[coord.x, coord.y]);
            }
        }
    }
}
