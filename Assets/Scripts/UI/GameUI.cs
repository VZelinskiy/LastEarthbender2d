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

    void Start()
    {
        controller = FindAnyObjectByType<LevelController>();
        if (controller == null || !controller.enabled)
        {
            Debug.LogError("GameUI: needs a working LevelController in the scene.", this);
            enabled = false;
            return;
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        EnsureEventSystem();
        var canvas = CreateCanvas();
        BuildHud(canvas.transform);
        BuildOverlay(canvas.transform);

        controller.StateChanged += OnStateChanged;
        OnStateChanged(controller.State);
    }

    void OnDestroy()
    {
        if (controller != null) controller.StateChanged -= OnStateChanged;
    }

    void Update()
    {
        int ballsRemaining = controller.BallsRemaining;
        int levelNumber = controller.LevelNumber;
        if (ballsRemaining == shownBallsRemaining && levelNumber == shownLevelNumber) return;

        shownBallsRemaining = ballsRemaining;
        shownLevelNumber = levelNumber;
        hudText.text = $"{controller.CurrentLevelName}  ({levelNumber}/{controller.LevelCount})\nBalls left: {ballsRemaining}";
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
        rect.sizeDelta = new Vector2(700f, 100f);
        hudText.gameObject.AddComponent<Shadow>();
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
