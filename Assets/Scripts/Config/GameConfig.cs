using UnityEngine;

/// <summary>
/// Configuración centralizada del juego usando ScriptableObject
/// Permite ajustar valores sin modificar código
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Starbound Orbit/Game Config", order = 1)]
public class GameConfig : ScriptableObject
{
    [Header("Difficulty Settings")]
    [Tooltip("Intervalo entre aumentos de dificultad (en segundos)")]
    public float difficultyIncreaseInterval = 7f;
    
    [Tooltip("Multiplicador de velocidad por aumento de dificultad")]
    public float speedMultiplier = 1.05f;
    
    [Header("Obstacle Spawn Settings")]
    [Tooltip("Intervalo mínimo entre spawns de obstáculos")]
    public float minSpawnInterval = 2f;
    
    [Tooltip("Intervalo máximo entre spawns de obstáculos")]
    public float maxSpawnInterval = 4f;
    
    [Tooltip("Intervalo mínimo al máximo de dificultad")]
    public float minSpawnIntervalMin = 0.5f;
    
    [Tooltip("Intervalo máximo al máximo de dificultad")]
    public float maxSpawnIntervalMin = 1.0f;
    
    [Tooltip("Tasa de aumento de dificultad por segundo")]
    public float difficultyIncreaseRate = 0.02f;
    
    [Tooltip("Cada cuánto actualizar la dificultad (en segundos)")]
    public float difficultyUpdateInterval = 1f;
    
    [Tooltip("Tiempo para alcanzar dificultad máxima (en segundos)")]
    public float maxDifficultyTime = 120f;
    
    [Header("Obstacle Movement")]
    [Tooltip("Velocidad base de los obstáculos")]
    public float obstacleSpeed = 3f;
    
    [Tooltip("Distancia desde la pantalla para spawnear")]
    public float spawnDistanceFromScreen = 12f;
    
    [Tooltip("Variación de velocidad (1.0x a este valor)")]
    public float speedVariation = 1.2f;
    
    [Tooltip("Variación de tamaño (1.0x a este valor)")]
    public float sizeVariation = 2.0f;
    
    [Tooltip("Máximo número de obstáculos en pantalla")]
    public int maxObstaclesOnScreen = 3;
    
    [Header("Breathing Room System")]
    [Tooltip("Duración de pausa después de near miss (en segundos)")]
    public float breathingRoomDuration = 1.5f;
    
    [Tooltip("Distancia mínima para considerar near miss")]
    public float nearMissDistance = 0.8f;
    
    [Tooltip("Multiplicador de intervalo cuando hay peligro")]
    public float dangerSpawnMultiplier = 1.5f;
    
    [Header("Object Pooling")]
    [Tooltip("Tamaño inicial del pool para cada tipo de obstáculo")]
    public int poolInitialSize = 5;
    
    [Tooltip("Tamaño máximo del pool (0 = ilimitado)")]
    public int poolMaxSize = 20;
    
    [Header("Score Settings")]
    [Tooltip("Puntos para desbloquear dificultad Medium")]
    public float scoreToUnlockMedium = 15f;
    
    [Tooltip("Puntos para desbloquear dificultad Hard")]
    public float scoreToUnlockHard = 30f;
    
    [Tooltip("Puntos para desbloquear dificultad VeryHard")]
    public float scoreToUnlockVeryHard = 60f;
    
    [Header("Time-based Unlocks (si useScoreBasedDifficulty es false)")]
    [Tooltip("Tiempo para desbloquear dificultad Medium (en segundos)")]
    public float timeToUnlockMedium = 20f;
    
    [Tooltip("Tiempo para desbloquear dificultad Hard (en segundos)")]
    public float timeToUnlockHard = 50f;
    
    [Tooltip("Tiempo para desbloquear dificultad VeryHard (en segundos)")]
    public float timeToUnlockVeryHard = 90f;
    
    [Header("Ad Settings")]
    [Tooltip("Partidas entre anuncios")]
    public int gamesBetweenAds = 3;
    
    [Tooltip("Tiempo mínimo entre anuncios (en segundos)")]
    public float minTimeBetweenAds = 180f;
    
    [Tooltip("Puntuación mínima para mostrar anuncio")]
    public int minGameScore = 15;
    
    [Tooltip("Partidas para forzar anuncio (ignora minGameScore)")]
    public int gamesToForceAd = 5;
    
    [Header("Pause Settings")]
    [Tooltip("Pausar automáticamente cuando la app va a background")]
    public bool autoPauseOnBackground = true;
    
    [Tooltip("Guardar estado automáticamente al pausar")]
    public bool autoSaveOnPause = true;
    
    /// <summary>
    /// Instancia estática del config (se carga automáticamente)
    /// </summary>
    private static GameConfig _instance;
    public static GameConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameConfig>("Config/GameConfig");
                if (_instance == null)
                {
                    Debug.LogWarning("[GameConfig] No se encontró GameConfig en Resources/Config/GameConfig. Usando valores por defecto.");
                    _instance = CreateInstance<GameConfig>();
                }
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Carga el config desde Resources
    /// </summary>
    public static void LoadConfig()
    {
        _instance = Resources.Load<GameConfig>("Config/GameConfig");
        if (_instance == null)
        {
            Debug.LogWarning("[GameConfig] No se encontró GameConfig. Creando uno con valores por defecto.");
            _instance = CreateInstance<GameConfig>();
        }
    }
}
