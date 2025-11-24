using UnityEngine;

/// <summary>
/// Fragmento que rota mientras se mueve, creando una trayectoria espiral
/// </summary>
[ObstacleDifficulty(ObstacleDifficultyLevel.Hard)]
public class SpiralFragment : ObstacleBase, IObstacleDifficulty
{
    [Header("Spiral Settings")]
    public float rotationSpeed = 180f; // Grados por segundo
    public float spiralIntensity = 0.3f; // Intensidad del movimiento espiral (0 = recto, 1 = muy espiral)
    
    private GameObject fragmentObject;
    private Vector3 initialPosition;
    private float rotationTime = 0f;
    private ObstacleMover mover;

    public ObstacleDifficultyLevel GetDifficulty()
    {
        return ObstacleDifficultyLevel.Hard;
    }

    private void Start()
    {
        Debug.Log($"SpiralFragment: Start() called for {gameObject.name} at {transform.position}");
        initialPosition = transform.position;
        CreateFragment();
        
        // Obtener el ObstacleMover para modificar su dirección
        mover = GetComponent<ObstacleMover>();
        
        Debug.Log($"SpiralFragment: CreateFragment() completed for {gameObject.name}");
    }

    private void Update()
    {
        rotationTime += Time.deltaTime;
        
        // Rotar el objeto
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        
        // Modificar la dirección del movimiento para crear espiral
        // Solo si el movimiento es significativo y tenemos ObstacleMover
        if (mover != null && spiralIntensity > 0f && mover.direction != Vector2.zero)
        {
            // Calcular un offset perpendicular a la dirección de movimiento
            Vector2 perpendicular = new Vector2(-mover.direction.y, mover.direction.x);
            float spiralOffset = Mathf.Sin(rotationTime * rotationSpeed * Mathf.Deg2Rad) * spiralIntensity;
            
            // Aplicar el offset espiral después de que ObstacleMover haya movido el objeto
            // Esto se hace en LateUpdate para que se ejecute después de ObstacleMover.Update()
            // Pero como no podemos usar LateUpdate aquí, aplicamos el offset directamente
            // El ObstacleMover moverá el objeto en su Update(), y luego nosotros añadimos el offset
            Vector3 spiralOffset3D = (Vector3)(perpendicular * spiralOffset * Time.deltaTime * mover.speed);
            transform.position += spiralOffset3D;
        }
    }

    private void CreateFragment()
    {
        fragmentObject = new GameObject("Fragment");
        fragmentObject.transform.SetParent(transform);
        fragmentObject.transform.localPosition = Vector3.zero;
        
        SpriteRenderer sr = fragmentObject.AddComponent<SpriteRenderer>();
        sr.sprite = CreateFragmentSprite();
        sr.color = Color.white;
        sr.sortingOrder = 5;
        sr.sortingLayerName = "Default";
        
        // Calcular el tamaño real del sprite en unidades del mundo
        float spriteWorldSize = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
        CircleCollider2D collider = fragmentObject.AddComponent<CircleCollider2D>();
        collider.radius = spriteWorldSize / 2f;
        collider.isTrigger = true;
        
        // Agregar detector de colisiones
        fragmentObject.AddComponent<ObstacleCollisionDetector>();
    }

    private Sprite CreateFragmentSprite()
    {
        // Crear fragmento de constelación con forma de estrella para el efecto espiral
        Color fragmentColor = Color.Lerp(CosmicTheme.CelestialBlue, CosmicTheme.EtherealLila, 0.5f);
        return ConstellationFragmentGenerator.CreateFragmentSprite(0.5f, fragmentColor, true);
    }
}

