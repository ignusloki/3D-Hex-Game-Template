using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PitstopEventModalPresenter : MonoBehaviour
{
    private HexPitstopEventModalDocumentController documentController;
    private HexGameplayUiRootController gameplayUiRootController;

    public bool IsOpen => documentController != null && documentController.IsOpen;

    public void ShowChoice(PitstopEventResult eventResult, CaravanResourceSnapshot resources, Action<int> onOptionSelected)
    {
        if (eventResult?.Encounter == null)
        {
            return;
        }

        LogDebug(
            $"ShowChoice title='{eventResult.Title}' encounter='{eventResult.Encounter.eventId}' optionCount={eventResult.Encounter.options?.Count ?? 0} food={resources.Food} morale={resources.Morale} gold={resources.Gold}.");

        EnsureView();
        if (documentController == null)
        {
            return;
        }

        documentController.ShowChoice(
            BuildMetaLabel(eventResult),
            eventResult.Title,
            eventResult.Description,
            BuildEffectChips(eventResult.EntryEffects, "No arrival bonus"),
            BuildResourceSummaryData(resources),
            BuildOptionViewData(eventResult.Encounter.options, resources),
            onOptionSelected);
    }

    public void ShowResolution(PitstopEventResult eventResult, CaravanResourceSnapshot resources, Action onCloseRequested)
    {
        if (eventResult == null)
        {
            return;
        }

        LogDebug(
            $"ShowResolution title='{eventResult.Title}' selectedOption='{eventResult.SelectedOption?.label ?? "none"}' food={resources.Food} morale={resources.Morale} gold={resources.Gold}.");

        EnsureView();
        if (documentController == null)
        {
            return;
        }

        string description = string.IsNullOrWhiteSpace(eventResult.OutcomeText)
            ? eventResult.Description
            : eventResult.OutcomeText;

        documentController.ShowResolution(
            BuildMetaLabel(eventResult),
            eventResult.Title,
            description,
            BuildEffectChips(eventResult.EntryEffects, "No arrival bonus"),
            BuildResourceSummaryData(resources),
            BuildResolutionChoiceText(eventResult),
            BuildEffectChips(eventResult.ChoiceEffects, "No resource change"),
            "Map",
            onCloseRequested);
    }

    public void Hide()
    {
        LogDebug("Hide requested.");
        documentController?.Hide();
    }

    private void EnsureView()
    {
        documentController ??= new HexPitstopEventModalDocumentController(this);
        documentController.EnsureInitialized();
    }

    internal void LogDebug(string message, bool verbose = false)
    {
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this, createIfMissing: false);
        gameplayUiRootController?.EnsureInitialized();
        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.LogDiagnostic("PitstopModal", message, this, verbose);
            return;
        }

        Debug.Log($"[PitstopEventModal] {message}", this);
    }

    private static List<HexPitstopOptionViewData> BuildOptionViewData(
        IReadOnlyList<PitstopEncounterOption> options,
        CaravanResourceSnapshot resources)
    {
        List<HexPitstopOptionViewData> viewData = new();
        if (options == null)
        {
            return viewData;
        }

        for (int index = 0; index < options.Count; index++)
        {
            PitstopEncounterOption option = options[index];
            if (option == null)
            {
                continue;
            }

            List<HexPitstopEffectChipData> chips = BuildEffectChips(option.resourceEffects, "No resource change");
            bool isEnabled = !option.TryGetUnavailableSummary(resources, out string unavailableSummary);
            if (!string.IsNullOrWhiteSpace(unavailableSummary))
            {
                chips.Add(new HexPitstopEffectChipData(unavailableSummary, HexPitstopEffectChipTone.Warning));
            }

            viewData.Add(new HexPitstopOptionViewData(index, option.label, chips, isEnabled));
        }

        return viewData;
    }

    private static List<HexPitstopEffectChipData> BuildEffectChips(
        IReadOnlyList<PitstopResourceEffectResult> effects,
        string fallbackText)
    {
        List<HexPitstopEffectChipData> chips = new();
        if (effects != null)
        {
            for (int index = 0; index < effects.Count; index++)
            {
                PitstopResourceEffectResult effect = effects[index];
                if (effect.Amount == 0)
                {
                    continue;
                }

                chips.Add(CreateChipData(effect.ResourceType, effect.Amount));
            }
        }

        if (chips.Count == 0 && !string.IsNullOrWhiteSpace(fallbackText))
        {
            chips.Add(new HexPitstopEffectChipData(fallbackText, HexPitstopEffectChipTone.Neutral));
        }

        return chips;
    }

    private static List<HexPitstopEffectChipData> BuildEffectChips(
        IReadOnlyList<PitstopResourceEffect> effects,
        string fallbackText)
    {
        List<HexPitstopEffectChipData> chips = new();
        if (effects != null)
        {
            for (int index = 0; index < effects.Count; index++)
            {
                PitstopResourceEffect effect = effects[index];
                if (effect == null || effect.amount == 0)
                {
                    continue;
                }

                chips.Add(CreateChipData(effect.resourceType, effect.amount));
            }
        }

        if (chips.Count == 0 && !string.IsNullOrWhiteSpace(fallbackText))
        {
            chips.Add(new HexPitstopEffectChipData(fallbackText, HexPitstopEffectChipTone.Neutral));
        }

        return chips;
    }

    private static HexPitstopEffectChipData CreateChipData(CaravanResourceType resourceType, int amount)
    {
        bool isNegative = amount < 0;
        string prefix = isNegative ? "-" : "+";
        string text = $"{prefix}{Mathf.Abs(amount)} {FormatResource(resourceType)}";

        HexPitstopEffectChipTone tone = resourceType switch
        {
            CaravanResourceType.Food => HexPitstopEffectChipTone.Food,
            CaravanResourceType.Morale => HexPitstopEffectChipTone.Morale,
            CaravanResourceType.Gold => HexPitstopEffectChipTone.Gold,
            _ => HexPitstopEffectChipTone.Neutral
        };

        return new HexPitstopEffectChipData(text, tone, isNegative);
    }

    private static List<HexPitstopResourceViewData> BuildResourceSummaryData(CaravanResourceSnapshot resources)
    {
        return new List<HexPitstopResourceViewData>(3)
        {
            new("F", Mathf.Max(resources.Food, 0).ToString(), HexPitstopEffectChipTone.Food),
            new("M", Mathf.Max(resources.Morale, 0).ToString(), HexPitstopEffectChipTone.Morale),
            new("G", Mathf.Max(resources.Gold, 0).ToString(), HexPitstopEffectChipTone.Gold)
        };
    }

    private static string BuildResolutionChoiceText(PitstopEventResult eventResult)
    {
        if (eventResult?.SelectedOption == null)
        {
            return "Outcome";
        }

        return eventResult.SelectedOption.label;
    }

    private static string BuildMetaLabel(PitstopEventResult eventResult)
    {
        PitstopSite site = eventResult?.Site;
        if (site == null)
        {
            return "Pitstop";
        }

        return $"{FormatPitstopKind(site.Kind)} • {site.Coordinates.Row},{site.Coordinates.Column}";
    }

    private static string FormatPitstopKind(PitstopKind kind)
    {
        string raw = kind.ToString();
        System.Text.StringBuilder builder = new(raw.Length + 8);
        for (int index = 0; index < raw.Length; index++)
        {
            char character = raw[index];
            if (index > 0 && char.IsUpper(character))
            {
                builder.Append(' ');
            }

            builder.Append(character);
        }

        builder.Append(" Pitstop");
        return builder.ToString().ToUpperInvariant();
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

internal enum HexPitstopEffectChipTone
{
    Neutral,
    Food,
    Morale,
    Gold,
    Warning
}

internal readonly struct HexPitstopEffectChipData
{
    public HexPitstopEffectChipData(string text, HexPitstopEffectChipTone tone, bool isNegative = false)
    {
        Text = text;
        Tone = tone;
        IsNegative = isNegative;
    }

    public string Text { get; }
    public HexPitstopEffectChipTone Tone { get; }
    public bool IsNegative { get; }
}

internal sealed class HexPitstopOptionViewData
{
    public HexPitstopOptionViewData(
        int index,
        string label,
        IReadOnlyList<HexPitstopEffectChipData> chips,
        bool isEnabled)
    {
        Index = index;
        Label = label;
        Chips = chips ?? Array.Empty<HexPitstopEffectChipData>();
        IsEnabled = isEnabled;
    }

    public int Index { get; }
    public string Label { get; }
    public IReadOnlyList<HexPitstopEffectChipData> Chips { get; }
    public bool IsEnabled { get; }
}

internal readonly struct HexPitstopResourceViewData
{
    public HexPitstopResourceViewData(string label, string value, HexPitstopEffectChipTone tone)
    {
        Label = label;
        Value = value;
        Tone = tone;
    }

    public string Label { get; }
    public string Value { get; }
    public HexPitstopEffectChipTone Tone { get; }
}

internal sealed class HexPitstopEventModalDocumentController
{
    private const string LayoutResourcePath = "UI/Modal/HexPitstopEventModal";
    private const string StyleSheetResourcePath = "UI/Modal/HexPitstopEventModalStyles";
    private const string SimpleModalFadeProfileResourcePath = "UI/Transitions/SimpleModalFade";
    private const string ModalMountName = "pitstop-modal-mount";
    private const string ScrimTransitionTargetKey = "scrim";
    private const string ShellTransitionTargetKey = "shell";

    private readonly MonoBehaviour owner;
    private readonly List<Button> optionButtons = new();
    private VisualTreeAsset layoutAsset;
    private StyleSheet styleSheet;
    private HexUiTransitionProfile simpleModalFadeProfile;
    private HexUiTransitionProfile activeModalFadeProfile;
    private HexGameplayUiRootController gameplayUiRootController;
    private VisualElement modalLayer;
    private VisualElement modalMount;
    private VisualElement modalRoot;
    private VisualElement overlayElement;
    private VisualElement scrimElement;
    private VisualElement shellElement;
    private Label metaLabel;
    private Label titleLabel;
    private Label descriptionLabel;
    private VisualElement arrivalChipsContainer;
    private VisualElement resourceStripContainer;
    private VisualElement optionsSection;
    private VisualElement optionsList;
    private VisualElement resolutionSection;
    private Label resolutionChoiceLabel;
    private VisualElement resolutionChipsContainer;
    private Button continueButton;
    private Action<int> optionSelected;
    private Action continueSelected;
    private HexHudDocumentController hudDocumentController;
    private HexUiTransitionPlayer modalTransitionPlayer;
    private bool isInitialized;
    private bool isOpen;
    private bool areTransitionControlsLocked;

    public HexPitstopEventModalDocumentController(MonoBehaviour owner)
    {
        this.owner = owner;
    }

    public bool IsOpen => isOpen;

    public void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        layoutAsset ??= Resources.Load<VisualTreeAsset>(LayoutResourcePath);
        styleSheet ??= Resources.Load<StyleSheet>(StyleSheetResourcePath);
        simpleModalFadeProfile ??= Resources.Load<HexUiTransitionProfile>(SimpleModalFadeProfileResourcePath);
        if (layoutAsset == null || styleSheet == null)
        {
            Debug.LogError("HexPitstopEventModalDocumentController could not load the UI Toolkit modal assets from Resources.", owner);
            return;
        }

        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(owner);
        if (gameplayUiRootController == null)
        {
            Debug.LogError("HexPitstopEventModalDocumentController could not resolve the shared gameplay UI root.", owner);
            return;
        }

        gameplayUiRootController.EnsureInitialized();
        modalLayer = gameplayUiRootController.GetLayer(HexGameplayUiLayerId.Modal);
        modalMount = gameplayUiRootController.RequestLayerMount(
            HexGameplayUiLayerId.Modal,
            ModalMountName,
            nameof(HexPitstopEventModalDocumentController),
            false,
            owner);

        if (modalLayer == null || modalMount == null)
        {
            Debug.LogError("HexPitstopEventModalDocumentController could not bind to the shared modal layer.", owner);
            return;
        }

        modalMount.Clear();
        modalMount.styleSheets.Clear();
        modalMount.styleSheets.Add(styleSheet);
        layoutAsset.CloneTree(modalMount);

        modalRoot = modalMount.Q<VisualElement>("pitstop-modal-root");
        overlayElement = modalMount.Q<VisualElement>("pitstop-modal-overlay");
        scrimElement = modalMount.Q<VisualElement>("pitstop-modal-scrim");
        shellElement = modalMount.Q<VisualElement>("pitstop-modal-shell");
        metaLabel = modalMount.Q<Label>("pitstop-modal-meta");
        titleLabel = modalMount.Q<Label>("pitstop-modal-title");
        descriptionLabel = modalMount.Q<Label>("pitstop-modal-description");
        arrivalChipsContainer = modalMount.Q<VisualElement>("pitstop-arrival-chips");
        resourceStripContainer = modalMount.Q<VisualElement>("pitstop-modal-resource-strip");
        optionsSection = modalMount.Q<VisualElement>("pitstop-options-section");
        optionsList = modalMount.Q<VisualElement>("pitstop-options-list");
        resolutionSection = modalMount.Q<VisualElement>("pitstop-resolution-section");
        resolutionChoiceLabel = modalMount.Q<Label>("pitstop-resolution-choice");
        resolutionChipsContainer = modalMount.Q<VisualElement>("pitstop-resolution-chips");
        continueButton = modalMount.Q<Button>("pitstop-continue-button");

        if (continueButton != null)
        {
            continueButton.clicked += HandleContinueClicked;
            continueButton.RegisterCallback<PointerEnterEvent>(_ => LogDebug("Continue button pointer enter.", true));
            continueButton.RegisterCallback<PointerLeaveEvent>(_ => LogDebug("Continue button pointer leave.", true));
            continueButton.RegisterCallback<PointerDownEvent>(evt => LogDebug($"Continue button pointer down at {evt.position}.", true));
            continueButton.RegisterCallback<PointerUpEvent>(evt => LogDebug($"Continue button pointer up at {evt.position}.", true));
            continueButton.RegisterCallback<ClickEvent>(_ => LogDebug("Continue button ClickEvent received.", true));
        }

        if (modalRoot != null)
        {
            modalRoot.style.display = DisplayStyle.None;
            modalTransitionPlayer = new HexUiTransitionPlayer(modalRoot);
        }

        LogDebug(
            $"Initialized shared-root modal. modalLayerFound={modalLayer != null} modalRootFound={modalRoot != null} shellFound={shellElement != null} optionsListFound={optionsList != null}.");

        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Modal, false);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Modal, false);

        isInitialized = true;
    }

    public void ShowChoice(
        string metaText,
        string titleText,
        string descriptionText,
        IReadOnlyList<HexPitstopEffectChipData> arrivalChips,
        IReadOnlyList<HexPitstopResourceViewData> resources,
        IReadOnlyList<HexPitstopOptionViewData> options,
        Action<int> onOptionSelected)
    {
        EnsureInitialized();
        if (!isInitialized)
        {
            return;
        }

        bool shouldPlayReveal = !isOpen;
        optionSelected = onOptionSelected;
        continueSelected = null;
        LogDebug($"ShowChoice internal. options={options?.Count ?? 0} callbackAssigned={optionSelected != null}.");
        SetHeader(metaText, titleText, descriptionText, arrivalChips, resources);
        PopulateOptions(options);
        PopulateChipContainer(resolutionChipsContainer, Array.Empty<HexPitstopEffectChipData>());
        resolutionChoiceLabel.text = string.Empty;
        optionsSection.style.display = DisplayStyle.Flex;
        resolutionSection.style.display = DisplayStyle.None;
        SetResultMode(false);
        SetModalVisibility(true);
        ShowModalContent(shouldPlayReveal);
    }

    public void ShowResolution(
        string metaText,
        string titleText,
        string descriptionText,
        IReadOnlyList<HexPitstopEffectChipData> arrivalChips,
        IReadOnlyList<HexPitstopResourceViewData> resources,
        string resolutionChoiceText,
        IReadOnlyList<HexPitstopEffectChipData> resolutionChips,
        string continueLabel,
        Action onContinueSelected)
    {
        EnsureInitialized();
        if (!isInitialized)
        {
            return;
        }

        bool shouldPlayReveal = !isOpen;
        optionSelected = null;
        continueSelected = onContinueSelected;
        LogDebug($"ShowResolution internal. continueAssigned={continueSelected != null}.");
        SetHeader(metaText, titleText, descriptionText, arrivalChips, resources);
        PopulateOptions(Array.Empty<HexPitstopOptionViewData>());
        resolutionChoiceLabel.text = string.IsNullOrWhiteSpace(resolutionChoiceText) ? "Outcome" : resolutionChoiceText;
        PopulateChipContainer(resolutionChipsContainer, resolutionChips);
        continueButton.text = string.IsNullOrWhiteSpace(continueLabel) ? "Continue" : continueLabel;
        optionsSection.style.display = DisplayStyle.None;
        resolutionSection.style.display = DisplayStyle.Flex;
        SetResultMode(true);
        SetModalVisibility(true);
        ShowModalContent(shouldPlayReveal);
    }

    public void Hide()
    {
        LogDebug("Hide internal.");
        StopModalReveal();
        optionSelected = null;
        continueSelected = null;
        PopulateOptions(Array.Empty<HexPitstopOptionViewData>());
        PopulateChipContainer(resolutionChipsContainer, Array.Empty<HexPitstopEffectChipData>());
        SetResultMode(false);
        SetModalVisibility(false);
    }

    private void SetHeader(
        string metaText,
        string titleText,
        string descriptionText,
        IReadOnlyList<HexPitstopEffectChipData> arrivalChips,
        IReadOnlyList<HexPitstopResourceViewData> resources)
    {
        metaLabel.text = string.IsNullOrWhiteSpace(metaText) ? "Pitstop" : metaText;
        titleLabel.text = string.IsNullOrWhiteSpace(titleText) ? "Pitstop" : titleText;
        descriptionLabel.text = string.IsNullOrWhiteSpace(descriptionText) ? string.Empty : descriptionText.Trim();
        PopulateChipContainer(arrivalChipsContainer, arrivalChips);
        PopulateResourceSummary(resources);
    }

    private void PopulateOptions(IReadOnlyList<HexPitstopOptionViewData> options)
    {
        if (optionsList == null)
        {
            return;
        }

        optionButtons.Clear();
        optionsList.Clear();
        if (options == null)
        {
            return;
        }

        for (int index = 0; index < options.Count; index++)
        {
            HexPitstopOptionViewData option = options[index];
            if (option == null)
            {
                continue;
            }

            LogDebug(
                $"Create option row index={option.Index} label='{option.Label}' enabled={option.IsEnabled} chipCount={option.Chips?.Count ?? 0}.",
                true);

            int optionIndex = option.Index;
            Button optionButton = new();
            optionButton.text = string.Empty;
            optionButton.AddToClassList("pitstop-option");
            optionButton.SetEnabled(option.IsEnabled);
            optionButton.focusable = option.IsEnabled;
            optionButton.tabIndex = option.IsEnabled ? 0 : -1;
            optionButton.userData = option.IsEnabled;
            optionButton.clicked += () => HandleOptionClicked(optionIndex);

            Label optionLabel = new(option.Label);
            optionLabel.AddToClassList("pitstop-option-label");
            optionButton.Add(optionLabel);

            VisualElement chipRow = new();
            chipRow.AddToClassList("pitstop-option-chip-row");
            PopulateChipContainer(chipRow, option.Chips);
            optionButton.Add(chipRow);

            if (option.IsEnabled)
            {
                optionButton.RegisterCallback<PointerEnterEvent>(_ =>
                    LogDebug($"Option pointer enter. index={optionIndex} label='{option.Label}'.", true));
                optionButton.RegisterCallback<PointerLeaveEvent>(_ =>
                    LogDebug($"Option pointer leave. index={optionIndex} label='{option.Label}'.", true));
                optionButton.RegisterCallback<PointerDownEvent>(evt =>
                    LogDebug($"Option pointer down. index={optionIndex} label='{option.Label}' position={evt.position}.", true));
                optionButton.RegisterCallback<PointerUpEvent>(evt =>
                    LogDebug($"Option pointer up. index={optionIndex} label='{option.Label}' position={evt.position}.", true));
                optionButton.RegisterCallback<FocusInEvent>(_ =>
                    LogDebug($"Option focus in. index={optionIndex} label='{option.Label}'.", true));
                optionButton.RegisterCallback<FocusOutEvent>(_ =>
                    LogDebug($"Option focus out. index={optionIndex} label='{option.Label}'.", true));
            }

            optionsList.Add(optionButton);
            optionButtons.Add(optionButton);
        }

        RefreshModalControlsState();
    }

    private static void PopulateChipContainer(VisualElement container, IReadOnlyList<HexPitstopEffectChipData> chips)
    {
        if (container == null)
        {
            return;
        }

        container.Clear();
        if (chips == null)
        {
            return;
        }

        for (int index = 0; index < chips.Count; index++)
        {
            HexPitstopEffectChipData chip = chips[index];
            if (string.IsNullOrWhiteSpace(chip.Text))
            {
                continue;
            }

            Label chipLabel = new(chip.Text);
            chipLabel.AddToClassList("pitstop-effect-chip");

            switch (chip.Tone)
            {
                case HexPitstopEffectChipTone.Food:
                    chipLabel.AddToClassList("pitstop-effect-chip--food");
                    break;

                case HexPitstopEffectChipTone.Morale:
                    chipLabel.AddToClassList("pitstop-effect-chip--morale");
                    break;

                case HexPitstopEffectChipTone.Gold:
                    chipLabel.AddToClassList("pitstop-effect-chip--gold");
                    break;

                case HexPitstopEffectChipTone.Warning:
                    chipLabel.AddToClassList("pitstop-effect-chip--warning");
                    break;

                default:
                    chipLabel.AddToClassList("pitstop-effect-chip--neutral");
                    break;
            }

            if (chip.IsNegative)
            {
                chipLabel.AddToClassList("pitstop-effect-chip--negative");
            }

            container.Add(chipLabel);
        }
    }

    private void PopulateResourceSummary(IReadOnlyList<HexPitstopResourceViewData> resources)
    {
        if (resourceStripContainer == null)
        {
            return;
        }

        resourceStripContainer.Clear();
        if (resources == null)
        {
            return;
        }

        for (int index = 0; index < resources.Count; index++)
        {
            HexPitstopResourceViewData resource = resources[index];
            if (string.IsNullOrWhiteSpace(resource.Label))
            {
                continue;
            }

            VisualElement module = new();
            module.AddToClassList("pitstop-resource-mini");

            switch (resource.Tone)
            {
                case HexPitstopEffectChipTone.Food:
                    module.AddToClassList("pitstop-resource-mini--food");
                    break;

                case HexPitstopEffectChipTone.Morale:
                    module.AddToClassList("pitstop-resource-mini--morale");
                    break;

                case HexPitstopEffectChipTone.Gold:
                    module.AddToClassList("pitstop-resource-mini--gold");
                    break;
            }

            Label label = new(resource.Label);
            label.AddToClassList("pitstop-resource-mini-label");
            module.Add(label);

            Label value = new(resource.Value);
            value.AddToClassList("pitstop-resource-mini-value");
            module.Add(value);

            resourceStripContainer.Add(module);
        }
    }

    private void SetResultMode(bool isResult)
    {
        if (shellElement != null)
        {
            shellElement.EnableInClassList("pitstop-modal-shell--result", isResult);
        }
    }

    private void SetModalVisibility(bool visible)
    {
        hudDocumentController ??= UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        hudDocumentController?.SetGameplayModalState(visible);
        gameplayUiRootController?.SetLayerVisible(HexGameplayUiLayerId.Modal, visible);
        gameplayUiRootController?.SetLayerInteractive(HexGameplayUiLayerId.Modal, visible);

        if (modalRoot != null)
        {
            modalRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        isOpen = visible;
        LogDebug($"SetModalVisibility visible={visible} isOpen={isOpen}.");
    }

    private void HandleOptionClicked(int optionIndex)
    {
        if (areTransitionControlsLocked)
        {
            return;
        }

        LogDebug($"HandleOptionClicked index={optionIndex} callbackAssigned={optionSelected != null}.");
        Action<int> callback = optionSelected;
        callback?.Invoke(optionIndex);
    }

    private void HandleContinueClicked()
    {
        if (areTransitionControlsLocked)
        {
            return;
        }

        LogDebug($"HandleContinueClicked callbackAssigned={continueSelected != null}.");
        Action callback = continueSelected;
        Hide();
        callback?.Invoke();
    }

    private void ShowModalContent(bool playReveal)
    {
        if (playReveal)
        {
            PlayModalReveal();
            return;
        }

        StopModalReveal();
        CompleteModalReveal();
    }

    private void PlayModalReveal()
    {
        StopModalReveal();
        if (modalTransitionPlayer == null || shellElement == null)
        {
            SetElementOpacity(scrimElement ?? overlayElement, 1f);
            SetElementOpacity(shellElement, 1f);
            SetTransitionInteractionLock(false);
            return;
        }

        activeModalFadeProfile = CreateSimpleModalFadeRuntimeProfile();
        HexUiTransitionTargetSet targets = new HexUiTransitionTargetSet()
            .Register(ScrimTransitionTargetKey, scrimElement ?? overlayElement)
            .Register(ShellTransitionTargetKey, shellElement);

        bool started = modalTransitionPlayer.Play(
            activeModalFadeProfile,
            targets,
            CompleteModalReveal,
            SetTransitionInteractionLock,
            message => LogDebug(message, true));
        if (!started)
        {
            CompleteModalReveal();
        }
    }

    private void StopModalReveal()
    {
        modalTransitionPlayer?.Cancel(applyEndState: false);
        SetTransitionInteractionLock(false);
        ReleaseModalFadeProfile();
    }

    private void CompleteModalReveal()
    {
        SetElementOpacity(scrimElement ?? overlayElement, 1f);
        SetElementOpacity(shellElement, 1f);
        SetTransitionInteractionLock(false);
        ReleaseModalFadeProfile();
    }

    private HexUiTransitionProfile CreateSimpleModalFadeRuntimeProfile()
    {
        return HexUiTransitionProfile.CreateRuntimeProfile(
            simpleModalFadeProfile,
            "simple-modal-fade",
            HexUiTransitionEasing.EaseOut,
            CreateDefaultSimpleModalFadeTracks());
    }

    private static IReadOnlyList<HexUiTransitionFadeTrack> CreateDefaultSimpleModalFadeTracks()
    {
        return new[]
        {
            new HexUiTransitionFadeTrack
            {
                targetKey = ScrimTransitionTargetKey,
                startTime = 0f,
                duration = 0.12f,
                startOpacity = 0f,
                endOpacity = 1f,
                isRequired = true
            },
            new HexUiTransitionFadeTrack
            {
                targetKey = ShellTransitionTargetKey,
                startTime = 0.04f,
                duration = 0.16f,
                startOpacity = 0f,
                endOpacity = 1f,
                isRequired = true
            }
        };
    }

    private void SetTransitionInteractionLock(bool locked)
    {
        areTransitionControlsLocked = locked;
        RefreshModalControlsState();
    }

    private void RefreshModalControlsState()
    {
        bool isInteractionEnabled = !areTransitionControlsLocked;
        for (int index = 0; index < optionButtons.Count; index++)
        {
            Button optionButton = optionButtons[index];
            if (optionButton == null)
            {
                continue;
            }

            bool isOptionEnabled = optionButton.userData is bool enabled && enabled;
            SetButtonInteractionEnabled(optionButton, isInteractionEnabled && isOptionEnabled);
        }

        SetButtonInteractionEnabled(continueButton, isInteractionEnabled);
    }

    private static void SetButtonInteractionEnabled(Button button, bool enabled)
    {
        if (button == null)
        {
            return;
        }

        button.SetEnabled(enabled);
        button.pickingMode = enabled ? PickingMode.Position : PickingMode.Ignore;
        button.focusable = enabled;
    }

    private static void SetElementOpacity(VisualElement element, float opacity)
    {
        if (element != null)
        {
            element.style.opacity = Mathf.Clamp01(opacity);
        }
    }

    private void ReleaseModalFadeProfile()
    {
        if (activeModalFadeProfile != null)
        {
            UnityEngine.Object.Destroy(activeModalFadeProfile);
            activeModalFadeProfile = null;
        }
    }

    private void LogDebug(string message, bool verbose = false)
    {
        if (owner is PitstopEventModalPresenter presenter)
        {
            presenter.LogDebug(message, verbose);
            return;
        }

        Debug.Log($"[PitstopEventModal] {message}", owner);
    }
}
