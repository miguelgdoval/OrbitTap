using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    [Header("UI Settings")]
    public bool createUIAtRuntime = true;
    
    private Text scoreText;
    private Text highScoreText;

    private void Awake()
    {
        Debug.Log("GameOverController: Awake() llamado");
    }

    private void Start()
    {
        Debug.Log("GameOverController: Start() llamado - createUIAtRuntime = " + createUIAtRuntime);
        
        // Configurar fondo cósmico
        if (GetComponent<CosmicBackground>() == null)
        {
            gameObject.AddComponent<CosmicBackground>();
        }
        
        // Forzar creación de UI si no existe
        if (createUIAtRuntime || GameObject.Find("Canvas") == null)
        {
            Debug.Log("GameOverController: Creando UI...");
            CreateUI();
        }
        else
        {
            Debug.Log("GameOverController: Canvas ya existe, buscando componentes...");
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                scoreText = GameObject.Find("ScoreText")?.GetComponent<Text>();
                highScoreText = GameObject.Find("HighScoreText")?.GetComponent<Text>();
            }
        }
        
        Debug.Log("GameOverController: Actualizando UI...");
        UpdateUI();
        
        // Verificar si se debe mostrar anuncio intersticial (con lógica inteligente)
        CheckAndShowInterstitialAd();
        
        Debug.Log("GameOverController: Inicialización completa");
    }
    
    /// <summary>
    /// Verifica las condiciones y muestra un anuncio intersticial si corresponde
    /// </summary>
    private void CheckAndShowInterstitialAd()
    {
        if (AdManager.Instance == null)
        {
            Debug.LogWarning("[GameOverController] AdManager.Instance es null");
            return;
        }
        
        // Obtener puntuación de la partida (el score es igual al tiempo sobrevivido en segundos)
        int gameScore = PlayerPrefs.GetInt("LastScore", 0);
        Debug.Log($"[GameOverController] Verificando anuncio. Puntuación: {gameScore}");
        
        // Verificar si se debe mostrar el anuncio
        if (AdManager.Instance.ShouldShowInterstitialAd(gameScore))
        {
            Debug.Log("[GameOverController] Condiciones cumplidas, programando anuncio para mostrar en 1 segundo...");
            // Mostrar anuncio después de un pequeño delay
            StartCoroutine(ShowAdAfterDelay(1f));
        }
        else
        {
            Debug.Log("[GameOverController] No se mostrará anuncio (condiciones no cumplidas)");
        }
    }
    
    /// <summary>
    /// Muestra un anuncio intersticial después de un delay
    /// </summary>
    private System.Collections.IEnumerator ShowAdAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (AdManager.Instance != null)
        {
            Debug.Log("[GameOverController] Intentando mostrar anuncio intersticial...");
            AdManager.Instance.ShowInterstitialAd();
        }
        else
        {
            Debug.LogWarning("[GameOverController] AdManager.Instance es null al intentar mostrar anuncio");
        }
    }

    private void CreateUI()
    {
        // Create Canvas
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.Log("GameOverController: Creando Canvas...");
            canvas = new GameObject("Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
            canvas.layer = 5; // UI layer

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.sizeDelta = Vector2.zero;
            Debug.Log("GameOverController: Canvas creado");
        }
        else
        {
            Debug.Log("GameOverController: Canvas ya existe");
        }

        // Create Score Text
        GameObject scoreObj = GameObject.Find("ScoreText");
        if (scoreObj == null)
        {
            scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(canvas.transform, false);
            scoreText = scoreObj.AddComponent<Text>();
            scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            scoreText.fontSize = 40;
            scoreText.color = CosmicTheme.SoftGold; // Dorado suave
            scoreText.alignment = TextAnchor.MiddleCenter;

            RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 0.5f);
            scoreRect.anchorMax = new Vector2(0.5f, 0.5f);
            scoreRect.anchoredPosition = new Vector2(0, 50);
            scoreRect.sizeDelta = new Vector2(400, 60);
        }
        else
        {
            scoreText = scoreObj.GetComponent<Text>();
        }

        // Create High Score Text
        GameObject highScoreObj = GameObject.Find("HighScoreText");
        if (highScoreObj == null)
        {
            highScoreObj = new GameObject("HighScoreText");
            highScoreObj.transform.SetParent(canvas.transform, false);
            highScoreText = highScoreObj.AddComponent<Text>();
            highScoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            highScoreText.fontSize = 35;
            highScoreText.color = CosmicTheme.CelestialBlue; // Azul celeste
            highScoreText.alignment = TextAnchor.MiddleCenter;

            RectTransform highScoreRect = highScoreObj.GetComponent<RectTransform>();
            highScoreRect.anchorMin = new Vector2(0.5f, 0.5f);
            highScoreRect.anchorMax = new Vector2(0.5f, 0.5f);
            highScoreRect.anchoredPosition = new Vector2(0, -20);
            highScoreRect.sizeDelta = new Vector2(400, 60);
        }
        else
        {
            highScoreText = highScoreObj.GetComponent<Text>();
        }

        // Create Tap to Main Menu Text
        GameObject tapObj = GameObject.Find("TapToRetryText");
        if (tapObj == null)
        {
            tapObj = new GameObject("TapToRetryText");
            tapObj.transform.SetParent(canvas.transform, false);
            Text tapText = tapObj.AddComponent<Text>();
            tapText.text = "Toca para volver al menú";
            tapText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tapText.fontSize = 30;
            tapText.color = CosmicTheme.EtherealLila; // Rosa-lila etéreo
            tapText.alignment = TextAnchor.MiddleCenter;

            RectTransform tapRect = tapObj.GetComponent<RectTransform>();
            tapRect.anchorMin = new Vector2(0.5f, 0.5f);
            tapRect.anchorMax = new Vector2(0.5f, 0.5f);
            tapRect.anchoredPosition = new Vector2(0, -100);
            tapRect.sizeDelta = new Vector2(300, 50);
        }
    }

    private void Update()
    {
        // Detect touch or click to go to main menu
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                GoToMainMenu();
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            GoToMainMenu();
        }
    }

    private void UpdateUI()
    {
        // Get saved values
        GameObject temp = new GameObject("TempScoreManager");
        ScoreManager tempSM = temp.AddComponent<ScoreManager>();
        tempSM.LoadHighScore();
        
        int currentScore = PlayerPrefs.GetInt("LastScore", 0);
        int highScore = tempSM.GetHighScore();
        
        if (scoreText != null)
        {
            scoreText.text = "Puntuación: " + currentScore;
        }
        
        if (highScoreText != null)
        {
            highScoreText.text = "Mejor puntuación: " + highScore;
        }
        
        Destroy(temp);
    }

    public void GoToMainMenu()
    {
        // Limpiar elementos visuales de la escena Game antes de volver al menú
        GameManager.CleanupGameScene();
        
        SceneManager.LoadScene("MainMenu");
    }

    // Mantener este método por compatibilidad, pero redirigir a MainMenu
    public void RestartGame()
    {
        GoToMainMenu();
    }
}

