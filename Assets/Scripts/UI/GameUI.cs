using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// A minimal HUD and the win/lose overlay, built at runtime so no scene assets are needed.
/// Swap it for real UI art later.
/// </summary>
[DefaultExecutionOrder(30)] // after LevelController has loaded the first level
public class GameUI : MonoBehaviour
{
    LevelController controller;
    Font font;
    Text hudText;
    GameObject overlayRoot;
    Text overlayTitle;
    Text overlayMessage;
    Text buttonLabel;

    int shownBallsRemaining = -1;
    int shownLevelNumber = -1;
    int shownSlowSeconds = -1;
    int shownReverseSeconds = -1;
    int shownAimSeconds = -1;
    int shownStrikeSeconds = -1;

    Shooter shooter;
    BallChain chain;
    RectTransform abilityRect;
    Image abilityCooldownOverlay;
    Text abilityLabel;

    void Start()
    {
        controller = FindAnyObjectByType<LevelController>();
        shooter = FindAnyObjectByType<Shooter>();
        chain = FindAnyObjectByType<BallChain>();

        if (controller == null || !controller.enabled || shooter == null || chain == null)
        {
            Debug.LogError("GameUI: needs a working LevelController, Shooter and BallChain in the scene.", this);
            enabled = false;
            return;
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        EnsureEventSystem();
        var canvas = CreateCanvas();
        BuildHud(canvas.transform);
        BuildAbilityButton(canvas.transform);
        BuildOverlay(canvas.transform); // built last so it covers everything

        shooter.IsPointerBlocked = IsOverAbilityButton;
        controller.StateChanged += OnStateChanged;
        OnStateChanged(controller.State);
    }

    void OnDestroy()
    {
        if (controller != null) controller.StateChanged -= OnStateChanged;
        if (shooter != null) shooter.IsPointerBlocked = null;
    }

    void Update()
    {
        int ballsRemaining = controller.BallsRemaining;
        int levelNumber = controller.LevelNumber;
        int slowSeconds = Mathf.CeilToInt(chain.SlowRemaining);
        int reverseSeconds = Mathf.CeilToInt(chain.ReverseRemaining);
        int aimSeconds = Mathf.CeilToInt(shooter.AimGuideRemaining);

        bool hudChanged = ballsRemaining != shownBallsRemaining
                          || levelNumber != shownLevelNumber
                          || slowSeconds != shownSlowSeconds
                          || reverseSeconds != shownReverseSeconds
                          || aimSeconds != shownAimSeconds;

        if (hudChanged)
        {
            shownBallsRemaining = ballsRemaining;
            shownLevelNumber = levelNumber;
            shownSlowSeconds = slowSeconds;
            shownReverseSeconds = reverseSeconds;
            shownAimSeconds = aimSeconds;

            string effects = "";
            if (slowSeconds > 0) effects += $"Slow {slowSeconds}s   ";
            if (reverseSeconds > 0) effects += $"Reverse {reverseSeconds}s   ";
            if (aimSeconds > 0) effects += $"Aim {aimSeconds}s";

            hudText.text = $"{controller.CurrentLevelName}  ({levelNumber}/{controller.LevelCount})\nBalls left: {ballsRemaining}\n{effects}";
        }

        UpdateAbilityButton();
    }

    void OnStateChanged(GameState state)
    {
        overlayRoot.SetActive(state != GameState.Playing);
        if (state == GameState.Playing) return;

        if (state == GameState.Won)
        {
            overlayTitle.text = "Level complete!";
            overlayMessage.text = controller.HasNextLevel ? "The earth answers your call." : "You cleared every level.";
            buttonLabel.text = controller.HasNextLevel ? "Next level" : "Play again";
        }
        else
        {
            overlayTitle.text = "The chain broke through";
            overlayMessage.text = "The balls reached the end of the path.";
            buttonLabel.text = "Try again";
        }
    }

    void OnButtonClicked()
    {
        if (controller.State == GameState.Won) controller.NextLevel();
        else controller.RestartLevel();
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        var inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("GameUICanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    void BuildHud(Transform parent)
    {
        hudText = CreateText("HudText", parent, 30, TextAnchor.UpperLeft, Color.white);
        var rect = hudText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -18f);
        rect.sizeDelta = new Vector2(700f, 130f);
        hudText.gameObject.AddComponent<Shadow>();
    }

    void BuildAbilityButton(Transform parent)
    {
        var buttonObject = new GameObject("AbilityButton");
        buttonObject.transform.SetParent(parent, false);

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.55f, 0.40f, 0.22f);
        abilityRect = buttonImage.rectTransform;
        abilityRect.anchorMin = new Vector2(1f, 0f);
        abilityRect.anchorMax = new Vector2(1f, 0f);
        abilityRect.pivot = new Vector2(1f, 0f);
        abilityRect.anchoredPosition = new Vector2(-24f, 24f);
        abilityRect.sizeDelta = new Vector2(280f, 84f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(OnAbilityClicked);

        // A dark layer that shrinks as the cooldown runs out.
        var overlayObject = new GameObject("CooldownOverlay");
        overlayObject.transform.SetParent(buttonObject.transform, false);
        abilityCooldownOverlay = overlayObject.AddComponent<Image>();
        abilityCooldownOverlay.sprite = PlaceholderSprites.Square;
        abilityCooldownOverlay.color = new Color(0f, 0f, 0f, 0.55f);
        abilityCooldownOverlay.type = Image.Type.Filled;
        abilityCooldownOverlay.fillMethod = Image.FillMethod.Horizontal;
        abilityCooldownOverlay.fillOrigin = (int)Image.OriginHorizontal.Right;
        abilityCooldownOverlay.fillAmount = 0f;
        abilityCooldownOverlay.raycastTarget = false;
        StretchToParent(abilityCooldownOverlay.rectTransform);

        abilityLabel = CreateText("Label", buttonObject.transform, 28, TextAnchor.MiddleCenter, Color.white);
        StretchToParent(abilityLabel.rectTransform);
        abilityLabel.text = "Earth Strike\n(Space)";
    }

    void OnAbilityClicked()
    {
        shooter.TryUseEarthStrike();
    }

    bool IsOverAbilityButton(Vector2 screenPoint)
    {
        return abilityRect != null
               && abilityRect.gameObject.activeInHierarchy
               && RectTransformUtility.RectangleContainsScreenPoint(abilityRect, screenPoint, null);
    }

    void UpdateAbilityButton()
    {
        float remaining = shooter.StrikeCooldownRemaining;
        float duration = shooter.StrikeCooldownDuration;
        abilityCooldownOverlay.fillAmount = duration > 0f ? remaining / duration : 0f;

        int remainingSeconds = Mathf.CeilToInt(remaining);
        if (remainingSeconds == shownStrikeSeconds) return;

        shownStrikeSeconds = remainingSeconds;
        abilityLabel.text = remainingSeconds > 0 ? $"Earth Strike\n{remainingSeconds}s" : "Earth Strike\n(Space)";
    }

    static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }


    void BuildOverlay(Transform parent)
    {
        overlayRoot = new GameObject("Overlay");
        overlayRoot.transform.SetParent(parent, false);
        var background = overlayRoot.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.65f);
        var backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        overlayTitle = CreateText("Title", overlayRoot.transform, 60, TextAnchor.MiddleCenter, Color.white);
        PlaceCentered(overlayTitle.rectTransform, new Vector2(0f, 110f), new Vector2(1000f, 100f));

        overlayMessage = CreateText("Message", overlayRoot.transform, 32, TextAnchor.MiddleCenter, new Color(0.9f, 0.85f, 0.75f));
        PlaceCentered(overlayMessage.rectTransform, new Vector2(0f, 30f), new Vector2(1000f, 70f));

        var buttonObject = new GameObject("ActionButton");
        buttonObject.transform.SetParent(overlayRoot.transform, false);
        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.85f, 0.65f, 0.35f);
        PlaceCentered(buttonImage.rectTransform, new Vector2(0f, -80f), new Vector2(320f, 90f));

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(OnButtonClicked);

        buttonLabel = CreateText("Label", buttonObject.transform, 38, TextAnchor.MiddleCenter, new Color(0.2f, 0.12f, 0.05f));
        var labelRect = buttonLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    Text CreateText(string objectName, Transform parent, int fontSize, TextAnchor alignment, Color color)
    {
        var textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        var text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    static void PlaceCentered(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }
}
