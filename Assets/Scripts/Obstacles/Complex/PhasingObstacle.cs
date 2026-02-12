using UnityEngine;
using static LogHelper;

/// <summary>
/// Obstáculo que se materializa y desmaterializa cíclicamente.
/// Cuando es intangible, el jugador puede pasar a través de él (sin collider activo).
/// Cuando es sólido, colisiona normalmente.
/// Un efecto visual de "estática/glitch" indica el estado actual.
/// 
/// Dificultad: VeryHard — requiere timing y observación.
/// </summary>
[ObstacleDifficulty(ObstacleDifficultyLevel.VeryHard)]
public class PhasingObstacle : ObstacleBase, IObstacleDifficulty
{
    [Header("Phasing Settings")]
    [Tooltip("Tiempo que el obstáculo permanece sólido (segundos)")]
    public float solidDuration = 1.2f;
    [Tooltip("Tiempo que el obstáculo permanece intangible (segundos)")]
    public float intangibleDuration = 0.8f;
    [Tooltip("Duración de la transición entre estados (segundos)")]
    public float transitionDuration = 0.3f;
    [Tooltip("Offset aleatorio del ciclo (para que no todos estén sincronizados)")]
    public bool randomizePhase = true;
    
    [Header("Visual")]
    [Tooltip("Alpha cuando está completamente sólido")]
    public float solidAlpha = 0.9f;
    [Tooltip("Alpha cuando está completamente intangible")]
    public float intangibleAlpha = 0.15f;
    [Tooltip("Velocidad del efecto de glitch/estática")]
    public float glitchSpeed = 15f;
    [Tooltip("Intensidad del efecto de glitch (desplazamiento en píxeles)")]
    public float glitchIntensity = 0.08f;

    // Estado
    private float phaseTimer = 0f;
    private bool isSolid = true;
    private float currentAlpha = 1f;
    
    // Componentes
    private GameObject fragmentObject;
    private SpriteRenderer fragmentRenderer;
    private Collider2D fragmentCollider;
    private Vector3 baseLocalPosition;

    public ObstacleDifficultyLevel GetDifficulty()
    {
        return ObstacleDifficultyLevel.VeryHard;
    }

    private void Start()
    {
        Log($"PhasingObstacle: Start() called for {gameObject.name} at {transform.position}");
        ConfigureRootRendererAsStar();
        CreateVisual();
        
        // Randomizar fase para que no todos empiecen iguales
        if (randomizePhase)
        {
            phaseTimer = Random.Range(0f, solidDuration + intangibleDuration);
        }
        
        Log($"PhasingObstacle: CreateVisual() completed for {gameObject.name}");
    }

    private void ConfigureRootRendererAsStar()
    {
        SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
        if (rootRenderer == null) return;

        Sprite starSprite = SpriteGenerator.CreateStarSprite(0.35f, CosmicTheme.EtherealLila);

        rootRenderer.sprite = starSprite;
        rootRenderer.color = new Color(CosmicTheme.EtherealLila.r, CosmicTheme.EtherealLila.g, CosmicTheme.EtherealLila.b, 0.85f);
        rootRenderer.sortingOrder = 4;
        rootRenderer.sortingLayerName = "Default";
    }

    private void Update()
    {
        phaseTimer += Time.deltaTime;
        
        float cycleDuration = solidDuration + intangibleDuration;
        float cyclePosition = phaseTimer % cycleDuration;
        
        // Determinar estado actual
        bool wasSolid = isSolid;
        isSolid = cyclePosition < solidDuration;
        
        // Calcular alpha con transición suave
        float targetAlpha;
        if (isSolid)
        {
            // Fase sólida
            float solidProgress = cyclePosition / solidDuration;
            if (solidProgress < transitionDuration / solidDuration)
            {
                // Transición intangible → sólido (fade in)
                float t = solidProgress / (transitionDuration / solidDuration);
                targetAlpha = Mathf.Lerp(intangibleAlpha, solidAlpha, t);
            }
            else if (solidProgress > 1f - (transitionDuration / solidDuration))
            {
                // Transición sólido → intangible (fade out)
                float t = (solidProgress - (1f - transitionDuration / solidDuration)) / (transitionDuration / solidDuration);
                targetAlpha = Mathf.Lerp(solidAlpha, intangibleAlpha, t);
            }
            else
            {
                targetAlpha = solidAlpha;
            }
        }
        else
        {
            // Fase intangible
            targetAlpha = intangibleAlpha;
        }
        
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 10f);
        
        // Actualizar visual
        if (fragmentRenderer != null)
        {
            Color c = fragmentRenderer.color;
            c.a = currentAlpha;
            fragmentRenderer.color = c;
            
            // Efecto de glitch cuando está en transición
            if (fragmentObject != null)
            {
                if (Mathf.Abs(currentAlpha - solidAlpha) > 0.1f && Mathf.Abs(currentAlpha - intangibleAlpha) > 0.1f)
                {
                    // En transición: aplicar desplazamiento aleatorio (glitch)
                    float offsetX = Mathf.Sin(Time.time * glitchSpeed) * glitchIntensity;
                    float offsetY = Mathf.Cos(Time.time * glitchSpeed * 1.3f) * glitchIntensity * 0.5f;
                    fragmentObject.transform.localPosition = baseLocalPosition + new Vector3(offsetX, offsetY, 0f);
                }
                else
                {
                    fragmentObject.transform.localPosition = baseLocalPosition;
                }
            }
        }
        
        // Actualizar collider (solo activo cuando es sólido)
        if (fragmentCollider != null)
        {
            // El collider se activa cuando alpha > umbral (mitad entre sólido e intangible)
            float colliderThreshold = (solidAlpha + intangibleAlpha) / 2f;
            fragmentCollider.enabled = currentAlpha > colliderThreshold;
        }
    }

    private void CreateVisual()
    {
        fragmentObject = new GameObject("PhasingFragment");
        fragmentObject.transform.SetParent(transform);
        fragmentObject.transform.localPosition = Vector3.zero;
        baseLocalPosition = Vector3.zero;
        
        fragmentRenderer = fragmentObject.AddComponent<SpriteRenderer>();
        fragmentRenderer.sprite = CreatePhasingSprite();
        fragmentRenderer.color = new Color(CosmicTheme.EtherealLila.r, CosmicTheme.EtherealLila.g, CosmicTheme.EtherealLila.b, 1f);
        fragmentRenderer.sortingOrder = 5;
        fragmentRenderer.sortingLayerName = "Default";
        
        // Collider
        float spriteWorldSize = fragmentRenderer.sprite.rect.width / fragmentRenderer.sprite.pixelsPerUnit;
        CircleCollider2D collider = fragmentObject.AddComponent<CircleCollider2D>();
        collider.radius = spriteWorldSize * 0.4f;
        collider.isTrigger = true;
        fragmentCollider = collider;
        
        // Detector de colisiones
        fragmentObject.AddComponent<ObstacleCollisionDetector>();
    }

    private Sprite CreatePhasingSprite()
    {
        // Forzar estrella procedural para evitar cualquier cuadrado de recursos.
        return SpriteGenerator.CreateStarSprite(0.4f, CosmicTheme.EtherealLila);
    }
}
