using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Estructura de datos centralizada para guardar el estado del juego
/// Versión actual: 1
/// </summary>
[System.Serializable]
public class SaveData
{
    [Header("Version Info")]
    public int saveVersion = 1; // Versión del formato de guardado
    public string saveDate; // Fecha del último guardado
    
    [Header("Player Stats")]
    public int highScore = 0;
    public int lastScore = 0;
    public string playerName = "Jugador";
    
    [Header("Currency")]
    public int stellarShards = 0;
    public int cosmicCrystals = 0;
    
    [Header("Settings")]
    public bool soundEnabled = true;
    public bool vibrationEnabled = true;
    public string language = "ES";
    public int graphicsQuality = 1; // 0=Low, 1=Medium, 2=High
    
    [Header("Audio Settings")]
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    
    [Header("Accessibility")]
    public bool colorBlindMode = false;
    public bool highContrastUI = false;
    public bool reduceAnimations = false;
    
    [Header("Game Settings")]
    public bool tutorialEnabled = true; // Habilitar tutorial por defecto
    
    [Header("Ad Tracking")]
    public int gamesSinceLastAd = 0;
    public long lastAdTimestamp = 0;
    public bool removeAdsPurchased = false;
    
    [Header("Skin Unlocks")]
    public Dictionary<string, bool> skinUnlocks = new Dictionary<string, bool>();
    
    [Header("Mission Progress")]
    public Dictionary<string, MissionProgressData> missionProgress = new Dictionary<string, MissionProgressData>();
    
    [Header("Leaderboard")]
    public List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>();
    
    [Header("Player Statistics")]
    public PlayerStatistics statistics = new PlayerStatistics();
    
    /// <summary>
    /// Crea una copia de los datos actuales (para backup)
    /// </summary>
    public SaveData Clone()
    {
        SaveData clone = new SaveData
        {
            saveVersion = this.saveVersion,
            saveDate = this.saveDate,
            highScore = this.highScore,
            lastScore = this.lastScore,
            playerName = this.playerName,
            stellarShards = this.stellarShards,
            cosmicCrystals = this.cosmicCrystals,
            soundEnabled = this.soundEnabled,
            vibrationEnabled = this.vibrationEnabled,
            language = this.language,
            graphicsQuality = this.graphicsQuality,
            masterVolume = this.masterVolume,
            musicVolume = this.musicVolume,
            sfxVolume = this.sfxVolume,
            colorBlindMode = this.colorBlindMode,
            highContrastUI = this.highContrastUI,
            reduceAnimations = this.reduceAnimations,
            tutorialEnabled = this.tutorialEnabled,
            gamesSinceLastAd = this.gamesSinceLastAd,
            lastAdTimestamp = this.lastAdTimestamp,
            removeAdsPurchased = this.removeAdsPurchased,
            statistics = this.statistics.Clone()
        };
        
        // Clonar diccionarios
        clone.skinUnlocks = new Dictionary<string, bool>(this.skinUnlocks);
        clone.missionProgress = new Dictionary<string, MissionProgressData>();
        foreach (var kvp in this.missionProgress)
        {
            clone.missionProgress[kvp.Key] = new MissionProgressData
            {
                currentProgress = kvp.Value.currentProgress,
                isCompleted = kvp.Value.isCompleted,
                isClaimed = kvp.Value.isClaimed
            };
        }
        
        // Clonar lista de leaderboard
        clone.leaderboardEntries = new List<LeaderboardEntry>();
        foreach (var entry in this.leaderboardEntries)
        {
            clone.leaderboardEntries.Add(new LeaderboardEntry
            {
                score = entry.score,
                playerName = entry.playerName,
                date = entry.date
            });
        }
        
        return clone;
    }
    
    /// <summary>
    /// Valida los datos y corrige valores inválidos
    /// </summary>
    public bool ValidateAndFix()
    {
        bool wasFixed = false;
        
        // Validar puntuaciones
        if (highScore < 0 || highScore > 999999999)
        {
            Debug.LogWarning($"[SaveData] HighScore inválido ({highScore}), reseteando a 0");
            highScore = 0;
            wasFixed = true;
        }
        
        if (lastScore < 0 || lastScore > 999999999)
        {
            Debug.LogWarning($"[SaveData] LastScore inválido ({lastScore}), reseteando a 0");
            lastScore = 0;
            wasFixed = true;
        }
        
        // Validar monedas
        if (stellarShards < 0 || stellarShards > 999999999)
        {
            Debug.LogWarning($"[SaveData] StellarShards inválido ({stellarShards}), reseteando a 0");
            stellarShards = 0;
            wasFixed = true;
        }
        
        if (cosmicCrystals < 0 || cosmicCrystals > 999999999)
        {
            Debug.LogWarning($"[SaveData] CosmicCrystals inválido ({cosmicCrystals}), reseteando a 0");
            cosmicCrystals = 0;
            wasFixed = true;
        }
        
        // Validar volúmenes
        masterVolume = Mathf.Clamp01(masterVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        
        // Validar graphics quality
        if (graphicsQuality < 0 || graphicsQuality > 2)
        {
            Debug.LogWarning($"[SaveData] GraphicsQuality inválido ({graphicsQuality}), reseteando a 1");
            graphicsQuality = 1;
            wasFixed = true;
        }
        
        // Validar nombre del jugador
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Jugador";
            wasFixed = true;
        }
        else if (playerName.Length > 20)
        {
            playerName = playerName.Substring(0, 20);
            wasFixed = true;
        }
        
        // Validar language
        if (string.IsNullOrEmpty(language))
        {
            language = "ES";
            wasFixed = true;
        }
        
        // Validar timestamp de ads
        if (lastAdTimestamp < 0)
        {
            lastAdTimestamp = 0;
            wasFixed = true;
        }
        
        if (gamesSinceLastAd < 0)
        {
            gamesSinceLastAd = 0;
            wasFixed = true;
        }
        
        // Validar estadísticas
        if (statistics == null)
        {
            statistics = new PlayerStatistics();
            wasFixed = true;
        }
        else
        {
            bool statsFixed = statistics.ValidateAndFix();
            if (statsFixed) wasFixed = true;
        }
        
        return wasFixed;
    }
}

/// <summary>
/// Estructura de datos para las estadísticas detalladas del jugador
/// </summary>
[System.Serializable]
public class PlayerStatistics
{
    [Header("Game Sessions")]
    public int totalGamesPlayed = 0;
    public float totalPlayTime = 0f; // En segundos
    public float averagePlayTime = 0f; // En segundos
    
    [Header("Score Statistics")]
    public int bestScore = 0;
    public int averageScore = 0;
    public int totalScore = 0;
    public int bestStreak = 0; // Mejor racha de partidas consecutivas
    
    [Header("Obstacle Statistics")]
    public int totalObstaclesAvoided = 0;
    public int totalNearMisses = 0;
    public int totalCollisions = 0;
    public float averageObstaclesPerGame = 0f;
    
    [Header("Time Statistics")]
    public float longestSurvivalTime = 0f; // En segundos
    public float averageSurvivalTime = 0f; // En segundos
    public float totalSurvivalTime = 0f; // En segundos
    
    [Header("Recent Performance")]
    public List<int> recentScores = new List<int>(); // Últimas 10 partidas
    public List<float> recentPlayTimes = new List<float>(); // Últimas 10 partidas
    
    [Header("Daily Statistics")]
    public int gamesPlayedToday = 0;
    public int bestScoreToday = 0;
    public float playTimeToday = 0f; // En segundos
    public string lastPlayDate = ""; // Fecha del último juego (yyyy-MM-dd)
    
    /// <summary>
    /// Crea una copia de las estadísticas
    /// </summary>
    public PlayerStatistics Clone()
    {
        PlayerStatistics clone = new PlayerStatistics
        {
            totalGamesPlayed = this.totalGamesPlayed,
            totalPlayTime = this.totalPlayTime,
            averagePlayTime = this.averagePlayTime,
            bestScore = this.bestScore,
            averageScore = this.averageScore,
            totalScore = this.totalScore,
            bestStreak = this.bestStreak,
            totalObstaclesAvoided = this.totalObstaclesAvoided,
            totalNearMisses = this.totalNearMisses,
            totalCollisions = this.totalCollisions,
            averageObstaclesPerGame = this.averageObstaclesPerGame,
            longestSurvivalTime = this.longestSurvivalTime,
            averageSurvivalTime = this.averageSurvivalTime,
            totalSurvivalTime = this.totalSurvivalTime,
            gamesPlayedToday = this.gamesPlayedToday,
            bestScoreToday = this.bestScoreToday,
            playTimeToday = this.playTimeToday,
            lastPlayDate = this.lastPlayDate
        };
        
        // Clonar listas
        clone.recentScores = new List<int>(this.recentScores);
        clone.recentPlayTimes = new List<float>(this.recentPlayTimes);
        
        return clone;
    }
    
    /// <summary>
    /// Valida y corrige las estadísticas
    /// </summary>
    public bool ValidateAndFix()
    {
        bool wasFixed = false;
        
        // Validar valores negativos
        if (totalGamesPlayed < 0) { totalGamesPlayed = 0; wasFixed = true; }
        if (totalPlayTime < 0) { totalPlayTime = 0; wasFixed = true; }
        if (bestScore < 0) { bestScore = 0; wasFixed = true; }
        if (averageScore < 0) { averageScore = 0; wasFixed = true; }
        if (totalScore < 0) { totalScore = 0; wasFixed = true; }
        if (bestStreak < 0) { bestStreak = 0; wasFixed = true; }
        if (totalObstaclesAvoided < 0) { totalObstaclesAvoided = 0; wasFixed = true; }
        if (totalNearMisses < 0) { totalNearMisses = 0; wasFixed = true; }
        if (totalCollisions < 0) { totalCollisions = 0; wasFixed = true; }
        if (longestSurvivalTime < 0) { longestSurvivalTime = 0; wasFixed = true; }
        if (averageSurvivalTime < 0) { averageSurvivalTime = 0; wasFixed = true; }
        if (totalSurvivalTime < 0) { totalSurvivalTime = 0; wasFixed = true; }
        if (gamesPlayedToday < 0) { gamesPlayedToday = 0; wasFixed = true; }
        if (bestScoreToday < 0) { bestScoreToday = 0; wasFixed = true; }
        if (playTimeToday < 0) { playTimeToday = 0; wasFixed = true; }
        
        // Limitar tamaño de listas recientes
        const int MAX_RECENT_ENTRIES = 10;
        if (recentScores.Count > MAX_RECENT_ENTRIES)
        {
            recentScores = recentScores.GetRange(recentScores.Count - MAX_RECENT_ENTRIES, MAX_RECENT_ENTRIES);
            wasFixed = true;
        }
        if (recentPlayTimes.Count > MAX_RECENT_ENTRIES)
        {
            recentPlayTimes = recentPlayTimes.GetRange(recentPlayTimes.Count - MAX_RECENT_ENTRIES, MAX_RECENT_ENTRIES);
            wasFixed = true;
        }
        
        // Recalcular promedios si es necesario
        if (totalGamesPlayed > 0)
        {
            float newAvgPlayTime = totalPlayTime / totalGamesPlayed;
            if (Mathf.Abs(averagePlayTime - newAvgPlayTime) > 0.01f)
            {
                averagePlayTime = newAvgPlayTime;
                wasFixed = true;
            }
            
            int newAvgScore = totalScore / totalGamesPlayed;
            if (averageScore != newAvgScore)
            {
                averageScore = newAvgScore;
                wasFixed = true;
            }
            
            float newAvgSurvival = totalSurvivalTime / totalGamesPlayed;
            if (Mathf.Abs(averageSurvivalTime - newAvgSurvival) > 0.01f)
            {
                averageSurvivalTime = newAvgSurvival;
                wasFixed = true;
            }
            
            float newAvgObstacles = totalGamesPlayed > 0 ? (float)totalObstaclesAvoided / totalGamesPlayed : 0f;
            if (Mathf.Abs(averageObstaclesPerGame - newAvgObstacles) > 0.01f)
            {
                averageObstaclesPerGame = newAvgObstacles;
                wasFixed = true;
            }
        }
        
        return wasFixed;
    }
}
