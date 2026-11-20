using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Ball : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    const float LabelCharacterSize = 0.07f;

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

    /// <summary>The special power this ball carries (Normal for most balls).</summary>
    public BallKind Kind { get; private set; }

    public void Init(int type, Color color, float diameter)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        Type = type;
        spriteRenderer.sprite = PlaceholderSprites.Circle;
        spriteRenderer.color = color;
        transform.localScale = Vector3.one * diameter;
    }

    /// <summary>Gives the ball a power and a letter on it so the player can tell it apart.</summary>
    public void SetKind(BallKind newKind)
    {
        Kind = newKind;
        if (newKind == BallKind.Normal) return;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var labelObject = new GameObject("BonusLabel");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        var textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.font = font;
        textMesh.text = GetKindLetter(newKind);
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = LabelCharacterSize;
        textMesh.fontSize = 64;
        textMesh.color = new Color(0.1f, 0.07f, 0.04f);

        var meshRenderer = labelObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = font.material;
        meshRenderer.sortingOrder = 1;
    }

    static string GetKindLetter(BallKind kind)
    {
        switch (kind)
        {
            case BallKind.Slow: return "S";
            case BallKind.Reverse: return "R";
            case BallKind.Bomb: return "B";
            case BallKind.Aim: return "A";
            default: return "";
        }
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
