using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sistema universal de destrucción para obstáculos.
/// Replica el sistema del planeta pero más rápido y ligero.
/// Funciona con cualquier sprite sin texturas adicionales.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ObstacleDestructionController : MonoBehaviour
{
    [Header("Destruction Settings")]
    [Tooltip("Número de fragmentos a generar (3-6 recomendado)")]
    [Range(3, 6)]
    public int fragmentCount = 4;
    
    [Tooltip("Velocidad inicial de los fragmentos")]
    [Range(2f, 8f)]
    public float fragmentSpeed = 5f;
    
    [Header("Particle Settings")]
    [Tooltip("Número de partículas en la explosión (más partículas = más fragmentos naranjas de fuego)")]
    [Range(15, 100)]
    public int particleCount = 50;
    
    [Tooltip("Velocidad de las partículas")]
    [Range(3f, 10f)]
    public float particleSpeed = 7f;
    
    [Header("Timing")]
    [Tooltip("Duración del flash blanco (más corto = más inmediato)")]
    public float flashDuration = 0.02f;
    
    [Tooltip("Duración del shake")]
    public float shakeDuration = 0.02f;
    
    [Tooltip("Amplitud del shake")]
    public float shakeAmplitude = 0.05f;
    
    [Tooltip("Tiempo antes de destruir el GameObject")]
    public float destroyDelay = 0.5f;
    
    private SpriteRenderer spriteRenderer;
    private Collider2D obstacleCollider;
    private Color originalColor;
    private Vector3 originalPosition;
    private Vector3 originalScale;
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
        obstacleCollider = GetComponent<Collider2D>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        originalPosition = transform.position;
        originalScale = transform.localScale;
    }
    
    /// <summary>
    /// Método público principal para iniciar la destrucción del obstáculo
    /// </summary>
    /// <param name="collisionPosition">Posición exacta de la colisión (opcional, usa transform.position si es null)</param>
    public void DestroyObstacle(Vector3? collisionPosition = null)
    {
        if (isDestroying) return; // Evitar llamadas dobles
        
        isDestroying = true;
        
        // CRÍTICO: Capturar la posición ACTUAL del obstáculo ANTES de hacer cualquier cosa
        // Esto debe ser lo PRIMERO que hacemos para asegurar que tenemos la posición exacta
        Vector3 exactPosition = transform.position;
        
        // CRÍTICO: Detener ObstacleMover INMEDIATAMENTE para evitar que mueva el obstáculo
        // Esto debe hacerse DESPUÉS de capturar la posición, igual que el planeta
        ObstacleMover mover = GetComponent<ObstacleMover>();
        if (mover != null)
        {
            mover.enabled = false;
        }
        
        // CRÍTICO: Detener Rigidbody2D si existe para evitar movimiento por física
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = true; // Hacerlo kinematic para que no se mueva
        }
        
        // CRÍTICO: Si se pasó un punto de colisión, SIEMPRE usarlo (es la posición del padre/obstáculo completo)
        // No verificar distancia porque puede ser un hijo con posición diferente
        Vector3 finalPosition;
        if (collisionPosition.HasValue)
        {
            // SIEMPRE usar la posición pasada - es la posición correcta del obstáculo
            finalPosition = collisionPosition.Value;
            Debug.Log($"ObstacleDestructionController: Usando posición pasada: {finalPosition} (exactPosition del hijo era: {exactPosition})");
        }
        else
        {
            // Si no se pasó punto, usar la posición actual del obstáculo
            finalPosition = exactPosition;
            Debug.Log($"ObstacleDestructionController: No se pasó posición, usando exactPosition: {finalPosition}");
        }
        
        // Guardar la posición exacta
        originalPosition = finalPosition;
        
        // Fijar posición INMEDIATAMENTE al punto exacto (igual que el planeta)
        transform.position = finalPosition;
        
        // Fijar posición INMEDIATAMENTE al punto exacto
        transform.position = finalPosition;
        
        // CRÍTICO: Ejecutar operaciones inmediatas pero seguras
        originalScale = transform.localScale;
        
        // Desactivar collider inmediatamente
        if (obstacleCollider != null)
        {
            obstacleCollider.enabled = false;
        }
        
        // Desactivar todos los componentes MonoBehaviour excepto este
        MonoBehaviour[] allComponents = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour comp in allComponents)
        {
            if (comp != this && comp != null && comp.enabled)
            {
                comp.enabled = false;
            }
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
            Debug.LogError($"Error al crear fragmentos/partículas: {e.Message}");
        }
        
        // Mantener posición fija durante toda la secuencia
        StartCoroutine(KeepPositionFixed(finalPosition));
        
        // Flash blanco (muy rápido, casi imperceptible) - solo en fragmentos
        StartCoroutine(FlashWhite());
        
        // Shake (muy corto) - usar posición fija como base
        StartCoroutine(ShakeAnimation(finalPosition));
        
        // Iniciar la coroutine solo para esperar y destruir (sin crear efectos aquí)
        StartCoroutine(DestructionSequence());
    }
    
    /// <summary>
    /// Secuencia completa de destrucción
    /// Las operaciones críticas ya se ejecutaron en DestroyObstacle() para que sean inmediatas
    /// </summary>
    private IEnumerator DestructionSequence()
    {
        // La posición ya se guardó y fijó en DestroyObstacle()
        // Los fragmentos y partículas ya se crearon en DestroyObstacle() para ser instantáneos
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
    /// </summary>
    private IEnumerator KeepPositionFixed(Vector3 fixedPosition)
    {
        while (isDestroying)
        {
            // Forzar posición en cada frame
            transform.position = fixedPosition;
            yield return new WaitForEndOfFrame();
        }
    }
    
    /// <summary>
    /// Crea el sistema de partículas de explosión con efecto de fuego (igual que el planeta)
    /// </summary>
    private void CreateExplosionParticles()
    {
        // Obtener color promedio del sprite
        Color averageColor = GetAverageColorFromSprite();
        
        // Crear colores de fuego (naranja/rojo/amarillo) mezclados con el color del obstáculo
        Color fireOrange = new Color(1f, 0.5f, 0.1f);
        Color fireRed = new Color(1f, 0.2f, 0f);
        Color fireYellow = new Color(1f, 0.8f, 0.2f);
        
        // Mezclar colores de fuego con el color del obstáculo
        Color fireColor1 = Color.Lerp(fireOrange, averageColor, 0.3f);
        Color fireColor2 = Color.Lerp(fireRed, averageColor, 0.2f);
        Color fireColor3 = Color.Lerp(fireYellow, averageColor, 0.4f);
        
        // CRÍTICO: Usar la posición guardada (igual que el planeta)
        Vector3 particlePosition = originalPosition;
        
        // DEBUG: Verificar posición de partículas
        Debug.Log($"ObstacleDestructionController: Creando partículas en posición: {particlePosition}, originalPosition: {originalPosition}, transform.position: {transform.position}");
        
        // Crear GameObject para las partículas en la posición exacta
        GameObject particleSystemObj = new GameObject("ObstacleExplosionParticles");
        particleSystemObj.transform.position = particlePosition;
        
        // Agregar ParticleSystem
        ParticleSystem ps = particleSystemObj.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        ParticleSystem.EmissionModule emission = ps.emission;
        ParticleSystem.ShapeModule shape = ps.shape;
        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        ParticleSystem.NoiseModule noise = ps.noise;
        
        // Configurar main module (igual que el planeta)
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f); // Un poco más corto que el planeta
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
        
        // Configurar velocity (radial)
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.radial = new ParticleSystem.MinMaxCurve(particleSpeed * 0.9f, particleSpeed * 1.4f);
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);
        
        // Configurar noise para movimiento de fuego más orgánico (igual que el planeta)
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        noise.frequency = 0.5f;
        noise.scrollSpeed = 2f;
        
        // Configurar size over lifetime (crece un poco y luego se desvanece) - igual que el planeta
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f); // Empieza pequeño
        sizeCurve.AddKey(0.2f, 1f); // Crece rápido
        sizeCurve.AddKey(0.6f, 0.8f); // Se mantiene
        sizeCurve.AddKey(1f, 0f); // Se desvanece
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Configurar color over lifetime (transición de fuego a humo) - igual que el planeta
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
        
        // Renderer module
        ParticleSystemRenderer renderer = particleSystemObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = spriteRenderer.sortingOrder + 5;
        
        // Destruir el sistema de partículas después de que termine
        Destroy(particleSystemObj, 2f);
    }
    
    /// <summary>
    /// Genera fragmentos reales del sprite del obstáculo usando RenderTexture (igual que el planeta)
    /// </summary>
    private void CreateFragments()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning("ObstacleDestructionController: No sprite available for fragments");
            return;
        }
        
        Sprite originalSprite = spriteRenderer.sprite;
        
        // Obtener el rect del sprite y el pivot
        Rect spriteRect = originalSprite.rect;
        int spriteWidth = (int)spriteRect.width;
        int spriteHeight = (int)spriteRect.height;
        Vector2 spritePivot = originalSprite.pivot;
        Vector2 spriteSize = originalSprite.rect.size;
        
        // Crear RenderTexture para copiar el sprite a una textura legible
        RenderTexture renderTexture = RenderTexture.GetTemporary(spriteWidth, spriteHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        
        // Crear cámara temporal para renderizar el sprite
        GameObject tempCameraObj = new GameObject("TempCamera");
        Camera tempCamera = tempCameraObj.AddComponent<Camera>();
        tempCamera.orthographic = true;
        float spriteWorldHeight = spriteHeight / originalSprite.pixelsPerUnit;
        tempCamera.orthographicSize = spriteWorldHeight * 0.5f;
        tempCamera.clearFlags = CameraClearFlags.SolidColor;
        tempCamera.backgroundColor = Color.clear;
        tempCamera.cullingMask = 1 << 0;
        tempCamera.targetTexture = renderTexture;
        tempCamera.transform.position = new Vector3(0, 0, -10);
        tempCamera.enabled = false;
        
        // Crear GameObject temporal para el sprite
        GameObject tempSpriteObj = new GameObject("TempSprite");
        tempSpriteObj.transform.position = Vector3.zero;
        tempSpriteObj.layer = 0;
        SpriteRenderer tempRenderer = tempSpriteObj.AddComponent<SpriteRenderer>();
        tempRenderer.sprite = originalSprite;
        tempRenderer.color = spriteRenderer.color;
        tempRenderer.sortingLayerName = "Default";
        tempRenderer.sortingOrder = 1000;
        
        // Asegurar que el sprite esté centrado en la cámara
        Vector2 pivotOffset = (spritePivot - spriteSize * 0.5f) / originalSprite.pixelsPerUnit;
        tempSpriteObj.transform.position = new Vector3(-pivotOffset.x, -pivotOffset.y, 0);
        
        // Renderizar manualmente
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);
        tempCamera.Render();
        
        // Leer píxeles del RenderTexture
        Texture2D readableTexture = new Texture2D(spriteWidth, spriteHeight, TextureFormat.RGBA32, false);
        readableTexture.ReadPixels(new Rect(0, 0, spriteWidth, spriteHeight), 0, 0);
        readableTexture.Apply();
        
        // Verificar que la textura tiene contenido válido (igual que el planeta)
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
            Debug.LogWarning("ObstacleDestructionController: La textura legible no tiene píxeles visibles. El sprite puede no haberse renderizado correctamente.");
        }
        else
        {
            Debug.Log($"ObstacleDestructionController: Textura legible creada con {visiblePixelCount} píxeles visibles de {allPixels.Length} totales.");
        }
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        
        // Limpiar objetos temporales
        Destroy(tempCameraObj);
        Destroy(tempSpriteObj);
        
        // Verificar que la textura se creó correctamente
        if (readableTexture == null || readableTexture.width == 0 || readableTexture.height == 0)
        {
            Debug.LogError("ObstacleDestructionController: No se pudo crear la textura legible para fragmentos");
            if (readableTexture != null) Destroy(readableTexture);
            return;
        }
        
        // Verificar que la textura tiene contenido (no está vacía) - igual que el planeta
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
            Debug.LogWarning("ObstacleDestructionController: La textura legible está vacía. Los fragmentos pueden no verse correctamente.");
        }
        
        // Crear fragmentos con tamaños y posiciones variadas
        List<FragmentData> fragmentDataList = new List<FragmentData>();
        
        // Generar posiciones y tamaños variados (menos fragmentos que el planeta)
        for (int i = 0; i < fragmentCount; i++)
        {
            FragmentData fragData = new FragmentData();
            
            // Tamaño variado
            float distanceFromCenter = Random.Range(0f, 0.7f);
            float sizeMultiplier = 1f - (distanceFromCenter * 0.4f);
            sizeMultiplier = Mathf.Clamp(sizeMultiplier, 0.4f, 1.2f);
            
            fragData.width = Mathf.RoundToInt((spriteWidth / Mathf.Sqrt(fragmentCount)) * sizeMultiplier);
            fragData.height = Mathf.RoundToInt((spriteHeight / Mathf.Sqrt(fragmentCount)) * sizeMultiplier);
            
            // Asegurar tamaño mínimo y máximo
            fragData.width = Mathf.Clamp(fragData.width, 12, spriteWidth / 2);
            fragData.height = Mathf.Clamp(fragData.height, 12, spriteHeight / 2);
            
            // Posición variada
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
        int fragmentsSkipped = 0;
        foreach (FragmentData fragData in fragmentDataList)
        {
            // Verificar que el fragmento esté dentro de los límites
            if (fragData.x < 0 || fragData.y < 0 || 
                fragData.x + fragData.width > spriteWidth || 
                fragData.y + fragData.height > spriteHeight)
            {
                fragmentsSkipped++;
                Debug.LogWarning($"ObstacleDestructionController: Fragmento fuera de límites: x={fragData.x}, y={fragData.y}, w={fragData.width}, h={fragData.height}, spriteW={spriteWidth}, spriteH={spriteHeight}");
                continue;
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
                fragmentsSkipped++;
                Debug.LogWarning($"ObstacleDestructionController: Fragmento sin píxeles visibles: x={fragData.x}, y={fragData.y}, w={fragData.width}, h={fragData.height}");
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
            
            // Calcular posición relativa del fragmento
            float offsetX = (fragData.x + fragData.width / 2f - spritePivot.x) / originalSprite.pixelsPerUnit;
            float offsetY = (fragData.y + fragData.height / 2f - spritePivot.y) / originalSprite.pixelsPerUnit;
            
            Vector3 fragmentOffset = new Vector3(
                offsetX * originalScale.x,
                offsetY * originalScale.y,
                0f
            );
            
            fragment.transform.position = originalPosition + fragmentOffset;
            fragment.transform.localScale = originalScale;
            
            // Configurar sprite
            SpriteRenderer fragRenderer = fragment.GetComponent<SpriteRenderer>();
            fragRenderer.sprite = fragmentSprite;
            fragRenderer.color = spriteRenderer.color;
            fragRenderer.sortingOrder = spriteRenderer.sortingOrder + 3;
            
            // Configurar física
            Rigidbody2D rb = fragment.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = fragment.AddComponent<Rigidbody2D>();
            }
            
            rb.gravityScale = 0f;
            rb.drag = 0f;
            rb.angularDrag = 0f;
            rb.isKinematic = false;
            
            // Calcular dirección radial desde el centro
            Vector2 direction = new Vector2(fragmentOffset.x, fragmentOffset.y);
            
            if (direction.magnitude < 0.01f)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            else
            {
                direction = direction.normalized;
            }
            
            // Velocidad en dirección radial
            float speed = fragmentSpeed * Random.Range(0.8f, 1.4f);
            rb.velocity = direction * speed;
            
            // Agregar rotación aleatoria
            rb.angularVelocity = Random.Range(-360f, 360f);
            
            // Escala inicial
            Vector3 initialScale = Vector3.one;
            fragment.transform.localScale = initialScale;
            
            // Activar fragmento
            fragment.SetActive(true);
            
            Debug.Log($"ObstacleDestructionController: Fragmento {fragmentsCreated + 1} creado en posición {fragment.transform.position}, escala {fragment.transform.localScale}, velocidad {rb.velocity}, sortingOrder {fragRenderer.sortingOrder}");
            
            // Animar fragmento: hacerlo más pequeño hasta desaparecer
            StartCoroutine(AnimateFragmentScale(fragment, initialScale, Random.Range(0.4f, 0.6f)));
            
            fragmentsCreated++;
        }
        
        Debug.Log($"ObstacleDestructionController: Creados {fragmentsCreated} fragmentos del sprite (saltados: {fragmentsSkipped})");
        
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
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                
                return fragment;
            }
        }
        
        // Crear nuevo fragmento si no hay disponibles
        GameObject newFragment = new GameObject("ObstacleFragment");
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
            
            // Reducir escala gradualmente
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
    /// Obtiene el color promedio del sprite del obstáculo
    /// </summary>
    private Color GetAverageColorFromSprite()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return originalColor;
        }
        
        // Intentar obtener el color del sprite renderer primero
        Color rendererColor = spriteRenderer.color;
        if (rendererColor != Color.white && rendererColor.a > 0.1f)
        {
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
    /// Detección de colisión con el jugador (opcional)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroying) return;
        
        bool isPlayer = other.CompareTag("Player") || 
                       other.gameObject.name == "Player" ||
                       other.gameObject.CompareTag("Player");
        
        if (isPlayer)
        {
            DestroyObstacle();
        }
    }
    
    /// <summary>
    /// Detección de colisión normal (opcional)
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroying) return;
        
        bool isPlayer = collision.gameObject.CompareTag("Player") || 
                       collision.gameObject.name == "Player" ||
                       collision.gameObject.CompareTag("Player");
        
        if (isPlayer)
        {
            DestroyObstacle();
        }
    }
    
    /// <summary>
    /// Verifica si el obstáculo está siendo destruido
    /// </summary>
    public bool IsDestroying()
    {
        return isDestroying;
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
