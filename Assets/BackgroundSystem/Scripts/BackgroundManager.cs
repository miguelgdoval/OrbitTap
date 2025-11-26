using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manager principal del sistema de fondos dinámicos.
/// Controla presets, transiciones y todas las capas.
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }
    
    [Header("Layer Containers")]
    [SerializeField] private Transform layerBase;
    [SerializeField] private Transform layerNebulas;
    [SerializeField] private Transform layerStarsFar;
    [SerializeField] private Transform layerStarsNear;
    [SerializeField] private Transform layerParticles;
    
    [Header("Presets")]
    public BackgroundPreset[] presets;
    [SerializeField] private int defaultPresetIndex = 0;
    
    [Header("Transition Settings")]
    [SerializeField] private float defaultTransitionDuration = 1f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Current State")]
    [SerializeField] private string currentPresetName = "";
    [SerializeField] private BackgroundPreset currentPreset;
    
    private Dictionary<string, BackgroundPreset> presetDictionary;
    private BackgroundLayer[] currentLayers = new BackgroundLayer[5];
    private BackgroundLayer[] oldLayers = new BackgroundLayer[5]; // Para transiciones
    private GameObject currentPrefabInstance; // Instancia actual del prefab
    private GameObject oldPrefabInstance; // Instancia anterior para transición
    private Coroutine transitionCoroutine;
    
    // Prefabs para instanciar capas
    private GameObject layerPrefab;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializePresets();
        CreateLayerContainers();
    }
    
    private void Start()
    {
        // Cargar preset por defecto solo si no hay uno ya aplicado
        // Esto previene duplicados si Start() se llama múltiples veces
        if (currentPreset == null && presets != null && presets.Length > 0)
        {
            int index = Mathf.Clamp(defaultPresetIndex, 0, presets.Length - 1);
            ApplyPreset(presets[index], 0f); // Sin transición al inicio
        }
    }
    
    private void InitializePresets()
    {
        presetDictionary = new Dictionary<string, BackgroundPreset>();
        
        if (presets != null)
        {
            Debug.Log($"📋 BackgroundManager: Inicializando {presets.Length} presets...");
            foreach (BackgroundPreset preset in presets)
            {
                if (preset != null)
                {
                    string key = preset.presetName.ToLower();
                    presetDictionary[key] = preset;
                    Debug.Log($"  ✅ Preset registrado: '{preset.presetName}' (key: '{key}')");
                }
                else
                {
                    Debug.LogWarning("  ⚠️ Preset NULL encontrado en array");
                }
            }
            Debug.Log($"✅ BackgroundManager: Diccionario inicializado con {presetDictionary.Count} presets");
        }
        else
        {
            Debug.LogError("❌ BackgroundManager: Array de presets es NULL!");
        }
    }
    
    private void CreateLayerContainers()
    {
        // Crear contenedores si no existen
        if (layerBase == null)
        {
            GameObject go = new GameObject("Layer_Base");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            layerBase = go.transform;
        }
        
        if (layerNebulas == null)
        {
            GameObject go = new GameObject("Layer_Nebulas");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            layerNebulas = go.transform;
        }
        
        if (layerStarsFar == null)
        {
            GameObject go = new GameObject("Layer_StarsFar");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            layerStarsFar = go.transform;
        }
        
        if (layerStarsNear == null)
        {
            GameObject go = new GameObject("Layer_StarsNear");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            layerStarsNear = go.transform;
        }
        
        if (layerParticles == null)
        {
            GameObject go = new GameObject("Layer_Particles");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            layerParticles = go.transform;
        }
        
        // Configurar Sorting Orders (muy negativos para estar detrás de todo)
        SetSortingOrder(layerBase, -20);
        SetSortingOrder(layerNebulas, -19);
        SetSortingOrder(layerStarsFar, -18);
        SetSortingOrder(layerStarsNear, -17);
        SetSortingOrder(layerParticles, -16);
    }
    
    private void SetSortingOrder(Transform layer, int order)
    {
        SpriteRenderer[] renderers = layer.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            sr.sortingOrder = order;
        }
    }
    
    /// <summary>
    /// Cambia el preset del fondo
    /// </summary>
    public void SetPreset(string presetName, float transitionDuration = -1f)
    {
        Debug.Log($"🎯 BackgroundManager.SetPreset: Llamado con '{presetName}', duration={transitionDuration}");
        
        if (string.IsNullOrEmpty(presetName))
        {
            Debug.LogWarning("❌ BackgroundManager: Preset name is null or empty");
            return;
        }
        
        string presetKey = presetName.ToLower().Trim();
        Debug.Log($"🔍 BackgroundManager: Buscando preset '{presetKey}' en diccionario (tamaño: {presetDictionary?.Count ?? 0})");
        
        // Intentar también sin espacios por si acaso
        string presetKeyNoSpaces = presetKey.Replace(" ", "");
        
        if (!presetDictionary.ContainsKey(presetKey) && !presetDictionary.ContainsKey(presetKeyNoSpaces))
        {
            Debug.LogError($"❌ BackgroundManager: Preset '{presetName}' (key: '{presetKey}' o '{presetKeyNoSpaces}') not found in dictionary!");
            Debug.LogError($"   Presets disponibles: {string.Join(", ", presetDictionary.Keys)}");
            return;
        }
        
        // Usar la key que existe
        if (!presetDictionary.ContainsKey(presetKey))
        {
            presetKey = presetKeyNoSpaces;
            Debug.Log($"   Usando key alternativa: '{presetKey}'");
        }
        
        BackgroundPreset preset = presetDictionary[presetKey];
        Debug.Log($"✅ BackgroundManager: Preset encontrado: '{preset.presetName}'");
        
        if (transitionDuration < 0f)
        {
            transitionDuration = defaultTransitionDuration;
            Debug.Log($"   Usando duración por defecto: {transitionDuration}s");
        }
        
        // Verificar si es el mismo preset
        if (currentPreset != null && currentPreset.presetName == preset.presetName)
        {
            Debug.Log($"⚠️ BackgroundManager: Ya está activo el preset '{preset.presetName}', ignorando cambio");
            return;
        }
        
        if (transitionDuration > 0f && currentPreset != null)
        {
            // Transición suave
            Debug.Log($"🔄 BackgroundManager: Iniciando transición de '{currentPreset.presetName}' a '{preset.presetName}' ({transitionDuration}s)");
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(TransitionToPreset(preset, transitionDuration));
        }
        else
        {
            // Cambio inmediato
            Debug.Log($"⚡ BackgroundManager: Cambio inmediato a '{preset.presetName}' (sin transición)");
            ApplyPreset(preset, 0f);
        }
    }
    
    private IEnumerator TransitionToPreset(BackgroundPreset newPreset, float duration)
    {
        Debug.Log($"🔄 BackgroundManager: Iniciando transición a '{newPreset.presetName}' (duración: {duration}s)");
        
        // Guardar capas antiguas antes de limpiar
        for (int i = 0; i < currentLayers.Length; i++)
        {
            oldLayers[i] = currentLayers[i];
        }
        
        // Guardar instancia antigua del prefab
        oldPrefabInstance = currentPrefabInstance;
        
        // Guardar opacidades actuales
        float[] oldOpacities = new float[5];
        for (int i = 0; i < oldLayers.Length; i++)
        {
            if (oldLayers[i] != null)
            {
                oldOpacities[i] = oldLayers[i].GetOpacity();
            }
            else
            {
                oldOpacities[i] = 0f;
            }
        }
        
        // Aplicar nuevo preset (invisible inicialmente)
        ApplyPresetForTransition(newPreset);
        
        // Esperar un frame para que se creen las nuevas capas
        yield return null;
        
        // Guardar opacidades objetivo ANTES de ponerlas a 0
        // Si hay prefab, usar valores del prefab; si no, usar valores del preset
        float[] targetOpacities = new float[5];
        for (int i = 0; i < currentLayers.Length; i++)
        {
            if (currentLayers[i] != null)
            {
                if (newPreset.backgroundPrefab != null)
                {
                    // Guardar el valor del prefab antes de modificarlo
                    targetOpacities[i] = currentLayers[i].GetOpacity();
                }
                else
                {
                    // Usar valores del preset
                    switch(i)
                    {
                        case 0: targetOpacities[i] = newPreset.baseOpacity; break;
                        case 1: targetOpacities[i] = newPreset.nebulaOpacity; break;
                        case 2: targetOpacities[i] = newPreset.starsFarOpacity; break;
                        case 3: targetOpacities[i] = newPreset.starsNearOpacity; break;
                        case 4: targetOpacities[i] = newPreset.particleOpacity; break;
                    }
                }
                // Ahora sí, poner a 0 para el fade in
                currentLayers[i].SetOpacity(0f);
            }
            else
            {
                targetOpacities[i] = 0f;
            }
        }
        
        // Fade out antiguas y fade in nuevas simultáneamente
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / duration);
            
            // Fade out de capas antiguas
            for (int i = 0; i < oldLayers.Length; i++)
            {
                if (oldLayers[i] != null)
                {
                    float targetOpacity = Mathf.Lerp(oldOpacities[i], 0f, t);
                    oldLayers[i].SetOpacity(targetOpacity);
                }
            }
            
            // Fade in de capas nuevas usando los valores objetivo guardados
            for (int i = 0; i < currentLayers.Length; i++)
            {
                if (currentLayers[i] != null)
                {
                    currentLayers[i].SetOpacity(Mathf.Lerp(0f, targetOpacities[i], t));
                }
            }
            
            yield return null;
        }
        
        // Asegurar valores finales usando los valores objetivo guardados
        for (int i = 0; i < currentLayers.Length; i++)
        {
            if (currentLayers[i] != null)
            {
                currentLayers[i].SetOpacity(targetOpacities[i]);
            }
        }
        
        // Destruir capas antiguas después de la transición
        if (oldPrefabInstance != null)
        {
            Destroy(oldPrefabInstance);
            oldPrefabInstance = null;
        }
        
        // Limpiar referencias antiguas
        for (int i = 0; i < oldLayers.Length; i++)
        {
            oldLayers[i] = null;
        }
        
        Debug.Log($"✅ BackgroundManager: Transición completada a '{newPreset.presetName}'");
    }
    
    /// <summary>
    /// Aplica un preset sin limpiar las capas anteriores (para transiciones)
    /// </summary>
    private void ApplyPresetForTransition(BackgroundPreset preset)
    {
        if (preset == null)
        {
            Debug.LogError("BackgroundManager: Cannot apply null preset!");
            return;
        }
        
        currentPreset = preset;
        currentPresetName = preset.presetName;
        
        // NO limpiar capas anteriores aquí (se hará después de la transición)
        // Solo crear las nuevas
        
        // Si el preset tiene un prefab asignado, usarlo directamente
        if (preset.backgroundPrefab != null)
        {
            ApplyPrefabPresetForTransition(preset);
            return;
        }
        
        // Modo dinámico (no usado con prefabs, pero por si acaso)
        // ... código para modo dinámico si es necesario
    }
    
    /// <summary>
    /// Aplica un preset usando prefab sin destruir el anterior (para transiciones)
    /// </summary>
    private void ApplyPrefabPresetForTransition(BackgroundPreset preset)
    {
        if (preset.backgroundPrefab == null)
        {
            Debug.LogError($"❌ BackgroundManager: Preset '{preset.presetName}' tiene backgroundPrefab NULL!");
            return;
        }
        
        // Instanciar el nuevo prefab
        GameObject prefabInstance = Instantiate(preset.backgroundPrefab, transform);
        prefabInstance.name = preset.presetName + "_New";
        currentPrefabInstance = prefabInstance;
        
        // Configurar Sorting Orders de las capas del prefab
        BackgroundLayer[] layers = prefabInstance.GetComponentsInChildren<BackgroundLayer>(true);
        Debug.Log($"🔍 BackgroundManager: Encontradas {layers.Length} capas en nuevo prefab '{preset.backgroundPrefab.name}'");
        
        if (layers.Length == 0)
        {
            Debug.LogError($"❌ BackgroundManager: El prefab '{preset.backgroundPrefab.name}' NO tiene componentes BackgroundLayer!");
            return;
        }
        
        // Limpiar referencias actuales (pero no destruir objetos)
        for (int i = 0; i < currentLayers.Length; i++)
        {
            currentLayers[i] = null;
        }
        
        int layerIndex = 0;
        foreach (BackgroundLayer layer in layers)
        {
            if (layer != null)
            {
                // Activar la capa
                layer.gameObject.SetActive(true);
                
                SpriteRenderer sr = layer.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // Solo ajustar Sorting Order para estar detrás de todo
                    sr.sortingOrder = -20 + layerIndex;
                }
                
                // Identificar el tipo de capa (pero RESPETAR valores del prefab)
                string layerName = layer.gameObject.name.ToLower();
                
                // Desactivar UV scrolling
                var useUVField = typeof(BackgroundLayer).GetField("useUVScrolling", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (useUVField != null)
                {
                    useUVField.SetValue(layer, false);
                }
                
                // Guardar referencias de capas (las opacidades se manejarán en la transición)
                if (layerName.Contains("base"))
                {
                    currentLayers[0] = layer;
                }
                else if (layerName.Contains("nebula"))
                {
                    currentLayers[1] = layer;
                }
                else if (layerName.Contains("star"))
                {
                    if (layerIndex == 2 || layerName.Contains("far"))
                    {
                        currentLayers[2] = layer;
                    }
                    else
                    {
                        currentLayers[3] = layer;
                    }
                }
                else if (layerName.Contains("particle"))
                {
                    currentLayers[4] = layer;
                }
                
                layer.enabled = true;
                layer.gameObject.SetActive(true);
                
                layerIndex++;
            }
        }
        
        // Aplicar color ambiente
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = preset.ambientColor;
        }
        
        Debug.Log($"✅ BackgroundManager: Nuevo preset '{preset.presetName}' preparado para transición - Valores del prefab respetados");
    }
    
    
    private void ApplyPreset(BackgroundPreset preset, float transitionTime)
    {
        if (preset == null)
        {
            Debug.LogError("BackgroundManager: Cannot apply null preset!");
            return;
        }
        
        currentPreset = preset;
        currentPresetName = preset.presetName;
        
        // Limpiar capas anteriores
        ClearLayers();
        
        // Si el preset tiene un prefab asignado, usarlo directamente
        if (preset.backgroundPrefab != null)
        {
            ApplyPrefabPreset(preset);
            return;
        }
        
        // Modo dinámico: crear capas desde configuración
        // Aplicar Layer 0: Base
        if (preset.enableBase)
        {
            CreateBaseLayer(preset);
        }
        
        // Aplicar Layer 1: Nebulas
        if (preset.enableNebulas)
        {
            CreateNebulaLayer(preset);
        }
        
        // Aplicar Layer 2: Stars Far
        if (preset.enableStarsFar)
        {
            CreateStarsFarLayer(preset);
        }
        
        // Aplicar Layer 3: Stars Near
        if (preset.enableStarsNear)
        {
            CreateStarsNearLayer(preset);
        }
        
        // Aplicar Layer 4: Particles
        if (preset.enableParticles)
        {
            CreateParticleLayer(preset);
        }
        
        // Aplicar color ambiente
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = preset.ambientColor;
        }
        
        Debug.Log($"✅ BackgroundManager: Applied preset '{preset.presetName}'");
        Debug.Log($"   - Base: {preset.enableBase}, Nebulas: {preset.enableNebulas}, StarsFar: {preset.enableStarsFar}, StarsNear: {preset.enableStarsNear}, Particles: {preset.enableParticles}");
    }
    
    /// <summary>
    /// Aplica un preset usando un prefab directamente
    /// RESPETA los valores del prefab (opacity, scrollSpeed, etc.) y NO los sobrescribe con el preset
    /// </summary>
    private void ApplyPrefabPreset(BackgroundPreset preset)
    {
        if (preset.backgroundPrefab == null)
        {
            Debug.LogError($"❌ BackgroundManager: Preset '{preset.presetName}' tiene backgroundPrefab NULL!");
            return;
        }
        
        // Instanciar el prefab
        GameObject prefabInstance = Instantiate(preset.backgroundPrefab, transform);
        prefabInstance.name = preset.presetName;
        currentPrefabInstance = prefabInstance;
        
        // Configurar Sorting Orders de las capas del prefab
        BackgroundLayer[] layers = prefabInstance.GetComponentsInChildren<BackgroundLayer>(true); // Incluir inactivos
        Debug.Log($"🔍 BackgroundManager: Encontradas {layers.Length} capas en prefab '{preset.backgroundPrefab.name}'");
        
        if (layers.Length == 0)
        {
            Debug.LogError($"❌ BackgroundManager: El prefab '{preset.backgroundPrefab.name}' NO tiene componentes BackgroundLayer!");
            Debug.LogError("   → Necesitas agregar BackgroundLayer a cada objeto hijo (BG_Base, BG_Nebula, BG_Stars, BG_Particles)");
            return;
        }
        
        int layerIndex = 0;
        foreach (BackgroundLayer layer in layers)
        {
            if (layer != null)
            {
                // Activar la capa
                layer.gameObject.SetActive(true);
                
                SpriteRenderer sr = layer.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // Solo ajustar Sorting Order para estar detrás de todo (necesario)
                    sr.sortingOrder = -20 + layerIndex;
                    Debug.Log($"  ✅ Capa {layerIndex}: {layer.gameObject.name}, Sprite: {(sr.sprite != null ? sr.sprite.name : "NULL")}, SortingOrder: {sr.sortingOrder}");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ Capa {layerIndex}: {layer.gameObject.name} NO tiene SpriteRenderer!");
                }
                
                // Identificar el tipo de capa para guardar referencia (pero NO modificar valores)
                string layerName = layer.gameObject.name.ToLower();
                
                // Desactivar UV scrolling para usar Transform (más confiable con prefabs)
                var useUVField = typeof(BackgroundLayer).GetField("useUVScrolling", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (useUVField != null)
                {
                    useUVField.SetValue(layer, false);
                }
                
                // Guardar referencias de capas (para transiciones) pero RESPETAR valores del prefab
                if (layerName.Contains("base"))
                {
                    currentLayers[0] = layer;
                    Debug.Log($"    → Identificada como Base (usando valores del prefab: opacity={layer.GetOpacity()}, speed={layer.GetScrollSpeed()})");
                }
                else if (layerName.Contains("nebula"))
                {
                    currentLayers[1] = layer;
                    Debug.Log($"    → Identificada como Nebula (usando valores del prefab: opacity={layer.GetOpacity()}, speed={layer.GetScrollSpeed()})");
                }
                else if (layerName.Contains("star"))
                {
                    // Determinar si es far o near por el nombre o posición
                    if (layerIndex == 2 || layerName.Contains("far"))
                    {
                        currentLayers[2] = layer;
                        Debug.Log($"    → Identificada como StarsFar (usando valores del prefab: opacity={layer.GetOpacity()}, speed={layer.GetScrollSpeed()})");
                    }
                    else
                    {
                        currentLayers[3] = layer;
                        Debug.Log($"    → Identificada como StarsNear (usando valores del prefab: opacity={layer.GetOpacity()}, speed={layer.GetScrollSpeed()})");
                    }
                }
                else if (layerName.Contains("particle"))
                {
                    currentLayers[4] = layer;
                    Debug.Log($"    → Identificada como Particles (usando valores del prefab: opacity={layer.GetOpacity()}, speed={layer.GetScrollSpeed()})");
                }
                else
                {
                    Debug.LogWarning($"    ⚠️ Capa no reconocida: {layerName} (usando valores del prefab)");
                }
                
                // Asegurar que la capa esté activa y habilitada
                layer.enabled = true;
                layer.gameObject.SetActive(true);
                
                layerIndex++;
            }
        }
        
        // Aplicar color ambiente de la cámara (útil para el fondo general)
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = preset.ambientColor;
        }
        
        Debug.Log($"✅ BackgroundManager: Applied prefab '{preset.presetName}' - Valores del prefab respetados (NO sobrescritos por preset)");
    }
    
    private void CreateBaseLayer(BackgroundPreset preset)
    {
        // La capa base solo se crea si hay un sprite real asignado
        // Si no hay sprite, solo usamos el color de la cámara
        if (preset.baseSprite == null)
        {
            // No crear sprite, solo usar el color de la cámara que ya se configuró
            currentLayers[0] = null;
            return;
        }
        
        GameObject layerObj = new GameObject("Base");
        layerObj.transform.SetParent(layerBase);
        layerObj.transform.localPosition = Vector3.zero;
        
        SpriteRenderer sr = layerObj.AddComponent<SpriteRenderer>();
        sr.sprite = preset.baseSprite;
        sr.color = preset.baseColor;
        sr.sortingOrder = -20; // Muy atrás
        
        BackgroundLayer layer = layerObj.AddComponent<BackgroundLayer>();
        layer.SetScrollSpeed(0f); // Base no se mueve
        layer.SetOpacity(preset.baseOpacity);
        
        currentLayers[0] = layer;
    }
    
    private void CreateNebulaLayer(BackgroundPreset preset)
    {
        // Solo crear si hay sprite o si queremos un placeholder muy transparente
        if (preset.nebulaSprite == null)
        {
            // No crear nebulosa sin sprite real
            currentLayers[1] = null;
            return;
        }
        
        GameObject layerObj = new GameObject("Nebulas");
        layerObj.transform.SetParent(layerNebulas);
        layerObj.transform.localPosition = Vector3.zero;
        
        SpriteRenderer sr = layerObj.AddComponent<SpriteRenderer>();
        sr.sprite = preset.nebulaSprite;
        sr.color = preset.nebulaTint;
        sr.sortingOrder = -19; // Muy atrás
        
        BackgroundLayer layer = layerObj.AddComponent<BackgroundLayer>();
        layer.SetScrollSpeed(preset.nebulaScrollSpeed);
        layer.SetParallaxMultiplier(0.8f); // Nebulas se mueven lento
        layer.SetOpacity(preset.nebulaOpacity);
        
        // Configurar dirección
        layer.Direction = preset.nebulaDirection;
        
        currentLayers[1] = layer;
    }
    
    private void CreateStarsFarLayer(BackgroundPreset preset)
    {
        // Crear estrellas lejanas solo si hay sprite, si no crear un placeholder muy sutil
        GameObject layerObj = new GameObject("StarsFar");
        layerObj.transform.SetParent(layerStarsFar);
        layerObj.transform.localPosition = Vector3.zero;
        
        SpriteRenderer sr = layerObj.AddComponent<SpriteRenderer>();
        if (preset.starsFarSprite != null)
        {
            sr.sprite = preset.starsFarSprite;
        }
        else
        {
            // Crear placeholder muy sutil (pocos puntos blancos)
            sr.sprite = CreateSubtleStarsSprite();
        }
        sr.color = Color.white;
        sr.sortingOrder = -18; // Muy atrás
        
        BackgroundLayer layer = layerObj.AddComponent<BackgroundLayer>();
        
        // Configurar densidad PRIMERO usando reflection (antes de Start)
        var densityField = typeof(BackgroundLayer).GetField("spriteDensity", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (densityField != null)
        {
            densityField.SetValue(layer, preset.starsFarDensity);
        }
        
        var infiniteScrollField = typeof(BackgroundLayer).GetField("infiniteScroll", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (infiniteScrollField != null)
        {
            infiniteScrollField.SetValue(layer, true);
        }
        
        // Configurar el resto
        layer.SetScrollSpeed(preset.starsFarScrollSpeed);
        layer.SetParallaxMultiplier(preset.starsFarParallax);
        layer.SetOpacity(preset.starsFarOpacity * 0.3f); // Reducir opacidad si es placeholder
        
        Debug.Log($"  ✅ StarsFar creada: Speed={preset.starsFarScrollSpeed}, Density={preset.starsFarDensity}");
        
        // Forzar reconfiguración del scroll infinito después de un frame
        if (Application.isPlaying)
        {
            StartCoroutine(RefreshLayerAfterFrame(layer));
        }
        
        currentLayers[2] = layer;
    }
    
    private void CreateStarsNearLayer(BackgroundPreset preset)
    {
        // Crear estrellas cercanas solo si hay sprite, si no crear un placeholder muy sutil
        GameObject layerObj = new GameObject("StarsNear");
        layerObj.transform.SetParent(layerStarsNear);
        layerObj.transform.localPosition = Vector3.zero;
        
        SpriteRenderer sr = layerObj.AddComponent<SpriteRenderer>();
        if (preset.starsNearSprite != null)
        {
            sr.sprite = preset.starsNearSprite;
        }
        else
        {
            // Crear placeholder muy sutil
            sr.sprite = CreateSubtleStarsSprite();
        }
        sr.color = Color.white;
        sr.sortingOrder = -17; // Muy atrás
        
        BackgroundLayer layer = layerObj.AddComponent<BackgroundLayer>();
        
        // Configurar densidad PRIMERO usando reflection (antes de Start)
        var densityField = typeof(BackgroundLayer).GetField("spriteDensity", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (densityField != null)
        {
            densityField.SetValue(layer, preset.starsNearDensity);
        }
        
        var infiniteScrollField = typeof(BackgroundLayer).GetField("infiniteScroll", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (infiniteScrollField != null)
        {
            infiniteScrollField.SetValue(layer, true);
        }
        
        // Configurar el resto
        layer.SetScrollSpeed(preset.starsNearScrollSpeed);
        layer.SetParallaxMultiplier(preset.starsNearParallax);
        layer.SetOpacity(preset.starsNearOpacity * 0.3f); // Reducir opacidad si es placeholder
        
        Debug.Log($"  ✅ StarsNear creada: Speed={preset.starsNearScrollSpeed}, Density={preset.starsNearDensity}, Opacity={preset.starsNearOpacity * 0.3f}");
        
        // Forzar reconfiguración del scroll infinito después de un frame
        if (Application.isPlaying)
        {
            StartCoroutine(RefreshLayerAfterFrame(layer));
        }
        
        currentLayers[3] = layer;
    }
    
    private void CreateParticleLayer(BackgroundPreset preset)
    {
        // Solo crear partículas si hay sprite real
        if (preset.particleSprite == null)
        {
            currentLayers[4] = null;
            return;
        }
        
        GameObject layerObj = new GameObject("Particles");
        layerObj.transform.SetParent(layerParticles);
        layerObj.transform.localPosition = Vector3.zero;
        
        SpriteRenderer sr = layerObj.AddComponent<SpriteRenderer>();
        sr.sprite = preset.particleSprite;
        sr.color = Color.yellow;
        sr.sortingOrder = -16; // Muy atrás
        
        BackgroundLayer layer = layerObj.AddComponent<BackgroundLayer>();
        layer.SetScrollSpeed(preset.particleScrollSpeed);
        layer.SetParallaxMultiplier(1.5f); // Partículas más rápidas
        layer.SetOpacity(preset.particleOpacity);
        layer.SetPulsing(preset.particlePulsing, preset.particlePulseSpeed);
        
        // Configurar densidad
        layer.SpriteDensity = preset.particleDensity;
        layer.InfiniteScroll = true;
        
        currentLayers[4] = layer;
    }
    
    private void ClearLayers()
    {
        // IMPORTANTE: Destruir instancias del prefab completo primero
        // Esto previene duplicados cuando se aplica un nuevo preset
        if (currentPrefabInstance != null)
        {
            if (Application.isPlaying)
                Destroy(currentPrefabInstance);
            else
                DestroyImmediate(currentPrefabInstance);
            currentPrefabInstance = null;
        }
        
        // También limpiar la instancia antigua si existe
        if (oldPrefabInstance != null)
        {
            if (Application.isPlaying)
                Destroy(oldPrefabInstance);
            else
                DestroyImmediate(oldPrefabInstance);
            oldPrefabInstance = null;
        }
        
        // Destruir capas anteriores de los contenedores
        foreach (Transform child in layerBase)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        
        foreach (Transform child in layerNebulas)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        
        foreach (Transform child in layerStarsFar)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        
        foreach (Transform child in layerStarsNear)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        
        foreach (Transform child in layerParticles)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        
        // Limpiar referencias
        for (int i = 0; i < currentLayers.Length; i++)
        {
            currentLayers[i] = null;
        }
    }
    
    private Sprite CreatePlaceholderSprite(Color color)
    {
        Texture2D texture = new Texture2D(256, 256);
        Color[] pixels = new Color[256 * 256];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f), 100f);
    }
    
    /// <summary>
    /// Crea un sprite sutil de estrellas (pocos puntos blancos sobre fondo transparente)
    /// </summary>
    private Sprite CreateSubtleStarsSprite()
    {
        Texture2D texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[512 * 512];
        
        // Rellenar con transparente
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // Agregar algunas "estrellas" (puntos blancos pequeños)
        System.Random rand = new System.Random();
        for (int i = 0; i < 100; i++) // Solo 100 estrellas
        {
            int x = rand.Next(0, 512);
            int y = rand.Next(0, 512);
            pixels[y * 512 + x] = Color.white;
            
            // Agregar algunos píxeles alrededor para hacerlas más visibles
            if (x > 0) pixels[y * 512 + (x - 1)] = new Color(1f, 1f, 1f, 0.5f);
            if (x < 511) pixels[y * 512 + (x + 1)] = new Color(1f, 1f, 1f, 0.5f);
            if (y > 0) pixels[(y - 1) * 512 + x] = new Color(1f, 1f, 1f, 0.5f);
            if (y < 511) pixels[(y + 1) * 512 + x] = new Color(1f, 1f, 1f, 0.5f);
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f), 100f);
    }
    
    /// <summary>
    /// Obtiene el nombre del preset actual
    /// </summary>
    public string GetCurrentPresetName()
    {
        return currentPresetName;
    }
    
    /// <summary>
    /// Activa o desactiva una capa específica
    /// </summary>
    public void SetLayerEnabled(int layerIndex, bool enabled)
    {
        if (layerIndex >= 0 && layerIndex < currentLayers.Length)
        {
            if (currentLayers[layerIndex] != null)
            {
                currentLayers[layerIndex].gameObject.SetActive(enabled);
            }
        }
    }
    
    /// <summary>
    /// Coroutine para refrescar una capa después de un frame (para que se configure correctamente)
    /// </summary>
    private IEnumerator RefreshLayerAfterFrame(BackgroundLayer layer)
    {
        yield return null; // Esperar un frame
        if (layer != null)
        {
            layer.RefreshInfiniteScroll();
        }
    }
}

