using UnityEngine;

/// <summary>
/// Componente que rastrea un obstáculo y lo desregistra del sistema de seguridad cuando se destruye
/// </summary>
public class ObstacleSafetyTracker : MonoBehaviour
{
    private void OnDestroy()
    {
        if (OrbitSafetySystem.Instance != null)
        {
            OrbitSafetySystem.Instance.UnregisterObstacle(gameObject);
        }
    }
}

