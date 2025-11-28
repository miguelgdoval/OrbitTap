using UnityEngine;

[ObstacleDifficulty(ObstacleDifficultyLevel.Medium)]
public class DoorRandom : ObstacleBase, IObstacleDifficulty
{
    public ObstacleDifficultyLevel GetDifficulty()
    {
        return ObstacleDifficultyLevel.Medium;
    }
    [Header("Door Settings")]
    public float gapSize = 1f;
    public float doorWidth = 0.2f;
    public float doorLength = 3f;
    public int numberOfGaps = 2;

    private void Start()
    {
        CreateRandomDoor();
    }

    private void CreateRandomDoor()
    {
        float totalLength = doorLength * numberOfGaps;
        float gapPosition = Random.Range(-totalLength / 2f, totalLength / 2f);

        // Create multiple door segments with random gap
        for (int i = 0; i < numberOfGaps; i++)
        {
            float segmentY = -totalLength / 2f + (i + 0.5f) * (totalLength / numberOfGaps);
            
            // Skip creating door at gap position
            if (Mathf.Abs(segmentY - gapPosition) < gapSize / 2f)
            {
                continue;
            }

            GameObject doorSegment = new GameObject("DoorSegment" + i);
            doorSegment.transform.SetParent(transform);
            doorSegment.transform.localPosition = new Vector3(0f, segmentY, 0f);
            
            SpriteRenderer sr = doorSegment.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDoorSprite();
            sr.color = Color.white; // El color viene del sprite
            sr.sortingOrder = 5; // Asegurar que esté visible
            sr.sortingLayerName = "Default";
            
            // Calcular el tamaño real del sprite en unidades del mundo para colisión pixel perfect
            float spriteWorldSize = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
            BoxCollider2D collider = doorSegment.AddComponent<BoxCollider2D>();
            // Usar el tamaño del sprite para el collider
            collider.size = new Vector2(spriteWorldSize, spriteWorldSize);
            collider.isTrigger = true;
            
            // Agregar detector de colisiones
            doorSegment.AddComponent<ObstacleCollisionDetector>();
        }
    }

    private Sprite CreateDoorSprite()
    {
        // Intentar cargar el sprite de asteroide
        Sprite asteroidSprite = LoadObstacleSprite("AsteroidObstacle");
        if (asteroidSprite != null)
        {
            return asteroidSprite;
        }
        
        // Fallback: crear fragmento de constelación si no se encuentra la imagen
        Color fragmentColor = Color.Lerp(CosmicTheme.ConstellationBlue, CosmicTheme.EtherealLila, 0.3f);
        return ConstellationFragmentGenerator.CreateFragmentSprite(0.5f, fragmentColor, true);
    }
}

