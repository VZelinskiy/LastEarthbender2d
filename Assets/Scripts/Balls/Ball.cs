using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Ball : MonoBehaviour
{
    SpriteRenderer spriteRenderer;

    /// <summary>Index into the chain's colour/type list.</summary>
    public int Type { get; private set; }

    /// <summary>Distance travelled along the path, in world units.</summary>
    public float Distance { get; set; }

    public void Init(int type, Color color, float diameter)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        Type = type;
        spriteRenderer.sprite = PlaceholderSprites.Circle;
        spriteRenderer.color = color;
        transform.localScale = Vector3.one * diameter;
    }
}
