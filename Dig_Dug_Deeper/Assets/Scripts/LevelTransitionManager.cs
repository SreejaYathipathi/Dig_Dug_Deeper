using System.Collections;
using UnityEngine;

public class LevelTransitionManager : MonoBehaviour
{
    public static LevelTransitionManager Instance;

    [SerializeField] private float enemyWanderDelay = 3f;
    // time until enemies resume normal AI (Time.time + delay)
    public float enemyWanderEndTime;

    private Camera _mainCamera;

    [SerializeField]
    private float cameraLerpDuration = 3f; 

    private Coroutine _cameraMoveRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _mainCamera = Camera.main;
    }

    private void Start()
    {
        UpdateEnemiesForCurrentLevel();
        // start delay when game begins
        enemyWanderEndTime = Time.time + enemyWanderDelay;
    }

    /// <summary>
    /// Try to move down one level when player steps across boundary.
    /// Returns true if transition occurred.
    /// </summary>
    public bool TryTransitionDown(Vector3 playerPos, Vector2 input)
    {
        if (input.y >= 0)
            return false;

        int newLevel = LevelManager.Instance.GetPlayerLevelByY(playerPos.y);
        LevelManager.Instance.currentLevel = newLevel;

        int currentY = Mathf.RoundToInt(playerPos.y);
        int boundary = LevelManager.Instance.GetTopRowOfCurrentLevel();
        bool cleared = GameManager.Instance.AreAllEnemiesCleared();

        if (currentY == boundary && cleared)
        {
            LevelManager.Instance.currentLevel++;
            MoveCameraDown();

            UpdateEnemiesForCurrentLevel();
            // trigger 3-second wander-only delay
            enemyWanderEndTime = Time.time + enemyWanderDelay;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if transition is blocked by uncleared enemies.
    /// </summary>
    public bool IsBlockedByEnemies(Vector3 playerPos, Vector2 input)
    {
        if (input.y >= 0)
            return false;

        int currentY = Mathf.RoundToInt(playerPos.y);
        int boundary = LevelManager.Instance.GetTopRowOfCurrentLevel();
        bool cleared = GameManager.Instance.AreAllEnemiesCleared();

        return (currentY == boundary && !cleared);
    }

    /// <summary>
    /// Disable all enemies not in the current level.
    /// </summary>
    private void UpdateEnemiesForCurrentLevel()
    {
        EnemyController[] allEnemies = Object.FindObjectsByType<EnemyController>(
            FindObjectsSortMode.None
        );
        int level = LevelManager.Instance.currentLevel;

        foreach (var enemy in allEnemies)
        {
            int enemyLevel = LevelManager.Instance.GetPlayerLevelByY(enemy.transform.position.y);
            enemy.gameObject.SetActive(enemyLevel == level);
        }
    }

    /// <summary>
    /// Move the main camera down by one level height, smoothly over duration.
    /// </summary>
    private void MoveCameraDown()
    {
        // If a previous move is still running, stop it
        if (_cameraMoveRoutine != null)
            StopCoroutine(_cameraMoveRoutine);

        // Start smooth move coroutine
        _cameraMoveRoutine = StartCoroutine(LerpCameraDown());
    }

    /// <summary>
    /// Coroutine: Lerps camera Y position down by one levelHeight over cameraLerpDuration seconds.
    /// </summary>
    private IEnumerator LerpCameraDown()
    {
        Vector3 startPos = _mainCamera.transform.position;
        Vector3 endPos = startPos;
        endPos.y -= LevelManager.Instance.levelHeight;

        float elapsed = 0f;
        while (elapsed < cameraLerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cameraLerpDuration);
            _mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Ensure final position is exact
        _mainCamera.transform.position = endPos;
        _cameraMoveRoutine = null;
    }
}
