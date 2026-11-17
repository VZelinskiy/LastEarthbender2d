using UnityEngine;

/// <summary>
/// Generates a simple shaded white circle at runtime so the game is playable
/// before real art exists. Tint it with SpriteRenderer.color.
/// </summary>
public static class PlaceholderSprites
{
    static Sprite circle;

    public static Sprite Circle
    {
        get
        {
            if (circle != null) return circle;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PlaceholderCircle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float radius = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int pixelY = 0; pixelY < size; pixelY++)
            {
                for (int pixelX = 0; pixelX < size; pixelX++)
                {
                    float offsetX = pixelX + 0.5f - radius;
                    float offsetY = pixelY + 0.5f - radius;
                    float distanceFromCenter = Mathf.Sqrt(offsetX * offsetX + offsetY * offsetY);
                    float alpha = Mathf.Clamp01(radius - distanceFromCenter);
                    float light = Mathf.Clamp01((-offsetX + offsetY) / (radius * 1.4f) * 0.5f + 0.5f);
                    float shade = Mathf.Lerp(0.7f, 1f, light);
                    byte brightness = (byte)Mathf.RoundToInt(shade * 255f);
                    pixels[pixelY * size + pixelX] = new Color32(brightness, brightness, brightness, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            // pixelsPerUnit == size, so the sprite is exactly 1 world unit wide.
            circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            circle.name = "PlaceholderCircle";
            return circle;
        }
    }
}
