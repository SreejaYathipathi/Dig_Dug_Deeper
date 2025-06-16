using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    private GameObject _dirtPrefab;
    public GameObject tunnelPrefab;
    public GameObject rockPrefab;
    public GameObject playerPrefab;
    public GameObject pookaPrefab;
    public GameObject fygarPrefab;

    public GameObject[] dirtVariants;

    public string levelFileName = "Levels/Level_1";
    public float tileUnitSize = 1f; // 1 = 16 pixels, since Pixels Per Unit = 16

    private int gridWidth;
    private int gridHeight;
    public static GridManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetDirtForCurrentLevel();
        LoadLevelFromFile();
    }

    void LoadLevelFromFile()
    {

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        TextAsset levelData = Resources.Load<TextAsset>(levelFileName);
        if (levelData == null)
        {
            Debug.LogError("Level file not found at Resources/" + levelFileName);
            return;
        }

        string[] lines = levelData.text.Split('\n');
        int height = lines.Length;
        int width = 0;
        foreach (string line in lines)
            width = Mathf.Max(width, line.Trim().Length);

        Vector3 bottomLeftOffset = new Vector3(0, 0, 0); // you can also add padding here if needed

        for (int y = 0; y < height; y++)
        {
            string line = lines[y].Trim();
            for (int x = 0; x < line.Length; x++)
            {
                char tileChar = line[x];
                GameObject prefabToSpawn = GetPrefabForChar(tileChar);

                if (prefabToSpawn != null)
                {
                    float spawnX = x * tileUnitSize;
                    float spawnY = (height - 1 - y) * tileUnitSize;
                    Vector3 spawnPos = new Vector3(spawnX, spawnY, 0) + bottomLeftOffset;
                    Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform);
                }
            }
        }


        gridWidth = width;
        gridHeight = height;

        CenterCameraOnGrid();

    }

    public bool IsWithinBounds(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.y);
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    void SetDirtForCurrentLevel()
    {
        if (levelFileName.Contains("Level_1"))
            _dirtPrefab = dirtVariants[0];
        else if (levelFileName.Contains("Level_2"))
            _dirtPrefab = dirtVariants[1];
        else if (levelFileName.Contains("Level_3"))
            _dirtPrefab = dirtVariants[2];
        else
            Debug.LogWarning("No matching dirt prefab found for: " + levelFileName);
    }

    GameObject GetPrefabForChar(char c)
    {
        return c switch
        {
            'D' => _dirtPrefab,
            'T' => tunnelPrefab,
            'R' => rockPrefab,
            'P' => playerPrefab,
            'K' => pookaPrefab,
            'F' => fygarPrefab,
            _ => null
        };
    }

    void CenterCameraOnGrid()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        float camX = (gridWidth * tileUnitSize) / 2f - tileUnitSize / 2f;
        float camY = (gridHeight * tileUnitSize) / 2f - tileUnitSize / 2f;

        mainCam.transform.position = new Vector3(camX, camY, -10);

        // Adjust orthographic size to fit height (or width depending on aspect ratio)
        float screenAspect = (float)Screen.width / Screen.height;
        float targetWidth = gridWidth * tileUnitSize;
        float targetHeight = gridHeight * tileUnitSize;

        mainCam.orthographicSize = Mathf.Max(targetHeight / 2f, (targetWidth / screenAspect) / 2f);
    }

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
