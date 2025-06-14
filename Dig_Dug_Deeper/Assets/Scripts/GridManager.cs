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

    public GameObject pookaPrefab;
    public GameObject fygarPrefab;

    private GridCell[,] grid;

    void Start()
    {
        GenerateGrid();

        SpawnEnemy(pookaPrefab, width / 2 - 2, height / 2 + 1);
        SpawnEnemy(fygarPrefab, width / 2 + 2, height / 2 + 1);
    }

    void GenerateGrid()
    {
        grid = new GridCell[width, height];

        // Shift to center grid based on total size
        Vector2 offset = new Vector2(-(width * tileSize) / 2f + tileSize / 2f,
                                     -(height * tileSize) / 2f + tileSize / 2f);
        int rockX = width / 2;
        int rockY = height / 2;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {

                if (x == rockX && y == rockY)
                    continue;

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
        Vector2 rockPos = new Vector2(rockX * tileSize, rockY * tileSize) + offset;
        GameObject rock = Instantiate(rockPrefab, rockPos, Quaternion.identity, transform);
        GridCell rockCell = rock.GetComponent<GridCell>();
        rockCell.Init(rockX, rockY, TileType.Rock);
        grid[rockX, rockY] = rockCell;
    }

    void SpawnEnemy(GameObject enemyPrefab, int x, int y)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError($"Enemy prefab is not assigned for position ({x}, {y})!");
            return;
        }

        GameObject enemy = Instantiate(enemyPrefab, GetWorldPosition(x, y), Quaternion.identity);
        EnemyController controller = enemy.GetComponent<EnemyController>();

        if (controller == null)
        {
            Debug.LogError("Spawned enemy is missing EnemyController!");
            return;
        }

        controller.Init(new Vector2Int(x, y));
    }

    public GridCell GetCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return null;
        return grid[x, y];
    }

    public void ClearCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        grid[x, y] = null;
    }

    public void SetCell(int x, int y, GridCell cell)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        grid[x, y] = cell;
    }

    public Vector2 GetWorldPosition(int x, int y)
    {
        Vector2 offset = new Vector2(
            -(width * tileSize) / 2f + tileSize / 2f,
            -(height * tileSize) / 2f + tileSize / 2f
        );

        return new Vector2(x * tileSize, y * tileSize) + offset;
    }
}
