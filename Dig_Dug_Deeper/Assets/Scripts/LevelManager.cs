using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public int currentLevel = 0;     // Starts at level 0 (top level)
    public int levelHeight = 10;     // Number of rows per level
    public int totalLevels = 1;

    private void Awake()
    {
        Instance = this;
    }

    /*public int GetPlayerLevelByY(float yPos)
    {
        int totalLevels = 3; // or calculate based on how many text files you load
        int totalHeight = totalLevels * levelHeight;
        int flippedY = Mathf.FloorToInt(totalHeight - yPos - 1);

        return flippedY / levelHeight;
    }

    public int GetBottomRowOfCurrentLevel()
    {
        int totalLevels = 3;
        int totalHeight = totalLevels * levelHeight;

        int levelTop = totalHeight - currentLevel * levelHeight;
        return levelTop - 1;
    }

    public int GetTopRowOfCurrentLevel()
    {
        return GetBottomRowOfCurrentLevel() - levelHeight + 1;
    }*/

    public int GetTopRowOfCurrentLevel()
    {
        return currentLevel * levelHeight;
    }

    public int GetBottomRowOfCurrentLevel()
    {
        return (currentLevel + 1) * levelHeight - 1;
    }

    public int GetPlayerLevelByY(float yPos)
    {
        return Mathf.FloorToInt(yPos / levelHeight);
    }
}
