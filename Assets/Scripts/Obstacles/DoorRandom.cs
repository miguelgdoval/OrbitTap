using UnityEngine;

public class DoorRandom : ObstacleBase
{
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
            sr.color = new Color(0.8f, 0.2f, 0.2f, 1f);
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
        // Crear un sprite MUY grande y simple - un cuadrado sólido
        int size = 256;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
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

