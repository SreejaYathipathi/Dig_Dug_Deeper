using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles smooth camera transitions between levels,
/// enforces a “wander-only” delay for enemies,
/// and provides a check for blocked transitions by uncleared enemies.
/// </summary>
public class LevelTransitionManager : MonoBehaviour
{
    public static LevelTransitionManager Instance;

    [Header("Enemy Wander Delay")]
    [SerializeField]
    private float enemyWanderDelay = 3f;   // Seconds enemies remain wandering after a level change
    public float enemyWanderEndTime;       // Time.time + enemyWanderDelay

    [Header("Camera Movement")]
    [SerializeField]
    private float cameraLerpDuration = 3f; // Seconds for smooth camera slide

    private Camera mainCamera;
    private Coroutine cameraMoveRoutine;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        mainCamera = Camera.main;
    }

    private void Start()
    {
        // Start the initial wander-only window
        enemyWanderEndTime = Time.time + enemyWanderDelay;
    }

    /// <summary>
    /// Attempts to move the player down one level.
    /// Returns true if a transition actually occurred.
    /// </summary>
    public bool TryTransitionDown(Vector3 playerPos, Vector2 input)
    {
        // Only handle downward input
        if (input.y >= 0f)
            return false;

        int playerRow = Mathf.RoundToInt(playerPos.y);
        int boundaryRow = LevelManager.Instance.GetTopRowOfCurrentLevel();
        bool allCleared = GameManager.Instance.AreAllEnemiesCleared();

        // Only transition if at the boundary and all enemies are cleared
        if (playerRow == boundaryRow && allCleared)
        {
            AudioManager.Instance.PlayLevelChange("LevelChange");
            // Increment level index, clamped
            LevelManager.Instance.currentLevel = Mathf.Min(
                LevelManager.Instance.currentLevel + 1,
                LevelManager.Instance.totalLevels - 1
            );

            // Smoothly move the camera down
            MoveCamera(Vector3.down * LevelManager.Instance.levelHeight);

            // Restart wander-only timer
            enemyWanderEndTime = Time.time + enemyWanderDelay;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to move the player up one level.
    /// Returns true if a transition actually occurred.
    /// </summary>
    public bool TryTransitionUp(Vector3 playerPos, Vector2 input)
    {
        // Only handle upward input
        if (input.y <= 0f)
            return false;

        int playerRow = Mathf.RoundToInt(playerPos.y);
        int boundaryRow = LevelManager.Instance.GetBottomRowOfCurrentLevel();
        bool allCleared = GameManager.Instance.AreAllEnemiesCleared();

        // Only transition if at the boundary and all enemies are cleared
        if (playerRow == boundaryRow && allCleared)
        {
            // Decrement level index, clamped
            LevelManager.Instance.currentLevel = Mathf.Max(
                LevelManager.Instance.currentLevel - 1,
                0
            );

            // Smoothly move the camera up
            MoveCamera(Vector3.up * LevelManager.Instance.levelHeight);

            // Restart wander-only timer
            enemyWanderEndTime = Time.time + enemyWanderDelay;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the player is at a boundary row but cannot transition
    /// because not all enemies have been cleared yet.
    /// </summary>
    public bool IsBlockedByEnemies(Vector3 playerPos, Vector2 input)
    {
        // Only consider downward attempts
        if (input.y >= 0f)
            return false;

        int playerRow = Mathf.RoundToInt(playerPos.y);
        int boundaryRow = LevelManager.Instance.GetTopRowOfCurrentLevel();
        bool allCleared = GameManager.Instance.AreAllEnemiesCleared();

        // Blocked if at boundary and some enemies remain
        return (playerRow == boundaryRow && !allCleared);
    }

    /// <summary>
    /// Initiates a smooth camera movement by the given delta.
    /// </summary>
    private void MoveCamera(Vector3 delta)
    {
        if (cameraMoveRoutine != null)
            StopCoroutine(cameraMoveRoutine);

        cameraMoveRoutine = StartCoroutine(LerpCamera(delta));
    }

    /// <summary>
    /// Coroutine that linearly interpolates the camera over cameraLerpDuration seconds.
    /// </summary>
    private IEnumerator LerpCamera(Vector3 delta)
    {
        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos = startPos + delta;

        float elapsed = 0f;
        while (elapsed < cameraLerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cameraLerpDuration);
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Snap to the exact final position
        mainCamera.transform.position = endPos;
        cameraMoveRoutine = null;
    }
}
