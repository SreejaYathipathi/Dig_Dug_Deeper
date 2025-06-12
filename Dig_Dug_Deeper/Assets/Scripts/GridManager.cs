using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 12;   // 192 / 16
    public int height = 10;  // 160 / 16
    public float tileSize = 1f;

    public GameObject[] dirtLayerPrefabs;
    public GameObject rockPrefab;

    private GridCell[,] grid;

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        grid = new GridCell[width, height];

        // Shift to center grid based on total size
        Vector2 offset = new Vector2(-(width * tileSize) / 2f + tileSize / 2f,
                                     -(height * tileSize) / 2f + tileSize / 2f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                Vector2 position = new Vector2(x * tileSize, y * tileSize) + offset;
                //GameObject tile = Instantiate(dirtPrefab, position, Quaternion.identity, transform);

                //int layerIndex = Mathf.FloorToInt((float)y / height * dirtLayerPrefabs.Length);

                int layerIndex = Mathf.FloorToInt((float)(height - 1 - y) / height * dirtLayerPrefabs.Length);
                layerIndex = Mathf.Clamp(layerIndex, 0, dirtLayerPrefabs.Length - 1);

                // Spawn correct dirt prefab
                GameObject tile = Instantiate(dirtLayerPrefabs[layerIndex], position, Quaternion.identity, transform);

                GridCell cell = tile.GetComponent<GridCell>();
                cell.Init(x, y, TileType.Dirt);

                grid[x, y] = cell;
            }
        }

        // Example: center rock in grid
        int rx = width / 2;
        int ry = height / 2;
        Vector2 rockPos = new Vector2(rx * tileSize, ry * tileSize) + offset;
        GameObject rock = Instantiate(rockPrefab, rockPos, Quaternion.identity, transform);
        grid[rx, ry] = rock.GetComponent<GridCell>();
        grid[rx, ry].Init(rx, ry, TileType.Rock);
    }

    public GridCell GetCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return null;
        return grid[x, y];
    }
}
