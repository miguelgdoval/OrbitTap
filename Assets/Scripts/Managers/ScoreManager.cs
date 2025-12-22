using UnityEngine;
using UnityEngine.UI;
using static LogHelper;

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
        
        // Reportar progreso de puntuación a MissionManager
        if (MissionManager.Instance != null)
        {
            int currentScoreInt = Mathf.FloorToInt(score);
            MissionManager.Instance.ReportValue(MissionObjectiveType.ReachScore, currentScoreInt);
            MissionManager.Instance.ReportValue(MissionObjectiveType.SurviveTime, currentScoreInt);
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

        // Asegurar que existe LocalLeaderboardManager incluso si se entra directo a la escena de juego
        if (LocalLeaderboardManager.Instance == null)
        {
            GameObject leaderboardObj = new GameObject("LocalLeaderboardManager");
            leaderboardObj.AddComponent<LocalLeaderboardManager>();
        }

        // Añadir siempre la puntuación actual al leaderboard local
        if (LocalLeaderboardManager.Instance != null)
        {
            // Obtener nombre del jugador desde PlayerPrefs
            string playerName = PlayerPrefs.GetString("PlayerName", "Jugador");
            Log($"[ScoreManager] Añadiendo puntuación {currentScore} al leaderboard local con nombre: {playerName}");
            LocalLeaderboardManager.Instance.AddScore(currentScore, playerName);
        }
        
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            
            // Reportar nuevo récord a MissionManager
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ReportValue(MissionObjectiveType.ReachHighScore, highScore);
            }
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

