using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Componente avanzado para capas de fondo con parallax, scroll infinito, pulsing y más.
/// Optimizado para móvil y resolución vertical (9:16).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundLayer : MonoBehaviour
{
    [System.Serializable]
    public enum ScrollDirection
    {
        Down = 0,
        Up = 1,
        Left = 2,
        Right = 3,
        DiagonalDownRight = 4,
        DiagonalDownLeft = 5
    }
    
    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 1f;
    [SerializeField] private ScrollDirection direction = ScrollDirection.Down;
    [SerializeField] private float parallaxMultiplier = 1f; // 1.0 = normal, <1.0 = más lento (lejano), >1.0 = más rápido (cercano)
    [SerializeField] private bool infiniteScroll = true;
    
    [Header("Sprite Settings")]
    [SerializeField] private int spriteDensity = 1; // Cuántos sprites instanciar para scroll infinito
    [SerializeField] private bool randomizeOffsets = true; // Evitar patrones repetidos
    [SerializeField] private Vector2 randomOffsetRange = new Vector2(0.5f, 2f);
    
    // Propiedades públicas para configuración desde código
    public int SpriteDensity { get => spriteDensity; set => spriteDensity = value; }
    public bool InfiniteScroll { get => infiniteScroll; set => infiniteScroll = value; }
    public ScrollDirection Direction { get => direction; set => direction = value; }
    
    [Header("Animation Settings")]
    [SerializeField] private bool enablePulsing = false;
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float pulseScaleMin = 0.95f;
    [SerializeField] private float pulseScaleMax = 1.05f;
    
    [Header("Visual Settings")]
    [SerializeField] [Range(0f, 1f)] private float opacity = 1f;
    [SerializeField] private bool useUVScrolling = true; // Más eficiente para móvil
    
    [Header("Auto-Scaling")]
    [SerializeField] private bool autoScaleToScreen = true;
    [SerializeField] private float scaleMultiplier = 1.1f; // Margen extra para evitar bordes
    
    private SpriteRenderer spriteRenderer;
    private Material materialInstance;
    private Vector2 uvOffset = Vector2.zero;
    private Vector3 initialScale;
    private Vector3[] spritePositions;
    private GameObject[] spriteInstances;
    private float pulseTime = 0f;
    private Camera mainCamera;
    private float screenHeight;
    private float screenWidth;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // Crear instancia del material para no afectar el material compartido
        if (spriteRenderer.material != null)
        {
            materialInstance = new Material(spriteRenderer.material);
            spriteRenderer.material = materialInstance;
            
            // Configurar wrap mode repeat para la textura del sprite
            if (spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
            {
                Texture2D spriteTexture = spriteRenderer.sprite.texture;
                // Nota: No podemos cambiar el wrap mode de una textura importada directamente
                // El wrap mode debe configurarse en los Import Settings de la textura
                // Pero podemos verificar que esté configurado correctamente
                if (spriteTexture.wrapMode != TextureWrapMode.Repeat)
                {
                    Debug.LogWarning($"[BackgroundLayer] {gameObject.name}: La textura '{spriteTexture.name}' no tiene Wrap Mode = Repeat. " +
                        "Configúralo en los Import Settings de la textura para que el scroll infinito funcione correctamente.");
                }
            }
        }
        
        // Calcular dimensiones de pantalla
        CalculateScreenDimensions();
        
        // Configurar auto-scaling
        if (autoScaleToScreen)
        {
            ScaleToScreen();
        }
        
        // El scroll infinito se configurará en Start() después de que se configuren todas las propiedades
    }
    
    private void Start()
    {
        // Log inicial para todas las capas
        Debug.Log($"[BackgroundLayer] {gameObject.name}: Start() llamado - infiniteScroll={infiniteScroll}, spriteDensity={spriteDensity}, scrollSpeed={scrollSpeed}");
        
        // Aplicar opacidad inicial
        SetOpacity(opacity);
        
        // Verificar valores serializados (debug)
        var densityField = typeof(BackgroundLayer).GetField("spriteDensity", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (densityField != null)
        {
            int serializedDensity = (int)densityField.GetValue(this);
            Debug.Log($"[BackgroundLayer] {gameObject.name}: spriteDensity - Serializado={serializedDensity}, Propiedad={spriteDensity}, infiniteScroll={infiniteScroll}");
            
            if (serializedDensity != spriteDensity)
            {
                Debug.LogWarning($"[BackgroundLayer] {gameObject.name}: Discrepancia detectada. Usando valor serializado: {serializedDensity}");
                spriteDensity = serializedDensity;
            }
        }
        
        // Si es una capa de estrellas y tiene infiniteScroll pero spriteDensity=1, forzar a 3
        string layerName = gameObject.name.ToLower();
        if (layerName.Contains("star") && infiniteScroll && spriteDensity == 1)
        {
            Debug.LogWarning($"[BackgroundLayer] {gameObject.name}: Forzando spriteDensity a 3 para scroll infinito de estrellas");
            spriteDensity = 3;
            if (densityField != null)
            {
                densityField.SetValue(this, 3);
            }
        }
        
        // Verificar condiciones para scroll infinito
        bool hasSprite = spriteRenderer != null && spriteRenderer.sprite != null;
        Debug.Log($"[BackgroundLayer] {gameObject.name}: Condiciones - infiniteScroll={infiniteScroll}, spriteDensity={spriteDensity}, hasSprite={hasSprite}, spriteName={spriteRenderer?.sprite?.name ?? "NULL"}");
        
        // Configurar scroll infinito después de que todas las propiedades estén configuradas
        // Si spriteDensity = 1 y useUVScrolling está habilitado, usar UV scrolling (permite wrap mode repeat)
        // Si spriteDensity > 1, usar método Transform con múltiples instancias
        if (infiniteScroll && spriteDensity > 1 && hasSprite)
        {
            SetupInfiniteScroll();
            Debug.Log($"[BackgroundLayer] {gameObject.name}: ✅ Scroll infinito configurado (Density={spriteDensity}, Speed={scrollSpeed}, Instances={spriteInstances?.Length ?? 0})");
        }
        else if (infiniteScroll && spriteDensity == 1 && useUVScrolling && materialInstance != null)
        {
            // UV scrolling con wrap mode repeat - no necesita instancias
            Sprite sprite = spriteRenderer.sprite;
            if (sprite != null && sprite.texture != null)
            {
                Debug.Log($"[BackgroundLayer] {gameObject.name}: ✅ Scroll infinito UV configurado (Wrap Mode: {sprite.texture.wrapMode}, Speed={scrollSpeed}, Texture: {sprite.texture.name})");
                if (sprite.texture.wrapMode != TextureWrapMode.Repeat)
                {
                    Debug.LogError($"[BackgroundLayer] {gameObject.name}: ⚠️ La textura '{sprite.texture.name}' NO tiene Wrap Mode = Repeat! " +
                        "Ve a los Import Settings de la textura y cambia Wrap Mode a Repeat para que funcione el scroll infinito.");
                }
            }
            else
            {
                Debug.LogWarning($"[BackgroundLayer] {gameObject.name}: ⚠️ No se puede configurar UV scrolling - sprite o textura es NULL");
            }
        }
        else if (scrollSpeed > 0)
        {
            if (infiniteScroll && spriteDensity <= 1)
            {
                Debug.LogWarning($"[BackgroundLayer] {gameObject.name}: ⚠️ Scroll infinito habilitado pero spriteDensity={spriteDensity}. Necesita > 1 para funcionar.");
            }
            if (infiniteScroll && !hasSprite)
            {
                Debug.LogWarning($"[BackgroundLayer] {gameObject.name}: ⚠️ Scroll infinito habilitado pero no hay sprite asignado.");
            }
            Debug.Log($"[BackgroundLayer] {gameObject.name}: Scroll simple activo (Speed={scrollSpeed}, Density={spriteDensity})");
        }
        else
        {
            Debug.Log($"[BackgroundLayer] {gameObject.name}: Sin scroll (Speed={scrollSpeed})");
        }
    }
    
    private void Update()
    {
        // Scroll
        if (scrollSpeed > 0f)
        {
            UpdateScroll();
        }
        
        // Pulsing
        if (enablePulsing)
        {
            UpdatePulsing();
        }
        
        // Actualizar opacidad si cambió
        SetOpacity(opacity);
    }
    
    private void CalculateScreenDimensions()
    {
        if (mainCamera != null && mainCamera.orthographic)
        {
            screenHeight = mainCamera.orthographicSize * 2f;
            screenWidth = screenHeight * mainCamera.aspect;
        }
        else
        {
            // Fallback para pantalla estándar móvil 9:16
            screenHeight = 10f;
            screenWidth = screenHeight * (9f / 16f);
        }
    }
    
    private void ScaleToScreen()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        
        Sprite sprite = spriteRenderer.sprite;
        float spriteWidth = sprite.bounds.size.x;
        float spriteHeight = sprite.bounds.size.y;
        
        // Calcular escala necesaria para cubrir toda la pantalla
        float scaleX = (screenWidth / spriteWidth) * scaleMultiplier;
        float scaleY = (screenHeight / spriteHeight) * scaleMultiplier;
        
        // Usar el mayor para asegurar cobertura completa
        float scale = Mathf.Max(scaleX, scaleY);
        
        transform.localScale = initialScale * scale;
    }
    
    private void SetupInfiniteScroll()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        
        // Limpiar instancias anteriores si existen
        if (spriteInstances != null)
        {
            foreach (GameObject instance in spriteInstances)
            {
                if (instance != null)
                {
                    Destroy(instance);
                }
            }
        }
        
        spriteInstances = new GameObject[spriteDensity];
        spritePositions = new Vector3[spriteDensity];
        
        Sprite sprite = spriteRenderer.sprite;
        float spriteHeight = sprite.bounds.size.y * transform.localScale.y;
        if (spriteHeight <= 0) spriteHeight = 10f; // Fallback para sprites muy pequeños
        float spacing = spriteHeight;
        
        // Crear instancias duplicadas
        // Para scroll infinito, necesitamos instancias que cubran desde -spacing hasta spacing * (density-1)
        // Esto asegura que siempre haya sprites visibles mientras otros se reposicionan
        for (int i = 0; i < spriteDensity; i++)
        {
            GameObject instance = new GameObject($"{gameObject.name}_Instance_{i}");
            instance.transform.SetParent(transform);
            
            SpriteRenderer instanceSR = instance.AddComponent<SpriteRenderer>();
            instanceSR.sprite = spriteRenderer.sprite;
            instanceSR.material = spriteRenderer.material;
            instanceSR.sortingOrder = spriteRenderer.sortingOrder;
            
            // Aplicar color y opacidad correctamente a las instancias
            Color instanceColor = spriteRenderer.color;
            instanceColor.a = opacity; // Asegurar que la opacidad sea la correcta
            instanceSR.color = instanceColor;
            
            // Asegurar que la instancia esté activa y visible
            instance.SetActive(true);
            instanceSR.enabled = true;
            
            // Posición inicial: espaciadas uniformemente alrededor del origen
            // Para scroll hacia abajo, empezamos desde arriba (positivo) y vamos hacia abajo (negativo)
            // Con 3 instancias: offset será -spacing, 0, spacing (centradas en 0)
            float offset = (i - (spriteDensity - 1) * 0.5f) * spacing;
            if (randomizeOffsets && i > 0)
            {
                offset += Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
            }
            
            Vector3 position = GetScrollDirectionVector() * offset;
            instance.transform.localPosition = position;
            // Usar la misma escala que el objeto padre para que las instancias tengan el tamaño correcto
            instance.transform.localScale = transform.localScale;
            
            spriteInstances[i] = instance;
            spritePositions[i] = position;
            
            // Debug log para verificar creación (solo para estrellas para no saturar)
            if (gameObject.name.Contains("Star"))
            {
                Debug.Log($"[BackgroundLayer] {gameObject.name} Instance {i} creada - Sprite: {instanceSR.sprite?.name ?? "NULL"}, Color: {instanceColor}, Opacity: {opacity}, Enabled: {instanceSR.enabled}, Active: {instance.activeSelf}, Position: {position}, Offset: {offset:F2}, Spacing: {spacing:F2}");
            }
        }
        
        // Ocultar el sprite original si tenemos instancias
        if (spriteDensity > 1)
        {
            spriteRenderer.enabled = false;
            Debug.Log($"[BackgroundLayer] {gameObject.name}: Sprite original ocultado, usando {spriteDensity} instancias");
            
            // Verificar que las instancias se crearon correctamente
            int activeInstances = 0;
            for (int i = 0; i < spriteInstances.Length; i++)
            {
                if (spriteInstances[i] != null && spriteInstances[i].activeSelf)
                {
                    SpriteRenderer sr = spriteInstances[i].GetComponent<SpriteRenderer>();
                    if (sr != null && sr.enabled && sr.sprite != null)
                    {
                        activeInstances++;
                    }
                }
            }
            Debug.Log($"[BackgroundLayer] {gameObject.name}: {activeInstances}/{spriteDensity} instancias activas y visibles");
        }
    }
    
    private void UpdateScroll()
    {
        Vector2 directionVector = GetScrollDirectionVector();
        float deltaMovement = scrollSpeed * parallaxMultiplier * Time.deltaTime;
        
        // Usar método de múltiples instancias para scroll infinito (más confiable que UV scrolling)
        if (infiniteScroll && spriteInstances != null && spriteInstances.Length > 0)
        {
            // Método Transform: para scroll infinito con múltiples sprites
            Sprite sprite = spriteRenderer.sprite;
            if (sprite != null)
            {
                float spriteHeight = sprite.bounds.size.y * transform.localScale.y;
                if (spriteHeight <= 0) spriteHeight = 10f; // Fallback
                
                // Calcular el rango de posiciones válidas
                float spacing = spriteHeight;
                float totalRange = spacing * spriteDensity;
                
                for (int i = 0; i < spriteInstances.Length; i++)
                {
                    if (spriteInstances[i] != null)
                    {
                        // Mover la instancia
                        spriteInstances[i].transform.localPosition += (Vector3)(directionVector * deltaMovement);
                        
                        // Obtener posición actual
                        Vector3 pos = spriteInstances[i].transform.localPosition;
                        
                        // Para scroll vertical (Down/Up)
                        if (direction == ScrollDirection.Down || direction == ScrollDirection.Up)
                        {
                            float posY = pos.y;
                            
                            // Para scroll hacia abajo (direction = Down)
                            if (direction == ScrollDirection.Down)
                            {
                                // Calcular el rango visible de la pantalla
                                float screenHeight = mainCamera != null && mainCamera.orthographic ? 
                                    mainCamera.orthographicSize * 2f : 10f;
                                float topScreen = screenHeight * 0.5f; // Límite superior visible
                                float bottomScreen = -screenHeight * 0.5f; // Límite inferior visible
                                
                                // Encontrar la instancia más arriba (mayor Y) pero solo dentro de un rango razonable
                                // Limitar la búsqueda a instancias que estén cerca de la pantalla visible
                                float maxY = float.MinValue;
                                float searchRange = screenHeight * 1.5f; // Buscar en un rango de 1.5 pantallas
                                
                                for (int j = 0; j < spriteInstances.Length; j++)
                                {
                                    if (j != i && spriteInstances[j] != null)
                                    {
                                        float otherY = spriteInstances[j].transform.localPosition.y;
                                        // Solo considerar instancias que estén en un rango razonable (cerca de la pantalla)
                                        if (otherY > maxY && otherY <= topScreen + searchRange && otherY >= bottomScreen - searchRange)
                                        {
                                            maxY = otherY;
                                        }
                                    }
                                }
                                
                                // Si no hay otras instancias válidas, usar un valor por defecto razonable
                                if (maxY == float.MinValue)
                                {
                                    maxY = topScreen;
                                }
                                
                                // Reposicionar cuando la instancia esté fuera de la pantalla por abajo
                                // Y esté más de spacing por debajo de la más alta
                                // Esto asegura que siempre haya instancias visibles
                                if (posY < bottomScreen - spacing && posY < maxY - spacing)
                                {
                                    float oldY = posY;
                                    // Reposicionar justo arriba de la más alta
                                    posY = maxY + spacing;
                                    if (randomizeOffsets)
                                    {
                                        posY += Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
                                    }
                                    // Asegurar que la nueva posición esté en un rango visible razonable
                                    // Limitar a no más de 1 pantalla arriba del límite superior
                                    float maxAllowedY = topScreen + screenHeight;
                                    float minAllowedY = bottomScreen - screenHeight * 0.5f;
                                    posY = Mathf.Clamp(posY, minAllowedY, maxAllowedY);
                                    spriteInstances[i].transform.localPosition = new Vector3(pos.x, posY, pos.z);
                                    
                                    // Debug log para estrellas (solo ocasionalmente)
                                    if (gameObject.name.Contains("Star") && Random.Range(0, 200) == 0)
                                    {
                                        Debug.Log($"[BackgroundLayer] {gameObject.name} Instance {i} reposicionada: {oldY:F2} -> {posY:F2} (maxY={maxY:F2}, spacing={spacing:F2}, clamped to [{minAllowedY:F2}, {maxAllowedY:F2}])");
                                    }
                                }
                            }
                            // Para scroll hacia arriba (direction = Up)
                            else
                            {
                                // Encontrar la instancia más abajo (menor Y)
                                float minY = float.MaxValue;
                                bool foundOther = false;
                                
                                for (int j = 0; j < spriteInstances.Length; j++)
                                {
                                    if (j != i && spriteInstances[j] != null)
                                    {
                                        float otherY = spriteInstances[j].transform.localPosition.y;
                                        if (otherY < minY) minY = otherY;
                                        foundOther = true;
                                    }
                                }
                                
                                // Si no hay otras instancias, usar un valor por defecto
                                if (!foundOther)
                                {
                                    float screenHeight = mainCamera != null && mainCamera.orthographic ? 
                                        mainCamera.orthographicSize * 2f : 10f;
                                    minY = -screenHeight * 0.5f;
                                }
                                
                                // Si esta instancia está más de (spacing) por arriba de la más baja,
                                // reposicionarla debajo de la más baja para crear un loop continuo
                                if (posY > minY + spacing)
                                {
                                    float oldY = posY;
                                    posY = minY - spacing;
                                    if (randomizeOffsets)
                                    {
                                        posY += Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
                                    }
                                    spriteInstances[i].transform.localPosition = new Vector3(pos.x, posY, pos.z);
                                    
                                    // Debug log para estrellas
                                    if (gameObject.name.Contains("Star"))
                                    {
                                        Debug.Log($"[BackgroundLayer] {gameObject.name} Instance {i} reposicionada (Up): {oldY:F2} -> {posY:F2} (minY={minY:F2}, spacing={spacing:F2})");
                                    }
                                }
                            }
                        }
                        // Para scroll horizontal (Left/Right)
                        else if (direction == ScrollDirection.Left || direction == ScrollDirection.Right)
                        {
                            float posX = pos.x;
                            float threshold = totalRange * 0.5f;
                            
                            if (posX < -threshold)
                            {
                                posX += totalRange;
                                if (randomizeOffsets)
                                {
                                    posX += Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
                                }
                                spriteInstances[i].transform.localPosition = new Vector3(posX, pos.y, pos.z);
                            }
                            else if (posX > threshold)
                            {
                                posX -= totalRange;
                                if (randomizeOffsets)
                                {
                                    posX += Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
                                }
                                spriteInstances[i].transform.localPosition = new Vector3(posX, pos.y, pos.z);
                            }
                        }
                        // Para scroll diagonal, usar la componente principal
                        else
                        {
                            // Para diagonales, usar la distancia desde el origen
                            float distance = Vector3.Distance(pos, Vector3.zero);
                            if (distance > totalRange)
                            {
                                // Reposicionar al inicio del loop
                                Vector2 dir = GetScrollDirectionVector();
                                float newOffset = -totalRange * 0.5f;
                            if (randomizeOffsets)
                            {
                                newOffset += Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
                                }
                                spriteInstances[i].transform.localPosition = (Vector3)(dir * newOffset);
                            }
                        }
                    }
                }
            }
        }
        else if (useUVScrolling && materialInstance != null && spriteDensity <= 1)
        {
            // Método UV: más eficiente para móvil (solo si material está disponible)
            uvOffset += directionVector * deltaMovement;
            
            // Normalizar para evitar overflow
            if (uvOffset.y > 1f) uvOffset.y -= 1f;
            if (uvOffset.y < -1f) uvOffset.y += 1f;
            if (uvOffset.x > 1f) uvOffset.x -= 1f;
            if (uvOffset.x < -1f) uvOffset.x += 1f;
            
            materialInstance.SetTextureOffset("_MainTex", uvOffset);
        }
        else
        {
            // Método Transform simple (siempre funciona)
            transform.localPosition += (Vector3)(directionVector * deltaMovement);
            
            // Resetear posición cuando se sale de pantalla (para scroll simple)
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                float spriteHeight = spriteRenderer.sprite.bounds.size.y * transform.localScale.y;
                if (spriteHeight > 0)
                {
                    Vector3 pos = transform.localPosition;
                    if (Mathf.Abs(pos.y) > spriteHeight * 2f)
                    {
                        transform.localPosition = new Vector3(pos.x, -spriteHeight, pos.z);
                    }
                }
            }
        }
    }
    
    private void UpdatePulsing()
    {
        pulseTime += Time.deltaTime * pulseSpeed;
        float pulseValue = Mathf.Lerp(pulseScaleMin, pulseScaleMax, (Mathf.Sin(pulseTime) + 1f) * 0.5f);
        transform.localScale = initialScale * pulseValue;
    }
    
    private Vector2 GetScrollDirectionVector()
    {
        switch (direction)
        {
            case ScrollDirection.Down: return Vector2.down;
            case ScrollDirection.Up: return Vector2.up;
            case ScrollDirection.Left: return Vector2.left;
            case ScrollDirection.Right: return Vector2.right;
            case ScrollDirection.DiagonalDownRight: return new Vector2(1f, -1f).normalized;
            case ScrollDirection.DiagonalDownLeft: return new Vector2(-1f, -1f).normalized;
            default: return Vector2.down;
        }
    }
    
    /// <summary>
    /// Establece la opacidad de la capa
    /// </summary>
    public void SetOpacity(float newOpacity)
    {
        opacity = Mathf.Clamp01(newOpacity);
        
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = opacity;
            spriteRenderer.color = c;
        }
        
        // Aplicar a instancias también
        if (spriteInstances != null)
        {
            foreach (GameObject instance in spriteInstances)
            {
                if (instance != null)
                {
                    SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = opacity;
                        sr.color = c;
                        // Asegurar que el sprite renderer esté habilitado
                        sr.enabled = true;
                    }
                    // Asegurar que el GameObject esté activo
                    instance.SetActive(true);
                }
            }
        }
    }
    
    /// <summary>
    /// Establece la velocidad de scroll
    /// </summary>
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
        Debug.Log($"[BackgroundLayer] {gameObject.name}: Scroll speed set to {speed}");
    }
    
    /// <summary>
    /// Obtiene la velocidad de scroll actual
    /// </summary>
    public float GetScrollSpeed()
    {
        return scrollSpeed;
    }
    
    /// <summary>
    /// Obtiene la opacidad actual de la capa
    /// </summary>
    public float GetOpacity()
    {
        if (spriteRenderer != null)
        {
            return spriteRenderer.color.a;
        }
        return opacity;
    }
    
    /// <summary>
    /// Fuerza la reconfiguración del scroll infinito (útil cuando se cambia SpriteDensity después de Start)
    /// </summary>
    public void RefreshInfiniteScroll()
    {
        // Limpiar instancias anteriores
        if (spriteInstances != null)
        {
            foreach (GameObject instance in spriteInstances)
            {
                if (instance != null)
                {
                    Destroy(instance);
                }
            }
        }
        
        // Reconfigurar si es necesario
        if (infiniteScroll && spriteDensity > 1 && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            SetupInfiniteScroll();
        }
    }
    
    /// <summary>
    /// Establece el multiplicador de parallax
    /// </summary>
    public void SetParallaxMultiplier(float multiplier)
    {
        parallaxMultiplier = multiplier;
    }
    
    /// <summary>
    /// Activa o desactiva el pulsing
    /// </summary>
    public void SetPulsing(bool enabled, float speed = 1f, float minScale = 0.95f, float maxScale = 1.05f)
    {
        enablePulsing = enabled;
        pulseSpeed = speed;
        pulseScaleMin = minScale;
        pulseScaleMax = maxScale;
    }
    
    /// <summary>
    /// Resetea la capa a su estado inicial
    /// </summary>
    public void ResetLayer()
    {
        uvOffset = Vector2.zero;
        pulseTime = 0f;
        transform.localScale = initialScale;
        
        if (materialInstance != null)
        {
            materialInstance.SetTextureOffset("_MainTex", Vector2.zero);
        }
        
        // Resetear posiciones de instancias
        if (spriteInstances != null && spritePositions != null)
        {
            for (int i = 0; i < spriteInstances.Length; i++)
            {
                if (spriteInstances[i] != null)
                {
                    spriteInstances[i].transform.localPosition = spritePositions[i];
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        // Limpiar material instanciado
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
        
        // Limpiar instancias
        if (spriteInstances != null)
        {
            foreach (GameObject instance in spriteInstances)
            {
                if (instance != null)
                {
                    Destroy(instance);
                }
            }
        }
    }
    
    private void OnValidate()
    {
        // Actualizar opacidad en el editor
        if (Application.isPlaying && spriteRenderer != null)
        {
            SetOpacity(opacity);
        }
    }
}

