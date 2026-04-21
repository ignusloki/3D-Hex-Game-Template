using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class HexRunStateModalPresenter : MonoBehaviour
{
    private enum TransitionScreenMode
    {
        None,
        Intermission,
        Selection
    }

    [Header("Layout")]
    [Min(260f)] [SerializeField] private float panelWidth = 680f;
    [Min(320f)] [SerializeField] private float panelHeight = 760f;
    [Min(640f)] [SerializeField] private float transitionIntermissionPanelWidth = 840f;
    [Min(520f)] [SerializeField] private float transitionIntermissionPanelHeight = 760f;
    [Min(820f)] [SerializeField] private float transitionSelectionPanelWidth = 980f;
    [Min(560f)] [SerializeField] private float transitionSelectionPanelHeight = 660f;
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
    [SerializeField] private Color journalPageColor = new(0.81f, 0.78f, 0.71f, 0.98f);
    [SerializeField] private Color journalPageBorderColor = new(0.45f, 0.39f, 0.31f, 0.7f);
    [SerializeField] private Color journalTitleColor = new(0.16f, 0.18f, 0.2f, 1f);
    [SerializeField] private Color journalMetaColor = new(0.43f, 0.34f, 0.22f, 0.94f);
    [SerializeField] private Color journalBodyColor = new(0.19f, 0.21f, 0.24f, 0.96f);
    [SerializeField] private Color journalMutedBodyColor = new(0.31f, 0.33f, 0.36f, 0.82f);
    [SerializeField] private Color journalDividerColor = new(0.54f, 0.47f, 0.37f, 0.42f);
    [SerializeField] private Color journalImageFrameColor = new(0.63f, 0.58f, 0.49f, 0.62f);
    [SerializeField] private Color journalImageInsetColor = new(0.68f, 0.64f, 0.57f, 0.92f);
    [SerializeField] private Color journalChipColor = new(0.16f, 0.19f, 0.23f, 0.95f);
    [SerializeField] private Color journalChipBorderColor = new(0.36f, 0.31f, 0.24f, 0.72f);
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
    private RectTransform transitionIntermissionRect;
    private RectTransform transitionIntermissionContentRect;
    private RectTransform transitionSelectionRect;
    private RectTransform transitionGrantChipContainerRect;
    private RectTransform transitionResourceChipContainerRect;
    private RectTransform transitionSelectionResourceChipContainerRect;
    private RectTransform transitionSelectionOptionsRect;
    private RectTransform transitionSelectionDetailRect;
    private Image panelImage;
    private Image transitionIllustrationImage;
    private Text titleText;
    private Text bodyText;
    private Text transitionMetaText;
    private Text transitionTitleText;
    private Text transitionBodyText;
    private Text transitionSummaryLabelText;
    private Text transitionGrantLabelText;
    private Text transitionGrantEmptyText;
    private Text transitionResourceLabelText;
    private Text selectionPromptText;
    private Text selectionContextNoteText;
    private Text transitionIllustrationFallbackText;
    private Text transitionSelectionDetailMetaText;
    private Text transitionSelectionDetailTitleText;
    private Text transitionSelectionDetailEffectText;
    private Text transitionSelectionDetailLoreText;
    private Button actionButton;
    private Text actionButtonText;
    private Font uiFont;
    private HexHudDocumentController hudDocumentController;
    private Action actionCallback;
    private Action<HexBoonDefinition> transitionActionCallback;
    private readonly List<TransitionBoonCardUi> transitionCards = new();
    private HexActTransitionDisplayData activeTransitionDisplayData;
    private HexBoonDefinition[] transitionBoonOptions = Array.Empty<HexBoonDefinition>();
    private HexBoonDefinition selectedTransitionBoon;
    private HexBoonDefinition hoveredTransitionBoon;
    private TransitionScreenMode transitionScreenMode;

    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

    private sealed class TransitionBoonCardUi
    {
        public RectTransform RootRect;
        public LayoutElement LayoutElement;
        public RectTransform MotionRect;
        public Button Button;
        public Image FrameImage;
        public Image SurfaceImage;
        public Image ArtFrameImage;
        public Image ArtImage;
        public Text ArtFallbackText;
        public Text TitleText;
        public Text EffectText;
        public Image FamilyBadgeImage;
        public Text FamilyBadgeText;
        public bool IsHovered;
        public float TargetScale = 1f;
        public float TargetLift;
    }

    private void Update()
    {
        AnimateTransitionCards();
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
                string.Empty,
                continueButtonLabel,
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
        activeTransitionDisplayData = displayData;
        transitionBoonOptions = displayData.BoonOptions ?? Array.Empty<HexBoonDefinition>();
        selectedTransitionBoon = null;
        hoveredTransitionBoon = null;
        transitionScreenMode = TransitionScreenMode.None;

        SetModalVisibility(true);
        ApplyPanelMode(isTransitionLayout: true);
        RefreshTransitionStaticContent();
        EnsureTransitionCardCount(transitionBoonOptions.Length);
        RefreshTransitionCards();
        ShowTransitionScreen(TransitionScreenMode.Intermission);
    }

    public void Hide()
    {
        actionCallback = null;
        transitionActionCallback = null;
        activeTransitionDisplayData = default;
        transitionBoonOptions = Array.Empty<HexBoonDefinition>();
        selectedTransitionBoon = null;
        hoveredTransitionBoon = null;
        transitionScreenMode = TransitionScreenMode.None;
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
        activeTransitionDisplayData = default;
        transitionBoonOptions = Array.Empty<HexBoonDefinition>();
        selectedTransitionBoon = null;
        hoveredTransitionBoon = null;
        transitionScreenMode = TransitionScreenMode.None;
        SetModalVisibility(true);
        ApplyPanelMode(isTransitionLayout: false);
        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(true);
        }

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

        Canvas modalCanvas = overlayRoot.AddComponent<Canvas>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = 250;
        overlayRoot.AddComponent<GraphicRaycaster>();

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
        StretchToParent(standardContentRect, 34f, 28f);
        standardContentRect.offsetMin = new Vector2(34f, 104f);
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
        transitionContentRect.offsetMin = new Vector2(30f, 82f);
        transitionContentRect.offsetMax = new Vector2(-30f, -24f);
        transitionContentRect.gameObject.SetActive(false);

        BuildTransitionIntermissionUi();
        BuildTransitionSelectionUi();

        RectTransform buttonRect = CreateRectTransform("Action Button", panelRect);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 22f);
        buttonRect.sizeDelta = new Vector2(184f, 42f);

        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = buttonColor;
        actionButton = buttonRect.gameObject.AddComponent<Button>();
        actionButton.targetGraphic = buttonImage;
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

    private void BuildTransitionIntermissionUi()
    {
        transitionIntermissionRect = CreateRectTransform("Transition Intermission Screen", transitionContentRect);
        StretchToParent(transitionIntermissionRect);
        transitionIntermissionRect.gameObject.SetActive(false);

        transitionIntermissionContentRect = CreateRectTransform("Transition Intermission Content", transitionIntermissionRect);
        StretchToParent(transitionIntermissionContentRect);
        Image pageImage = transitionIntermissionContentRect.gameObject.AddComponent<Image>();
        pageImage.color = journalPageColor;
        Outline pageOutline = transitionIntermissionContentRect.gameObject.AddComponent<Outline>();
        pageOutline.effectColor = journalPageBorderColor;
        pageOutline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup intermissionLayout = transitionIntermissionContentRect.gameObject.AddComponent<VerticalLayoutGroup>();
        intermissionLayout.padding = new RectOffset(52, 52, 38, 34);
        intermissionLayout.spacing = 16f;
        intermissionLayout.childAlignment = TextAnchor.UpperCenter;
        intermissionLayout.childControlHeight = true;
        intermissionLayout.childControlWidth = true;
        intermissionLayout.childForceExpandHeight = false;
        intermissionLayout.childForceExpandWidth = true;

        transitionMetaText = CreateText("Transition Meta", transitionIntermissionContentRect, 12, FontStyle.Bold, journalMetaColor);
        LayoutElement metaLayout = transitionMetaText.gameObject.AddComponent<LayoutElement>();
        metaLayout.preferredHeight = 18f;
        transitionMetaText.alignment = TextAnchor.MiddleCenter;

        transitionTitleText = CreateText("Transition Title", transitionIntermissionContentRect, 36, FontStyle.Bold, journalTitleColor);
        LayoutElement transitionTitleLayout = transitionTitleText.gameObject.AddComponent<LayoutElement>();
        transitionTitleLayout.preferredHeight = 46f;
        transitionTitleText.alignment = TextAnchor.MiddleCenter;

        CreateDivider("Transition Header Divider", transitionIntermissionContentRect, journalDividerColor);

        RectTransform narrativeBlockRect = CreateRectTransform("Transition Narrative Block", transitionIntermissionContentRect);
        LayoutElement narrativeBlockLayout = narrativeBlockRect.gameObject.AddComponent<LayoutElement>();
        narrativeBlockLayout.preferredHeight = 84f;
        HorizontalLayoutGroup narrativeBlockLayoutGroup = narrativeBlockRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        narrativeBlockLayoutGroup.padding = new RectOffset(42, 42, 0, 0);
        narrativeBlockLayoutGroup.spacing = 0;
        narrativeBlockLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        narrativeBlockLayoutGroup.childControlHeight = true;
        narrativeBlockLayoutGroup.childControlWidth = true;
        narrativeBlockLayoutGroup.childForceExpandHeight = false;
        narrativeBlockLayoutGroup.childForceExpandWidth = true;

        transitionBodyText = CreateText("Transition Body", narrativeBlockRect, 17, FontStyle.Normal, journalBodyColor);
        LayoutElement transitionBodyLayout = transitionBodyText.gameObject.AddComponent<LayoutElement>();
        transitionBodyLayout.preferredHeight = 84f;
        transitionBodyText.alignment = TextAnchor.UpperLeft;
        transitionBodyText.lineSpacing = 1.12f;
        transitionBodyText.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform illustrationRect = CreateRectTransform("Transition Illustration", transitionIntermissionContentRect);
        LayoutElement illustrationLayout = illustrationRect.gameObject.AddComponent<LayoutElement>();
        illustrationLayout.preferredHeight = 236f;
        Image illustrationFrame = illustrationRect.gameObject.AddComponent<Image>();
        illustrationFrame.color = journalImageFrameColor;
        Outline illustrationOutline = illustrationRect.gameObject.AddComponent<Outline>();
        illustrationOutline.effectColor = journalPageBorderColor;
        illustrationOutline.effectDistance = new Vector2(1f, -1f);

        RectTransform illustrationInsetRect = CreateRectTransform("Transition Illustration Inset", illustrationRect);
        StretchToParent(illustrationInsetRect, 10f, 10f);
        Image illustrationInset = illustrationInsetRect.gameObject.AddComponent<Image>();
        illustrationInset.color = journalImageInsetColor;

        RectTransform illustrationImageRect = CreateRectTransform("Transition Illustration Image", illustrationInsetRect);
        StretchToParent(illustrationImageRect, 12f, 12f);
        transitionIllustrationImage = illustrationImageRect.gameObject.AddComponent<Image>();
        transitionIllustrationImage.preserveAspect = true;
        transitionIllustrationImage.color = new Color(1f, 1f, 1f, 0.96f);

        transitionIllustrationFallbackText = CreateText("Transition Illustration Placeholder", illustrationInsetRect, 16, FontStyle.Italic, journalMutedBodyColor);
        StretchToParent(transitionIllustrationFallbackText.rectTransform, 24f, 24f);
        transitionIllustrationFallbackText.alignment = TextAnchor.MiddleCenter;
        transitionIllustrationFallbackText.text = "placeholder";

        CreateDivider("Transition Summary Divider", transitionIntermissionContentRect, journalDividerColor);

        transitionSummaryLabelText = CreateText("Transition Summary Label", transitionIntermissionContentRect, 12, FontStyle.Bold, journalMetaColor);
        LayoutElement summaryLabelLayout = transitionSummaryLabelText.gameObject.AddComponent<LayoutElement>();
        summaryLabelLayout.preferredHeight = 18f;
        transitionSummaryLabelText.alignment = TextAnchor.MiddleLeft;
        transitionSummaryLabelText.text = "Travel provisions";

        RectTransform carryOverRowRect = CreateRectTransform("Carry Over Row", transitionIntermissionContentRect);
        LayoutElement carryOverRowLayout = carryOverRowRect.gameObject.AddComponent<LayoutElement>();
        carryOverRowLayout.preferredHeight = 30f;
        HorizontalLayoutGroup carryOverRowLayoutGroup = carryOverRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        carryOverRowLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        carryOverRowLayoutGroup.spacing = 12f;
        carryOverRowLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
        carryOverRowLayoutGroup.childControlHeight = true;
        carryOverRowLayoutGroup.childControlWidth = true;
        carryOverRowLayoutGroup.childForceExpandHeight = false;
        carryOverRowLayoutGroup.childForceExpandWidth = false;

        transitionResourceLabelText = CreateText("Carry Over Label", carryOverRowRect, 13, FontStyle.Bold, journalBodyColor);
        LayoutElement resourceLabelLayout = transitionResourceLabelText.gameObject.AddComponent<LayoutElement>();
        resourceLabelLayout.preferredWidth = 136f;
        resourceLabelLayout.preferredHeight = 20f;
        transitionResourceLabelText.alignment = TextAnchor.MiddleLeft;
        transitionResourceLabelText.text = "Carry-over";

        transitionResourceChipContainerRect = CreateRectTransform("Carry Over Chips", carryOverRowRect);
        LayoutElement resourceChipLayout = transitionResourceChipContainerRect.gameObject.AddComponent<LayoutElement>();
        resourceChipLayout.preferredHeight = 28f;
        HorizontalLayoutGroup resourceChipLayoutGroup = transitionResourceChipContainerRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        resourceChipLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        resourceChipLayoutGroup.spacing = 8f;
        resourceChipLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
        resourceChipLayoutGroup.childControlHeight = true;
        resourceChipLayoutGroup.childControlWidth = true;
        resourceChipLayoutGroup.childForceExpandHeight = false;
        resourceChipLayoutGroup.childForceExpandWidth = false;

        RectTransform grantRowRect = CreateRectTransform("Grant Row", transitionIntermissionContentRect);
        LayoutElement grantRowLayout = grantRowRect.gameObject.AddComponent<LayoutElement>();
        grantRowLayout.preferredHeight = 30f;
        HorizontalLayoutGroup grantRowLayoutGroup = grantRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        grantRowLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        grantRowLayoutGroup.spacing = 12f;
        grantRowLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
        grantRowLayoutGroup.childControlHeight = true;
        grantRowLayoutGroup.childControlWidth = true;
        grantRowLayoutGroup.childForceExpandHeight = false;
        grantRowLayoutGroup.childForceExpandWidth = false;

        transitionGrantLabelText = CreateText("Grant Label", grantRowRect, 13, FontStyle.Bold, journalBodyColor);
        LayoutElement grantLabelLayout = transitionGrantLabelText.gameObject.AddComponent<LayoutElement>();
        grantLabelLayout.preferredWidth = 136f;
        grantLabelLayout.preferredHeight = 20f;
        transitionGrantLabelText.alignment = TextAnchor.MiddleLeft;
        transitionGrantLabelText.text = "Fresh supplies";

        transitionGrantChipContainerRect = CreateRectTransform("Grant Chips", grantRowRect);
        LayoutElement grantChipLayout = transitionGrantChipContainerRect.gameObject.AddComponent<LayoutElement>();
        grantChipLayout.preferredHeight = 28f;
        HorizontalLayoutGroup grantChipLayoutGroup = transitionGrantChipContainerRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        grantChipLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        grantChipLayoutGroup.spacing = 8f;
        grantChipLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
        grantChipLayoutGroup.childControlHeight = true;
        grantChipLayoutGroup.childControlWidth = true;
        grantChipLayoutGroup.childForceExpandHeight = false;
        grantChipLayoutGroup.childForceExpandWidth = false;

        transitionGrantEmptyText = CreateText("Grant Empty", grantRowRect, 12, FontStyle.Italic, journalMutedBodyColor);
        LayoutElement grantEmptyLayout = transitionGrantEmptyText.gameObject.AddComponent<LayoutElement>();
        grantEmptyLayout.preferredHeight = 18f;
        transitionGrantEmptyText.alignment = TextAnchor.MiddleLeft;
        transitionGrantEmptyText.text = "No fresh provisions";
        transitionGrantEmptyText.gameObject.SetActive(false);
    }

    private void BuildTransitionSelectionUi()
    {
        transitionSelectionRect = CreateRectTransform("Transition Selection Screen", transitionContentRect);
        StretchToParent(transitionSelectionRect);
        VerticalLayoutGroup selectionLayout = transitionSelectionRect.gameObject.AddComponent<VerticalLayoutGroup>();
        selectionLayout.padding = new RectOffset(0, 0, 0, 0);
        selectionLayout.spacing = 16f;
        selectionLayout.childAlignment = TextAnchor.UpperCenter;
        selectionLayout.childControlHeight = true;
        selectionLayout.childControlWidth = true;
        selectionLayout.childForceExpandHeight = false;
        selectionLayout.childForceExpandWidth = true;
        transitionSelectionRect.gameObject.SetActive(false);

        RectTransform selectionHeaderRect = CreateRectTransform("Selection Header", transitionSelectionRect);
        LayoutElement selectionHeaderLayout = selectionHeaderRect.gameObject.AddComponent<LayoutElement>();
        selectionHeaderLayout.preferredHeight = 70f;
        HorizontalLayoutGroup selectionHeaderLayoutGroup = selectionHeaderRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        selectionHeaderLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        selectionHeaderLayoutGroup.spacing = 16f;
        selectionHeaderLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        selectionHeaderLayoutGroup.childControlHeight = true;
        selectionHeaderLayoutGroup.childControlWidth = true;
        selectionHeaderLayoutGroup.childForceExpandHeight = false;
        selectionHeaderLayoutGroup.childForceExpandWidth = false;

        RectTransform selectionCopyRect = CreateRectTransform("Selection Copy", selectionHeaderRect);
        LayoutElement selectionCopyLayout = selectionCopyRect.gameObject.AddComponent<LayoutElement>();
        selectionCopyLayout.flexibleWidth = 1f;
        VerticalLayoutGroup selectionCopyLayoutGroup = selectionCopyRect.gameObject.AddComponent<VerticalLayoutGroup>();
        selectionCopyLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        selectionCopyLayoutGroup.spacing = 4f;
        selectionCopyLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        selectionCopyLayoutGroup.childControlHeight = true;
        selectionCopyLayoutGroup.childControlWidth = true;
        selectionCopyLayoutGroup.childForceExpandHeight = false;
        selectionCopyLayoutGroup.childForceExpandWidth = true;

        selectionPromptText = CreateText("Selection Prompt", selectionCopyRect, 18, FontStyle.Bold, titleColor);
        LayoutElement selectionPromptLayout = selectionPromptText.gameObject.AddComponent<LayoutElement>();
        selectionPromptLayout.preferredHeight = 26f;
        selectionPromptText.alignment = TextAnchor.MiddleLeft;
        selectionPromptText.verticalOverflow = VerticalWrapMode.Truncate;

        selectionContextNoteText = CreateText("Selection Note", selectionCopyRect, 13, FontStyle.Normal, mutedBodyColor);
        LayoutElement selectionNoteLayout = selectionContextNoteText.gameObject.AddComponent<LayoutElement>();
        selectionNoteLayout.preferredHeight = 18f;
        selectionContextNoteText.alignment = TextAnchor.MiddleLeft;
        selectionContextNoteText.verticalOverflow = VerticalWrapMode.Truncate;
        selectionContextNoteText.gameObject.SetActive(false);

        RectTransform selectionResourcePanelRect = CreateInfoSurface("Selection Resource Summary", selectionHeaderRect);
        LayoutElement selectionResourcePanelLayout = selectionResourcePanelRect.GetComponent<LayoutElement>();
        selectionResourcePanelLayout.preferredWidth = 240f;
        selectionResourcePanelLayout.minWidth = 240f;
        selectionResourcePanelLayout.preferredHeight = 58f;
        VerticalLayoutGroup selectionResourceLayout = selectionResourcePanelRect.gameObject.AddComponent<VerticalLayoutGroup>();
        selectionResourceLayout.padding = new RectOffset(12, 12, 10, 10);
        selectionResourceLayout.spacing = 6f;
        selectionResourceLayout.childAlignment = TextAnchor.UpperCenter;
        selectionResourceLayout.childControlHeight = true;
        selectionResourceLayout.childControlWidth = true;
        selectionResourceLayout.childForceExpandHeight = false;
        selectionResourceLayout.childForceExpandWidth = true;

        Text selectionResourceLabelText = CreateText("Selection Carry Over Label", selectionResourcePanelRect, 10, FontStyle.Bold, metaColor);
        LayoutElement selectionResourceLabelLayout = selectionResourceLabelText.gameObject.AddComponent<LayoutElement>();
        selectionResourceLabelLayout.preferredHeight = 14f;
        selectionResourceLabelText.alignment = TextAnchor.MiddleCenter;
        selectionResourceLabelText.text = "Carry-over";

        transitionSelectionResourceChipContainerRect = CreateRectTransform("Selection Carry Over Chips", selectionResourcePanelRect);
        LayoutElement selectionResourceChipLayout = transitionSelectionResourceChipContainerRect.gameObject.AddComponent<LayoutElement>();
        selectionResourceChipLayout.preferredHeight = 28f;
        HorizontalLayoutGroup selectionResourceChipLayoutGroup = transitionSelectionResourceChipContainerRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        selectionResourceChipLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        selectionResourceChipLayoutGroup.spacing = 6f;
        selectionResourceChipLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        selectionResourceChipLayoutGroup.childControlHeight = true;
        selectionResourceChipLayoutGroup.childControlWidth = true;
        selectionResourceChipLayoutGroup.childForceExpandHeight = false;
        selectionResourceChipLayoutGroup.childForceExpandWidth = false;

        transitionSelectionOptionsRect = CreateRectTransform("Transition Card Row", transitionSelectionRect);
        LayoutElement cardRowLayout = transitionSelectionOptionsRect.gameObject.AddComponent<LayoutElement>();
        cardRowLayout.preferredHeight = Mathf.Max(transitionCardHeight + 12f, 360f);
        HorizontalLayoutGroup cardRowLayoutGroup = transitionSelectionOptionsRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        cardRowLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        cardRowLayoutGroup.spacing = 20f;
        cardRowLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        cardRowLayoutGroup.childControlHeight = true;
        cardRowLayoutGroup.childControlWidth = true;
        cardRowLayoutGroup.childForceExpandHeight = false;
        cardRowLayoutGroup.childForceExpandWidth = false;

        transitionSelectionDetailRect = CreateInfoSurface("Transition Detail Panel", transitionSelectionRect);
        LayoutElement detailLayout = transitionSelectionDetailRect.GetComponent<LayoutElement>();
        detailLayout.preferredHeight = 112f;
        VerticalLayoutGroup detailLayoutGroup = transitionSelectionDetailRect.gameObject.AddComponent<VerticalLayoutGroup>();
        detailLayoutGroup.padding = new RectOffset(18, 18, 16, 16);
        detailLayoutGroup.spacing = 4f;
        detailLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        detailLayoutGroup.childControlHeight = true;
        detailLayoutGroup.childControlWidth = true;
        detailLayoutGroup.childForceExpandHeight = false;
        detailLayoutGroup.childForceExpandWidth = true;

        transitionSelectionDetailMetaText = CreateText("Transition Detail Meta", transitionSelectionDetailRect, 11, FontStyle.Bold, metaColor);
        LayoutElement detailMetaLayout = transitionSelectionDetailMetaText.gameObject.AddComponent<LayoutElement>();
        detailMetaLayout.preferredHeight = 14f;
        transitionSelectionDetailMetaText.alignment = TextAnchor.MiddleLeft;

        transitionSelectionDetailTitleText = CreateText("Transition Detail Title", transitionSelectionDetailRect, 20, FontStyle.Bold, titleColor);
        LayoutElement detailTitleLayout = transitionSelectionDetailTitleText.gameObject.AddComponent<LayoutElement>();
        detailTitleLayout.preferredHeight = 24f;
        transitionSelectionDetailTitleText.alignment = TextAnchor.MiddleLeft;

        transitionSelectionDetailEffectText = CreateText("Transition Detail Effect", transitionSelectionDetailRect, 15, FontStyle.Normal, bodyColor);
        LayoutElement detailEffectLayout = transitionSelectionDetailEffectText.gameObject.AddComponent<LayoutElement>();
        detailEffectLayout.preferredHeight = 20f;
        transitionSelectionDetailEffectText.alignment = TextAnchor.MiddleLeft;
        transitionSelectionDetailEffectText.verticalOverflow = VerticalWrapMode.Truncate;

        transitionSelectionDetailLoreText = CreateText("Transition Detail Lore", transitionSelectionDetailRect, 13, FontStyle.Italic, mutedBodyColor);
        LayoutElement detailLoreLayout = transitionSelectionDetailLoreText.gameObject.AddComponent<LayoutElement>();
        detailLoreLayout.preferredHeight = 32f;
        transitionSelectionDetailLoreText.alignment = TextAnchor.UpperLeft;
        transitionSelectionDetailLoreText.verticalOverflow = VerticalWrapMode.Truncate;
        transitionSelectionDetailLoreText.lineSpacing = 1.05f;
    }

    private void HandleActionClicked()
    {
        if (transitionScreenMode == TransitionScreenMode.Intermission)
        {
            HandleIntermissionContinueClicked();
            return;
        }

        if (transitionScreenMode == TransitionScreenMode.Selection)
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

    private void HandleIntermissionContinueClicked()
    {
        if (activeTransitionDisplayData.RequiresBoonSelection)
        {
            ShowTransitionScreen(TransitionScreenMode.Selection);
            return;
        }

        Action<HexBoonDefinition> callback = transitionActionCallback;
        Hide();
        callback?.Invoke(null);
    }

    private void RefreshTransitionStaticContent()
    {
        transitionMetaText.text = BuildTransitionMetaLabel(activeTransitionDisplayData);
        transitionTitleText.text = string.IsNullOrWhiteSpace(activeTransitionDisplayData.Title)
            ? "Act Complete"
            : activeTransitionDisplayData.Title.Trim();
        transitionBodyText.text = string.IsNullOrWhiteSpace(activeTransitionDisplayData.Body)
            ? "The caravan gathers itself for the road ahead."
            : activeTransitionDisplayData.Body.Trim();
        transitionBodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(transitionBodyText.text));
        selectionPromptText.text = string.IsNullOrWhiteSpace(activeTransitionDisplayData.SelectionPrompt)
            ? BuildDefaultSelectionPrompt(activeTransitionDisplayData.NextActNumber)
            : activeTransitionDisplayData.SelectionPrompt.Trim();
        selectionPromptText.gameObject.SetActive(activeTransitionDisplayData.RequiresBoonSelection);
        selectionContextNoteText.text = string.IsNullOrWhiteSpace(activeTransitionDisplayData.SelectionContextNote)
            ? string.Empty
            : activeTransitionDisplayData.SelectionContextNote.Trim();
        selectionContextNoteText.gameObject.SetActive(!string.IsNullOrWhiteSpace(selectionContextNoteText.text));

        RefreshTransitionIllustration();
        RebuildIntermissionGrantSummary(activeTransitionDisplayData);
        RebuildIntermissionCarryOverSummary(activeTransitionDisplayData.CurrentResources);
        RebuildSelectionCarryOverSummary(activeTransitionDisplayData.CurrentResources);
        RefreshTransitionDetailPanel();
    }

    private void ShowTransitionScreen(TransitionScreenMode mode)
    {
        transitionScreenMode = mode;

        if (transitionIntermissionRect != null)
        {
            transitionIntermissionRect.gameObject.SetActive(mode == TransitionScreenMode.Intermission);
        }

        if (transitionSelectionRect != null)
        {
            bool showSelection = mode == TransitionScreenMode.Selection && activeTransitionDisplayData.RequiresBoonSelection;
            transitionSelectionRect.gameObject.SetActive(showSelection);
        }

        if (panelRect != null)
        {
            panelRect.sizeDelta = mode == TransitionScreenMode.Selection
                ? ResolveTransitionSelectionPanelSize()
                : ResolveTransitionIntermissionPanelSize();
        }

        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(mode != TransitionScreenMode.None);
        }

        RefreshTransitionActionState();
        RefreshTransitionDetailPanel();
        ApplyTransitionSelectionVisuals();
        Canvas.ForceUpdateCanvases();
    }

    private void RefreshTransitionActionState()
    {
        if (actionButton == null || actionButtonText == null)
        {
            return;
        }

        if (transitionScreenMode == TransitionScreenMode.Selection)
        {
            actionButtonText.text = string.IsNullOrWhiteSpace(activeTransitionDisplayData.ContinueButtonLabel)
                ? "Continue"
                : activeTransitionDisplayData.ContinueButtonLabel.Trim();
            actionButton.interactable = selectedTransitionBoon != null;
            return;
        }

        if (transitionScreenMode == TransitionScreenMode.Intermission)
        {
            string intermissionLabel = activeTransitionDisplayData.RequiresBoonSelection
                ? activeTransitionDisplayData.IntermissionContinueButtonLabel
                : activeTransitionDisplayData.ContinueButtonLabel;
            actionButtonText.text = string.IsNullOrWhiteSpace(intermissionLabel)
                ? "Continue"
                : intermissionLabel.Trim();
            actionButton.interactable = true;
            return;
        }

        actionButton.interactable = true;
    }

    private void AnimateTransitionCards()
    {
        if (transitionScreenMode != TransitionScreenMode.Selection || transitionCards.Count == 0)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        if (deltaTime <= 0f)
        {
            return;
        }

        float blend = 1f - Mathf.Exp(-18f * deltaTime);
        for (int index = 0; index < transitionCards.Count; index++)
        {
            TransitionBoonCardUi card = transitionCards[index];
            if (card?.RootRect == null || !card.RootRect.gameObject.activeSelf || card.MotionRect == null)
            {
                continue;
            }

            card.MotionRect.localScale = Vector3.Lerp(card.MotionRect.localScale, Vector3.one * card.TargetScale, blend);
            Vector3 motionPosition = card.MotionRect.localPosition;
            motionPosition.y = Mathf.Lerp(motionPosition.y, card.TargetLift, blend);
            card.MotionRect.localPosition = motionPosition;
        }
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
        if (transitionSelectionOptionsRect == null)
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
        if (transitionSelectionOptionsRect != null)
        {
            transitionSelectionOptionsRect.gameObject.SetActive(hasOptions);
        }

        for (int index = 0; index < transitionCards.Count; index++)
        {
            bool shouldBeVisible = hasOptions && index < transitionBoonOptions.Length;
            TransitionBoonCardUi card = transitionCards[index];
            card.RootRect.gameObject.SetActive(shouldBeVisible);
            card.IsHovered = false;
            card.TargetScale = 1f;
            card.TargetLift = 0f;
            card.MotionRect.localScale = Vector3.one;
            card.MotionRect.localPosition = Vector3.zero;

            if (!shouldBeVisible)
            {
                continue;
            }

            PopulateTransitionCard(card, transitionBoonOptions[index]);
        }

        ApplyTransitionSelectionVisuals();
        RefreshTransitionDetailPanel();
        Canvas.ForceUpdateCanvases();
    }

    private void HandleTransitionOptionClicked(int optionIndex)
    {
        if (transitionBoonOptions == null || optionIndex < 0 || optionIndex >= transitionBoonOptions.Length)
        {
            return;
        }

        selectedTransitionBoon = transitionBoonOptions[optionIndex];
        RefreshTransitionActionState();
        ApplyTransitionSelectionVisuals();
        RefreshTransitionDetailPanel();
    }

    private void HandleTransitionOptionHoverChanged(int optionIndex, bool isHovered)
    {
        if (transitionBoonOptions == null || optionIndex < 0 || optionIndex >= transitionBoonOptions.Length)
        {
            return;
        }

        if (optionIndex >= transitionCards.Count)
        {
            return;
        }

        TransitionBoonCardUi card = transitionCards[optionIndex];
        card.IsHovered = isHovered;

        if (isHovered)
        {
            hoveredTransitionBoon = transitionBoonOptions[optionIndex];
        }
        else if (hoveredTransitionBoon != null
                 && transitionBoonOptions[optionIndex] != null
                 && string.Equals(hoveredTransitionBoon.id, transitionBoonOptions[optionIndex].id, StringComparison.OrdinalIgnoreCase))
        {
            hoveredTransitionBoon = null;
        }

        ApplyTransitionSelectionVisuals();
        RefreshTransitionDetailPanel();
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
            bool isHovered = card.IsHovered;
            Color accentColor = option != null ? GetArchetypeAccent(option.archetypeFamily) : cardSelectedFrameColor;

            if (isSelected)
            {
                card.FrameImage.color = Color.Lerp(accentColor, cardSelectedFrameColor, 0.3f);
                card.SurfaceImage.color = cardSelectedSurfaceColor;
                card.TargetScale = isHovered ? 1.045f : 1.035f;
                card.TargetLift = isHovered ? 12f : 9f;
            }
            else if (isHovered)
            {
                card.FrameImage.color = Color.Lerp(cardFrameColor, accentColor, 0.35f);
                card.SurfaceImage.color = cardHoverColor;
                card.TargetScale = 1.018f;
                card.TargetLift = 6f;
            }
            else
            {
                card.FrameImage.color = cardFrameColor;
                card.SurfaceImage.color = cardSurfaceColor;
                card.TargetScale = 1f;
                card.TargetLift = 0f;
            }
        }
    }

    private void RefreshTransitionDetailPanel()
    {
        if (transitionSelectionDetailRect == null)
        {
            return;
        }

        bool shouldShow = transitionScreenMode == TransitionScreenMode.Selection && activeTransitionDisplayData.RequiresBoonSelection;
        transitionSelectionDetailRect.gameObject.SetActive(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        HexBoonDefinition focusedBoon = GetFocusedTransitionBoon();
        if (focusedBoon == null)
        {
            transitionSelectionDetailMetaText.text = "Transition Detail";
            transitionSelectionDetailTitleText.text = "Hover a boon to inspect it.";
            transitionSelectionDetailEffectText.text = !string.IsNullOrWhiteSpace(activeTransitionDisplayData.SelectionContextNote)
                ? activeTransitionDisplayData.SelectionContextNote.Trim()
                : "Choose one boon to carry into the next act.";
            transitionSelectionDetailLoreText.text = string.Empty;
            transitionSelectionDetailLoreText.gameObject.SetActive(false);
            return;
        }

        focusedBoon.Validate();
        transitionSelectionDetailMetaText.text = BuildSelectionDetailMeta(focusedBoon);
        transitionSelectionDetailTitleText.text = focusedBoon.GetResolvedDisplayName();
        transitionSelectionDetailEffectText.text = GetExpandedBoonEffect(focusedBoon.description);

        string lore = NormalizeInlineText(focusedBoon.flavorText);
        if (!string.IsNullOrWhiteSpace(activeTransitionDisplayData.SelectionContextNote))
        {
            lore = string.IsNullOrWhiteSpace(lore)
                ? activeTransitionDisplayData.SelectionContextNote.Trim()
                : $"{lore}\n{activeTransitionDisplayData.SelectionContextNote.Trim()}";
        }

        transitionSelectionDetailLoreText.text = lore;
        transitionSelectionDetailLoreText.gameObject.SetActive(!string.IsNullOrWhiteSpace(lore));
    }

    private string BuildSelectionDetailMeta(HexBoonDefinition boon)
    {
        bool isSelected = selectedTransitionBoon != null
            && string.Equals(selectedTransitionBoon.id, boon.id, StringComparison.OrdinalIgnoreCase);
        bool isHovered = hoveredTransitionBoon != null
            && string.Equals(hoveredTransitionBoon.id, boon.id, StringComparison.OrdinalIgnoreCase);
        string stateLabel = isSelected ? "Selected path" : isHovered ? "Previewing path" : "Available path";
        return $"{stateLabel} · {FormatArchetype(boon.archetypeFamily)} family";
    }

    private void RefreshTransitionIllustration()
    {
        if (transitionIllustrationImage == null || transitionIllustrationFallbackText == null)
        {
            return;
        }

        Sprite illustration = activeTransitionDisplayData.IntermissionIllustration;
        if (illustration != null)
        {
            transitionIllustrationImage.sprite = illustration;
            transitionIllustrationImage.enabled = true;
            transitionIllustrationFallbackText.gameObject.SetActive(false);
            return;
        }

        transitionIllustrationImage.sprite = null;
        transitionIllustrationImage.enabled = false;
        transitionIllustrationFallbackText.text = "placeholder";
        transitionIllustrationFallbackText.gameObject.SetActive(true);
    }

    private void ApplyPanelMode(bool isTransitionLayout)
    {
        if (panelRect == null)
        {
            return;
        }

        panelRect.sizeDelta = isTransitionLayout
            ? ResolveTransitionIntermissionPanelSize()
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

    private void RebuildIntermissionGrantSummary(HexActTransitionDisplayData displayData)
    {
        ClearChildren(transitionGrantChipContainerRect);

        int chipCount = 0;
        chipCount += TryCreateGrantChip(transitionGrantChipContainerRect, "Food", displayData.BetweenActFood);
        chipCount += TryCreateGrantChip(transitionGrantChipContainerRect, "Morale", displayData.BetweenActMorale);
        chipCount += TryCreateGrantChip(transitionGrantChipContainerRect, "Gold", displayData.BetweenActGold);
        transitionGrantEmptyText.gameObject.SetActive(chipCount == 0);
    }

    private void RebuildIntermissionCarryOverSummary(CaravanResourceSnapshot currentResources)
    {
        ClearChildren(transitionResourceChipContainerRect);
        CreateIntermissionSummaryChip(transitionResourceChipContainerRect, "Food", currentResources.Food.ToString(), new Color(0.56f, 0.35f, 0.18f, 1f));
        CreateIntermissionSummaryChip(transitionResourceChipContainerRect, "Morale", currentResources.Morale.ToString(), new Color(0.51f, 0.21f, 0.24f, 1f));
        CreateIntermissionSummaryChip(transitionResourceChipContainerRect, "Gold", currentResources.Gold.ToString(), new Color(0.57f, 0.43f, 0.12f, 1f));
    }

    private void RebuildSelectionCarryOverSummary(CaravanResourceSnapshot currentResources)
    {
        ClearChildren(transitionSelectionResourceChipContainerRect);
        CreateResourceSummaryChip(transitionSelectionResourceChipContainerRect, "Food", currentResources.Food.ToString(), new Color(0.56f, 0.35f, 0.18f, 1f), compact: true);
        CreateResourceSummaryChip(transitionSelectionResourceChipContainerRect, "Morale", currentResources.Morale.ToString(), new Color(0.51f, 0.21f, 0.24f, 1f), compact: true);
        CreateResourceSummaryChip(transitionSelectionResourceChipContainerRect, "Gold", currentResources.Gold.ToString(), new Color(0.57f, 0.43f, 0.12f, 1f), compact: true);
    }

    private TransitionBoonCardUi CreateTransitionCardUi(int optionIndex)
    {
        RectTransform cardRect = CreateRectTransform($"Transition Card {optionIndex + 1}", transitionSelectionOptionsRect);
        LayoutElement layoutElement = cardRect.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = transitionCardWidth;
        layoutElement.minWidth = transitionCardWidth;
        layoutElement.preferredHeight = transitionCardHeight;
        layoutElement.minHeight = transitionCardHeight;

        Button button = cardRect.gameObject.AddComponent<Button>();
        ColorBlock buttonColors = button.colors;
        buttonColors.normalColor = cardSurfaceColor;
        buttonColors.highlightedColor = cardHoverColor;
        buttonColors.pressedColor = cardPressedColor;
        buttonColors.selectedColor = cardHoverColor;
        buttonColors.disabledColor = disabledCardColor;
        buttonColors.fadeDuration = 0.08f;
        button.colors = buttonColors;

        RectTransform motionRect = CreateRectTransform("Motion", cardRect);
        StretchToParent(motionRect);
        Image frameImage = motionRect.gameObject.AddComponent<Image>();
        frameImage.color = cardFrameColor;

        RectTransform surfaceRect = CreateRectTransform("Surface", motionRect);
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
        artLayout.preferredHeight = 164f;
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
        artFallbackText.text = "placeholder";

        Text effectText = CreateText("Effect", surfaceRect, 15, FontStyle.Bold, bodyColor);
        LayoutElement effectLayout = effectText.gameObject.AddComponent<LayoutElement>();
        effectLayout.preferredHeight = 42f;
        effectText.alignment = TextAnchor.UpperLeft;
        effectText.verticalOverflow = VerticalWrapMode.Truncate;
        effectText.lineSpacing = 1.05f;

        RectTransform familyBadgeRect = CreateRectTransform("Family Badge", surfaceRect);
        LayoutElement familyBadgeLayout = familyBadgeRect.gameObject.AddComponent<LayoutElement>();
        familyBadgeLayout.preferredHeight = 26f;
        Image familyBadgeImage = familyBadgeRect.gameObject.AddComponent<Image>();
        familyBadgeImage.color = chipColor;

        Text familyBadgeText = CreateText("Family Badge Text", familyBadgeRect, 12, FontStyle.Bold, titleColor);
        StretchToParent(familyBadgeText.rectTransform, 10f, 5f);
        familyBadgeText.alignment = TextAnchor.MiddleCenter;

        int capturedIndex = optionIndex;
        button.onClick.AddListener(() => HandleTransitionOptionClicked(capturedIndex));

        EventTrigger eventTrigger = cardRect.gameObject.AddComponent<EventTrigger>();
        AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter, _ => HandleTransitionOptionHoverChanged(capturedIndex, true));
        AddEventTrigger(eventTrigger, EventTriggerType.PointerExit, _ => HandleTransitionOptionHoverChanged(capturedIndex, false));

        return new TransitionBoonCardUi
        {
            RootRect = cardRect,
            LayoutElement = layoutElement,
            MotionRect = motionRect,
            Button = button,
            FrameImage = frameImage,
            SurfaceImage = surfaceImage,
            ArtFrameImage = artFrameImage,
            ArtImage = artImage,
            ArtFallbackText = artFallbackText,
            TitleText = cardTitleText,
            EffectText = effectText,
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
            card.FamilyBadgeText.text = "Unavailable";
            card.ArtImage.sprite = null;
            card.ArtImage.enabled = false;
            card.ArtFallbackText.text = "placeholder";
            card.ArtFallbackText.gameObject.SetActive(true);
            card.FamilyBadgeImage.color = chipColor;
            return;
        }

        boon.Validate();
        card.Button.interactable = boon.isEnabled;
        Color accentColor = GetArchetypeAccent(boon.archetypeFamily);
        card.TitleText.text = boon.GetResolvedDisplayName();
        card.EffectText.text = GetCompactBoonEffect(boon.description);
        card.FamilyBadgeText.text = $"{FormatArchetype(boon.archetypeFamily)} Family";
        card.FamilyBadgeImage.color = Color.Lerp(accentColor, chipColor, 0.48f);
        card.ArtFrameImage.color = Color.Lerp(accentColor, artPanelColor, 0.72f);

        card.ArtImage.sprite = null;
        card.ArtImage.enabled = false;
        card.ArtFallbackText.text = "placeholder";
        card.ArtFallbackText.color = mutedBodyColor;
        card.ArtFallbackText.gameObject.SetActive(true);
    }

    private HexBoonDefinition GetFocusedTransitionBoon()
    {
        return hoveredTransitionBoon ?? selectedTransitionBoon;
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
        chipLayout.preferredWidth = Mathf.Max(94f, 36f + chipCopy.Length * 6f);
        chipLayout.preferredHeight = 28f;
        Image chipImage = chipRect.gameObject.AddComponent<Image>();
        chipImage.color = Color.Lerp(journalChipColor, GetSummaryAccent(label), 0.2f);
        Outline chipOutline = chipRect.gameObject.AddComponent<Outline>();
        chipOutline.effectColor = journalChipBorderColor;
        chipOutline.effectDistance = new Vector2(1f, -1f);

        Text chipText = CreateText("Text", chipRect, 12, FontStyle.Bold, titleColor);
        StretchToParent(chipText.rectTransform, 10f, 5f);
        chipText.alignment = TextAnchor.MiddleCenter;
        chipText.text = chipCopy;
        return 1;
    }

    private void CreateIntermissionSummaryChip(Transform parent, string label, string value, Color accentColor)
    {
        RectTransform chipRect = CreateRectTransform($"{label} Journal Resource", parent);
        LayoutElement chipLayout = chipRect.gameObject.AddComponent<LayoutElement>();
        string chipCopy = $"{label} {value}";
        chipLayout.preferredWidth = Mathf.Max(94f, 36f + chipCopy.Length * 6f);
        chipLayout.preferredHeight = 28f;
        Image chipImage = chipRect.gameObject.AddComponent<Image>();
        chipImage.color = Color.Lerp(journalChipColor, accentColor, 0.2f);
        Outline chipOutline = chipRect.gameObject.AddComponent<Outline>();
        chipOutline.effectColor = journalChipBorderColor;
        chipOutline.effectDistance = new Vector2(1f, -1f);

        Text chipText = CreateText("Text", chipRect, 12, FontStyle.Bold, titleColor);
        StretchToParent(chipText.rectTransform, 10f, 5f);
        chipText.alignment = TextAnchor.MiddleCenter;
        chipText.text = chipCopy;
    }

    private void CreateResourceSummaryChip(Transform parent, string label, string value, Color accentColor, bool compact)
    {
        RectTransform chipRect = CreateRectTransform($"{label} Resource", parent);
        LayoutElement chipLayout = chipRect.gameObject.AddComponent<LayoutElement>();
        chipLayout.preferredWidth = compact ? 68f : 72f;
        chipLayout.preferredHeight = compact ? 28f : 48f;
        Image chipImage = chipRect.gameObject.AddComponent<Image>();
        chipImage.color = chipColor;
        Outline chipOutline = chipRect.gameObject.AddComponent<Outline>();
        chipOutline.effectColor = chipBorderColor;
        chipOutline.effectDistance = new Vector2(1f, -1f);

        if (compact)
        {
            HorizontalLayoutGroup compactLayout = chipRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            compactLayout.padding = new RectOffset(6, 6, 4, 4);
            compactLayout.spacing = 4f;
            compactLayout.childAlignment = TextAnchor.MiddleCenter;
            compactLayout.childControlHeight = true;
            compactLayout.childControlWidth = true;
            compactLayout.childForceExpandHeight = false;
            compactLayout.childForceExpandWidth = false;

            Text labelText = CreateText("Label", chipRect, 10, FontStyle.Bold, Color.Lerp(accentColor, Color.white, 0.38f));
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = Mathf.Max(22f, label.Length * 5f);
            labelLayout.preferredHeight = 14f;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = label.Substring(0, 1);

            Text valueText = CreateText("Value", chipRect, 12, FontStyle.Bold, titleColor);
            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredHeight = 14f;
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.text = value;
            return;
        }

        VerticalLayoutGroup chipLayoutGroup = chipRect.gameObject.AddComponent<VerticalLayoutGroup>();
        chipLayoutGroup.padding = new RectOffset(8, 8, 6, 6);
        chipLayoutGroup.spacing = 3f;
        chipLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        chipLayoutGroup.childControlHeight = true;
        chipLayoutGroup.childControlWidth = true;
        chipLayoutGroup.childForceExpandHeight = false;
        chipLayoutGroup.childForceExpandWidth = true;

        Text fullLabelText = CreateText("Label", chipRect, 10, FontStyle.Bold, Color.Lerp(accentColor, Color.white, 0.38f));
        LayoutElement fullLabelLayout = fullLabelText.gameObject.AddComponent<LayoutElement>();
        fullLabelLayout.preferredHeight = 12f;
        fullLabelText.alignment = TextAnchor.MiddleCenter;
        fullLabelText.text = label;

        Text fullValueText = CreateText("Value", chipRect, 13, FontStyle.Bold, titleColor);
        LayoutElement fullValueLayout = fullValueText.gameObject.AddComponent<LayoutElement>();
        fullValueLayout.preferredHeight = 16f;
        fullValueText.alignment = TextAnchor.MiddleCenter;
        fullValueText.text = value;
    }

    private static void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, UnityAction<BaseEventData> callback)
    {
        trigger.triggers ??= new List<EventTrigger.Entry>();
        EventTrigger.Entry entry = new()
        {
            eventID = eventType
        };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    private Image CreateDivider(string name, Transform parent, Color color, float height = 1f)
    {
        RectTransform dividerRect = CreateRectTransform(name, parent);
        LayoutElement dividerLayout = dividerRect.gameObject.AddComponent<LayoutElement>();
        dividerLayout.preferredHeight = height;
        Image dividerImage = dividerRect.gameObject.AddComponent<Image>();
        dividerImage.color = color;
        return dividerImage;
    }

    private RectTransform CreateInfoSurface(string name, Transform parent)
    {
        RectTransform rect = CreateRectTransform(name, parent);
        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 100f;
        Image background = rect.gameObject.AddComponent<Image>();
        background.color = chipColor;
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = chipBorderColor;
        outline.effectDistance = new Vector2(1f, -1f);
        return rect;
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

    private Vector2 ResolveTransitionIntermissionPanelSize()
    {
        return new Vector2(
            Mathf.Max(840f, transitionIntermissionPanelWidth),
            Mathf.Max(760f, transitionIntermissionPanelHeight));
    }

    private Vector2 ResolveTransitionSelectionPanelSize()
    {
        return new Vector2(
            Mathf.Max(820f, transitionSelectionPanelWidth),
            Mathf.Max(560f, transitionSelectionPanelHeight));
    }

    private static string BuildTransitionMetaLabel(HexActTransitionDisplayData displayData)
    {
        if (displayData.CompletedActNumber > 0 && displayData.NextActNumber > 0)
        {
            return $"ROAD INTERMISSION · ACT {displayData.CompletedActNumber} TO ACT {displayData.NextActNumber}";
        }

        return "ROAD INTERMISSION";
    }

    private static string BuildDefaultSelectionPrompt(int nextActNumber)
    {
        return nextActNumber > 0
            ? $"Choose one boon for Act {nextActNumber}."
            : "Choose one boon for the next act.";
    }

    private static string GetCompactBoonEffect(string rawDescription)
    {
        string normalized = NormalizeInlineText(rawDescription);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "No boon effect listed.";
        }

        int sentenceBreak = normalized.IndexOf('.');
        if (sentenceBreak > 0 && sentenceBreak < 68)
        {
            normalized = normalized[..(sentenceBreak + 1)];
        }

        if (normalized.Length > 68)
        {
            normalized = $"{normalized[..65].TrimEnd()}...";
        }

        return normalized;
    }

    private static string GetExpandedBoonEffect(string rawDescription)
    {
        string normalized = NormalizeInlineText(rawDescription);
        return string.IsNullOrWhiteSpace(normalized) ? "No gameplay bonus described." : normalized;
    }

    private static string NormalizeInlineText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        return rawText
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("  ", " ")
            .Trim();
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }

    private static Color GetSummaryAccent(string label)
    {
        return label switch
        {
            "Food" => new Color(0.56f, 0.35f, 0.18f, 1f),
            "Morale" => new Color(0.51f, 0.21f, 0.24f, 1f),
            "Gold" => new Color(0.57f, 0.43f, 0.12f, 1f),
            _ => new Color(0.42f, 0.46f, 0.52f, 1f)
        };
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
