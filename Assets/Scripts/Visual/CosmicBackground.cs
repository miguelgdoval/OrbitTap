using UnityEngine;

/// <summary>
/// Configura el fondo cósmico del juego
/// </summary>
public class CosmicBackground : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        if (!Application.isPlaying) return;

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        if (mainCamera != null)
        {
            // Configurar color de fondo cósmico
            mainCamera.backgroundColor = CosmicTheme.DarkBlue;
        }

        // Configurar iluminación ambiental
        RenderSettings.ambientSkyColor = CosmicTheme.NightBlue;
        RenderSettings.ambientIntensity = 0.3f;
    }
}

