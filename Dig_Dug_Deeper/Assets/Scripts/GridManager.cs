using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    // Prefab references for different tile and entity types
    private GameObject _dirtPrefab;
    public GameObject tunnelPrefab;
    public GameObject rockPrefab;
    public GameObject playerPrefab;
    public GameObject pookaPrefab;
    public GameObject fygarPrefab;

    public GameObject[] dirtVariants; // Array of dirt variants for different levels

    public string levelFileName = "Levels/Level_1"; // Path to level file in Resources folder
    public float tileUnitSize = 1f; // 1 = 16 pixels, since Pixels Per Unit = 16

    private int gridWidth;
    private int gridHeight;
    public static GridManager Instance; // Singleton reference

    private void Awake()
    {
        Instance = this; // Assign self as singleton instance
    }

    void Start()
    {
        LoadLevelFromFile();      // Load and build level grid from file
    }

    void LoadLevelFromFile()
    {

        foreach (Transform child in transform)
            Destroy(child.gameObject);

        List<string> combinedLines = new List<string>();
        List<GameObject> dirtByLine = new List<GameObject>(); // To track which dirt to use per line

        int totalLevels = 3;
        LevelManager.Instance.totalLevels = totalLevels;

        for (int i = 1; i <= totalLevels; i++)
        {
            string path = $"Levels/Level_{i}";
            TextAsset data = Resources.Load<TextAsset>(path);
            if (data == null) continue;

            string[] lines = data.text.Split('\n');

            // Get the correct dirt prefab for this level file
            GameObject dirtForThisLevel = GetDirtForLevelFile(path);

            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    combinedLines.Add(line.TrimEnd());
                    dirtByLine.Add(dirtForThisLevel); // Add dirt reference per line
                }
            }
        }

        BuildGridFromLines(combinedLines.ToArray(), dirtByLine.ToArray());

    }

    void BuildGridFromLines(string[] lines, GameObject[] dirtPrefabsByLine)
    {
        int height = lines.Length;
        int width = 0;
        foreach (string line in lines)
            width = Mathf.Max(width, line.Trim().Length);

        gridHeight = height;
        gridWidth = width;

        Vector3 bottomLeftOffset = new Vector3(0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            string line = lines[y].Trim();
            GameObject thisLineDirt = dirtPrefabsByLine[y]; // Use correct dirt

            for (int x = 0; x < line.Length; x++)
            {
                char tileChar = line[x];
                float spawnX = x * tileUnitSize;
                float spawnY = (height - 1 - y) * tileUnitSize;
                Vector3 spawnPos = new Vector3(spawnX, spawnY, 0) + bottomLeftOffset;

                if (tileChar == 'K' || tileChar == 'F' || tileChar == 'P')
                {
                    Instantiate(tunnelPrefab, spawnPos, Quaternion.identity, transform);
                    Instantiate(GetPrefabForChar(tileChar), spawnPos, Quaternion.identity, transform);
                }
                else if (tileChar == 'D')
                {
                    Instantiate(thisLineDirt, spawnPos, Quaternion.identity, transform);
                }
                else
                {
                    GameObject prefabToSpawn = GetPrefabForChar(tileChar);
                    if (prefabToSpawn != null)
                        Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform);
                }
            }
        }

        CenterCameraOnGrid();
    }

    GameObject GetDirtForLevelFile(string levelFilePath)
    {
        if (levelFilePath.Contains("Level_1"))
            return dirtVariants[0];
        else if (levelFilePath.Contains("Level_2"))
            return dirtVariants[1];
        else if (levelFilePath.Contains("Level_3"))
            return dirtVariants[2];

        Debug.LogWarning("No matching dirt prefab found for: " + levelFilePath);
        return dirtVariants[0]; // fallback
    }

    // Returns true if a position is within the grid dimensions
    public bool IsWithinBounds(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.y);
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    // Returns the corresponding prefab for each level character symbol
    GameObject GetPrefabForChar(char c)
    {
        return c switch
        {
            //'D' => _dirtPrefab,
            'T' => tunnelPrefab,
            'R' => rockPrefab,
            'P' => playerPrefab,
            'K' => pookaPrefab,
            'F' => fygarPrefab,
            _ => null
        };
    }

    // Adjusts camera position and zoom to center and fit the full grid on screen
    void CenterCameraOnGrid()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        float screenAspect = (float)Screen.width / Screen.height;
        float targetWidth = gridWidth * tileUnitSize;
        float targetHeight = LevelManager.Instance.levelHeight * tileUnitSize;

        // Center X stays the same
        float camX = (gridWidth * tileUnitSize) / 2f - tileUnitSize / 2f;

        // Shift Y to the topmost level
        int totalLevels = 3; // update if dynamic
        float camY = (gridHeight * tileUnitSize) - (targetHeight / 2f) - (tileUnitSize / 2f);

        mainCam.transform.position = new Vector3(camX, camY, -10);
        mainCam.orthographicSize = Mathf.Max(targetHeight / 2f, (targetWidth / screenAspect) / 2f);
    }

    // Checks if there is a tunnel GameObject at a given world position
    public bool IsTunnelAt(Vector2 worldPos)
    {
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Tunnel"))
            {
                if (Vector2.Distance(child.position, worldPos) < 0.1f)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
