using UnityEngine;
using static LogHelper;

/// <summary>
/// API simple para cambiar fondos desde cualquier script
/// </summary>
public static class BackgroundSystemAPI
{
    /// <summary>
    /// Cambia el preset del fondo
    /// </summary>
    /// <param name="presetName">Nombre del preset (VoidSpace, BlueDrift, NebulaStorm, CosmicWinds, SupernovaEcho)</param>
    /// <param name="transitionDuration">Duración de la transición (0 = cambio inmediato)</param>
    public static void SetPreset(string presetName, float transitionDuration = 1f)
    {
        Log($"🎨 BackgroundSystemAPI: SetPreset llamado - Preset: '{presetName}', Duración: {transitionDuration}s");
        
        if (BackgroundManager.Instance != null)
        {
            Log($"✅ BackgroundSystemAPI: BackgroundManager.Instance encontrado, llamando SetPreset...");
            BackgroundManager.Instance.SetPreset(presetName, transitionDuration);
        }
        else
        {
            LogError("❌ BackgroundSystemAPI: BackgroundManager.Instance is NULL!");
        }
    }
    
    /// <summary>
    /// Obtiene el nombre del preset actual
    /// </summary>
    public static string GetCurrentPreset()
    {
        if (BackgroundManager.Instance != null)
        {
            return BackgroundManager.Instance.GetCurrentPresetName();
        }
        return "";
    }
    
    /// <summary>
    /// Activa o desactiva una capa específica
    /// </summary>
    /// <param name="layerIndex">0=Base, 1=Nebulas, 2=StarsFar, 3=StarsNear, 4=Particles</param>
    /// <param name="enabled">Activar o desactivar</param>
    public static void SetLayerEnabled(int layerIndex, bool enabled)
    {
        if (BackgroundManager.Instance != null)
        {
            BackgroundManager.Instance.SetLayerEnabled(layerIndex, enabled);
        }
    }
}

