using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script para generar automáticamente los prefabs de backgrounds
/// </summary>
public class BackgroundPrefabGenerator : EditorWindow
{
    private string[] backgroundNames = { "VoidHorizon", "NebulaDrift", "CosmicSurge", "SolarRift", "EventHorizon" };
    private float[] baseScrollSpeeds = { 0.5f, 0.7f, 0.9f, 1.1f, 1.3f };
    private float[] nebulaScrollSpeeds = { 0.3f, 0.4f, 0.5f, 0.6f, 0.7f };
    private float[] starsScrollSpeeds = { 0.8f, 1.0f, 1.2f, 1.4f, 1.6f };
    private float[] particlesScrollSpeeds = { 1.0f, 1.2f, 1.4f, 1.6f, 1.8f };
    
    [MenuItem("Tools/Generate Background Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<BackgroundPrefabGenerator>("Background Prefab Generator");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Background Prefab Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Generate All Background Prefabs", GUILayout.Height(30)))
        {
            GenerateAllPrefabs();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("This will create prefabs in Assets/Prefabs/Backgrounds/", EditorStyles.helpBox);
    }
    
    private void GenerateAllPrefabs()
    {
        string prefabPath = "Assets/Prefabs/Backgrounds/";
        
        // Asegurar que la carpeta existe
        if (!AssetDatabase.IsValidFolder(prefabPath))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Backgrounds");
        }
        
        for (int i = 0; i < backgroundNames.Length; i++)
        {
            CreateBackgroundPrefab(backgroundNames[i], prefabPath, 
                baseScrollSpeeds[i], 
                nebulaScrollSpeeds[i], 
                starsScrollSpeeds[i], 
                particlesScrollSpeeds[i]);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("All background prefabs generated successfully!");
        EditorUtility.DisplayDialog("Success", "All background prefabs have been generated!", "OK");
    }
    
    private void CreateBackgroundPrefab(string bgName, string path, float baseSpeed, float nebulaSpeed, float starsSpeed, float particlesSpeed)
    {
        // Crear GameObject raíz
        GameObject bgRoot = new GameObject(bgName);
        
        // Crear capas
        GameObject baseLayer = CreateLayer(bgRoot, "BG_Base", baseSpeed);
        GameObject nebulaLayer = CreateLayer(bgRoot, "BG_Nebula", nebulaSpeed);
        GameObject starsLayer = CreateLayer(bgRoot, "BG_Stars", starsSpeed);
        GameObject particlesLayer = CreateLayer(bgRoot, "BG_Particles", particlesSpeed);
        
        // Crear sprites dummy si no tienen sprite
        CreateDummySprite(baseLayer, Color.black);
        CreateDummySprite(nebulaLayer, new Color(0.2f, 0.1f, 0.3f, 0.8f));
        CreateDummySprite(starsLayer, Color.white);
        CreateDummySprite(particlesLayer, new Color(1f, 0.8f, 0.2f, 0.6f));
        
        // Guardar como prefab
        string prefabFilePath = path + bgName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(bgRoot, prefabFilePath);
        
        // Limpiar
        DestroyImmediate(bgRoot);
        
        Debug.Log($"Created prefab: {prefabFilePath}");
    }
    
    private GameObject CreateLayer(GameObject parent, string layerName, float scrollSpeed)
    {
        GameObject layer = new GameObject(layerName);
        layer.transform.SetParent(parent.transform);
        layer.transform.localPosition = Vector3.zero;
        
        // Agregar SpriteRenderer
        SpriteRenderer sr = layer.AddComponent<SpriteRenderer>();
        sr.sortingOrder = GetSortingOrder(layerName);
        
        // Agregar BackgroundLayer
        BackgroundLayer bgLayer = layer.AddComponent<BackgroundLayer>();
        
        // Establecer scrollSpeed usando SerializedObject
        SerializedObject so = new SerializedObject(bgLayer);
        SerializedProperty scrollSpeedProp = so.FindProperty("scrollSpeed");
        if (scrollSpeedProp != null)
        {
            scrollSpeedProp.floatValue = scrollSpeed;
            so.ApplyModifiedProperties();
        }
        
        return layer;
    }
    
    private int GetSortingOrder(string layerName)
    {
        switch (layerName)
        {
            case "BG_Base": return -10;
            case "BG_Nebula": return -9;
            case "BG_Stars": return -8;
            case "BG_Particles": return -7;
            default: return -10;
        }
    }
    
    private void CreateDummySprite(GameObject obj, Color color)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
        {
            // Crear un sprite dummy de 1920x1080 (Full HD)
            Texture2D texture = new Texture2D(1920, 1080);
            Color[] pixels = new Color[1920 * 1080];
            
            // Rellenar con el color base
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            // Agregar algún patrón básico según el tipo
            if (obj.name.Contains("Stars"))
            {
                // Agregar "estrellas" aleatorias
                for (int i = 0; i < 500; i++)
                {
                    int x = Random.Range(0, 1920);
                    int y = Random.Range(0, 1080);
                    pixels[y * 1920 + x] = Color.white;
                }
            }
            else if (obj.name.Contains("Nebula"))
            {
                // Agregar gradiente
                for (int y = 0; y < 1080; y++)
                {
                    float t = (float)y / 1080f;
                    Color gradColor = Color.Lerp(color, color * 1.5f, Mathf.Sin(t * Mathf.PI));
                    for (int x = 0; x < 1920; x++)
                    {
                        pixels[y * 1920 + x] = gradColor;
                    }
                }
            }
            else if (obj.name.Contains("Particles"))
            {
                // Agregar partículas pequeñas
                for (int i = 0; i < 200; i++)
                {
                    int x = Random.Range(0, 1920);
                    int y = Random.Range(0, 1080);
                    int size = Random.Range(2, 5);
                    for (int dx = -size; dx <= size; dx++)
                    {
                        for (int dy = -size; dy <= size; dy++)
                        {
                            int px = x + dx;
                            int py = y + dy;
                            if (px >= 0 && px < 1920 && py >= 0 && py < 1080)
                            {
                                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                                if (dist <= size)
                                {
                                    pixels[py * 1920 + px] = Color.Lerp(pixels[py * 1920 + px], color, 0.5f);
                                }
                            }
                        }
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1920, 1080), new Vector2(0.5f, 0.5f), 100f);
            sr.sprite = sprite;
            sr.color = Color.white;
        }
    }
}

