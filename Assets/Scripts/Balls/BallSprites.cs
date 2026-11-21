using UnityEngine;

public static class BallSprites
{
    /// <summary>
    /// The scale that makes a sprite exactly `diameter` world units wide, whatever its pixel size.
    /// Lets any artwork (or the placeholder circle) be used for a ball without tuning import settings.
    /// </summary>
    public static float ScaleFor(Sprite sprite, float diameter)
    {
        float spriteWidth = sprite != null ? sprite.bounds.size.x : 1f;
        return spriteWidth > 0.0001f ? diameter / spriteWidth : diameter;
    }
}
