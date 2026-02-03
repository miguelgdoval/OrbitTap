using UnityEngine;

/// <summary>
/// Script de diagnóstico completo para encontrar problemas
/// </summary>
public class BackgroundTroubleshooter : MonoBehaviour
{
    [ContextMenu("Diagnose Background System")]
    public void Diagnose()
    {
        Debug.Log("=== BACKGROUND SYSTEM DIAGNOSIS ===");
        
        // 1. Verificar BackgroundManager
        if (BackgroundManager.Instance == null)
        {
            Debug.LogError("❌ BackgroundManager.Instance es NULL!");
            return;
        }
        
        Debug.Log("✅ BackgroundManager.Instance encontrado");
        
        // 2. Verificar presets
        BackgroundManager bgManager = BackgroundManager.Instance;
        var presetsField = typeof(BackgroundManager).GetField("presets", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (presetsField != null)
        {
            BackgroundPreset[] presets = presetsField.GetValue(bgManager) as BackgroundPreset[];
            Debug.Log($"📋 Presets asignados: {presets?.Length ?? 0}");
            
            if (presets != null)
            {
                for (int i = 0; i < presets.Length; i++)
                {
                    if (presets[i] == null)
                    {
                        Debug.LogWarning($"  ⚠️ Preset {i}: NULL");
                    }
                    else
                    {
                        Debug.Log($"  ✅ Preset {i}: {presets[i].presetName}");
                        Debug.Log($"     - Prefab asignado: {(presets[i].backgroundPrefab != null ? presets[i].backgroundPrefab.name : "NINGUNO")}");
                        Debug.Log($"     - Nebula Speed: {presets[i].nebulaScrollSpeed}");
                        Debug.Log($"     - Stars Far Speed: {presets[i].starsFarScrollSpeed}");
                        Debug.Log($"     - Stars Near Speed: {presets[i].starsNearScrollSpeed}");
                    }
                }
            }
        }
        
        // 3. Verificar preset actual
        string currentPreset = BackgroundSystemAPI.GetCurrentPreset();
        Debug.Log($"🎯 Preset actual: {currentPreset}");
        
        // 4. Verificar capas activas
        var layersField = typeof(BackgroundManager).GetField("currentLayers", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (layersField != null)
        {
            BackgroundLayer[] layers = layersField.GetValue(bgManager) as BackgroundLayer[];
            if (layers != null)
            {
                Debug.Log($"📦 Capas activas: {layers.Length}");
                for (int i = 0; i < layers.Length; i++)
                {
                    if (layers[i] != null)
                    {
                        float speed = layers[i].GetScrollSpeed();
                        bool active = layers[i].gameObject.activeSelf;
                        Debug.Log($"  ✅ Capa {i}: {layers[i].gameObject.name}, Speed={speed}, Active={active}");
                    }
                    else
                    {
                        Debug.Log($"  ❌ Capa {i}: NULL");
                    }
                }
            }
        }
        
        // 5. Buscar todos los BackgroundLayers en la escena
        BackgroundLayer[] allLayers = FindObjectsByType<BackgroundLayer>(FindObjectsSortMode.None);
        Debug.Log($"🔍 BackgroundLayers encontrados en escena: {allLayers.Length}");
        foreach (BackgroundLayer layer in allLayers)
        {
            if (layer != null)
            {
                SpriteRenderer sr = layer.GetComponent<SpriteRenderer>();
                bool hasSprite = sr != null && sr.sprite != null;
                float speed = layer.GetScrollSpeed();
                Debug.Log($"  - {layer.gameObject.name}: Speed={speed}, HasSprite={hasSprite}, Enabled={layer.enabled}");
            }
        }
        
        // 6. Verificar prefabs instanciados
        Transform bgManagerTransform = bgManager.transform;
        int childCount = bgManagerTransform.childCount;
        Debug.Log($"👶 Objetos hijos de BackgroundManager: {childCount}");
        for (int i = 0; i < childCount; i++)
        {
            Transform child = bgManagerTransform.GetChild(i);
            Debug.Log($"  - {child.name} (Children: {child.childCount})");
        }
        
        Debug.Log("=== FIN DIAGNOSIS ===");
    }
    
    [ContextMenu("Test Change Preset")]
    public void TestChangePreset()
    {
        Debug.Log("🧪 Probando cambio de preset a BlueDrift...");
        BackgroundSystemAPI.SetPreset("BlueDrift", 1f);
        Invoke("Diagnose", 1.5f); // Diagnosticar después de 1.5 segundos
    }
    
    [ContextMenu("Force Apply First Preset")]
    public void ForceApplyFirstPreset()
    {
        BackgroundManager bgManager = BackgroundManager.Instance;
        if (bgManager == null) return;
        
        var presetsField = typeof(BackgroundManager).GetField("presets", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (presetsField != null)
        {
            BackgroundPreset[] presets = presetsField.GetValue(bgManager) as BackgroundPreset[];
            if (presets != null && presets.Length > 0 && presets[0] != null)
            {
                Debug.Log($"🔄 Forzando aplicación de preset: {presets[0].presetName}");
                var applyMethod = typeof(BackgroundManager).GetMethod("ApplyPreset", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (applyMethod != null)
                {
                    applyMethod.Invoke(bgManager, new object[] { presets[0], 0f });
                }
            }
        }
        
        Invoke("Diagnose", 0.5f);
    }
}

