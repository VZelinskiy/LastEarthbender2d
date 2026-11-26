using UnityEngine;

public static class SpriteFit
{
    /// <summary>The scale that makes a sprite exactly worldHeight units tall, whatever its pixel size.</summary>
    public static float ScaleForHeight(Sprite sprite, float worldHeight)
    {
        float spriteHeight = sprite != null ? sprite.bounds.size.y : 1f;
        return spriteHeight > 0.0001f ? worldHeight / spriteHeight : worldHeight;
    }
}
