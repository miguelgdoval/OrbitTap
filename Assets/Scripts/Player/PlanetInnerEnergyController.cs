using UnityEngine;
using System.Collections;
using static LogHelper;

/// <summary>
/// Sistema de energía interna para planetas.
/// Aplica un shader procedural que simula un núcleo energético interno
/// que pulsa y brilla hacia fuera. Compatible con PlanetIdleAnimator, 
/// PlanetDestructionController y PlanetDamageEffect.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlanetInnerEnergyController : MonoBehaviour
{
    [Header("Energy Settings")]
    [Tooltip("Nivel de energía actual (0 = sin energía, 1 = máxima energía)")]
    [Range(0f, 1f)]
    public float energyLevel = 0f;
    
    [Tooltip("Velocidad de pulsación")]
    [Range(0f, 5f)]
    public float pulseSpeed = 1f;
    
    [Tooltip("Intensidad del brillo interno")]
    [Range(0f, 2f)]
    public float glowIntensity = 1f;
    
    [Tooltip("Cantidad de distorsión térmica")]
    [Range(0f, 0.1f)]
    public float distortionAmount = 0.02f;
    
    [Tooltip("Color de la energía")]
    public Color energyColor = new Color(0.2f, 0.8f, 1.0f, 1.0f);
    
    [Tooltip("Escala del ruido procedural")]
    [Range(1f, 20f)]
    public float noiseScale = 5f;
    
    private SpriteRenderer spriteRenderer;
    private Material energyMaterial;
    private Material originalMaterial;
    private bool materialCreated = false;
    
    // Referencias a otros componentes para compatibilidad
    private PlanetIdleAnimator idleAnimator;
    private PlanetDestructionController destructionController;
    private PlanetDamageEffect damageEffect;
    
    // Estado de overcharge
    private bool isOvercharging = false;
    private Coroutine overchargeCoroutine;
    
    // IDs de propiedades del shader (cached para evitar GC)
    private static readonly int EnergyLevelID = Shader.PropertyToID("_EnergyLevel");
    private static readonly int PulseSpeedID = Shader.PropertyToID("_PulseSpeed");
    private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");
    private static readonly int DistortionAmountID = Shader.PropertyToID("_DistortionAmount");
    private static readonly int EnergyColorID = Shader.PropertyToID("_EnergyColor");
    private static readonly int NoiseScaleID = Shader.PropertyToID("_NoiseScale");
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            LogWarning($"PlanetInnerEnergyController: No SpriteRenderer found on {gameObject.name}. Component disabled.");
            enabled = false;
            return;
        }
        
        // Guardar material original
        originalMaterial = spriteRenderer.sharedMaterial;
        
        // Obtener referencias a otros componentes
        idleAnimator = GetComponent<PlanetIdleAnimator>();
        destructionController = GetComponent<PlanetDestructionController>();
        damageEffect = GetComponent<PlanetDamageEffect>();
        
        // Configurar el material de energía
        SetupEnergyMaterial();
    }
    
    private void SetupEnergyMaterial()
    {
        // Intentar cargar el shader
        Shader energyShader = Shader.Find("Custom/PlanetInnerEnergyShader");
        
        if (energyShader == null)
        {
            LogWarning("PlanetInnerEnergyController: Shader 'Custom/PlanetInnerEnergyShader' not found. Energy effects will not work.");
            return;
        }
        
        // Guardar el material actual (puede ser de otros componentes)
        Material currentMaterial = spriteRenderer.sharedMaterial;
        
        // Crear material si no existe o si el shader cambió
        if (currentMaterial == null || currentMaterial.shader != energyShader)
        {
            // Crear nuevo material
            energyMaterial = new Material(energyShader);
            energyMaterial.name = "PlanetEnergyMaterial";
            
            // Copiar propiedades del material actual o del sprite
            if (currentMaterial != null && currentMaterial.HasProperty("_MainTex"))
            {
                energyMaterial.mainTexture = currentMaterial.mainTexture;
            }
            else if (spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
            {
                energyMaterial.mainTexture = spriteRenderer.sprite.texture;
            }
            
            // Copiar el color del sprite
            energyMaterial.color = spriteRenderer.color;
            
            // Aplicar el material
            spriteRenderer.material = energyMaterial;
            materialCreated = true;
            
            // Inicializar valores del shader
            UpdateShaderProperties();
        }
        else
        {
            energyMaterial = spriteRenderer.material;
        }
    }
    
    private void Update()
    {
        // Si el sprite está deshabilitado, no actualizar
        if (spriteRenderer == null || !spriteRenderer.enabled)
        {
            return;
        }
        
        // Actualizar propiedades del shader en cada frame
        if (materialCreated && energyMaterial != null)
        {
            UpdateShaderProperties();
        }
    }
    
    private void UpdateShaderProperties()
    {
        if (energyMaterial == null) return;
        
        energyMaterial.SetFloat(EnergyLevelID, energyLevel);
        energyMaterial.SetFloat(PulseSpeedID, pulseSpeed);
        energyMaterial.SetFloat(GlowIntensityID, glowIntensity);
        energyMaterial.SetFloat(DistortionAmountID, distortionAmount);
        energyMaterial.SetColor(EnergyColorID, energyColor);
        energyMaterial.SetFloat(NoiseScaleID, noiseScale);
    }
    
    /// <summary>
    /// Establece el nivel de energía (0 = sin energía, 1 = máxima energía)
    /// </summary>
    /// <param name="value">Nivel de energía de 0 a 1</param>
    public void SetEnergyLevel(float value)
    {
        energyLevel = Mathf.Clamp01(value);
        
        // Asegurar que el material de energía esté aplicado cuando hay energía
        if (energyLevel > 0.001f && materialCreated && energyMaterial != null)
        {
            if (spriteRenderer.material != energyMaterial)
            {
                // Guardar textura y color del material actual antes de cambiar
                if (spriteRenderer.material != null)
                {
                    if (spriteRenderer.material.HasProperty("_MainTex"))
                    {
                        energyMaterial.mainTexture = spriteRenderer.material.mainTexture;
                    }
                    if (spriteRenderer.material.HasProperty("_Color"))
                    {
                        energyMaterial.color = spriteRenderer.material.color;
                    }
                }
                spriteRenderer.material = energyMaterial;
            }
        }
    }
    
    /// <summary>
    /// Ejecuta un pulso único de energía (sube y baja rápidamente)
    /// </summary>
    public void PulseOnce()
    {
        StartCoroutine(PulseOnceCoroutine());
    }
    
    /// <summary>
    /// Coroutine para el pulso único
    /// </summary>
    private IEnumerator PulseOnceCoroutine()
    {
        float startEnergy = energyLevel;
        float peakEnergy = Mathf.Min(1f, startEnergy + 0.4f);
        float duration = 0.5f;
        float elapsed = 0f;
        
        // Subir al pico (primera mitad)
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration * 0.5f);
            energyLevel = Mathf.Lerp(startEnergy, peakEnergy, t);
            yield return null;
        }
        
        // Bajar de vuelta (segunda mitad)
        elapsed = 0f;
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration * 0.5f);
            energyLevel = Mathf.Lerp(peakEnergy, startEnergy, t);
            yield return null;
        }
        
        // Asegurar que volvamos al valor inicial
        energyLevel = startEnergy;
    }
    
    /// <summary>
    /// Sobrecarga el planeta: energía sube a 1 y vibra durante la duración especificada
    /// </summary>
    /// <param name="duration">Duración de la sobrecarga en segundos</param>
    public void Overcharge(float duration)
    {
        // Si ya hay una sobrecarga en curso, cancelarla
        if (isOvercharging && overchargeCoroutine != null)
        {
            StopCoroutine(overchargeCoroutine);
        }
        
        overchargeCoroutine = StartCoroutine(OverchargeCoroutine(duration));
    }
    
    /// <summary>
    /// Coroutine para la sobrecarga
    /// </summary>
    private IEnumerator OverchargeCoroutine(float duration)
    {
        isOvercharging = true;
        float startEnergy = energyLevel;
        float elapsed = 0f;
        
        // Subir energía a 1.0 rápidamente
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            energyLevel = Mathf.Lerp(startEnergy, 1f, elapsed / 0.2f);
            yield return null;
        }
        
        energyLevel = 1f;
        elapsed = 0f;
        
        // Mantener en sobrecarga con vibración
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Vibración: oscilar entre 0.9 y 1.0
            float vibration = Mathf.Sin(elapsed * 20f) * 0.05f;
            energyLevel = Mathf.Clamp01(1f + vibration);
            yield return null;
        }
        
        // Bajar energía de vuelta
        float finalEnergy = startEnergy;
        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            energyLevel = Mathf.Lerp(1f, finalEnergy, elapsed / 0.3f);
            yield return null;
        }
        
        energyLevel = finalEnergy;
        isOvercharging = false;
        overchargeCoroutine = null;
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
            if (materialCreated && energyMaterial != null && newSprite != null && newSprite.texture != null)
            {
                energyMaterial.mainTexture = newSprite.texture;
            }
        }
    }
    
    /// <summary>
    /// Resetea la energía a 0
    /// </summary>
    public void ResetEnergy()
    {
        SetEnergyLevel(0f);
    }
    
    /// <summary>
    /// Sincroniza la textura del material de energía con el material actual del SpriteRenderer
    /// Útil cuando PlanetIdleAnimator u otro componente actualiza el material
    /// </summary>
    public void SyncMaterialTexture()
    {
        if (materialCreated && energyMaterial != null && spriteRenderer != null)
        {
            Material currentMaterial = spriteRenderer.material;
            
            // Si hay un material actual con textura, sincronizarla
            if (currentMaterial != null && currentMaterial.HasProperty("_MainTex"))
            {
                Texture mainTex = currentMaterial.mainTexture;
                if (mainTex != null)
                {
                    energyMaterial.mainTexture = mainTex;
                }
            }
            // Si no, usar la textura del sprite directamente
            else if (spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
            {
                energyMaterial.mainTexture = spriteRenderer.sprite.texture;
            }
        }
    }
    
    private void OnDestroy()
    {
        // Limpiar material creado si fue creado por este componente
        if (materialCreated && energyMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(energyMaterial);
            }
            else
            {
                DestroyImmediate(energyMaterial);
            }
        }
    }
    
    private void OnValidate()
    {
        // Asegurar que los valores estén en rangos válidos
        energyLevel = Mathf.Clamp01(energyLevel);
        pulseSpeed = Mathf.Clamp(pulseSpeed, 0f, 5f);
        glowIntensity = Mathf.Clamp(glowIntensity, 0f, 2f);
        distortionAmount = Mathf.Clamp(distortionAmount, 0f, 0.1f);
        noiseScale = Mathf.Clamp(noiseScale, 1f, 20f);
        
        // Actualizar shader en el editor si el material existe
        if (Application.isPlaying && materialCreated && energyMaterial != null)
        {
            UpdateShaderProperties();
        }
    }
}

