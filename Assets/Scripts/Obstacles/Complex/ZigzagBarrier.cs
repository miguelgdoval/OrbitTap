using UnityEngine;

/// <summary>
/// Barrera que se mueve en zigzag perpendicular a su dirección de movimiento
/// </summary>
[ObstacleDifficulty(ObstacleDifficultyLevel.Hard)]
public class ZigzagBarrier : ObstacleBase, IObstacleDifficulty
{
    [Header("Zigzag Settings")]
    public float zigzagAmplitude = 1.5f; // Amplitud del movimiento en zigzag
    public float zigzagFrequency = 2f; // Frecuencia del zigzag
    public float barrierWidth = 0.2f;
    public float barrierLength = 2f;
    
    private GameObject barrierObject;
    private Vector3 basePosition;
    private float zigzagTime = 0f;
    private Vector2 perpendicularDirection;
    private ObstacleMover mover;

    public ObstacleDifficultyLevel GetDifficulty()
    {
        return ObstacleDifficultyLevel.Hard;
    }

    private void Start()
    {
        Debug.Log($"ZigzagBarrier: Start() called for {gameObject.name} at {transform.position}");
        basePosition = transform.position;
        CreateBarrier();
        
        // Obtener el ObstacleMover para calcular la dirección perpendicular
        mover = GetComponent<ObstacleMover>();
        if (mover != null && mover.direction != Vector2.zero)
        {
            // Calcular dirección perpendicular (90 grados)
            perpendicularDirection = new Vector2(-mover.direction.y, mover.direction.x).normalized;
        }
        else
        {
            // Fallback: usar dirección hacia arriba
            perpendicularDirection = Vector2.up;
        }
        
        Debug.Log($"ZigzagBarrier: CreateBarrier() completed for {gameObject.name}");
    }

    private void Update()
    {
        if (mover == null) return;
        
        zigzagTime += Time.deltaTime * zigzagFrequency;
        
        // Actualizar la posición base según el movimiento normal
        // ObstacleMover moverá el objeto en su Update(), así que actualizamos basePosition
        basePosition += (Vector3)(mover.direction.normalized * mover.speed * Time.deltaTime);
    }
    
    private void LateUpdate()
    {
        if (mover == null) return;
        
        // Calcular el offset del zigzag
        float zigzagOffset = Mathf.Sin(zigzagTime) * zigzagAmplitude;
        
        // Aplicar el movimiento en zigzag perpendicular a la dirección de movimiento
        // Esto se ejecuta después de que ObstacleMover haya movido el objeto en Update()
        Vector3 zigzagOffset3D = (Vector3)(perpendicularDirection * zigzagOffset);
        transform.position = basePosition + zigzagOffset3D;
    }

    private void CreateBarrier()
    {
        barrierObject = new GameObject("Barrier");
        barrierObject.transform.SetParent(transform);
        barrierObject.transform.localPosition = Vector3.zero;
        
        SpriteRenderer sr = barrierObject.AddComponent<SpriteRenderer>();
        sr.sprite = CreateBarrierSprite();
        sr.color = Color.white;
        sr.sortingOrder = 5;
        sr.sortingLayerName = "Default";
        
        // Calcular el tamaño real del sprite en unidades del mundo
        float spriteWorldSize = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
        BoxCollider2D collider = barrierObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(spriteWorldSize, spriteWorldSize);
        collider.isTrigger = true;
        
        // Agregar detector de colisiones
        barrierObject.AddComponent<ObstacleCollisionDetector>();
    }

    private Sprite CreateBarrierSprite()
    {
        // Crear fragmento de constelación alargado para la barrera
        Color fragmentColor = Color.Lerp(CosmicTheme.ConstellationBlue, CosmicTheme.CelestialBlue, 0.6f);
        return ConstellationFragmentGenerator.CreateFragmentSprite(0.5f, fragmentColor, false);
    }
}

