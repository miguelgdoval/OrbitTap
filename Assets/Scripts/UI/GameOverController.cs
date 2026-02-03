using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static LogHelper;

public class GameOverController : MonoBehaviour
{
    [Header("UI Settings")]
    public bool createUIAtRuntime = true;
    
    private Text scoreText;
    private Text highScoreText;
    private Button shareButton;
    private GameObject copyMessageObj;

    private void Awake()
    {
        Log("GameOverController: Awake() llamado");
    }

    private void Start()
    {
        Log("GameOverController: Start() llamado - createUIAtRuntime = " + createUIAtRuntime);
        
        // Configurar fondo cósmico
        if (GetComponent<CosmicBackground>() == null)
        {
            gameObject.AddComponent<CosmicBackground>();
        }
        
        // Forzar creación de UI si no existe
        GameObject canvas = GameObject.Find("Canvas");
        if (createUIAtRuntime || canvas == null)
        {
            Log("GameOverController: Creando UI...");
            CreateUI();
            // Cachear referencias después de crear
            canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                scoreText = canvas.transform.Find("ScoreText")?.GetComponent<Text>();
                highScoreText = canvas.transform.Find("HighScoreText")?.GetComponent<Text>();
                shareButton = canvas.transform.Find("ShareButton")?.GetComponent<Button>();
            }
        }
        else
        {
            Log("GameOverController: Canvas ya existe, buscando componentes...");
            // Usar Transform.Find en lugar de GameObject.Find (más eficiente)
            scoreText = canvas.transform.Find("ScoreText")?.GetComponent<Text>();
            highScoreText = canvas.transform.Find("HighScoreText")?.GetComponent<Text>();
            
            // Asegurar que existe el botón de compartir
            Transform shareButtonTransform = canvas.transform.Find("ShareButton");
            if (shareButtonTransform == null)
            {
                CreateShareButton(canvas);
            }
            else
            {
                shareButton = shareButtonTransform.GetComponent<Button>();
            }
        }
        
        Log("GameOverController: Actualizando UI...");
        UpdateUI();
        
        // Verificar si se debe mostrar anuncio intersticial (con lógica inteligente)
        CheckAndShowInterstitialAd();
        
        Log("GameOverController: Inicialización completa");
    }
    
    /// <summary>
    /// Verifica las condiciones y muestra un anuncio intersticial si corresponde
    /// </summary>
    private void CheckAndShowInterstitialAd()
    {
        if (AdManager.Instance == null)
        {
            LogWarning("[GameOverController] AdManager.Instance es null");
            return;
        }
        
        // Obtener puntuación de la partida (el score es igual al tiempo sobrevivido en segundos)
        int gameScore = PlayerPrefs.GetInt("LastScore", 0);
        Log($"[GameOverController] Verificando anuncio. Puntuación: {gameScore}");
        
        // Verificar si se debe mostrar el anuncio
        if (AdManager.Instance.ShouldShowInterstitialAd(gameScore))
        {
            Log("[GameOverController] Condiciones cumplidas, programando anuncio para mostrar en 1 segundo...");
            // Mostrar anuncio después de un pequeño delay
            StartCoroutine(ShowAdAfterDelay(1f));
        }
        else
        {
            Log("[GameOverController] No se mostrará anuncio (condiciones no cumplidas)");
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
            Log("[GameOverController] Intentando mostrar anuncio intersticial...");
            AdManager.Instance.ShowInterstitialAd();
        }
        else
        {
            LogWarning("[GameOverController] AdManager.Instance es null al intentar mostrar anuncio");
        }
    }

    private void CreateUI()
    {
        // Create Canvas
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Log("GameOverController: Creando Canvas...");
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
            Log("GameOverController: Canvas creado");
        }
        else
        {
            Log("GameOverController: Canvas ya existe");
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
        
        // Crear botón de compartir
        CreateShareButton(canvas);
    }
    
    private void CreateShareButton(GameObject canvas)
    {
        GameObject shareBtnObj = new GameObject("ShareButton");
        shareBtnObj.transform.SetParent(canvas.transform, false);
        shareButton = shareBtnObj.AddComponent<Button>();
        
        Image shareBtnImg = shareBtnObj.AddComponent<Image>();
        shareBtnImg.color = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.8f);
        
        RectTransform shareBtnRect = shareBtnObj.GetComponent<RectTransform>();
        shareBtnRect.anchorMin = new Vector2(0.5f, 0f);
        shareBtnRect.anchorMax = new Vector2(0.5f, 0f);
        shareBtnRect.pivot = new Vector2(0.5f, 0.5f);
        shareBtnRect.sizeDelta = new Vector2(200, 50);
        shareBtnRect.anchoredPosition = new Vector2(0, 100);
        
        GameObject shareTextObj = new GameObject("Text");
        shareTextObj.transform.SetParent(shareBtnObj.transform, false);
        Text shareText = shareTextObj.AddComponent<Text>();
        shareText.text = "Compartir";
        shareText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        shareText.fontSize = 24;
        shareText.color = Color.white;
        shareText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform shareTextRect = shareTextObj.GetComponent<RectTransform>();
        shareTextRect.anchorMin = Vector2.zero;
        shareTextRect.anchorMax = Vector2.one;
        shareTextRect.sizeDelta = Vector2.zero;
        
        shareButton.onClick.AddListener(() => {
            int currentScore = PlayerPrefs.GetInt("LastScore", 0);
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            bool isNewRecord = currentScore == highScore && currentScore > 0;
            
            if (SocialShareManager.Instance != null)
            {
                SocialShareManager.Instance.ShareScore(currentScore, highScore, isNewRecord);
                
                // Analytics: Registrar compartir
                if (AnalyticsManager.Instance != null)
                {
                    AnalyticsManager.Instance.TrackShare(currentScore);
                }
                
                // Mostrar mensaje visual en editor/desktop
                #if !UNITY_ANDROID && !UNITY_IOS
                ShowCopyMessage();
                #endif
            }
            else
            {
                LogWarning("[GameOverController] SocialShareManager.Instance es null");
            }
        });
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
        // Leer directamente de PlayerPrefs (más eficiente que crear GameObject temporal)
        int currentScore = PlayerPrefs.GetInt("LastScore", 0);
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        
        if (scoreText != null)
        {
            scoreText.text = "Puntuación: " + currentScore;
        }
        
        if (highScoreText != null)
        {
            highScoreText.text = "Mejor puntuación: " + highScore;
        }
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
    
    private void ShowCopyMessage()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        
        // Eliminar mensaje anterior si existe
        if (copyMessageObj != null)
        {
            Destroy(copyMessageObj);
        }
        
        // Crear mensaje temporal
        copyMessageObj = new GameObject("CopyMessage");
        copyMessageObj.transform.SetParent(canvas.transform, false);
        
        RectTransform messageRect = copyMessageObj.AddComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.sizeDelta = new Vector2(400, 50);
        messageRect.anchoredPosition = new Vector2(0, 150);
        
        // Añadir fondo semi-transparente
        Image bgImage = copyMessageObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);
        
        // Crear texto como hijo
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(copyMessageObj.transform, false);
        Text messageText = textObj.AddComponent<Text>();
        messageText.text = "✓ Texto copiado al portapapeles";
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = 24;
        messageText.color = CosmicTheme.NeonCyan;
        messageText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        // Destruir después de 2 segundos
        Destroy(copyMessageObj, 2f);
    }
}

