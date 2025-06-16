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

    // Call this to mark the game as over
    public void GameOver()
    {
        isGameOver = true;
        Debug.Log("Game Over!");
    }
}
