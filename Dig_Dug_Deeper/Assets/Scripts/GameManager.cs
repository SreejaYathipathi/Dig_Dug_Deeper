using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton instance accessible from anywhere
    public static GameManager Instance { get; private set; }

    public bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public bool AreAllEnemiesCleared()
    {
        int top = LevelManager.Instance.GetTopRowOfCurrentLevel();
        int bottom = LevelManager.Instance.GetBottomRowOfCurrentLevel();

        EnemyController[] enemies = FindObjectsOfType<EnemyController>();

        foreach (EnemyController enemy in enemies)
        {
            if (enemy.IsDead) continue;

            int enemyY = Mathf.RoundToInt(enemy.transform.position.y);
            if (enemyY >= top && enemyY <= bottom)
            {
                return false; // enemy alive and inside current level zone
            }
        }

        return true;
    }

    // Call this to mark the game as over
    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("Game Over!");
    }
}
