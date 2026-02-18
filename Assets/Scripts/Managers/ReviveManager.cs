using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using static LogHelper;

/// <summary>
/// Manager para el sistema de segunda oportunidad (Revive).
/// Permite al jugador revivir una vez por partida viendo un anuncio o gastando Cosmic Crystals.
/// </summary>
public class ReviveManager : MonoBehaviour
{
    public static ReviveManager Instance { get; private set; }
    
    [Header("Revive Settings")]
    [Tooltip("Coste en Cosmic Crystals para revivir")]
    public int reviveCrystalCost = 10;
    
    [Tooltip("Duración de invulnerabilidad después de revivir (segundos)")]
    public float invulnerabilityDuration = 3f;
    
    [Tooltip("Máximo de revives por partida")]
    public int maxRevivesPerGame = 1;
    
    [Tooltip("Tiempo para decidir si revivir (segundos)")]
    public float decisionTimeout = 5f;
    
    // Estado
    private int revivesUsedThisGame = 0;
    private bool isShowingReviveUI = false;
    private bool isWaitingForAdResult = false;
    private bool isPlayerInvulnerable = false;
    private float invulnerabilityTimer = 0f;
    private GameObject reviveUIRoot;
    private Coroutine countdownCoroutine;
    
    // Callbacks
    private System.Action onReviveAccepted;
    private System.Action onReviveDeclined;
    
    // Referencia al callback original de rewarded ad
    private System.Action originalRewardedCallback;
    
    /// <summary>
    /// True si la UI de revive está visible (para que otros sistemas no abran pausa encima).
    /// </summary>
    public bool IsShowingReviveUI => isShowingReviveUI;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        // Actualizar timer de invulnerabilidad
        if (isPlayerInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0f)
            {
                EndInvulnerability();
            }
        }
    }
    
    /// <summary>
    /// Limpieza al destruir - asegurar que Time.timeScale no quede en 0
    /// </summary>
    private void OnDestroy()
    {
        if (isShowingReviveUI)
        {
            Time.timeScale = 1f;
            isShowingReviveUI = false;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    /// <summary>
    /// Resetea el estado del revive al iniciar una nueva partida
    /// </summary>
    public void ResetForNewGame()
    {
        revivesUsedThisGame = 0;
        isPlayerInvulnerable = false;
        invulnerabilityTimer = 0f;
        isShowingReviveUI = false;
        
        if (reviveUIRoot != null)
        {
            Destroy(reviveUIRoot);
            reviveUIRoot = null;
        }
    }
    
    /// <summary>
    /// Comprueba si el jugador puede revivir
    /// </summary>
    public bool CanRevive()
    {
        return revivesUsedThisGame < maxRevivesPerGame;
    }
    
    /// <summary>
    /// Comprueba si el jugador es invulnerable (post-revive)
    /// </summary>
    public bool IsInvulnerable()
    {
        return isPlayerInvulnerable;
    }
    
    /// <summary>
    /// Muestra la UI de revive y espera la decisión del jugador
    /// </summary>
    public void ShowReviveOption(System.Action onAccepted, System.Action onDeclined)
    {
        if (isShowingReviveUI) return;
        
        isShowingReviveUI = true;
        isWaitingForAdResult = false;
        onReviveAccepted = onAccepted;
        onReviveDeclined = onDeclined;
        
        Log("[ReviveManager] Mostrando opción de revive");
        
        // Pausar el juego (slow-mo dramático)
        Time.timeScale = 0f;
        
        // Crear UI de revive
        CreateReviveUI();
        
        // Iniciar countdown
        countdownCoroutine = StartCoroutine(ReviveCountdown());
    }
    
    /// <summary>
    /// Ejecuta el revive: recrea el jugador, limpia obstáculos, da invulnerabilidad
    /// </summary>
    public void ExecuteRevive()
    {
        revivesUsedThisGame++;
        isShowingReviveUI = false;
        isWaitingForAdResult = false;
        
        Log("[ReviveManager] Ejecutando revive...");
        
        // Detener countdown
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        
        // Cerrar UI
        if (reviveUIRoot != null)
        {
            Destroy(reviveUIRoot);
            reviveUIRoot = null;
        }
        
        // Restaurar timeScale
        Time.timeScale = 1f;
        
        // Limpiar todos los obstáculos en pantalla
        ClearAllObstacles();
        
        // Recrear el jugador
        RespawnPlayer();
        
        // Activar invulnerabilidad
        StartInvulnerability();
        
        // Callback
        onReviveAccepted?.Invoke();
        
        Log("[ReviveManager] Revive completado exitosamente");
    }
    
    /// <summary>
    /// Declina el revive y procede al Game Over
    /// </summary>
    public void DeclineRevive()
    {
        // Si hay un anuncio en curso para revive, no declinar en segundo plano.
        if (isWaitingForAdResult)
        {
            Log("[ReviveManager] Ignorando declive: anuncio de revive en progreso");
            return;
        }

        isShowingReviveUI = false;
        isWaitingForAdResult = false;
        
        Log("[ReviveManager] Revive declinado");
        
        // Detener countdown
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        
        // Cerrar UI
        if (reviveUIRoot != null)
        {
            Destroy(reviveUIRoot);
            reviveUIRoot = null;
        }
        
        // Restaurar timeScale
        Time.timeScale = 1f;
        
        // Callback
        onReviveDeclined?.Invoke();
    }
    
    /// <summary>
    /// Intenta revivir viendo un anuncio con recompensa
    /// </summary>
    private void ReviveWithAd()
    {
        if (isWaitingForAdResult)
        {
            return;
        }

        Log("[ReviveManager] Intentando revive con anuncio...");
        
        if (AdManager.Instance != null)
        {
            isWaitingForAdResult = true;
            SetReviveButtonsInteractable(false);

            // Restaurar timeScale temporalmente para que el ad funcione
            Time.timeScale = 1f;
            
            AdManager.Instance.ShowRewardedAdWithCallback(
                onCompleted: () =>
                {
                    isWaitingForAdResult = false;
                    Log("[ReviveManager] Anuncio completado, ejecutando revive");
                    ExecuteRevive();
                },
                onFailed: () =>
                {
                    isWaitingForAdResult = false;
                    Log("[ReviveManager] Anuncio cancelado o falló");

                    // Si la UI de revive sigue activa, volver al estado de decisión.
                    if (isShowingReviveUI)
                    {
                        Time.timeScale = 0f; // Volver a pausar
                        SetReviveButtonsInteractable(true);
                    }
                }
            );
            return;
        }
        
        // Si no hay ads disponibles, ejecutar revive directamente (en dev/testing)
        Log("[ReviveManager] Ads no disponibles, revive directo (testing)");
        ExecuteRevive();
    }
    
    /// <summary>
    /// Intenta revivir gastando Cosmic Crystals
    /// </summary>
    private void ReviveWithCrystals()
    {
        Log($"[ReviveManager] Intentando revive con {reviveCrystalCost} Cosmic Crystals...");
        
        if (CurrencyManager.Instance != null)
        {
            bool spent = CurrencyManager.Instance.SpendCosmicCrystals(reviveCrystalCost);
            if (spent)
            {
                Log("[ReviveManager] Crystals gastados, ejecutando revive");
                ExecuteRevive();
            }
            else
            {
                Log("[ReviveManager] No hay suficientes Cosmic Crystals");
                // Mostrar feedback visual
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.ShowWarning($"No tienes suficientes 💎 ({reviveCrystalCost} necesarios)");
                }
            }
        }
        else
        {
            LogWarning("[ReviveManager] CurrencyManager no encontrado");
        }
    }
    
    /// <summary>
    /// Limpia todos los obstáculos en pantalla
    /// </summary>
    private void ClearAllObstacles()
    {
        Log("[ReviveManager] Limpiando todos los obstáculos...");
        
        // Buscar todos los obstáculos activos
        ObstacleDestructionController[] obstacles = FindObjectsByType<ObstacleDestructionController>(FindObjectsSortMode.None);
        int count = 0;
        
        foreach (ObstacleDestructionController obstacle in obstacles)
        {
            if (obstacle != null && obstacle.gameObject != null)
            {
                Destroy(obstacle.gameObject);
                count++;
            }
        }
        
        // También limpiar ObstacleMover por si hay obstáculos sin DestructionController
        ObstacleMover[] movers = FindObjectsByType<ObstacleMover>(FindObjectsSortMode.None);
        foreach (ObstacleMover mover in movers)
        {
            if (mover != null && mover.gameObject != null)
            {
                Destroy(mover.gameObject);
                count++;
            }
        }
        
        Log($"[ReviveManager] {count} obstáculos eliminados");
    }
    
    /// <summary>
    /// Recrea el jugador en la escena
    /// </summary>
    private void RespawnPlayer()
    {
        Log("[ReviveManager] Recreando jugador...");
        
        // Buscar centro
        GameObject center = GameObject.Find("Center");
        if (center == null)
        {
            center = new GameObject("Center");
            center.transform.position = Vector3.zero;
        }
        
        // Crear nuevo jugador
        GameObject player = new GameObject("Player");
        
        try
        {
            player.tag = "Player";
        }
        catch
        {
            LogWarning("[ReviveManager] Tag 'Player' no encontrado");
        }
        
        player.transform.position = new Vector3(2, 0, 0);
        player.transform.localScale = Vector3.one * 0.64f;
        
        // SpriteRenderer
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = LoadPlayerSprite();
        if (sr.sprite == null)
        {
            sr.sprite = SpriteGenerator.CreateStarSprite(0.3f, CosmicTheme.EtherealLila);
            sr.color = CosmicTheme.EtherealLila;
        }
        else
        {
            sr.color = Color.white;
        }
        sr.sortingOrder = 10;
        
        // Rigidbody2D
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        
        // CircleCollider2D
        CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
        collider.radius = 0.25f;
        collider.isTrigger = true;
        
        // PlayerOrbit
        PlayerOrbit orbit = player.AddComponent<PlayerOrbit>();
        orbit.radius = 2f;
        orbit.angle = 0f;
        orbit.angularSpeed = 2f; // Velocidad base (se irá ajustando con la dificultad)
        orbit.center = center.transform;
        
        // FlashEffect
        FlashEffect flash = player.AddComponent<FlashEffect>();
        orbit.flashEffect = flash;
        orbit.spriteRenderer = sr;
        
        // StarParticleTrail
        player.AddComponent<StarParticleTrail>();
        
        // PlanetSurface
        PlanetSurface planetSurface = player.AddComponent<PlanetSurface>();
        planetSurface.rotationSpeed = 20f;
        
        // PlanetIdleAnimator
        PlanetIdleAnimator idleAnimator = player.AddComponent<PlanetIdleAnimator>();
        idleAnimator.rotationSpeed = 15f;
        idleAnimator.scaleAmplitude = 0.03f;
        idleAnimator.scaleFrequency = 0.4f;
        idleAnimator.glowAmplitude = 0.15f;
        idleAnimator.glowFrequency = 1.2f;
        
        // PlanetDestructionController
        PlanetDestructionController destructionController = player.AddComponent<PlanetDestructionController>();
        destructionController.fragmentCount = 8;
        destructionController.fragmentSpeed = 5f;
        destructionController.fragmentGravity = 2f;
        destructionController.particleCount = 40;
        destructionController.particleSpeed = 7f;
        
        // Actualizar referencia en InputController
        InputController inputController = FindFirstObjectByType<InputController>();
        if (inputController != null)
        {
            inputController.enabled = true;
        }
        
        // Efecto visual de respawn (flash + partículas)
        StartCoroutine(RespawnEffect(player));
        
        Log("[ReviveManager] Jugador recreado exitosamente");
    }
    
    /// <summary>
    /// Efecto visual de respawn (destello + aparición gradual)
    /// </summary>
    private IEnumerator RespawnEffect(GameObject player)
    {
        if (player == null) yield break;
        
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        
        // Hacer el sprite transparente al inicio
        Color originalColor = sr.color;
        sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        
        // Crear flash de respawn
        GameObject flashObj = new GameObject("RespawnFlash");
        flashObj.transform.position = player.transform.position;
        SpriteRenderer flashSR = flashObj.AddComponent<SpriteRenderer>();
        flashSR.sprite = SpriteGenerator.CreateStarSprite(1.5f, Color.white);
        flashSR.color = new Color(1f, 1f, 1f, 0.8f);
        flashSR.sortingOrder = 15;
        
        // Animar flash (expandir y desvanecer)
        float flashDuration = 0.5f;
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            
            if (flashObj != null)
            {
                flashObj.transform.localScale = Vector3.one * (1f + t * 2f);
                flashSR.color = new Color(1f, 1f, 1f, 0.8f * (1f - t));
                flashObj.transform.position = player.transform.position;
            }
            
            yield return null;
        }
        
        if (flashObj != null) Destroy(flashObj);
        
        // Aparecer gradualmente (fade in)
        elapsed = 0f;
        float fadeInDuration = 0.3f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            
            if (sr != null)
            {
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, t);
            }
            
            yield return null;
        }
        
        if (sr != null)
        {
            sr.color = originalColor;
        }
    }
    
    /// <summary>
    /// Activa la invulnerabilidad post-revive
    /// </summary>
    private void StartInvulnerability()
    {
        isPlayerInvulnerable = true;
        invulnerabilityTimer = invulnerabilityDuration;
        
        Log($"[ReviveManager] Invulnerabilidad activada por {invulnerabilityDuration}s");
        
        // Efecto visual de invulnerabilidad (parpadeo)
        StartCoroutine(InvulnerabilityVisualEffect());
    }
    
    /// <summary>
    /// Finaliza la invulnerabilidad
    /// </summary>
    private void EndInvulnerability()
    {
        isPlayerInvulnerable = false;
        invulnerabilityTimer = 0f;
        
        Log("[ReviveManager] Invulnerabilidad finalizada");
        
        // Asegurar que el sprite sea visible al 100%
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        
        if (player != null)
        {
            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, 1f);
            }
        }
    }
    
    /// <summary>
    /// Efecto visual de parpadeo durante la invulnerabilidad
    /// </summary>
    private IEnumerator InvulnerabilityVisualEffect()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        
        if (player == null) yield break;
        
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        
        Color originalColor = sr.color;
        float blinkInterval = 0.15f;
        bool visible = true;
        
        while (isPlayerInvulnerable)
        {
            // Parpadear entre visible e invisible
            visible = !visible;
            
            if (sr != null)
            {
                float alpha = visible ? 1f : 0.3f;
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }
            else
            {
                yield break;
            }
            
            yield return new WaitForSeconds(blinkInterval);
        }
        
        // Asegurar que el sprite sea visible al finalizar
        if (sr != null)
        {
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
        }
    }
    
    /// <summary>
    /// Countdown para la decisión de revive
    /// </summary>
    private IEnumerator ReviveCountdown()
    {
        float timeRemaining = decisionTimeout;
        Text countdownText = null;
        
        // Buscar el texto del countdown en la UI
        if (reviveUIRoot != null)
        {
            Transform countdownTransform = reviveUIRoot.transform.Find("RevivePanel/CountdownText");
            if (countdownTransform != null)
            {
                countdownText = countdownTransform.GetComponent<Text>();
            }
        }
        
        while (timeRemaining > 0f)
        {
            if (!isShowingReviveUI)
            {
                yield break;
            }

            // Congelar countdown mientras el anuncio está en pantalla.
            if (isWaitingForAdResult)
            {
                if (countdownText != null)
                {
                    countdownText.text = Mathf.CeilToInt(timeRemaining).ToString();
                }

                yield return null;
                continue;
            }

            // Usar unscaledDeltaTime porque el juego está pausado (timeScale = 0)
            timeRemaining -= Time.unscaledDeltaTime;
            
            if (countdownText != null)
            {
                countdownText.text = Mathf.CeilToInt(timeRemaining).ToString();
            }
            
            yield return null;
        }
        
        // Si ya no estamos mostrando revive, no hacer nada.
        if (!isShowingReviveUI)
        {
            yield break;
        }

        // Si justo terminó el tiempo pero hay anuncio en curso, no declinar en segundo plano.
        if (isWaitingForAdResult)
        {
            yield break;
        }

        // Tiempo agotado, declinar automáticamente
        Log("[ReviveManager] Tiempo de decisión agotado");
        DeclineRevive();
    }

    private void SetReviveButtonsInteractable(bool interactable)
    {
        if (reviveUIRoot == null) return;

        Button[] buttons = reviveUIRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].interactable = interactable;
            }
        }
    }
    
    /// <summary>
    /// Carga el sprite del jugador seleccionado
    /// </summary>
    private Sprite LoadPlayerSprite()
    {
        string selectedPlanet = PlayerPrefs.GetString("SelectedPlanet", "AsteroideErrante");
        
        // Intentar cargar desde Resources
        Sprite sprite = ResourceLoader.LoadSprite($"Art/Protagonist/{selectedPlanet}", selectedPlanet);
        if (sprite != null && sprite.name != "DefaultSprite")
        {
            return sprite;
        }
        
        // Mapeo para caracteres especiales
        if (selectedPlanet == "PlanetaOceanico")
        {
            sprite = ResourceLoader.LoadSprite("Art/Protagonist/PlanetaOceánico", "PlanetaOceánico");
            if (sprite != null && sprite.name != "DefaultSprite") return sprite;
        }
        
        #if UNITY_EDITOR
        try
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{selectedPlanet} t:Sprite");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }
        catch { }
        #endif
        
        return null;
    }
    
    // ==========================================
    // UI DE REVIVE
    // ==========================================
    
    /// <summary>
    /// Crea la interfaz de usuario para la opción de revive
    /// </summary>
    private void CreateReviveUI()
    {
        if (reviveUIRoot != null) Destroy(reviveUIRoot);
        
        // Buscar Canvas existente
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            LogError("[ReviveManager] No se encontró Canvas para la UI de revive");
            DeclineRevive();
            return;
        }
        
        // Asegurar que el Canvas tiene GraphicRaycaster
        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
        
        // Asegurar que existe EventSystem (necesario para que los botones funcionen)
        if (EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Log("[ReviveManager] EventSystem creado");
        }
        
        // Root
        reviveUIRoot = new GameObject("ReviveUI");
        reviveUIRoot.transform.SetParent(canvas.transform, false);
        reviveUIRoot.transform.SetAsLastSibling(); // Asegurar que está encima de todo
        RectTransform rootRect = reviveUIRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        
        // Fondo oscuro semitransparente
        Image overlay = reviveUIRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.7f);
        overlay.raycastTarget = true; // Bloquear clicks fuera del panel (los botones hijos tienen prioridad)
        
        // Panel central
        GameObject panel = new GameObject("RevivePanel");
        panel.transform.SetParent(reviveUIRoot.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500, 380);
        
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = CosmicTheme.GlassPanel;
        
        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.6f);
        panelOutline.effectDistance = new Vector2(2, 2);
        
        // Countdown circular (número grande en la esquina)
        CreateCountdownDisplay(panel.transform);
        
        // Título "¿Segunda Oportunidad?"
        CreateTitle(panel.transform);
        
        // Botón: Ver anuncio (opción principal)
        CreateAdButton(panel.transform);
        
        // Botón: Gastar Crystals
        CreateCrystalButton(panel.transform);
        
        // Botón: Declinar
        CreateDeclineButton(panel.transform);
        
        // Animación de entrada
        StartCoroutine(AnimateReviveUI(panel));
    }
    
    private void CreateCountdownDisplay(Transform parent)
    {
        GameObject countdownObj = new GameObject("CountdownText");
        countdownObj.transform.SetParent(parent, false);
        
        RectTransform countdownRect = countdownObj.AddComponent<RectTransform>();
        countdownRect.anchorMin = new Vector2(0.5f, 1f);
        countdownRect.anchorMax = new Vector2(0.5f, 1f);
        countdownRect.pivot = new Vector2(0.5f, 1f);
        countdownRect.anchoredPosition = new Vector2(0, -15);
        countdownRect.sizeDelta = new Vector2(60, 60);
        
        Text countdownText = countdownObj.AddComponent<Text>();
        countdownText.text = Mathf.CeilToInt(decisionTimeout).ToString();
        countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countdownText.fontSize = 36;
        countdownText.fontStyle = FontStyle.Bold;
        countdownText.color = CosmicTheme.NeonCyan;
        countdownText.alignment = TextAnchor.MiddleCenter;
        
        AccessibilityHelper.ApplyAccessibilityToText(countdownText);
    }
    
    private void CreateTitle(Transform parent)
    {
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(parent, false);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -75);
        titleRect.sizeDelta = new Vector2(450, 50);
        
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "¿Segunda Oportunidad?";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = CosmicTheme.SpaceWhite;
        titleText.alignment = TextAnchor.MiddleCenter;
        
        AccessibilityHelper.ApplyAccessibilityToText(titleText);
        
        // Glow
        Outline titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.5f);
        titleOutline.effectDistance = new Vector2(1, 1);
    }
    
    private void CreateAdButton(Transform parent)
    {
        GameObject buttonObj = new GameObject("AdButton");
        buttonObj.transform.SetParent(parent, false);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0, 20);
        buttonRect.sizeDelta = new Vector2(380, 60);
        
        Image buttonBg = buttonObj.AddComponent<Image>();
        buttonBg.color = new Color(CosmicTheme.NeonCyan.r * 0.3f, CosmicTheme.NeonCyan.g * 0.3f, CosmicTheme.NeonCyan.b * 0.3f, 0.9f);
        
        Outline buttonOutline = buttonObj.AddComponent<Outline>();
        buttonOutline.effectColor = CosmicTheme.NeonCyan;
        buttonOutline.effectDistance = new Vector2(2, 2);
        
        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.3f);
        colors.pressedColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.5f);
        button.colors = colors;
        button.onClick.AddListener(() => ReviveWithAd());
        
        // Texto del botón
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = "▶  Ver Anuncio  (Gratis)";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 24;
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.color = CosmicTheme.NeonCyan;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        AccessibilityHelper.ApplyAccessibilityToText(buttonText);
    }
    
    private void CreateCrystalButton(Transform parent)
    {
        GameObject buttonObj = new GameObject("CrystalButton");
        buttonObj.transform.SetParent(parent, false);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0, -50);
        buttonRect.sizeDelta = new Vector2(380, 50);
        
        Image buttonBg = buttonObj.AddComponent<Image>();
        buttonBg.color = new Color(CosmicTheme.NeonMagenta.r * 0.2f, CosmicTheme.NeonMagenta.g * 0.2f, CosmicTheme.NeonMagenta.b * 0.2f, 0.9f);
        
        Outline buttonOutline = buttonObj.AddComponent<Outline>();
        buttonOutline.effectColor = new Color(CosmicTheme.NeonMagenta.r, CosmicTheme.NeonMagenta.g, CosmicTheme.NeonMagenta.b, 0.7f);
        buttonOutline.effectDistance = new Vector2(1, 1);
        
        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(CosmicTheme.NeonMagenta.r, CosmicTheme.NeonMagenta.g, CosmicTheme.NeonMagenta.b, 0.3f);
        colors.pressedColor = new Color(CosmicTheme.NeonMagenta.r, CosmicTheme.NeonMagenta.g, CosmicTheme.NeonMagenta.b, 0.5f);
        button.colors = colors;
        
        // Verificar si hay suficientes crystals
        int crystals = CurrencyManager.Instance != null ? CurrencyManager.Instance.CosmicCrystals : 0;
        bool canAfford = crystals >= reviveCrystalCost;
        
        button.onClick.AddListener(() => ReviveWithCrystals());
        button.interactable = canAfford;
        
        // Texto del botón
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = canAfford 
            ? $"💎  {reviveCrystalCost} Cosmic Crystals" 
            : $"💎  {reviveCrystalCost} Cosmic Crystals (insuficientes)";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 20;
        buttonText.fontStyle = FontStyle.Normal;
        buttonText.color = canAfford ? CosmicTheme.NeonMagenta : new Color(0.5f, 0.5f, 0.5f, 0.7f);
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        AccessibilityHelper.ApplyAccessibilityToText(buttonText);
    }
    
    private void CreateDeclineButton(Transform parent)
    {
        GameObject buttonObj = new GameObject("DeclineButton");
        buttonObj.transform.SetParent(parent, false);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0, 20);
        buttonRect.sizeDelta = new Vector2(200, 40);
        
        Button button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(() => DeclineRevive());
        
        // Sin fondo (botón transparente)
        Image buttonBg = buttonObj.AddComponent<Image>();
        buttonBg.color = Color.clear;
        
        // Texto
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = "No, gracias";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 18;
        buttonText.fontStyle = FontStyle.Normal;
        buttonText.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        AccessibilityHelper.ApplyAccessibilityToText(buttonText);
    }
    
    /// <summary>
    /// Animación de entrada de la UI de revive (escala desde 0)
    /// </summary>
    private IEnumerator AnimateReviveUI(GameObject panel)
    {
        if (panel == null) yield break;
        
        panel.transform.localScale = Vector3.zero;
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Usar unscaled porque timeScale = 0
            float t = elapsed / duration;
            
            // Ease out bounce
            float scale = 1f;
            if (t < 0.6f)
            {
                scale = (t / 0.6f) * 1.1f;
            }
            else
            {
                scale = 1.1f - (t - 0.6f) / 0.4f * 0.1f;
            }
            
            if (panel != null)
            {
                panel.transform.localScale = Vector3.one * scale;
            }
            
            yield return null;
        }
        
        if (panel != null)
        {
            panel.transform.localScale = Vector3.one;
        }
    }
}
