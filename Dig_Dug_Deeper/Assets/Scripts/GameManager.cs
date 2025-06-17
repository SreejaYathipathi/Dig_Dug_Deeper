using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool isGameOver = false;

    // Add this list to track enemies
    private readonly List<EnemyController> _activeEnemies = new();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Registers a new enemy to the manager.
    /// </summary>
    public void RegisterEnemy(EnemyController enemy)
    {
        if (!_activeEnemies.Contains(enemy))
            _activeEnemies.Add(enemy);
    }

    /// <summary>
    /// Unregisters a dead or destroyed enemy.
    /// </summary>
    public void UnregisterEnemy(EnemyController enemy)
    {
        _activeEnemies.Remove(enemy);
    }

    /// <summary>
    /// Checks if all enemies in the current level are cleared.
    /// </summary>
    public bool AreAllEnemiesCleared()
    {
        int top = LevelManager.Instance.GetTopRowOfCurrentLevel();
        int bottom = LevelManager.Instance.GetBottomRowOfCurrentLevel();

        foreach (EnemyController enemy in _activeEnemies)
        {
            if (enemy.IsDead) continue;

            int enemyY = Mathf.RoundToInt(enemy.transform.position.y);
            if (enemyY >= top && enemyY <= bottom)
            {
                return false;
            }
        }

        return true;
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("Game Over!");
    }
}
