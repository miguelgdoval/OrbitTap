using UnityEngine;
using UnityEditor;

/// <summary>
/// Script para asignar automáticamente los prefabs a los presets
/// </summary>
public class AssignPrefabsToPresets : EditorWindow
{
    [MenuItem("Tools/Background System/Assign Prefabs to Presets")]
    public static void ShowWindow()
    {
        GetWindow<AssignPrefabsToPresets>("Assign Prefabs to Presets");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Assign Prefabs to Presets", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("Este script asignará automáticamente los prefabs", EditorStyles.helpBox);
        GUILayout.Label("a los presets correspondientes:", EditorStyles.helpBox);
        GUILayout.Label("", EditorStyles.helpBox);
        GUILayout.Label("• VoidSpace → VoidHorizon.prefab", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• BlueDrift → NebulaDrift.prefab", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• NebulaStorm → CosmicSurge.prefab", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• CosmicWinds → SolarRift.prefab", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• SupernovaEcho → EventHorizon.prefab", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("Asignar Prefabs a Presets", GUILayout.Height(40)))
        {
            AssignAllPrefabs();
        }
    }
    
    private void AssignAllPrefabs()
    {
        // Mapeo de presets a prefabs
        var presetToPrefab = new System.Collections.Generic.Dictionary<string, string>
        {
            { "VoidSpace", "Assets/Prefabs/Backgrounds/VoidHorizon.prefab" },
            { "BlueDrift", "Assets/Prefabs/Backgrounds/NebulaDrift.prefab" },
            { "NebulaStorm", "Assets/Prefabs/Backgrounds/CosmicSurge.prefab" },
            { "CosmicWinds", "Assets/Prefabs/Backgrounds/SolarRift.prefab" },
            { "SupernovaEcho", "Assets/Prefabs/Backgrounds/EventHorizon.prefab" }
        };
        
        string presetsPath = "Assets/BackgroundSystem/Presets/";
        int assignedCount = 0;
        
        foreach (var mapping in presetToPrefab)
        {
            string presetName = mapping.Key;
            string prefabPath = mapping.Value;
            
            // Cargar el preset
            string presetPath = presetsPath + presetName + ".asset";
            BackgroundPreset preset = AssetDatabase.LoadAssetAtPath<BackgroundPreset>(presetPath);
            
            if (preset == null)
            {
                Debug.LogWarning($"⚠️ Preset no encontrado: {presetPath}");
                continue;
            }
            
            // Cargar el prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogWarning($"⚠️ Prefab no encontrado: {prefabPath}");
                continue;
            }
            
            // Asignar el prefab al preset
            preset.backgroundPrefab = prefab;
            EditorUtility.SetDirty(preset);
            
            assignedCount++;
            Debug.Log($"✅ Asignado: {presetName} → {prefab.name}");
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        string message = $"✅ Completado!\n\n";
        message += $"• {assignedCount} presets actualizados\n\n";
        message += "Los prefabs ahora están asignados a los presets.";
        
        EditorUtility.DisplayDialog("Completado", message, "OK");
        
        Debug.Log($"=== ASSIGNMENT COMPLETADO ===");
        Debug.Log($"Presets actualizados: {assignedCount}");
    }
}

