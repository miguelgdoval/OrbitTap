using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor script para asignar automáticamente los sprites a las capas de los prefabs de fondo
/// </summary>
public class BackgroundSpriteAssigner : EditorWindow
{
    private string[] backgroundNames = { "VoidHorizon", "NebulaDrift", "CosmicSurge", "SolarRift", "EventHorizon" };
    private string[] layerNames = { "BG_Base", "BG_Nebula", "BG_Stars", "BG_Particles" };
    
    [MenuItem("Tools/Assign Background Sprites")]
    public static void ShowWindow()
    {
        GetWindow<BackgroundSpriteAssigner>("Background Sprite Assigner");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Background Sprite Assigner", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("Este script asignará los sprites de Assets/Art/Backgrounds/", EditorStyles.helpBox);
        GUILayout.Label("a las capas correspondientes de los prefabs.", EditorStyles.helpBox);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Asignar Sprites a Todos los Prefabs", GUILayout.Height(30)))
        {
            AssignAllSprites();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Verificar Configuración de Prefabs", GUILayout.Height(30)))
        {
            VerifyPrefabs();
        }
    }
    
    private void AssignAllSprites()
    {
        string prefabPath = "Assets/Prefabs/Backgrounds/";
        string spritePath = "Assets/Art/Backgrounds/";
        
        int assignedCount = 0;
        int errorCount = 0;
        
        for (int i = 0; i < backgroundNames.Length; i++)
        {
            string bgName = backgroundNames[i];
            string prefabFilePath = prefabPath + bgName + ".prefab";
            
            // Cargar el prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
            if (prefab == null)
            {
                Debug.LogWarning($"No se encontró el prefab: {prefabFilePath}");
                errorCount++;
                continue;
            }
            
            // Cargar el sprite principal del fondo
            string spriteFilePath = spritePath + bgName + "/" + bgName + ".png";
            Sprite mainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteFilePath);
            
            if (mainSprite == null)
            {
                // Intentar cargar como Texture2D y convertirlo
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spriteFilePath);
                if (texture != null)
                {
                    // Crear sprite desde texture
                    mainSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
            
            if (mainSprite == null)
            {
                Debug.LogWarning($"No se encontró el sprite: {spriteFilePath}");
                errorCount++;
                continue;
            }
            
            // Abrir el prefab en modo edición
            bool prefabOpened = PrefabUtility.IsPartOfPrefabInstance(prefab) || PrefabUtility.IsPartOfPrefabAsset(prefab);
            
            // Buscar las capas en el prefab
            foreach (string layerName in layerNames)
            {
                Transform layerTransform = prefab.transform.Find(layerName);
                if (layerTransform == null)
                {
                    Debug.LogWarning($"No se encontró la capa {layerName} en {bgName}");
                    continue;
                }
                
                SpriteRenderer sr = layerTransform.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    Debug.LogWarning($"No se encontró SpriteRenderer en {layerName} de {bgName}");
                    continue;
                }
                
                // Asignar el sprite (todos usan el mismo sprite por ahora)
                // En el futuro puedes tener sprites específicos por capa
                sr.sprite = mainSprite;
                
                // Asegurar que el color sea blanco (alpha = 1)
                sr.color = Color.white;
                
                // Asegurar Sorting Order correcto
                int sortingOrder = GetSortingOrder(layerName);
                sr.sortingOrder = sortingOrder;
                
                assignedCount++;
            }
            
            // Guardar el prefab
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"✓ Sprites asignados a {bgName}");
        }
        
        AssetDatabase.Refresh();
        
        string message = $"Asignación completada!\n✓ {assignedCount} sprites asignados";
        if (errorCount > 0)
        {
            message += $"\n⚠ {errorCount} errores";
        }
        
        EditorUtility.DisplayDialog("Completado", message, "OK");
    }
    
    private void VerifyPrefabs()
    {
        string prefabPath = "Assets/Prefabs/Backgrounds/";
        string report = "=== VERIFICACIÓN DE PREFABS ===\n\n";
        
        for (int i = 0; i < backgroundNames.Length; i++)
        {
            string bgName = backgroundNames[i];
            string prefabFilePath = prefabPath + bgName + ".prefab";
            
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
            if (prefab == null)
            {
                report += $"❌ {bgName}: Prefab no encontrado\n";
                continue;
            }
            
            report += $"✓ {bgName}:\n";
            
            bool allLayersOK = true;
            foreach (string layerName in layerNames)
            {
                Transform layerTransform = prefab.transform.Find(layerName);
                if (layerTransform == null)
                {
                    report += $"  ❌ {layerName}: No encontrada\n";
                    allLayersOK = false;
                    continue;
                }
                
                SpriteRenderer sr = layerTransform.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    report += $"  ❌ {layerName}: Sin SpriteRenderer\n";
                    allLayersOK = false;
                    continue;
                }
                
                BackgroundLayer bgLayer = layerTransform.GetComponent<BackgroundLayer>();
                if (bgLayer == null)
                {
                    report += $"  ❌ {layerName}: Sin BackgroundLayer\n";
                    allLayersOK = false;
                    continue;
                }
                
                if (sr.sprite == null)
                {
                    report += $"  ⚠ {layerName}: Sin sprite asignado\n";
                    allLayersOK = false;
                }
                else
                {
                    report += $"  ✓ {layerName}: OK (Sorting Order: {sr.sortingOrder})\n";
                }
            }
            
            if (!allLayersOK)
            {
                report += "  → Usa 'Asignar Sprites' para corregir\n";
            }
            
            report += "\n";
        }
        
        Debug.Log(report);
        EditorUtility.DisplayDialog("Verificación", report, "OK");
    }
    
    private int GetSortingOrder(string layerName)
    {
        switch (layerName)
        {
            case "BG_Base": return -10;
            case "BG_Nebula": return -9;
            case "BG_Stars": return -8;
            case "BG_Particles": return -7;
            default: return 0;
        }
    }
}

