using UnityEngine;

/// <summary>
/// Rota el sprite del planeta lentamente sobre su eje para simular rotación de superficie.
/// La rotación es independiente del movimiento orbital.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlanetSurface : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Velocidad de rotación del planeta en grados por segundo (10-40 recomendado)")]
    [Range(0.0f, 100.0f)]
    public float rotationSpeed = 20f;
    
    private float currentRotation = 0f;
    private Transform spriteTransform;

    private void Start()
    {
        // Crear un objeto hijo para el sprite si no existe, o usar el transform actual
        // Esto permite rotar solo el sprite sin afectar la posición/orbita
        spriteTransform = transform;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        
        // Rotar solo el sprite sobre su eje Z (no afecta la posición)
        currentRotation += rotationSpeed * Time.deltaTime;
        if (currentRotation >= 360f)
        {
            currentRotation -= 360f;
        }
        
        // Rotar solo el componente visual, manteniendo la posición
        spriteTransform.rotation = Quaternion.Euler(0, 0, currentRotation);
    }
}

