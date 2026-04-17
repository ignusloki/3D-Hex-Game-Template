using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class HexRunStateModalPresenter : MonoBehaviour
{
    [Header("Layout")]
    [Min(200f)] [SerializeField] private float panelWidth = 560f;
    [Min(180f)] [SerializeField] private float panelHeight = 360f;

    [Header("Colors")]
    [SerializeField] private Color overlayColor = new(0f, 0f, 0f, 0.9f);
    [SerializeField] private Color titleColor = new(0.98f, 0.98f, 0.98f, 1f);
    [SerializeField] private Color bodyColor = new(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color buttonColor = new(0.12f, 0.15f, 0.19f, 0.96f);
    [SerializeField] private Color buttonHighlightColor = new(0.19f, 0.24f, 0.31f, 0.98f);
    [SerializeField] private Color buttonPressedColor = new(0.28f, 0.34f, 0.42f, 1f);
    [SerializeField] private Color buttonTextColor = new(0.96f, 0.96f, 0.96f, 1f);

    private GameObject overlayRoot;
    private Text titleText;
    private Text bodyText;
    private Button actionButton;
    private Text actionButtonText;
    private Font uiFont;
    private Action actionCallback;

    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

    public void ShowVictory(Action onRetryRequested)
    {
        Show("You win!", string.Empty, "Retry", onRetryRequested);
    }

    public void ShowDefeat(Action onRetryRequested)
    {
        Show("Game over!", string.Empty, "Retry", onRetryRequested);
    }

    public void ShowTransition(string title, string body, string continueButtonLabel, Action onContinueRequested)
    {
        Show(title, body, continueButtonLabel, onContinueRequested);
    }

    public void Hide()
    {
        actionCallback = null;
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    private void Show(string title, string body, string buttonLabel, Action onActionRequested)
    {
        EnsureUi();
        if (overlayRoot == null)
        {
            return;
        }

        actionCallback = onActionRequested;
        overlayRoot.SetActive(true);
        titleText.text = title;
        bodyText.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
        bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(bodyText.text));
        actionButtonText.text = string.IsNullOrWhiteSpace(buttonLabel) ? "Continue" : buttonLabel.Trim();
    }

    private void EnsureUi()
    {
        if (overlayRoot != null)
        {
            return;
        }

        Canvas targetCanvas = FindAnyObjectByType<Canvas>();
        if (targetCanvas == null)
        {
            Debug.LogError("HexRunStateModalPresenter requires a Canvas in the scene.", this);
            return;
        }

        uiFont = ResolveFont();

        RectTransform overlayRect = CreateRectTransform("Run State Overlay", targetCanvas.transform);
        overlayRoot = overlayRect.gameObject;
        StretchToParent(overlayRect);

        Image overlayImage = overlayRoot.AddComponent<Image>();
        overlayImage.color = overlayColor;
        overlayImage.raycastTarget = true;
        overlayRoot.SetActive(false);

        RectTransform panelRect = CreateRectTransform("Run State Panel", overlayRect);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        titleText = CreateText("Title", panelRect, 34, FontStyle.Bold, titleColor);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);
        titleRect.sizeDelta = new Vector2(panelWidth - 48f, 64f);
        titleText.alignment = TextAnchor.MiddleCenter;

        bodyText = CreateText("Body", panelRect, 22, FontStyle.Italic, bodyColor);
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.offsetMin = new Vector2(32f, 104f);
        bodyRect.offsetMax = new Vector2(-32f, -104f);
        bodyText.alignment = TextAnchor.UpperCenter;
        bodyText.lineSpacing = 1.15f;
        bodyText.gameObject.SetActive(false);

        RectTransform buttonRect = CreateRectTransform("Action Button", panelRect);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 28f);
        buttonRect.sizeDelta = new Vector2(260f, 60f);

        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = buttonColor;
        actionButton = buttonRect.gameObject.AddComponent<Button>();
        ColorBlock buttonColors = actionButton.colors;
        buttonColors.normalColor = buttonColor;
        buttonColors.highlightedColor = buttonHighlightColor;
        buttonColors.pressedColor = buttonPressedColor;
        buttonColors.selectedColor = buttonHighlightColor;
        buttonColors.disabledColor = buttonColor * 0.6f;
        actionButton.colors = buttonColors;
        actionButton.onClick.AddListener(HandleActionClicked);

        actionButtonText = CreateText("Action Button Text", buttonRect, 24, FontStyle.Bold, buttonTextColor);
        StretchToParent(actionButtonText.rectTransform, 12f, 8f);
        actionButtonText.alignment = TextAnchor.MiddleCenter;
    }

    private void HandleActionClicked()
    {
        Action callback = actionCallback;
        Hide();
        callback?.Invoke();
    }

    private Font ResolveFont()
    {
        Text existingText = FindAnyObjectByType<Text>();
        if (existingText != null && existingText.font != null)
        {
            return existingText.font;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle, Color color)
    {
        RectTransform textRect = CreateRectTransform(name, parent);
        Text text = textRect.gameObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRectTransform(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = parent.gameObject.layer;
        return gameObject.GetComponent<RectTransform>();
    }

    private static void StretchToParent(RectTransform rectTransform, float horizontalPadding = 0f, float verticalPadding = 0f)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        rectTransform.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }
}

public sealed class HexMockQuestMarkerModalPresenter : MonoBehaviour
{
    [Header("Layout")]
    [Min(200f)] [SerializeField] private float panelWidth = 600f;
    [Min(140f)] [SerializeField] private float panelHeight = 380f;

    [Header("Colors")]
    [SerializeField] private Color overlayColor = new(0f, 0f, 0f, 0.88f);
    [SerializeField] private Color titleColor = new(0.98f, 0.98f, 0.98f, 1f);
    [SerializeField] private Color bodyColor = new(0.89f, 0.89f, 0.89f, 1f);
    [SerializeField] private Color buttonColor = new(0.12f, 0.15f, 0.19f, 0.96f);
    [SerializeField] private Color buttonHighlightColor = new(0.19f, 0.24f, 0.31f, 0.98f);
    [SerializeField] private Color buttonPressedColor = new(0.28f, 0.34f, 0.42f, 1f);
    [SerializeField] private Color buttonTextColor = new(0.96f, 0.96f, 0.96f, 1f);

    private GameObject overlayRoot;
    private Text titleText;
    private Text bodyText;
    private Button closeButton;
    private Text closeButtonText;
    private Font uiFont;
    private Action closeCallback;

    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

    public void Show(string title, string body, Action onCloseRequested)
    {
        EnsureUi();
        if (overlayRoot == null)
        {
            return;
        }

        closeCallback = onCloseRequested;
        overlayRoot.SetActive(true);
        titleText.text = string.IsNullOrWhiteSpace(title) ? "Quest Marker" : title.Trim();
        bodyText.text = string.IsNullOrWhiteSpace(body) ? "Mock quest marker." : body.Trim();
        closeButtonText.text = "Close";
    }

    public void Hide()
    {
        closeCallback = null;
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    private void EnsureUi()
    {
        if (overlayRoot != null)
        {
            return;
        }

        Canvas targetCanvas = FindAnyObjectByType<Canvas>();
        if (targetCanvas == null)
        {
            Debug.LogError("HexMockQuestMarkerModalPresenter requires a Canvas in the scene.", this);
            return;
        }

        uiFont = ResolveFont();

        RectTransform overlayRect = CreateRectTransform("Mock Quest Marker Overlay", targetCanvas.transform);
        overlayRoot = overlayRect.gameObject;
        StretchToParent(overlayRect);

        Image overlayImage = overlayRoot.AddComponent<Image>();
        overlayImage.color = overlayColor;
        overlayImage.raycastTarget = true;
        overlayRoot.SetActive(false);

        RectTransform panelRect = CreateRectTransform("Mock Quest Marker Panel", overlayRect);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        titleText = CreateText("Title", panelRect, 38, FontStyle.Bold, titleColor);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -34f);
        titleRect.sizeDelta = new Vector2(panelWidth - 72f, 56f);
        titleText.alignment = TextAnchor.MiddleCenter;

        bodyText = CreateText("Body", panelRect, 22, FontStyle.Italic, bodyColor);
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.offsetMin = new Vector2(36f, 112f);
        bodyRect.offsetMax = new Vector2(-36f, -108f);
        bodyText.alignment = TextAnchor.UpperCenter;

        RectTransform buttonRect = CreateRectTransform("Close Button", panelRect);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 36f);
        buttonRect.sizeDelta = new Vector2(220f, 56f);

        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = buttonColor;
        closeButton = buttonRect.gameObject.AddComponent<Button>();
        ColorBlock buttonColors = closeButton.colors;
        buttonColors.normalColor = buttonColor;
        buttonColors.highlightedColor = buttonHighlightColor;
        buttonColors.pressedColor = buttonPressedColor;
        buttonColors.selectedColor = buttonHighlightColor;
        buttonColors.disabledColor = buttonColor * 0.6f;
        closeButton.colors = buttonColors;
        closeButton.onClick.AddListener(HandleCloseClicked);

        closeButtonText = CreateText("Close Button Text", buttonRect, 24, FontStyle.Bold, buttonTextColor);
        StretchToParent(closeButtonText.rectTransform, 12f, 8f);
        closeButtonText.alignment = TextAnchor.MiddleCenter;
    }

    private void HandleCloseClicked()
    {
        Action callback = closeCallback;
        Hide();
        callback?.Invoke();
    }

    private Font ResolveFont()
    {
        Text existingText = FindAnyObjectByType<Text>();
        if (existingText != null && existingText.font != null)
        {
            return existingText.font;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle, Color color)
    {
        RectTransform textRect = CreateRectTransform(name, parent);
        Text text = textRect.gameObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRectTransform(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = parent.gameObject.layer;
        return gameObject.GetComponent<RectTransform>();
    }

    private static void StretchToParent(RectTransform rectTransform, float horizontalPadding = 0f, float verticalPadding = 0f)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        rectTransform.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }
}
