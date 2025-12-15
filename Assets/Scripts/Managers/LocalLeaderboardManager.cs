using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Entrada individual del leaderboard
/// </summary>
[System.Serializable]
public class LeaderboardEntry
{
    public int score;
    public string playerName;
    public string date;
}

/// <summary>
/// Manager para el leaderboard local (top 10 puntuaciones del dispositivo)
/// </summary>
public class LocalLeaderboardManager : MonoBehaviour
{
    public static LocalLeaderboardManager Instance { get; private set; }
    
    private const string LEADERBOARD_KEY = "LocalLeaderboard";
    private const int MAX_ENTRIES = 10;
    
    private List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLeaderboard();
            Debug.Log($"[LocalLeaderboardManager] Inicializado. Entradas cargadas: {entries.Count}");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Añade una puntuación al leaderboard local
    /// </summary>
    public void AddScore(int score, string playerName = null)
    {
        if (score <= 0) return;
        
        // Si no se proporciona nombre, usar uno genérico
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Jugador";
        }
        
        LeaderboardEntry entry = new LeaderboardEntry
        {
            score = score,
            playerName = playerName,
            date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };
        
        entries.Add(entry);
        
        // Ordenar por puntuación descendente y mantener solo top 10
        entries = entries.OrderByDescending(e => e.score).Take(MAX_ENTRIES).ToList();
        
        SaveLeaderboard();
        Debug.Log($"[LocalLeaderboardManager] Puntuación añadida: {score} puntos. Total entradas: {entries.Count}");
    }
    
    /// <summary>
    /// Obtiene todas las entradas del leaderboard
    /// </summary>
    public List<LeaderboardEntry> GetTopScores()
    {
        return new List<LeaderboardEntry>(entries);
    }
    
    /// <summary>
    /// Obtiene el rango del jugador actual basado en su high score
    /// </summary>
    public int GetPlayerRank(int playerHighScore)
    {
        if (playerHighScore <= 0) return -1;
        
        int rank = 1;
        foreach (var entry in entries.OrderByDescending(e => e.score))
        {
            if (entry.score >= playerHighScore)
                rank++;
            else
                break;
        }
        
        return rank <= MAX_ENTRIES ? rank : -1;
    }
    
    private void SaveLeaderboard()
    {
        string json = JsonUtility.ToJson(new LeaderboardData { entries = entries });
        PlayerPrefs.SetString(LEADERBOARD_KEY, json);
        PlayerPrefs.Save();
    }
    
    private void LoadLeaderboard()
    {
        if (PlayerPrefs.HasKey(LEADERBOARD_KEY))
        {
            string json = PlayerPrefs.GetString(LEADERBOARD_KEY);
            LeaderboardData data = JsonUtility.FromJson<LeaderboardData>(json);
            if (data != null && data.entries != null)
            {
                entries = data.entries;
            }
        }
    }
    
    [System.Serializable]
    private class LeaderboardData
    {
        public List<LeaderboardEntry> entries;
    }
}

