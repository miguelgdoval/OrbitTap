using UnityEngine;
using UnityEngine.SceneManagement;
using static LogHelper;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Difficulty")]
    [Tooltip("Si está vacío, se usará GameConfig. Si está asignado, se usará este valor.")]
    public float difficultyIncreaseInterval = 0f; // 0 = usar GameConfig
    [Tooltip("Si está vacío, se usará GameConfig. Si está asignado, se usará este valor.")]
    public float speedMultiplier = 0f; // 0 = usar GameConfig

    private PlayerOrbit player;
    private ScoreManager scoreManager; // Cacheado para evitar FindObjectOfType
    private float timeSinceLastIncrease = 0f;
    private bool isGameOver = false;

    private void Awake()
    {
        if (!Application.isPlaying) return;
        
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        
        // Asegurar que Time.timeScale esté en 1 al inicio del juego
        if (Time.timeScale == 0f)
        {
            LogWarning("GameManager: Time.timeScale estaba en 0, restaurando a 1");
            Time.timeScale = 1f;
        }
        
        // Buscar el player después de que GameInitializer lo haya creado
        StartCoroutine(FindPlayerDelayed());
    }

    private System.Collections.IEnumerator FindPlayerDelayed()
    {
        // Esperar un frame para que GameInitializer termine de crear todo
        yield return null;
        
        player = FindFirstObjectByType<PlayerOrbit>();
        if (player == null)
        {
            LogWarning("GameManager: PlayerOrbit not found! Retrying...");
            // Intentar de nuevo después de otro frame
            yield return null;
            player = FindFirstObjectByType<PlayerOrbit>();
            if (player == null)
            {
                LogError("GameManager: PlayerOrbit still not found after retry!");
            }
        }
        
        // Cachear ScoreManager también
        scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager == null)
        {
            LogWarning("GameManager: ScoreManager not found! Will try to find it in GameOver()");
        }
        
        // Iniciar tutorial si es necesario (INMEDIATAMENTE, antes de que ObstacleManager pueda spawnear)
        // No esperar frames - iniciar inmediatamente
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.StartTutorialIfNeeded();
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        timeSinceLastIncrease += Time.deltaTime;
        
        // Usar GameConfig si el valor local es 0
        float interval = difficultyIncreaseInterval > 0 ? difficultyIncreaseInterval : 
                        (GameConfig.Instance != null ? GameConfig.Instance.difficultyIncreaseInterval : 7f);

        if (timeSinceLastIncrease >= interval)
        {
            IncreaseDifficulty();
            timeSinceLastIncrease = 0f;
        }
    }
    
    private void IncreaseDifficulty()
    {
        if (player != null)
        {
            // Usar GameConfig si el valor local es 0
            float multiplier = speedMultiplier > 0 ? speedMultiplier : 
                              (GameConfig.Instance != null ? GameConfig.Instance.speedMultiplier : 1.05f);
            player.angularSpeed *= multiplier;
        }
    }

    public void GameOver()
    {
        if (isGameOver)
        {
            Log("GameManager: GameOver ya fue llamado, ignorando...");
            return;
        }

        Log("GameManager: GameOver() llamado");
        isGameOver = true;
        
        // Detener la puntuación (pero NO guardar aún - puede haber revive)
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }
        
        if (scoreManager != null)
        {
            scoreManager.StopScoring();
        }
        
        // Detener spawning de coleccionables (pero mantener los datos de sesión)
        if (CollectibleManager.Instance != null)
        {
            CollectibleManager.Instance.StopSpawning();
        }
        
        // Detener spawning de power-ups
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.StopSpawning();
        }
        
        // Comprobar si el jugador puede usar segunda oportunidad
        if (ReviveManager.Instance != null && ReviveManager.Instance.CanRevive())
        {
            Log("GameManager: Revive disponible, mostrando opción...");
            ReviveManager.Instance.ShowReviveOption(
                onAccepted: () => OnReviveAccepted(),
                onDeclined: () => OnReviveDeclined()
            );
        }
        else
        {
            // Sin revive disponible, game over directo
            FinalizeGameOver();
        }
    }
    
    /// <summary>
    /// Se llama cuando el jugador acepta revivir
    /// </summary>
    private void OnReviveAccepted()
    {
        Log("GameManager: Revive aceptado, reanudando partida...");
        isGameOver = false;
        
        // Reanudar scoring
        if (scoreManager != null)
        {
            scoreManager.ResumeScoring();
        }
        
        // Reanudar coleccionables
        if (CollectibleManager.Instance != null)
        {
            CollectibleManager.Instance.StartSpawning();
        }
        
        // Reanudar power-ups
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.ResumeSpawning();
        }
        
        // Buscar nuevo player (fue recreado por ReviveManager)
        StartCoroutine(FindPlayerDelayed());
    }
    
    /// <summary>
    /// Se llama cuando el jugador declina revivir
    /// </summary>
    private void OnReviveDeclined()
    {
        Log("GameManager: Revive declinado, procediendo con GameOver...");
        FinalizeGameOver();
    }
    
    /// <summary>
    /// Finaliza el GameOver: guarda puntuación, analytics, carga escena
    /// </summary>
    private void FinalizeGameOver()
    {
        float playTime = 0f;
        int score = 0;
        int highScore = 0;
        
        if (scoreManager != null)
        {
            Log("GameManager: Guardando puntuación...");
            score = scoreManager.GetCurrentScore();
            highScore = scoreManager.GetHighScore();
            playTime = Time.timeSinceLevelLoad;
            scoreManager.SaveHighScore();
        }
        else
        {
            LogWarning("GameManager: ScoreManager no encontrado!");
        }
        
        // Analytics: Registrar fin de partida
        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.TrackGameOver(score, highScore, playTime);
        }
        
        // Statistics: Finalizar rastreo de estadísticas
        if (StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.EndTrackingGame();
        }
        
        // Power-ups: Detener spawning y resetear
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.ResetSession();
        }
        
        // Coleccionables: Detener spawning y otorgar ganancias
        if (CollectibleManager.Instance != null)
        {
            CollectibleManager.Instance.StopSpawning();
            CollectibleManager.Instance.AwardSessionEarnings();
            
            // Guardar datos de sesión en PlayerPrefs para que GameOverController los muestre
            PlayerPrefs.SetInt("LastShardsCollected", CollectibleManager.Instance.TotalShardsCollected);
            PlayerPrefs.SetInt("LastShardsValue", CollectibleManager.Instance.TotalValueCollected);
        }

        // Esperar un poco para que la animación de destrucción se complete antes de cargar la escena
        StartCoroutine(LoadGameOverSceneDelayed());
    }
    
    /// <summary>
    /// Se llama cuando se inicia una nueva partida
    /// </summary>
    public void OnGameStart()
    {
        // Reportar que se inició una partida
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.ReportProgress(MissionObjectiveType.PlayGames);
        }
        
        // Analytics: Registrar inicio de partida
        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.TrackGameStart();
        }
        
        // Statistics: Iniciar rastreo de estadísticas
        if (StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.StartTrackingGame();
        }
        
        // Resetear estado de revive para la nueva partida
        if (ReviveManager.Instance != null)
        {
            ReviveManager.Instance.ResetForNewGame();
        }
        
        // Resetear e iniciar coleccionables (Stellar Shards en la órbita)
        if (CollectibleManager.Instance != null)
        {
            CollectibleManager.Instance.ResetSession();
            CollectibleManager.Instance.StartSpawning();
        }
        
        // Resetear e iniciar power-ups
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.ResetSession();
            PowerUpManager.Instance.StartSpawning();
        }
    }
    
    private System.Collections.IEnumerator LoadGameOverSceneDelayed()
    {
        // Esperar 1 segundo para que la animación de destrucción se complete
        yield return new WaitForSeconds(1f);
        
        // Load game over scene
        Log("GameManager: Cargando escena GameOver...");
        try
        {
            SceneManager.LoadScene("GameOver");
            Log("GameManager: Escena GameOver cargada exitosamente");
        }
        catch (System.Exception e)
        {
            LogError("GameManager: Error al cargar escena GameOver: " + e.Message);
        }
    }
    
    public static void CleanupGameScene()
    {
        Log("GameManager: Limpiando elementos de la escena Game...");
        
        // Destruir CosmicBackground de la escena Game
        GameObject cosmicBg = GameObject.Find("CosmicBackground");
        if (cosmicBg != null)
        {
            Log("GameManager: Destruyendo CosmicBackground...");
            Destroy(cosmicBg);
        }
        
        // Limpiar BackgroundManager y sus capas
        BackgroundManager bgManager = FindFirstObjectByType<BackgroundManager>();
        if (bgManager != null)
        {
            Log("GameManager: Limpiando BackgroundManager...");
            // Destruir el BackgroundManager completo (incluye todas las capas)
            Destroy(bgManager.gameObject);
        }
        
        // Buscar objetos por nombre que puedan ser parte del sistema de fondo
        string[] backgroundNames = { "BackgroundManager", "LayerBase", "LayerNebulas", "LayerStarsFar", "LayerStarsNear", "LayerParticles" };
        foreach (string name in backgroundNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                Log($"GameManager: Destruyendo objeto: {name}");
                Destroy(obj);
            }
        }
        
        Log("GameManager: Limpieza completada");
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("Game");
    }
}

