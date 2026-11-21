using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Ball : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    const float LabelCharacterSize = 0.07f;

    float ballDiameter;
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

    public void Init(int type, Sprite sprite, Color tint, float diameter)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        Type = type;
        ballDiameter = diameter;
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = tint;
        transform.localScale = Vector3.one * BallSprites.ScaleFor(sprite, diameter);
    }

    /// <summary>Gives the ball a power and a letter on it so the player can tell it apart.</summary>
    public void SetKind(BallKind newKind)
    {
        Kind = newKind;
        if (newKind == BallKind.Normal) return;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        string letter = GetKindLetter(newKind);

        // A dark copy slightly behind the white letter keeps it readable on both light and dark artwork.
        var labelRoot = new GameObject("BonusLabel");
        labelRoot.transform.SetParent(transform, false);
        labelRoot.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        // The ball itself is scaled to fit its artwork, so undo that to keep the letter a fixed world size.
        float ballScale = Mathf.Max(transform.localScale.x, 0.0001f);
        labelRoot.transform.localScale = Vector3.one * (ballDiameter / ballScale);

        CreateLabelText(labelRoot.transform, font, letter, new Color(0.05f, 0.03f, 0.02f), new Vector3(0.02f, -0.02f, 0f), 1);
        CreateLabelText(labelRoot.transform, font, letter, Color.white, Vector3.zero, 2);
    }

    static void CreateLabelText(Transform parent, Font font, string letter, Color color, Vector3 localOffset, int sortingOrder)
    {
        var textObject = new GameObject("Letter");
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localOffset;

        var textMesh = textObject.AddComponent<TextMesh>();
        textMesh.font = font;
        textMesh.text = letter;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = LabelCharacterSize;
        textMesh.fontSize = 64;
        textMesh.color = color;

        var meshRenderer = textObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = font.material;
        meshRenderer.sortingOrder = sortingOrder;
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
