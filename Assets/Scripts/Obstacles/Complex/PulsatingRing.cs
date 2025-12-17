using UnityEngine;
using static LogHelper;

/// <summary>
/// Anillo que pulsa (expande y contrae) creando ventanas de oportunidad para el jugador
/// </summary>
[ObstacleDifficulty(ObstacleDifficultyLevel.Hard)]
public class PulsatingRing : ObstacleBase, IObstacleDifficulty
{
    [Header("Pulsation Settings")]
    public float minRadius = 0.5f;
    public float maxRadius = 1.5f;
    public float pulseSpeed = 2f;
    public int ringSegments = 32;
    
    private float currentRadius;
    private float pulseTime = 0f;
    private GameObject ringObject;
    private CircleCollider2D ringCollider;
    private SpriteRenderer ringRenderer;

    public ObstacleDifficultyLevel GetDifficulty()
    {
        return ObstacleDifficultyLevel.Hard;
    }

    private void Start()
    {
        Log($"PulsatingRing: Start() called for {gameObject.name} at {transform.position}");
        CreateRing();
        currentRadius = minRadius;
        Log($"PulsatingRing: CreateRing() completed for {gameObject.name}");
    }

    private void Update()
    {
        // Pulsar el anillo
        pulseTime += Time.deltaTime * pulseSpeed;
        float normalizedPulse = (Mathf.Sin(pulseTime) + 1f) / 2f; // 0 a 1
        currentRadius = Mathf.Lerp(minRadius, maxRadius, normalizedPulse);
        
        if (ringObject != null)
        {
            ringObject.transform.localScale = Vector3.one * currentRadius;
            
            // Actualizar el collider para que coincida con el tamaño actual
            if (ringCollider != null)
            {
                // El collider se escala automáticamente con el transform, pero ajustamos el radio base
                float baseRadius = 0.5f; // Radio base del sprite
                ringCollider.radius = baseRadius * currentRadius;
            }
        }
    }

    private void CreateRing()
    {
        ringObject = new GameObject("Ring");
        ringObject.transform.SetParent(transform);
        ringObject.transform.localPosition = Vector3.zero;
        ringObject.transform.localScale = Vector3.one * minRadius;
        
        // Crear sprite de anillo
        ringRenderer = ringObject.AddComponent<SpriteRenderer>();
        ringRenderer.sprite = CreateRingSprite();
        ringRenderer.color = Color.white;
        ringRenderer.sortingOrder = 5;
        ringRenderer.sortingLayerName = "Default";
        
        // Crear collider circular que se ajustará al pulso
        ringCollider = ringObject.AddComponent<CircleCollider2D>();
        float baseRadius = 0.5f; // Radio base del sprite (ajustar según el sprite)
        ringCollider.radius = baseRadius * minRadius;
        ringCollider.isTrigger = true;
        
        // Agregar detector de colisiones
        ringObject.AddComponent<ObstacleCollisionDetector>();
    }

    private Sprite CreateRingSprite()
    {
        // Intentar cargar el sprite de estrella de neutrinos
        Sprite starSprite = LoadObstacleSprite("NeutrineStarObstacle");
        if (starSprite != null)
        {
            return starSprite;
        }
        
        // Fallback: crear anillo programáticamente si no se encuentra la imagen
        int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] colors = new Color[textureSize * textureSize];

        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float outerRadius = textureSize / 2f - 4f;
        float innerRadius = outerRadius * 0.6f; // Anillo (no círculo sólido)

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                
                // Crear anillo (entre innerRadius y outerRadius)
                if (dist >= innerRadius && dist <= outerRadius)
                {
                    float alpha = 1f;
                    
                    // Gradiente suave en los bordes
                    if (dist < innerRadius + 3f)
                    {
                        alpha = Mathf.Clamp01((dist - innerRadius) / 3f);
                    }
                    else if (dist > outerRadius - 3f)
                    {
                        alpha = Mathf.Clamp01((outerRadius - dist) / 3f);
                    }
                    
                    // Trazo dorado en los bordes exteriores
                    if (dist > outerRadius - 4f)
                    {
                        Color edgeColor = Color.Lerp(
                            Color.Lerp(CosmicTheme.ConstellationBlue, CosmicTheme.EtherealLila, 0.4f),
                            CosmicTheme.SoftGold,
                            0.7f
                        );
                        colors[y * textureSize + x] = new Color(edgeColor.r, edgeColor.g, edgeColor.b, alpha * 0.8f);
                    }
                    else
                    {
                        Color ringColor = Color.Lerp(CosmicTheme.ConstellationBlue, CosmicTheme.EtherealLila, 0.4f);
                        colors[y * textureSize + x] = new Color(ringColor.r, ringColor.g, ringColor.b, alpha * 0.6f);
                    }
                }
                else
                {
                    colors[y * textureSize + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), 200f);
    }
}

