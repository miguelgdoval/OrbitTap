using UnityEngine;

/// <summary>
/// Script de debug para probar el sistema de fondos
/// </summary>
public class BackgroundDebug : MonoBehaviour
{
    [Header("Debug Controls")]
    [SerializeField] private KeyCode testPreset1 = KeyCode.Alpha1;
    [SerializeField] private KeyCode testPreset2 = KeyCode.Alpha2;
    [SerializeField] private KeyCode testPreset3 = KeyCode.Alpha3;
    [SerializeField] private KeyCode testPreset4 = KeyCode.Alpha4;
    [SerializeField] private KeyCode testPreset5 = KeyCode.Alpha5;
    
    private void Update()
    {
        if (Input.GetKeyDown(testPreset1))
        {
            Debug.Log("Cambiando a VoidSpace");
            BackgroundSystemAPI.SetPreset("VoidSpace", 1f);
        }
        else if (Input.GetKeyDown(testPreset2))
        {
            Debug.Log("Cambiando a BlueDrift");
            BackgroundSystemAPI.SetPreset("BlueDrift", 1f);
        }
        else if (Input.GetKeyDown(testPreset3))
        {
            Debug.Log("Cambiando a NebulaStorm");
            BackgroundSystemAPI.SetPreset("NebulaStorm", 1f);
        }
        else if (Input.GetKeyDown(testPreset4))
        {
            Debug.Log("Cambiando a CosmicWinds");
            BackgroundSystemAPI.SetPreset("CosmicWinds", 1f);
        }
        else if (Input.GetKeyDown(testPreset5))
        {
            Debug.Log("Cambiando a SupernovaEcho");
            BackgroundSystemAPI.SetPreset("SupernovaEcho", 1f);
        }
    }
    
    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Background Debug Controls", style);
        GUILayout.Label("Presiona 1-5 para cambiar presets", style);
        GUILayout.Label($"Preset actual: {BackgroundSystemAPI.GetCurrentPreset()}", style);
        GUILayout.EndArea();
    }
}

