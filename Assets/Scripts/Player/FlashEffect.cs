using UnityEngine;

public class FlashEffect : MonoBehaviour
{
    [Header("Flash Settings")]
    public float flashDuration = 0.2f;
    public Color flashColor = CosmicTheme.SoftGold; // Pulso dorado como latido estelar

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void TriggerFlash()
    {
        if (spriteRenderer == null || isFlashing) return;

        StartCoroutine(FlashCoroutine());
    }

    private System.Collections.IEnumerator FlashCoroutine()
    {
        isFlashing = true;
        spriteRenderer.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        spriteRenderer.color = originalColor;
        isFlashing = false;
    }
}

