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
            Debug.LogWarning("GameManager: PlayerOrbit not found! Retrying...");
            // Intentar de nuevo después de otro frame
            yield return null;
            player = FindFirstObjectByType<PlayerOrbit>();
            if (player == null)
            {
                Debug.LogError("GameManager: PlayerOrbit still not found after retry!");
            }
        }
        
        // Cachear ScoreManager también
        scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager == null)
        {
            LogWarning("GameManager: ScoreManager not found! Will try to find it in GameOver()");
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
        
        // Detener la puntuación y guardar (usar referencia cacheada)
        float playTime = 0f;
        int score = 0;
        int highScore = 0;
        
        // Si no está cacheado, intentar encontrarlo una vez
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }
        
        if (scoreManager != null)
        {
            Log("GameManager: Deteniendo puntuación...");
            score = scoreManager.GetCurrentScore();
            highScore = scoreManager.GetHighScore();
            playTime = Time.timeSinceLevelLoad; // Tiempo de juego aproximado
            scoreManager.StopScoring();
            Log("GameManager: Guardando puntuación...");
            scoreManager.SaveHighScore();
        }
        else
        {
            Debug.LogWarning("GameManager: ScoreManager no encontrado!");
        }
        
        // Analytics: Registrar fin de partida
        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.TrackGameOver(score, highScore, playTime);
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
    }
    
    private System.Collections.IEnumerator LoadGameOverSceneDelayed()
    {
        // Esperar 1 segundo para que la animación de destrucción se complete
        yield return new WaitForSeconds(1f);
        
        // Load game over scene
        Debug.Log("GameManager: Cargando escena GameOver...");
        try
        {
            SceneManager.LoadScene("GameOver");
            Debug.Log("GameManager: Escena GameOver cargada exitosamente");
        }
        catch (System.Exception e)
        {
            Debug.LogError("GameManager: Error al cargar escena GameOver: " + e.Message);
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

