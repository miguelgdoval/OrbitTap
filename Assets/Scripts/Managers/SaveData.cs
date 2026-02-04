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
            removeAdsPurchased = this.removeAdsPurchased
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
        
        return wasFixed;
    }
}
