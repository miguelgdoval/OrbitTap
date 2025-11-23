using UnityEngine;

public class DoorFixed : ObstacleBase
{
    [Header("Door Settings")]
    public float gapSize = 1f;
    public float doorWidth = 0.2f;
    public float doorLength = 3f;

    private void Start()
    {
        Debug.Log($"DoorFixed: Start() called for {gameObject.name} at {transform.position}");
        CreateDoor();
        Debug.Log($"DoorFixed: CreateDoor() completed for {gameObject.name}");
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
        leftSR.color = new Color(1f, 0f, 0f, 1f); // Rojo brillante
        leftSR.sortingOrder = 5; // Asegurar que esté visible
        leftSR.sortingLayerName = "Default";
        
        // Calcular el tamaño real del sprite en unidades del mundo para colisión pixel perfect
        float spriteWorldSize = leftSR.sprite.rect.width / leftSR.sprite.pixelsPerUnit;
        BoxCollider2D leftCollider = leftDoor.AddComponent<BoxCollider2D>();
        // Usar el tamaño del sprite para el collider (ajustar según la orientación)
        leftCollider.size = new Vector2(spriteWorldSize, spriteWorldSize);
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
        rightSR.sortingOrder = 5; // Asegurar que esté visible
        rightSR.sortingLayerName = "Default";
        
        // Calcular el tamaño real del sprite en unidades del mundo para colisión pixel perfect
        float spriteWorldSizeRight = rightSR.sprite.rect.width / rightSR.sprite.pixelsPerUnit;
        BoxCollider2D rightCollider = rightDoor.AddComponent<BoxCollider2D>();
        // Usar el tamaño del sprite para el collider
        rightCollider.size = new Vector2(spriteWorldSizeRight, spriteWorldSizeRight);
        rightCollider.isTrigger = true;
        
        // Agregar detector de colisiones
        rightDoor.AddComponent<ObstacleCollisionDetector>();
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

