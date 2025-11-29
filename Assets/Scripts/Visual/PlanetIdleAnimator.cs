using UnityEngine;

/// <summary>
/// Sistema universal de animación idle para planetas.
/// Proporciona rotación, breathing effect, glow animado y highlight falso 2D.
/// Funciona con cualquier sprite sin necesidad de texturas adicionales.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlanetIdleAnimator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Velocidad de rotación en grados por segundo")]
    public float rotationSpeed = 10f;
    
    [Header("Breathing Effect")]
    [Tooltip("Amplitud de la oscilación de escala (0.02 = 2% de variación)")]
    [Range(0f, 0.1f)]
    public float scaleAmplitude = 0.02f;
    
    [Tooltip("Frecuencia de la oscilación de escala")]
    public float scaleFrequency = 1f;
    
    [Header("Glow Animation")]
    [Tooltip("Amplitud del glow (0.2 = 20% más brillante en el pico)")]
    [Range(0f, 0.5f)]
    public float glowAmplitude = 0.2f;
    
    [Tooltip("Frecuencia del glow")]
    public float glowFrequency = 1.5f;
    
    [Header("Fake Lighting")]
    [Tooltip("Dirección de la luz falsa (normalizado)")]
    public Vector2 lightDirection = new Vector2(0.7f, 0.7f);
    
    [Tooltip("Intensidad base de la luz falsa")]
    [Range(0f, 1f)]
    public float baseLightIntensity = 0.3f;
    
    [Tooltip("Amplitud de la animación de la luz")]
    [Range(0f, 0.3f)]
    public float lightAnimationAmplitude = 0.1f;
    
    [Tooltip("Frecuencia de la animación de la luz")]
    public float lightAnimationFrequency = 0.8f;
    
    private SpriteRenderer spriteRenderer;
    private Material planetMaterial;
    private Vector3 baseScale;
    private Color baseColor;
    private bool materialCreated = false;
    private bool shouldRotate = true; // Si hay PlanetSurface, no rotamos desde aquí
    
    // IDs de propiedades del shader (cached para evitar GC)
    private static readonly int LightDirID = Shader.PropertyToID("_LightDir");
    private static readonly int LightIntensityID = Shader.PropertyToID("_LightIntensity");
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"PlanetIdleAnimator: No SpriteRenderer found on {gameObject.name}. Component disabled.");
            enabled = false;
            return;
        }
        
        // Si hay PlanetSurface, desactivar la rotación de este componente
        // PlanetSurface ya maneja la rotación visual del sprite
        PlanetSurface planetSurface = GetComponent<PlanetSurface>();
        if (planetSurface != null)
        {
            shouldRotate = false;
        }
        
        baseScale = transform.localScale;
        baseColor = spriteRenderer.color;
        
        // Crear o obtener el material
        SetupMaterial();
    }
    
    private void SetupMaterial()
    {
        // Intentar cargar el shader (detectar pipeline y usar el shader apropiado)
        Shader planetShader = null;
        
        // Intentar primero el shader Built-in
        planetShader = Shader.Find("Custom/FakePlanetLight");
        
        // Si no se encuentra, intentar el shader URP
        if (planetShader == null)
        {
            planetShader = Shader.Find("Custom/FakePlanetLightURP");
        }
        
        if (planetShader == null)
        {
            Debug.LogWarning("PlanetIdleAnimator: Shader 'Custom/FakePlanetLight' or 'Custom/FakePlanetLightURP' not found. Using default material. The fake lighting effect will not work, but rotation and breathing will still function.");
            // Continuar sin el shader - las otras animaciones seguirán funcionando
            return;
        }
        
        // Crear material si no existe o si el shader cambió
        if (spriteRenderer.sharedMaterial == null || 
            spriteRenderer.sharedMaterial.shader != planetShader)
        {
            // Crear nuevo material
            planetMaterial = new Material(planetShader);
            planetMaterial.name = "PlanetIdleMaterial";
            
            // Copiar la textura del sprite al material
            if (spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
            {
                planetMaterial.mainTexture = spriteRenderer.sprite.texture;
            }
            
            // Copiar el color del sprite al material
            planetMaterial.color = spriteRenderer.color;
            
            // Aplicar el material
            spriteRenderer.material = planetMaterial;
            materialCreated = true;
            
            // Inicializar valores del shader
            UpdateShaderProperties();
        }
        else
        {
            planetMaterial = spriteRenderer.material;
        }
    }
    
    private void Update()
    {
        if (spriteRenderer == null) return;
        
        float time = Time.time;
        
        // Rotación constante (solo si no hay PlanetSurface)
        if (shouldRotate)
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
        
        // Breathing effect (oscilación de escala)
        float scaleOffset = Mathf.Sin(time * scaleFrequency * 2f * Mathf.PI) * scaleAmplitude;
        transform.localScale = baseScale * (1f + scaleOffset);
        
        // Glow animado (oscilación de brillo del color)
        float glowOffset = 1f + Mathf.Sin(time * glowFrequency * 2f * Mathf.PI) * glowAmplitude;
        // Aplicar glow manteniendo el tinte original, solo aumentando el brillo
        Color currentColor = baseColor;
        // Multiplicar RGB por el glow, pero mantener el alpha
        currentColor.r = Mathf.Clamp01(currentColor.r * glowOffset);
        currentColor.g = Mathf.Clamp01(currentColor.g * glowOffset);
        currentColor.b = Mathf.Clamp01(currentColor.b * glowOffset);
        spriteRenderer.color = currentColor;
        
        // Actualizar propiedades del shader para el highlight falso
        if (materialCreated && planetMaterial != null)
        {
            UpdateShaderProperties();
        }
    }
    
    private void UpdateShaderProperties()
    {
        if (planetMaterial == null) return;
        
        // Normalizar dirección de la luz
        Vector2 normalizedLightDir = lightDirection.normalized;
        
        // Animar la intensidad de la luz
        float lightIntensity = baseLightIntensity + 
            Mathf.Sin(Time.time * lightAnimationFrequency * 2f * Mathf.PI) * lightAnimationAmplitude;
        lightIntensity = Mathf.Clamp01(lightIntensity);
        
        // Actualizar propiedades del shader
        planetMaterial.SetVector(LightDirID, normalizedLightDir);
        planetMaterial.SetFloat(LightIntensityID, lightIntensity);
    }
    
    private void OnValidate()
    {
        // Normalizar dirección de la luz en el editor
        if (lightDirection.magnitude > 0.01f)
        {
            lightDirection = lightDirection.normalized;
        }
        else
        {
            lightDirection = new Vector2(0.7f, 0.7f).normalized;
        }
    }
    
    private void OnDestroy()
    {
        // Limpiar material creado si fue creado por este componente
        if (materialCreated && planetMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(planetMaterial);
            }
            else
            {
                DestroyImmediate(planetMaterial);
            }
        }
    }
    
    /// <summary>
    /// Actualiza el sprite del planeta (útil cuando se cambia de skin)
    /// </summary>
    public void UpdateSprite(Sprite newSprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = newSprite;
            
            // Actualizar textura del material si existe
            if (materialCreated && planetMaterial != null && newSprite != null && newSprite.texture != null)
            {
                planetMaterial.mainTexture = newSprite.texture;
            }
        }
    }
    
    /// <summary>
    /// Resetea el color base (útil cuando se cambia de skin)
    /// </summary>
    public void SetBaseColor(Color color)
    {
        baseColor = color;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
}

