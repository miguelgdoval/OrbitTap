using UnityEngine;
using static LogHelper;

/// <summary>
/// Script de diagnóstico para verificar el estado del sistema de fondos
/// </summary>
public class BackgroundDiagnostics : MonoBehaviour
{
    private void Start()
    {
        InvokeRepeating("LogDiagnostics", 2f, 5f); // Log cada 5 segundos
    }
    
    private void LogDiagnostics()
    {
        if (BackgroundManager.Instance == null)
        {
            LogError("❌ BackgroundManager.Instance es NULL!");
            return;
        }
        
        Log($"=== BACKGROUND SYSTEM DIAGNOSTICS ===");
        Log($"Preset actual: {BackgroundSystemAPI.GetCurrentPreset()}");
        
        // Verificar capas
        BackgroundManager bgManager = BackgroundManager.Instance;
        var layersField = typeof(BackgroundManager).GetField("currentLayers", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (layersField != null)
        {
            BackgroundLayer[] layers = layersField.GetValue(bgManager) as BackgroundLayer[];
            if (layers != null)
            {
                for (int i = 0; i < layers.Length; i++)
                {
                    if (layers[i] != null)
                    {
                        float speed = layers[i].GetScrollSpeed();
                        Log($"  Capa {i}: Activa, Scroll Speed: {speed}");
                    }
                    else
                    {
                        Log($"  Capa {i}: NULL (no creada)");
                    }
                }
            }
        }
        
        // Verificar presets
        var presetsField = typeof(BackgroundManager).GetField("presets", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (presetsField != null)
        {
            BackgroundPreset[] presets = presetsField.GetValue(bgManager) as BackgroundPreset[];
            Log($"Presets asignados: {presets?.Length ?? 0}");
        }
    }
    
    [ContextMenu("Test Preset Change")]
    public void TestPresetChange()
    {
        Log("🧪 Probando cambio de preset...");
        BackgroundSystemAPI.SetPreset("BlueDrift", 1f);
    }
}

