using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class PitstopEventModalPresenter : MonoBehaviour
{
    [Header("Layout")]
    [Min(32f)] [SerializeField] private float panelWidth = 860f;
    [Min(32f)] [SerializeField] private float panelHeight = 640f;
    [Min(32f)] [SerializeField] private float optionHeight = 58f;
    [Min(8f)] [SerializeField] private float optionSpacing = 14f;
    [Min(120f)] [SerializeField] private float optionSummaryWidth = 270f;
    [Min(8f)] [SerializeField] private float optionSummaryGap = 24f;

    [Header("Colors")]
    [SerializeField] private Color overlayColor = new(0f, 0f, 0f, 0.86f);
    [SerializeField] private Color titleColor = new(0.98f, 0.98f, 0.98f, 1f);
    [SerializeField] private Color arrivalRewardColor = new(0.76f, 0.81f, 0.93f, 1f);
    [SerializeField] private Color bodyColor = new(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color optionBackgroundColor = new(0.12f, 0.15f, 0.19f, 0.94f);
    [SerializeField] private Color optionHighlightedColor = new(0.19f, 0.24f, 0.31f, 0.98f);
    [SerializeField] private Color optionPressedColor = new(0.28f, 0.34f, 0.42f, 1f);
    [SerializeField] private Color optionTextColor = new(0.96f, 0.96f, 0.96f, 1f);
    [SerializeField] private Color disabledOptionTextColor = new(0.62f, 0.62f, 0.62f, 1f);
    [SerializeField] private Color optionEffectColor = new(0.94f, 0.84f, 0.62f, 1f);
    [SerializeField] private Color disabledOptionEffectColor = new(0.68f, 0.62f, 0.54f, 1f);
    [SerializeField] private Color resultColor = new(0.93f, 0.86f, 0.64f, 1f);

    private Canvas targetCanvas;
    private GameObject overlayRoot;
    private Text titleText;
    private Text arrivalRewardText;
    private Text descriptionText;
    private Text resultText;
    private RectTransform optionsRoot;
    private Button mapButton;
    private Text mapButtonText;
    private Font uiFont;
    private readonly List<GameObject> spawnedOptionObjects = new();
    private Action<int> selectionCallback;
    private Action closeCallback;

    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

    public void ShowChoice(PitstopEventResult eventResult, CaravanResourceSnapshot resources, Action<int> onOptionSelected)
    {
        if (eventResult?.Encounter == null)
        {
            return;
        }

        EnsureUi();
        if (overlayRoot == null)
        {
            return;
        }

        selectionCallback = onOptionSelected;
        closeCallback = null;
        overlayRoot.SetActive(true);
        titleText.text = eventResult.Title;
        arrivalRewardText.text = FormatEntrySummary(eventResult);
        arrivalRewardText.gameObject.SetActive(true);
        descriptionText.text = eventResult.Description;
        resultText.gameObject.SetActive(false);
        resultText.text = string.Empty;
        mapButton.gameObject.SetActive(false);
        optionsRoot.gameObject.SetActive(true);
        RebuildOptions(eventResult.Encounter.options, resources);
    }

    public void ShowResolution(PitstopEventResult eventResult, Action onCloseRequested)
    {
        if (eventResult == null)
        {
            return;
        }

        EnsureUi();
        if (overlayRoot == null)
        {
            return;
        }

        selectionCallback = null;
        closeCallback = onCloseRequested;
        overlayRoot.SetActive(true);
        titleText.text = eventResult.Title;
        arrivalRewardText.text = FormatEntrySummary(eventResult);
        arrivalRewardText.gameObject.SetActive(true);
        descriptionText.text = string.IsNullOrWhiteSpace(eventResult.OutcomeText)
            ? eventResult.Description
            : eventResult.OutcomeText;
        optionsRoot.gameObject.SetActive(false);
        ClearOptions();
        resultText.text = FormatResolutionSummary(eventResult);
        resultText.gameObject.SetActive(true);
        mapButtonText.text = "Map";
        mapButton.gameObject.SetActive(true);
    }

    public void Hide()
    {
        selectionCallback = null;
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

        targetCanvas = FindAnyObjectByType<Canvas>();
        if (targetCanvas == null)
        {
            Debug.LogError("PitstopEventModalPresenter requires a Canvas in the scene.", this);
            return;
        }

        uiFont = ResolveFont();
        RectTransform overlayRect = CreateRectTransform("Pitstop Event Overlay", targetCanvas.transform);
        overlayRoot = overlayRect.gameObject;
        StretchToParent(overlayRect);

        Image overlayImage = overlayRoot.AddComponent<Image>();
        overlayImage.color = overlayColor;
        overlayImage.raycastTarget = true;
        overlayRoot.SetActive(false);

        RectTransform contentRoot = CreateRectTransform("Content Root", overlayRect);
        contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        contentRoot.pivot = new Vector2(0.5f, 0.5f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(panelWidth, panelHeight);

        titleText = CreateText("Event Title", contentRoot, 58, FontStyle.Bold, titleColor);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);
        titleRect.sizeDelta = new Vector2(panelWidth, 72f);
        titleText.alignment = TextAnchor.UpperLeft;

        arrivalRewardText = CreateText("Arrival Reward", contentRoot, 20, FontStyle.Italic, arrivalRewardColor);
        RectTransform arrivalRewardRect = arrivalRewardText.rectTransform;
        arrivalRewardRect.anchorMin = new Vector2(0.5f, 1f);
        arrivalRewardRect.anchorMax = new Vector2(0.5f, 1f);
        arrivalRewardRect.pivot = new Vector2(0.5f, 1f);
        arrivalRewardRect.anchoredPosition = new Vector2(0f, -78f);
        arrivalRewardRect.sizeDelta = new Vector2(panelWidth, 34f);
        arrivalRewardText.alignment = TextAnchor.UpperLeft;

        descriptionText = CreateText("Event Description", contentRoot, 28, FontStyle.Italic, bodyColor);
        RectTransform descriptionRect = descriptionText.rectTransform;
        descriptionRect.anchorMin = new Vector2(0.5f, 1f);
        descriptionRect.anchorMax = new Vector2(0.5f, 1f);
        descriptionRect.pivot = new Vector2(0.5f, 1f);
        descriptionRect.anchoredPosition = new Vector2(0f, -122f);
        descriptionRect.sizeDelta = new Vector2(panelWidth, 248f);

        optionsRoot = CreateRectTransform("Options Root", contentRoot);
        optionsRoot.anchorMin = new Vector2(0.5f, 1f);
        optionsRoot.anchorMax = new Vector2(0.5f, 1f);
        optionsRoot.pivot = new Vector2(0.5f, 1f);
        optionsRoot.anchoredPosition = new Vector2(0f, -386f);
        optionsRoot.sizeDelta = new Vector2(panelWidth, 220f);

        resultText = CreateText("Result Text", contentRoot, 24, FontStyle.Bold, resultColor);
        RectTransform resultRect = resultText.rectTransform;
        resultRect.anchorMin = new Vector2(0.5f, 0f);
        resultRect.anchorMax = new Vector2(0.5f, 0f);
        resultRect.pivot = new Vector2(0.5f, 0f);
        resultRect.anchoredPosition = new Vector2(0f, 110f);
        resultRect.sizeDelta = new Vector2(panelWidth, 90f);
        resultText.alignment = TextAnchor.UpperLeft;
        resultText.gameObject.SetActive(false);

        RectTransform buttonRect = CreateRectTransform("Map Button", contentRoot);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 18f);
        buttonRect.sizeDelta = new Vector2(220f, 56f);

        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = optionBackgroundColor;
        mapButton = buttonRect.gameObject.AddComponent<Button>();
        ColorBlock buttonColors = mapButton.colors;
        buttonColors.normalColor = optionBackgroundColor;
        buttonColors.highlightedColor = optionHighlightedColor;
        buttonColors.pressedColor = optionPressedColor;
        buttonColors.selectedColor = optionHighlightedColor;
        buttonColors.disabledColor = optionBackgroundColor * 0.6f;
        mapButton.colors = buttonColors;
        mapButton.onClick.AddListener(HandleMapClicked);

        mapButtonText = CreateText("Map Button Text", buttonRect, 24, FontStyle.Bold, optionTextColor);
        RectTransform mapButtonTextRect = mapButtonText.rectTransform;
        StretchToParent(mapButtonTextRect, 16f, 10f);
        mapButtonText.alignment = TextAnchor.MiddleCenter;
        mapButton.gameObject.SetActive(false);
    }

    private void RebuildOptions(IReadOnlyList<PitstopEncounterOption> options, CaravanResourceSnapshot resources)
    {
        ClearOptions();
        if (options == null)
        {
            return;
        }

        for (int index = 0; index < options.Count; index++)
        {
            PitstopEncounterOption option = options[index];
            if (option == null)
            {
                continue;
            }

            CreateOptionButton(option, index, resources);
        }
    }

    private void ClearOptions()
    {
        for (int index = 0; index < spawnedOptionObjects.Count; index++)
        {
            if (spawnedOptionObjects[index] != null)
            {
                Destroy(spawnedOptionObjects[index]);
            }
        }

        spawnedOptionObjects.Clear();
    }

    private void CreateOptionButton(PitstopEncounterOption option, int index, CaravanResourceSnapshot resources)
    {
        RectTransform rowRect = CreateRectTransform($"Option {index + 1}", optionsRoot);
        GameObject rowObject = rowRect.gameObject;
        spawnedOptionObjects.Add(rowObject);
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -index * (optionHeight + optionSpacing));
        rowRect.sizeDelta = new Vector2(panelWidth, optionHeight);

        float buttonWidth = Mathf.Max(220f, panelWidth - optionSummaryWidth - optionSummaryGap);
        RectTransform buttonRect = CreateRectTransform("Choice Button", rowRect);
        GameObject buttonObject = buttonRect.gameObject;
        buttonRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRect.anchorMax = new Vector2(0f, 0.5f);
        buttonRect.pivot = new Vector2(0f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(buttonWidth, optionHeight);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = optionBackgroundColor;
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = optionBackgroundColor;
        colors.highlightedColor = optionHighlightedColor;
        colors.pressedColor = optionPressedColor;
        colors.selectedColor = optionHighlightedColor;
        colors.disabledColor = optionBackgroundColor * 0.6f;
        button.colors = colors;
        bool canAfford = option.CanAfford(resources);
        button.interactable = canAfford;
        int optionIndex = index;
        button.onClick.AddListener(() => HandleOptionSelected(optionIndex));

        Text optionText = CreateText("Label", buttonRect, 24, FontStyle.Bold, optionTextColor);
        RectTransform textRect = optionText.rectTransform;
        StretchToParent(textRect, 18f, 10f);
        optionText.text = option.label;
        optionText.alignment = TextAnchor.MiddleLeft;
        optionText.color = canAfford ? optionTextColor : disabledOptionTextColor;

        Text optionEffectText = CreateText("Effect Summary", rowRect, 18, FontStyle.Italic, optionEffectColor);
        RectTransform effectRect = optionEffectText.rectTransform;
        effectRect.anchorMin = new Vector2(1f, 0.5f);
        effectRect.anchorMax = new Vector2(1f, 0.5f);
        effectRect.pivot = new Vector2(1f, 0.5f);
        effectRect.anchoredPosition = Vector2.zero;
        effectRect.sizeDelta = new Vector2(optionSummaryWidth, optionHeight);
        optionEffectText.alignment = TextAnchor.MiddleLeft;
        optionEffectText.color = canAfford ? optionEffectColor : disabledOptionEffectColor;
        optionEffectText.text = FormatOptionSummary(option, resources);
    }

    private void HandleOptionSelected(int optionIndex)
    {
        Action<int> callback = selectionCallback;
        callback?.Invoke(optionIndex);
    }

    private void HandleMapClicked()
    {
        Action close = closeCallback;
        Hide();
        close?.Invoke();
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

    private static string FormatResolutionSummary(PitstopEventResult eventResult)
    {
        if (eventResult == null || eventResult.ChoiceEffects.Count == 0)
        {
            return "No resource change.";
        }

        List<string> gains = new();
        List<string> losses = new();
        for (int index = 0; index < eventResult.ChoiceEffects.Count; index++)
        {
            PitstopResourceEffectResult effect = eventResult.ChoiceEffects[index];
            string label = $"{Mathf.Abs(effect.Amount)} {FormatResource(effect.ResourceType)}";
            if (effect.Amount >= 0)
            {
                gains.Add(label);
            }
            else
            {
                losses.Add(label);
            }
        }

        List<string> lines = new();
        if (gains.Count > 0)
        {
            lines.Add($"Gained: {string.Join(", ", gains)}");
        }

        if (losses.Count > 0)
        {
            lines.Add($"Lost: {string.Join(", ", losses)}");
        }

        return string.Join("\n", lines);
    }

    private static string FormatEntrySummary(PitstopEventResult eventResult)
    {
        if (eventResult == null)
        {
            return string.Empty;
        }

        string summary = FormatEffectSummary(eventResult.EntryEffects);
        return string.IsNullOrWhiteSpace(summary)
            ? "Arrival bonus: none"
            : $"Arrival bonus: {summary}";
    }

    private static string FormatOptionSummary(PitstopEncounterOption option, CaravanResourceSnapshot resources)
    {
        if (option == null)
        {
            return "No resource change";
        }

        string summary = FormatEffectSummary(option.resourceEffects);
        if (option.TryGetUnavailableSummary(resources, out string unavailableSummary))
        {
            summary = string.IsNullOrWhiteSpace(summary)
                ? unavailableSummary
                : $"{summary}\n{unavailableSummary}";
        }

        return string.IsNullOrWhiteSpace(summary) ? "No resource change" : summary;
    }

    private static string FormatEffectSummary(IReadOnlyList<PitstopResourceEffectResult> effects)
    {
        if (effects == null || effects.Count == 0)
        {
            return string.Empty;
        }

        List<string> gains = new();
        List<string> losses = new();
        for (int index = 0; index < effects.Count; index++)
        {
            PitstopResourceEffectResult effect = effects[index];
            string label = $"{Mathf.Abs(effect.Amount)} {FormatResource(effect.ResourceType)}";
            if (effect.Amount >= 0)
            {
                gains.Add(label);
            }
            else
            {
                losses.Add(label);
            }
        }

        return JoinEffectGroups(gains, losses);
    }

    private static string FormatEffectSummary(IReadOnlyList<PitstopResourceEffect> effects)
    {
        if (effects == null || effects.Count == 0)
        {
            return string.Empty;
        }

        List<string> gains = new();
        List<string> losses = new();
        for (int index = 0; index < effects.Count; index++)
        {
            PitstopResourceEffect effect = effects[index];
            if (effect == null || effect.amount == 0)
            {
                continue;
            }

            string label = $"{Mathf.Abs(effect.amount)} {FormatResource(effect.resourceType)}";
            if (effect.amount >= 0)
            {
                gains.Add(label);
            }
            else
            {
                losses.Add(label);
            }
        }

        return JoinEffectGroups(gains, losses);
    }

    private static string JoinEffectGroups(IReadOnlyList<string> gains, IReadOnlyList<string> losses)
    {
        List<string> groups = new();
        if (gains != null && gains.Count > 0)
        {
            groups.Add($"Gain: {string.Join(", ", gains)}");
        }

        if (losses != null && losses.Count > 0)
        {
            groups.Add($"Lose: {string.Join(", ", losses)}");
        }

        return string.Join("\n", groups);
    }

    private static string FormatResource(CaravanResourceType resourceType)
    {
        return resourceType switch
        {
            CaravanResourceType.Food => "Food",
            CaravanResourceType.Morale => "Morale",
            CaravanResourceType.Gold => "Gold",
            _ => resourceType.ToString()
        };
    }
}
