using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Ball : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    bool isPopping;
    float popTimer;
    float popDuration;
    Vector3 startScale;

    /// <summary>Index into the chain's colour/type list.</summary>
    public int Type { get; private set; }

    /// <summary>Distance travelled along the path, in world units.</summary>
    public float Distance { get; set; }

    /// <summary>Extra space between this ball and the ball ahead of it (0 when they touch).</summary>
    public float GapAhead { get; set; }

    /// <summary>Combo level to use when the gap ahead of this ball closes and the seam is checked.</summary>
    public int SeamComboLevel { get; set; }

    public void Init(int type, Color color, float diameter)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        Type = type;
        spriteRenderer.sprite = PlaceholderSprites.Circle;
        spriteRenderer.color = color;
        transform.localScale = Vector3.one * diameter;
    }

void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        enabled = false; // Update is only needed while the ball is popping
    }

    /// <summary>Shrinks the ball to nothing over the given time, then destroys it.</summary>
    public void Pop(float duration)
    {
        isPopping = true;
        popTimer = 0f;
        popDuration = Mathf.Max(duration, 0.01f);
        startScale = transform.localScale;
        enabled = true;
    }

    void Update()
    {
        if (!isPopping) return;

        popTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(popTimer / popDuration);
        transform.localScale = startScale * (1f - progress);

        if (progress >= 1f)
            Destroy(gameObject);
    }

}
