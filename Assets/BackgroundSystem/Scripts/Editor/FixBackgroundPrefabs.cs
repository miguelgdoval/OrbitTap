using UnityEngine;
using UnityEditor;

/// <summary>
/// Script para agregar automáticamente BackgroundLayer a todos los prefabs de fondo
/// </summary>
public class FixBackgroundPrefabs : EditorWindow
{
    [MenuItem("Tools/Background System/Fix Prefabs (Add BackgroundLayer)")]
    public static void ShowWindow()
    {
        GetWindow<FixBackgroundPrefabs>("Fix Background Prefabs");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Fix Background Prefabs", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("Este script agregará automáticamente el componente", EditorStyles.helpBox);
        GUILayout.Label("BackgroundLayer a todos los objetos hijos de tus prefabs.", EditorStyles.helpBox);
        GUILayout.Label("", EditorStyles.helpBox);
        GUILayout.Label("Prefabs a modificar:", EditorStyles.helpBox);
        GUILayout.Label("• VoidHorizon.prefab", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• NebulaDrift.prefab", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• CosmicSurge.prefab", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• SolarRift.prefab", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• EventHorizon.prefab", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("Agregar BackgroundLayer a Todos los Prefabs", GUILayout.Height(40)))
        {
            FixAllPrefabs();
        }
    }
    
    private void FixAllPrefabs()
    {
        string[] prefabPaths = {
            "Assets/Prefabs/Backgrounds/VoidHorizon.prefab",
            "Assets/Prefabs/Backgrounds/NebulaDrift.prefab",
            "Assets/Prefabs/Backgrounds/CosmicSurge.prefab",
            "Assets/Prefabs/Backgrounds/SolarRift.prefab",
            "Assets/Prefabs/Backgrounds/EventHorizon.prefab"
        };
        
        int fixedCount = 0;
        int totalLayersFixed = 0;
        
        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"⚠️ Prefab no encontrado: {prefabPath}");
                continue;
            }
            
            // Abrir el prefab en modo edición
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            bool modified = false;
            int layersAdded = 0;
            
            Debug.Log($"\n🔧 Procesando prefab: {prefab.name}");
            
            // Buscar todos los objetos hijos que tengan SpriteRenderer
            SpriteRenderer[] allRenderers = prefabRoot.GetComponentsInChildren<SpriteRenderer>(true);
            Debug.Log($"  Encontrados {allRenderers.Length} SpriteRenderers");
            
            if (allRenderers.Length == 0)
            {
                Debug.LogWarning($"  ⚠️ No se encontraron SpriteRenderers en {prefab.name}!");
            }
            
            foreach (SpriteRenderer sr in allRenderers)
            {
                if (sr != null)
                {
                    GameObject obj = sr.gameObject;
                    BackgroundLayer existingLayer = obj.GetComponent<BackgroundLayer>();
                    
                    if (existingLayer == null)
                    {
                        // Agregar BackgroundLayer
                        BackgroundLayer newLayer = obj.AddComponent<BackgroundLayer>();
                        
                        // Configurar valores por defecto según el nombre usando SerializedObject
                        string objName = obj.name.ToLower();
                        SerializedObject so = new SerializedObject(newLayer);
                        
                        if (objName.Contains("base"))
                        {
                            // Base no se mueve
                            so.FindProperty("scrollSpeed").floatValue = 0f;
                            Debug.Log($"    ✅ Agregado BackgroundLayer a {obj.name} (Base, Speed=0)");
                        }
                        else if (objName.Contains("nebula"))
                        {
                            so.FindProperty("scrollSpeed").floatValue = 0.2f;
                            Debug.Log($"    ✅ Agregado BackgroundLayer a {obj.name} (Nebula, Speed=0.2)");
                        }
                        else if (objName.Contains("star"))
                        {
                            so.FindProperty("scrollSpeed").floatValue = 0.5f;
                            so.FindProperty("spriteDensity").intValue = 2;
                            so.FindProperty("infiniteScroll").boolValue = true;
                            Debug.Log($"    ✅ Agregado BackgroundLayer a {obj.name} (Stars, Speed=0.5)");
                        }
                        else if (objName.Contains("particle"))
                        {
                            so.FindProperty("scrollSpeed").floatValue = 1.0f;
                            so.FindProperty("spriteDensity").intValue = 2;
                            so.FindProperty("infiniteScroll").boolValue = true;
                            Debug.Log($"    ✅ Agregado BackgroundLayer a {obj.name} (Particles, Speed=1.0)");
                        }
                        else
                        {
                            // Por defecto
                            so.FindProperty("scrollSpeed").floatValue = 0.5f;
                            Debug.Log($"    ✅ Agregado BackgroundLayer a {obj.name} (Default, Speed=0.5)");
                        }
                        
                        // Desactivar UV scrolling para usar Transform (más confiable)
                        so.FindProperty("useUVScrolling").boolValue = false;
                        so.ApplyModifiedProperties();
                        
                        layersAdded++;
                        modified = true;
                    }
                    else
                    {
                        Debug.Log($"    ✓ Ya tiene BackgroundLayer: {obj.name}");
                    }
                }
            }
            
            if (modified)
            {
                // Guardar el prefab
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                fixedCount++;
                totalLayersFixed += layersAdded;
                Debug.Log($"✅ Prefab corregido: {prefab.name} ({layersAdded} capas agregadas)");
            }
            else
            {
                Debug.Log($"✓ Prefab ya estaba correcto: {prefab.name}");
            }
            
            // Limpiar
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        string message = $"✅ Completado!\n\n";
        message += $"• {fixedCount} prefabs modificados\n";
        message += $"• {totalLayersFixed} componentes BackgroundLayer agregados\n\n";
        message += "Los prefabs ahora tienen BackgroundLayer en todos sus objetos hijos.";
        
        EditorUtility.DisplayDialog("Completado", message, "OK");
        
        Debug.Log($"=== FIX COMPLETADO ===");
        Debug.Log($"Prefabs modificados: {fixedCount}");
        Debug.Log($"BackgroundLayers agregados: {totalLayersFixed}");
    }
}

