using UnityEngine;
using UnityEditor;

/// <summary>
/// Script para corregir los presets y desactivar capas sin sprites
/// </summary>
public class FixBackgroundPresets : EditorWindow
{
    [MenuItem("Tools/Background System/Fix Presets (Disable Empty Layers)")]
    public static void ShowWindow()
    {
        GetWindow<FixBackgroundPresets>("Fix Background Presets");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Fix Background Presets", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("Este script desactivará las capas que no tienen sprites asignados", EditorStyles.helpBox);
        GUILayout.Label("para evitar que los placeholders tapen el juego.", EditorStyles.helpBox);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("Corregir Todos los Presets", GUILayout.Height(40)))
        {
            FixAllPresets();
        }
    }
    
    private void FixAllPresets()
    {
        string[] presetPaths = {
            "Assets/BackgroundSystem/Presets/VoidSpace.asset",
            "Assets/BackgroundSystem/Presets/BlueDrift.asset",
            "Assets/BackgroundSystem/Presets/NebulaStorm.asset",
            "Assets/BackgroundSystem/Presets/CosmicWinds.asset",
            "Assets/BackgroundSystem/Presets/SupernovaEcho.asset"
        };
        
        int fixedCount = 0;
        
        foreach (string path in presetPaths)
        {
            BackgroundPreset preset = AssetDatabase.LoadAssetAtPath<BackgroundPreset>(path);
            if (preset != null)
            {
                bool modified = false;
                
                // Desactivar Base si no tiene sprite
                if (preset.baseSprite == null && preset.enableBase)
                {
                    preset.enableBase = false;
                    modified = true;
                }
                
                // Desactivar Nebulas si no tiene sprite
                if (preset.nebulaSprite == null && preset.enableNebulas)
                {
                    preset.enableNebulas = false;
                    modified = true;
                }
                
                // Desactivar Particles si no tiene sprite
                if (preset.particleSprite == null && preset.enableParticles)
                {
                    preset.enableParticles = false;
                    modified = true;
                }
                
                // Reducir opacidad de estrellas si no tienen sprite
                if (preset.starsFarSprite == null && preset.starsFarOpacity > 0.3f)
                {
                    preset.starsFarOpacity = 0.2f; // Muy sutil
                    modified = true;
                }
                
                if (preset.starsNearSprite == null && preset.starsNearOpacity > 0.3f)
                {
                    preset.starsNearOpacity = 0.2f; // Muy sutil
                    modified = true;
                }
                
                if (modified)
                {
                    EditorUtility.SetDirty(preset);
                    fixedCount++;
                    Debug.Log($"✓ Corregido: {preset.presetName}");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Completado", 
            $"Se corrigieron {fixedCount} presets.\n\nLas capas sin sprites han sido desactivadas.", 
            "OK");
    }
}

