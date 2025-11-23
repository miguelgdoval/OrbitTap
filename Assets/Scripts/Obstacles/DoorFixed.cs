using UnityEngine;

public class DoorFixed : ObstacleBase
{
    [Header("Door Settings")]
    public float gapSize = 1f;
    public float doorWidth = 0.2f;
    public float doorLength = 3f;

    private void Start()
    {
        CreateDoor();
    }

    private void CreateDoor()
    {
        // Las puertas se crean en el eje Y local (radial desde el centro)
        // para que bloqueen la órbita del jugador
        
        // Create left door (hacia el centro)
        GameObject leftDoor = new GameObject("LeftDoor");
        leftDoor.transform.SetParent(transform);
        leftDoor.transform.localPosition = new Vector3(0f, -gapSize / 2f - doorWidth / 2f, 0f);
        
        SpriteRenderer leftSR = leftDoor.AddComponent<SpriteRenderer>();
        leftSR.sprite = CreateDoorSprite();
        leftSR.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        
        BoxCollider2D leftCollider = leftDoor.AddComponent<BoxCollider2D>();
        leftCollider.size = new Vector2(doorLength, doorWidth);
        leftCollider.isTrigger = true;
        
        // Agregar detector de colisiones
        leftDoor.AddComponent<ObstacleCollisionDetector>();

        // Create right door (alejándose del centro)
        GameObject rightDoor = new GameObject("RightDoor");
        rightDoor.transform.SetParent(transform);
        rightDoor.transform.localPosition = new Vector3(0f, gapSize / 2f + doorWidth / 2f, 0f);
        
        SpriteRenderer rightSR = rightDoor.AddComponent<SpriteRenderer>();
        rightSR.sprite = CreateDoorSprite();
        rightSR.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        
        BoxCollider2D rightCollider = rightDoor.AddComponent<BoxCollider2D>();
        rightCollider.size = new Vector2(doorLength, doorWidth);
        rightCollider.isTrigger = true;
        
        // Agregar detector de colisiones
        rightDoor.AddComponent<ObstacleCollisionDetector>();
    }

    private Sprite CreateDoorSprite()
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

