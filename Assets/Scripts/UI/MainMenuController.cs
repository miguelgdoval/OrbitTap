using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Controlador principal del menú - Versión completa con todas las secciones
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    private GameObject canvas;
    private GameObject playSection;
    private GameObject skinsSection;
    private GameObject shopSection;
    private SettingsPanel settingsPanel;
    
    [Header("Play Section")]
    private Text titleText;
    private GameObject playerDemo;
    private Button playButton;
    private Text playButtonText;
    
    [Header("Top Panel")]
    private Text currencyText;
    private Button currencyButton;
    private Button settingsButton;
    
    [Header("Bottom Navigation")]
    private Button homeButton;
    private Button skinsNavButton;
    private Button shopNavButton;
    
    private MenuSection currentSection = MenuSection.Play;
    private CurrencyManager currencyManager;
    
    private void Start()
    {
        // Configurar fondo cósmico
        if (GetComponent<CosmicBackground>() == null)
        {
            gameObject.AddComponent<CosmicBackground>();
        }
        
        // Crear o encontrar CurrencyManager
        if (CurrencyManager.Instance == null)
        {
            GameObject currencyObj = new GameObject("CurrencyManager");
            currencyManager = currencyObj.AddComponent<CurrencyManager>();
        }
        else
        {
            currencyManager = CurrencyManager.Instance;
        }
        
        CreateUI();
        CreatePlayerDemo();
    }
    
    private void CreateUI()
    {
        // Create Canvas
        canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvas.AddComponent<GraphicRaycaster>();
            canvas.layer = 5; // UI layer
            
            // Asegurar que existe un EventSystem para los botones
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.anchorMin = Vector2.zero;
                canvasRect.anchorMax = Vector2.one;
                canvasRect.sizeDelta = Vector2.zero;
            }
        }
        
        // Verificar que el canvas tiene RectTransform
        if (canvas != null && canvas.GetComponent<RectTransform>() == null)
        {
            canvas.AddComponent<RectTransform>();
        }
        
        if (canvas == null)
        {
            Debug.LogError("MainMenuController: Failed to create Canvas!");
            return;
        }
        
        CreateTopPanel();
        CreatePlaySection();
        CreateSkinsSection();
        CreateShopSection();
        CreateBottomNavigation();
        
        // Inicialmente mostrar solo Play
        ShowSection(MenuSection.Play);
    }
    
    private void CreateTopPanel()
    {
        if (canvas == null)
        {
            Debug.LogError("MainMenuController: Canvas is null in CreateTopPanel!");
            return;
        }
        
        // Panel superior
        GameObject topPanel = new GameObject("TopPanel");
        topPanel.transform.SetParent(canvas.transform, false);
        RectTransform topRect = topPanel.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0, -40); // Negativo para estar dentro de la pantalla
        topRect.sizeDelta = new Vector2(0, 80);
        
        // Botón Settings (izquierda)
        GameObject settingsObj = new GameObject("SettingsButton");
        settingsObj.transform.SetParent(topPanel.transform, false);
        settingsButton = settingsObj.AddComponent<Button>();
        Image settingsImg = settingsObj.AddComponent<Image>();
        settingsImg.color = Color.clear; // Fondo transparente
        
        RectTransform settingsRect = settingsObj.GetComponent<RectTransform>();
        settingsRect.anchorMin = new Vector2(0, 0.5f);
        settingsRect.anchorMax = new Vector2(0, 0.5f);
        settingsRect.pivot = new Vector2(0.5f, 0.5f);
        settingsRect.anchoredPosition = new Vector2(50, 0);
        settingsRect.sizeDelta = new Vector2(60, 60);
        
        // Crear objeto hijo para el icono
        GameObject settingsIconObj = new GameObject("Icon");
        settingsIconObj.transform.SetParent(settingsObj.transform, false);
        Image settingsIconImg = settingsIconObj.AddComponent<Image>();
        
        // Cargar el sprite del icono
        Sprite optionsIcon = LoadOptionsIcon();
        if (optionsIcon != null)
        {
            settingsIconImg.sprite = optionsIcon;
        }
        else
        {
            // Fallback: usar un sprite simple si no se encuentra el icono
            Debug.LogWarning("No se pudo cargar OptionsIcon, usando fallback");
        }
        
        settingsIconImg.color = CosmicTheme.NeonCyan; // Color neon cian para el icono
        settingsIconImg.preserveAspect = true; // Mantener proporción del icono
        
        // Configurar para mejor calidad de renderizado
        settingsIconImg.type = Image.Type.Simple; // Tipo Simple para mejor calidad
        settingsIconImg.useSpriteMesh = false; // Desactivar mesh para mejor calidad en UI
        
        RectTransform iconRect = settingsIconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        // Icono más grande que el contenedor (120% del tamaño)
        iconRect.sizeDelta = new Vector2(72, 72); // 60 * 1.2 = 72
        
        // Añadir animación de pulsado
        AddButtonPressAnimation(settingsButton, iconRect);
        
        settingsButton.onClick.AddListener(ShowSettings);
        
        // Monedas (derecha)
        GameObject currencyObj = new GameObject("CurrencyDisplay");
        currencyObj.transform.SetParent(topPanel.transform, false);
        currencyButton = currencyObj.AddComponent<Button>();
        Image currencyImg = currencyObj.AddComponent<Image>();
        currencyImg.color = new Color(0, 0, 0, 0.3f);
        
        RectTransform currencyRect = currencyObj.GetComponent<RectTransform>();
        currencyRect.anchorMin = new Vector2(1, 0.5f);
        currencyRect.anchorMax = new Vector2(1, 0.5f);
        currencyRect.pivot = new Vector2(1f, 0.5f);
        currencyRect.anchoredPosition = new Vector2(-100, 0);
        currencyRect.sizeDelta = new Vector2(150, 60);
        
        // Crear objeto hijo para el texto
        GameObject currencyTextObj = new GameObject("Text");
        currencyTextObj.transform.SetParent(currencyObj.transform, false);
        currencyText = currencyTextObj.AddComponent<Text>();
        if (currencyText != null)
        {
            currencyText.text = "0 ⭐";
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont != null)
            {
                currencyText.font = defaultFont;
            }
            currencyText.fontSize = 24;
            currencyText.alignment = TextAnchor.MiddleCenter;
            currencyText.color = CosmicTheme.SoftGold;
            
            RectTransform textRect = currencyTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
        }
        
        currencyButton.onClick.AddListener(() => NavigateTo(MenuSection.Shop));
        
        // Actualizar monedas
        if (currencyManager != null)
        {
            UpdateCurrencyDisplay();
            currencyManager.OnCurrencyChanged += (amount) => UpdateCurrencyDisplay();
        }
    }
    
    private void CreatePlaySection()
    {
        // Sección Play
        playSection = new GameObject("PlaySection");
        playSection.transform.SetParent(canvas.transform, false);
        RectTransform playRect = playSection.AddComponent<RectTransform>();
        playRect.anchorMin = Vector2.zero;
        playRect.anchorMax = Vector2.one;
        playRect.sizeDelta = Vector2.zero;
        
        // Título
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(playSection.transform, false);
        titleText = titleObj.AddComponent<Text>();
        titleText.text = "STARBOUND ORBIT";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 50;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = CosmicTheme.SoftGold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0, 200); // Más arriba para dejar espacio
        titleRect.sizeDelta = new Vector2(600, 80);
        
        // Animación de pulso para el título
        StartCoroutine(PulseTitle());
        
        // Botón Play (debajo del centro)
        GameObject playBtnObj = new GameObject("PlayButton");
        playBtnObj.transform.SetParent(playSection.transform, false);
        playButton = playBtnObj.AddComponent<Button>();
        Image playImg = playBtnObj.AddComponent<Image>();
        playImg.color = new Color(CosmicTheme.SoftGold.r, CosmicTheme.SoftGold.g, CosmicTheme.SoftGold.b, 0.8f);
        playImg.raycastTarget = true; // Asegurar que recibe raycasts
        
        RectTransform playBtnRect = playBtnObj.GetComponent<RectTransform>();
        playBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
        playBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
        playBtnRect.pivot = new Vector2(0.5f, 0.5f);
        playBtnRect.anchoredPosition = new Vector2(0, -50); // Más cerca del centro
        playBtnRect.sizeDelta = new Vector2(300, 80);
        
        // Crear objeto hijo para el texto
        GameObject playTextObj = new GameObject("Text");
        playTextObj.transform.SetParent(playBtnObj.transform, false);
        playButtonText = playTextObj.AddComponent<Text>();
        if (playButtonText != null)
        {
            playButtonText.text = "TAP TO PLAY";
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont != null)
            {
                playButtonText.font = defaultFont;
            }
            playButtonText.fontSize = 32;
            playButtonText.fontStyle = FontStyle.Bold;
            playButtonText.alignment = TextAnchor.MiddleCenter;
            playButtonText.color = Color.white;
            
            RectTransform textRect = playTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
        }
        
        playButton.onClick.AddListener(LoadGame);
        
        // Animación de pulso para el botón
        StartCoroutine(PulseButton(playBtnRect));
    }
    
    private void CreateBottomNavigation()
    {
        // Panel de navegación inferior
        GameObject navPanel = new GameObject("NavigationPanel");
        navPanel.transform.SetParent(canvas.transform, false);
        RectTransform navRect = navPanel.AddComponent<RectTransform>();
        navRect.anchorMin = new Vector2(0, 0);
        navRect.anchorMax = new Vector2(1, 0);
        navRect.pivot = new Vector2(0.5f, 0f);
        navRect.anchoredPosition = new Vector2(0, 40); // Positivo para estar dentro de la pantalla
        navRect.sizeDelta = new Vector2(0, 80);
        
        Image navImg = navPanel.AddComponent<Image>();
        navImg.color = new Color(0, 0, 0, 0.5f);
        
        // Botón Home
        homeButton = CreateNavButton("HomeButton", "🏠", navPanel.transform, -100);
        homeButton.onClick.AddListener(() => NavigateTo(MenuSection.Play));
        
        // Botón Skins
        skinsNavButton = CreateNavButton("SkinsButton", "🎨", navPanel.transform, 0);
        skinsNavButton.onClick.AddListener(() => NavigateTo(MenuSection.Skins));
        
        // Botón Shop
        shopNavButton = CreateNavButton("ShopButton", "🛒", navPanel.transform, 100);
        shopNavButton.onClick.AddListener(() => NavigateTo(MenuSection.Shop));
    }
    
    private Button CreateNavButton(string name, string icon, Transform parent, float xPos)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Button btn = btnObj.AddComponent<Button>();
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.2f);
        
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xPos, 0);
        rect.sizeDelta = new Vector2(60, 60);
        
        // Crear objeto hijo para el texto
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = icon;
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont != null)
        {
            text.font = defaultFont;
        }
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = CosmicTheme.SoftGold;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        return btn;
    }
    
    private void CreatePlayerDemo()
    {
        // Crear un player demo orbitando en el centro
        GameObject center = new GameObject("MenuCenter");
        center.transform.position = Vector3.zero;
        
        GameObject player = new GameObject("PlayerDemo");
        player.transform.position = new Vector3(2, 0, 0);
        player.transform.localScale = Vector3.one * 0.64f; // Más pequeño para el asteroide
        
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = LoadPlayerSprite();
        if (sr.sprite == null)
        {
            // Fallback a estrella si no se encuentra el sprite
            sr.sprite = SpriteGenerator.CreateStarSprite(0.3f, CosmicTheme.EtherealLila);
            sr.color = CosmicTheme.EtherealLila;
        }
        else
        {
            sr.color = Color.white; // Color blanco para mantener los colores originales del sprite
        }
        sr.sortingOrder = 5;
        
        PlayerOrbit orbit = player.AddComponent<PlayerOrbit>();
        orbit.radius = 2f;
        orbit.angle = 0f;
        orbit.angularSpeed = 1f; // Más lento que en el juego
        orbit.center = center.transform;
        
        // PlanetSurface - Rotación de superficie del planeta
        PlanetSurface planetSurface = player.AddComponent<PlanetSurface>();
        planetSurface.rotationSpeed = 20f; // Misma velocidad que en el juego
        
        playerDemo = player;
    }
    
    private void CreateSkinsSection()
    {
        // Crear sección de Skins
        GameObject skinsObj = new GameObject("SkinsSection");
        skinsObj.transform.SetParent(canvas.transform, false);
        RectTransform skinsRect = skinsObj.AddComponent<RectTransform>();
        skinsRect.anchorMin = Vector2.zero;
        skinsRect.anchorMax = Vector2.one;
        skinsRect.sizeDelta = Vector2.zero;
        skinsSection = skinsObj;
        
        // Añadir componente SkinsSection
        SkinsSection skinsComponent = skinsObj.AddComponent<SkinsSection>();
        skinsSection.SetActive(false);
    }
    
    private void CreateShopSection()
    {
        // Crear sección de Shop
        GameObject shopObj = new GameObject("ShopSection");
        shopObj.transform.SetParent(canvas.transform, false);
        RectTransform shopRect = shopObj.AddComponent<RectTransform>();
        shopRect.anchorMin = Vector2.zero;
        shopRect.anchorMax = Vector2.one;
        shopRect.sizeDelta = Vector2.zero;
        shopSection = shopObj;
        
        // Añadir componente ShopSection
        ShopSection shopComponent = shopObj.AddComponent<ShopSection>();
        shopSection.SetActive(false);
    }
    
    private void ShowSection(MenuSection section)
    {
        currentSection = section;
        
        if (playSection != null) playSection.SetActive(section == MenuSection.Play);
        if (skinsSection != null) skinsSection.SetActive(section == MenuSection.Skins);
        if (shopSection != null) shopSection.SetActive(section == MenuSection.Shop);
    }
    
    public void NavigateTo(MenuSection section)
    {
        ShowSection(section);
    }
    
    private void ShowSettings()
    {
        if (settingsPanel == null)
        {
            GameObject settingsObj = new GameObject("SettingsPanel");
            settingsPanel = settingsObj.AddComponent<SettingsPanel>();
        }
        settingsPanel.Show();
    }
    
    private void AddButtonPressAnimation(Button button, RectTransform iconRect)
    {
        // Crear EventTrigger para detectar cuando se presiona y suelta el botón
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // Evento: PointerDown (cuando se presiona)
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => {
            StartCoroutine(AnimateButtonPress(iconRect, true));
        });
        trigger.triggers.Add(pointerDown);
        
        // Evento: PointerUp (cuando se suelta)
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => {
            StartCoroutine(AnimateButtonPress(iconRect, false));
        });
        trigger.triggers.Add(pointerUp);
        
        // Evento: PointerExit (si el mouse sale del botón mientras está presionado)
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => {
            StartCoroutine(AnimateButtonPress(iconRect, false));
        });
        trigger.triggers.Add(pointerExit);
    }
    
    private IEnumerator AnimateButtonPress(RectTransform iconRect, bool isPressed)
    {
        Vector3 targetScale = isPressed ? Vector3.one * 0.85f : Vector3.one;
        Vector3 currentScale = iconRect.localScale;
        float duration = 0.1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease out para suavidad
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            iconRect.localScale = Vector3.Lerp(currentScale, targetScale, easeT);
            yield return null;
        }
        
        iconRect.localScale = targetScale;
    }
    
    private void UpdateCurrencyDisplay()
    {
        if (currencyText != null && currencyManager != null)
        {
            currencyText.text = $"{currencyManager.CurrentCurrency} ⭐";
        }
    }
    
    private IEnumerator PulseTitle()
    {
        while (true)
        {
            float time = 0f;
            float duration = 2f;
            float minScale = 0.98f;
            float maxScale = 1.02f;
            
            while (time < duration)
            {
                time += Time.deltaTime;
                float scale = Mathf.Lerp(minScale, maxScale, Mathf.Sin(time / duration * Mathf.PI));
                if (titleText != null)
                {
                    titleText.transform.localScale = Vector3.one * scale;
                }
                yield return null;
            }
        }
    }
    
    private IEnumerator PulseButton(RectTransform buttonRect)
    {
        while (true)
        {
            float time = 0f;
            float duration = 1.5f;
            float minScale = 0.97f;
            float maxScale = 1.03f;
            
            while (time < duration)
            {
                time += Time.deltaTime;
                float scale = Mathf.Lerp(minScale, maxScale, Mathf.Sin(time / duration * Mathf.PI));
                if (buttonRect != null)
                {
                    buttonRect.localScale = Vector3.one * scale;
                }
                yield return null;
            }
        }
    }
    
    public void LoadGame()
    {
        SceneManager.LoadScene("Game");
    }
    
    #if UNITY_EDITOR
    private Sprite LoadPlayerSprite()
    {
        if (!Application.isPlaying) return null;
        
        try
        {
            // Buscar el sprite del asteroide errante
            string[] guids = AssetDatabase.FindAssets("AsteroideErrante t:Sprite");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
                
                // Si no se encuentra como Sprite, intentar como Texture2D
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    // Crear sprite desde texture
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo cargar el sprite del asteroide: {e.Message}");
        }
        return null;
    }
    private Sprite LoadOptionsIcon()
    {
        if (!Application.isPlaying) return null;
        
        try
        {
            // Buscar el sprite del icono de opciones
            string[] guids = AssetDatabase.FindAssets("OptionsIcon t:Sprite");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
                
                // Si no se encuentra como Sprite, intentar como Texture2D
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    // Crear sprite desde texture
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo cargar el icono de opciones: {e.Message}");
        }
        return null;
    }
    #else
    private Sprite LoadPlayerSprite()
    {
        // En build, intentar cargar desde Resources
        return Resources.Load<Sprite>("Art/Protagonist/AsteroideErrante");
    }
    
    private Sprite LoadOptionsIcon()
    {
        // En build, intentar cargar desde Resources
        return Resources.Load<Sprite>("Art/Icons/OptionsIcon");
    }
    #endif
}
