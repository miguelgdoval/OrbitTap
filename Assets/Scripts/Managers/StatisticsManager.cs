using UnityEngine;
using System;
using System.Collections.Generic;
using static LogHelper;

/// <summary>
/// Manager para rastrear y gestionar estadísticas detalladas del jugador
/// </summary>
public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }
    
    // Estadísticas de la partida actual
    private int currentGameObstaclesAvoided = 0;
    private int currentGameNearMisses = 0;
    private int currentGameCollisions = 0;
    private float currentGameStartTime = 0f;
    private float currentGamePlayTime = 0f;
    private int currentGameScore = 0;
    private bool isTrackingCurrentGame = false;
    
    // Referencias a otros managers
    private ScoreManager scoreManager;
    private ObstacleManager obstacleManager;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Buscar referencias a otros managers
        scoreManager = FindFirstObjectByType<ScoreManager>();
        obstacleManager = FindFirstObjectByType<ObstacleManager>();
    }
    
    private void Update()
    {
        if (isTrackingCurrentGame)
        {
            // Actualizar tiempo de juego actual
            currentGamePlayTime = Time.time - currentGameStartTime;
            
            // Actualizar puntuación actual
            if (scoreManager != null)
            {
                currentGameScore = scoreManager.GetCurrentScore();
            }
        }
    }
    
    /// <summary>
    /// Inicia el rastreo de una nueva partida
    /// </summary>
    public void StartTrackingGame()
    {
        if (isTrackingCurrentGame)
        {
            LogWarning("[StatisticsManager] Ya se está rastreando una partida. Finalizando la anterior...");
            EndTrackingGame();
        }
        
        currentGameObstaclesAvoided = 0;
        currentGameNearMisses = 0;
        currentGameCollisions = 0;
        currentGameStartTime = Time.time;
        currentGamePlayTime = 0f;
        currentGameScore = 0;
        isTrackingCurrentGame = true;
        
        Log("[StatisticsManager] Iniciando rastreo de nueva partida");
    }
    
    /// <summary>
    /// Finaliza el rastreo de la partida actual y guarda las estadísticas
    /// </summary>
    public void EndTrackingGame()
    {
        if (!isTrackingCurrentGame)
        {
            LogWarning("[StatisticsManager] No hay partida activa para finalizar");
            return;
        }
        
        // Calcular tiempo final de juego
        currentGamePlayTime = Time.time - currentGameStartTime;
        
        // Obtener puntuación final
        if (scoreManager != null)
        {
            currentGameScore = scoreManager.GetCurrentScore();
        }
        
        // Guardar estadísticas
        SaveGameStatistics();
        
        // Resetear para la próxima partida
        isTrackingCurrentGame = false;
        
        Log($"[StatisticsManager] Partida finalizada. Score: {currentGameScore}, Tiempo: {currentGamePlayTime:F2}s, Obstáculos evitados: {currentGameObstaclesAvoided}");
    }
    
    /// <summary>
    /// Registra que se evitó un obstáculo
    /// </summary>
    public void RecordObstacleAvoided()
    {
        if (isTrackingCurrentGame)
        {
            currentGameObstaclesAvoided++;
        }
    }
    
    /// <summary>
    /// Registra un near miss
    /// </summary>
    public void RecordNearMiss()
    {
        if (isTrackingCurrentGame)
        {
            currentGameNearMisses++;
        }
    }
    
    /// <summary>
    /// Registra una colisión
    /// </summary>
    public void RecordCollision()
    {
        if (isTrackingCurrentGame)
        {
            currentGameCollisions++;
        }
    }
    
    /// <summary>
    /// Guarda las estadísticas de la partida actual
    /// </summary>
    private void SaveGameStatistics()
    {
        if (SaveDataManager.Instance == null)
        {
            LogWarning("[StatisticsManager] SaveDataManager no está disponible");
            return;
        }
        
        SaveData saveData = SaveDataManager.Instance.GetSaveData();
        if (saveData == null)
        {
            LogWarning("[StatisticsManager] SaveData es null");
            return;
        }
        
        PlayerStatistics stats = saveData.statistics;
        if (stats == null)
        {
            stats = new PlayerStatistics();
            saveData.statistics = stats;
        }
        
        // Actualizar fecha del día
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        bool isNewDay = stats.lastPlayDate != today;
        
        if (isNewDay)
        {
            // Resetear estadísticas diarias
            stats.gamesPlayedToday = 0;
            stats.bestScoreToday = 0;
            stats.playTimeToday = 0f;
            stats.lastPlayDate = today;
        }
        
        // Incrementar partidas jugadas
        stats.totalGamesPlayed++;
        stats.gamesPlayedToday++;
        
        // Actualizar tiempo de juego
        stats.totalPlayTime += currentGamePlayTime;
        stats.playTimeToday += currentGamePlayTime;
        stats.totalSurvivalTime += currentGamePlayTime;
        
        // Recalcular promedios
        if (stats.totalGamesPlayed > 0)
        {
            stats.averagePlayTime = stats.totalPlayTime / stats.totalGamesPlayed;
            stats.averageSurvivalTime = stats.totalSurvivalTime / stats.totalGamesPlayed;
        }
        
        // Actualizar puntuaciones
        stats.totalScore += currentGameScore;
        if (stats.totalGamesPlayed > 0)
        {
            stats.averageScore = stats.totalScore / stats.totalGamesPlayed;
        }
        
        // Actualizar mejor puntuación (ya se actualiza en ScoreManager, pero lo verificamos)
        if (currentGameScore > stats.bestScore)
        {
            stats.bestScore = currentGameScore;
        }
        
        // Actualizar mejor puntuación del día
        if (currentGameScore > stats.bestScoreToday)
        {
            stats.bestScoreToday = currentGameScore;
        }
        
        // Actualizar tiempo de supervivencia más largo
        if (currentGamePlayTime > stats.longestSurvivalTime)
        {
            stats.longestSurvivalTime = currentGamePlayTime;
        }
        
        // Actualizar obstáculos
        stats.totalObstaclesAvoided += currentGameObstaclesAvoided;
        stats.totalNearMisses += currentGameNearMisses;
        stats.totalCollisions += currentGameCollisions;
        
        // Recalcular promedio de obstáculos por partida
        if (stats.totalGamesPlayed > 0)
        {
            stats.averageObstaclesPerGame = (float)stats.totalObstaclesAvoided / stats.totalGamesPlayed;
        }
        
        // Añadir a historial reciente (máximo 10 entradas)
        const int MAX_RECENT = 10;
        stats.recentScores.Add(currentGameScore);
        stats.recentPlayTimes.Add(currentGamePlayTime);
        
        if (stats.recentScores.Count > MAX_RECENT)
        {
            stats.recentScores.RemoveAt(0);
        }
        if (stats.recentPlayTimes.Count > MAX_RECENT)
        {
            stats.recentPlayTimes.RemoveAt(0);
        }
        
        // Calcular mejor racha (simplificado: si la puntuación es > 0, incrementar racha)
        // TODO: Implementar lógica más sofisticada de rachas
        
        // Guardar inmediatamente para no perder datos
        SaveDataManager.Instance.ForceSave();
        
        Log($"[StatisticsManager] Estadísticas guardadas. Total partidas: {stats.totalGamesPlayed}");
    }
    
    /// <summary>
    /// Obtiene las estadísticas actuales
    /// </summary>
    public PlayerStatistics GetStatistics()
    {
        if (SaveDataManager.Instance == null) return null;
        
        SaveData saveData = SaveDataManager.Instance.GetSaveData();
        return saveData?.statistics;
    }
    
    /// <summary>
    /// Obtiene las estadísticas de la partida actual
    /// </summary>
    public GameSessionStats GetCurrentGameStats()
    {
        return new GameSessionStats
        {
            score = currentGameScore,
            playTime = currentGamePlayTime,
            obstaclesAvoided = currentGameObstaclesAvoided,
            nearMisses = currentGameNearMisses,
            collisions = currentGameCollisions
        };
    }
    
    /// <summary>
    /// Resetea todas las estadísticas (útil para testing)
    /// </summary>
    public void ResetAllStatistics()
    {
        if (SaveDataManager.Instance == null) return;
        
        SaveData saveData = SaveDataManager.Instance.GetSaveData();
        if (saveData != null)
        {
            saveData.statistics = new PlayerStatistics();
            SaveDataManager.Instance.MarkDirty();
            Log("[StatisticsManager] Todas las estadísticas han sido reseteadas");
        }
    }
}

/// <summary>
/// Estadísticas de una sesión de juego individual
/// </summary>
[System.Serializable]
public class GameSessionStats
{
    public int score;
    public float playTime;
    public int obstaclesAvoided;
    public int nearMisses;
    public int collisions;
}
