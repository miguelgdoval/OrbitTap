using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    [Header("UI")]
    public Text scoreText;
    public Text highScoreText;

    private float score = 0f;
    private int highScore = 0;
    private const string HIGH_SCORE_KEY = "HighScore";
    private bool isGameOver = false;

    private void Start()
    {
        LoadHighScore();
        
        if (highScoreText != null)
        {
            highScoreText.text = "Best: " + highScore;
        }
    }

    private void Update()
    {
        // No aumentar la puntuación si el juego terminó
        if (isGameOver) return;
        
        score += Time.deltaTime;

        if (scoreText != null)
        {
            scoreText.text = Mathf.FloorToInt(score).ToString();
        }
    }
    
    /// <summary>
    /// Detiene el aumento de la puntuación cuando el juego termina
    /// </summary>
    public void StopScoring()
    {
        isGameOver = true;
    }

    public void SaveHighScore()
    {
        int currentScore = Mathf.FloorToInt(score);
        
        // Save last score
        PlayerPrefs.SetInt("LastScore", currentScore);
        
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
        }
        
        PlayerPrefs.Save();
    }

    public void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    public int GetCurrentScore()
    {
        return Mathf.FloorToInt(score);
    }

    public int GetHighScore()
    {
        return highScore;
    }
}

