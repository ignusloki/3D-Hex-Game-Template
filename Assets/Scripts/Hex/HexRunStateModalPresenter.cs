using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class HexRunStateModalPresenter : MonoBehaviour
{
    [Header("Layout")]
    [Min(260f)] [SerializeField] private float panelWidth = 680f;
    [Min(320f)] [SerializeField] private float panelHeight = 760f;
    [Min(900f)] [SerializeField] private float transitionPanelWidth = 1040f;
    [Min(620f)] [SerializeField] private float transitionPanelHeight = 724f;
    [Min(220f)] [SerializeField] private float transitionCardWidth = 240f;
    [Min(340f)] [SerializeField] private float transitionCardHeight = 380f;

    [Header("Colors")]
    [SerializeField] private Color overlayColor = new(0f, 0f, 0f, 0.76f);
    [SerializeField] private Color panelColor = new(0.08f, 0.11f, 0.15f, 0.96f);
    [SerializeField] private Color panelBorderColor = new(0.24f, 0.29f, 0.35f, 0.94f);
    [SerializeField] private Color titleColor = new(0.97f, 0.95f, 0.9f, 1f);
    [SerializeField] private Color metaColor = new(0.8f, 0.73f, 0.58f, 0.74f);
    [SerializeField] private Color bodyColor = new(0.86f, 0.89f, 0.92f, 0.92f);
    [SerializeField] private Color mutedBodyColor = new(0.73f, 0.78f, 0.83f, 0.78f);
    [SerializeField] private Color chipColor = new(0.12f, 0.16f, 0.21f, 0.94f);
    [SerializeField] private Color chipBorderColor = new(0.22f, 0.27f, 0.34f, 0.96f);
    [SerializeField] private Color buttonColor = new(0.12f, 0.15f, 0.19f, 0.96f);
    [SerializeField] private Color buttonHighlightColor = new(0.19f, 0.24f, 0.31f, 0.98f);
    [SerializeField] private Color buttonPressedColor = new(0.28f, 0.34f, 0.42f, 1f);
    [SerializeField] private Color buttonTextColor = new(0.96f, 0.96f, 0.96f, 1f);
    [SerializeField] private Color cardFrameColor = new(0.22f, 0.27f, 0.34f, 0.98f);
    [SerializeField] private Color cardSurfaceColor = new(0.12f, 0.15f, 0.2f, 0.98f);
    [SerializeField] private Color cardHoverColor = new(0.16f, 0.2f, 0.26f, 1f);
    [SerializeField] private Color cardPressedColor = new(0.22f, 0.27f, 0.34f, 1f);
    [SerializeField] private Color cardSelectedSurfaceColor = new(0.16f, 0.2f, 0.25f, 1f);
    [SerializeField] private Color cardSelectedFrameColor = new(0.78f, 0.67f, 0.42f, 1f);
    [SerializeField] private Color artPanelColor = new(0.11f, 0.14f, 0.18f, 0.98f);
    [SerializeField] private Color disabledCardColor = new(0.08f, 0.1f, 0.13f, 0.84f);

    private GameObject overlayRoot;
    private RectTransform panelRect;
    private RectTransform standardContentRect;
    private RectTransform transitionContentRect;
    private RectTransform transitionGrantChipContainerRect;
    private RectTransform transitionResourceChipContainerRect;
    private RectTransform optionsContentRect;
    private Image panelImage;
    private Text titleText;
    private Text bodyText;
    private Text transitionMetaText;
    private Text transitionTitleText;
    private Text transitionBodyText;
    private Text transitionGrantLabelText;
    private Text transitionGrantEmptyText;
    private Text transitionResourceLabelText;
    private Text selectionPromptText;
    private Button actionButton;
    private Text actionButtonText;
    private Font uiFont;
    private HexHudDocumentController hudDocumentController;
    private Action actionCallback;
    private Action<HexBoonDefinition> transitionActionCallback;
    private readonly List<TransitionBoonCardUi> transitionCards = new();
    private HexBoonDefinition[] transitionBoonOptions = Array.Empty<HexBoonDefinition>();
    private HexBoonDefinition selectedTransitionBoon;
    private bool isTransitionSelectionMode;

    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

    private sealed class TransitionBoonCardUi
    {
        public RectTransform RootRect;
        public LayoutElement LayoutElement;
        public Button Button;
        public Image FrameImage;
        public Image SurfaceImage;
        public Image ArtFrameImage;
        public Image ArtImage;
        public Text ArtFallbackText;
        public Text TitleText;
        public Text EffectText;
        public Text LoreText;
        public Image FamilyBadgeImage;
        public Text FamilyBadgeText;
    }

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

        SetModalVisibility(true);
        ApplyPanelMode(isTransitionLayout: true, hasCards: displayData.RequiresBoonSelection);
        transitionMetaText.text = BuildTransitionMetaLabel(displayData);
        transitionTitleText.text = string.IsNullOrWhiteSpace(displayData.Title) ? "Act Complete" : displayData.Title.Trim();
        transitionBodyText.text = string.IsNullOrWhiteSpace(displayData.Body)
            ? "The caravan gathers itself for the road ahead."
            : displayData.Body.Trim();
        transitionBodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(transitionBodyText.text));
        selectionPromptText.text = string.IsNullOrWhiteSpace(displayData.SelectionPrompt)
            ? "Choose one boon for the next act."
            : displayData.SelectionPrompt.Trim();
        selectionPromptText.gameObject.SetActive(!string.IsNullOrWhiteSpace(selectionPromptText.text));
        actionButtonText.text = string.IsNullOrWhiteSpace(displayData.ContinueButtonLabel)
            ? "Continue"
            : displayData.ContinueButtonLabel.Trim();
        actionButton.interactable = !displayData.RequiresBoonSelection;

        RebuildGrantSummary(displayData);
        RebuildCarryOverSummary(displayData.CurrentResources);
        EnsureTransitionCardCount(transitionBoonOptions.Length);
        RefreshTransitionCards();
    }

    public void Hide()
    {
        actionCallback = null;
        transitionActionCallback = null;
        transitionBoonOptions = Array.Empty<HexBoonDefinition>();
        selectedTransitionBoon = null;
        isTransitionSelectionMode = false;
        SetModalVisibility(false);
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
        SetModalVisibility(true);
        ApplyPanelMode(isTransitionLayout: false, hasCards: false);
        titleText.text = title;
        bodyText.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
        bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(bodyText.text));
        actionButtonText.text = string.IsNullOrWhiteSpace(buttonLabel) ? "Continue" : buttonLabel.Trim();
        actionButton.interactable = true;
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
        panelRect.sizeDelta = ResolveStandardPanelSize();
        panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.color = panelColor;
        Outline panelOutline = panelRect.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = panelBorderColor;
        panelOutline.effectDistance = new Vector2(1f, -1f);

        standardContentRect = CreateRectTransform("Standard Content", panelRect);
        standardContentRect.anchorMin = Vector2.zero;
        standardContentRect.anchorMax = Vector2.one;
        standardContentRect.offsetMin = new Vector2(34f, 104f);
        standardContentRect.offsetMax = new Vector2(-34f, -28f);
        VerticalLayoutGroup standardLayout = standardContentRect.gameObject.AddComponent<VerticalLayoutGroup>();
        standardLayout.padding = new RectOffset(0, 0, 0, 0);
        standardLayout.spacing = 14f;
        standardLayout.childAlignment = TextAnchor.UpperCenter;
        standardLayout.childControlHeight = true;
        standardLayout.childControlWidth = true;
        standardLayout.childForceExpandHeight = false;
        standardLayout.childForceExpandWidth = true;

        titleText = CreateText("Title", standardContentRect, 34, FontStyle.Bold, titleColor);
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 56f;
        titleText.alignment = TextAnchor.MiddleCenter;

        bodyText = CreateText("Body", standardContentRect, 22, FontStyle.Italic, bodyColor);
        LayoutElement bodyLayout = bodyText.gameObject.AddComponent<LayoutElement>();
        bodyLayout.flexibleHeight = 1f;
        bodyLayout.minHeight = 120f;
        bodyText.alignment = TextAnchor.UpperCenter;
        bodyText.lineSpacing = 1.12f;
        bodyText.gameObject.SetActive(false);

        transitionContentRect = CreateRectTransform("Transition Content", panelRect);
        transitionContentRect.anchorMin = Vector2.zero;
        transitionContentRect.anchorMax = Vector2.one;
        transitionContentRect.offsetMin = new Vector2(28f, 94f);
        transitionContentRect.offsetMax = new Vector2(-28f, -28f);
        VerticalLayoutGroup transitionLayout = transitionContentRect.gameObject.AddComponent<VerticalLayoutGroup>();
        transitionLayout.padding = new RectOffset(0, 0, 0, 0);
        transitionLayout.spacing = 16f;
        transitionLayout.childAlignment = TextAnchor.UpperCenter;
        transitionLayout.childControlHeight = true;
        transitionLayout.childControlWidth = true;
        transitionLayout.childForceExpandHeight = false;
        transitionLayout.childForceExpandWidth = true;
        transitionContentRect.gameObject.SetActive(false);

        RectTransform headerRect = CreateRectTransform("Transition Header", transitionContentRect);
        LayoutElement headerLayout = headerRect.gameObject.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 182f;
        VerticalLayoutGroup headerVerticalLayout = headerRect.gameObject.AddComponent<VerticalLayoutGroup>();
        headerVerticalLayout.padding = new RectOffset(0, 0, 0, 0);
        headerVerticalLayout.spacing = 12f;
        headerVerticalLayout.childAlignment = TextAnchor.UpperLeft;
        headerVerticalLayout.childControlHeight = true;
        headerVerticalLayout.childControlWidth = true;
        headerVerticalLayout.childForceExpandHeight = false;
        headerVerticalLayout.childForceExpandWidth = true;

        RectTransform topRowRect = CreateRectTransform("Transition Header Top Row", headerRect);
        LayoutElement topRowLayout = topRowRect.gameObject.AddComponent<LayoutElement>();
        topRowLayout.preferredHeight = 120f;
        HorizontalLayoutGroup topRowHorizontalLayout = topRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        topRowHorizontalLayout.padding = new RectOffset(0, 0, 0, 0);
        topRowHorizontalLayout.spacing = 24f;
        topRowHorizontalLayout.childAlignment = TextAnchor.UpperLeft;
        topRowHorizontalLayout.childControlHeight = true;
        topRowHorizontalLayout.childControlWidth = true;
        topRowHorizontalLayout.childForceExpandHeight = false;
        topRowHorizontalLayout.childForceExpandWidth = false;

        RectTransform headerCopyRect = CreateRectTransform("Transition Header Copy", topRowRect);
        LayoutElement headerCopyLayout = headerCopyRect.gameObject.AddComponent<LayoutElement>();
        headerCopyLayout.flexibleWidth = 1f;
        headerCopyLayout.minWidth = 440f;
        VerticalLayoutGroup headerCopyVerticalLayout = headerCopyRect.gameObject.AddComponent<VerticalLayoutGroup>();
        headerCopyVerticalLayout.padding = new RectOffset(0, 0, 0, 0);
        headerCopyVerticalLayout.spacing = 6f;
        headerCopyVerticalLayout.childAlignment = TextAnchor.UpperLeft;
        headerCopyVerticalLayout.childControlHeight = true;
        headerCopyVerticalLayout.childControlWidth = true;
        headerCopyVerticalLayout.childForceExpandHeight = false;
        headerCopyVerticalLayout.childForceExpandWidth = true;

        transitionMetaText = CreateText("Transition Meta", headerCopyRect, 11, FontStyle.Bold, metaColor);
        LayoutElement metaLayout = transitionMetaText.gameObject.AddComponent<LayoutElement>();
        metaLayout.preferredHeight = 16f;
        transitionMetaText.alignment = TextAnchor.MiddleLeft;

        transitionTitleText = CreateText("Transition Title", headerCopyRect, 32, FontStyle.Bold, titleColor);
        LayoutElement transitionTitleLayout = transitionTitleText.gameObject.AddComponent<LayoutElement>();
        transitionTitleLayout.preferredHeight = 42f;
        transitionTitleText.alignment = TextAnchor.UpperLeft;

        transitionBodyText = CreateText("Transition Body", headerCopyRect, 17, FontStyle.Normal, bodyColor);
        LayoutElement transitionBodyLayout = transitionBodyText.gameObject.AddComponent<LayoutElement>();
        transitionBodyLayout.preferredHeight = 56f;
        transitionBodyText.alignment = TextAnchor.UpperLeft;
        transitionBodyText.lineSpacing = 1.1f;
        transitionBodyText.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform resourceSummaryRect = CreateRectTransform("Transition Resource Summary", topRowRect);
        LayoutElement resourceSummaryLayout = resourceSummaryRect.gameObject.AddComponent<LayoutElement>();
        resourceSummaryLayout.preferredWidth = 236f;
        resourceSummaryLayout.minWidth = 236f;
        VerticalLayoutGroup resourceSummaryVerticalLayout = resourceSummaryRect.gameObject.AddComponent<VerticalLayoutGroup>();
        resourceSummaryVerticalLayout.padding = new RectOffset(0, 0, 0, 0);
        resourceSummaryVerticalLayout.spacing = 6f;
        resourceSummaryVerticalLayout.childAlignment = TextAnchor.UpperRight;
        resourceSummaryVerticalLayout.childControlHeight = true;
        resourceSummaryVerticalLayout.childControlWidth = true;
        resourceSummaryVerticalLayout.childForceExpandHeight = false;
        resourceSummaryVerticalLayout.childForceExpandWidth = false;

        transitionResourceLabelText = CreateText("Carry Over Label", resourceSummaryRect, 11, FontStyle.Bold, metaColor);
        LayoutElement resourceLabelLayout = transitionResourceLabelText.gameObject.AddComponent<LayoutElement>();
        resourceLabelLayout.preferredHeight = 16f;
        transitionResourceLabelText.alignment = TextAnchor.MiddleRight;
        transitionResourceLabelText.text = "Carry-over";

        transitionResourceChipContainerRect = CreateRectTransform("Carry Over Chips", resourceSummaryRect);
        LayoutElement resourceChipLayout = transitionResourceChipContainerRect.gameObject.AddComponent<LayoutElement>();
        resourceChipLayout.preferredHeight = 54f;
        HorizontalLayoutGroup resourceChipHorizontalLayout = transitionResourceChipContainerRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        resourceChipHorizontalLayout.padding = new RectOffset(0, 0, 0, 0);
        resourceChipHorizontalLayout.spacing = 8f;
        resourceChipHorizontalLayout.childAlignment = TextAnchor.UpperRight;
        resourceChipHorizontalLayout.childControlHeight = true;
        resourceChipHorizontalLayout.childControlWidth = true;
        resourceChipHorizontalLayout.childForceExpandHeight = false;
        resourceChipHorizontalLayout.childForceExpandWidth = false;

        RectTransform grantRowRect = CreateRectTransform("Transition Grant Row", headerRect);
        LayoutElement grantRowLayout = grantRowRect.gameObject.AddComponent<LayoutElement>();
        grantRowLayout.preferredHeight = 34f;
        HorizontalLayoutGroup grantRowHorizontalLayout = grantRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        grantRowHorizontalLayout.padding = new RectOffset(0, 0, 0, 0);
        grantRowHorizontalLayout.spacing = 12f;
        grantRowHorizontalLayout.childAlignment = TextAnchor.MiddleLeft;
        grantRowHorizontalLayout.childControlHeight = true;
        grantRowHorizontalLayout.childControlWidth = true;
        grantRowHorizontalLayout.childForceExpandHeight = false;
        grantRowHorizontalLayout.childForceExpandWidth = false;

        transitionGrantLabelText = CreateText("Grant Label", grantRowRect, 13, FontStyle.Bold, bodyColor);
        LayoutElement grantLabelLayout = transitionGrantLabelText.gameObject.AddComponent<LayoutElement>();
        grantLabelLayout.preferredWidth = 130f;
        transitionGrantLabelText.alignment = TextAnchor.MiddleLeft;
        transitionGrantLabelText.text = "Between-act grant";

        transitionGrantChipContainerRect = CreateRectTransform("Grant Chips", grantRowRect);
        LayoutElement grantChipLayout = transitionGrantChipContainerRect.gameObject.AddComponent<LayoutElement>();
        grantChipLayout.flexibleWidth = 1f;
        HorizontalLayoutGroup grantChipHorizontalLayout = transitionGrantChipContainerRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        grantChipHorizontalLayout.padding = new RectOffset(0, 0, 0, 0);
        grantChipHorizontalLayout.spacing = 8f;
        grantChipHorizontalLayout.childAlignment = TextAnchor.MiddleLeft;
        grantChipHorizontalLayout.childControlHeight = true;
        grantChipHorizontalLayout.childControlWidth = true;
        grantChipHorizontalLayout.childForceExpandHeight = false;
        grantChipHorizontalLayout.childForceExpandWidth = false;

        transitionGrantEmptyText = CreateText("Grant Empty", transitionGrantChipContainerRect, 13, FontStyle.Normal, mutedBodyColor);
        transitionGrantEmptyText.alignment = TextAnchor.MiddleLeft;
        transitionGrantEmptyText.text = "No between-act grant";
        transitionGrantEmptyText.gameObject.SetActive(false);

        selectionPromptText = CreateText("Selection Prompt", transitionContentRect, 16, FontStyle.Bold, bodyColor);
        LayoutElement selectionPromptLayout = selectionPromptText.gameObject.AddComponent<LayoutElement>();
        selectionPromptLayout.preferredHeight = 52f;
        selectionPromptText.alignment = TextAnchor.MiddleCenter;
        selectionPromptText.lineSpacing = 1.08f;
        selectionPromptText.verticalOverflow = VerticalWrapMode.Truncate;
        selectionPromptText.gameObject.SetActive(false);

        RectTransform cardRowRect = CreateRectTransform("Transition Card Row", transitionContentRect);
        LayoutElement cardRowLayout = cardRowRect.gameObject.AddComponent<LayoutElement>();
        cardRowLayout.preferredHeight = Mathf.Max(transitionCardHeight + 10f, 360f);
        HorizontalLayoutGroup cardRowHorizontalLayout = cardRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        cardRowHorizontalLayout.padding = new RectOffset(0, 0, 0, 0);
        cardRowHorizontalLayout.spacing = 24f;
        cardRowHorizontalLayout.childAlignment = TextAnchor.MiddleCenter;
        cardRowHorizontalLayout.childControlHeight = true;
        cardRowHorizontalLayout.childControlWidth = true;
        cardRowHorizontalLayout.childForceExpandHeight = false;
        cardRowHorizontalLayout.childForceExpandWidth = false;
        optionsContentRect = cardRowRect;

        RectTransform buttonRect = CreateRectTransform("Action Button", panelRect);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 26f);
        buttonRect.sizeDelta = new Vector2(172f, 44f);

        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = buttonColor;
        actionButton = buttonRect.gameObject.AddComponent<Button>();
        ColorBlock buttonColors = actionButton.colors;
        buttonColors.normalColor = buttonColor;
        buttonColors.highlightedColor = buttonHighlightColor;
        buttonColors.pressedColor = buttonPressedColor;
        buttonColors.selectedColor = buttonHighlightColor;
        buttonColors.disabledColor = buttonColor * 0.6f;
        buttonColors.fadeDuration = 0.08f;
        actionButton.colors = buttonColors;
        actionButton.onClick.AddListener(HandleActionClicked);

        Outline buttonOutline = buttonRect.gameObject.AddComponent<Outline>();
        buttonOutline.effectColor = new Color(0f, 0f, 0f, 0.2f);
        buttonOutline.effectDistance = new Vector2(1f, -1f);

        actionButtonText = CreateText("Action Button Text", buttonRect, 16, FontStyle.Bold, buttonTextColor);
        StretchToParent(actionButtonText.rectTransform, 10f, 6f);
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

    private void EnsureTransitionCardCount(int count)
    {
        if (optionsContentRect == null)
        {
            return;
        }

        while (transitionCards.Count < count)
        {
            int optionIndex = transitionCards.Count;
            TransitionBoonCardUi card = CreateTransitionCardUi(optionIndex);
            transitionCards.Add(card);
        }
    }

    private void RefreshTransitionCards()
    {
        bool hasOptions = transitionBoonOptions != null && transitionBoonOptions.Length > 0;
        if (optionsContentRect != null)
        {
            optionsContentRect.gameObject.SetActive(hasOptions);
        }

        for (int index = 0; index < transitionCards.Count; index++)
        {
            bool shouldBeVisible = hasOptions && index < transitionBoonOptions.Length;
            transitionCards[index].RootRect.gameObject.SetActive(shouldBeVisible);
            if (!shouldBeVisible)
            {
                continue;
            }

            HexBoonDefinition boon = transitionBoonOptions[index];
            PopulateTransitionCard(transitionCards[index], boon);
        }

        ApplyTransitionSelectionVisuals();
        Canvas.ForceUpdateCanvases();
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
        for (int index = 0; index < transitionCards.Count; index++)
        {
            TransitionBoonCardUi card = transitionCards[index];
            if (!card.RootRect.gameObject.activeSelf)
            {
                continue;
            }

            HexBoonDefinition option = transitionBoonOptions != null && index < transitionBoonOptions.Length
                ? transitionBoonOptions[index]
                : null;
            bool isSelected = selectedTransitionBoon != null
                && option != null
                && string.Equals(option.id, selectedTransitionBoon.id, StringComparison.OrdinalIgnoreCase);
            Color accentColor = option != null ? GetArchetypeAccent(option.archetypeFamily) : cardSelectedFrameColor;

            card.FrameImage.color = isSelected
                ? Color.Lerp(accentColor, cardSelectedFrameColor, 0.45f)
                : cardFrameColor;

            ColorBlock cardColors = card.Button.colors;
            if (isSelected)
            {
                cardColors.normalColor = cardSelectedSurfaceColor;
                cardColors.highlightedColor = Color.Lerp(cardSelectedSurfaceColor, Color.white, 0.08f);
                cardColors.pressedColor = Color.Lerp(cardSelectedSurfaceColor, Color.white, 0.14f);
                cardColors.selectedColor = cardColors.highlightedColor;
            }
            else
            {
                cardColors.normalColor = cardSurfaceColor;
                cardColors.highlightedColor = cardHoverColor;
                cardColors.pressedColor = cardPressedColor;
                cardColors.selectedColor = cardHoverColor;
            }

            card.Button.colors = cardColors;
            card.SurfaceImage.color = cardColors.normalColor;
            card.RootRect.localScale = isSelected ? Vector3.one * 1.03f : Vector3.one;
        }
    }

    private void ApplyPanelMode(bool isTransitionLayout, bool hasCards)
    {
        if (panelRect == null)
        {
            return;
        }

        panelRect.sizeDelta = isTransitionLayout
            ? ResolveTransitionPanelSize(hasCards)
            : ResolveStandardPanelSize();

        if (standardContentRect != null)
        {
            standardContentRect.gameObject.SetActive(!isTransitionLayout);
        }

        if (transitionContentRect != null)
        {
            transitionContentRect.gameObject.SetActive(isTransitionLayout);
        }

        if (!isTransitionLayout)
        {
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }

    private void RebuildGrantSummary(HexActTransitionDisplayData displayData)
    {
        ClearChildren(transitionGrantChipContainerRect);

        int chipCount = 0;
        chipCount += TryCreateGrantChip(transitionGrantChipContainerRect, "Food", displayData.BetweenActFood);
        chipCount += TryCreateGrantChip(transitionGrantChipContainerRect, "Morale", displayData.BetweenActMorale);
        chipCount += TryCreateGrantChip(transitionGrantChipContainerRect, "Gold", displayData.BetweenActGold);

        if (chipCount == 0)
        {
            transitionGrantEmptyText = CreateText("Grant Empty", transitionGrantChipContainerRect, 13, FontStyle.Normal, mutedBodyColor);
            transitionGrantEmptyText.alignment = TextAnchor.MiddleLeft;
            transitionGrantEmptyText.text = "No between-act grant";
        }
    }

    private void RebuildCarryOverSummary(CaravanResourceSnapshot currentResources)
    {
        ClearChildren(transitionResourceChipContainerRect);
        CreateResourceSummaryChip(transitionResourceChipContainerRect, "Food", currentResources.Food.ToString(), new Color(0.56f, 0.35f, 0.18f, 1f));
        CreateResourceSummaryChip(transitionResourceChipContainerRect, "Morale", currentResources.Morale.ToString(), new Color(0.51f, 0.21f, 0.24f, 1f));
        CreateResourceSummaryChip(transitionResourceChipContainerRect, "Gold", currentResources.Gold.ToString(), new Color(0.57f, 0.43f, 0.12f, 1f));
    }

    private TransitionBoonCardUi CreateTransitionCardUi(int optionIndex)
    {
        RectTransform cardRect = CreateRectTransform($"Transition Card {optionIndex + 1}", optionsContentRect);
        LayoutElement layoutElement = cardRect.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = transitionCardWidth;
        layoutElement.minWidth = transitionCardWidth;
        layoutElement.preferredHeight = transitionCardHeight;
        layoutElement.minHeight = transitionCardHeight;

        Image frameImage = cardRect.gameObject.AddComponent<Image>();
        frameImage.color = cardFrameColor;

        Button button = cardRect.gameObject.AddComponent<Button>();
        ColorBlock buttonColors = button.colors;
        buttonColors.normalColor = cardSurfaceColor;
        buttonColors.highlightedColor = cardHoverColor;
        buttonColors.pressedColor = cardPressedColor;
        buttonColors.selectedColor = cardHoverColor;
        buttonColors.disabledColor = disabledCardColor;
        buttonColors.fadeDuration = 0.08f;
        button.colors = buttonColors;
        int capturedIndex = optionIndex;
        button.onClick.AddListener(() => HandleTransitionOptionClicked(capturedIndex));

        RectTransform surfaceRect = CreateRectTransform("Surface", cardRect);
        StretchToParent(surfaceRect, 2f, 2f);
        Image surfaceImage = surfaceRect.gameObject.AddComponent<Image>();
        surfaceImage.color = cardSurfaceColor;
        button.targetGraphic = surfaceImage;

        VerticalLayoutGroup surfaceLayout = surfaceRect.gameObject.AddComponent<VerticalLayoutGroup>();
        surfaceLayout.padding = new RectOffset(16, 16, 16, 16);
        surfaceLayout.spacing = 10f;
        surfaceLayout.childAlignment = TextAnchor.UpperLeft;
        surfaceLayout.childControlHeight = true;
        surfaceLayout.childControlWidth = true;
        surfaceLayout.childForceExpandHeight = false;
        surfaceLayout.childForceExpandWidth = true;

        Text cardTitleText = CreateText("Title", surfaceRect, 20, FontStyle.Bold, titleColor);
        LayoutElement cardTitleLayout = cardTitleText.gameObject.AddComponent<LayoutElement>();
        cardTitleLayout.preferredHeight = 48f;
        cardTitleText.alignment = TextAnchor.UpperLeft;
        cardTitleText.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform artRect = CreateRectTransform("Art", surfaceRect);
        LayoutElement artLayout = artRect.gameObject.AddComponent<LayoutElement>();
        artLayout.preferredHeight = 156f;
        Image artFrameImage = artRect.gameObject.AddComponent<Image>();
        artFrameImage.color = artPanelColor;

        RectTransform artInsetRect = CreateRectTransform("Art Inset", artRect);
        StretchToParent(artInsetRect, 2f, 2f);
        Image artInsetImage = artInsetRect.gameObject.AddComponent<Image>();
        artInsetImage.color = new Color(0.09f, 0.11f, 0.14f, 0.98f);

        RectTransform artImageRect = CreateRectTransform("Art Sprite", artInsetRect);
        StretchToParent(artImageRect, 10f, 10f);
        Image artImage = artImageRect.gameObject.AddComponent<Image>();
        artImage.preserveAspect = true;
        artImage.color = Color.white;

        Text artFallbackText = CreateText("Art Placeholder", artInsetRect, 18, FontStyle.Bold, mutedBodyColor);
        StretchToParent(artFallbackText.rectTransform, 16f, 16f);
        artFallbackText.alignment = TextAnchor.MiddleCenter;
        artFallbackText.text = "Illustration";

        Text effectText = CreateText("Effect", surfaceRect, 15, FontStyle.Bold, bodyColor);
        LayoutElement effectLayout = effectText.gameObject.AddComponent<LayoutElement>();
        effectLayout.preferredHeight = 56f;
        effectText.alignment = TextAnchor.UpperLeft;
        effectText.verticalOverflow = VerticalWrapMode.Truncate;
        effectText.lineSpacing = 1.08f;

        Text loreText = CreateText("Lore", surfaceRect, 13, FontStyle.Italic, mutedBodyColor);
        LayoutElement loreLayout = loreText.gameObject.AddComponent<LayoutElement>();
        loreLayout.preferredHeight = 42f;
        loreText.alignment = TextAnchor.UpperLeft;
        loreText.verticalOverflow = VerticalWrapMode.Truncate;
        loreText.lineSpacing = 1.06f;

        RectTransform familyBadgeRect = CreateRectTransform("Family Badge", surfaceRect);
        LayoutElement familyBadgeLayout = familyBadgeRect.gameObject.AddComponent<LayoutElement>();
        familyBadgeLayout.preferredHeight = 28f;
        Image familyBadgeImage = familyBadgeRect.gameObject.AddComponent<Image>();
        familyBadgeImage.color = chipColor;

        Text familyBadgeText = CreateText("Family Badge Text", familyBadgeRect, 12, FontStyle.Bold, titleColor);
        StretchToParent(familyBadgeText.rectTransform, 10f, 5f);
        familyBadgeText.alignment = TextAnchor.MiddleCenter;

        return new TransitionBoonCardUi
        {
            RootRect = cardRect,
            LayoutElement = layoutElement,
            Button = button,
            FrameImage = frameImage,
            SurfaceImage = surfaceImage,
            ArtFrameImage = artFrameImage,
            ArtImage = artImage,
            ArtFallbackText = artFallbackText,
            TitleText = cardTitleText,
            EffectText = effectText,
            LoreText = loreText,
            FamilyBadgeImage = familyBadgeImage,
            FamilyBadgeText = familyBadgeText
        };
    }

    private void PopulateTransitionCard(TransitionBoonCardUi card, HexBoonDefinition boon)
    {
        if (card == null)
        {
            return;
        }

        if (boon == null)
        {
            card.Button.interactable = false;
            card.TitleText.text = "Missing Boon";
            card.EffectText.text = "No boon data is available.";
            card.LoreText.text = string.Empty;
            card.LoreText.gameObject.SetActive(false);
            card.FamilyBadgeText.text = "Unavailable";
            card.ArtImage.sprite = null;
            card.ArtImage.enabled = false;
            card.ArtFallbackText.text = "Missing";
            card.ArtFallbackText.gameObject.SetActive(true);
            card.FamilyBadgeImage.color = chipColor;
            return;
        }

        boon.Validate();
        card.Button.interactable = boon.isEnabled;
        Color accentColor = GetArchetypeAccent(boon.archetypeFamily);
        string effectText = string.IsNullOrWhiteSpace(boon.description)
            ? "No gameplay bonus described."
            : boon.description.Trim();
        string loreText = string.IsNullOrWhiteSpace(boon.flavorText)
            ? string.Empty
            : boon.flavorText.Trim();

        card.TitleText.text = boon.GetResolvedDisplayName();
        card.EffectText.text = effectText;
        card.LoreText.text = loreText;
        card.LoreText.gameObject.SetActive(!string.IsNullOrWhiteSpace(loreText));
        card.FamilyBadgeText.text = $"Nemesis Family: {FormatArchetype(boon.archetypeFamily)}";
        card.FamilyBadgeImage.color = Color.Lerp(accentColor, chipColor, 0.48f);
        card.ArtFrameImage.color = Color.Lerp(accentColor, artPanelColor, 0.72f);

        if (boon.icon != null)
        {
            card.ArtImage.sprite = boon.icon;
            card.ArtImage.enabled = true;
            card.ArtFallbackText.gameObject.SetActive(false);
        }
        else
        {
            card.ArtImage.sprite = null;
            card.ArtImage.enabled = false;
            card.ArtFallbackText.text = $"{FormatArchetype(boon.archetypeFamily)}\nFamily";
            card.ArtFallbackText.color = Color.Lerp(accentColor, Color.white, 0.3f);
            card.ArtFallbackText.gameObject.SetActive(true);
        }
    }

    private void SetModalVisibility(bool visible)
    {
        hudDocumentController ??= FindAnyObjectByType<HexHudDocumentController>();
        hudDocumentController?.SetGameplayModalState(visible);

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(visible);
        }
    }

    private int TryCreateGrantChip(Transform parent, string label, int value)
    {
        if (value == 0)
        {
            return 0;
        }

        RectTransform chipRect = CreateRectTransform($"{label} Grant Chip", parent);
        LayoutElement chipLayout = chipRect.gameObject.AddComponent<LayoutElement>();
        string chipCopy = $"{FormatSigned(value)} {label}";
        chipLayout.preferredWidth = Mathf.Max(84f, 28f + chipCopy.Length * 6f);
        chipLayout.preferredHeight = 26f;
        Image chipImage = chipRect.gameObject.AddComponent<Image>();
        chipImage.color = chipColor;

        Text chipText = CreateText("Text", chipRect, 12, FontStyle.Bold, titleColor);
        StretchToParent(chipText.rectTransform, 10f, 5f);
        chipText.alignment = TextAnchor.MiddleCenter;
        chipText.text = chipCopy;
        return 1;
    }

    private void CreateResourceSummaryChip(Transform parent, string label, string value, Color accentColor)
    {
        RectTransform chipRect = CreateRectTransform($"{label} Resource", parent);
        LayoutElement chipLayout = chipRect.gameObject.AddComponent<LayoutElement>();
        chipLayout.preferredWidth = 68f;
        chipLayout.preferredHeight = 48f;
        Image chipImage = chipRect.gameObject.AddComponent<Image>();
        chipImage.color = chipColor;
        Outline chipOutline = chipRect.gameObject.AddComponent<Outline>();
        chipOutline.effectColor = chipBorderColor;
        chipOutline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup chipLayoutGroup = chipRect.gameObject.AddComponent<VerticalLayoutGroup>();
        chipLayoutGroup.padding = new RectOffset(8, 8, 6, 6);
        chipLayoutGroup.spacing = 3f;
        chipLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        chipLayoutGroup.childControlHeight = true;
        chipLayoutGroup.childControlWidth = true;
        chipLayoutGroup.childForceExpandHeight = false;
        chipLayoutGroup.childForceExpandWidth = true;

        Text labelText = CreateText("Label", chipRect, 10, FontStyle.Bold, Color.Lerp(accentColor, Color.white, 0.38f));
        LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredHeight = 12f;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.text = label;

        Text valueText = CreateText("Value", chipRect, 13, FontStyle.Bold, titleColor);
        LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
        valueLayout.preferredHeight = 16f;
        valueText.alignment = TextAnchor.MiddleCenter;
        valueText.text = value;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(index).gameObject);
        }
    }

    private Vector2 ResolveStandardPanelSize()
    {
        return new Vector2(Mathf.Max(620f, panelWidth), Mathf.Clamp(panelHeight, 360f, 520f));
    }

    private Vector2 ResolveTransitionPanelSize(bool hasCards)
    {
        if (hasCards)
        {
            return new Vector2(Mathf.Max(980f, transitionPanelWidth), Mathf.Max(680f, transitionPanelHeight));
        }

        return new Vector2(Mathf.Max(760f, panelWidth), Mathf.Clamp(panelHeight, 420f, 560f));
    }

    private static string BuildTransitionMetaLabel(HexActTransitionDisplayData displayData)
    {
        if (displayData.CompletedActNumber > 0 && displayData.NextActNumber > 0)
        {
            return $"ROAD INTERMISSION · ACT {displayData.CompletedActNumber} TO ACT {displayData.NextActNumber}";
        }

        return "ROAD INTERMISSION";
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }

    private Color GetArchetypeAccent(HexNemesisArchetype archetype)
    {
        return archetype switch
        {
            HexNemesisArchetype.Hunter => new Color(0.65f, 0.47f, 0.22f, 1f),
            HexNemesisArchetype.Echo => new Color(0.33f, 0.55f, 0.7f, 1f),
            HexNemesisArchetype.Corruptor => new Color(0.57f, 0.27f, 0.35f, 1f),
            _ => new Color(0.42f, 0.46f, 0.52f, 1f)
        };
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
