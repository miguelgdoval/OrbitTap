using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Generador automático del sistema completo de fondos
/// </summary>
public class BackgroundSystemGenerator : EditorWindow
{
    [MenuItem("Tools/Background System/Generate Complete System")]
    public static void ShowWindow()
    {
        GetWindow<BackgroundSystemGenerator>("Background System Generator");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Background System Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("Este script generará:", EditorStyles.helpBox);
        GUILayout.Label("• 5 Presets de fondo (ScriptableObjects)", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Materiales optimizados para móvil", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Prefabs de capas", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Escena de demo", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("Generar Todo el Sistema", GUILayout.Height(40)))
        {
            GenerateCompleteSystem();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Solo Generar Presets", GUILayout.Height(30)))
        {
            GeneratePresets();
        }
        
        if (GUILayout.Button("Solo Generar Materiales", GUILayout.Height(30)))
        {
            GenerateMaterials();
        }
    }
    
    private void GenerateCompleteSystem()
    {
        GeneratePresets();
        GenerateMaterials();
        GeneratePrefabs();
        GenerateDemoScene();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Completado", "Sistema completo generado exitosamente!", "OK");
    }
    
    private void GeneratePresets()
    {
        string presetsPath = "Assets/BackgroundSystem/Presets";
        if (!AssetDatabase.IsValidFolder(presetsPath))
        {
            AssetDatabase.CreateFolder("Assets/BackgroundSystem", "Presets");
        }
        
        // 1. Void Space
        BackgroundPreset voidSpace = CreatePreset("VoidSpace", "Void Space");
        voidSpace.baseColor = new Color(0.05f, 0.05f, 0.1f, 1f);
        voidSpace.baseOpacity = 1f;
        voidSpace.enableNebulas = false;
        voidSpace.enableStarsFar = true;
        voidSpace.starsFarScrollSpeed = 0.3f;
        voidSpace.starsFarOpacity = 0.4f;
        voidSpace.starsFarDensity = 1;
        voidSpace.enableStarsNear = false;
        voidSpace.enableParticles = false;
        voidSpace.ambientColor = new Color(0.05f, 0.05f, 0.1f, 1f);
        AssetDatabase.CreateAsset(voidSpace, presetsPath + "/VoidSpace.asset");
        
        // 2. Blue Drift
        BackgroundPreset blueDrift = CreatePreset("BlueDrift", "Blue Drift");
        blueDrift.baseColor = new Color(0.1f, 0.15f, 0.3f, 1f);
        blueDrift.baseOpacity = 1f;
        blueDrift.enableNebulas = true;
        blueDrift.nebulaScrollSpeed = 0.2f;
        blueDrift.nebulaOpacity = 0.6f;
        blueDrift.nebulaTint = new Color(0.3f, 0.5f, 1f, 1f);
        blueDrift.enableStarsFar = true;
        blueDrift.starsFarScrollSpeed = 0.5f;
        blueDrift.starsFarOpacity = 0.8f;
        blueDrift.starsFarDensity = 2;
        blueDrift.enableStarsNear = true;
        blueDrift.starsNearScrollSpeed = 1f;
        blueDrift.starsNearOpacity = 1f;
        blueDrift.starsNearDensity = 2;
        blueDrift.enableParticles = true;
        blueDrift.particleScrollSpeed = 1.2f;
        blueDrift.particleOpacity = 0.5f;
        blueDrift.particleDensity = 2;
        blueDrift.ambientColor = new Color(0.1f, 0.15f, 0.3f, 1f);
        AssetDatabase.CreateAsset(blueDrift, presetsPath + "/BlueDrift.asset");
        
        // 3. Nebula Storm
        BackgroundPreset nebulaStorm = CreatePreset("NebulaStorm", "Nebula Storm");
        nebulaStorm.baseColor = new Color(0.15f, 0.1f, 0.25f, 1f);
        nebulaStorm.baseOpacity = 1f;
        nebulaStorm.enableNebulas = true;
        nebulaStorm.nebulaScrollSpeed = 0.4f;
        nebulaStorm.nebulaOpacity = 0.8f;
        nebulaStorm.nebulaTint = new Color(0.8f, 0.4f, 1f, 1f);
        nebulaStorm.enableStarsFar = true;
        nebulaStorm.starsFarScrollSpeed = 0.6f;
        nebulaStorm.starsFarOpacity = 0.9f;
        nebulaStorm.starsFarDensity = 3;
        nebulaStorm.enableStarsNear = true;
        nebulaStorm.starsNearScrollSpeed = 1.2f;
        nebulaStorm.starsNearOpacity = 1f;
        nebulaStorm.starsNearDensity = 3;
        nebulaStorm.enableParticles = true;
        nebulaStorm.particleScrollSpeed = 1.5f;
        nebulaStorm.particleOpacity = 0.7f;
        nebulaStorm.particleDensity = 3;
        nebulaStorm.ambientColor = new Color(0.15f, 0.1f, 0.25f, 1f);
        AssetDatabase.CreateAsset(nebulaStorm, presetsPath + "/NebulaStorm.asset");
        
        // 4. Cosmic Winds
        BackgroundPreset cosmicWinds = CreatePreset("CosmicWinds", "Cosmic Winds");
        cosmicWinds.baseColor = new Color(0.2f, 0.15f, 0.3f, 1f);
        cosmicWinds.baseOpacity = 1f;
        cosmicWinds.enableNebulas = true;
        cosmicWinds.nebulaScrollSpeed = 0.3f;
        cosmicWinds.nebulaOpacity = 0.5f;
        cosmicWinds.nebulaTint = new Color(0.6f, 0.8f, 1f, 1f);
        cosmicWinds.nebulaDirection = BackgroundLayer.ScrollDirection.DiagonalDownRight;
        cosmicWinds.enableStarsFar = true;
        cosmicWinds.starsFarScrollSpeed = 0.7f;
        cosmicWinds.starsFarOpacity = 0.8f;
        cosmicWinds.starsFarDensity = 2;
        cosmicWinds.enableStarsNear = true;
        cosmicWinds.starsNearScrollSpeed = 1.5f;
        cosmicWinds.starsNearOpacity = 1f;
        cosmicWinds.starsNearDensity = 3;
        cosmicWinds.enableParticles = true;
        cosmicWinds.particleScrollSpeed = 2f;
        cosmicWinds.particleOpacity = 0.8f;
        cosmicWinds.particleDensity = 4;
        cosmicWinds.ambientColor = new Color(0.2f, 0.15f, 0.3f, 1f);
        AssetDatabase.CreateAsset(cosmicWinds, presetsPath + "/CosmicWinds.asset");
        
        // 5. Supernova Echo
        BackgroundPreset supernovaEcho = CreatePreset("SupernovaEcho", "Supernova Echo");
        supernovaEcho.baseColor = new Color(0.25f, 0.1f, 0.15f, 1f);
        supernovaEcho.baseOpacity = 1f;
        supernovaEcho.enableNebulas = true;
        supernovaEcho.nebulaScrollSpeed = 0.25f;
        supernovaEcho.nebulaOpacity = 0.7f;
        supernovaEcho.nebulaTint = new Color(1f, 0.5f, 0.3f, 1f);
        supernovaEcho.enableStarsFar = true;
        supernovaEcho.starsFarScrollSpeed = 0.4f;
        supernovaEcho.starsFarOpacity = 0.9f;
        supernovaEcho.starsFarDensity = 3;
        supernovaEcho.enableStarsNear = true;
        supernovaEcho.starsNearScrollSpeed = 1f;
        supernovaEcho.starsNearOpacity = 1f;
        supernovaEcho.starsNearDensity = 3;
        supernovaEcho.enableParticles = true;
        supernovaEcho.particleScrollSpeed = 1.3f;
        supernovaEcho.particleOpacity = 0.9f;
        supernovaEcho.particleDensity = 3;
        supernovaEcho.particlePulsing = true;
        supernovaEcho.particlePulseSpeed = 2f;
        supernovaEcho.ambientColor = new Color(0.25f, 0.1f, 0.15f, 1f);
        AssetDatabase.CreateAsset(supernovaEcho, presetsPath + "/SupernovaEcho.asset");
        
        Debug.Log("✓ 5 Presets generados en Assets/BackgroundSystem/Presets/");
    }
    
    private BackgroundPreset CreatePreset(string name, string displayName)
    {
        BackgroundPreset preset = ScriptableObject.CreateInstance<BackgroundPreset>();
        preset.presetName = displayName;
        preset.description = $"Preset: {displayName}";
        return preset;
    }
    
    private void GenerateMaterials()
    {
        string materialsPath = "Assets/BackgroundSystem/Materials";
        if (!AssetDatabase.IsValidFolder(materialsPath))
        {
            AssetDatabase.CreateFolder("Assets/BackgroundSystem", "Materials");
        }
        
        // Material para nebulosas (soft additive)
        Material nebulaMaterial = new Material(Shader.Find("Sprites/Default"));
        nebulaMaterial.name = "NebulaMaterial";
        nebulaMaterial.SetFloat("_Mode", 2); // Fade mode
        AssetDatabase.CreateAsset(nebulaMaterial, materialsPath + "/NebulaMaterial.mat");
        
        // Material para estrellas (unlit)
        Material starMaterial = new Material(Shader.Find("Sprites/Default"));
        starMaterial.name = "StarMaterial";
        AssetDatabase.CreateAsset(starMaterial, materialsPath + "/StarMaterial.mat");
        
        // Material para partículas (unlit)
        Material particleMaterial = new Material(Shader.Find("Sprites/Default"));
        particleMaterial.name = "ParticleMaterial";
        AssetDatabase.CreateAsset(particleMaterial, materialsPath + "/ParticleMaterial.mat");
        
        Debug.Log("✓ Materiales generados en Assets/BackgroundSystem/Materials/");
    }
    
    private void GeneratePrefabs()
    {
        string prefabsPath = "Assets/BackgroundSystem/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabsPath))
        {
            AssetDatabase.CreateFolder("Assets/BackgroundSystem", "Prefabs");
        }
        
        // Prefab de capa base
        GameObject layerPrefab = new GameObject("BackgroundLayer");
        layerPrefab.AddComponent<SpriteRenderer>();
        layerPrefab.AddComponent<BackgroundLayer>();
        PrefabUtility.SaveAsPrefabAsset(layerPrefab, prefabsPath + "/BackgroundLayer.prefab");
        DestroyImmediate(layerPrefab);
        
        // Prefab de BackgroundManager
        GameObject managerPrefab = new GameObject("BackgroundManager");
        managerPrefab.AddComponent<BackgroundManager>();
        PrefabUtility.SaveAsPrefabAsset(managerPrefab, prefabsPath + "/BackgroundManager.prefab");
        DestroyImmediate(managerPrefab);
        
        Debug.Log("✓ Prefabs generados en Assets/BackgroundSystem/Prefabs/");
    }
    
    private void GenerateDemoScene()
    {
        // Crear nueva escena
        UnityEngine.SceneManagement.Scene newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
        
        // Configurar cámara para móvil vertical (9:16)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.backgroundColor = new Color(0.1f, 0.15f, 0.3f, 1f);
        }
        
        // Crear BackgroundManager
        GameObject bgManager = new GameObject("BackgroundManager");
        BackgroundManager manager = bgManager.AddComponent<BackgroundManager>();
        
        // Cargar presets
        BackgroundPreset[] presets = new BackgroundPreset[5];
        presets[0] = AssetDatabase.LoadAssetAtPath<BackgroundPreset>("Assets/BackgroundSystem/Presets/VoidSpace.asset");
        presets[1] = AssetDatabase.LoadAssetAtPath<BackgroundPreset>("Assets/BackgroundSystem/Presets/BlueDrift.asset");
        presets[2] = AssetDatabase.LoadAssetAtPath<BackgroundPreset>("Assets/BackgroundSystem/Presets/NebulaStorm.asset");
        presets[3] = AssetDatabase.LoadAssetAtPath<BackgroundPreset>("Assets/BackgroundSystem/Presets/CosmicWinds.asset");
        presets[4] = AssetDatabase.LoadAssetAtPath<BackgroundPreset>("Assets/BackgroundSystem/Presets/SupernovaEcho.asset");
        
        // Asignar presets usando reflection
        var presetField = typeof(BackgroundManager).GetField("presets", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (presetField != null)
        {
            presetField.SetValue(manager, presets);
        }
        
        var defaultPresetField = typeof(BackgroundManager).GetField("defaultPresetIndex", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (defaultPresetField != null)
        {
            defaultPresetField.SetValue(manager, 1); // Blue Drift por defecto
        }
        
        // Guardar escena
        string scenePath = "Assets/BackgroundSystem/BackgroundDemoScene.unity";
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);
        
        Debug.Log($"✓ Escena de demo creada: {scenePath}");
    }
}

