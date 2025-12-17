using UnityEngine;
using static LogHelper;

/// <summary>
/// Hace que los obstáculos brillen aleatoriamente para darles variación visual
/// </summary>
public class ObstacleGlow : MonoBehaviour
{
    [Header("Glow Settings")]
    public float minGlowIntensity = 0.8f;
    public float maxGlowIntensity = 1.2f;
    public float pulseSpeed = 1f;
    public bool randomizeOnStart = true;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private float glowIntensity;
    private float pulsePhase;
    private float baseIntensity;

    private void Start()
    {
        if (!Application.isPlaying) return;

        // Obtener todos los SpriteRenderers del obstáculo y sus hijos
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        
        if (spriteRenderers.Length == 0)
        {
            LogWarning($"ObstacleGlow: No SpriteRenderers found on {gameObject.name}");
            return;
        }

        // Guardar colores originales
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }

        // Aleatorizar intensidad base y fase inicial
        if (randomizeOnStart)
        {
            baseIntensity = Random.Range(minGlowIntensity, maxGlowIntensity);
            pulsePhase = Random.Range(0f, Mathf.PI * 2f); // Fase aleatoria para que no todos pulsen igual
            pulseSpeed = Random.Range(0.5f, 1.5f); // Velocidad de pulso aleatoria
        }
        else
        {
            baseIntensity = 1f;
            pulsePhase = 0f;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (spriteRenderers == null || spriteRenderers.Length == 0) return;

        // Calcular pulso usando seno para un efecto suave
        pulsePhase += Time.deltaTime * pulseSpeed;
        float pulse = Mathf.Sin(pulsePhase) * 0.15f + 1f; // Oscila entre 0.85 y 1.15
        
        // Aplicar brillo combinando la intensidad base con el pulso
        glowIntensity = baseIntensity * pulse;
        glowIntensity = Mathf.Clamp(glowIntensity, minGlowIntensity, maxGlowIntensity);

        // Aplicar el brillo a todos los sprites
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                Color originalColor = originalColors[i];
                Color glowingColor = new Color(
                    originalColor.r * glowIntensity,
                    originalColor.g * glowIntensity,
                    originalColor.b * glowIntensity,
                    originalColor.a
                );
                spriteRenderers[i].color = glowingColor;
            }
        }
    }

    private void OnDestroy()
    {
        // Restaurar colores originales si es necesario
        if (spriteRenderers != null && originalColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length && i < originalColors.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = originalColors[i];
                }
            }
        }
    }
}

