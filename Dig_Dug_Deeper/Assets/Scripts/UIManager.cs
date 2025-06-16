using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject gameOverMenu;
    [SerializeField] private TextMeshProUGUI lastScoreText;

    void Start()
    {
        Time.timeScale = 1f;

        // If this is the Game scene, make sure Game Over UI is hidden
        if (gameOverMenu != null)
            gameOverMenu.SetActive(false);
    }

    // Called when "Start Game" button is clicked from Main Menu
    public void StartGame()
    {
        SceneManager.LoadScene("DigDugDeeper"); 
    }

    // Called when the player dies
    public void TriggerGameOver()
    {
        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(true);
            Time.timeScale = 0f; // Pause game
        }

        if (lastScoreText != null)
        {
            lastScoreText.text = "Last Score: " + ScoreManager.Instance.LastRunScore;
        }
    }

    // Restart the current level
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Go back to main menu
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // Quit the game (won't work in editor)
    public void QuitGame()
    {
        Application.Quit();
    }
}
