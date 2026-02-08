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
    
    // Optimización: actualizar score solo cada segundo en lugar de cada frame
    private float lastScoreUpdateTime = 0f;
    private const float SCORE_UPDATE_INTERVAL = 1f; // Actualizar score cada segundo
    
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
            // Actualizar tiempo de juego actual (esto es necesario cada frame para precisión)
            currentGamePlayTime = Time.time - currentGameStartTime;
            
            // Optimización: actualizar puntuación solo cada segundo en lugar de cada frame
            float currentTime = Time.time;
            if (currentTime - lastScoreUpdateTime >= SCORE_UPDATE_INTERVAL)
            {
                lastScoreUpdateTime = currentTime;
                if (scoreManager != null)
                {
                    currentGameScore = scoreManager.GetCurrentScore();
                }
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
        
        // Calcular mejor racha: una racha es una secuencia de partidas donde el score aumenta
        // o se mantiene alto (dentro de un margen razonable)
        CalculateBestStreak(stats);
        
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
    
    /// <summary>
    /// Calcula la mejor racha de partidas consecutivas basándose en el historial reciente.
    /// Una racha es una secuencia de partidas donde el jugador:
    /// - Mejora su puntuación, O
    /// - Mantiene una puntuación alta (dentro del 80% de su mejor score), O
    /// - Supera su promedio de puntuación
    /// </summary>
    private void CalculateBestStreak(PlayerStatistics stats)
    {
        if (stats.recentScores == null || stats.recentScores.Count < 2)
        {
            // No hay suficientes datos para calcular racha
            return;
        }
        
        int currentStreak = 1; // La partida actual cuenta como 1
        int bestStreakFound = stats.bestStreak;
        
        // Calcular promedio de puntuación para usar como referencia
        float averageScore = stats.averageScore > 0 ? stats.averageScore : 
                           (stats.bestScore > 0 ? stats.bestScore * 0.5f : 10f);
        
        // Calcular umbral mínimo para considerar una partida "buena" (80% del mejor score o promedio)
        float goodScoreThreshold = Mathf.Max(
            stats.bestScore * 0.8f,
            averageScore
        );
        
        // Analizar partidas recientes de atrás hacia adelante (más antiguas primero)
        for (int i = stats.recentScores.Count - 2; i >= 0; i--)
        {
            int currentScore = stats.recentScores[i + 1];
            int previousScore = stats.recentScores[i];
            
            // Una partida continúa la racha si:
            // 1. Mejora la puntuación anterior, O
            // 2. Mantiene una puntuación "buena" (>= umbral), O
            // 3. Está por encima del promedio
            bool continuesStreak = 
                currentScore >= previousScore || // Mejora o mantiene
                currentScore >= goodScoreThreshold || // Puntuación "buena"
                currentScore >= averageScore; // Por encima del promedio
            
            if (continuesStreak)
            {
                currentStreak++;
            }
            else
            {
                // La racha se rompió, actualizar mejor racha si es necesario
                if (currentStreak > bestStreakFound)
                {
                    bestStreakFound = currentStreak;
                }
                currentStreak = 1; // Reiniciar racha
            }
        }
        
        // Verificar si la racha actual es la mejor
        if (currentStreak > bestStreakFound)
        {
            bestStreakFound = currentStreak;
        }
        
        // Actualizar mejor racha si es mayor
        if (bestStreakFound > stats.bestStreak)
        {
            stats.bestStreak = bestStreakFound;
            Log($"[StatisticsManager] Nueva mejor racha: {bestStreakFound} partidas consecutivas");
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
