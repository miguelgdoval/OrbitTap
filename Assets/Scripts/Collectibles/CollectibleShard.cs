using UnityEngine;
using static LogHelper;

/// <summary>
/// Stellar Shard coleccionable que aparece en la órbita del jugador.
/// El jugador lo recoge al pasar por encima.
/// </summary>
public class CollectibleShard : MonoBehaviour
{
    public enum ShardRarity
    {
        Normal,   // 2 ⭐ (80%)
        Rare,     // 5 ⭐ (15%)
        UltraRare // 15 ⭐ (5%)
    }
    
    [Header("Shard Settings")]
    public ShardRarity rarity = ShardRarity.Normal;
    public int value = 2;
    
    [Header("Visual")]
    public float pulseSpeed = 2f;
    public float pulseAmplitude = 0.15f;
    public float rotationSpeed = 45f;
    
    [Header("Lifetime")]
    public float lifetime = 4.5f; // Corto: ~1.5 órbitas para recogerlo → requiere reacción rápida
    
    private float spawnTime;
    private float baseScale;
    private SpriteRenderer spriteRenderer;
    private bool isCollected = false;
    
    // Colores por rareza
    private static readonly Color NormalColor = new Color(1f, 0.85f, 0.4f, 1f);      // Dorado cálido
    private static readonly Color RareColor = new Color(0.4f, 0.9f, 1f, 1f);          // Cian brillante
    private static readonly Color UltraRareColor = new Color(1f, 0.6f, 1f, 1f);       // Rosa-magenta
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spawnTime = Time.time;
        baseScale = transform.localScale.x;
    }
    
    /// <summary>
    /// Configura el shard según su rareza
    /// </summary>
    public void Setup(ShardRarity shardRarity)
    {
        rarity = shardRarity;
        
        switch (rarity)
        {
            case ShardRarity.Normal:
                value = 2;
                SetupVisual(NormalColor, 0.9f);
                break;
            case ShardRarity.Rare:
                value = 5;
                SetupVisual(RareColor, 1.1f);
                pulseSpeed = 3f;
                break;
            case ShardRarity.UltraRare:
                value = 15;
                SetupVisual(UltraRareColor, 1.3f);
                pulseSpeed = 4f;
                pulseAmplitude = 0.25f;
                break;
        }
    }
    
    private void SetupVisual(Color color, float scaleMultiplier)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            // Usar sprite de estrellita de 4 puntas (más bonito que un círculo difuso)
            spriteRenderer.sprite = CreateSparkleSprite(color);
            spriteRenderer.color = Color.white; // El color ya está en el sprite
            spriteRenderer.sortingOrder = 11; // Por ENCIMA del jugador (sortingOrder 10)
        }
        
        // Escala pequeña — las estrellitas deben ser discretas
        transform.localScale = Vector3.one * scaleMultiplier;
        baseScale = scaleMultiplier;
        
        // Glow para todos los shards (más visible)
        CreateGlowEffect(color);
    }
    
    /// <summary>
    /// Crea un sprite de estrellita de 4 puntas (sparkle) con brillo central
    /// </summary>
    private static Sprite CreateSparkleSprite(Color color)
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxRadius = size / 2f - 1f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dx = Mathf.Abs(pos.x - center.x);
                float dy = Mathf.Abs(pos.y - center.y);
                float dist = Vector2.Distance(pos, center);
                
                // Forma de estrella de 4 puntas — puntas más largas y definidas
                float starShape = Mathf.Min(dx + dy * 3f, dy + dx * 3f);
                
                // Núcleo central blanco brillante (más grande)
                float coreAlpha = 0f;
                if (dist < maxRadius * 0.3f)
                {
                    coreAlpha = 1f;
                }
                else if (dist < maxRadius * 0.5f)
                {
                    coreAlpha = 1f - ((dist - maxRadius * 0.3f) / (maxRadius * 0.2f));
                }
                
                // Puntas de la estrella (más largas y brillantes)
                float spikeAlpha = 0f;
                if (starShape < maxRadius * 0.85f)
                {
                    spikeAlpha = 1f - (starShape / (maxRadius * 0.85f));
                    spikeAlpha = Mathf.Pow(spikeAlpha, 1.5f); // Suave pero definido
                }
                
                // Resplandor radial de fondo (halo suave)
                float glowAlpha = 0f;
                if (dist < maxRadius * 0.7f)
                {
                    glowAlpha = 0.3f * (1f - dist / (maxRadius * 0.7f));
                }
                
                float alpha = Mathf.Max(Mathf.Max(coreAlpha, spikeAlpha * 0.9f), glowAlpha);
                
                if (alpha > 0.01f)
                {
                    // Centro blanco puro, puntas con color saturado
                    float whiteness = Mathf.Clamp01(coreAlpha * 0.85f);
                    Color pixelColor = Color.Lerp(color, Color.white, whiteness);
                    colors[y * size + x] = new Color(pixelColor.r, pixelColor.g, pixelColor.b, alpha);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }
    
    private void CreateGlowEffect(Color color)
    {
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = Vector3.zero;
        
        SpriteRenderer glowSR = glow.AddComponent<SpriteRenderer>();
        float glowAlpha = rarity == ShardRarity.UltraRare ? 0.4f : 
                          rarity == ShardRarity.Rare ? 0.35f : 0.25f;
        Color glowColor = new Color(color.r, color.g, color.b, glowAlpha);
        glowSR.sprite = SpriteGenerator.CreateCircleSprite(0.3f, glowColor);
        glowSR.color = glowColor;
        glowSR.sortingOrder = 10;
        
        float glowScale = rarity == ShardRarity.UltraRare ? 2.5f : 
                          rarity == ShardRarity.Rare ? 2f : 1.6f;
        glow.transform.localScale = Vector3.one * glowScale;
    }
    
    private void Update()
    {
        if (isCollected) return;
        
        // Animación de pulso (breathing)
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
        transform.localScale = Vector3.one * baseScale * pulse;
        
        // Rotación suave
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        
        // Desvanecer gradualmente en los últimos 3 segundos
        float elapsed = Time.time - spawnTime;
        float remaining = lifetime - elapsed;
        
        if (remaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        
        if (remaining <= 1.5f && spriteRenderer != null)
        {
            float fadeAlpha = remaining / 3f;
            Color c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, fadeAlpha);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;
        
        bool isPlayer = false;
        try { isPlayer = collision.CompareTag("Player"); } catch { }
        if (!isPlayer) isPlayer = collision.gameObject.name == "Player";
        
        if (isPlayer)
        {
            Collect();
        }
    }
    
    private void Collect()
    {
        isCollected = true;
        
        // Aplicar multiplicador de Fever Mode (shards valen doble)
        int finalValue = value;
        bool isFeverActive = false;
        if (FeverModeManager.Instance != null && FeverModeManager.Instance.IsFeverActive)
        {
            finalValue = Mathf.RoundToInt(value * FeverModeManager.Instance.GetShardValueMultiplier());
            isFeverActive = true;
        }
        
        // Sobreescribir el valor para que el manager registre el valor con bonus
        int originalValue = value;
        value = finalValue;
        
        // Notificar al manager
        if (CollectibleManager.Instance != null)
        {
            CollectibleManager.Instance.OnShardCollected(this);
        }
        
        // Restaurar valor original (por si acaso)
        value = originalValue;
        
        // Guardar posición ANTES de destruir
        Vector3 worldPos = transform.position;
        Color feedbackColor = rarity == ShardRarity.UltraRare ? UltraRareColor :
                              rarity == ShardRarity.Rare ? RareColor : NormalColor;
        
        // Si Fever activo, usar color dorado para el feedback
        if (isFeverActive)
        {
            feedbackColor = new Color(1f, 0.85f, 0.2f, 1f); // Dorado fever
        }
        
        int feedbackSize = rarity == ShardRarity.UltraRare ? 28 : 
                           rarity == ShardRarity.Rare ? 24 : 20;
        
        // Crear feedback que se auto-anima (NO depende de esta coroutine)
        CreateSelfAnimatingFeedback(worldPos, finalValue, feedbackColor, feedbackSize);
        
        // Partículas de colección
        SpawnCollectionParticles(worldPos, feedbackColor);
        
        // Feedback háptico
        if (GameFeedbackManager.Instance != null)
        {
            GameFeedbackManager.Instance.OnShardCollected(value);
        }
        
        // Destruir el shard inmediatamente
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Crea un texto flotante "+X ⭐" que se auto-anima y auto-destruye.
    /// Usa un componente propio para que la animación no dependa del shard.
    /// </summary>
    private void CreateSelfAnimatingFeedback(Vector3 worldPos, int shardValue, Color color, int fontSize)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        
        GameObject feedbackObj = new GameObject("ShardFeedback");
        feedbackObj.transform.SetParent(canvas.transform, false);
        
        UnityEngine.UI.Text feedbackText = feedbackObj.AddComponent<UnityEngine.UI.Text>();
        feedbackText.text = $"+{shardValue} ⭐";
        feedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        feedbackText.fontSize = fontSize;
        feedbackText.alignment = TextAnchor.MiddleCenter;
        feedbackText.raycastTarget = false;
        feedbackText.color = color;
        
        UnityEngine.UI.Outline outline = feedbackObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        outline.effectDistance = new Vector2(1, 1);
        
        RectTransform rect = feedbackObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 40);
        
        // Posicionar en pantalla
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            rect.position = cam.WorldToScreenPoint(worldPos);
        }
        
        // Añadir componente auto-animador (se destruye solo)
        ShardFeedbackAnimator animator = feedbackObj.AddComponent<ShardFeedbackAnimator>();
        animator.Initialize(rect.position, color);
    }
    
    /// <summary>
    /// Crea partículas pequeñas de colección
    /// </summary>
    private void SpawnCollectionParticles(Vector3 pos, Color particleColor)
    {
        int particleCount = rarity == ShardRarity.UltraRare ? 8 : 
                            rarity == ShardRarity.Rare ? 5 : 3;
        
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject("ShardParticle");
            particle.transform.position = pos;
            
            SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateCircleSprite(0.05f, particleColor);
            sr.color = particleColor;
            sr.sortingOrder = 9;
            
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(1.5f, 3.5f);
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            
            Rigidbody2D rb = particle.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = velocity;
            rb.linearDamping = 4f;
            
            particle.transform.localScale = Vector3.one * Random.Range(0.2f, 0.4f);
            
            Destroy(particle, 0.5f);
        }
    }
}

/// <summary>
/// Componente auto-animador para el texto flotante "+X ⭐".
/// Vive en su propio GameObject y se destruye solo después de la animación.
/// </summary>
public class ShardFeedbackAnimator : MonoBehaviour
{
    private RectTransform rect;
    private UnityEngine.UI.Text text;
    private Vector2 startPos;
    private Color startColor;
    private float elapsed = 0f;
    private const float Duration = 0.8f;
    
    public void Initialize(Vector2 position, Color color)
    {
        startPos = position;
        startColor = color;
    }
    
    private void Start()
    {
        rect = GetComponent<RectTransform>();
        text = GetComponent<UnityEngine.UI.Text>();
        
        if (rect != null && startPos == Vector2.zero)
        {
            startPos = rect.position;
        }
        if (text != null && startColor == default)
        {
            startColor = text.color;
        }
    }
    
    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / Duration;
        
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        
        // Subir
        if (rect != null)
        {
            rect.position = startPos + Vector2.up * (60f * t);
        }
        
        // Desvanecer
        if (text != null)
        {
            float alpha = 1f - (t * t);
            text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }
    }
}
