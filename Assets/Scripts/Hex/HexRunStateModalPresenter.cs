using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class HexRunStateModalPresenter : MonoBehaviour
{
    [Header("Layout")]
    [Min(260f)] [SerializeField] private float panelWidth = 680f;
    [Min(320f)] [SerializeField] private float panelHeight = 620f;
    [Min(52f)] [SerializeField] private float transitionOptionHeight = 70f;

    [Header("Colors")]
    [SerializeField] private Color overlayColor = new(0f, 0f, 0f, 0.9f);
    [SerializeField] private Color titleColor = new(0.98f, 0.98f, 0.98f, 1f);
    [SerializeField] private Color bodyColor = new(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color buttonColor = new(0.12f, 0.15f, 0.19f, 0.96f);
    [SerializeField] private Color buttonHighlightColor = new(0.19f, 0.24f, 0.31f, 0.98f);
    [SerializeField] private Color buttonPressedColor = new(0.28f, 0.34f, 0.42f, 1f);
    [SerializeField] private Color buttonTextColor = new(0.96f, 0.96f, 0.96f, 1f);
    [SerializeField] private Color optionSelectedColor = new(0.24f, 0.31f, 0.4f, 1f);

    private GameObject overlayRoot;
    private RectTransform panelRect;
    private RectTransform bodyRect;
    private RectTransform selectionPromptRect;
    private RectTransform optionsContainerRect;
    private Text titleText;
    private Text bodyText;
    private Text selectionPromptText;
    private Button actionButton;
    private Text actionButtonText;
    private Font uiFont;
    private Action actionCallback;
    private Action<HexBoonDefinition> transitionActionCallback;
    private readonly List<Button> transitionOptionButtons = new();
    private readonly List<Image> transitionOptionButtonImages = new();
    private readonly List<Text> transitionOptionButtonTexts = new();
    private HexBoonDefinition[] transitionBoonOptions = Array.Empty<HexBoonDefinition>();
    private HexBoonDefinition selectedTransitionBoon;
    private bool isTransitionSelectionMode;

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
        ShowTransition(
            new HexActTransitionDisplayData(
                title,
                body,
                string.Empty,
                continueButtonLabel,
                Array.Empty<HexBoonDefinition>()),
            _ => onContinueRequested?.Invoke());
    }

    public void ShowTransition(HexActTransitionDisplayData displayData, Action<HexBoonDefinition> onContinueRequested)
    {
        EnsureUi();
        if (overlayRoot == null)
        {
            return;
        }

        transitionActionCallback = onContinueRequested;
        actionCallback = null;
        isTransitionSelectionMode = displayData.RequiresBoonSelection;
        transitionBoonOptions = displayData.BoonOptions ?? Array.Empty<HexBoonDefinition>();
        selectedTransitionBoon = null;

        overlayRoot.SetActive(true);
        titleText.text = displayData.Title;
        bodyText.text = string.IsNullOrWhiteSpace(displayData.Body) ? string.Empty : displayData.Body.Trim();
        bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(bodyText.text));
        selectionPromptText.text = string.IsNullOrWhiteSpace(displayData.SelectionPrompt)
            ? "Choose one boon for the next act."
            : displayData.SelectionPrompt.Trim();
        selectionPromptText.gameObject.SetActive(isTransitionSelectionMode && !string.IsNullOrWhiteSpace(selectionPromptText.text));
        actionButtonText.text = string.IsNullOrWhiteSpace(displayData.ContinueButtonLabel)
            ? "Continue"
            : displayData.ContinueButtonLabel.Trim();
        actionButton.interactable = !displayData.RequiresBoonSelection;

        ApplyBodyLayout(displayData.RequiresBoonSelection);
        EnsureTransitionOptionButtonCount(transitionBoonOptions.Length);
        RefreshTransitionOptionButtons();
    }

    public void Hide()
    {
        actionCallback = null;
        transitionActionCallback = null;
        transitionBoonOptions = Array.Empty<HexBoonDefinition>();
        selectedTransitionBoon = null;
        isTransitionSelectionMode = false;
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
        transitionActionCallback = null;
        transitionBoonOptions = Array.Empty<HexBoonDefinition>();
        selectedTransitionBoon = null;
        isTransitionSelectionMode = false;
        overlayRoot.SetActive(true);
        titleText.text = title;
        bodyText.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
        bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(bodyText.text));
        selectionPromptText.gameObject.SetActive(false);
        actionButtonText.text = string.IsNullOrWhiteSpace(buttonLabel) ? "Continue" : buttonLabel.Trim();
        actionButton.interactable = true;
        ApplyBodyLayout(false);
        RefreshTransitionOptionButtons();
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

        panelRect = CreateRectTransform("Run State Panel", overlayRect);
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
        titleRect.anchoredPosition = new Vector2(0f, -26f);
        titleRect.sizeDelta = new Vector2(panelWidth - 48f, 64f);
        titleText.alignment = TextAnchor.MiddleCenter;

        bodyText = CreateText("Body", panelRect, 22, FontStyle.Italic, bodyColor);
        bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyText.alignment = TextAnchor.UpperCenter;
        bodyText.lineSpacing = 1.15f;
        bodyText.gameObject.SetActive(false);

        selectionPromptText = CreateText("Selection Prompt", panelRect, 20, FontStyle.Bold, bodyColor);
        selectionPromptRect = selectionPromptText.rectTransform;
        selectionPromptRect.anchorMin = new Vector2(0.5f, 0f);
        selectionPromptRect.anchorMax = new Vector2(0.5f, 0f);
        selectionPromptRect.pivot = new Vector2(0.5f, 0f);
        selectionPromptRect.anchoredPosition = new Vector2(0f, 360f);
        selectionPromptRect.sizeDelta = new Vector2(panelWidth - 64f, 58f);
        selectionPromptText.alignment = TextAnchor.MiddleCenter;
        selectionPromptText.gameObject.SetActive(false);

        optionsContainerRect = CreateRectTransform("Transition Options", panelRect);
        optionsContainerRect.anchorMin = new Vector2(0f, 0f);
        optionsContainerRect.anchorMax = new Vector2(1f, 0f);
        optionsContainerRect.pivot = new Vector2(0.5f, 0f);
        optionsContainerRect.anchoredPosition = new Vector2(0f, 108f);
        optionsContainerRect.sizeDelta = new Vector2(-56f, 224f);
        VerticalLayoutGroup optionsLayout = optionsContainerRect.gameObject.AddComponent<VerticalLayoutGroup>();
        optionsLayout.padding = new RectOffset(0, 0, 0, 0);
        optionsLayout.spacing = 10f;
        optionsLayout.childAlignment = TextAnchor.UpperCenter;
        optionsLayout.childControlHeight = true;
        optionsLayout.childControlWidth = true;
        optionsLayout.childForceExpandHeight = false;
        optionsLayout.childForceExpandWidth = true;
        optionsContainerRect.gameObject.SetActive(false);

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
        if (isTransitionSelectionMode)
        {
            if (selectedTransitionBoon == null)
            {
                return;
            }

            Action<HexBoonDefinition> callback = transitionActionCallback;
            HexBoonDefinition selectedBoon = selectedTransitionBoon;
            Hide();
            callback?.Invoke(selectedBoon);
            return;
        }

        Action standardCallback = actionCallback;
        Hide();
        standardCallback?.Invoke();
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

    private void EnsureTransitionOptionButtonCount(int count)
    {
        if (optionsContainerRect == null)
        {
            return;
        }

        while (transitionOptionButtons.Count < count)
        {
            int optionIndex = transitionOptionButtons.Count;
            RectTransform optionRect = CreateRectTransform($"Transition Option {optionIndex + 1}", optionsContainerRect);
            LayoutElement layoutElement = optionRect.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = transitionOptionHeight;
            layoutElement.minHeight = transitionOptionHeight;

            Image optionImage = optionRect.gameObject.AddComponent<Image>();
            optionImage.color = buttonColor;

            Button optionButton = optionRect.gameObject.AddComponent<Button>();
            ColorBlock optionColors = optionButton.colors;
            optionColors.normalColor = buttonColor;
            optionColors.highlightedColor = buttonHighlightColor;
            optionColors.pressedColor = buttonPressedColor;
            optionColors.selectedColor = buttonHighlightColor;
            optionColors.disabledColor = buttonColor * 0.6f;
            optionButton.colors = optionColors;
            optionButton.onClick.AddListener(() => HandleTransitionOptionClicked(optionIndex));

            Text optionText = CreateText("Label", optionRect, 20, FontStyle.Normal, buttonTextColor);
            StretchToParent(optionText.rectTransform, 16f, 12f);
            optionText.alignment = TextAnchor.MiddleLeft;
            optionText.supportRichText = true;
            optionText.lineSpacing = 1.05f;

            transitionOptionButtons.Add(optionButton);
            transitionOptionButtonImages.Add(optionImage);
            transitionOptionButtonTexts.Add(optionText);
        }
    }

    private void RefreshTransitionOptionButtons()
    {
        bool hasOptions = transitionBoonOptions != null && transitionBoonOptions.Length > 0;
        if (optionsContainerRect != null)
        {
            optionsContainerRect.gameObject.SetActive(hasOptions);
        }

        for (int index = 0; index < transitionOptionButtons.Count; index++)
        {
            bool shouldBeVisible = hasOptions && index < transitionBoonOptions.Length;
            transitionOptionButtons[index].gameObject.SetActive(shouldBeVisible);
            if (!shouldBeVisible)
            {
                continue;
            }

            HexBoonDefinition boon = transitionBoonOptions[index];
            transitionOptionButtonTexts[index].text = FormatTransitionOptionLabel(boon);
        }

        ApplyTransitionSelectionVisuals();
    }

    private void HandleTransitionOptionClicked(int optionIndex)
    {
        if (transitionBoonOptions == null || optionIndex < 0 || optionIndex >= transitionBoonOptions.Length)
        {
            return;
        }

        selectedTransitionBoon = transitionBoonOptions[optionIndex];
        actionButton.interactable = selectedTransitionBoon != null;
        ApplyTransitionSelectionVisuals();
    }

    private void ApplyTransitionSelectionVisuals()
    {
        for (int index = 0; index < transitionOptionButtonImages.Count; index++)
        {
            if (!transitionOptionButtons[index].gameObject.activeSelf)
            {
                continue;
            }

            HexBoonDefinition option = transitionBoonOptions != null && index < transitionBoonOptions.Length
                ? transitionBoonOptions[index]
                : null;
            bool isSelected = selectedTransitionBoon != null
                && option != null
                && string.Equals(option.id, selectedTransitionBoon.id, StringComparison.OrdinalIgnoreCase);
            transitionOptionButtonImages[index].color = isSelected ? optionSelectedColor : buttonColor;
        }
    }

    private void ApplyBodyLayout(bool showTransitionOptions)
    {
        if (bodyRect == null)
        {
            return;
        }

        if (showTransitionOptions)
        {
            int optionCount = transitionBoonOptions != null ? transitionBoonOptions.Length : 0;
            float spacing = optionCount > 1 ? (optionCount - 1) * 10f : 0f;
            float optionsHeight = (optionCount * transitionOptionHeight) + spacing;
            float promptBottom = 108f + optionsHeight + 20f;
            optionsContainerRect.sizeDelta = new Vector2(-56f, optionsHeight);
            selectionPromptRect.anchoredPosition = new Vector2(0f, promptBottom);
            bodyRect.offsetMin = new Vector2(32f, promptBottom + 72f);
            bodyRect.offsetMax = new Vector2(-32f, -92f);
        }
        else
        {
            optionsContainerRect.sizeDelta = new Vector2(-56f, 224f);
            selectionPromptRect.anchoredPosition = new Vector2(0f, 360f);
            bodyRect.offsetMin = new Vector2(32f, 112f);
            bodyRect.offsetMax = new Vector2(-32f, -104f);
        }
    }

    private static string FormatTransitionOptionLabel(HexBoonDefinition boon)
    {
        if (boon == null)
        {
            return "<b>Missing Boon</b>";
        }

        boon.Validate();
        string description = string.IsNullOrWhiteSpace(boon.description)
            ? "No description available."
            : boon.description.Trim();
        return $"<b>{boon.GetResolvedDisplayName()}</b>\n<size=18>{description}</size>\n<size=16>Family: {FormatArchetype(boon.archetypeFamily)}</size>";
    }

    private static string FormatArchetype(HexNemesisArchetype archetype)
    {
        return archetype switch
        {
            HexNemesisArchetype.Hunter => "Hunter",
            HexNemesisArchetype.Echo => "Echo",
            HexNemesisArchetype.Corruptor => "Corruptor",
            _ => "None"
        };
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
