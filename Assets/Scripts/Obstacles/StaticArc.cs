using UnityEngine;

[ObstacleDifficulty(ObstacleDifficultyLevel.Easy)]
public class StaticArc : ObstacleBase, IObstacleDifficulty
{
    public ObstacleDifficultyLevel GetDifficulty()
    {
        return ObstacleDifficultyLevel.Easy;
    }
    [Header("Arc Settings")]
    public float arcRadius = 0.5f; // Radio del collider (más pequeño para que esté en la órbita)
    public float arcWidth = 0.2f;
    public float arcAngle = 120f; // degrees

    private void Start()
    {
        Debug.Log($"StaticArc: Start() called for {gameObject.name} at {transform.position}");
        CreateArc();
        Debug.Log($"StaticArc: CreateArc() completed for {gameObject.name}");
    }

    private void CreateArc()
    {
        GameObject arc = new GameObject("Arc");
        arc.transform.SetParent(transform);
        arc.transform.localPosition = Vector3.zero;
        
        SpriteRenderer sr = arc.AddComponent<SpriteRenderer>();
        sr.sprite = CreateArcSprite();
        sr.color = Color.white; // El color viene del sprite
        sr.sortingOrder = 5; // Asegurar que esté visible
        sr.sortingLayerName = "Default";
        
        // Calcular el tamaño real del sprite en unidades del mundo para colisión pixel perfect
        float spriteWorldSize = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
        // El radio del collider debe ser la mitad del tamaño del sprite
        float colliderRadius = spriteWorldSize / 2f;
        
        CircleCollider2D collider = arc.AddComponent<CircleCollider2D>();
        collider.radius = colliderRadius;
        collider.isTrigger = true;
        
        // Agregar detector de colisiones
        arc.AddComponent<ObstacleCollisionDetector>();
    }

    private Sprite CreateArcSprite()
    {
        // Crear fragmento de constelación
        Color fragmentColor = Color.Lerp(CosmicTheme.ConstellationBlue, CosmicTheme.CelestialBlue, 0.5f);
        return ConstellationFragmentGenerator.CreateFragmentSprite(0.5f, fragmentColor, false);
    }
}

