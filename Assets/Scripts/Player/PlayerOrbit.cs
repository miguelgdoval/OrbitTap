using UnityEngine;

public class PlayerOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    public float radius = 2f;
    public float angle = 0f;
    public float angularSpeed = 2f;
    public Transform center;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public FlashEffect flashEffect;

    private void Start()
    {
        if (!Application.isPlaying) return;
        
        if (center == null)
        {
            GameObject centerObj = GameObject.Find("Center");
            if (centerObj != null)
            {
                center = centerObj.transform;
            }
            else
            {
                center = new GameObject("Center").transform;
                center.position = Vector3.zero;
            }
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Create sprite if none exists
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = SpriteGenerator.CreateCircleSprite(0.5f, new Color(0f, 0.8784314f, 1f, 1f));
        }

        if (flashEffect == null)
        {
            flashEffect = GetComponent<FlashEffect>();
        }
    }

    private void Update()
    {
        if (center == null) return;

        // Update angle based on angular speed
        angle += angularSpeed * Time.deltaTime;

        // Calculate position using circular motion
        float x = center.position.x + Mathf.Cos(angle) * radius;
        float y = center.position.y + Mathf.Sin(angle) * radius;

        transform.position = new Vector3(x, y, 0f);
    }

    public void ToggleDirection()
    {
        angularSpeed = -angularSpeed;
        
        // Trigger flash effect
        if (flashEffect != null)
        {
            flashEffect.TriggerFlash();
        }
    }
}

