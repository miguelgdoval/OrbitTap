using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Difficulty")]
    public float difficultyIncreaseInterval = 7f;
    public float speedMultiplier = 1.05f;

    private PlayerOrbit player;
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
        
        // Buscar el player después de que GameInitializer lo haya creado
        StartCoroutine(FindPlayerDelayed());
    }

    private System.Collections.IEnumerator FindPlayerDelayed()
    {
        // Esperar un frame para que GameInitializer termine de crear todo
        yield return null;
        
        player = FindObjectOfType<PlayerOrbit>();
        if (player == null)
        {
            Debug.LogWarning("GameManager: PlayerOrbit not found! Retrying...");
            // Intentar de nuevo después de otro frame
            yield return null;
            player = FindObjectOfType<PlayerOrbit>();
            if (player == null)
            {
                Debug.LogError("GameManager: PlayerOrbit still not found after retry!");
            }
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        timeSinceLastIncrease += Time.deltaTime;

        if (timeSinceLastIncrease >= difficultyIncreaseInterval)
        {
            IncreaseDifficulty();
            timeSinceLastIncrease = 0f;
        }
    }

    private void IncreaseDifficulty()
    {
        if (player != null)
        {
            player.angularSpeed *= speedMultiplier;
        }
    }

    public void GameOver()
    {
        if (isGameOver)
        {
            Debug.Log("GameManager: GameOver ya fue llamado, ignorando...");
            return;
        }

        Debug.Log("GameManager: GameOver() llamado");
        isGameOver = true;
        
        // Detener la puntuación y guardar
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager != null)
        {
            Debug.Log("GameManager: Deteniendo puntuación...");
            scoreManager.StopScoring();
            Debug.Log("GameManager: Guardando puntuación...");
            scoreManager.SaveHighScore();
        }
        else
        {
            Debug.LogWarning("GameManager: ScoreManager no encontrado!");
        }

        // Esperar un poco para que la animación de destrucción se complete antes de cargar la escena
        StartCoroutine(LoadGameOverSceneDelayed());
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
        Debug.Log("GameManager: Limpiando elementos de la escena Game...");
        
        // Destruir CosmicBackground de la escena Game
        GameObject cosmicBg = GameObject.Find("CosmicBackground");
        if (cosmicBg != null)
        {
            Debug.Log("GameManager: Destruyendo CosmicBackground...");
            Destroy(cosmicBg);
        }
        
        // Limpiar BackgroundManager y sus capas
        BackgroundManager bgManager = FindObjectOfType<BackgroundManager>();
        if (bgManager != null)
        {
            Debug.Log("GameManager: Limpiando BackgroundManager...");
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
                Debug.Log($"GameManager: Destruyendo objeto: {name}");
                Destroy(obj);
            }
        }
        
        Debug.Log("GameManager: Limpieza completada");
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("Game");
    }
}

