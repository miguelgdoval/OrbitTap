using UnityEngine;

public class OscillatingBarrier : ObstacleBase
{
    [Header("Oscillation Settings")]
    public float amplitude = 2f;
    public float frequency = 1f;
    public float barrierWidth = 0.2f;
    public float barrierLength = 2f;

    private Vector3 startPosition;
    private float time = 0f;
    private Vector3 radialDirection;

    private void Start()
    {
        startPosition = transform.position;
        
        // Calcular dirección radial (hacia/desde el centro)
        GameObject centerObj = GameObject.Find("Center");
        if (centerObj != null)
        {
            Vector3 toCenter = centerObj.transform.position - transform.position;
            radialDirection = toCenter.normalized;
        }
        else
        {
            radialDirection = transform.up; // Fallback
        }
        
        CreateBarrier();
    }

    private void Update()
    {
        // Si tiene ObstacleMover, no oscilar (el movimiento lo maneja ObstacleMover)
        if (GetComponent<ObstacleMover>() != null)
        {
            return;
        }

        time += Time.deltaTime;
        float offset = Mathf.Sin(time * frequency) * amplitude;
        // Oscilar en dirección radial (hacia/desde el centro)
        transform.position = startPosition + radialDirection * offset;
    }

    private void CreateBarrier()
    {
        GameObject barrier = new GameObject("Barrier");
        barrier.transform.SetParent(transform);
        barrier.transform.localPosition = Vector3.zero;
        
        SpriteRenderer sr = barrier.AddComponent<SpriteRenderer>();
        sr.sprite = CreateBarrierSprite();
        sr.color = Color.white; // El color viene del sprite
        sr.sortingOrder = 5; // Asegurar que esté visible
        sr.sortingLayerName = "Default";
        
        // Calcular el tamaño real del sprite en unidades del mundo para colisión pixel perfect
        float spriteWorldSize = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
        BoxCollider2D collider = barrier.AddComponent<BoxCollider2D>();
        // Usar el tamaño del sprite para el collider
        collider.size = new Vector2(spriteWorldSize, spriteWorldSize);
        collider.isTrigger = true;
        
        // Agregar detector de colisiones
        barrier.AddComponent<ObstacleCollisionDetector>();
    }

    private Sprite CreateBarrierSprite()
    {
        // Crear fragmento de constelación
        Color fragmentColor = Color.Lerp(CosmicTheme.CelestialBlue, CosmicTheme.EtherealLila, 0.4f);
        return ConstellationFragmentGenerator.CreateFragmentSprite(0.5f, fragmentColor, false);
    }
}

