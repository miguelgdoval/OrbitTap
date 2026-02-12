using UnityEngine;
using System.Collections;
using static LogHelper;

/// <summary>
/// Rayo láser que cruza la pantalla a través de la órbita del jugador.
/// 
/// Secuencia:
/// 1. Aparece una línea de aviso fina y parpadeante (1.2s) — el jugador ve POR DÓNDE va a pasar.
/// 2. El láser se activa con un flash intenso y se expande al grosor completo (~0.3s).
/// 3. El láser permanece activo y peligroso durante un tiempo corto (0.8s).
/// 4. El láser se desvanece y desaparece.
/// 
/// El láser se posiciona tangencialmente a un punto de la órbita, asegurando que
/// el jugador DEBE moverse para evitarlo.
/// 
/// Dificultad: VeryHard — requiere reacción rápida al ver el aviso.
/// </summary>
[ObstacleDifficulty(ObstacleDifficultyLevel.VeryHard)]
public class LaserBeam : ObstacleBase, IObstacleDifficulty
{
    [Header("Timing")]
    [Tooltip("Duración del aviso antes de activarse (segundos)")]
    public float warningDuration = 1.2f;
    [Tooltip("Duración del flash de activación (segundos)")]
    public float activationFlashDuration = 0.15f;
    [Tooltip("Duración del láser activo y peligroso (segundos)")]
    public float activeDuration = 0.8f;
    [Tooltip("Duración del fade-out (segundos)")]
    public float fadeOutDuration = 0.3f;
    
    [Header("Visual")]
    [Tooltip("Grosor del láser cuando está activo")]
    public float beamWidth = 0.35f;
    [Tooltip("Grosor de la línea de aviso")]
    public float warningWidth = 0.06f;
    [Tooltip("Longitud del láser (debe cubrir toda la pantalla)")]
    public float beamLength = 20f;
    
    [Header("Colors")]
    public Color warningColor = new Color(0.7f, 0.3f, 1f, 0.5f);      // Púrpura tenue (estética cósmica)
    public Color beamColor = new Color(0.4f, 0.8f, 1f, 0.95f);        // Cyan brillante (estética cósmica)
    public Color beamCoreColor = new Color(0.937f, 0.851f, 1f, 1f);   // EtherealLila (núcleo)

    // Estado
    private enum LaserState { Warning, Activating, Active, FadingOut, Done }
    private LaserState currentState = LaserState.Warning;
    private float stateTimer = 0f;
    
    // Componentes
    private GameObject beamObject;
    private SpriteRenderer beamRenderer;
    private BoxCollider2D beamCollider;
    private GameObject coreObject;
    private SpriteRenderer coreRenderer;
    private GameObject[] beamSegments;
    private SpriteRenderer[] segmentRenderers;
    private CircleCollider2D[] segmentColliders;
    private float[] segmentSizeVariations;
    private GameObject starEmitter;              // Estrella roja giratoria en el origen del láser
    private SpriteRenderer starEmitterRenderer;

    // Cache de sprites para evitar generar texturas en cada spawn del láser
    private static Sprite cachedStarSprite;
    private static Sprite cachedBeamSprite;
    private static Sprite cachedBeamCoreSprite;
    private static Sprite cachedBeamSegmentSprite;
    
    // Se usa ObstacleMover del padre para el movimiento general,
    // pero el láser en sí es estático respecto a su posición de spawn.
    // Sin embargo, necesitamos desactivar el mover para que el láser no se mueva.
    private ObstacleMover mover;

    public ObstacleDifficultyLevel GetDifficulty()
    {
        return ObstacleDifficultyLevel.VeryHard;
    }

    private void Start()
    {
        Log($"LaserBeam: Start() called for {gameObject.name} at {transform.position}");
        
        // Desactivar el ObstacleMover — el láser no se mueve, es estático en su posición
        mover = GetComponent<ObstacleMover>();
        if (mover != null)
        {
            // Guardar la dirección original para orientar el láser
            Vector2 moveDir = mover.direction;
            
            // Desactivar movimiento
            mover.enabled = false;
            
            // Posicionar el láser en la órbita del jugador
            PositionOnOrbit(moveDir);
        }
        
        CreateBeam();
        CreateStarEmitter(); // Estrella roja giratoria en el origen (evita el cuadrado blanco)
        
        // Iniciar en estado de aviso
        currentState = LaserState.Warning;
        stateTimer = 0f;
        
        // Collider desactivado durante el aviso
        if (beamCollider != null) beamCollider.enabled = false;
        
        Log($"LaserBeam: Beam created, starting warning phase ({warningDuration}s)");
    }

    /// <summary>
    /// Crea una estrella roja giratoria en el origen del láser para sustituir el cuadrado blanco.
    /// </summary>
    private void CreateStarEmitter()
    {
        Sprite starSprite = GetOrCreateStarSprite();

        // Configurar el SpriteRenderer raíz para que nunca se vea como cuadrado.
        // Si ObstacleGlow le cambia alpha/color, seguirá siendo forma de estrella.
        SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
        if (rootRenderer != null)
        {
            rootRenderer.sprite = starSprite;
            rootRenderer.sortingOrder = 6;
            rootRenderer.sortingLayerName = "Default";

            Color c = new Color(1f, 0.25f, 0.25f, 0f);
            rootRenderer.color = c;
        }

        // Crear estrella como hijo del láser
        starEmitter = new GameObject("LaserStarEmitter");
        starEmitter.transform.SetParent(transform);
        starEmitter.transform.localPosition = Vector3.zero;
        starEmitter.transform.localRotation = Quaternion.identity;
        starEmitter.transform.localScale = Vector3.one * 0.7f;
        
        starEmitterRenderer = starEmitter.AddComponent<SpriteRenderer>();

        starEmitterRenderer.sprite = starSprite;
        starEmitterRenderer.color = new Color(1f, 0.25f, 0.25f, 1f); // Rojo intenso
        starEmitterRenderer.sortingOrder = 7; // Por encima del rayo y del núcleo
        starEmitterRenderer.sortingLayerName = "Default";
    }

    private Sprite GetOrCreateStarSprite()
    {
        if (cachedStarSprite != null) return cachedStarSprite;

        cachedStarSprite = LoadObstacleSprite("StarObstacle");
        if (cachedStarSprite == null)
        {
            cachedStarSprite = SpriteGenerator.CreateStarSprite(0.5f, new Color(1f, 0.35f, 0.35f, 1f));
        }

        return cachedStarSprite;
    }

    private void PositionOnOrbit(Vector2 moveDirection)
    {
        // Encontrar el centro de la órbita
        GameObject centerObj = GameObject.Find("Center");
        Transform center = centerObj != null ? centerObj.transform : null;
        
        PlayerOrbit playerOrbit = FindFirstObjectByType<PlayerOrbit>();
        float orbitRadius = playerOrbit != null ? playerOrbit.radius : 2f;
        
        if (center != null)
        {
            // Elegir un punto aleatorio en la órbita
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 pointOnOrbit = center.position + new Vector3(
                Mathf.Cos(angle) * orbitRadius,
                Mathf.Sin(angle) * orbitRadius,
                0f
            );
            
            // Posicionar el láser en ese punto
            transform.position = pointOnOrbit;
            
            // Orientar tangencialmente a la órbita (perpendicular al radio)
            float tangentAngle = angle + Mathf.PI / 2f; // 90° del radio
            float rotationDeg = tangentAngle * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, rotationDeg);
        }
    }

    private void Update()
    {
        stateTimer += Time.deltaTime;
        
        // Rotar la estrella emisora (solo visual)
        if (starEmitter != null)
        {
            starEmitter.transform.Rotate(0f, 0f, -180f * Time.deltaTime);
        }
        
        switch (currentState)
        {
            case LaserState.Warning:
                UpdateWarning();
                break;
            case LaserState.Activating:
                UpdateActivating();
                break;
            case LaserState.Active:
                UpdateActive();
                break;
            case LaserState.FadingOut:
                UpdateFadingOut();
                break;
            case LaserState.Done:
                // Auto-destruir
                Destroy(gameObject);
                break;
        }
    }

    private void UpdateWarning()
    {
        // Parpadeo de los segmentos de aviso
        float blinkSpeed = 8f + stateTimer * 4f; // Parpadeo cada vez más rápido
        float blink = Mathf.Abs(Mathf.Sin(stateTimer * blinkSpeed));
        
        Color c = warningColor;
        c.a = warningColor.a * blink;
        
        // Actualizar todos los segmentos
        if (segmentRenderers != null)
        {
            for (int i = 0; i < segmentRenderers.Length; i++)
            {
                if (segmentRenderers[i] != null)
                {
                    segmentRenderers[i].color = c;
                }
            }
        }
        
        if (beamRenderer != null)
        {
            beamRenderer.color = c;
        }
        
        if (stateTimer >= warningDuration)
        {
            currentState = LaserState.Activating;
            stateTimer = 0f;
            Log("[LaserBeam] Warning phase ended, activating!");
        }
    }

    private void UpdateActivating()
    {
        // Flash de activación: rápida expansión del ancho
        float t = stateTimer / activationFlashDuration;
        t = Mathf.Clamp01(t);
        
        // Expandir grosor de segmentos
        float currentWidth = Mathf.Lerp(warningWidth, beamWidth, t);
        
        // Actualizar segmentos
        if (beamSegments != null)
        {
            for (int i = 0; i < beamSegments.Length; i++)
            {
                if (beamSegments[i] != null)
                {
                    float sizeVariation = segmentSizeVariations != null && i < segmentSizeVariations.Length
                        ? segmentSizeVariations[i]
                        : 1f;
                    beamSegments[i].transform.localScale = Vector3.one * currentWidth * sizeVariation;
                    
                    if (segmentRenderers[i] != null)
                    {
                        Color flashColor = Color.Lerp(Color.white, beamColor, t);
                        flashColor.a = 1f;
                        segmentRenderers[i].color = flashColor;
                    }
                }
            }
        }
        
        if (beamObject != null)
        {
            beamObject.transform.localScale = new Vector3(beamLength, currentWidth, 1f);
        }
        
        if (beamRenderer != null)
        {
            Color flashColor = Color.Lerp(Color.white, beamColor, t);
            flashColor.a = 1f;
            beamRenderer.color = flashColor;
        }
        
        // Core visible
        if (coreRenderer != null)
        {
            coreRenderer.color = new Color(beamCoreColor.r, beamCoreColor.g, beamCoreColor.b, t);
            if (coreObject != null)
            {
                coreObject.transform.localScale = new Vector3(beamLength, currentWidth * 0.4f, 1f);
            }
        }
        
        // Activar colliders al 50% de la activación
        if (t > 0.5f)
        {
            if (beamCollider != null) beamCollider.enabled = true;
            if (segmentColliders != null)
            {
                for (int i = 0; i < segmentColliders.Length; i++)
                {
                    if (segmentColliders[i] != null)
                    {
                        segmentColliders[i].enabled = true;
                    }
                }
            }
        }
        
        // Haptic feedback
        if (t < 0.1f && GameFeedbackManager.Instance != null)
        {
            GameFeedbackManager.Instance.TriggerHaptic(GameFeedbackManager.HapticType.Medium);
        }
        
        if (stateTimer >= activationFlashDuration)
        {
            currentState = LaserState.Active;
            stateTimer = 0f;
        }
    }

    private void UpdateActive()
    {
        // Láser activo: grosor completo, pulsación sutil
        float pulse = 1f + Mathf.Sin(stateTimer * 20f) * 0.05f;
        
        // Actualizar segmentos
        if (beamSegments != null)
        {
            for (int i = 0; i < beamSegments.Length; i++)
            {
                if (beamSegments[i] != null)
                {
                    float sizeVariation = segmentSizeVariations != null && i < segmentSizeVariations.Length
                        ? segmentSizeVariations[i]
                        : 1f;
                    beamSegments[i].transform.localScale = Vector3.one * beamWidth * pulse * sizeVariation;
                    
                    if (segmentRenderers[i] != null)
                    {
                        Color c = beamColor;
                        c.a = 0.85f + Mathf.Sin(stateTimer * 15f + i * 0.2f) * 0.1f;
                        segmentRenderers[i].color = c;
                    }
                }
            }
        }
        
        if (beamObject != null)
        {
            beamObject.transform.localScale = new Vector3(beamLength, beamWidth * pulse, 1f);
        }
        
        if (beamRenderer != null)
        {
            Color c = beamColor;
            c.a = 0.85f + Mathf.Sin(stateTimer * 15f) * 0.1f;
            beamRenderer.color = c;
        }
        
        // Core pulsante
        if (coreRenderer != null)
        {
            Color core = beamCoreColor;
            core.a = 0.9f + Mathf.Sin(stateTimer * 20f) * 0.1f;
            coreRenderer.color = core;
            if (coreObject != null)
            {
                coreObject.transform.localScale = new Vector3(beamLength, beamWidth * 0.35f * pulse, 1f);
            }
        }
        
        if (stateTimer >= activeDuration)
        {
            currentState = LaserState.FadingOut;
            stateTimer = 0f;
            
            // Desactivar colliders
            if (beamCollider != null) beamCollider.enabled = false;
            if (segmentColliders != null)
            {
                for (int i = 0; i < segmentColliders.Length; i++)
                {
                    if (segmentColliders[i] != null)
                    {
                        segmentColliders[i].enabled = false;
                    }
                }
            }
        }
    }

    private void UpdateFadingOut()
    {
        float t = stateTimer / fadeOutDuration;
        t = Mathf.Clamp01(t);
        
        // Reducir grosor y alpha de segmentos
        float currentWidth = Mathf.Lerp(beamWidth, 0f, t);
        
        if (beamSegments != null)
        {
            for (int i = 0; i < beamSegments.Length; i++)
            {
                if (beamSegments[i] != null)
                {
                    float sizeVariation = segmentSizeVariations != null && i < segmentSizeVariations.Length
                        ? segmentSizeVariations[i]
                        : 1f;
                    beamSegments[i].transform.localScale = Vector3.one * currentWidth * sizeVariation;
                    
                    if (segmentRenderers[i] != null)
                    {
                        Color c = beamColor;
                        c.a = Mathf.Lerp(beamColor.a, 0f, t);
                        segmentRenderers[i].color = c;
                    }
                }
            }
        }
        
        if (beamObject != null)
        {
            beamObject.transform.localScale = new Vector3(beamLength, currentWidth, 1f);
        }
        
        if (beamRenderer != null)
        {
            Color c = beamColor;
            c.a = Mathf.Lerp(beamColor.a, 0f, t);
            beamRenderer.color = c;
        }
        
        if (coreRenderer != null)
        {
            Color core = beamCoreColor;
            core.a = Mathf.Lerp(1f, 0f, t);
            coreRenderer.color = core;
            if (coreObject != null)
            {
                coreObject.transform.localScale = new Vector3(beamLength, currentWidth * 0.35f, 1f);
            }
        }
        
        if (stateTimer >= fadeOutDuration)
        {
            currentState = LaserState.Done;
            stateTimer = 0f;
        }
    }

    private void CreateBeam()
    {
        // Crear múltiples segmentos orgánicos en lugar de un rectángulo simple
        // Esto crea una línea de energía con forma irregular y orgánica
        int segmentCount = Mathf.RoundToInt(beamLength / 0.8f); // Menos segmentos = menos pico de CPU/GC
        segmentCount = Mathf.Clamp(segmentCount, 12, 28);
        
        beamSegments = new GameObject[segmentCount];
        segmentRenderers = new SpriteRenderer[segmentCount];
        segmentColliders = new CircleCollider2D[segmentCount];
        segmentSizeVariations = new float[segmentCount];

        if (cachedBeamSegmentSprite == null)
        {
            cachedBeamSegmentSprite = CreateBeamSegmentSprite();
        }
        
        for (int i = 0; i < segmentCount; i++)
        {
            float segmentPos = (i / (float)(segmentCount - 1) - 0.5f) * beamLength;
            
            GameObject segment = new GameObject($"BeamSegment_{i}");
            segment.transform.SetParent(transform);
            segment.transform.localPosition = new Vector3(segmentPos, 0f, 0f);
            segment.transform.localRotation = Quaternion.identity;
            
            // Variación de tamaño para forma orgánica
            float sizeVariation = 0.85f + Mathf.PerlinNoise(i * 0.37f, 0.11f) * 0.3f;
            segmentSizeVariations[i] = sizeVariation;
            segment.transform.localScale = Vector3.one * warningWidth * sizeVariation;
            
            SpriteRenderer sr = segment.AddComponent<SpriteRenderer>();
            sr.sprite = cachedBeamSegmentSprite;
            sr.color = warningColor;
            sr.sortingOrder = 5;
            sr.sortingLayerName = "Default";
            segmentRenderers[i] = sr;
            
            // Collider circular para cada segmento
            CircleCollider2D collider = segment.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;
            collider.isTrigger = true;
            collider.enabled = false;
            segmentColliders[i] = collider;
            
            segment.AddComponent<ObstacleCollisionDetector>();
            beamSegments[i] = segment;
        }
        
        // Objeto principal del rayo (para el collider principal)
        beamObject = new GameObject("Beam");
        beamObject.transform.SetParent(transform);
        beamObject.transform.localPosition = Vector3.zero;
        beamObject.transform.localRotation = Quaternion.identity;
        beamObject.transform.localScale = new Vector3(beamLength, warningWidth, 1f);
        
        beamRenderer = beamObject.AddComponent<SpriteRenderer>();
        beamRenderer.sprite = GetOrCreateBeamSprite();
        beamRenderer.color = warningColor;
        beamRenderer.sortingOrder = 4; // Detrás de los segmentos
        beamRenderer.sortingLayerName = "Default";
        
        // Collider principal (largo y fino)
        beamCollider = beamObject.AddComponent<BoxCollider2D>();
        beamCollider.size = new Vector2(1f, 1f);
        beamCollider.isTrigger = true;
        beamCollider.enabled = false;
        
        // Núcleo brillante del láser (línea orgánica)
        coreObject = new GameObject("BeamCore");
        coreObject.transform.SetParent(transform);
        coreObject.transform.localPosition = Vector3.zero;
        coreObject.transform.localRotation = Quaternion.identity;
        coreObject.transform.localScale = new Vector3(beamLength, warningWidth * 0.35f, 1f);
        
        coreRenderer = coreObject.AddComponent<SpriteRenderer>();
        coreRenderer.sprite = GetOrCreateBeamCoreSprite();
        coreRenderer.color = new Color(beamCoreColor.r, beamCoreColor.g, beamCoreColor.b, 0f);
        coreRenderer.sortingOrder = 6;
        coreRenderer.sortingLayerName = "Default";
    }

    private Sprite GetOrCreateBeamSprite()
    {
        if (cachedBeamSprite == null)
        {
            cachedBeamSprite = CreateBeamSprite();
        }
        return cachedBeamSprite;
    }

    private Sprite CreateBeamSprite()
    {
        // Sprite circular/orgánico para el fondo del rayo (no rectángulo)
        int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] colors = new Color[textureSize * textureSize];
        
        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float maxRadius = textureSize / 2f - 2f;
        
        // Colores cósmicos del tema del juego
        Color coreColor = CosmicTheme.EtherealLila;
        Color edgeColor = Color.Lerp(CosmicTheme.ConstellationBlue, CosmicTheme.EtherealLila, 0.5f);
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                
                if (dist <= maxRadius)
                {
                    float normalizedDist = dist / maxRadius;
                    float alpha = Mathf.Pow(1f - normalizedDist, 1.5f);
                    
                    Color pixelColor = Color.Lerp(coreColor, edgeColor, normalizedDist);
                    
                    // Destellos dorados sutiles
                    float sparkle = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                    if (sparkle > 0.85f)
                    {
                        pixelColor = Color.Lerp(pixelColor, CosmicTheme.SoftGold, (sparkle - 0.85f) / 0.15f * 0.3f);
                    }
                    
                    colors[y * textureSize + x] = new Color(pixelColor.r, pixelColor.g, pixelColor.b, alpha * 0.6f);
                }
                else
                {
                    colors[y * textureSize + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
    }
    
    private Sprite CreateBeamSegmentSprite()
    {
        // Sprite circular/orgánico para cada segmento del rayo
        // Usar ConstellationFragmentGenerator para mantener consistencia
        Color segmentColor = Color.Lerp(CosmicTheme.CelestialBlue, CosmicTheme.EtherealLila, 0.5f);
        return ConstellationFragmentGenerator.CreateFragmentSprite(0.4f, segmentColor, false);
    }

    private Sprite GetOrCreateBeamCoreSprite()
    {
        if (cachedBeamCoreSprite == null)
        {
            cachedBeamCoreSprite = CreateBeamCoreSprite();
        }
        return cachedBeamCoreSprite;
    }

    private Sprite CreateBeamCoreSprite()
    {
        // Núcleo brillante circular (no rectángulo)
        int textureSize = 48;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] colors = new Color[textureSize * textureSize];
        
        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float maxRadius = textureSize / 2f - 2f;
        
        // Colores del tema cósmico
        Color coreBright = CosmicTheme.EtherealLila;
        Color coreGlow = CosmicTheme.CelestialBlue;
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= maxRadius)
                {
                    float normalizedDist = dist / maxRadius;
                    float alpha = Mathf.Pow(1f - normalizedDist, 3f);
                    
                    Color pixelColor = Color.Lerp(coreBright, coreGlow, normalizedDist);
                    
                    // Efecto de pulsación/energía
                    float angle = Mathf.Atan2(y - center.y, x - center.x);
                    float energyWave = Mathf.Sin(angle * 4f + normalizedDist * 8f) * 0.15f;
                    pixelColor = Color.Lerp(pixelColor, CosmicTheme.SoftGold, energyWave * 0.3f);
                    
                    colors[y * textureSize + x] = new Color(pixelColor.r, pixelColor.g, pixelColor.b, alpha);
                }
                else
                {
                    colors[y * textureSize + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
    }
}
