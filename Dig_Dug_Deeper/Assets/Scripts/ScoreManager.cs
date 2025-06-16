using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int HighScore { get; private set; }
    public int CurrentScore { get; private set; } = 0;
    public int LastRunScore { get; private set; } = 0;

    [SerializeField] private TextMeshProUGUI scoreText;     // drag in inspector
    [SerializeField] private TextMeshProUGUI highScoreText; // optional: drag in inspector

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadHighScore();
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount;
        Debug.Log("Score: " + CurrentScore);
        UpdateUI(); // Only updates UI, no high score check here!
    }

    public void EvaluateHighScore()
    {
        LastRunScore = CurrentScore;

        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
            SaveHighScore();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + CurrentScore;

        if (highScoreText != null)
            highScoreText.text = "High Score: " + HighScore;
    }

    private void LoadHighScore()
    {
        HighScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void SaveHighScore()
    {
        PlayerPrefs.SetInt("HighScore", HighScore);
        PlayerPrefs.Save();
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        UpdateUI();
    }
}
