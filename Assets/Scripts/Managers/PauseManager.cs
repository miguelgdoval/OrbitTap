using UnityEngine;
using static LogHelper;

/// <summary>
/// Manager para manejar pausa/resume del juego, especialmente cuando la app va a background
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    
    private bool isPaused = false;
    private bool wasPausedByAd = false; // Para distinguir entre pausa por anuncio y pausa por background
    private float savedTimeScale = 1f;
    
    // Eventos
    public System.Action OnGamePaused;
    public System.Action OnGameResumed;
    
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
    
    private void OnApplicationPause(bool pauseStatus)
    {
        #if UNITY_ANDROID || UNITY_IOS
        if (pauseStatus)
        {
            // App fue a background
            PauseGame(false); // false = no fue por anuncio
        }
        else
        {
            // App volvió a foreground
            ResumeGame(false); // false = no fue por anuncio
        }
        #endif
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        #if UNITY_ANDROID || UNITY_IOS
        if (!hasFocus)
        {
            // App perdió foco
            PauseGame(false);
        }
        else
        {
            // App recuperó foco
            ResumeGame(false);
        }
        #endif
    }
    
    /// <summary>
    /// Pausa el juego
    /// </summary>
    /// <param name="byAd">True si la pausa es causada por un anuncio</param>
    public void PauseGame(bool byAd = false)
    {
        if (isPaused) return;
        
        // Guardar el timeScale actual antes de pausar
        savedTimeScale = Time.timeScale;
        
        // Si es pausa por anuncio, el AdManager ya puso timeScale a 0
        if (byAd)
        {
            wasPausedByAd = true;
        }
        else
        {
            // Pausa por background - pausar el juego
            Time.timeScale = 0f;
            wasPausedByAd = false;
        }
        
        isPaused = true;
        
        // Guardar estado del juego si es necesario
        SaveGameState();
        
        Log($"[PauseManager] Juego pausado (por anuncio: {byAd})");
        OnGamePaused?.Invoke();
    }
    
    /// <summary>
    /// Reanuda el juego
    /// </summary>
    /// <param name="byAd">True si la reanudación es causada por un anuncio</param>
    public void ResumeGame(bool byAd = false)
    {
        if (!isPaused) return;
        
        // Si fue pausado por anuncio y ahora se reanuda por anuncio, no hacer nada
        // (el AdManager manejará el timeScale)
        if (wasPausedByAd && byAd)
        {
            wasPausedByAd = false;
            isPaused = false;
            Log("[PauseManager] Juego reanudado (anuncio completado)");
            OnGameResumed?.Invoke();
            return;
        }
        
        // Si fue pausado por background, restaurar timeScale
        if (!wasPausedByAd)
        {
            Time.timeScale = savedTimeScale > 0 ? savedTimeScale : 1f;
        }
        
        isPaused = false;
        wasPausedByAd = false;
        
        Log($"[PauseManager] Juego reanudado (timeScale: {Time.timeScale})");
        OnGameResumed?.Invoke();
    }
    
    /// <summary>
    /// Guarda el estado del juego antes de pausar
    /// </summary>
    private void SaveGameState()
    {
        // Guardar puntuación actual si estamos en juego
        ScoreManager scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager != null)
        {
            scoreManager.SaveHighScore();
        }
        
        // Guardar progreso de misiones
        if (MissionManager.Instance != null)
        {
            // Las misiones se guardan automáticamente, pero forzamos un guardado
            PlayerPrefs.Save();
        }
        
        Log("[PauseManager] Estado del juego guardado");
    }
    
    /// <summary>
    /// Verifica si el juego está pausado
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
}
