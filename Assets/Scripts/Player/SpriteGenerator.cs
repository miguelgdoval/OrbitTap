using UnityEngine;

public static class SpriteGenerator
{
    public static Sprite CreateCircleSprite(float radius, Color color)
    {
        int size = Mathf.RoundToInt(radius * 2 * 32);
        size = Mathf.Max(size, 32);
        
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radiusSquared = (size / 2f - 1) * (size / 2f - 1);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float distSquared = dx * dx + dy * dy;
                
                if (distSquared <= radiusSquared)
                {
                    colors[y * size + x] = color;
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
    }

    /// <summary>
    /// Crea un sprite de estrella naciente con borde brillante
    /// </summary>
    public static Sprite CreateStarSprite(float radius, Color color)
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxRadius = size / 2f - 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                
                // Núcleo brillante
                if (dist <= maxRadius * 0.4f)
                {
                    float alpha = 1f - (dist / (maxRadius * 0.4f));
                    colors[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
                // Borde brillante suave
                else if (dist <= maxRadius * 0.7f)
                {
                    float alpha = 0.6f * (1f - (dist - maxRadius * 0.4f) / (maxRadius * 0.3f));
                    // Mezclar con dorado para el borde
                    Color edgeColor = Color.Lerp(color, CosmicTheme.SoftGold, 0.5f);
                    colors[y * size + x] = new Color(edgeColor.r, edgeColor.g, edgeColor.b, alpha);
                }
                // Resplandor exterior muy sutil
                else if (dist <= maxRadius)
                {
                    float alpha = 0.2f * (1f - (dist - maxRadius * 0.7f) / (maxRadius * 0.3f));
                    colors[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}

