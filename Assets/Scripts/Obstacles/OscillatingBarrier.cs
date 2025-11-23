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
        sr.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        
        // El collider debe estar orientado tangencialmente a la órbita
        BoxCollider2D collider = barrier.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(barrierLength, barrierWidth);
        collider.isTrigger = true;
        
        // Agregar detector de colisiones
        barrier.AddComponent<ObstacleCollisionDetector>();
    }

    private Sprite CreateBarrierSprite()
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
}

