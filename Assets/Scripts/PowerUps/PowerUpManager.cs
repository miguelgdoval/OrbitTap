using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using static LogHelper;

/// <summary>
/// Gestiona el spawning de power-ups, el efecto activo, y el HUD indicador.
/// 
/// Power-Ups disponibles:
/// - Shield (🛡️): 3s de invulnerabilidad (bloquea colisiones con feedback visual)
/// - Slowmo (⏳): Obstáculos al 50% de velocidad durante 4s
/// - Magnet (🧲): Atrae shards cercanos durante 10s
/// - Double Points (✨): Puntuación ×2 durante 5s
/// 
/// Spawn: cada 12-20s, máximo 1 en pantalla, con 8s de delay inicial.
/// Solo puede haber un power-up activo a la vez (el nuevo reemplaza al anterior).
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }
    
    [Header("Spawn Settings")]
    [Tooltip("Intervalo mínimo entre spawns (segundos)")]
    public float minSpawnInterval = 12f;
    [Tooltip("Intervalo máximo entre spawns (segundos)")]
    public float maxSpawnInterval = 20f;
    [Tooltip("Delay inicial antes de empezar a spawnear (segundos)")]
    public float initialDelay = 8f;
    [Tooltip("Máximo de power-ups en pantalla")]
    public int maxOnScreen = 1;
    
    [Header("Effect Durations")]
    public float shieldDuration = 3f;
    public float slowmoDuration = 4f;
    public float magnetDuration = 10f;
    public float doublePointsDuration = 5f;
    
    [Header("Magnet Settings")]
    [Tooltip("Radio de atracción del imán (unidades del mundo)")]
    public float magnetRadius = 4f;
    [Tooltip("Velocidad de atracción del imán")]
    public float magnetSpeed = 6f;
    
    [Header("Slowmo Settings")]
    [Tooltip("Multiplicador de velocidad de obstáculos durante slowmo (0.5 = mitad)")]
    public float slowmoMultiplier = 0.5f;
    
    [Header("Spawn Safety")]
    [Tooltip("Distancia mínima del player para spawnear (en ángulos)")]
    public float minAngleFromPlayer = 90f;
    
    // Estado de spawning
    private bool isSpawning = false;
    private float spawnTimer = 0f;
    private float nextSpawnTime;
    
    // Estado del efecto activo
    private PowerUpType? activePowerUp = null;
    private float activeTimer = 0f;
    private float activeDuration = 0f;
    
    // Referencias
    private PlayerOrbit playerOrbit;
    private Transform center;
    
    // HUD
    private GameObject hudRoot;
    private Image hudFillBar;
    private Image hudFillBackground;
    private Text hudLabel;
    private RectTransform hudFillRect;
    private float hudFillMaxWidth;
    
    // Propiedades públicas de solo lectura
    public bool HasActivePowerUp => activePowerUp.HasValue;
    public PowerUpType? ActivePowerUp => activePowerUp;
    
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
        // Actualizar timer del efecto activo
        if (activePowerUp.HasValue)
        {
            activeTimer -= Time.deltaTime;
            
            // Actualizar HUD
            UpdateHUD();
            
            // Efecto del imán: atraer shards
            if (activePowerUp == PowerUpType.Magnet)
            {
                ApplyMagnetEffect();
            }
            
            // Expiración
            if (activeTimer <= 0f)
            {
                DeactivatePowerUp();
            }
        }
        
        // Spawning
        if (!isSpawning) return;
        
        // Re-buscar player si la referencia se perdió
        if (playerOrbit == null)
        {
            playerOrbit = FindFirstObjectByType<PlayerOrbit>();
            if (playerOrbit == null) return;
        }
        
        if (center == null)
        {
            GameObject centerObj = GameObject.Find("Center");
            if (centerObj != null) center = centerObj.transform;
            if (center == null) return;
        }
        
        spawnTimer += Time.deltaTime;
        
        if (spawnTimer >= nextSpawnTime)
        {
            int currentOnScreen = CountActivePowerUpsOnScreen();
            
            if (currentOnScreen < maxOnScreen)
            {
                SpawnPowerUp();
            }
            
            spawnTimer = 0f;
            nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }
    
    // =========================================================================
    // SPAWNING
    // =========================================================================
    
    public void StartSpawning()
    {
        isSpawning = true;
        spawnTimer = 0f;
        nextSpawnTime = initialDelay;
        
        // Cachear referencias
        playerOrbit = FindFirstObjectByType<PlayerOrbit>();
        GameObject centerObj = GameObject.Find("Center");
        if (centerObj != null) center = centerObj.transform;
        
        Log($"[PowerUpManager] Spawning iniciado (delay={initialDelay}s, interval={minSpawnInterval}-{maxSpawnInterval}s, center={center != null}, player={playerOrbit != null})");
    }
    
    public void StopSpawning()
    {
        isSpawning = false;
        Log("[PowerUpManager] Spawning detenido");
    }
    
    public void ResumeSpawning()
    {
        isSpawning = true;
        Log("[PowerUpManager] Spawning reanudado");
    }
    
    public void ResetSession()
    {
        isSpawning = false;
        spawnTimer = 0f;
        DeactivatePowerUp();
        
        // Destruir power-ups activos en escena
        PowerUp[] activePowerUps = FindObjectsByType<PowerUp>(FindObjectsSortMode.None);
        foreach (PowerUp pu in activePowerUps)
        {
            if (pu != null) Destroy(pu.gameObject);
        }
        
        // Ocultar HUD
        if (hudRoot != null) hudRoot.SetActive(false);
        
        Log("[PowerUpManager] Sesión reseteada");
    }
    
    private int CountActivePowerUpsOnScreen()
    {
        PowerUp[] pups = FindObjectsByType<PowerUp>(FindObjectsSortMode.None);
        return pups.Length;
    }
    
    private void SpawnPowerUp()
    {
        if (center == null || playerOrbit == null)
        {
            Log($"[PowerUpManager] SpawnPowerUp abortado: center={center != null}, playerOrbit={playerOrbit != null}");
            return;
        }
        
        float orbitRadius = playerOrbit.radius;
        
        // Elegir tipo aleatorio
        PowerUpType type = DetermineType();
        
        // Encontrar ángulo seguro
        float spawnAngle = FindSafeSpawnAngle(orbitRadius);
        if (spawnAngle < 0f) return;
        
        // Crear el power-up
        GameObject puObj = new GameObject($"PowerUp_{type}");
        puObj.transform.position = center.position + new Vector3(
            Mathf.Cos(spawnAngle) * orbitRadius,
            Mathf.Sin(spawnAngle) * orbitRadius,
            0f
        );
        
        // SpriteRenderer
        SpriteRenderer sr = puObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 12;
        
        // Collider (trigger) — un poco más grande que shards
        CircleCollider2D collider = puObj.AddComponent<CircleCollider2D>();
        collider.radius = 0.45f;
        collider.isTrigger = true;
        
        // Rigidbody2D
        Rigidbody2D rb = puObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        
        // Componente PowerUp
        PowerUp pu = puObj.AddComponent<PowerUp>();
        pu.Setup(type);
        
        Log($"[PowerUpManager] Power-Up {type} spawneado en ángulo {spawnAngle * Mathf.Rad2Deg:F0}°");
    }
    
    /// <summary>
    /// Elige un tipo de power-up aleatorio (distribución equitativa)
    /// </summary>
    private PowerUpType DetermineType()
    {
        int roll = Random.Range(0, 4);
        switch (roll)
        {
            case 0: return PowerUpType.Shield;
            case 1: return PowerUpType.Slowmo;
            case 2: return PowerUpType.Magnet;
            case 3: return PowerUpType.DoublePoints;
            default: return PowerUpType.Shield;
        }
    }
    
    /// <summary>
    /// Encuentra un ángulo seguro para spawnear, lejos del jugador.
    /// Si no encuentra uno aleatorio, usa el lado opuesto al jugador.
    /// </summary>
    private float FindSafeSpawnAngle(float orbitRadius)
    {
        float playerAngle = playerOrbit.angle;
        
        for (int attempt = 0; attempt < 20; attempt++)
        {
            float candidateAngle = Random.Range(0f, Mathf.PI * 2f);
            
            float angleDiffFromPlayer = Mathf.Abs(Mathf.DeltaAngle(
                candidateAngle * Mathf.Rad2Deg,
                playerAngle * Mathf.Rad2Deg
            ));
            
            if (angleDiffFromPlayer < minAngleFromPlayer)
            {
                continue;
            }
            
            return candidateAngle;
        }
        
        // Fallback: spawnear en el lado opuesto al jugador (siempre válido)
        float oppositeAngle = playerAngle + Mathf.PI;
        // Añadir un poco de variación aleatoria (±30°)
        oppositeAngle += Random.Range(-30f, 30f) * Mathf.Deg2Rad;
        Log("[PowerUpManager] Usando fallback de ángulo opuesto para spawn");
        return oppositeAngle;
    }
    
    // =========================================================================
    // ACTIVACIÓN / DESACTIVACIÓN DE EFECTOS
    // =========================================================================
    
    /// <summary>
    /// Llamado por PowerUp cuando el jugador lo recoge
    /// </summary>
    public void OnPowerUpCollected(PowerUp powerUp)
    {
        // Si ya hay un power-up activo, desactivarlo primero
        if (activePowerUp.HasValue)
        {
            DeactivatePowerUp();
        }
        
        activePowerUp = powerUp.type;
        
        switch (powerUp.type)
        {
            case PowerUpType.Shield:
                activeDuration = shieldDuration;
                break;
            case PowerUpType.Slowmo:
                activeDuration = slowmoDuration;
                break;
            case PowerUpType.Magnet:
                activeDuration = magnetDuration;
                break;
            case PowerUpType.DoublePoints:
                activeDuration = doublePointsDuration;
                break;
        }
        
        activeTimer = activeDuration;
        
        // Mostrar HUD
        ShowHUD(powerUp.type);
        
        Log($"[PowerUpManager] Power-Up activado: {powerUp.type} ({activeDuration}s)");
    }
    
    /// <summary>
    /// Desactiva el power-up actual
    /// </summary>
    private void DeactivatePowerUp()
    {
        if (!activePowerUp.HasValue) return;
        
        Log($"[PowerUpManager] Power-Up expirado: {activePowerUp.Value}");
        
        activePowerUp = null;
        activeTimer = 0f;
        activeDuration = 0f;
        
        // Ocultar HUD
        if (hudRoot != null) hudRoot.SetActive(false);
    }
    
    // =========================================================================
    // GETTERS PÚBLICOS para sistemas externos
    // =========================================================================
    
    /// <summary>
    /// ¿Está el escudo activo? (Usado por ObstacleBase para invulnerabilidad)
    /// </summary>
    public bool IsShieldActive()
    {
        return activePowerUp == PowerUpType.Shield;
    }
    
    /// <summary>
    /// Multiplicador de velocidad de obstáculos (1.0 normal, 0.5 en slowmo)
    /// </summary>
    public float GetObstacleSpeedMultiplier()
    {
        if (activePowerUp == PowerUpType.Slowmo)
        {
            return slowmoMultiplier;
        }
        return 1f;
    }
    
    /// <summary>
    /// Multiplicador de puntuación (1.0 normal, 2.0 con double points)
    /// </summary>
    public float GetScoreMultiplier()
    {
        if (activePowerUp == PowerUpType.DoublePoints)
        {
            return 2f;
        }
        return 1f;
    }
    
    /// <summary>
    /// ¿Está el imán activo? (Usado externamente si se necesita)
    /// </summary>
    public bool IsMagnetActive()
    {
        return activePowerUp == PowerUpType.Magnet;
    }
    
    /// <summary>
    /// Llamado cuando el escudo bloquea una colisión. 
    /// Muestra feedback visual y destruye el obstáculo.
    /// </summary>
    public void OnShieldBlocked(Vector3 impactPosition)
    {
        Log("[PowerUpManager] ¡Escudo bloqueó un golpe!");
        
        // Efecto visual de impacto en el escudo
        StartCoroutine(ShieldImpactEffect(impactPosition));
        
        // Feedback visual (flash + shake) y háptico
        if (GameFeedbackManager.Instance != null)
        {
            GameFeedbackManager.Instance.OnShieldBlock(impactPosition);
        }
    }
    
    /// <summary>
    /// Efecto visual cuando el escudo bloquea un golpe
    /// </summary>
    private IEnumerator ShieldImpactEffect(Vector3 position)
    {
        // Crear partículas de impacto azules
        Color shieldColor = new Color(0.3f, 0.7f, 1f, 1f);
        
        for (int i = 0; i < 8; i++)
        {
            GameObject particle = new GameObject("ShieldParticle");
            particle.transform.position = position;
            
            SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateCircleSprite(0.08f, shieldColor);
            sr.color = shieldColor;
            sr.sortingOrder = 15;
            
            // Dirección aleatoria
            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = Random.Range(3f, 6f);
            
            StartCoroutine(AnimateShieldParticle(particle, dir, speed));
        }
        
        // Flash en el HUD
        if (hudRoot != null && hudFillBar != null)
        {
            Color originalColor = hudFillBar.color;
            hudFillBar.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            hudFillBar.color = originalColor;
        }
        else
        {
            yield return null;
        }
    }
    
    private IEnumerator AnimateShieldParticle(GameObject particle, Vector2 direction, float speed)
    {
        float duration = 0.4f;
        float timer = 0f;
        SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
        
        while (timer < duration && particle != null)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            particle.transform.position += (Vector3)(direction * speed * Time.deltaTime);
            
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f - t;
                sr.color = c;
            }
            
            float scale = 1f - t * 0.5f;
            particle.transform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        if (particle != null) Destroy(particle);
    }
    
    // =========================================================================
    // EFECTO DEL IMÁN
    // =========================================================================
    
    /// <summary>
    /// Atrae shards cercanos al jugador cuando el imán está activo
    /// </summary>
    private void ApplyMagnetEffect()
    {
        if (playerOrbit == null)
        {
            playerOrbit = FindFirstObjectByType<PlayerOrbit>();
            if (playerOrbit == null) return;
        }
        
        Vector3 playerPos = playerOrbit.transform.position;
        
        CollectibleShard[] shards = FindObjectsByType<CollectibleShard>(FindObjectsSortMode.None);
        foreach (CollectibleShard shard in shards)
        {
            if (shard == null) continue;
            
            float dist = Vector3.Distance(shard.transform.position, playerPos);
            
            if (dist < magnetRadius)
            {
                // Mover hacia el jugador — más rápido cuanto más cerca
                float speedFactor = 1f + (1f - dist / magnetRadius) * 2f;
                shard.transform.position = Vector3.MoveTowards(
                    shard.transform.position,
                    playerPos,
                    magnetSpeed * speedFactor * Time.deltaTime
                );
            }
        }
    }
    
    // =========================================================================
    // HUD - Barra de duración del power-up activo
    // =========================================================================
    
    /// <summary>
    /// Asigna la referencia de HUD creada externamente
    /// </summary>
    public void SetHUD(GameObject root, Image fillBar, Text label, RectTransform fillRect, float maxWidth)
    {
        hudRoot = root;
        hudFillBar = fillBar;
        hudLabel = label;
        hudFillRect = fillRect;
        hudFillMaxWidth = maxWidth;
        
        if (hudRoot != null) hudRoot.SetActive(false);
    }
    
    /// <summary>
    /// Muestra el HUD con el power-up activo
    /// </summary>
    private void ShowHUD(PowerUpType type)
    {
        if (hudRoot == null)
        {
            // Intentar crear HUD si no existe
            CreateHUDFallback();
            if (hudRoot == null) return;
        }
        
        Color color = PowerUp.GetColorForType(type);
        string label = $"{PowerUp.GetIconForType(type)} {PowerUp.GetNameForType(type)}";
        
        hudRoot.SetActive(true);
        
        if (hudLabel != null)
        {
            hudLabel.text = label;
            hudLabel.color = color;
        }
        
        if (hudFillBar != null)
        {
            hudFillBar.color = color;
        }
    }
    
    /// <summary>
    /// Actualiza la barra de progreso del HUD
    /// </summary>
    private void UpdateHUD()
    {
        if (hudFillRect == null || !activePowerUp.HasValue) return;
        
        float progress = Mathf.Clamp01(activeTimer / activeDuration);
        hudFillRect.sizeDelta = new Vector2(hudFillMaxWidth * progress, hudFillRect.sizeDelta.y);
        
        // Parpadeo cuando queda poco tiempo (último 25%)
        if (progress < 0.25f && hudFillBar != null)
        {
            float flash = Mathf.Sin(Time.time * 10f) * 0.3f + 0.7f;
            Color baseColor = PowerUp.GetColorForType(activePowerUp.Value);
            hudFillBar.color = new Color(baseColor.r, baseColor.g, baseColor.b, flash);
        }
    }
    
    /// <summary>
    /// Crea el HUD programáticamente si no fue creado por GameInitializer
    /// </summary>
    private void CreateHUDFallback()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        
        CreateHUDInCanvas(canvas.gameObject);
    }
    
    /// <summary>
    /// Crea los elementos de HUD dentro de un canvas dado
    /// </summary>
    public void CreateHUDInCanvas(GameObject canvas)
    {
        // Contenedor principal
        hudRoot = new GameObject("PowerUpHUD");
        hudRoot.transform.SetParent(canvas.transform, false);
        
        RectTransform rootRect = hudRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0, -150); // Debajo del score
        rootRect.sizeDelta = new Vector2(220, 35);
        
        // Fondo de la barra
        Image bgImage = hudRoot.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.5f);
        bgImage.raycastTarget = false;
        
        // Outline
        Outline bgOutline = hudRoot.AddComponent<Outline>();
        bgOutline.effectColor = new Color(1f, 1f, 1f, 0.2f);
        bgOutline.effectDistance = new Vector2(1, 1);
        
        // Barra de progreso (fill)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(hudRoot.transform, false);
        
        hudFillBar = fillObj.AddComponent<Image>();
        hudFillBar.color = Color.white;
        hudFillBar.raycastTarget = false;
        
        hudFillRect = fillObj.GetComponent<RectTransform>();
        hudFillRect.anchorMin = new Vector2(0, 0);
        hudFillRect.anchorMax = new Vector2(0, 1);
        hudFillRect.pivot = new Vector2(0, 0.5f);
        hudFillRect.offsetMin = new Vector2(3, 3);
        hudFillRect.offsetMax = new Vector2(3, -3);
        hudFillMaxWidth = 214f; // 220 - 6 padding
        hudFillRect.sizeDelta = new Vector2(hudFillMaxWidth, hudFillRect.sizeDelta.y);
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(hudRoot.transform, false);
        
        hudLabel = labelObj.AddComponent<Text>();
        hudLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hudLabel.fontSize = 18;
        hudLabel.fontStyle = FontStyle.Bold;
        hudLabel.alignment = TextAnchor.MiddleCenter;
        hudLabel.raycastTarget = false;
        hudLabel.color = Color.white;
        
        Outline labelOutline = labelObj.AddComponent<Outline>();
        labelOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        labelOutline.effectDistance = new Vector2(1, 1);
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;
        
        hudRoot.SetActive(false);
        
        Log("[PowerUpManager] HUD creado");
    }
}
