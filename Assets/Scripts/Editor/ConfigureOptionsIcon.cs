#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;

/// <summary>
/// Script de editor para configurar automáticamente el icono de opciones
/// con la mejor calidad posible cuando se escala
/// </summary>
public class ConfigureOptionsIcon : AssetPostprocessor
{
    // Variable estática para controlar si el script debe ejecutarse automáticamente
    private static bool autoConfigureEnabled = false;
    
    private void OnPreprocessTexture()
    {
        // Solo ejecutar si está habilitado y es el icono de opciones
        if (!autoConfigureEnabled || !assetPath.Contains("OptionsIcon"))
        {
            return;
        }
        
        TextureImporter textureImporter = (TextureImporter)assetImporter;
        
        // Solo configurar si no está ya configurado correctamente
        // Esto evita sobrescribir cambios manuales del usuario
        
        // Configurar tipo de sprite si no está configurado
        if (textureImporter.textureType != TextureImporterType.Sprite)
        {
            textureImporter.textureType = TextureImporterType.Sprite;
        }
        
        // Configurar modo de sprite si no está configurado
        if (textureImporter.spriteImportMode != SpriteImportMode.Single)
        {
            textureImporter.spriteImportMode = SpriteImportMode.Single;
        }
        
        // NO sobrescribir filterMode, mipmaps, etc. si el usuario los ha configurado manualmente
        // Solo establecer valores por defecto si no están configurados
        
        // Pixels Per Unit: Solo cambiar si es el valor por defecto (100)
        if (textureImporter.spritePixelsPerUnit == 100)
        {
            textureImporter.spritePixelsPerUnit = 200;
        }
        
        // Max Size: Solo si no está configurado
        if (textureImporter.maxTextureSize < 512)
        {
            textureImporter.maxTextureSize = 4096;
        }
        
        Debug.Log($"OptionsIcon: Auto-configuración aplicada (solo valores por defecto). PPU={textureImporter.spritePixelsPerUnit}");
    }
    
    [MenuItem("Tools/Configure Options Icon Quality")]
    public static void ConfigureIconManually()
    {
        string[] guids = AssetDatabase.FindAssets("OptionsIcon t:Texture2D");
        if (guids.Length == 0)
        {
            Debug.LogWarning("No se encontró OptionsIcon");
            return;
        }
        
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        
        if (textureImporter != null)
        {
            // Configurar para mejor calidad
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            
            // Valores recomendados para mejor calidad al escalar
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.maxTextureSize = 4096;
            textureImporter.mipmapEnabled = false; // Desactivar mipmaps para UI
            textureImporter.anisoLevel = 0;
            textureImporter.spritePixelsPerUnit = 200; // Mayor PPU = mejor calidad al escalar
            textureImporter.isReadable = false;
            
            // Aplicar cambios
            EditorUtility.SetDirty(textureImporter);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            
            Debug.Log($"✅ OptionsIcon configurado para máxima calidad:\n" +
                      $"  - Pixels Per Unit: {textureImporter.spritePixelsPerUnit}\n" +
                      $"  - Filter Mode: {textureImporter.filterMode}\n" +
                      $"  - Mipmaps: {textureImporter.mipmapEnabled}\n" +
                      $"  - Compression: {textureImporter.textureCompression}\n" +
                      $"Path: {path}");
        }
    }
    
    [MenuItem("Tools/Toggle Auto-Configure Options Icon")]
    public static void ToggleAutoConfigure()
    {
        autoConfigureEnabled = !autoConfigureEnabled;
        Debug.Log($"Auto-configuración de OptionsIcon: {(autoConfigureEnabled ? "HABILITADA" : "DESHABILITADA")}");
        Menu.SetChecked("Tools/Toggle Auto-Configure Options Icon", autoConfigureEnabled);
    }
    
    [MenuItem("Tools/Toggle Auto-Configure Options Icon", true)]
    public static bool ToggleAutoConfigureValidate()
    {
        Menu.SetChecked("Tools/Toggle Auto-Configure Options Icon", autoConfigureEnabled);
        return true;
    }
}
#endif

