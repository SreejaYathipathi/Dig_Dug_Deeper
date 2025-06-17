using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public int currentLevel = 0;      // 0 = top level
    public int levelHeight = 10;      // number of rows per level
    public int totalLevels = 3;       // total number of levels

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Calculate flipped level index based on player Y position.
    /// Top level corresponds to index 0.
    /// </summary>
    /// <param name="yPos">World Y position of the player.</param>
    /// <returns>Clamped level index between 0 and totalLevels - 1.</returns>
    public int GetPlayerLevelByY(float yPos)
    {
        int rawLevel = Mathf.FloorToInt(yPos / levelHeight);
        int flippedLevel = (totalLevels - 1) - rawLevel;
        return Mathf.Clamp(flippedLevel, 0, totalLevels - 1);
    }

    /// <summary>
    /// Get the bottom row index of the current level.
    /// Levels are arranged from top (index 0) downwards.
    /// </summary>
    /// <returns>Index of the bottom row for the current level.</returns>
    public int GetBottomRowOfCurrentLevel()
    {
        int totalHeight = totalLevels * levelHeight;
        int levelTopIndex = totalHeight - (currentLevel * levelHeight);
        return levelTopIndex - 1;
    }

    /// <summary>
    /// Get the top row index of the current level.
    /// </summary>
    /// <returns>Index of the top row for the current level.</returns>
    public int GetTopRowOfCurrentLevel()
    {
        return GetBottomRowOfCurrentLevel() - (levelHeight - 1);
    }
}
