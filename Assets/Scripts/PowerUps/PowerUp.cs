using UnityEngine;
using static LogHelper;

/// <summary>
/// Tipos de power-up disponibles
/// </summary>
public enum PowerUpType
{
    Shield,       // 🛡️ Invulnerabilidad temporal (3s)
    Slowmo,       // ⚡ Ralentiza obstáculos 50% (4s)
    Magnet,       // 🧲 Atrae shards cercanos (5s)
    DoublePoints  // 💫 Puntuación ×2 (5s)
}

/// <summary>
/// Power-Up coleccionable que aparece en la órbita del jugador.
/// Se recoge al pasar por encima y activa un efecto temporal.
/// Visualmente más grande y llamativo que los shards para ser distinguible.
/// </summary>
public class PowerUp : MonoBehaviour
{
    [Header("Power-Up Settings")]
    public PowerUpType type;
    
    [Header("Visual")]
    public float pulseSpeed = 3.5f;
    public float pulseAmplitude = 0.25f;
    public float rotationSpeed = 120f;
    
    [Header("Lifetime")]
    public float lifetime = 7f; // Más tiempo que shards para dar oportunidad de cogerlo
    
    private float spawnTime;
    private float baseScale;
    private SpriteRenderer spriteRenderer;
    private bool isCollected = false;
    
    // Colores por tipo
    public static readonly Color ShieldColor = new Color(0.3f, 0.65f, 1f, 1f);       // Azul eléctrico
    public static readonly Color SlowmoColor = new Color(0.7f, 0.35f, 1f, 1f);       // Púrpura intenso
    public static readonly Color MagnetColor = new Color(1f, 0.5f, 0.2f, 1f);        // Naranja cálido
    public static readonly Color DoublePointsColor = new Color(1f, 0.85f, 0.0f, 1f); // Dorado brillante
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spawnTime = Time.time;
    }
    
    /// <summary>
    /// Configura el power-up según su tipo
    /// </summary>
    public void Setup(PowerUpType powerUpType)
    {
        type = powerUpType;
        
        Color color = GetColorForType(type);
        float scale = 1.3f; // Más grande que shards (~0.9)
        
        SetupVisual(color, scale);
    }
    
    /// <summary>
    /// Obtiene el color asociado a un tipo de power-up
    /// </summary>
    public static Color GetColorForType(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Shield: return ShieldColor;
            case PowerUpType.Slowmo: return SlowmoColor;
            case PowerUpType.Magnet: return MagnetColor;
            case PowerUpType.DoublePoints: return DoublePointsColor;
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// Obtiene el nombre legible de un tipo de power-up
    /// </summary>
    public static string GetNameForType(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Shield: return "ESCUDO";
            case PowerUpType.Slowmo: return "SLOWMO";
            case PowerUpType.Magnet: return "IMÁN";
            case PowerUpType.DoublePoints: return "×2 PUNTOS";
            default: return "";
        }
    }
    
    /// <summary>
    /// Obtiene el emoji/icono de un tipo de power-up
    /// </summary>
    public static string GetIconForType(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Shield: return "🛡️";
            case PowerUpType.Slowmo: return "⏳";
            case PowerUpType.Magnet: return "🧲";
            case PowerUpType.DoublePoints: return "✨";
            default: return "⭐";
        }
    }
    
    private void SetupVisual(Color color, float scaleMultiplier)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = CreatePowerUpSprite(type, color);
            spriteRenderer.color = Color.white; // Color ya está en el sprite
            spriteRenderer.sortingOrder = 12; // Por encima de shards (11) y jugador (10)
        }
        
        transform.localScale = Vector3.one * scaleMultiplier;
        baseScale = scaleMultiplier;
        
        // Glow para todos los power-ups
        CreateGlowEffect(color);
    }
    
    private void Update()
    {
        if (isCollected) return;
        
        float elapsed = Time.time - spawnTime;
        
        // Autodestrucción por timeout
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        
        // Animación de pulso (más pronunciada que shards)
        float pulse = 1f + Mathf.Sin(elapsed * pulseSpeed) * pulseAmplitude;
        transform.localScale = Vector3.one * baseScale * pulse;
        
        // Rotación constante
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        
        // Fade out en los últimos 2 segundos
        float remaining = lifetime - elapsed;
        if (remaining < 2f && spriteRenderer != null)
        {
            float fadeAlpha = remaining / 2f;
            // Parpadeo rápido en el último segundo
            if (remaining < 1f)
            {
                fadeAlpha *= (Mathf.Sin(elapsed * 15f) * 0.5f + 0.5f);
            }
            spriteRenderer.color = new Color(1f, 1f, 1f, fadeAlpha);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;
        
        bool isPlayer = collision.CompareTag("Player") || 
                        collision.gameObject.name == "Player" ||
                        collision.gameObject.CompareTag("Player");
        
        if (isPlayer)
        {
            Collect();
        }
    }
    
    private void Collect()
    {
        isCollected = true;
        
        // Notificar al manager
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpCollected(this);
        }
        
        // Feedback visual
        Vector3 worldPos = transform.position;
        Color color = GetColorForType(type);
        
        CreateCollectionFeedback(worldPos, color);
        SpawnCollectionParticles(worldPos, color);
        
        // Feedback háptico
        if (GameFeedbackManager.Instance != null)
        {
            GameFeedbackManager.Instance.OnPowerUpCollected();
        }
        
        // Destruir inmediatamente
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Texto flotante con el nombre del power-up
    /// </summary>
    private void CreateCollectionFeedback(Vector3 worldPos, Color color)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        
        GameObject feedbackObj = new GameObject("PowerUpFeedback");
        feedbackObj.transform.SetParent(canvas.transform, false);
        
        UnityEngine.UI.Text feedbackText = feedbackObj.AddComponent<UnityEngine.UI.Text>();
        feedbackText.text = $"{GetIconForType(type)} {GetNameForType(type)}";
        feedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        feedbackText.fontSize = 26;
        feedbackText.alignment = TextAnchor.MiddleCenter;
        feedbackText.raycastTarget = false;
        feedbackText.color = color;
        feedbackText.fontStyle = FontStyle.Bold;
        
        UnityEngine.UI.Outline outline = feedbackObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, 1.5f);
        
        RectTransform rect = feedbackObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 40);
        
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            rect.position = cam.WorldToScreenPoint(worldPos);
        }
        
        // Auto-animador
        ShardFeedbackAnimator animator = feedbackObj.AddComponent<ShardFeedbackAnimator>();
        animator.Initialize(rect.position, color);
    }
    
    /// <summary>
    /// Partículas de colección (más numerosas que shards)
    /// </summary>
    private void SpawnCollectionParticles(Vector3 pos, Color particleColor)
    {
        int particleCount = 10;
        
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject("PowerUpParticle");
            particle.transform.position = pos;
            
            SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateCircleSprite(0.06f, particleColor);
            sr.color = particleColor;
            sr.sortingOrder = 13;
            
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(2f, 5f);
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            
            Rigidbody2D rb = particle.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = velocity;
            rb.linearDamping = 3f;
            
            particle.transform.localScale = Vector3.one * Random.Range(0.3f, 0.6f);
            
            Destroy(particle, 0.6f);
        }
    }
    
    private void CreateGlowEffect(Color color)
    {
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = Vector3.zero;
        
        SpriteRenderer glowSR = glow.AddComponent<SpriteRenderer>();
        float glowAlpha = 0.4f;
        Color glowColor = new Color(color.r, color.g, color.b, glowAlpha);
        glowSR.sprite = SpriteGenerator.CreateCircleSprite(0.35f, glowColor);
        glowSR.color = glowColor;
        glowSR.sortingOrder = 11; // Debajo del icono
        
        glow.transform.localScale = Vector3.one * 2.5f;
    }
    
    // =========================================================================
    // GENERACIÓN DE SPRITES PROCEDURALES
    // Cada tipo tiene una forma única para distinguirlo de las shards (estrellitas)
    // =========================================================================
    
    /// <summary>
    /// Crea el sprite adecuado según el tipo de power-up
    /// </summary>
    private static Sprite CreatePowerUpSprite(PowerUpType type, Color color)
    {
        switch (type)
        {
            case PowerUpType.Shield: return CreateShieldSprite(color);
            case PowerUpType.Slowmo: return CreateSlowmoSprite(color);
            case PowerUpType.Magnet: return CreateMagnetSprite(color);
            case PowerUpType.DoublePoints: return CreateDoublePointsSprite(color);
            default: return SpriteGenerator.CreateCircleSprite(0.3f, color);
        }
    }
    
    /// <summary>
    /// Escudo: Forma de diamante/hexágono con núcleo brillante
    /// </summary>
    private static Sprite CreateShieldSprite(Color color)
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxRadius = size / 2f - 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center.x);
                float dy = Mathf.Abs(y - center.y);
                float dist = Vector2.Distance(new Vector2(x, y), center);
                
                // Forma de diamante: |dx| + |dy| < radius
                float diamondDist = dx + dy;
                
                if (diamondDist < maxRadius * 0.85f)
                {
                    float edge = 1f - (diamondDist / (maxRadius * 0.85f));
                    
                    // Núcleo blanco brillante
                    float coreAlpha = 0f;
                    if (dist < maxRadius * 0.3f)
                    {
                        coreAlpha = 1f;
                    }
                    else if (dist < maxRadius * 0.45f)
                    {
                        coreAlpha = 1f - ((dist - maxRadius * 0.3f) / (maxRadius * 0.15f));
                    }
                    
                    float alpha = Mathf.Max(edge * 0.9f, coreAlpha);
                    float whiteness = Mathf.Clamp01(coreAlpha * 0.8f);
                    Color pixelColor = Color.Lerp(color, Color.white, whiteness);
                    
                    // Borde brillante
                    if (diamondDist > maxRadius * 0.7f)
                    {
                        pixelColor = Color.Lerp(pixelColor, Color.white, 0.5f);
                    }
                    
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
    
    /// <summary>
    /// Slowmo: Reloj de arena / espiral temporal
    /// </summary>
    private static Sprite CreateSlowmoSprite(Color color)
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxRadius = size / 2f - 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float angle = Mathf.Atan2(dy, dx);
                
                float alpha = 0f;
                
                // Anillo exterior
                float ringOuter = maxRadius * 0.85f;
                float ringInner = maxRadius * 0.65f;
                if (dist >= ringInner && dist <= ringOuter)
                {
                    float ringEdge = 1f - Mathf.Abs(dist - (ringInner + ringOuter) * 0.5f) / ((ringOuter - ringInner) * 0.5f);
                    alpha = ringEdge * 0.9f;
                }
                
                // Núcleo central brillante
                if (dist < maxRadius * 0.25f)
                {
                    alpha = Mathf.Max(alpha, 1f);
                }
                else if (dist < maxRadius * 0.4f)
                {
                    float core = 1f - ((dist - maxRadius * 0.25f) / (maxRadius * 0.15f));
                    alpha = Mathf.Max(alpha, core);
                }
                
                // "Manecillas" del reloj (3 líneas radiales)
                for (int h = 0; h < 3; h++)
                {
                    float handAngle = h * Mathf.PI * 2f / 3f;
                    float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, handAngle * Mathf.Rad2Deg));
                    if (angleDiff < 8f && dist < maxRadius * 0.6f && dist > maxRadius * 0.15f)
                    {
                        float handAlpha = (1f - angleDiff / 8f) * 0.8f;
                        alpha = Mathf.Max(alpha, handAlpha);
                    }
                }
                
                if (alpha > 0.01f)
                {
                    float whiteness = dist < maxRadius * 0.3f ? 0.8f : 0f;
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
    
    /// <summary>
    /// Imán: Forma de herradura / anillo abierto
    /// </summary>
    private static Sprite CreateMagnetSprite(Color color)
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxRadius = size / 2f - 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float angle = Mathf.Atan2(dy, dx); // -PI to PI
                
                float alpha = 0f;
                
                // Anillo en forma de C (herradura abierta por abajo)
                float outerRadius = maxRadius * 0.85f;
                float innerRadius = maxRadius * 0.5f;
                
                if (dist >= innerRadius && dist <= outerRadius)
                {
                    // Ángulo en grados: la apertura está abajo (-90° ± 40°)
                    float angleDeg = angle * Mathf.Rad2Deg;
                    float gapCenter = -90f;
                    float gapHalf = 35f;
                    
                    float angleDiffFromGap = Mathf.Abs(Mathf.DeltaAngle(angleDeg, gapCenter));
                    
                    if (angleDiffFromGap > gapHalf)
                    {
                        float ringEdge = 1f - Mathf.Abs(dist - (innerRadius + outerRadius) * 0.5f) / ((outerRadius - innerRadius) * 0.5f);
                        alpha = Mathf.Pow(ringEdge, 0.7f) * 0.95f;
                        
                        // Puntas más brillantes (los extremos de la herradura)
                        if (angleDiffFromGap < gapHalf + 20f)
                        {
                            alpha = Mathf.Min(1f, alpha * 1.3f);
                        }
                    }
                }
                
                // Centro pequeño brillante
                if (dist < maxRadius * 0.2f)
                {
                    float core = 1f - dist / (maxRadius * 0.2f);
                    alpha = Mathf.Max(alpha, core * 0.6f);
                }
                
                if (alpha > 0.01f)
                {
                    // Puntas rojas, cuerpo naranja
                    float whiteness = dist < maxRadius * 0.25f ? 0.7f : 0f;
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
    
    /// <summary>
    /// Doble Puntos: Estrella de 8 puntas (starburst)
    /// </summary>
    private static Sprite CreateDoublePointsSprite(Color color)
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxRadius = size / 2f - 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float angle = Mathf.Atan2(dy, dx);
                
                float alpha = 0f;
                
                // Estrella de 8 puntas
                // La distancia máxima varía con el ángulo
                float starAngle = angle * 4f; // 8 puntas (4 * 2)
                float starRadius = maxRadius * (0.45f + 0.4f * Mathf.Abs(Mathf.Cos(starAngle)));
                
                if (dist < starRadius)
                {
                    float edge = 1f - (dist / starRadius);
                    alpha = Mathf.Pow(edge, 0.6f);
                }
                
                // Núcleo central muy brillante
                if (dist < maxRadius * 0.3f)
                {
                    alpha = Mathf.Max(alpha, 1f);
                }
                else if (dist < maxRadius * 0.45f)
                {
                    float core = 1f - ((dist - maxRadius * 0.3f) / (maxRadius * 0.15f));
                    alpha = Mathf.Max(alpha, core);
                }
                
                // Halo suave
                if (dist < maxRadius * 0.6f && alpha < 0.3f)
                {
                    float halo = 0.3f * (1f - dist / (maxRadius * 0.6f));
                    alpha = Mathf.Max(alpha, halo);
                }
                
                if (alpha > 0.01f)
                {
                    float whiteness = Mathf.Clamp01((dist < maxRadius * 0.35f) ? 0.85f : 0.1f);
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
}
