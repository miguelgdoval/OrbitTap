using UnityEngine;
using System.Collections.Generic;

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
        // Aplicar opacidad inicial
        SetOpacity(opacity);
        
        // Configurar scroll infinito después de que todas las propiedades estén configuradas
        if (infiniteScroll && spriteDensity > 1 && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            SetupInfiniteScroll();
            Debug.Log($"[BackgroundLayer] {gameObject.name}: Scroll infinito configurado (Density={spriteDensity}, Speed={scrollSpeed})");
        }
        else if (scrollSpeed > 0)
        {
            Debug.Log($"[BackgroundLayer] {gameObject.name}: Scroll simple activo (Speed={scrollSpeed}, Density={spriteDensity})");
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
        for (int i = 0; i < spriteDensity; i++)
        {
            GameObject instance = new GameObject($"{gameObject.name}_Instance_{i}");
            instance.transform.SetParent(transform);
            
            SpriteRenderer instanceSR = instance.AddComponent<SpriteRenderer>();
            instanceSR.sprite = spriteRenderer.sprite;
            instanceSR.material = spriteRenderer.material;
            instanceSR.sortingOrder = spriteRenderer.sortingOrder;
            instanceSR.color = spriteRenderer.color;
            
            // Posición con offset aleatorio si está habilitado
            float offset = i * spacing;
            if (randomizeOffsets && i > 0)
            {
                offset += Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
            }
            
            Vector3 position = GetScrollDirectionVector() * offset;
            instance.transform.localPosition = position;
            instance.transform.localScale = Vector3.one;
            
            spriteInstances[i] = instance;
            spritePositions[i] = position;
        }
        
        // Ocultar el sprite original si tenemos instancias
        if (spriteDensity > 1)
        {
            spriteRenderer.enabled = false;
        }
    }
    
    private void UpdateScroll()
    {
        Vector2 directionVector = GetScrollDirectionVector();
        float deltaMovement = scrollSpeed * parallaxMultiplier * Time.deltaTime;
        
        // Siempre usar método Transform para prefabs (más confiable)
        // El método UV puede fallar si el material no está configurado correctamente
        if (infiniteScroll && spriteInstances != null && spriteInstances.Length > 0)
        {
            // Método Transform: para scroll infinito con múltiples sprites
            Sprite sprite = spriteRenderer.sprite;
            if (sprite != null)
            {
                float spriteHeight = sprite.bounds.size.y * transform.localScale.y;
                if (spriteHeight <= 0) spriteHeight = 10f; // Fallback
                
                float resetDistance = spriteHeight * spriteDensity;
                
                for (int i = 0; i < spriteInstances.Length; i++)
                {
                    if (spriteInstances[i] != null)
                    {
                        spriteInstances[i].transform.localPosition += (Vector3)(directionVector * deltaMovement);
                        
                        // Resetear posición cuando se sale de pantalla
                        Vector3 pos = spriteInstances[i].transform.localPosition;
                        float distance = Mathf.Abs(pos.y); // Usar distancia absoluta en Y para scroll vertical
                        
                        if (distance > resetDistance)
                        {
                            // Reposicionar al inicio
                            float newOffset = -resetDistance + (spriteHeight * 0.5f);
                            if (randomizeOffsets)
                            {
                                newOffset += Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
                            }
                            spriteInstances[i].transform.localPosition = GetScrollDirectionVector() * newOffset;
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
                    }
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

