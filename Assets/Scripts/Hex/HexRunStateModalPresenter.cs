using System;
using System.Collections.Generic;
using UnityEngine;
using UIE = UnityEngine.UIElements;

[CreateAssetMenu(
    fileName = "ActTransitionPlaceholderArtLibrary",
    menuName = "Hex/Acts/Act Transition Placeholder Art Library")]
public sealed class HexActTransitionPlaceholderArtLibrary : ScriptableObject
{
    public Texture2D[] act1ToAct2 = Array.Empty<Texture2D>();
    public Texture2D[] act2ToAct3 = Array.Empty<Texture2D>();

    public Texture2D GetRandomTextureForCompletedAct(int completedAct)
    {
        Texture2D[] pool = completedAct switch
        {
            1 => act1ToAct2,
            2 => act2ToAct3,
            _ => Array.Empty<Texture2D>()
        };

        if (pool == null || pool.Length == 0)
        {
            return null;
        }

        int validCount = 0;
        for (int index = 0; index < pool.Length; index++)
        {
            if (pool[index] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int chosenIndex = UnityEngine.Random.Range(0, validCount);
        for (int index = 0; index < pool.Length; index++)
        {
            Texture2D texture = pool[index];
            if (texture == null)
            {
                continue;
            }

            if (chosenIndex == 0)
            {
                return texture;
            }

            chosenIndex--;
        }

        return null;
    }
}

public sealed class HexActTransitionModalPresenter : MonoBehaviour
{
    private HexActTransitionModalDocumentController documentController;
    private HexGameplayUiRootController gameplayUiRootController;

    public bool IsOpen => documentController != null && documentController.IsOpen;

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
        EnsureView();
        documentController?.Show(displayData, onContinueRequested, openSelectionImmediately: false);
    }

    public void ShowTransitionSelection(HexActTransitionDisplayData displayData, Action<HexBoonDefinition> onContinueRequested)
    {
        EnsureView();
        documentController?.Show(displayData, onContinueRequested, openSelectionImmediately: true);
    }

    public void Hide()
    {
        documentController?.Hide();
    }

    private void EnsureView()
    {
        documentController ??= new HexActTransitionModalDocumentController(this);
        documentController.EnsureInitialized();
    }

    internal void LogDebug(string message, bool verbose = false)
    {
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this, createIfMissing: false);
        gameplayUiRootController?.EnsureInitialized();
        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.LogDiagnostic("TransitionModal", message, this, verbose);
            return;
        }

        Debug.Log($"[GameplayUI:TransitionModal] {message}", this);
    }
}

internal sealed class HexActTransitionModalDocumentController
{
    private const string LayoutResourcePath = "UI/Modal/HexActTransitionModal";
    private const string StyleSheetResourcePath = "UI/Modal/HexActTransitionModalStyles";
    private const string ModalMountName = "act-transition-modal-mount";
    private const string PlaceholderArtLibraryResourcePath = "Acts/ActTransitionPlaceholderArtLibrary";
    private const string RimouskiFontEditorAssetPath = "Assets/Art/Fonts/rimouski sb.otf";

    private enum TransitionScreenMode
    {
        None,
        Intermission,
        Selection
    }

    private readonly MonoBehaviour owner;
    private readonly List<TransitionBoonCardView> cardViews = new();

    private UIE.VisualTreeAsset layoutAsset;
    private UIE.StyleSheet styleSheet;
    private HexActTransitionPlaceholderArtLibrary placeholderArtLibrary;
    private Font rimouskiFont;
    private HexGameplayUiRootController gameplayUiRootController;
    private UIE.VisualElement modalMount;
    private UIE.VisualElement modalRoot;
    private UIE.VisualElement intermissionShell;
    private UIE.VisualElement selectionShell;
    private UIE.Label intermissionMetaLabel;
    private UIE.Label intermissionTitleLabel;
    private UIE.Label intermissionBodyLabel;
    private UIE.Label intermissionSummaryLabel;
    private UIE.Image intermissionIllustrationImage;
    private UIE.Label intermissionIllustrationPlaceholderLabel;
    private UIE.VisualElement carryOverChipsContainer;
    private UIE.VisualElement grantChipsContainer;
    private UIE.Label grantEmptyLabel;
    private UIE.Button intermissionContinueButton;
    private UIE.Label selectionPromptLabel;
    private UIE.Label selectionNoteLabel;
    private UIE.Label selectionResourceLabel;
    private UIE.VisualElement selectionResourceStripContainer;
    private UIE.VisualElement selectionCardsRow;
    private UIE.Label detailMetaLabel;
    private UIE.Label detailTitleLabel;
    private UIE.Label detailKeywordLabel;
    private UIE.Label detailEffectLabel;
    private UIE.VisualElement detailGlossaryContainer;
    private UIE.Label detailFlavorLabel;
    private UIE.Label detailNoteLabel;
    private UIE.Button selectionContinueButton;
    private HexHudDocumentController hudDocumentController;
    private Action<HexBoonDefinition> continueRequested;
    private HexActTransitionDisplayData activeDisplayData;
    private HexBoonDefinition[] boonOptions = Array.Empty<HexBoonDefinition>();
    private HexBoonDefinition selectedBoon;
    private HexBoonDefinition hoveredBoon;
    private HexBoonDefinition focusedBoon;
    private TransitionScreenMode screenMode;
    private bool isInitialized;
    private bool isOpen;

    private sealed class TransitionBoonCardView
    {
        public UIE.VisualElement Root;
        public UIE.Label TitleLabel;
        public UIE.Image ArtImage;
        public UIE.Label ArtPlaceholderLabel;
        public UIE.Label SummaryLabel;
        public UIE.Label FamilyLabel;
        public HexBoonDefinition Boon;
        public bool IsHovered;
        public bool IsFocused;
    }

    public HexActTransitionModalDocumentController(MonoBehaviour owner)
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

        layoutAsset ??= Resources.Load<UIE.VisualTreeAsset>(LayoutResourcePath);
        styleSheet ??= Resources.Load<UIE.StyleSheet>(StyleSheetResourcePath);
        placeholderArtLibrary ??= Resources.Load<HexActTransitionPlaceholderArtLibrary>(PlaceholderArtLibraryResourcePath);
        rimouskiFont ??= LoadRimouskiFont();
        if (layoutAsset == null || styleSheet == null)
        {
            Debug.LogError("HexActTransitionModalDocumentController could not load the UI Toolkit modal assets from Resources.", owner);
            return;
        }

        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(owner);
        if (gameplayUiRootController == null)
        {
            Debug.LogError("HexActTransitionModalDocumentController could not resolve the shared gameplay UI root.", owner);
            return;
        }

        gameplayUiRootController.EnsureInitialized();
        modalMount = gameplayUiRootController.RequestLayerMount(
            HexGameplayUiLayerId.Modal,
            ModalMountName,
            nameof(HexActTransitionModalDocumentController),
            false,
            owner);
        if (modalMount == null)
        {
            Debug.LogError("HexActTransitionModalDocumentController could not bind to the shared modal layer.", owner);
            return;
        }

        modalMount.Clear();
        modalMount.styleSheets.Clear();
        modalMount.styleSheets.Add(styleSheet);
        layoutAsset.CloneTree(modalMount);

        modalRoot = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-modal-root");
        intermissionShell = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-intermission-shell");
        selectionShell = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-selection-shell");

        intermissionMetaLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-meta");
        intermissionTitleLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-title");
        intermissionBodyLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-body");
        intermissionSummaryLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-summary-label");
        intermissionIllustrationImage = UIE.UQueryExtensions.Q<UIE.Image>(modalMount, "act-transition-intermission-illustration-image");
        intermissionIllustrationPlaceholderLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-illustration-placeholder");
        if (intermissionIllustrationImage != null)
        {
            intermissionIllustrationImage.scaleMode = ScaleMode.ScaleAndCrop;
        }
        carryOverChipsContainer = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-intermission-carry-chips");
        grantChipsContainer = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-intermission-grant-chips");
        grantEmptyLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-grant-empty");
        intermissionContinueButton = UIE.UQueryExtensions.Q<UIE.Button>(modalMount, "act-transition-intermission-button");

        selectionPromptLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-prompt");
        selectionNoteLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-note");
        selectionResourceLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-resource-label");
        selectionResourceStripContainer = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-selection-resource-strip");
        selectionCardsRow = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-selection-card-row");
        detailMetaLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-detail-meta");
        detailTitleLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-detail-title");
        detailKeywordLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-detail-keywords");
        detailEffectLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-detail-effect");
        detailGlossaryContainer = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-selection-detail-glossary");
        detailFlavorLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-detail-flavor");
        detailNoteLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-detail-note");
        selectionContinueButton = UIE.UQueryExtensions.Q<UIE.Button>(modalMount, "act-transition-selection-button");
        ApplyIntermissionTypographyTheme();
        ApplySelectionTypographyTheme();

        if (intermissionContinueButton != null)
        {
            intermissionContinueButton.clicked += HandleIntermissionContinueClicked;
        }

        if (selectionContinueButton != null)
        {
            selectionContinueButton.clicked += HandleSelectionContinueClicked;
        }

        if (modalRoot != null)
        {
            modalRoot.style.display = UIE.DisplayStyle.None;
        }

        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Modal, false);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Modal, false);
        LogDebug(
            $"Initialized shared-root transition modal. modalRootFound={modalRoot != null} intermissionFound={intermissionShell != null} selectionFound={selectionShell != null} cardsRowFound={selectionCardsRow != null}.");
        isInitialized = true;
    }

    public void Show(HexActTransitionDisplayData displayData, Action<HexBoonDefinition> onContinueRequested, bool openSelectionImmediately)
    {
        EnsureInitialized();
        if (!isInitialized || modalRoot == null)
        {
            return;
        }

        continueRequested = onContinueRequested;
        activeDisplayData = displayData;
        boonOptions = displayData.BoonOptions ?? Array.Empty<HexBoonDefinition>();
        selectedBoon = null;
        hoveredBoon = null;
        focusedBoon = null;
        screenMode = TransitionScreenMode.None;

        PopulateIntermission();
        PopulateSelection();

        SetModalVisibility(true);
        LogDebug(
            $"ShowTransition title='{activeDisplayData.Title}' nextAct={activeDisplayData.NextActNumber} requiresSelection={activeDisplayData.RequiresBoonSelection} boonCount={boonOptions.Length} openSelectionImmediately={openSelectionImmediately}.");

        if (openSelectionImmediately && activeDisplayData.RequiresBoonSelection)
        {
            ShowScreen(TransitionScreenMode.Selection);
            return;
        }

        ShowScreen(TransitionScreenMode.Intermission);
    }

    public void Hide()
    {
        continueRequested = null;
        activeDisplayData = default;
        boonOptions = Array.Empty<HexBoonDefinition>();
        selectedBoon = null;
        hoveredBoon = null;
        focusedBoon = null;
        screenMode = TransitionScreenMode.None;
        cardViews.Clear();

        if (selectionCardsRow != null)
        {
            selectionCardsRow.Clear();
        }

        SetModalVisibility(false);
        LogDebug("Hide transition modal.");
    }

    private void PopulateIntermission()
    {
        if (intermissionMetaLabel == null)
        {
            return;
        }

        intermissionMetaLabel.text = BuildTransitionMetaLabel(activeDisplayData);
        intermissionTitleLabel.text = string.IsNullOrWhiteSpace(activeDisplayData.Title)
            ? "Act Complete"
            : activeDisplayData.Title.Trim();
        intermissionBodyLabel.text = string.IsNullOrWhiteSpace(activeDisplayData.Body)
            ? "The caravan gathers itself for the road ahead."
            : NormalizeIntermissionBody(activeDisplayData.Body);

        ApplyIntermissionIllustration(activeDisplayData.IntermissionIllustration);
        RebuildIntermissionCarryOverSummary(activeDisplayData.CurrentResources);
        RebuildIntermissionGrantSummary(activeDisplayData);

        string intermissionLabel = activeDisplayData.RequiresBoonSelection
            ? activeDisplayData.IntermissionContinueButtonLabel
            : activeDisplayData.ContinueButtonLabel;
        if (intermissionContinueButton != null)
        {
            intermissionContinueButton.text = string.IsNullOrWhiteSpace(intermissionLabel) ? "Continue" : intermissionLabel.Trim();
        }
    }

    private void PopulateSelection()
    {
        if (selectionPromptLabel == null)
        {
            return;
        }

        selectionPromptLabel.text = BuildSelectionHeaderTitle(activeDisplayData.NextActNumber);
        if (selectionNoteLabel != null)
        {
            string supportLine = BuildSelectionSupportLine(activeDisplayData);
            selectionNoteLabel.text = supportLine;
            selectionNoteLabel.style.display = string.IsNullOrWhiteSpace(supportLine) ? UIE.DisplayStyle.None : UIE.DisplayStyle.Flex;
        }

        RebuildSelectionCarryOverSummary(activeDisplayData.CurrentResources);
        RebuildSelectionCards();
        RefreshSelectionActionState();
        RefreshDetailPanel();
    }

    private void ShowScreen(TransitionScreenMode mode)
    {
        screenMode = mode;
        bool showSelection = mode == TransitionScreenMode.Selection && activeDisplayData.RequiresBoonSelection;
        if (intermissionShell != null)
        {
            intermissionShell.style.display = mode == TransitionScreenMode.Intermission ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
        }

        if (selectionShell != null)
        {
            selectionShell.style.display = showSelection ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
        }

        LogDebug(
            $"ShowScreen mode={mode} intermissionVisible={intermissionShell?.resolvedStyle.display == UIE.DisplayStyle.Flex} selectionVisible={selectionShell?.resolvedStyle.display == UIE.DisplayStyle.Flex}.",
            true);
    }

    private void HandleIntermissionContinueClicked()
    {
        LogDebug(
            $"HandleIntermissionContinueClicked requiresSelection={activeDisplayData.RequiresBoonSelection} boonCount={boonOptions.Length} modeBefore={screenMode}.");
        if (activeDisplayData.RequiresBoonSelection)
        {
            ShowScreen(TransitionScreenMode.Selection);
            return;
        }

        Action<HexBoonDefinition> callback = continueRequested;
        Hide();
        callback?.Invoke(null);
    }

    private void HandleSelectionContinueClicked()
    {
        if (selectedBoon == null)
        {
            LogDebug("HandleSelectionContinueClicked ignored because no boon is selected.", true);
            return;
        }

        Action<HexBoonDefinition> callback = continueRequested;
        HexBoonDefinition chosenBoon = selectedBoon;
        LogDebug($"HandleSelectionContinueClicked selectedBoon='{chosenBoon.id}'.");
        Hide();
        callback?.Invoke(chosenBoon);
    }

    private void RebuildIntermissionCarryOverSummary(CaravanResourceSnapshot currentResources)
    {
        if (carryOverChipsContainer == null)
        {
            return;
        }

        carryOverChipsContainer.Clear();
        carryOverChipsContainer.Add(CreateJournalChip("Food", currentResources.Food.ToString()));
        carryOverChipsContainer.Add(CreateJournalChip("Morale", currentResources.Morale.ToString()));
        carryOverChipsContainer.Add(CreateJournalChip("Gold", currentResources.Gold.ToString()));
    }

    private void RebuildIntermissionGrantSummary(HexActTransitionDisplayData displayData)
    {
        if (grantChipsContainer == null || grantEmptyLabel == null)
        {
            return;
        }

        grantChipsContainer.Clear();
        int chipCount = 0;
        chipCount += TryAddGrantChip(grantChipsContainer, "Food", displayData.BetweenActFood);
        chipCount += TryAddGrantChip(grantChipsContainer, "Morale", displayData.BetweenActMorale);
        chipCount += TryAddGrantChip(grantChipsContainer, "Gold", displayData.BetweenActGold);
        grantEmptyLabel.style.display = chipCount == 0 ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
    }

    private void RebuildSelectionCarryOverSummary(CaravanResourceSnapshot currentResources)
    {
        if (selectionResourceStripContainer == null)
        {
            return;
        }

        selectionResourceStripContainer.Clear();
        selectionResourceStripContainer.Add(CreateResourceMiniModule("Food", currentResources.Food.ToString(), "act-transition-resource-mini--food"));
        selectionResourceStripContainer.Add(CreateResourceMiniModule("Morale", currentResources.Morale.ToString(), "act-transition-resource-mini--morale"));
        selectionResourceStripContainer.Add(CreateResourceMiniModule("Gold", currentResources.Gold.ToString(), "act-transition-resource-mini--gold"));
    }

    private void RebuildSelectionCards()
    {
        cardViews.Clear();
        if (selectionCardsRow == null)
        {
            return;
        }

        selectionCardsRow.Clear();
        for (int index = 0; index < boonOptions.Length; index++)
        {
            HexBoonDefinition boon = boonOptions[index];
            if (boon == null)
            {
                continue;
            }

            boon.Validate();
            TransitionBoonCardView cardView = CreateCardView(boon);
            cardViews.Add(cardView);
            selectionCardsRow.Add(cardView.Root);
        }

        RefreshCardVisuals();
    }

    private TransitionBoonCardView CreateCardView(HexBoonDefinition boon)
    {
        UIE.VisualElement root = new();
        root.AddToClassList("act-transition-card");
        root.focusable = true;
        root.tabIndex = 0;
        root.pickingMode = UIE.PickingMode.Position;

        UIE.Label titleLabel = new();
        titleLabel.AddToClassList("act-transition-card-title");
        titleLabel.text = boon.GetResolvedDisplayName();
        ApplyRimouskiFont(titleLabel);

        UIE.VisualElement artFrame = new();
        artFrame.AddToClassList("act-transition-card-art-frame");

        UIE.VisualElement artInset = new();
        artInset.AddToClassList("act-transition-card-art-inset");

        UIE.Image artImage = new();
        artImage.AddToClassList("act-transition-card-art-image");
        artImage.scaleMode = ScaleMode.ScaleAndCrop;

        UIE.Label artPlaceholderLabel = new(string.Empty);
        artPlaceholderLabel.AddToClassList("act-transition-card-art-placeholder");

        artInset.Add(artImage);
        artInset.Add(artPlaceholderLabel);
        artFrame.Add(artInset);

        UIE.Label summaryLabel = new();
        summaryLabel.AddToClassList("act-transition-card-summary");
        summaryLabel.text = boon.GetCardSummary();

        UIE.Label familyLabel = new();
        familyLabel.AddToClassList("act-transition-card-family");
        familyLabel.text = $"{FormatArchetype(boon.archetypeFamily)} Family";
        ApplyRimouskiFont(familyLabel);

        root.Add(titleLabel);
        root.Add(artFrame);
        root.Add(summaryLabel);
        root.Add(familyLabel);

        Texture cardTexture = boon.icon != null ? boon.icon.texture : ResolveRandomPlaceholderTexture();
        if (cardTexture != null)
        {
            artImage.image = cardTexture;
            artImage.style.display = UIE.DisplayStyle.Flex;
            artPlaceholderLabel.style.display = UIE.DisplayStyle.None;
        }
        else
        {
            artImage.image = null;
            artImage.style.display = UIE.DisplayStyle.None;
            artPlaceholderLabel.style.display = UIE.DisplayStyle.Flex;
        }

        root.RegisterCallback<UIE.ClickEvent>(_ => HandleCardClicked(boon));
        root.RegisterCallback<UIE.PointerEnterEvent>(_ => HandleCardHovered(boon, true));
        root.RegisterCallback<UIE.PointerLeaveEvent>(_ => HandleCardHovered(boon, false));
        root.RegisterCallback<UIE.FocusInEvent>(_ => HandleCardFocused(boon, true));
        root.RegisterCallback<UIE.FocusOutEvent>(_ => HandleCardFocused(boon, false));
        root.RegisterCallback<UIE.KeyDownEvent>(evt =>
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.Space)
            {
                return;
            }

            HandleCardClicked(boon);
            evt.StopPropagation();
        });

        root.SetEnabled(boon.isEnabled);

        return new TransitionBoonCardView
        {
            Root = root,
            TitleLabel = titleLabel,
            ArtImage = artImage,
            ArtPlaceholderLabel = artPlaceholderLabel,
            SummaryLabel = summaryLabel,
            FamilyLabel = familyLabel,
            Boon = boon
        };
    }

    private void HandleCardClicked(HexBoonDefinition boon)
    {
        if (boon == null || !boon.isEnabled)
        {
            return;
        }

        selectedBoon = boon;
        LogDebug($"HandleCardClicked boon='{boon.id}'.", true);
        RefreshSelectionActionState();
        RefreshCardVisuals();
        RefreshDetailPanel();
    }

    private void HandleCardHovered(HexBoonDefinition boon, bool isHovered)
    {
        for (int index = 0; index < cardViews.Count; index++)
        {
            TransitionBoonCardView view = cardViews[index];
            if (view.Boon == null || !string.Equals(view.Boon.id, boon.id, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            view.IsHovered = isHovered;
            break;
        }

        if (isHovered)
        {
            hoveredBoon = boon;
        }
        else if (hoveredBoon != null && string.Equals(hoveredBoon.id, boon.id, StringComparison.OrdinalIgnoreCase))
        {
            hoveredBoon = null;
        }

        RefreshCardVisuals();
        RefreshDetailPanel();
    }

    private void HandleCardFocused(HexBoonDefinition boon, bool isFocused)
    {
        for (int index = 0; index < cardViews.Count; index++)
        {
            TransitionBoonCardView view = cardViews[index];
            if (view.Boon == null || !string.Equals(view.Boon.id, boon.id, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            view.IsFocused = isFocused;
            break;
        }

        if (isFocused)
        {
            focusedBoon = boon;
        }
        else if (focusedBoon != null && string.Equals(focusedBoon.id, boon.id, StringComparison.OrdinalIgnoreCase))
        {
            focusedBoon = null;
        }

        RefreshCardVisuals();
        RefreshDetailPanel();
    }

    private void RefreshSelectionActionState()
    {
        if (selectionContinueButton == null)
        {
            return;
        }

        selectionContinueButton.text = string.IsNullOrWhiteSpace(activeDisplayData.ContinueButtonLabel)
            ? "Continue"
            : activeDisplayData.ContinueButtonLabel.Trim();
        selectionContinueButton.SetEnabled(selectedBoon != null);
    }

    private void RefreshCardVisuals()
    {
        for (int index = 0; index < cardViews.Count; index++)
        {
            TransitionBoonCardView view = cardViews[index];
            if (view?.Root == null)
            {
                continue;
            }

            bool isSelected = selectedBoon != null
                && view.Boon != null
                && string.Equals(selectedBoon.id, view.Boon.id, StringComparison.OrdinalIgnoreCase);
            bool isHovered = view.IsHovered || view.IsFocused;

            view.Root.EnableInClassList("act-transition-card--selected", isSelected);
            view.Root.EnableInClassList("act-transition-card--hovered", isHovered);
            view.Root.EnableInClassList("act-transition-card--disabled", view.Boon == null || !view.Boon.isEnabled);
        }
    }

    private void RefreshDetailPanel()
    {
        if (detailMetaLabel == null)
        {
            return;
        }

        HexBoonDefinition inspectBoon = hoveredBoon ?? focusedBoon ?? selectedBoon;
        if (inspectBoon == null)
        {
            detailMetaLabel.text = "Inspect";
            detailTitleLabel.text = "Select a boon to inspect it.";
            SetDetailLabel(detailKeywordLabel, string.Empty);
            SetDetailLabel(detailEffectLabel, string.Empty);
            RebuildSelectionGlossary(null);
            SetDetailLabel(detailFlavorLabel, string.Empty);
            SetDetailLabel(detailNoteLabel, string.Empty);
            return;
        }

        inspectBoon.Validate();
        detailMetaLabel.text = BuildSelectionDetailMeta(inspectBoon);
        detailTitleLabel.text = inspectBoon.GetResolvedDisplayName();
        SetDetailLabel(detailKeywordLabel, BuildSelectionKeywordHeading(inspectBoon));
        SetDetailLabel(detailEffectLabel, BuildSelectionDescription(inspectBoon));
        RebuildSelectionGlossary(inspectBoon);
        SetDetailLabel(detailFlavorLabel, NormalizeFlavorLine(inspectBoon.flavorText));
        SetDetailLabel(detailNoteLabel, BuildSelectionFamilyNote(inspectBoon));
    }

    private string BuildSelectionDetailMeta(HexBoonDefinition boon)
    {
        bool isSelected = selectedBoon != null && string.Equals(selectedBoon.id, boon.id, StringComparison.OrdinalIgnoreCase);
        return isSelected ? "Selected boon" : "Inspect";
    }

    private void ApplyIntermissionIllustration(Sprite illustration)
    {
        if (intermissionIllustrationImage == null || intermissionIllustrationPlaceholderLabel == null)
        {
            return;
        }

        Texture illustrationTexture = illustration != null ? illustration.texture : ResolveRandomPlaceholderTexture();
        if (illustrationTexture != null)
        {
            intermissionIllustrationImage.image = illustrationTexture;
            intermissionIllustrationImage.style.display = UIE.DisplayStyle.Flex;
            intermissionIllustrationPlaceholderLabel.style.display = UIE.DisplayStyle.None;
            return;
        }

        intermissionIllustrationImage.image = null;
        intermissionIllustrationImage.style.display = UIE.DisplayStyle.None;
        intermissionIllustrationPlaceholderLabel.text = string.Empty;
        intermissionIllustrationPlaceholderLabel.style.display = UIE.DisplayStyle.Flex;
    }

    private Texture2D ResolveRandomPlaceholderTexture()
    {
        if (placeholderArtLibrary == null)
        {
            placeholderArtLibrary = Resources.Load<HexActTransitionPlaceholderArtLibrary>(PlaceholderArtLibraryResourcePath);
        }

        if (placeholderArtLibrary == null)
        {
            return null;
        }

        return placeholderArtLibrary.GetRandomTextureForCompletedAct(activeDisplayData.CompletedActNumber);
    }

    private UIE.VisualElement CreateJournalChip(string label, string value)
    {
        UIE.VisualElement chip = new();
        chip.AddToClassList("act-transition-journal-chip");
        AddResourceModifierClass(chip, label);

        UIE.Label chipLabel = new($"{label} {value}");
        chipLabel.AddToClassList("act-transition-journal-chip-label");
        ApplyRimouskiFont(chipLabel);
        chip.Add(chipLabel);
        return chip;
    }

    private int TryAddGrantChip(UIE.VisualElement parent, string label, int value)
    {
        if (value == 0)
        {
            return 0;
        }

        UIE.VisualElement chip = new();
        chip.AddToClassList("act-transition-journal-chip");
        AddResourceModifierClass(chip, label);

        UIE.Label chipLabel = new($"{FormatSigned(value)} {label}");
        chipLabel.AddToClassList("act-transition-journal-chip-label");
        ApplyRimouskiFont(chipLabel);
        chip.Add(chipLabel);
        parent.Add(chip);
        return 1;
    }

    private UIE.VisualElement CreateResourceMiniModule(string label, string value, string toneClass)
    {
        UIE.VisualElement module = new();
        module.AddToClassList("act-transition-resource-mini");
        module.AddToClassList(toneClass);

        UIE.Label labelElement = new(label);
        labelElement.AddToClassList("act-transition-resource-mini-label");
        UIE.Label valueElement = new(value);
        valueElement.AddToClassList("act-transition-resource-mini-value");
        ApplyRimouskiFont(labelElement);
        ApplyRimouskiFont(valueElement);

        module.Add(labelElement);
        module.Add(valueElement);
        return module;
    }

    private void RebuildSelectionGlossary(HexBoonDefinition boon)
    {
        if (detailGlossaryContainer == null)
        {
            return;
        }

        detailGlossaryContainer.Clear();
        if (boon?.keywords == null || boon.keywords.Length == 0)
        {
            detailGlossaryContainer.style.display = UIE.DisplayStyle.None;
            return;
        }

        int rowCount = 0;
        for (int index = 0; index < boon.keywords.Length; index++)
        {
            HexBoonKeywordPresentationData keyword = boon.keywords[index];
            if (keyword == null || !keyword.IsUsable)
            {
                continue;
            }

            UIE.VisualElement row = CreateSelectionGlossaryRow(keyword);
            detailGlossaryContainer.Add(row);
            rowCount++;
        }

        detailGlossaryContainer.style.display = rowCount == 0 ? UIE.DisplayStyle.None : UIE.DisplayStyle.Flex;
    }

    private UIE.VisualElement CreateSelectionGlossaryRow(HexBoonKeywordPresentationData keyword)
    {
        UIE.VisualElement row = new();
        row.AddToClassList("act-transition-glossary-row");

        UIE.Label termLabel = new(keyword.label.Trim());
        termLabel.AddToClassList("act-transition-glossary-term");
        ApplyRimouskiFont(termLabel);

        UIE.Label descriptionLabel = new(
            string.IsNullOrWhiteSpace(keyword.explanation)
                ? "No explanation listed."
                : NormalizeInlineText(keyword.explanation));
        descriptionLabel.AddToClassList("act-transition-glossary-text");

        row.Add(termLabel);
        row.Add(descriptionLabel);
        return row;
    }

    private static void SetDetailLabel(UIE.Label label, string text)
    {
        if (label == null)
        {
            return;
        }

        bool hasText = !string.IsNullOrWhiteSpace(text);
        label.text = hasText ? text.Trim() : string.Empty;
        label.style.display = hasText ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
    }

    private static void AddResourceModifierClass(UIE.VisualElement element, string label)
    {
        switch (label)
        {
            case "Food":
                element.AddToClassList("act-transition-resource--food");
                break;

            case "Morale":
                element.AddToClassList("act-transition-resource--morale");
                break;

            case "Gold":
                element.AddToClassList("act-transition-resource--gold");
                break;
        }
    }

    private static string BuildTransitionMetaLabel(HexActTransitionDisplayData displayData)
    {
        if (displayData.CompletedActNumber > 0 && displayData.NextActNumber > 0)
        {
            return $"ROAD INTERMISSION · ACT {displayData.CompletedActNumber} TO ACT {displayData.NextActNumber}";
        }

        return "ROAD INTERMISSION";
    }

    private static string BuildSelectionHeaderTitle(int nextActNumber)
    {
        return nextActNumber > 0
            ? $"Choose one boon for Act {nextActNumber}"
            : "Choose one boon for the next act";
    }

    private static string BuildSelectionSupportLine(HexActTransitionDisplayData displayData)
    {
        string contextNote = NormalizeInlineText(displayData.SelectionContextNote);
        if (!string.IsNullOrWhiteSpace(contextNote))
        {
            if (contextNote.IndexOf("determines the family for Act 3", StringComparison.OrdinalIgnoreCase) >= 0
                || contextNote.IndexOf("locks the Act 3 family", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "This choice determines the family for Act 3.";
            }

            return contextNote;
        }

        string prompt = NormalizeInlineText(displayData.SelectionPrompt);
        if (prompt.IndexOf("locked family", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Choose from the locked family for the final stretch ahead.";
        }

        return string.Empty;
    }

    private static string BuildSelectionKeywordHeading(HexBoonDefinition boon)
    {
        string keywordLine = boon?.GetKeywordLine() ?? string.Empty;
        return string.IsNullOrWhiteSpace(keywordLine) ? string.Empty : $"Keywords: {keywordLine}";
    }

    private static string BuildSelectionDescription(HexBoonDefinition boon)
    {
        string normalized = NormalizeInlineText(boon?.description);
        return string.IsNullOrWhiteSpace(normalized) ? "No gameplay bonus described." : normalized;
    }

    private string BuildSelectionFamilyNote(HexBoonDefinition boon)
    {
        string familyNote = $"Family: {FormatArchetype(boon.archetypeFamily)}.";
        string contextNote = NormalizeInlineText(activeDisplayData.SelectionContextNote);
        if (string.IsNullOrWhiteSpace(contextNote))
        {
            return familyNote;
        }

        if (contextNote.IndexOf("determines the family for Act 3", StringComparison.OrdinalIgnoreCase) >= 0
            || contextNote.IndexOf("locks the Act 3 family", StringComparison.OrdinalIgnoreCase) >= 0
            || contextNote.IndexOf("already locked for Act 3", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return familyNote;
        }

        return $"{familyNote} {contextNote}";
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

    private static string NormalizeFlavorLine(string rawText)
    {
        string normalized = NormalizeInlineText(rawText);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        int sentenceBreakIndex = FindSentenceBreakIndex(normalized);
        if (sentenceBreakIndex > 0)
        {
            string sentence = normalized.Substring(0, sentenceBreakIndex).Trim();
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                return sentence;
            }
        }

        const int maxLength = 96;
        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized.Substring(0, maxLength).TrimEnd()}...";
    }

    private static int FindSentenceBreakIndex(string text)
    {
        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];
            if (current != '.' && current != '!' && current != '?')
            {
                continue;
            }

            return index + 1;
        }

        return -1;
    }

    private static string NormalizeIntermissionBody(string rawText)
    {
        return NormalizeInlineText(rawText);
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
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

    private void ApplyIntermissionTypographyTheme()
    {
        ApplyRimouskiFont(intermissionMetaLabel);
        ApplyRimouskiFont(intermissionTitleLabel);
        ApplyRimouskiFont(intermissionSummaryLabel);
        ApplyRimouskiFont(intermissionContinueButton);
    }

    private void ApplySelectionTypographyTheme()
    {
        ApplyRimouskiFont(selectionPromptLabel);
        ApplyRimouskiFont(selectionResourceLabel);
        ApplyRimouskiFont(detailMetaLabel);
        ApplyRimouskiFont(detailTitleLabel);
        ApplyRimouskiFont(selectionContinueButton);
    }

    private void ApplyRimouskiFont(UIE.VisualElement element)
    {
        if (element == null || rimouskiFont == null)
        {
            return;
        }

        element.style.unityFont = new UIE.StyleFont(rimouskiFont);
        element.style.unityFontDefinition = UIE.FontDefinition.FromFont(rimouskiFont);
    }

    private static Font LoadRimouskiFont()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(RimouskiFontEditorAssetPath);
#else
        return null;
#endif
    }

    private void SetModalVisibility(bool visible)
    {
        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        hudDocumentController?.SetGameplayModalState(visible);
        gameplayUiRootController?.SetLayerVisible(HexGameplayUiLayerId.Modal, visible);
        gameplayUiRootController?.SetLayerInteractive(HexGameplayUiLayerId.Modal, visible);

        if (modalRoot != null)
        {
            modalRoot.style.display = visible ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
        }

        isOpen = visible;
        LogDebug($"SetModalVisibility visible={visible} isOpen={isOpen}.");
    }

    private void LogDebug(string message, bool verbose = false)
    {
        if (owner is HexActTransitionModalPresenter presenter)
        {
            presenter.LogDebug(message, verbose);
            return;
        }

        Debug.Log($"[GameplayUI:TransitionModal] {message}", owner);
    }
}

public sealed class HexRunEndModalPresenter : MonoBehaviour
{
    private HexSimpleActionModalDocumentController documentController;
    private HexGameplayUiRootController gameplayUiRootController;

    public bool IsOpen => documentController != null && documentController.IsOpen;

    public void ShowVictory(Action onRetryRequested)
    {
        LogDebug("ShowVictory requested.");
        EnsureView();
        documentController?.Show(
            "Victory",
            "The caravan completed the road and reached the next horizon.",
            "Retry",
            onRetryRequested);
    }

    public void ShowDefeat(Action onRetryRequested)
    {
        LogDebug("ShowDefeat requested.");
        EnsureView();
        documentController?.Show(
            "Game Over",
            "The caravan could not withstand the pressure of the journey.",
            "Retry",
            onRetryRequested);
    }

    public void Hide()
    {
        LogDebug("Hide requested.");
        documentController?.Hide();
    }

    private void EnsureView()
    {
        documentController ??= new HexSimpleActionModalDocumentController(
            this,
            "run-end-modal-mount",
            "RunEndModal");
        documentController.EnsureInitialized();
    }

    internal void LogDebug(string message, bool verbose = false)
    {
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this, createIfMissing: false);
        gameplayUiRootController?.EnsureInitialized();
        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.LogDiagnostic("RunEndModal", message, this, verbose);
            return;
        }

        Debug.Log($"[GameplayUI:RunEndModal] {message}", this);
    }
}

internal sealed class HexSimpleActionModalDocumentController
{
    private const string LayoutResourcePath = "UI/Modal/HexSimpleActionModal";
    private const string StyleSheetResourcePath = "UI/Modal/HexSimpleActionModalStyles";

    private readonly MonoBehaviour owner;
    private readonly string mountName;
    private readonly string diagnosticScope;

    private UIE.VisualTreeAsset layoutAsset;
    private UIE.StyleSheet styleSheet;
    private HexGameplayUiRootController gameplayUiRootController;
    private UIE.VisualElement modalMount;
    private UIE.VisualElement modalRoot;
    private UIE.VisualElement shellElement;
    private UIE.Label titleLabel;
    private UIE.Label bodyLabel;
    private UIE.Button actionButton;
    private HexHudDocumentController hudDocumentController;
    private Action actionRequested;
    private bool isInitialized;
    private bool isOpen;

    public HexSimpleActionModalDocumentController(MonoBehaviour owner, string mountName, string diagnosticScope)
    {
        this.owner = owner;
        this.mountName = mountName;
        this.diagnosticScope = diagnosticScope;
    }

    public bool IsOpen => isOpen;

    public void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        layoutAsset ??= Resources.Load<UIE.VisualTreeAsset>(LayoutResourcePath);
        styleSheet ??= Resources.Load<UIE.StyleSheet>(StyleSheetResourcePath);
        if (layoutAsset == null || styleSheet == null)
        {
            Debug.LogError($"HexSimpleActionModalDocumentController could not load modal assets for {diagnosticScope}.", owner);
            return;
        }

        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(owner);
        if (gameplayUiRootController == null)
        {
            Debug.LogError($"HexSimpleActionModalDocumentController could not resolve the shared gameplay UI root for {diagnosticScope}.", owner);
            return;
        }

        gameplayUiRootController.EnsureInitialized();
        modalMount = gameplayUiRootController.RequestLayerMount(
            HexGameplayUiLayerId.Modal,
            mountName,
            diagnosticScope,
            false,
            owner);
        if (modalMount == null)
        {
            Debug.LogError($"HexSimpleActionModalDocumentController could not bind to the shared modal layer for {diagnosticScope}.", owner);
            return;
        }

        modalMount.Clear();
        modalMount.styleSheets.Clear();
        modalMount.styleSheets.Add(styleSheet);
        layoutAsset.CloneTree(modalMount);

        modalRoot = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "simple-action-modal-root");
        shellElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "simple-action-modal-shell");
        titleLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "simple-action-modal-title");
        bodyLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "simple-action-modal-body");
        actionButton = UIE.UQueryExtensions.Q<UIE.Button>(modalMount, "simple-action-modal-button");

        if (actionButton != null)
        {
            actionButton.clicked += HandleActionClicked;
        }

        if (modalRoot != null)
        {
            modalRoot.style.display = UIE.DisplayStyle.None;
        }

        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Modal, false);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Modal, false);
        isInitialized = true;
        LogDebug($"Initialized modal mount='{mountName}'. shellFound={shellElement != null} actionButtonFound={actionButton != null}.");
    }

    public void Show(string title, string body, string actionLabel, Action onActionRequested)
    {
        EnsureInitialized();
        if (!isInitialized || modalRoot == null)
        {
            return;
        }

        actionRequested = onActionRequested;
        titleLabel.text = string.IsNullOrWhiteSpace(title) ? "Modal" : title.Trim();
        bodyLabel.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
        bodyLabel.style.display = string.IsNullOrWhiteSpace(bodyLabel.text) ? UIE.DisplayStyle.None : UIE.DisplayStyle.Flex;
        if (actionButton != null)
        {
            actionButton.text = string.IsNullOrWhiteSpace(actionLabel) ? "Continue" : actionLabel.Trim();
        }

        SetModalVisibility(true);
        LogDebug($"Show title='{titleLabel.text}' bodyVisible={bodyLabel.style.display == UIE.DisplayStyle.Flex} actionLabel='{actionButton?.text ?? "Continue"}'.");
    }

    public void Hide()
    {
        actionRequested = null;
        SetModalVisibility(false);
        LogDebug("Hide modal.");
    }

    private void HandleActionClicked()
    {
        LogDebug($"HandleActionClicked callbackAssigned={actionRequested != null}.");
        Action callback = actionRequested;
        Hide();
        callback?.Invoke();
    }

    private void SetModalVisibility(bool visible)
    {
        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        hudDocumentController?.SetGameplayModalState(visible);
        gameplayUiRootController?.SetLayerVisible(HexGameplayUiLayerId.Modal, visible);
        gameplayUiRootController?.SetLayerInteractive(HexGameplayUiLayerId.Modal, visible);

        if (modalRoot != null)
        {
            modalRoot.style.display = visible ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
        }

        isOpen = visible;
        LogDebug($"SetModalVisibility visible={visible} isOpen={isOpen}.");
    }

    private void LogDebug(string message, bool verbose = false)
    {
        switch (owner)
        {
            case HexRunEndModalPresenter runEndPresenter:
                runEndPresenter.LogDebug(message, verbose);
                return;

            case HexMockQuestMarkerModalPresenter questPresenter:
                questPresenter.LogDebug(message, verbose);
                return;
        }

        Debug.Log($"[GameplayUI:{diagnosticScope}] {message}", owner);
    }
}

public sealed class HexMockQuestMarkerModalPresenter : MonoBehaviour
{
    private HexSimpleActionModalDocumentController documentController;
    private HexGameplayUiRootController gameplayUiRootController;

    public bool IsOpen => documentController != null && documentController.IsOpen;

    public void Show(string title, string body, Action onCloseRequested)
    {
        LogDebug($"Show title='{title}'.");
        EnsureView();
        documentController?.Show(
            string.IsNullOrWhiteSpace(title) ? "Quest Marker" : title.Trim(),
            string.IsNullOrWhiteSpace(body) ? "Mock quest marker." : body.Trim(),
            "Close",
            onCloseRequested);
    }

    public void Hide()
    {
        LogDebug("Hide requested.");
        documentController?.Hide();
    }

    private void EnsureView()
    {
        documentController ??= new HexSimpleActionModalDocumentController(
            this,
            "quest-marker-modal-mount",
            "QuestModal");
        documentController.EnsureInitialized();
    }

    internal void LogDebug(string message, bool verbose = false)
    {
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this, createIfMissing: false);
        gameplayUiRootController?.EnsureInitialized();
        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.LogDiagnostic("QuestModal", message, this, verbose);
            return;
        }

        Debug.Log($"[GameplayUI:QuestModal] {message}", this);
    }
}
