using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The earthbender in the middle of the playfield. Aims at the pointer (mouse or finger)
/// and fires earth balls. Visuals are built from placeholder sprites until real art exists.
/// </summary>
[DefaultExecutionOrder(10)] // start after BallChain so the first colours exist in the chain
public class Shooter : MonoBehaviour
{
    [SerializeField] BallChain chain;
    [SerializeField] Camera gameCamera;

    [Header("Shooting")]
    [SerializeField, Min(0f)] float shotCooldown = 0.25f;
    [SerializeField, Min(0.1f)] float projectileSpeed = 14f;
    [SerializeField, Min(0f)] float minimumAimDistance = 0.5f;

    [Header("Placeholder look")]
    [SerializeField] float muzzleDistance = 0.9f;
    [SerializeField] Color bodyColor = new Color(0.55f, 0.40f, 0.25f);
    [SerializeField] Color barrelColor = new Color(0.35f, 0.25f, 0.15f);

    Transform aimPivot;
    SpriteRenderer loadedBallRenderer;
    SpriteRenderer nextBallRenderer;
    int loadedType;
    int nextType;
    float cooldownTimer;

    /// <summary>Set to false to stop aiming and shooting (game over, menus).</summary>
    public bool InputEnabled { get; set; } = true;

    void Start()
    {
        if (chain == null) chain = FindAnyObjectByType<BallChain>();
        if (gameCamera == null) gameCamera = Camera.main;

        if (chain == null || gameCamera == null)
        {
            Debug.LogError("Shooter: needs a BallChain and a camera in the scene.", this);
            enabled = false;
            return;
        }

        BuildPlaceholderVisuals();

        loadedType = chain.PickShooterType();
        nextType = chain.PickShooterType();
        RefreshBallVisuals();
    }

    void Update()
    {
        if (!InputEnabled) return;

        cooldownTimer -= Time.deltaTime;

        var pointer = Pointer.current;
        if (pointer == null) return;

        Vector2 screenPosition = pointer.position.ReadValue();
        Vector3 worldPosition = gameCamera.ScreenToWorldPoint(screenPosition);
        Vector2 aimVector = (Vector2)worldPosition - (Vector2)transform.position;
        if (aimVector.magnitude < minimumAimDistance) return;

        float aimAngleDegrees = Mathf.Atan2(aimVector.y, aimVector.x) * Mathf.Rad2Deg;
        aimPivot.rotation = Quaternion.Euler(0f, 0f, aimAngleDegrees - 90f);

        if (pointer.press.wasPressedThisFrame && cooldownTimer <= 0f)
            Fire(aimVector.normalized);
    }

    void Fire(Vector2 direction)
    {
        var projectileObject = new GameObject("Projectile");
        projectileObject.transform.position = loadedBallRenderer.transform.position;

        var projectile = projectileObject.AddComponent<Projectile>();
        projectile.Launch(chain, loadedType, direction, projectileSpeed);

        loadedType = nextType;
        nextType = chain.PickShooterType();
        RefreshBallVisuals();

        cooldownTimer = shotCooldown;
    }

    /// <summary>Loads fresh balls, e.g. when a new level starts.</summary>
    public void ResetShooter()
    {
        if (loadedBallRenderer == null) return; // the visuals are built in Start

        loadedType = chain.PickShooterType();
        nextType = chain.PickShooterType();
        RefreshBallVisuals();
        cooldownTimer = 0f;
    }


    void RefreshBallVisuals()
    {
        loadedBallRenderer.color = chain.GetColor(loadedType);
        nextBallRenderer.color = chain.GetColor(nextType);
    }

    void BuildPlaceholderVisuals()
    {
        aimPivot = new GameObject("AimPivot").transform;
        aimPivot.SetParent(transform, false);

        CreateVisual("Body", transform, PlaceholderSprites.Circle, bodyColor, Vector3.zero, new Vector3(1.2f, 1.2f, 1f), 10);
        CreateVisual("Barrel", aimPivot, PlaceholderSprites.Square, barrelColor, new Vector3(0f, 0.45f, 0f), new Vector3(0.3f, 0.7f, 1f), 11);

        float ballDiameter = chain.BallDiameter;
        loadedBallRenderer = CreateVisual("LoadedBall", aimPivot, PlaceholderSprites.Circle, Color.white, new Vector3(0f, muzzleDistance, 0f), Vector3.one * ballDiameter, 12);
        nextBallRenderer = CreateVisual("NextBall", aimPivot, PlaceholderSprites.Circle, Color.white, new Vector3(0f, -0.4f, 0f), Vector3.one * ballDiameter * 0.6f, 12);
    }

    static SpriteRenderer CreateVisual(string visualName, Transform parent, Sprite sprite, Color color, Vector3 localPosition, Vector3 localScale, int sortingOrder)
    {
        var visualObject = new GameObject(visualName);
        visualObject.transform.SetParent(parent, false);
        visualObject.transform.localPosition = localPosition;
        visualObject.transform.localScale = localScale;

        var spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
        return spriteRenderer;
    }
}
