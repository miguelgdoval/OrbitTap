using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static LogHelper;

public class GameOverController : MonoBehaviour
{
    [Header("UI Settings")]
    public bool createUIAtRuntime = true;
    
    private Text scoreText;
    private Text highScoreText;
    private Text shardsCollectedText;
    private Button shareButton;
    private GameObject copyMessageObj;
    
    private float inputDelay = 0.8f; // Delay antes de aceptar input para volver al menú
    private float timeSinceStart = 0f;
    private bool canAcceptInput = false;
    private bool isNavigating = false; // Prevenir múltiples navegaciones

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
        int gameScore = GetLastScore();
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
            
            // Asegurar EventSystem para que los botones funcionen
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

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
            scoreText.raycastTarget = false; // No bloquear toques
            
            // Aplicar alto contraste si está habilitado
            AccessibilityHelper.ApplyAccessibilityToText(scoreText);

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
            highScoreText.raycastTarget = false; // No bloquear toques
            
            // Aplicar alto contraste si está habilitado
            AccessibilityHelper.ApplyAccessibilityToText(highScoreText);

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

        // Create Combo/Streak Text (solo si hubo racha)
        int lastStreak = PlayerPrefs.GetInt("LastComboStreak", 0);
        if (lastStreak > 2)
        {
            GameObject comboObj = new GameObject("ComboText");
            comboObj.transform.SetParent(canvas.transform, false);
            Text comboText = comboObj.AddComponent<Text>();
            
            float lastMaxMult = PlayerPrefs.GetFloat("LastMaxMultiplier", 1f);
            comboText.text = $"Racha máxima: {lastStreak} (×{lastMaxMult:F1})";
            comboText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            comboText.fontSize = 22;
            comboText.color = new Color(0.5f, 1f, 0.5f, 0.9f);
            comboText.alignment = TextAnchor.MiddleCenter;
            comboText.raycastTarget = false;
            
            AccessibilityHelper.ApplyAccessibilityToText(comboText);
            
            RectTransform comboRect = comboObj.GetComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(0.5f, 0.5f);
            comboRect.anchorMax = new Vector2(0.5f, 0.5f);
            comboRect.anchoredPosition = new Vector2(0, -55);
            comboRect.sizeDelta = new Vector2(400, 35);
        }
        
        // Create Shards Collected Text (solo si se recogieron shards)
        int shardsValue = PlayerPrefs.GetInt("LastShardsValue", 0);
        if (shardsValue > 0)
        {
            GameObject shardsObj = new GameObject("ShardsCollectedText");
            shardsObj.transform.SetParent(canvas.transform, false);
            shardsCollectedText = shardsObj.AddComponent<Text>();
            
            int shardsCount = PlayerPrefs.GetInt("LastShardsCollected", 0);
            shardsCollectedText.text = $"+{shardsValue} ⭐ ({shardsCount} recogidos)";
            shardsCollectedText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            shardsCollectedText.fontSize = 26;
            shardsCollectedText.color = CosmicTheme.SoftGold; // Dorado suave
            shardsCollectedText.alignment = TextAnchor.MiddleCenter;
            shardsCollectedText.raycastTarget = false;
            
            AccessibilityHelper.ApplyAccessibilityToText(shardsCollectedText);
            
            // Outline para legibilidad
            Outline shardsOutline = shardsObj.AddComponent<Outline>();
            shardsOutline.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shardsOutline.effectDistance = new Vector2(1, 1);
            
            RectTransform shardsRect = shardsObj.GetComponent<RectTransform>();
            shardsRect.anchorMin = new Vector2(0.5f, 0.5f);
            shardsRect.anchorMax = new Vector2(0.5f, 0.5f);
            shardsRect.anchoredPosition = new Vector2(0, -80);
            shardsRect.sizeDelta = new Vector2(400, 35);
        }
        
        // Crear botones de acción (Reintentar + Menú)
        CreateActionButtons(canvas);
        
        // Crear botón de compartir
        CreateShareButton(canvas);
    }
    
    /// <summary>
    /// Crea los botones de Reintentar y Menú
    /// </summary>
    private void CreateActionButtons(GameObject canvas)
    {
        // Contenedor de botones
        GameObject buttonsContainer = new GameObject("ButtonsContainer");
        buttonsContainer.transform.SetParent(canvas.transform, false);
        
        RectTransform containerRect = buttonsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0, -160);
        containerRect.sizeDelta = new Vector2(420, 55);
        
        // --- Botón REINTENTAR (primario, más grande, colorido) ---
        GameObject retryBtnObj = new GameObject("RetryButton");
        retryBtnObj.transform.SetParent(buttonsContainer.transform, false);
        Button retryBtn = retryBtnObj.AddComponent<Button>();
        
        Image retryBtnImg = retryBtnObj.AddComponent<Image>();
        retryBtnImg.color = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.85f);
        
        // Outline de brillo
        Outline retryOutline = retryBtnObj.AddComponent<Outline>();
        retryOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.5f);
        retryOutline.effectDistance = new Vector2(2, 2);
        
        RectTransform retryRect = retryBtnObj.GetComponent<RectTransform>();
        retryRect.anchorMin = new Vector2(0f, 0f);
        retryRect.anchorMax = new Vector2(0.48f, 1f);
        retryRect.sizeDelta = Vector2.zero;
        retryRect.anchoredPosition = Vector2.zero;
        
        // Texto del botón Reintentar
        GameObject retryTextObj = new GameObject("Text");
        retryTextObj.transform.SetParent(retryBtnObj.transform, false);
        Text retryText = retryTextObj.AddComponent<Text>();
        retryText.text = "▶ Reintentar";
        retryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        retryText.fontSize = 24;
        retryText.fontStyle = FontStyle.Bold;
        retryText.color = Color.white;
        retryText.alignment = TextAnchor.MiddleCenter;
        retryText.raycastTarget = false;
        AccessibilityHelper.ApplyAccessibilityToText(retryText);
        
        RectTransform retryTextRect = retryTextObj.GetComponent<RectTransform>();
        retryTextRect.anchorMin = Vector2.zero;
        retryTextRect.anchorMax = Vector2.one;
        retryTextRect.sizeDelta = Vector2.zero;
        
        retryBtn.onClick.AddListener(() => {
            QuickRestart();
        });
        
        // --- Botón MENÚ (secundario, más discreto) ---
        GameObject menuBtnObj = new GameObject("MenuButton");
        menuBtnObj.transform.SetParent(buttonsContainer.transform, false);
        Button menuBtn = menuBtnObj.AddComponent<Button>();
        
        Image menuBtnImg = menuBtnObj.AddComponent<Image>();
        menuBtnImg.color = new Color(0.3f, 0.3f, 0.4f, 0.7f);
        
        Outline menuOutline = menuBtnObj.AddComponent<Outline>();
        menuOutline.effectColor = new Color(CosmicTheme.EtherealLila.r, CosmicTheme.EtherealLila.g, CosmicTheme.EtherealLila.b, 0.4f);
        menuOutline.effectDistance = new Vector2(1, 1);
        
        RectTransform menuRect = menuBtnObj.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.52f, 0f);
        menuRect.anchorMax = new Vector2(1f, 1f);
        menuRect.sizeDelta = Vector2.zero;
        menuRect.anchoredPosition = Vector2.zero;
        
        // Texto del botón Menú
        GameObject menuTextObj = new GameObject("Text");
        menuTextObj.transform.SetParent(menuBtnObj.transform, false);
        Text menuText = menuTextObj.AddComponent<Text>();
        menuText.text = "Menú";
        menuText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        menuText.fontSize = 22;
        menuText.color = CosmicTheme.EtherealLila;
        menuText.alignment = TextAnchor.MiddleCenter;
        menuText.raycastTarget = false;
        AccessibilityHelper.ApplyAccessibilityToText(menuText);
        
        RectTransform menuTextRect = menuTextObj.GetComponent<RectTransform>();
        menuTextRect.anchorMin = Vector2.zero;
        menuTextRect.anchorMax = Vector2.one;
        menuTextRect.sizeDelta = Vector2.zero;
        
        menuBtn.onClick.AddListener(() => {
            GoToMainMenu();
        });
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
        shareBtnRect.sizeDelta = new Vector2(180, 45);
        shareBtnRect.anchoredPosition = new Vector2(0, 60);
        
        GameObject shareTextObj = new GameObject("Text");
        shareTextObj.transform.SetParent(shareBtnObj.transform, false);
        Text shareText = shareTextObj.AddComponent<Text>();
        shareText.text = "Compartir";
        shareText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        shareText.fontSize = 24;
        shareText.color = Color.white;
        shareText.alignment = TextAnchor.MiddleCenter;
        shareText.raycastTarget = false; // El raycast lo maneja el Image del botón padre
        
        // Aplicar alto contraste si está habilitado
        AccessibilityHelper.ApplyAccessibilityToText(shareText);
        
        RectTransform shareTextRect = shareTextObj.GetComponent<RectTransform>();
        shareTextRect.anchorMin = Vector2.zero;
        shareTextRect.anchorMax = Vector2.one;
        shareTextRect.sizeDelta = Vector2.zero;
        
        shareButton.onClick.AddListener(() => {
            int currentScore = GetLastScore();
            int highScore = GetHighScore();
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
        // Esperar antes de aceptar input (evitar toques accidentales durante transición)
        if (!canAcceptInput)
        {
            timeSinceStart += Time.deltaTime;
            if (timeSinceStart >= inputDelay)
            {
                canAcceptInput = true;
            }
        }
        
        // Ya no procesamos "tap en cualquier sitio" — ahora hay botones explícitos
    }
    
    /// <summary>
    /// Comprueba si el toque/click está sobre un elemento de UI (ej: botón Share)
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        
        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void UpdateUI()
    {
        int currentScore = GetLastScore();
        int highScore = GetHighScore();
        
        if (scoreText != null)
        {
            scoreText.text = "Puntuación: " + currentScore;
        }
        
        if (highScoreText != null)
        {
            highScoreText.text = "Mejor puntuación: " + highScore;
        }
    }

    private int GetLastScore()
    {
        // Priorizar SaveData (fuente principal actual)
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                return saveData.lastScore;
            }
        }

        // Fallback por compatibilidad
        return PlayerPrefs.GetInt("LastScore", 0);
    }

    private int GetHighScore()
    {
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                return saveData.highScore;
            }
        }

        return PlayerPrefs.GetInt("HighScore", 0);
    }

    public void GoToMainMenu()
    {
        // Prevenir múltiples llamadas
        if (isNavigating) return;
        isNavigating = true;
        
        // Limpiar elementos visuales de la escena Game antes de volver al menú
        GameManager.CleanupGameScene();
        
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Reinicia la partida directamente sin pasar por el menú principal
    /// </summary>
    public void QuickRestart()
    {
        if (isNavigating) return;
        isNavigating = true;
        
        Log("[GameOverController] Quick Restart — cargando escena Game directamente");
        
        // Limpiar elementos visuales de la escena Game antes de reiniciar
        GameManager.CleanupGameScene();
        
        SceneManager.LoadScene("Game");
    }
    
    // Mantener este método por compatibilidad
    public void RestartGame()
    {
        QuickRestart();
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
        
        // Aplicar alto contraste si está habilitado
        AccessibilityHelper.ApplyAccessibilityToText(messageText);
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        // Destruir después de 2 segundos
        Destroy(copyMessageObj, 2f);
    }
}

