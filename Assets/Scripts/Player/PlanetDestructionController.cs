using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static LogHelper;

/// <summary>
/// Sistema universal de animación de destrucción para planetas.
/// Funciona con cualquier sprite, sin necesidad de animaciones dibujadas ni texturas adicionales.
/// Todo es procedural usando Unity.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlanetDestructionController : MonoBehaviour
{
    [Header("Destruction Settings")]
    [Tooltip("Número de fragmentos a generar (5-10 recomendado)")]
    [Range(5, 15)]
    public int fragmentCount = 8;
    
    [Tooltip("Velocidad inicial de los fragmentos")]
    [Range(2f, 10f)]
    public float fragmentSpeed = 5f;
    
    [Tooltip("Gravedad falsa para los fragmentos (0 = sin gravedad)")]
    [Range(0f, 5f)]
    public float fragmentGravity = 0f; // Sin gravedad por defecto para que vuelen en todas direcciones
    
    [Header("Particle Settings")]
    [Tooltip("Número de partículas en la explosión (más partículas = más fragmentos naranjas de fuego)")]
    [Range(20, 150)]
    public int particleCount = 80;
    
    [Tooltip("Velocidad de las partículas")]
    [Range(3f, 12f)]
    public float particleSpeed = 7f;
    
    [Header("Timing")]
    [Tooltip("Duración del flash blanco (más corto = más inmediato)")]
    public float flashDuration = 0.02f;
    
    [Tooltip("Duración del shake")]
    public float shakeDuration = 0.02f;
    
    [Tooltip("Amplitud del shake")]
    public float shakeAmplitude = 0.1f;
    
    [Tooltip("Duración del fade-out del sprite original")]
    public float fadeOutDuration = 0.15f;
    
    [Tooltip("Tiempo antes de destruir el GameObject")]
    public float destroyDelay = 1f;
    
    private SpriteRenderer spriteRenderer;
    private PlanetIdleAnimator idleAnimator;
    private PlanetSurface planetSurface;
    private PlayerOrbit playerOrbit;
    private Collider2D planetCollider;
    private Color originalColor;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private bool isDestroying = false;
    
    // Pool de fragmentos para optimización
    private static List<GameObject> fragmentPool = new List<GameObject>();
    private static int poolIndex = 0;
    
    // Estructura para datos de fragmentos
    private struct FragmentData
    {
        public int x;
        public int y;
        public int width;
        public int height;
    }
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        idleAnimator = GetComponent<PlanetIdleAnimator>();
        planetSurface = GetComponent<PlanetSurface>();
        playerOrbit = GetComponent<PlayerOrbit>();
        planetCollider = GetComponent<Collider2D>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale; // Guardar escala original
    }
    
    /// <summary>
    /// LateUpdate asegura que la posición se fije después de todos los Updates
    /// Esto es crítico para evitar que PlayerOrbit mueva el planeta después del choque
    /// </summary>
    private void LateUpdate()
    {
        if (isDestroying && originalPosition != Vector3.zero)
        {
            // Forzar posición fija en cada frame durante la destrucción
            // Esto asegura que ningún otro sistema pueda mover el planeta
            transform.position = originalPosition;
        }
    }
    
    /// <summary>
    /// FixedUpdate también fija la posición para asegurar que no se mueva en física
    /// </summary>
    private void FixedUpdate()
    {
        if (isDestroying && originalPosition != Vector3.zero)
        {
            // Forzar posición fija también en FixedUpdate
            transform.position = originalPosition;
        }
    }
    
    /// <summary>
    /// Método público principal para iniciar la destrucción del planeta
    /// </summary>
    /// <param name="collisionPoint">Punto exacto de colisión (opcional, usa transform.position si es null)</param>
    public void DestroyPlanet(Vector3? collisionPoint = null)
    {
        if (isDestroying) return; // Evitar llamadas dobles
        
        isDestroying = true;
        
        // CRÍTICO: Capturar la posición ACTUAL del planeta ANTES de hacer cualquier cosa
        // Esto debe ser lo PRIMERO que hacemos para asegurar que tenemos la posición exacta
        Vector3 exactPosition = transform.position;
        
        // CRÍTICO: Detener PlayerOrbit INMEDIATAMENTE para evitar que mueva el planeta
        if (playerOrbit != null)
        {
            playerOrbit.enabled = false;
        }
        
        // CRÍTICO: Si se pasó un punto de colisión, verificar si es más preciso
        // Pero siempre usar la posición actual del transform como base
        Vector3 collisionPosition;
        if (collisionPoint.HasValue)
        {
            // Usar el punto pasado, pero verificar que sea razonable
            // Si la diferencia es muy grande, usar la posición actual
            float distance = Vector3.Distance(collisionPoint.Value, exactPosition);
            if (distance < 0.5f) // Si está cerca, usar el punto pasado
            {
                collisionPosition = collisionPoint.Value;
            }
            else
            {
                // Si está lejos, usar la posición actual (más confiable)
                collisionPosition = exactPosition;
            }
        }
        else
        {
            // Si no se pasó punto, usar la posición actual del planeta
            collisionPosition = exactPosition;
        }
        
        // Guardar la posición exacta
        originalPosition = collisionPosition;
        
        // Fijar posición INMEDIATAMENTE al punto exacto
        transform.position = collisionPosition;
        
        // CRÍTICO: Ejecutar operaciones inmediatas pero seguras
        originalScale = transform.localScale;
        StopAllAnimations();
        
        // Desactivar collider inmediatamente
        if (planetCollider != null)
        {
            planetCollider.enabled = false;
        }
        
        // Ocultar el sprite original INMEDIATAMENTE
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // CRÍTICO: Crear fragmentos y partículas INMEDIATAMENTE aquí (no en la coroutine)
        // Esto asegura que la explosión sea instantánea sin ningún delay
        try
        {
            CreateFragments();
            CreateExplosionParticles();
        }
        catch (System.Exception e)
        {
            LogError($"Error al crear fragmentos/partículas: {e.Message}");
        }
        
        // Mantener posición fija durante toda la secuencia
        StartCoroutine(KeepPositionFixed(collisionPosition));
        
        // Flash blanco (muy rápido, casi imperceptible) - solo en fragmentos
        StartCoroutine(FlashWhite());
        
        // Shake (muy corto) - usar posición fija como base
        StartCoroutine(ShakeAnimation(collisionPosition));
        
        // Iniciar la coroutine solo para esperar y destruir (sin crear efectos aquí)
        StartCoroutine(DestructionSequence());
    }
    
    /// <summary>
    /// Secuencia completa de destrucción
    /// Las operaciones críticas ya se ejecutaron en DestroyPlanet() para que sean inmediatas
    /// </summary>
    private IEnumerator DestructionSequence()
    {
        // La posición ya se guardó y fijó en DestroyPlanet()
        // Los fragmentos y partículas ya se crearon en DestroyPlanet() para ser instantáneos
        // Esta coroutine solo espera y destruye el GameObject
        
        // Esperar a que termine la animación antes de destruir
        yield return new WaitForSeconds(destroyDelay);
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Flash blanco interpola hacia white y vuelve
    /// </summary>
    private IEnumerator FlashWhite()
    {
        if (spriteRenderer == null) yield break;
        
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;
        
        // Ir a blanco
        while (elapsed < flashDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (flashDuration / 2f);
            spriteRenderer.color = Color.Lerp(startColor, Color.white, t);
            yield return null;
        }
        
        // Volver al color original
        elapsed = 0f;
        while (elapsed < flashDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (flashDuration / 2f);
            spriteRenderer.color = Color.Lerp(Color.white, startColor, t);
            yield return null;
        }
        
        spriteRenderer.color = startColor;
    }
    
    /// <summary>
    /// Animación de shake
    /// </summary>
    private IEnumerator ShakeAnimation(Vector3 basePosition)
    {
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Random.Range(-shakeAmplitude, shakeAmplitude);
            float offsetY = Random.Range(-shakeAmplitude, shakeAmplitude);
            transform.position = basePosition + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }
        
        transform.position = basePosition;
    }
    
    /// <summary>
    /// Mantiene la posición fija durante la destrucción
    /// Se ejecuta en LateUpdate para asegurar que se ejecute después de todos los Updates
    /// </summary>
    private IEnumerator KeepPositionFixed(Vector3 fixedPosition)
    {
        while (isDestroying)
        {
            // Forzar posición en cada frame (especialmente en LateUpdate)
            // Esto asegura que ningún otro sistema pueda mover el planeta
            transform.position = fixedPosition;
            yield return new WaitForEndOfFrame(); // Esperar al final del frame
        }
    }
    
    /// <summary>
    /// Detiene todas las animaciones del planeta inmediatamente
    /// </summary>
    private void StopAllAnimations()
    {
        // Detener PlanetIdleAnimator
        if (idleAnimator != null)
        {
            idleAnimator.enabled = false;
        }
        
        // Detener PlanetSurface (rotación)
        if (planetSurface != null)
        {
            planetSurface.enabled = false;
            // Resetear rotación a la original
            transform.rotation = originalRotation;
        }
        
        // Detener PlayerOrbit (movimiento orbital)
        if (playerOrbit != null)
        {
            playerOrbit.enabled = false;
            // Asegurar que el componente no se ejecute más
            MonoBehaviour orbitMono = playerOrbit as MonoBehaviour;
            if (orbitMono != null)
            {
                orbitMono.enabled = false;
            }
        }
        
        // CRÍTICO: Mantener la escala original del planeta
        // Esto evita que el planeta se haga gigante por el breathing effect
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// Crea el sistema de partículas de explosión con efecto de fuego
    /// </summary>
    private void CreateExplosionParticles()
    {
        // Obtener color promedio del sprite
        Color averageColor = GetAverageColorFromSprite();
        
        // Crear colores de fuego (naranja/rojo/amarillo) mezclados con el color del planeta
        Color fireOrange = new Color(1f, 0.5f, 0.1f);
        Color fireRed = new Color(1f, 0.2f, 0f);
        Color fireYellow = new Color(1f, 0.8f, 0.2f);
        
        // Mezclar colores de fuego con el color del planeta
        Color fireColor1 = Color.Lerp(fireOrange, averageColor, 0.3f);
        Color fireColor2 = Color.Lerp(fireRed, averageColor, 0.2f);
        Color fireColor3 = Color.Lerp(fireYellow, averageColor, 0.4f);
        
        // Crear GameObject para las partículas en la posición exacta del choque
        GameObject particleSystemObj = new GameObject("ExplosionParticles");
        particleSystemObj.transform.position = originalPosition; // Usar posición guardada
        
        // Agregar ParticleSystem
        ParticleSystem ps = particleSystemObj.AddComponent<ParticleSystem>();
        
        // Aplicar reducción de animaciones si está habilitado
        AccessibilityHelper.ApplyAccessibilityToParticle(ps);
        
        ParticleSystem.MainModule main = ps.main;
        ParticleSystem.EmissionModule emission = ps.emission;
        ParticleSystem.ShapeModule shape = ps.shape;
        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        ParticleSystem.NoiseModule noise = ps.noise;
        
        // Configurar main module
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(particleSpeed * 0.6f, particleSpeed * 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(fireColor1, fireColor3);
        main.maxParticles = particleCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.loop = false;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.startRotation3D = true;
        
        // Configurar emission (burst inicial)
        emission.enabled = true;
        // Asegurar que hay al menos un burst antes de configurarlo
        if (emission.burstCount == 0)
        {
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, particleCount) });
        }
        else
        {
            emission.SetBurst(0, new ParticleSystem.Burst(0f, particleCount));
        }
        
        // Configurar shape (círculo pequeño)
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        
        // Configurar velocity (radial con algo de caos)
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        // Usar solo radial para evitar conflictos de modo
        velocity.radial = new ParticleSystem.MinMaxCurve(particleSpeed * 0.9f, particleSpeed * 1.4f);
        // Desactivar X e Y para usar solo radial
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);
        
        // Configurar noise para movimiento de fuego más orgánico
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        noise.frequency = 0.5f;
        noise.scrollSpeed = 2f;
        
        // Configurar size over lifetime (crece un poco y luego se desvanece)
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f); // Empieza pequeño
        sizeCurve.AddKey(0.2f, 1f); // Crece rápido
        sizeCurve.AddKey(0.6f, 0.8f); // Se mantiene
        sizeCurve.AddKey(1f, 0f); // Se desvanece
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Configurar color over lifetime (transición de fuego a humo)
        colorOverLifetime.enabled = true;
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(fireColor3, 0f),      // Amarillo brillante al inicio
                new GradientColorKey(fireColor1, 0.3f),    // Naranja
                new GradientColorKey(fireColor2, 0.6f),    // Rojo
                new GradientColorKey(Color.Lerp(fireColor2, Color.gray, 0.5f), 0.9f), // Humo
                new GradientColorKey(Color.clear, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.4f),
                new GradientAlphaKey(0.8f, 0.7f),
                new GradientAlphaKey(0.3f, 0.9f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = colorGradient;
        
        // Renderer module para soft particles
        ParticleSystemRenderer renderer = particleSystemObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = spriteRenderer.sortingOrder + 5;
        
        // Destruir el sistema de partículas después de que termine
        Destroy(particleSystemObj, 2f);
    }
    
    /// <summary>
    /// Genera fragmentos reales del sprite del planeta usando RenderTexture
    /// </summary>
    private void CreateFragments()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            LogWarning("PlanetDestructionController: No sprite available for fragments");
            return;
        }
        
        Sprite originalSprite = spriteRenderer.sprite;
        
        // Obtener el rect del sprite y el pivot (se usará más adelante)
        Rect spriteRect = originalSprite.rect;
        int spriteWidth = (int)spriteRect.width;
        int spriteHeight = (int)spriteRect.height;
        Vector2 spritePivot = originalSprite.pivot; // Declarar aquí para usar en todo el método
        Vector2 spriteSize = originalSprite.rect.size; // Declarar aquí para usar en todo el método
        
        // Crear RenderTexture para copiar el sprite a una textura legible
        RenderTexture renderTexture = RenderTexture.GetTemporary(spriteWidth, spriteHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        
        // Crear cámara temporal para renderizar el sprite
        GameObject tempCameraObj = new GameObject("TempCamera");
        Camera tempCamera = tempCameraObj.AddComponent<Camera>();
        tempCamera.orthographic = true;
        // Ajustar tamaño de cámara para que el sprite quepa exactamente
        float spriteWorldHeight = spriteHeight / originalSprite.pixelsPerUnit;
        tempCamera.orthographicSize = spriteWorldHeight * 0.5f;
        tempCamera.clearFlags = CameraClearFlags.SolidColor;
        tempCamera.backgroundColor = Color.clear;
        tempCamera.cullingMask = 1 << 0; // Renderizar solo layer Default
        tempCamera.targetTexture = renderTexture;
        tempCamera.transform.position = new Vector3(0, 0, -10);
        tempCamera.enabled = false; // Desactivar para evitar interferencias
        
        // Crear GameObject temporal para el sprite
        GameObject tempSpriteObj = new GameObject("TempSprite");
        tempSpriteObj.transform.position = Vector3.zero;
        tempSpriteObj.layer = 0; // Layer Default
        SpriteRenderer tempRenderer = tempSpriteObj.AddComponent<SpriteRenderer>();
        tempRenderer.sprite = originalSprite;
        tempRenderer.color = spriteRenderer.color;
        tempRenderer.sortingLayerName = "Default";
        tempRenderer.sortingOrder = 1000;
        
        // Asegurar que el sprite esté centrado en la cámara
        // El pivot del sprite puede estar en (0.5, 0.5), así que ajustamos la posición
        // (spritePivot y spriteSize ya están declarados arriba)
        Vector2 pivotOffset = (spritePivot - spriteSize * 0.5f) / originalSprite.pixelsPerUnit;
        tempSpriteObj.transform.position = new Vector3(-pivotOffset.x, -pivotOffset.y, 0);
        
        // Renderizar manualmente
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear); // Limpiar el RenderTexture antes de renderizar
        tempCamera.Render();
        
        // Leer píxeles del RenderTexture
        Texture2D readableTexture = new Texture2D(spriteWidth, spriteHeight, TextureFormat.RGBA32, false);
        readableTexture.ReadPixels(new Rect(0, 0, spriteWidth, spriteHeight), 0, 0);
        readableTexture.Apply();
        
        // Verificar que la textura tiene contenido válido
        Color[] allPixels = readableTexture.GetPixels();
        int visiblePixelCount = 0;
        foreach (Color pixel in allPixels)
        {
            if (pixel.a > 0.1f)
            {
                visiblePixelCount++;
            }
        }
        
        if (visiblePixelCount == 0)
        {
            LogWarning("PlanetDestructionController: La textura legible no tiene píxeles visibles. El sprite puede no haberse renderizado correctamente.");
        }
        else
        {
            Log($"PlanetDestructionController: Textura legible creada con {visiblePixelCount} píxeles visibles de {allPixels.Length} totales.");
        }
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        
        // Limpiar objetos temporales
        Destroy(tempCameraObj);
        Destroy(tempSpriteObj);
        
        // Verificar que la textura se creó correctamente
        if (readableTexture == null || readableTexture.width == 0 || readableTexture.height == 0)
        {
            LogError("PlanetDestructionController: No se pudo crear la textura legible para fragmentos");
            if (readableTexture != null) Destroy(readableTexture);
            return;
        }
        
        // Verificar que la textura tiene contenido (no está vacía)
        Color[] testPixels = readableTexture.GetPixels(0, 0, Mathf.Min(10, spriteWidth), Mathf.Min(10, spriteHeight));
        bool hasContent = false;
        foreach (Color pixel in testPixels)
        {
            if (pixel.a > 0.1f)
            {
                hasContent = true;
                break;
            }
        }
        
        if (!hasContent)
        {
            LogWarning("PlanetDestructionController: La textura legible está vacía. Los fragmentos pueden no verse correctamente.");
        }
        
        // Crear fragmentos con tamaños y posiciones variadas (más orgánico)
        List<FragmentData> fragmentDataList = new List<FragmentData>();
        
        // Generar posiciones y tamaños variados
        for (int i = 0; i < fragmentCount; i++)
        {
            FragmentData fragData = new FragmentData();
            
            // Tamaño variado (más grande en el centro, más pequeño en los bordes)
            float distanceFromCenter = Random.Range(0f, 0.7f); // 0 = centro, 1 = borde
            float sizeMultiplier = 1f - (distanceFromCenter * 0.4f); // Más grande en el centro
            sizeMultiplier = Mathf.Clamp(sizeMultiplier, 0.4f, 1.2f);
            
            fragData.width = Mathf.RoundToInt((spriteWidth / Mathf.Sqrt(fragmentCount)) * sizeMultiplier);
            fragData.height = Mathf.RoundToInt((spriteHeight / Mathf.Sqrt(fragmentCount)) * sizeMultiplier);
            
            // Asegurar tamaño mínimo y máximo
            fragData.width = Mathf.Clamp(fragData.width, 12, spriteWidth / 2);
            fragData.height = Mathf.Clamp(fragData.height, 12, spriteHeight / 2);
            
            // Posición variada (más concentrada en el centro)
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = distanceFromCenter * Mathf.Min(spriteWidth, spriteHeight) * 0.35f;
            
            fragData.x = Mathf.RoundToInt(spriteWidth / 2f + Mathf.Cos(angle) * radius - fragData.width / 2f);
            fragData.y = Mathf.RoundToInt(spriteHeight / 2f + Mathf.Sin(angle) * radius - fragData.height / 2f);
            
            // Asegurar que esté dentro de los límites
            fragData.x = Mathf.Clamp(fragData.x, 0, spriteWidth - fragData.width);
            fragData.y = Mathf.Clamp(fragData.y, 0, spriteHeight - fragData.height);
            
            fragmentDataList.Add(fragData);
        }
        
        // Crear fragmentos
        int fragmentsCreated = 0;
        foreach (FragmentData fragData in fragmentDataList)
        {
            // Verificar que el fragmento esté dentro de los límites
            if (fragData.x < 0 || fragData.y < 0 || 
                fragData.x + fragData.width > spriteWidth || 
                fragData.y + fragData.height > spriteHeight)
            {
                continue; // Saltar este fragmento si está fuera de los límites
            }
            
            // Crear textura del fragmento (parte real del sprite)
            Texture2D fragmentTexture = new Texture2D(fragData.width, fragData.height, TextureFormat.RGBA32, false);
            
            // Obtener píxeles de la región específica del sprite
            Color[] pixels = readableTexture.GetPixels(fragData.x, fragData.y, fragData.width, fragData.height);
            
            // Verificar que hay píxeles con contenido
            bool hasVisiblePixels = false;
            foreach (Color pixel in pixels)
            {
                if (pixel.a > 0.1f)
                {
                    hasVisiblePixels = true;
                    break;
                }
            }
            
            if (!hasVisiblePixels)
            {
                // Si no hay píxeles visibles, saltar este fragmento
                continue;
            }
            
            fragmentTexture.SetPixels(pixels);
            fragmentTexture.Apply();
            fragmentTexture.filterMode = FilterMode.Bilinear;
                
                // Crear sprite del fragmento
                Sprite fragmentSprite = Sprite.Create(
                    fragmentTexture,
                    new Rect(0, 0, fragData.width, fragData.height),
                    new Vector2(0.5f, 0.5f),
                    originalSprite.pixelsPerUnit
                );
                
                // Crear GameObject del fragmento
                GameObject fragment = GetOrCreateFragment();
                
                // Calcular posición relativa del fragmento (basado en su posición en el sprite)
                // El pivot del sprite puede no estar en el centro, así que usamos el pivot real
                // (spritePivot y spriteSize ya están declarados arriba)
                
                // Calcular el offset desde el pivot del sprite (no desde el centro)
                // fragData.x e y son coordenadas en píxeles desde la esquina superior izquierda del sprite
                // Necesitamos convertir esto a un offset desde el pivot
                float offsetX = (fragData.x + fragData.width / 2f - spritePivot.x) / originalSprite.pixelsPerUnit;
                float offsetY = (fragData.y + fragData.height / 2f - spritePivot.y) / originalSprite.pixelsPerUnit;
                
                // Aplicar la escala del planeta al offset
                Vector3 fragmentOffset = new Vector3(
                    offsetX * originalScale.x,
                    offsetY * originalScale.y,
                    0f
                );
                
                // Los fragmentos deben aparecer exactamente donde estaba el sprite original
                // originalPosition es donde está el pivot del sprite original
                fragment.transform.position = originalPosition + fragmentOffset;
                
                // Asegurar que los fragmentos tengan la misma escala que el planeta original
                fragment.transform.localScale = originalScale;
                
                // Configurar sprite
                SpriteRenderer fragRenderer = fragment.GetComponent<SpriteRenderer>();
                fragRenderer.sprite = fragmentSprite;
                // Usar el color original del sprite (sin modificaciones)
                fragRenderer.color = spriteRenderer.color;
                fragRenderer.sortingOrder = spriteRenderer.sortingOrder + 3;
                
                // CRÍTICO: Los fragmentos deben aparecer exactamente donde estaba el sprite original
                // Ya están posicionados correctamente usando originalPosition + fragmentOffset
                
                // Configurar física
                Rigidbody2D rb = fragment.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = fragment.AddComponent<Rigidbody2D>();
                }
                
                // CRÍTICO: Configurar física ANTES de calcular velocidad
                rb.gravityScale = 0f; // Sin gravedad
                rb.linearDamping = 0f; // Sin resistencia al aire
                rb.angularDamping = 0f; // Sin resistencia angular
                rb.bodyType = RigidbodyType2D.Dynamic; // Debe ser dynamic para que la velocidad funcione
                
                // Calcular dirección radial desde el centro del planeta
                // Usar el offset del fragmento para calcular la dirección
                Vector2 direction = new Vector2(fragmentOffset.x, fragmentOffset.y);
                
                // Si el fragmento está muy cerca del centro, usar dirección aleatoria
                if (direction.magnitude < 0.01f || fragmentOffset.magnitude < 0.01f)
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                }
                else
                {
                    // Normalizar la dirección
                    direction = direction.normalized;
                }
                
                // Velocidad en dirección radial
                float speed = fragmentSpeed * Random.Range(0.8f, 1.4f);
                rb.linearVelocity = direction * speed;
                
                // Agregar rotación aleatoria
                rb.angularVelocity = Random.Range(-360f, 360f);
                
                // Escala inicial (mantener tamaño relativo al fragmento, sin modificar)
                // Los fragmentos ya tienen el tamaño correcto basado en su textura
                Vector3 initialScale = Vector3.one; // Escala base 1:1
                fragment.transform.localScale = initialScale;
                
                // Activar fragmento
                fragment.SetActive(true);
                
                // Animar fragmento: hacerlo más pequeño hasta desaparecer
                StartCoroutine(AnimateFragmentScale(fragment, initialScale, Random.Range(0.6f, 1f)));
                
                fragmentsCreated++;
        }
        
        Log($"PlanetDestructionController: Creados {fragmentsCreated} fragmentos del sprite");
        
        // Limpiar textura legible
        Destroy(readableTexture);
    }
    
    /// <summary>
    /// Obtiene o crea un fragmento del pool
    /// </summary>
    private GameObject GetOrCreateFragment()
    {
        // Buscar fragmento inactivo en el pool
        for (int i = 0; i < fragmentPool.Count; i++)
        {
            int index = (poolIndex + i) % fragmentPool.Count;
            if (fragmentPool[index] != null && !fragmentPool[index].activeSelf)
            {
                poolIndex = (index + 1) % fragmentPool.Count;
                GameObject fragment = fragmentPool[index];
                
                // Limpiar sprite anterior si existe
                SpriteRenderer fragRenderer = fragment.GetComponent<SpriteRenderer>();
                if (fragRenderer != null && fragRenderer.sprite != null)
                {
                    // Destruir textura anterior si existe
                    if (fragRenderer.sprite.texture != null)
                    {
                        Destroy(fragRenderer.sprite.texture);
                    }
                    Destroy(fragRenderer.sprite);
                    fragRenderer.sprite = null;
                }
                
                // Limpiar física
                Rigidbody2D rb = fragment.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                
                return fragment;
            }
        }
        
        // Crear nuevo fragmento si no hay disponibles
        GameObject newFragment = new GameObject("PlanetFragment");
        newFragment.AddComponent<SpriteRenderer>();
        newFragment.SetActive(false);
        fragmentPool.Add(newFragment);
        return newFragment;
    }
    
    /// <summary>
    /// Anima un fragmento: lo hace más pequeño y lo desvanece
    /// </summary>
    private IEnumerator AnimateFragmentScale(GameObject fragment, Vector3 initialScale, float lifetime)
    {
        if (fragment == null) yield break;
        
        SpriteRenderer fragRenderer = fragment.GetComponent<SpriteRenderer>();
        if (fragRenderer == null) yield break;
        
        Color startColor = fragRenderer.color;
        float elapsed = 0f;
        
        while (elapsed < lifetime && fragment != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;
            
            // Reducir escala gradualmente (de 1.0 a 0.0)
            float scaleFactor = 1f - t;
            fragment.transform.localScale = initialScale * scaleFactor;
            
            // Fade-out de alpha
            float alpha = 1f - t;
            fragRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            yield return null;
        }
        
        // Desactivar fragmento
        if (fragment != null)
        {
            fragment.SetActive(false);
        }
    }
    
    /// <summary>
    /// Fade-out del planeta
    /// </summary>
    private IEnumerator FadeOutPlanet()
    {
        if (spriteRenderer == null) yield break;
        
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            float alpha = 1f - t;
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        
        spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
    }
    
    /// <summary>
    /// Obtiene el color promedio del sprite del planeta
    /// </summary>
    private Color GetAverageColorFromSprite()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return originalColor;
        }
        
        // Intentar obtener el color del sprite renderer primero (más rápido)
        Color rendererColor = spriteRenderer.color;
        if (rendererColor != Color.white && rendererColor.a > 0.1f)
        {
            // Si el color no es blanco, probablemente es el color del planeta
            return rendererColor;
        }
        
        // Si la textura no es legible, usar RenderTexture para obtener el color
        Sprite sprite = spriteRenderer.sprite;
        Rect spriteRect = sprite.rect;
        int spriteWidth = (int)spriteRect.width;
        int spriteHeight = (int)spriteRect.height;
        
        // Crear RenderTexture pequeño solo para muestrear color
        RenderTexture sampleRT = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        
        // Crear cámara temporal
        GameObject tempCamObj = new GameObject("TempColorCamera");
        Camera tempCam = tempCamObj.AddComponent<Camera>();
        tempCam.orthographic = true;
        tempCam.orthographicSize = spriteHeight / (2f * sprite.pixelsPerUnit);
        tempCam.clearFlags = CameraClearFlags.SolidColor;
        tempCam.backgroundColor = Color.clear;
        tempCam.cullingMask = 1 << 0;
        tempCam.targetTexture = sampleRT;
        tempCam.transform.position = new Vector3(0, 0, -10);
        tempCam.enabled = false;
        
        // Crear sprite temporal
        GameObject tempSpriteObj = new GameObject("TempColorSprite");
        tempSpriteObj.transform.position = Vector3.zero;
        tempSpriteObj.layer = 0;
        SpriteRenderer tempRenderer = tempSpriteObj.AddComponent<SpriteRenderer>();
        tempRenderer.sprite = sprite;
        tempRenderer.color = spriteRenderer.color;
        tempRenderer.sortingOrder = 1000;
        
        // Renderizar y leer
        RenderTexture.active = sampleRT;
        tempCam.Render();
        
        Texture2D sampleTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        sampleTex.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
        sampleTex.Apply();
        
        // Muestrear algunos píxeles del centro
        float r = 0f, g = 0f, b = 0f;
        int count = 0;
        for (int y = 20; y < 44; y += 4)
        {
            for (int x = 20; x < 44; x += 4)
            {
                Color pixel = sampleTex.GetPixel(x, y);
                if (pixel.a > 0.1f)
                {
                    r += pixel.r;
                    g += pixel.g;
                    b += pixel.b;
                    count++;
                }
            }
        }
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(sampleRT);
        Destroy(tempCamObj);
        Destroy(tempSpriteObj);
        Destroy(sampleTex);
        
        if (count > 0)
        {
            return new Color(r / count, g / count, b / count, 1f);
        }
        
        // Fallback al color original
        return originalColor;
    }
    
    /// <summary>
    /// Método para colisiones (opcional, puede ser llamado desde otros scripts)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo destruir si no está ya destruyéndose
        if (!isDestroying)
        {
            // Aquí puedes agregar lógica para detectar qué tipo de colisión debe destruir el planeta
            // Por ejemplo, si colisiona con un obstáculo específico
        }
    }
    
    /// <summary>
    /// Limpia el pool de fragmentos (llamar cuando se cambia de escena)
    /// </summary>
    public static void ClearFragmentPool()
    {
        foreach (GameObject fragment in fragmentPool)
        {
            if (fragment != null)
            {
                Destroy(fragment);
            }
        }
        fragmentPool.Clear();
        poolIndex = 0;
    }
}

