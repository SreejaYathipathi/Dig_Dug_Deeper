using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public float tileSize = 1f;

    public GameObject[] dirtLayerPrefabs;
    public GameObject rockPrefab;
    public GameObject playerPrefab;
    public GameObject pookaPrefab;
    public GameObject fygarPrefab;

    public TextAsset levelFile; // assign .txt file in inspector

    private GridCell[,] grid;
    public int width;
    public int height;

    public void LoadLevelFromText(TextAsset file)
    {
        if (file == null)
        {
            Debug.LogError("No level file assigned!");
            return;
        }

        else
        {
            Debug.Log($"Level file loaded with {file.text.Length} characters.");
        }

        string[] rawLines = file.text.Split('\n');
        List<string> lines = new List<string>();

        foreach (var line in rawLines)
        {
            string trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                lines.Add(trimmed);
        }

        height = lines.Count;
        width = lines[0].Length;

        grid = new GridCell[width, height];

        Vector2 offset = new Vector2(-(width * tileSize) / 2f + tileSize / 2f, -(height * tileSize) / 2f + tileSize / 2f);

        for (int y = 0; y < height; y++)
        {
            string line = lines[y].Trim();
            int gridY = height - 1 - y;
            //string line = lines[height - 1 - y].Trim();

            for (int x = 0; x < width; x++)
            {
                char c = line[x];
                //Vector2 pos = new Vector2(x * tileSize, y * tileSize) + offset;
                Vector2 pos = new Vector2(x * tileSize, gridY * tileSize) + offset;

                TileType type = TileType.Dirt;
                GameObject prefabToUse = null;

                switch (c)
                {
                    case 'D':
                        type = TileType.Dirt;
                        int layerIndex = Mathf.FloorToInt((float)(height - 1 - y) / height * dirtLayerPrefabs.Length);
                        layerIndex = Mathf.Clamp(layerIndex, 0, dirtLayerPrefabs.Length - 1);
                        prefabToUse = dirtLayerPrefabs[layerIndex];
                        break;

                    case 'T':
                        type = TileType.Tunnel;
                        break;

                    case 'R':
                        type = TileType.Rock;
                        prefabToUse = rockPrefab;
                        break;

                    case 'P':
                        type = TileType.Tunnel;
                        var playerObj = Instantiate(playerPrefab, pos, Quaternion.identity);
                        Vector2Int playerGridPos = new Vector2Int(x, gridY); // <-- USE gridY, not y!
                        playerObj.GetComponent<PlayerController>().Init(playerGridPos);
                        break;

                    case 'O': // Pooka
                        type = TileType.Tunnel;
                        SpawnEnemy(pookaPrefab, x, y);
                        break;

                    case 'F': // Fygar
                        type = TileType.Tunnel;
                        SpawnEnemy(fygarPrefab, x, y);
                        break;

                    case 'I': // Indestructible Dirt
                        type = TileType.Indestructible;
                        layerIndex = Mathf.FloorToInt((float)(height - 1 - y) / height * dirtLayerPrefabs.Length);
                        layerIndex = Mathf.Clamp(layerIndex, 0, dirtLayerPrefabs.Length - 1);
                        prefabToUse = dirtLayerPrefabs[layerIndex]; // reuse dirt prefab but tag it differently
                        break;

                    default:
                        Debug.LogWarning($"Unknown tile character '{c}' at ({x},{y})");
                        break;
                }

                GameObject cellObj;

                if (prefabToUse != null)
                {
                    cellObj = Instantiate(prefabToUse, pos, Quaternion.identity, transform);
                    Debug.Log($"Spawned tile at ({x}, {y}): {cellObj.name}");
                }
                else
                {
                    cellObj = new GameObject($"Cell_{x}_{y}");
                    cellObj.transform.position = pos;
                    cellObj.transform.parent = transform;
                    cellObj.AddComponent<SpriteRenderer>().enabled = false;
                }

                GridCell cell = cellObj.GetComponent<GridCell>();
                if (cell == null) cell = cellObj.AddComponent<GridCell>();
                cell.Init(x, y, type);
                grid[x, gridY] = cell;

                if (type == TileType.Rock)
                {
                    RockController rock = cellObj.GetComponent<RockController>();
                    if (rock != null) rock.Init(x, y);
                }
            }
        }
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
        GameManager.Instance.RegisterEnemy();
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

    public void ClearLevel()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Optionally also destroy player/enemy clones
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Destroy(enemy);
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Destroy(player);
        }
    }
}
