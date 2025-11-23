using UnityEngine;

public class RotatingArc : ObstacleBase
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 90f;
    public float arcRadius = 0.5f; // Radio del collider (más pequeño para que esté en la órbita)
    public float arcWidth = 0.2f;
    public float arcAngle = 90f; // degrees

    private void Start()
    {
        Debug.Log($"RotatingArc: Start() called for {gameObject.name} at {transform.position}");
        CreateArc();
        Debug.Log($"RotatingArc: CreateArc() completed for {gameObject.name}");
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    private void CreateArc()
    {
        GameObject arc = new GameObject("Arc");
        arc.transform.SetParent(transform);
        arc.transform.localPosition = Vector3.zero;
        
        SpriteRenderer sr = arc.AddComponent<SpriteRenderer>();
        sr.sprite = CreateArcSprite();
        sr.color = new Color(1f, 0f, 0f, 1f); // Rojo brillante para máxima visibilidad
        sr.sortingOrder = 5; // Asegurar que esté visible
        sr.sortingLayerName = "Default";
        
        // Calcular el tamaño real del sprite en unidades del mundo para colisión pixel perfect
        float spriteWorldSize = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
        // El radio del collider debe ser la mitad del tamaño del sprite
        float colliderRadius = spriteWorldSize / 2f;
        
        // Create collider que coincida exactamente con el sprite
        CircleCollider2D collider = arc.AddComponent<CircleCollider2D>();
        collider.radius = colliderRadius;
        collider.isTrigger = true;
        
        // Agregar detector de colisiones
        arc.AddComponent<ObstacleCollisionDetector>();
    }

    private Sprite CreateArcSprite()
    {
        // Crear un sprite MUY grande y simple - un cuadrado sólido
        int size = 256;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        // Llenar todo el sprite de blanco (máxima visibilidad)
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        // Usar pixelsPerUnit más alto para hacer el sprite más pequeño - 200 hace que 256px = 1.28 unidades en el mundo
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 200f);
    }
}

