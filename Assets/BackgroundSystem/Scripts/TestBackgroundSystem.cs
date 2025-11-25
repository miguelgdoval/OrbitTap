using UnityEngine;

/// <summary>
/// Script de prueba para verificar que el sistema funciona
/// </summary>
public class TestBackgroundSystem : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private float testInterval = 3f; // Cambiar preset cada 3 segundos
    
    private float timer = 0f;
    private int currentTestIndex = 0;
    private string[] testPresets = { "VoidSpace", "BlueDrift", "NebulaStorm", "CosmicWinds", "SupernovaEcho" };
    
    private void Update()
    {
        if (BackgroundManager.Instance == null) return;
        
        timer += Time.deltaTime;
        
        if (timer >= testInterval)
        {
            timer = 0f;
            TestNextPreset();
        }
    }
    
    private void TestNextPreset()
    {
        if (currentTestIndex >= testPresets.Length)
        {
            currentTestIndex = 0;
        }
        
        string presetName = testPresets[currentTestIndex];
        Debug.Log($"🧪 TEST: Cambiando a preset {presetName}");
        BackgroundSystemAPI.SetPreset(presetName, 1f);
        
        currentTestIndex++;
    }
    
    [ContextMenu("Test All Presets")]
    public void TestAllPresets()
    {
        StartCoroutine(TestAllPresetsCoroutine());
    }
    
    private System.Collections.IEnumerator TestAllPresetsCoroutine()
    {
        foreach (string preset in testPresets)
        {
            Debug.Log($"🧪 TEST: Cambiando a {preset}");
            BackgroundSystemAPI.SetPreset(preset, 1f);
            yield return new WaitForSeconds(2f);
        }
    }
}

