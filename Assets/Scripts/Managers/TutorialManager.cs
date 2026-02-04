using UnityEngine;
using System.Collections;
using static LogHelper;

/// <summary>
/// Manager para el sistema de tutorial/onboarding del juego
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    
    [Header("Tutorial Settings")]
    [Tooltip("Habilitar tutorial automáticamente para nuevos jugadores")]
    [SerializeField] private bool enableTutorial = true;
    
    [Tooltip("Tiempo de espera antes de mostrar cada paso (en segundos)")]
    [SerializeField] private float stepDelay = 1f;
    
    private const string TUTORIAL_COMPLETED_KEY = "TutorialCompleted";
    
    private bool isTutorialActive = false;
    private int currentStep = 0;
    private TutorialUI tutorialUI;
    private InputController inputController; // Para deshabilitar controles durante el tutorial
    private PlayerOrbit playerOrbit; // Para pausar el movimiento del jugador
    private ObstacleManager obstacleManager; // Para pausar el spawn de obstáculos
    private ScoreManager scoreManager; // Para pausar el score
    private bool wasPlayerOrbitEnabled = false;
    private bool wasObstacleManagerEnabled = false;
    private bool wasScoreManagerEnabled = false;
    
    // Eventos
    public System.Action OnTutorialStarted;
    public System.Action OnTutorialCompleted;
    public System.Action OnTutorialSkipped;
    
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
    
    /// <summary>
    /// Verifica si el tutorial ya fue completado
    /// </summary>
    public bool IsTutorialCompleted()
    {
        // Usar SaveDataManager si está disponible
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                // Por ahora, guardamos en PlayerPrefs hasta que agreguemos el campo a SaveData
                return PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
            }
        }
        
        return PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
    }
    
    /// <summary>
    /// Marca el tutorial como completado
    /// </summary>
    public void MarkTutorialCompleted()
    {
        PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 1);
        PlayerPrefs.Save();
        
        // También marcar en SaveDataManager si está disponible
        if (SaveDataManager.Instance != null)
        {
            SaveDataManager.Instance.MarkDirty();
        }
        
        Log("[TutorialManager] Tutorial marcado como completado");
    }
    
    /// <summary>
    /// Inicia el tutorial si es necesario
    /// </summary>
    public void StartTutorialIfNeeded()
    {
        Log("[TutorialManager] StartTutorialIfNeeded() llamado");
        
        if (!enableTutorial)
        {
            Log("[TutorialManager] Tutorial deshabilitado (enableTutorial = false)");
            return;
        }
        
        if (isTutorialActive)
        {
            LogWarning("[TutorialManager] Tutorial ya está activo");
            return;
        }
        
        // Verificar si el tutorial está habilitado en configuración
        // Este es el único check necesario - si está habilitado, se muestra
        bool tutorialEnabledInSettings = true;
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                tutorialEnabledInSettings = saveData.tutorialEnabled;
                Log($"[TutorialManager] Tutorial habilitado en configuración: {tutorialEnabledInSettings}");
            }
            else
            {
                LogWarning("[TutorialManager] SaveData es null");
            }
        }
        else
        {
            // Fallback a PlayerPrefs
            tutorialEnabledInSettings = PlayerPrefs.GetInt("TutorialEnabled", 1) == 1;
            Log($"[TutorialManager] Usando PlayerPrefs, tutorial habilitado: {tutorialEnabledInSettings}");
        }
        
        if (!tutorialEnabledInSettings) 
        {
            Log("[TutorialManager] Tutorial deshabilitado en configuración del usuario");
            return;
        }
        
        Log("[TutorialManager] Iniciando tutorial...");
        StartTutorial();
    }
    
    /// <summary>
    /// Fuerza el inicio del tutorial (ignora si ya fue completado)
    /// </summary>
    public void ForceStartTutorial()
    {
        if (isTutorialActive)
        {
            LogWarning("[TutorialManager] Tutorial ya está activo");
            return;
        }
        
        Log("[TutorialManager] Forzando inicio del tutorial...");
        StartTutorial();
    }
    
    /// <summary>
    /// Inicia el tutorial
    /// </summary>
    public void StartTutorial()
    {
        if (isTutorialActive)
        {
            LogWarning("[TutorialManager] Tutorial ya está activo");
            return;
        }
        
        isTutorialActive = true;
        currentStep = 0;
        
        Log("[TutorialManager] Iniciando tutorial...");
        
        // PRIMERO: Pausar componentes del juego ANTES de crear la UI
        // Esto evita que se generen obstáculos mientras se muestra el tutorial
        PauseGameComponents();
        
        // Esperar un frame para asegurar que todo esté pausado
        StartCoroutine(StartTutorialAfterPause());
    }
    
    /// <summary>
    /// Inicia el tutorial después de pausar los componentes
    /// </summary>
    private System.Collections.IEnumerator StartTutorialAfterPause()
    {
        yield return null; // Esperar un frame para asegurar que todo esté pausado
        
        // Crear UI del tutorial
        CreateTutorialUI();
        
        OnTutorialStarted?.Invoke();
        
        // Iniciar el primer paso
        StartCoroutine(ShowTutorialStep(0));
    }
    
    /// <summary>
    /// Crea la UI del tutorial
    /// </summary>
    private void CreateTutorialUI()
    {
        if (tutorialUI != null)
        {
            Destroy(tutorialUI.gameObject);
        }
        
        GameObject tutorialObj = new GameObject("TutorialUI");
        tutorialUI = tutorialObj.AddComponent<TutorialUI>();
        tutorialObj.transform.SetParent(transform);
    }
    
    /// <summary>
    /// Muestra un paso específico del tutorial
    /// </summary>
    private IEnumerator ShowTutorialStep(int stepIndex)
    {
        currentStep = stepIndex;
        
        // Esperar un poco antes de mostrar el paso
        yield return new WaitForSecondsRealtime(stepDelay);
        
        if (tutorialUI == null)
        {
            LogError("[TutorialManager] TutorialUI es null, no se puede mostrar el paso");
            yield break;
        }
        
        switch (stepIndex)
        {
            case 0:
                // Paso 1: Bienvenida y objetivo
                tutorialUI.ShowStep(
                    "¡Bienvenido a Starbound Orbit!",
                    "Tu objetivo es sobrevivir el mayor tiempo posible orbitando alrededor del centro.",
                    "Continuar"
                );
                break;
                
            case 1:
                // Paso 2: Cómo moverse
                tutorialUI.ShowStep(
                    "Cómo moverse",
                    "Toca la pantalla para cambiar la dirección de rotación. El planeta cambiará de sentido.",
                    "Entendido"
                );
                break;
                
            case 2:
                // Paso 3: Obstáculos
                tutorialUI.ShowStep(
                    "¡Cuidado con los obstáculos!",
                    "Evita chocar con los obstáculos que aparecen. Si chocas, el juego termina.",
                    "Continuar"
                );
                break;
                
            case 3:
                // Paso 4: Puntuación
                tutorialUI.ShowStep(
                    "Puntuación",
                    "Tu puntuación aumenta con el tiempo. ¡Intenta batir tu récord!",
                    "¡A jugar!"
                );
                break;
                
            default:
                // Tutorial completado
                CompleteTutorial();
                yield break;
        }
    }
    
    /// <summary>
    /// Avanza al siguiente paso del tutorial
    /// </summary>
    public void NextStep()
    {
        if (!isTutorialActive) return;
        
        int nextStep = currentStep + 1;
        
        if (nextStep >= 4) // 4 pasos en total (0-3)
        {
            CompleteTutorial();
        }
        else
        {
            StartCoroutine(ShowTutorialStep(nextStep));
        }
    }
    
    /// <summary>
    /// Salta el tutorial
    /// </summary>
    public void SkipTutorial()
    {
        if (!isTutorialActive) return;
        
        Log("[TutorialManager] Tutorial saltado por el jugador");
        
        MarkTutorialCompleted();
        isTutorialActive = false;
        
        // Desactivar el tutorial automáticamente cuando se salta
        DisableTutorialInSettings();
        
        // Ocultar UI
        if (tutorialUI != null)
        {
            tutorialUI.Hide();
        }
        
        // Rehabilitar componentes del juego
        ResumeGameComponents();
        
        OnTutorialSkipped?.Invoke();
    }
    
    /// <summary>
    /// Completa el tutorial
    /// </summary>
    private void CompleteTutorial()
    {
        if (!isTutorialActive) return;
        
        Log("[TutorialManager] Tutorial completado");
        
        MarkTutorialCompleted();
        isTutorialActive = false;
        
        // Desactivar el tutorial automáticamente cuando se completa
        DisableTutorialInSettings();
        
        // Ocultar UI
        if (tutorialUI != null)
        {
            tutorialUI.Hide();
        }
        
        // Rehabilitar componentes del juego
        ResumeGameComponents();
        
        OnTutorialCompleted?.Invoke();
    }
    
    /// <summary>
    /// Desactiva el tutorial en la configuración
    /// </summary>
    private void DisableTutorialInSettings()
    {
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                saveData.tutorialEnabled = false;
                SaveDataManager.Instance.MarkDirty();
                Log("[TutorialManager] Tutorial desactivado automáticamente en configuración");
            }
        }
        else
        {
            PlayerPrefs.SetInt("TutorialEnabled", 0);
            PlayerPrefs.Save();
        }
    }
    
    /// <summary>
    /// Verifica si el tutorial está activo
    /// </summary>
    public bool IsTutorialActive()
    {
        return isTutorialActive;
    }
    
    /// <summary>
    /// Pausa los componentes del juego durante el tutorial
    /// </summary>
    private void PauseGameComponents()
    {
        // PRIMERO: Pausar spawn de obstáculos ANTES que nada para evitar que se generen
        obstacleManager = FindFirstObjectByType<ObstacleManager>();
        if (obstacleManager != null)
        {
            wasObstacleManagerEnabled = obstacleManager.enabled;
            obstacleManager.enabled = false;
            
            // Detener todas las coroutines del ObstacleManager
            obstacleManager.StopAllCoroutines();
            
            // Limpiar todos los obstáculos existentes
            ClearAllObstacles();
            
            Log("[TutorialManager] ObstacleManager pausado, coroutines detenidas y obstáculos limpiados");
        }
        
        // Deshabilitar controles
        inputController = FindFirstObjectByType<InputController>();
        if (inputController != null)
        {
            inputController.enabled = false;
            Log("[TutorialManager] InputController deshabilitado");
        }
        
        // Pausar movimiento del jugador
        playerOrbit = FindFirstObjectByType<PlayerOrbit>();
        if (playerOrbit != null)
        {
            wasPlayerOrbitEnabled = playerOrbit.enabled;
            playerOrbit.enabled = false;
            Log("[TutorialManager] PlayerOrbit pausado");
        }
        
        // Pausar score
        scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager != null)
        {
            wasScoreManagerEnabled = scoreManager.enabled;
            scoreManager.enabled = false;
            Log("[TutorialManager] ScoreManager pausado");
        }
    }
    
    /// <summary>
    /// Reanuda los componentes del juego
    /// </summary>
    private void ResumeGameComponents()
    {
        // Rehabilitar controles
        if (inputController != null)
        {
            inputController.enabled = true;
        }
        else
        {
            inputController = FindFirstObjectByType<InputController>();
            if (inputController != null)
            {
                inputController.enabled = true;
            }
        }
        
        // Reanudar movimiento del jugador
        if (playerOrbit != null)
        {
            playerOrbit.enabled = wasPlayerOrbitEnabled;
        }
        else
        {
            playerOrbit = FindFirstObjectByType<PlayerOrbit>();
            if (playerOrbit != null)
            {
                playerOrbit.enabled = true;
            }
        }
        
        // Reanudar spawn de obstáculos
        if (obstacleManager != null)
        {
            // Siempre reactivar el ObstacleManager cuando termina el tutorial
            obstacleManager.enabled = true;
            Log($"[TutorialManager] ObstacleManager reactivado (enabled: {obstacleManager.enabled})");
            
            // Resetear el flag firstObstacleSpawned para permitir que se spawnee el primer obstáculo
            obstacleManager.ResetFirstObstacleSpawned();
            Log("[TutorialManager] ResetFirstObstacleSpawned() llamado");
        }
        else
        {
            obstacleManager = FindFirstObjectByType<ObstacleManager>();
            if (obstacleManager != null)
            {
                obstacleManager.enabled = true;
                obstacleManager.ResetFirstObstacleSpawned();
                Log("[TutorialManager] ObstacleManager encontrado, reactivado y ResetFirstObstacleSpawned() llamado");
            }
            else
            {
                LogWarning("[TutorialManager] ObstacleManager no encontrado al intentar reactivar");
            }
        }
        
        // Reanudar score
        if (scoreManager != null)
        {
            scoreManager.enabled = wasScoreManagerEnabled;
        }
        else
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
            if (scoreManager != null)
            {
                scoreManager.enabled = true;
            }
        }
        
        Log("[TutorialManager] Componentes del juego reanudados");
    }
    
    /// <summary>
    /// Limpia todos los obstáculos existentes en pantalla
    /// </summary>
    private void ClearAllObstacles()
    {
        if (obstacleManager == null) return;
        
        // Buscar todos los obstáculos en la escena
        ObstacleDestructionController[] allObstacles = FindObjectsByType<ObstacleDestructionController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        int clearedCount = 0;
        foreach (var obstacle in allObstacles)
        {
            if (obstacle != null && obstacle.gameObject != null)
            {
                // Devolver al pool en lugar de destruir
                obstacleManager.ReturnToPool(obstacle.gameObject);
                clearedCount++;
            }
        }
        
        if (clearedCount > 0)
        {
            Log($"[TutorialManager] {clearedCount} obstáculos limpiados de la pantalla");
        }
    }
    
    /// <summary>
    /// Reinicia el tutorial (útil para testing)
    /// </summary>
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TUTORIAL_COMPLETED_KEY);
        PlayerPrefs.Save();
        Log("[TutorialManager] Tutorial reiniciado");
    }
}
