using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private int _score = 0;
    public int GetScore() => _score;

    private int _enemiesRemaining;
    public bool IsGameOver { get; private set; } = false;

    [SerializeField] private TextAsset[] levelFiles;
    private int _currentLevelIndex = 0;

    private void Start()
    {
        LoadLevel(_currentLevelIndex);
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void LoadLevel(int index)
    {
        if(index < 0 || index >= levelFiles.Length)
        {
            Debug.LogError("Invalid level index!");
            return;
        }

        _enemiesRemaining = 0; // Reset enemy count for the new level

        GridManager grid = FindObjectOfType<GridManager>();
        grid.ClearLevel(); // Clear previous tiles, player, enemies
        grid.LoadLevelFromText(levelFiles[index]); // Load the new level;
    }

    public void AddScore(int amount)
    {
        _score += amount;
        Debug.Log($"Score: {_score}");
    }

    public void GameOver()
    {
        Debug.Log("[GameManager] GAME OVER");
        IsGameOver = true;
    }

    public void RegisterEnemy() => _enemiesRemaining++;
    public void EnemyDied()
    {
        _enemiesRemaining--;
        if (_enemiesRemaining <= 0)
        {
            Debug.Log("Level Complete!");
            LoadNextLevel();
        }
    }

    public void LoadNextLevel()
    {
        _currentLevelIndex++;
        if (_currentLevelIndex >= levelFiles.Length)
        {
            Debug.Log("All levels complete!");
            return;
        }

        LoadLevel(_currentLevelIndex);
    }
}
