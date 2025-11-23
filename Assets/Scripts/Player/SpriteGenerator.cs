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
}

