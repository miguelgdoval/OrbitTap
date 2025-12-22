using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static LogHelper;

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
            Log($"[LocalLeaderboardManager] Inicializado. Entradas cargadas: {entries.Count}");
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
        Log($"[LocalLeaderboardManager] Puntuación añadida: {score} puntos. Total entradas: {entries.Count}");
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
    
    /// <summary>
    /// Actualiza todas las entradas del leaderboard del jugador actual con el nuevo nombre
    /// Identifica las entradas del jugador por su high score o por el nombre anterior
    /// </summary>
    public void UpdatePlayerName(string oldName, string newName)
    {
        if (string.IsNullOrEmpty(newName) || oldName == newName)
        {
            return;
        }
        
        // Obtener el high score del jugador actual
        int playerHighScore = PlayerPrefs.GetInt("HighScore", 0);
        
        // Log para debug: mostrar todas las entradas y sus nombres
        Log($"[LocalLeaderboardManager] Intentando actualizar nombre de '{oldName}' a '{newName}' (highScore: {playerHighScore})");
        Log($"[LocalLeaderboardManager] Total entradas en leaderboard: {entries.Count}");
        foreach (var entry in entries)
        {
            Log($"[LocalLeaderboardManager] Entrada: score={entry.score}, name='{entry.playerName}'");
        }
        
        bool updated = false;
        int updatedCount = 0;
        
        // Actualizar todas las entradas que:
        // 1. Tienen el nombre anterior (para cubrir casos donde el usuario cambió el nombre varias veces)
        // 2. O tienen el high score del jugador (para asegurar que se actualicen todas las entradas del jugador)
        foreach (var entry in entries)
        {
            bool shouldUpdate = false;
            
            // Actualizar si tiene el nombre anterior
            if (entry.playerName == oldName)
            {
                shouldUpdate = true;
                Log($"[LocalLeaderboardManager] Coincidencia por nombre anterior: score={entry.score}, name='{entry.playerName}'");
            }
            // O si tiene el high score del jugador (y no es el nuevo nombre)
            else if (playerHighScore > 0 && entry.score == playerHighScore && entry.playerName != newName)
            {
                shouldUpdate = true;
                Log($"[LocalLeaderboardManager] Coincidencia por high score: score={entry.score}, name='{entry.playerName}'");
            }
            
            if (shouldUpdate)
            {
                string previousName = entry.playerName;
                entry.playerName = newName;
                updated = true;
                updatedCount++;
                Log($"[LocalLeaderboardManager] Actualizando entrada (score: {entry.score}) de '{previousName}' a '{newName}'");
            }
        }
        
        if (updated)
        {
            SaveLeaderboard();
            Log($"[LocalLeaderboardManager] Nombre actualizado de '{oldName}' a '{newName}' en {updatedCount} entradas");
        }
        else
        {
            LogWarning($"[LocalLeaderboardManager] No se encontraron entradas para actualizar (oldName: '{oldName}', highScore: {playerHighScore})");
        }
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

