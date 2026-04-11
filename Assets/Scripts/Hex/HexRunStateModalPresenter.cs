using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class HexRunStateModalPresenter : MonoBehaviour
{
    [Header("Layout")]
    [Min(160f)] [SerializeField] private float panelWidth = 420f;
    [Min(120f)] [SerializeField] private float panelHeight = 220f;

    [Header("Colors")]
    [SerializeField] private Color overlayColor = new(0f, 0f, 0f, 0.9f);
    [SerializeField] private Color titleColor = new(0.98f, 0.98f, 0.98f, 1f);
    [SerializeField] private Color buttonColor = new(0.12f, 0.15f, 0.19f, 0.96f);
    [SerializeField] private Color buttonHighlightColor = new(0.19f, 0.24f, 0.31f, 0.98f);
    [SerializeField] private Color buttonPressedColor = new(0.28f, 0.34f, 0.42f, 1f);
    [SerializeField] private Color buttonTextColor = new(0.96f, 0.96f, 0.96f, 1f);

    private GameObject overlayRoot;
    private Text titleText;
    private Button retryButton;
    private Text retryButtonText;
    private Font uiFont;
    private Action retryCallback;

    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

    public void ShowVictory(Action onRetryRequested)
    {
        Show("You win!", onRetryRequested);
    }

    public void ShowDefeat(Action onRetryRequested)
    {
        Show("Game over!", onRetryRequested);
    }

    public void Hide()
    {
        retryCallback = null;
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    private void Show(string title, Action onRetryRequested)
    {
        EnsureUi();
        if (overlayRoot == null)
        {
            return;
        }

        retryCallback = onRetryRequested;
        overlayRoot.SetActive(true);
        titleText.text = title;
        retryButtonText.text = "Retry";
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

        titleText = CreateText("Title", panelRect, 42, FontStyle.Bold, titleColor);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -32f);
        titleRect.sizeDelta = new Vector2(panelWidth, 60f);
        titleText.alignment = TextAnchor.MiddleCenter;

        RectTransform buttonRect = CreateRectTransform("Retry Button", panelRect);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 32f);
        buttonRect.sizeDelta = new Vector2(220f, 56f);

        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = buttonColor;
        retryButton = buttonRect.gameObject.AddComponent<Button>();
        ColorBlock buttonColors = retryButton.colors;
        buttonColors.normalColor = buttonColor;
        buttonColors.highlightedColor = buttonHighlightColor;
        buttonColors.pressedColor = buttonPressedColor;
        buttonColors.selectedColor = buttonHighlightColor;
        buttonColors.disabledColor = buttonColor * 0.6f;
        retryButton.colors = buttonColors;
        retryButton.onClick.AddListener(HandleRetryClicked);

        retryButtonText = CreateText("Retry Button Text", buttonRect, 24, FontStyle.Bold, buttonTextColor);
        StretchToParent(retryButtonText.rectTransform, 12f, 8f);
        retryButtonText.alignment = TextAnchor.MiddleCenter;
    }

    private void HandleRetryClicked()
    {
        Action callback = retryCallback;
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
