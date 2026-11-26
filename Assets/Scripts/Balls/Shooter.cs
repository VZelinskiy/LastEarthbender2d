using System.Collections.Generic;
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

    [Header("Earth Strike ability")]
    [SerializeField, Min(0f)] float strikeCooldown = 12f;
    [SerializeField, Min(0)] int strikeRadiusInBalls = 2;
    [SerializeField, Min(1f)] float strikeSpeedMultiplier = 1.3f;

    [Header("Aim guide (Aim bonus)")]
    [SerializeField, Min(0f)] float aimGuideDuration = 8f;

    [Header("Hero art (optional - without it the placeholder body is used)")]
    [SerializeField] Sprite idleSprite;   // facing up (north); rotates to follow the aim
    [SerializeField] Sprite shootSprite;  // shown briefly after a shot
    [SerializeField] Sprite strikeSprite; // shown briefly after the Earth Strike
    [SerializeField, Min(0.5f)] float heroWorldHeight = 1.8f;
    [SerializeField, Min(0f)] float poseDuration = 0.15f;
    [SerializeField, Min(0f)] float recoilDistance = 0.1f;

    [Header("Hero art: standing figure that does not rotate (optional)")]
    [SerializeField] bool rotateHeroWithAim = true; // true: top-down art that turns to the aim; false: a standing figure
    [SerializeField] Sprite frontIdleSprite;   // used instead of the sprites above while aiming towards the camera
    [SerializeField] Sprite frontShootSprite;
    [SerializeField] Sprite frontStrikeSprite;

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

    const float RecoilRecoverSpeed = 1.2f;
    SpriteRenderer heroRenderer;
    float poseTimer;
    float recoilAmount;
    bool facingFront;
    HeroPose currentPose;

    enum HeroPose
    {
        Idle,
        Shoot,
        Strike
    }

    /// <summary>Set to false to stop aiming and shooting (game over, menus).</summary>
    public bool InputEnabled { get; set; } = true;

    /// <summary>Lets the UI claim a screen position (e.g. over a button) so it does not fire a ball.</summary>
    public System.Predicate<Vector2> IsPointerBlocked { get; set; }

    public float StrikeCooldownRemaining => strikeTimer;
    public float StrikeCooldownDuration => strikeCooldown;
    public float AimGuideRemaining => guideTimer;

    const float GuideMaxLength = 14f;
    const float GuideStep = 0.1f;
    const float GuideDotSpacing = 0.5f;
    const int GuideDotCount = 28;

    float strikeTimer;
    float guideTimer;
    Vector2 lastAimDirection = Vector2.up;
    readonly List<SpriteRenderer> guideDots = new List<SpriteRenderer>();

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
        BuildGuideDots();
        chain.BonusTriggered += OnBonusTriggered;

        loadedType = chain.PickShooterType();
        nextType = chain.PickShooterType();
        RefreshBallVisuals();
    }

    void Update()
    {
        if (!InputEnabled)
        {
            HideGuide();
            return;
        }

        cooldownTimer -= Time.deltaTime;
        strikeTimer = Mathf.Max(0f, strikeTimer - Time.deltaTime);
        guideTimer = Mathf.Max(0f, guideTimer - Time.deltaTime);

        var pointer = Pointer.current;
        if (pointer == null) return;

        Vector2 screenPosition = pointer.position.ReadValue();
        Vector3 worldPosition = gameCamera.ScreenToWorldPoint(screenPosition);
        Vector2 aimVector = (Vector2)worldPosition - (Vector2)transform.position;
        bool aimIsValid = aimVector.magnitude >= minimumAimDistance;

        if (aimIsValid)
        {
            float aimAngleDegrees = Mathf.Atan2(aimVector.y, aimVector.x) * Mathf.Rad2Deg;
            aimPivot.rotation = Quaternion.Euler(0f, 0f, aimAngleDegrees - 90f);
            lastAimDirection = aimVector.normalized;
        }

        UpdateHeroFacing();
        UpdateHeroPose(Time.deltaTime);
        UpdateAimGuide();

        bool pointerIsBlocked = IsPointerBlocked != null && IsPointerBlocked(screenPosition);
        if (aimIsValid && !pointerIsBlocked && cooldownTimer <= 0f && WasPrimaryPressed(pointer))
            Fire(lastAimDirection);

        bool strikePressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                             || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
        if (strikePressed) TryUseEarthStrike();
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
        PlayPose(HeroPose.Shoot, recoilDistance);
    }

    // Switches the hero to another pose for a moment and kicks it back a little (art only).
    // Switches the hero to another pose for a moment and kicks it back a little (art only).
    void PlayPose(HeroPose pose, float recoil)
    {
        if (heroRenderer == null) return;

        recoilAmount = recoil;
        currentPose = pose;
        poseTimer = poseDuration;
        ShowHeroSprite(GetHeroSprite(pose));
    }

    void UpdateHeroPose(float deltaTime)
    {
        if (heroRenderer == null) return;

        if (poseTimer > 0f)
        {
            poseTimer -= deltaTime;
            if (poseTimer <= 0f)
            {
                currentPose = HeroPose.Idle;
                ShowHeroSprite(GetHeroSprite(currentPose));
            }
        }

        // The kick goes against the aim: a turning hero moves along its own back, a standing one along the world.
        recoilAmount = Mathf.MoveTowards(recoilAmount, 0f, RecoilRecoverSpeed * deltaTime);
        Vector2 recoilDirection = rotateHeroWithAim ? Vector2.down : -lastAimDirection;
        heroRenderer.transform.localPosition = recoilDirection * recoilAmount;
    }

    // Standing hero: back view while aiming up the screen, front view while aiming towards the camera.
    void UpdateHeroFacing()
    {
        if (heroRenderer == null || rotateHeroWithAim) return;

        // Two thresholds (hysteresis) so the hero does not flicker when the aim is close to horizontal.
        bool shouldFaceFront = facingFront ? lastAimDirection.y < 0.25f : lastAimDirection.y < -0.25f;
        if (shouldFaceFront == facingFront) return;

        facingFront = shouldFaceFront;
        ShowHeroSprite(GetHeroSprite(currentPose));
    }

    // Picks the sprite for a pose, falling back to the idle one of the same facing when a pose has no artwork.
    Sprite GetHeroSprite(HeroPose pose)
    {
        Sprite backSprite = pose == HeroPose.Shoot ? shootSprite : (pose == HeroPose.Strike ? strikeSprite : idleSprite);
        Sprite frontSprite = pose == HeroPose.Shoot ? frontShootSprite : (pose == HeroPose.Strike ? frontStrikeSprite : frontIdleSprite);
        bool useFront = !rotateHeroWithAim && facingFront;

        Sprite chosen = useFront ? frontSprite : backSprite;
        if (chosen == null) chosen = useFront ? frontIdleSprite : idleSprite;
        return chosen != null ? chosen : idleSprite;
    }


    void ShowHeroSprite(Sprite heroSprite)
    {
        heroRenderer.sprite = heroSprite;
        heroRenderer.transform.localScale = Vector3.one * SpriteFit.ScaleForHeight(heroSprite, heroWorldHeight);
    }


    /// <summary>Loads fresh balls, e.g. when a new level starts.</summary>
    public void ResetShooter()
    {
        if (loadedBallRenderer == null) return; // the visuals are built in Start

        loadedType = chain.PickShooterType();
        nextType = chain.PickShooterType();
        RefreshBallVisuals();
        cooldownTimer = 0f;
        strikeTimer = 0f;
        guideTimer = 0f;
        HideGuide();

        facingFront = false;
        currentPose = HeroPose.Idle;
        poseTimer = 0f;
        recoilAmount = 0f;
        if (heroRenderer != null) ShowHeroSprite(GetHeroSprite(HeroPose.Idle));
    }

    void OnDestroy()
    {
        if (chain != null) chain.BonusTriggered -= OnBonusTriggered;
    }

    void OnBonusTriggered(BallKind kind)
    {
        if (kind == BallKind.Aim) guideTimer = aimGuideDuration;
    }

    /// <summary>Fires the heavy stone that clears several balls around the hit. Returns false while on cooldown.</summary>
    public bool TryUseEarthStrike()
    {
        if (!InputEnabled || strikeTimer > 0f || loadedBallRenderer == null) return false;

        var strikeObject = new GameObject("EarthStrike");
        strikeObject.transform.position = loadedBallRenderer.transform.position;

        var projectile = strikeObject.AddComponent<Projectile>();
        projectile.LaunchStrike(chain, lastAimDirection, projectileSpeed * strikeSpeedMultiplier, strikeRadiusInBalls);

        strikeTimer = strikeCooldown;
        PlayPose(HeroPose.Strike, recoilDistance * 2f);
        return true;
    }

    static bool WasPrimaryPressed(Pointer pointer)
    {
        // For a mouse only the left button shoots; the right button belongs to the Earth Strike.
        if (pointer is Mouse mouse) return mouse.leftButton.wasPressedThisFrame;
        return pointer.press.wasPressedThisFrame;
    }

    // Dotted line from the muzzle to the first ball the shot would hit.
    void UpdateAimGuide()
    {
        if (guideTimer <= 0f)
        {
            HideGuide();
            return;
        }

        Vector3 origin = loadedBallRenderer.transform.position;
        Vector3 direction = new Vector3(lastAimDirection.x, lastAimDirection.y, 0f);

        float hitDistance = GuideMaxLength;
        for (float travelled = 0f; travelled < GuideMaxLength; travelled += GuideStep)
        {
            int ignoredBallIndex;
            if (chain.TryFindHit(origin + direction * travelled, chain.BallDiameter * 0.85f, out ignoredBallIndex))
            {
                hitDistance = travelled;
                break;
            }
        }

        int visibleDotCount = Mathf.Min(guideDots.Count, Mathf.FloorToInt(hitDistance / GuideDotSpacing));
        for (int dotIndex = 0; dotIndex < guideDots.Count; dotIndex++)
        {
            bool isUsed = dotIndex < visibleDotCount;
            guideDots[dotIndex].gameObject.SetActive(isUsed);
            if (isUsed)
                guideDots[dotIndex].transform.position = origin + direction * ((dotIndex + 1) * GuideDotSpacing);
        }
    }

    void HideGuide()
    {
        foreach (var guideDot in guideDots)
        {
            if (guideDot.gameObject.activeSelf) guideDot.gameObject.SetActive(false);
        }
    }

    void BuildGuideDots()
    {
        for (int dotIndex = 0; dotIndex < GuideDotCount; dotIndex++)
        {
            var guideDot = CreateVisual("GuideDot", transform, PlaceholderSprites.Circle, new Color(1f, 1f, 1f, 0.7f), Vector3.zero, Vector3.one * 0.14f, 4);
            guideDot.gameObject.SetActive(false);
            guideDots.Add(guideDot);
        }
    }



    void RefreshBallVisuals()
    {
        ApplyBallLook(loadedBallRenderer, loadedType, chain.BallDiameter);
        ApplyBallLook(nextBallRenderer, nextType, chain.BallDiameter * 0.6f);
    }

    // Shows a ball type (artwork or placeholder circle) at the given world diameter.
    void ApplyBallLook(SpriteRenderer target, int typeIndex, float diameter)
    {
        Sprite sprite = chain.GetSprite(typeIndex);
        target.sprite = sprite;
        target.color = chain.GetColor(typeIndex);
        target.transform.localScale = Vector3.one * BallSprites.ScaleFor(sprite, diameter);
    }


    void BuildPlaceholderVisuals()
    {
        aimPivot = new GameObject("AimPivot").transform;
        aimPivot.SetParent(transform, false);

        float ballDiameter = chain.BallDiameter;
        Vector3 nextBallPosition;

        if (idleSprite != null)
        {
            // Real artwork. A top-down sprite turns with the aim; a standing figure stays upright.
            Transform heroParent = rotateHeroWithAim ? aimPivot : transform;
            heroRenderer = CreateVisual("HeroArt", heroParent, idleSprite, Color.white, Vector3.zero, Vector3.one, 10);
            ShowHeroSprite(idleSprite);
            nextBallPosition = new Vector3(0f, -heroWorldHeight * 0.5f - 0.1f, 0f);
        }
        else
        {
            CreateVisual("Body", transform, PlaceholderSprites.Circle, bodyColor, Vector3.zero, new Vector3(1.2f, 1.2f, 1f), 10);
            CreateVisual("Barrel", aimPivot, PlaceholderSprites.Square, barrelColor, new Vector3(0f, 0.45f, 0f), new Vector3(0.3f, 0.7f, 1f), 11);
            nextBallPosition = new Vector3(0f, -0.4f, 0f);
        }

        loadedBallRenderer = CreateVisual("LoadedBall", aimPivot, PlaceholderSprites.Circle, Color.white, new Vector3(0f, muzzleDistance, 0f), Vector3.one * ballDiameter, 12);
        nextBallRenderer = CreateVisual("NextBall", aimPivot, PlaceholderSprites.Circle, Color.white, nextBallPosition, Vector3.one * ballDiameter * 0.6f, 12);
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
