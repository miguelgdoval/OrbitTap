using UnityEngine;
using System.Collections;

/// <summary>
/// Sistema de efectos de daño fracturado para planetas.
/// Aplica un shader procedural que simula grietas, fracturas y desplazamiento
/// cuando el planeta recibe daño. Compatible con PlanetIdleAnimator y PlanetDestructionController.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlanetDamageEffect : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Cantidad actual de daño (0 = intacto, 1 = totalmente fracturado)")]
    [Range(0f, 1f)]
    public float damageAmount = 0f;
    
    [Tooltip("Intensidad del brillo en las grietas")]
    [Range(0f, 2f)]
    public float damageGlow = 1f;
    
    [Tooltip("Intensidad de la vibración/shake")]
    [Range(0f, 0.1f)]
    public float shakeIntensity = 0.02f;
    
    [Tooltip("Densidad de las células de fractura (más alto = fracturas más pequeñas)")]
    [Range(5f, 50f)]
    public float cellDensity = 20f;
    
    [Tooltip("Grosor de los bordes de las grietas")]
    [Range(0.001f, 0.1f)]
    public float edgeThickness = 0.02f;
    
    private SpriteRenderer spriteRenderer;
    private Material damageMaterial;
    private Material originalMaterial;
    private bool materialCreated = false;
    
    // Referencias a otros componentes para compatibilidad
    private PlanetIdleAnimator idleAnimator;
    private PlanetDestructionController destructionController;
    
    // IDs de propiedades del shader (cached para evitar GC)
    private static readonly int DamageAmountID = Shader.PropertyToID("_DamageAmount");
    private static readonly int DamageGlowID = Shader.PropertyToID("_DamageGlow");
    private static readonly int ShakeIntensityID = Shader.PropertyToID("_ShakeIntensity");
    private static readonly int CellDensityID = Shader.PropertyToID("_CellDensity");
    private static readonly int EdgeThicknessID = Shader.PropertyToID("_EdgeThickness");
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"PlanetDamageEffect: No SpriteRenderer found on {gameObject.name}. Component disabled.");
            enabled = false;
            return;
        }
        
        // Guardar material original
        originalMaterial = spriteRenderer.sharedMaterial;
        
        // Obtener referencias a otros componentes
        idleAnimator = GetComponent<PlanetIdleAnimator>();
        destructionController = GetComponent<PlanetDestructionController>();
        
        // Configurar el material de daño
        SetupDamageMaterial();
    }
    
    private void SetupDamageMaterial()
    {
        // Intentar cargar el shader
        Shader damageShader = Shader.Find("Custom/FracturedDamageShader");
        
        if (damageShader == null)
        {
            Debug.LogWarning("PlanetDamageEffect: Shader 'Custom/FracturedDamageShader' not found. Damage effects will not work.");
            return;
        }
        
        // Guardar el material actual (puede ser de PlanetIdleAnimator o el default)
        Material currentMaterial = spriteRenderer.sharedMaterial;
        
        // Crear material si no existe o si el shader cambió
        if (currentMaterial == null || currentMaterial.shader != damageShader)
        {
            // Crear nuevo material
            damageMaterial = new Material(damageShader);
            damageMaterial.name = "PlanetDamageMaterial";
            
            // Copiar propiedades del material actual o del sprite
            if (currentMaterial != null && currentMaterial.HasProperty("_MainTex"))
            {
                damageMaterial.mainTexture = currentMaterial.mainTexture;
            }
            else if (spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
            {
                damageMaterial.mainTexture = spriteRenderer.sprite.texture;
            }
            
            // Copiar el color del sprite
            damageMaterial.color = spriteRenderer.color;
            
            // Aplicar el material
            spriteRenderer.material = damageMaterial;
            materialCreated = true;
            
            // Inicializar valores del shader
            UpdateShaderProperties();
        }
        else
        {
            damageMaterial = spriteRenderer.material;
        }
    }
    
    private void Update()
    {
        // Si el sprite está deshabilitado (por ejemplo, por PlanetDestructionController),
        // no necesitamos actualizar el shader
        if (spriteRenderer == null || !spriteRenderer.enabled)
        {
            return;
        }
        
        // Actualizar propiedades del shader en cada frame
        if (materialCreated && damageMaterial != null)
        {
            UpdateShaderProperties();
        }
    }
    
    private void UpdateShaderProperties()
    {
        if (damageMaterial == null) return;
        
        damageMaterial.SetFloat(DamageAmountID, damageAmount);
        damageMaterial.SetFloat(DamageGlowID, damageGlow);
        damageMaterial.SetFloat(ShakeIntensityID, shakeIntensity);
        damageMaterial.SetFloat(CellDensityID, cellDensity);
        damageMaterial.SetFloat(EdgeThicknessID, edgeThickness);
    }
    
    /// <summary>
    /// Establece la cantidad de daño visual (0 = intacto, 1 = totalmente fracturado)
    /// </summary>
    /// <param name="amount">Cantidad de daño de 0 a 1</param>
    public void SetDamage(float amount)
    {
        damageAmount = Mathf.Clamp01(amount);
        
        // Asegurar que el material de daño esté aplicado cuando hay daño
        if (damageAmount > 0.001f && materialCreated && damageMaterial != null)
        {
            // Si el material actual no es el de daño, aplicarlo
            if (spriteRenderer.material != damageMaterial)
            {
                // Guardar textura y color del material actual antes de cambiar
                if (spriteRenderer.material != null)
                {
                    if (spriteRenderer.material.HasProperty("_MainTex"))
                    {
                        damageMaterial.mainTexture = spriteRenderer.material.mainTexture;
                    }
                    if (spriteRenderer.material.HasProperty("_Color"))
                    {
                        damageMaterial.color = spriteRenderer.material.color;
                    }
                }
                spriteRenderer.material = damageMaterial;
            }
        }
    }
    
    /// <summary>
    /// Anima un impacto corto (0.2s) que sube y baja el daño rápidamente
    /// </summary>
    public void AnimateHit()
    {
        StartCoroutine(HitAnimationCoroutine());
    }
    
    /// <summary>
    /// Coroutine para la animación de impacto
    /// </summary>
    private IEnumerator HitAnimationCoroutine()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        float startDamage = damageAmount;
        float peakDamage = Mathf.Min(1f, startDamage + 0.3f); // Sube 0.3 o hasta 1
        
        // Subir al pico (primera mitad)
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration * 0.5f);
            damageAmount = Mathf.Lerp(startDamage, peakDamage, t);
            yield return null;
        }
        
        // Bajar de vuelta (segunda mitad)
        elapsed = 0f;
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration * 0.5f);
            damageAmount = Mathf.Lerp(peakDamage, startDamage, t);
            yield return null;
        }
        
        // Asegurar que volvamos al valor inicial
        damageAmount = startDamage;
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
            if (materialCreated && damageMaterial != null && newSprite != null && newSprite.texture != null)
            {
                damageMaterial.mainTexture = newSprite.texture;
            }
        }
    }
    
    /// <summary>
    /// Sincroniza la textura del material de daño con el material actual del SpriteRenderer
    /// Útil cuando PlanetIdleAnimator u otro componente actualiza el material
    /// </summary>
    public void SyncMaterialTexture()
    {
        if (materialCreated && damageMaterial != null && spriteRenderer != null)
        {
            Material currentMaterial = spriteRenderer.material;
            
            // Si hay un material actual con textura, sincronizarla
            if (currentMaterial != null && currentMaterial.HasProperty("_MainTex"))
            {
                Texture mainTex = currentMaterial.mainTexture;
                if (mainTex != null)
                {
                    damageMaterial.mainTexture = mainTex;
                }
            }
            // Si no, usar la textura del sprite directamente
            else if (spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
            {
                damageMaterial.mainTexture = spriteRenderer.sprite.texture;
            }
        }
    }
    
    /// <summary>
    /// Resetea el daño a 0
    /// </summary>
    public void ResetDamage()
    {
        SetDamage(0f);
    }
    
    private void OnDestroy()
    {
        // Limpiar material creado si fue creado por este componente
        if (materialCreated && damageMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(damageMaterial);
            }
            else
            {
                DestroyImmediate(damageMaterial);
            }
        }
    }
    
    private void OnValidate()
    {
        // Asegurar que los valores estén en rangos válidos
        damageAmount = Mathf.Clamp01(damageAmount);
        damageGlow = Mathf.Clamp(damageGlow, 0f, 2f);
        shakeIntensity = Mathf.Clamp(shakeIntensity, 0f, 0.1f);
        cellDensity = Mathf.Clamp(cellDensity, 5f, 50f);
        edgeThickness = Mathf.Clamp(edgeThickness, 0.001f, 0.1f);
        
        // Actualizar shader en el editor si el material existe
        if (Application.isPlaying && materialCreated && damageMaterial != null)
        {
            UpdateShaderProperties();
        }
    }
}

