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
    private const string ChapterPageRevealProfileResourcePath = "UI/Transitions/ActTransitionChapterPageReveal";
    private const string RimouskiFontEditorAssetPath = "Assets/Art/Fonts/rimouski sb.otf";
    private const string ScrimTransitionTargetKey = "scrim";
    private const string PageTransitionTargetKey = "page";

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
    private HexUiTransitionProfile chapterPageRevealProfile;
    private HexUiTransitionProfile activeRevealProfile;
    private Font rimouskiFont;
    private HexGameplayUiRootController gameplayUiRootController;
    private UIE.VisualElement modalMount;
    private UIE.VisualElement modalRoot;
    private UIE.VisualElement modalOverlay;
    private UIE.VisualElement modalScrim;
    private UIE.VisualElement intermissionShell;
    private UIE.VisualElement selectionShell;
    private UIE.Label intermissionTitleLabel;
    private UIE.Label intermissionBodyLabel;
    private UIE.Image intermissionIllustrationImage;
    private UIE.Label intermissionIllustrationPlaceholderLabel;
    private UIE.Label intermissionCurrentFoodValueLabel;
    private UIE.Label intermissionCurrentMoraleValueLabel;
    private UIE.Label intermissionCurrentGoldValueLabel;
    private UIE.Label intermissionRewardFoodValueLabel;
    private UIE.Label intermissionRewardMoraleValueLabel;
    private UIE.Label intermissionRewardGoldValueLabel;
    private UIE.Button intermissionContinueButton;
    private UIE.Label selectionPromptLabel;
    private UIE.Label selectionResourceLabel;
    private UIE.VisualElement selectionResourceStripContainer;
    private UIE.VisualElement selectionCardsRow;
    private UIE.VisualElement selectionCardsGroup;
    private UIE.Label detailTitleLabel;
    private UIE.VisualElement detailFamilyRow;
    private UIE.Image detailFamilyIconImage;
    private UIE.Label detailFamilyLabel;
    private UIE.Label detailEffectLabel;
    private UIE.ScrollView detailGlossaryScrollView;
    private UIE.Button selectionContinueButton;
    private HexHudDocumentController hudDocumentController;
    private Action<HexBoonDefinition> continueRequested;
    private HexActTransitionDisplayData activeDisplayData;
    private HexBoonDefinition[] boonOptions = Array.Empty<HexBoonDefinition>();
    private HexBoonDefinition selectedBoon;
    private HexBoonDefinition hoveredBoon;
    private HexBoonDefinition focusedBoon;
    private HexUiTransitionPlayer revealTransitionPlayer;
    private TransitionScreenMode screenMode;
    private bool isInitialized;
    private bool isOpen;
    private bool areTransitionControlsLocked;

    private sealed class TransitionBoonCardView
    {
        public UIE.VisualElement Root;
        public UIE.Image ArtImage;
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
        chapterPageRevealProfile ??= Resources.Load<HexUiTransitionProfile>(ChapterPageRevealProfileResourcePath);
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
        modalOverlay = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-modal-overlay");
        modalScrim = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-modal-scrim");
        intermissionShell = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-intermission-shell");
        selectionShell = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-selection-shell");

        intermissionTitleLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-title");
        intermissionBodyLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-body");
        intermissionIllustrationImage = UIE.UQueryExtensions.Q<UIE.Image>(modalMount, "act-transition-intermission-illustration-image");
        intermissionIllustrationPlaceholderLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-illustration-placeholder");
        if (intermissionIllustrationImage != null)
        {
            intermissionIllustrationImage.scaleMode = ScaleMode.ScaleAndCrop;
        }
        intermissionCurrentFoodValueLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-current-food-value");
        intermissionCurrentMoraleValueLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-current-morale-value");
        intermissionCurrentGoldValueLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-current-gold-value");
        intermissionRewardFoodValueLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-reward-food-value");
        intermissionRewardMoraleValueLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-reward-morale-value");
        intermissionRewardGoldValueLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-intermission-reward-gold-value");
        intermissionContinueButton = UIE.UQueryExtensions.Q<UIE.Button>(modalMount, "act-transition-intermission-button");

        selectionPromptLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-prompt");
        selectionResourceLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-resource-label");
        selectionResourceStripContainer = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-selection-resource-strip");
        selectionCardsRow = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-selection-card-row");
        selectionCardsGroup = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-selection-card-group");
        detailTitleLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-detail-title");
        detailFamilyRow = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "act-transition-selection-detail-family-row");
        detailFamilyIconImage = UIE.UQueryExtensions.Q<UIE.Image>(modalMount, "act-transition-selection-detail-family-icon");
        detailFamilyLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-detail-family-label");
        detailEffectLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "act-transition-selection-detail-effect");
        detailGlossaryScrollView = UIE.UQueryExtensions.Q<UIE.ScrollView>(modalMount, "act-transition-selection-detail-glossary");
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
            revealTransitionPlayer = new HexUiTransitionPlayer(modalRoot);
        }

        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Modal, false);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Modal, false);
        LogDebug(
            $"Initialized shared-root transition modal. modalRootFound={modalRoot != null} intermissionFound={intermissionShell != null} selectionFound={selectionShell != null} cardsRowFound={selectionCardsRow != null} cardsGroupFound={selectionCardsGroup != null}.");
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
            ShowScreen(TransitionScreenMode.Selection, includeScrimFade: true);
            return;
        }

        ShowScreen(TransitionScreenMode.Intermission, includeScrimFade: true);
    }

    public void Hide()
    {
        StopRevealTransition();
        continueRequested = null;
        activeDisplayData = default;
        boonOptions = Array.Empty<HexBoonDefinition>();
        selectedBoon = null;
        hoveredBoon = null;
        focusedBoon = null;
        screenMode = TransitionScreenMode.None;
        cardViews.Clear();

        if (selectionCardsGroup != null)
        {
            selectionCardsGroup.Clear();
        }

        SetModalVisibility(false);
        LogDebug("Hide transition modal.");
    }

    private void PopulateIntermission()
    {
        if (intermissionTitleLabel == null)
        {
            return;
        }

        intermissionTitleLabel.text = string.IsNullOrWhiteSpace(activeDisplayData.Title)
            ? "Act Complete"
            : activeDisplayData.Title.Trim();
        intermissionBodyLabel.text = string.IsNullOrWhiteSpace(activeDisplayData.Body)
            ? "The caravan gathers itself for the road ahead."
            : NormalizeIntermissionBody(activeDisplayData.Body);

        ApplyIntermissionIllustration(activeDisplayData.IntermissionIllustration);
        RebuildIntermissionResourceSummary(activeDisplayData);

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
        RebuildSelectionCarryOverSummary(activeDisplayData.CurrentResources);
        RebuildSelectionCards();
        RefreshSelectionActionState();
        RefreshDetailPanel();
    }

    private void ShowScreen(TransitionScreenMode mode, bool includeScrimFade)
    {
        screenMode = mode;
        bool showSelection = mode == TransitionScreenMode.Selection && activeDisplayData.RequiresBoonSelection;
        UIE.VisualElement activeShell = showSelection ? selectionShell : intermissionShell;
        if (intermissionShell != null)
        {
            intermissionShell.style.display = mode == TransitionScreenMode.Intermission ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
        }

        if (selectionShell != null)
        {
            selectionShell.style.display = showSelection ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
        }

        UpdateOverlayForScreen(showSelection);
        PlayRevealTransition(activeShell, includeScrimFade);

        LogDebug(
            $"ShowScreen mode={mode} intermissionVisible={intermissionShell?.resolvedStyle.display == UIE.DisplayStyle.Flex} selectionVisible={selectionShell?.resolvedStyle.display == UIE.DisplayStyle.Flex}.",
            true);
    }

    private void HandleIntermissionContinueClicked()
    {
        if (areTransitionControlsLocked)
        {
            return;
        }

        LogDebug(
            $"HandleIntermissionContinueClicked requiresSelection={activeDisplayData.RequiresBoonSelection} boonCount={boonOptions.Length} modeBefore={screenMode}.");
        if (activeDisplayData.RequiresBoonSelection)
        {
            ShowScreen(TransitionScreenMode.Selection, includeScrimFade: false);
            return;
        }

        Action<HexBoonDefinition> callback = continueRequested;
        Hide();
        callback?.Invoke(null);
    }

    private void HandleSelectionContinueClicked()
    {
        if (areTransitionControlsLocked)
        {
            return;
        }

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

    private void RebuildIntermissionResourceSummary(HexActTransitionDisplayData displayData)
    {
        SetLabelText(intermissionCurrentFoodValueLabel, displayData.CurrentResources.Food.ToString());
        SetLabelText(intermissionCurrentMoraleValueLabel, displayData.CurrentResources.Morale.ToString());
        SetLabelText(intermissionCurrentGoldValueLabel, displayData.CurrentResources.Gold.ToString());

        SetLabelText(intermissionRewardFoodValueLabel, FormatSigned(displayData.BetweenActFood));
        SetLabelText(intermissionRewardMoraleValueLabel, FormatSigned(displayData.BetweenActMorale));
        SetLabelText(intermissionRewardGoldValueLabel, FormatSigned(displayData.BetweenActGold));
    }

    private void RebuildSelectionCarryOverSummary(CaravanResourceSnapshot currentResources)
    {
        if (selectionResourceStripContainer == null)
        {
            return;
        }

        selectionResourceStripContainer.Clear();
        selectionResourceStripContainer.Add(CreateResourceMiniModule("Food", currentResources.Food.ToString()));
        selectionResourceStripContainer.Add(CreateResourceMiniModule("Morale", currentResources.Morale.ToString()));
        selectionResourceStripContainer.Add(CreateResourceMiniModule("Gold", currentResources.Gold.ToString()));
    }

    private void RebuildSelectionCards()
    {
        cardViews.Clear();
        UIE.VisualElement cardsContainer = selectionCardsGroup ?? selectionCardsRow;
        if (cardsContainer == null)
        {
            return;
        }

        cardsContainer.Clear();
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
            cardsContainer.Add(cardView.Root);
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

        UIE.VisualElement artFrame = new();
        artFrame.AddToClassList("act-transition-card-art-frame");

        UIE.VisualElement artInset = new();
        artInset.AddToClassList("act-transition-card-art-inset");

        UIE.Image artImage = new();
        artImage.AddToClassList("act-transition-card-art-image");
        artImage.scaleMode = ScaleMode.ScaleToFit;

        artInset.Add(artImage);
        artFrame.Add(artInset);
        root.Add(artFrame);

        Texture cardTexture = boon.GetPortraitTexture();
        if (cardTexture != null)
        {
            artImage.image = cardTexture;
            artImage.style.display = UIE.DisplayStyle.Flex;
        }
        else
        {
            artImage.image = null;
            artImage.style.display = UIE.DisplayStyle.None;
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
            ArtImage = artImage,
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
        bool isEnabled = selectedBoon != null && !areTransitionControlsLocked;
        selectionContinueButton.SetEnabled(isEnabled);
        selectionContinueButton.pickingMode = isEnabled ? UIE.PickingMode.Position : UIE.PickingMode.Ignore;
        selectionContinueButton.focusable = isEnabled;
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
        if (detailTitleLabel == null)
        {
            return;
        }

        HexBoonDefinition inspectBoon = hoveredBoon ?? focusedBoon ?? selectedBoon;
        if (inspectBoon == null)
        {
            SetLabelText(detailTitleLabel, string.Empty);
            SetFamilyRow(null);
            SetDetailLabel(detailEffectLabel, string.Empty);
            RebuildSelectionGlossary(null);
            return;
        }

        inspectBoon.Validate();
        SetLabelText(detailTitleLabel, inspectBoon.GetResolvedDisplayName());
        SetFamilyRow(inspectBoon);
        SetDetailLabel(detailEffectLabel, BuildSelectionDescription(inspectBoon));
        RebuildSelectionGlossary(inspectBoon);
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
        intermissionIllustrationPlaceholderLabel.style.display = UIE.DisplayStyle.None;
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

    private void SetFamilyRow(HexBoonDefinition boon)
    {
        if (detailFamilyRow == null || detailFamilyIconImage == null || detailFamilyLabel == null)
        {
            return;
        }

        if (boon == null)
        {
            detailFamilyRow.style.display = UIE.DisplayStyle.None;
            detailFamilyIconImage.image = null;
            detailFamilyLabel.text = string.Empty;
            return;
        }

        detailFamilyLabel.text = $"Family: {FormatArchetype(boon.archetypeFamily)}";
        Texture familyIconTexture = boon.GetFamilyIconTexture();
        detailFamilyIconImage.image = familyIconTexture;
        detailFamilyIconImage.style.display = familyIconTexture != null ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
        detailFamilyRow.style.display = UIE.DisplayStyle.Flex;
    }

    private UIE.VisualElement CreateResourceMiniModule(string label, string value)
    {
        UIE.VisualElement module = new();
        module.AddToClassList("act-transition-resource-mini");

        UIE.Label labelElement = new(label);
        labelElement.AddToClassList("act-transition-resource-mini-label");
        UIE.Label valueElement = new(value);
        valueElement.AddToClassList("act-transition-resource-mini-value");

        module.Add(labelElement);
        module.Add(valueElement);
        return module;
    }

    private void RebuildSelectionGlossary(HexBoonDefinition boon)
    {
        if (detailGlossaryScrollView == null)
        {
            return;
        }

        detailGlossaryScrollView.contentContainer.Clear();
        if (boon?.keywords == null || boon.keywords.Length == 0)
        {
            detailGlossaryScrollView.style.display = UIE.DisplayStyle.None;
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
            detailGlossaryScrollView.contentContainer.Add(row);
            rowCount++;
        }

        detailGlossaryScrollView.style.display = rowCount == 0 ? UIE.DisplayStyle.None : UIE.DisplayStyle.Flex;
    }

    private UIE.VisualElement CreateSelectionGlossaryRow(HexBoonKeywordPresentationData keyword)
    {
        UIE.VisualElement row = new();
        row.AddToClassList("act-transition-glossary-row");

        UIE.Label termLabel = new(keyword.label.Trim());
        termLabel.AddToClassList("act-transition-glossary-term");

        UIE.Label descriptionLabel = new(
            string.IsNullOrWhiteSpace(keyword.explanation)
                ? string.Empty
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

    private static void SetLabelText(UIE.Label label, string text)
    {
        if (label == null)
        {
            return;
        }

        label.text = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
    }

    private static string BuildSelectionHeaderTitle(int nextActNumber)
    {
        return nextActNumber > 0
            ? $"Choose one boon for Act {nextActNumber}"
            : "Choose one boon for the next act";
    }

    private static string BuildSelectionDescription(HexBoonDefinition boon)
    {
        string normalized = NormalizeInlineText(boon?.description);
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

        const int maxLength = 128;
        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized.Substring(0, maxLength).TrimEnd()}...";
    }

    private static string BuildSelectionCardSummary(HexBoonDefinition boon)
    {
        if (boon == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(boon.cardSummary))
        {
            return boon.cardSummary.Trim();
        }

        string keywordLine = boon.GetKeywordLine();
        return string.IsNullOrWhiteSpace(keywordLine) ? string.Empty : keywordLine;
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
        string normalized = NormalizeInlineText(rawText);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        const int maxLength = 124;
        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized.Substring(0, maxLength).TrimEnd()}...";
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
        ApplyRimouskiFont(intermissionTitleLabel);
        ApplyRimouskiFont(intermissionContinueButton);
    }

    private void UpdateOverlayForScreen(bool showSelection)
    {
        if (modalOverlay == null)
        {
            return;
        }

        UIE.VisualElement scrim = modalScrim ?? modalOverlay;
        scrim.style.backgroundColor = showSelection
            ? new UIE.StyleColor(new Color(10f / 255f, 12f / 255f, 16f / 255f, 0.72f))
            : new UIE.StyleColor(new Color(8f / 255f, 10f / 255f, 16f / 255f, 0.58f));
    }

    private void ApplySelectionTypographyTheme()
    {
        ApplyRimouskiFont(selectionPromptLabel);
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

    private void PlayRevealTransition(UIE.VisualElement activeShell, bool includeScrimFade)
    {
        StopRevealTransition();
        if (revealTransitionPlayer == null || activeShell == null)
        {
            SetElementOpacity(modalScrim, 1f);
            SetElementOpacity(activeShell, 1f);
            SetTransitionInteractionLock(false);
            return;
        }

        activeRevealProfile = CreateRevealRuntimeProfile(includeScrimFade);
        HexUiTransitionTargetSet targets = new HexUiTransitionTargetSet()
            .Register(ScrimTransitionTargetKey, modalScrim)
            .Register(PageTransitionTargetKey, activeShell);

        bool started = revealTransitionPlayer.Play(
            activeRevealProfile,
            targets,
            () => CompleteRevealTransition(activeShell),
            SetTransitionInteractionLock,
            message => LogDebug(message, true));
        if (!started)
        {
            CompleteRevealTransition(activeShell);
        }
    }

    private void StopRevealTransition()
    {
        revealTransitionPlayer?.Cancel(applyEndState: false);
        SetTransitionInteractionLock(false);
        ReleaseRevealProfile();
    }

    private void CompleteRevealTransition(UIE.VisualElement activeShell)
    {
        SetElementOpacity(modalScrim, 1f);
        SetElementOpacity(activeShell, 1f);
        SetTransitionInteractionLock(false);
        ReleaseRevealProfile();
    }

    private HexUiTransitionProfile CreateRevealRuntimeProfile(bool includeScrimFade)
    {
        return HexUiTransitionProfile.CreateRuntimeProfile(
            chapterPageRevealProfile,
            "act-transition-chapter-page-reveal",
            HexUiTransitionEasing.EaseOut,
            CreateDefaultRevealTracks(),
            track => includeScrimFade || !string.Equals(track.targetKey, ScrimTransitionTargetKey, StringComparison.Ordinal));
    }

    private static IReadOnlyList<HexUiTransitionFadeTrack> CreateDefaultRevealTracks()
    {
        return new[]
        {
            new HexUiTransitionFadeTrack
            {
                targetKey = ScrimTransitionTargetKey,
                startTime = 0f,
                duration = 0.18f,
                startOpacity = 0f,
                endOpacity = 1f,
                isRequired = true
            },
            new HexUiTransitionFadeTrack
            {
                targetKey = PageTransitionTargetKey,
                startTime = 0.08f,
                duration = 0.24f,
                startOpacity = 0f,
                endOpacity = 1f,
                isRequired = true
            }
        };
    }

    private void SetTransitionInteractionLock(bool locked)
    {
        areTransitionControlsLocked = locked;
        SetButtonInteractionEnabled(intermissionContinueButton, !locked);
        RefreshSelectionActionState();
    }

    private static void SetButtonInteractionEnabled(UIE.Button button, bool enabled)
    {
        if (button == null)
        {
            return;
        }

        button.SetEnabled(enabled);
        button.pickingMode = enabled ? UIE.PickingMode.Position : UIE.PickingMode.Ignore;
        button.focusable = enabled;
    }

    private static void SetElementOpacity(UIE.VisualElement element, float opacity)
    {
        if (element != null)
        {
            element.style.opacity = Mathf.Clamp01(opacity);
        }
    }

    private void ReleaseRevealProfile()
    {
        if (activeRevealProfile != null)
        {
            UnityEngine.Object.Destroy(activeRevealProfile);
            activeRevealProfile = null;
        }
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
    [Header("Victory")]
    [SerializeField] private Texture2D victoryHeroImage;

    private HexSimpleActionModalDocumentController documentController;
    private HexVictoryOverlayDocumentController victoryDocumentController;
    private HexGameOverOverlayDocumentController gameOverDocumentController;
    private HexGameplayUiRootController gameplayUiRootController;

    internal Texture2D VictoryHeroImage => victoryHeroImage;

    public bool IsOpen =>
        (documentController != null && documentController.IsOpen)
        || (victoryDocumentController != null && victoryDocumentController.IsOpen)
        || (gameOverDocumentController != null && gameOverDocumentController.IsOpen);

    public void ShowVictory(Action onRetryRequested)
    {
        LogDebug("ShowVictory requested.");
        documentController?.Hide();
        gameOverDocumentController?.Hide();
        EnsureVictoryView();
        victoryDocumentController?.Show(onRetryRequested);
    }

    public void ShowDefeat(Action onRetryRequested)
    {
        LogDebug("ShowDefeat requested.");
        documentController?.Hide();
        victoryDocumentController?.Hide();
        EnsureGameOverView();
        gameOverDocumentController?.Show(onRetryRequested);
    }

    public void Hide()
    {
        LogDebug("Hide requested.");
        documentController?.Hide();
        victoryDocumentController?.Hide();
        gameOverDocumentController?.Hide();
    }

    private void EnsureView()
    {
        documentController ??= new HexSimpleActionModalDocumentController(
            this,
            "run-end-modal-mount",
            "RunEndModal");
        documentController.EnsureInitialized();
    }

    private void EnsureGameOverView()
    {
        gameOverDocumentController ??= new HexGameOverOverlayDocumentController(this);
        gameOverDocumentController.EnsureInitialized();
    }

    private void EnsureVictoryView()
    {
        victoryDocumentController ??= new HexVictoryOverlayDocumentController(this);
        victoryDocumentController.EnsureInitialized();
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

internal sealed class HexVictoryOverlayDocumentController
{
    private const string LayoutResourcePath = "UI/Modal/HexVictoryOverlay";
    private const string StyleSheetResourcePath = "UI/Modal/HexVictoryOverlayStyles";
    private const string SimpleModalFadeProfileResourcePath = "UI/Transitions/SimpleModalFade";
    private const string ModalMountName = "victory-overlay-mount";
    private const string RimouskiFontEditorAssetPath = "Assets/Art/Fonts/rimouski sb.otf";
    private const string ScrimTransitionTargetKey = "scrim";
    private const string ShellTransitionTargetKey = "shell";

    private readonly MonoBehaviour owner;

    private UIE.VisualTreeAsset layoutAsset;
    private UIE.StyleSheet styleSheet;
    private HexUiTransitionProfile simpleModalFadeProfile;
    private HexUiTransitionProfile activeTransitionProfile;
    private Font rimouskiFont;
    private Texture2D heroTexture;
    private HexGameplayUiRootController gameplayUiRootController;
    private UIE.VisualElement modalMount;
    private UIE.VisualElement modalRoot;
    private UIE.VisualElement scrimElement;
    private UIE.VisualElement pageElement;
    private UIE.Label titleLabel;
    private UIE.Button continueButton;
    private UIE.Image heroImage;
    private HexUiTransitionPlayer transitionPlayer;
    private HexHudDocumentController hudDocumentController;
    private HexGlobalUiTransitionController globalTransitionController;
    private Action continueRequested;
    private bool isInitialized;
    private bool isOpen;
    private bool areTransitionControlsLocked;
    private bool isContinueTransitioning;

    public HexVictoryOverlayDocumentController(MonoBehaviour owner)
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
        simpleModalFadeProfile ??= Resources.Load<HexUiTransitionProfile>(SimpleModalFadeProfileResourcePath);
        rimouskiFont ??= LoadRimouskiFont();
        heroTexture ??= ResolveVictoryHeroTexture();
        if (layoutAsset == null || styleSheet == null)
        {
            Debug.LogError("HexVictoryOverlayDocumentController could not load Victory UI Toolkit assets.", owner);
            return;
        }

        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(owner);
        if (gameplayUiRootController == null)
        {
            Debug.LogError("HexVictoryOverlayDocumentController could not resolve the shared gameplay UI root.", owner);
            return;
        }

        gameplayUiRootController.EnsureInitialized();
        modalMount = gameplayUiRootController.RequestLayerMount(
            HexGameplayUiLayerId.Modal,
            ModalMountName,
            "VictoryOverlay",
            false,
            owner);
        if (modalMount == null)
        {
            Debug.LogError("HexVictoryOverlayDocumentController could not bind to the shared modal layer.", owner);
            return;
        }

        modalMount.Clear();
        modalMount.styleSheets.Clear();
        modalMount.styleSheets.Add(styleSheet);
        layoutAsset.CloneTree(modalMount);

        modalRoot = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "VictoryOverlay");
        scrimElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "VictoryScrim");
        pageElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "VictoryPage");
        titleLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "VictoryTitle");
        continueButton = UIE.UQueryExtensions.Q<UIE.Button>(modalMount, "VictoryContinueButton");
        heroImage = UIE.UQueryExtensions.Q<UIE.Image>(modalMount, "VictoryHeroImage");

        ApplyRimouskiFont(titleLabel);
        ApplyRimouskiFont(continueButton);
        ApplyHeroImage();

        if (continueButton != null)
        {
            continueButton.clicked += HandleContinueClicked;
        }

        if (modalRoot != null)
        {
            modalRoot.style.display = UIE.DisplayStyle.None;
            transitionPlayer = new HexUiTransitionPlayer(modalRoot);
        }

        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        globalTransitionController ??= HexGlobalUiTransitionController.ResolveShared(owner);
        globalTransitionController?.EnsureInitialized();
        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Modal, false);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Modal, false);
        isInitialized = true;
        LogDebug($"Initialized Victory overlay. rootFound={modalRoot != null} pageFound={pageElement != null} heroFound={heroTexture != null}.");
    }

    public void Show(Action onContinueRequested)
    {
        EnsureInitialized();
        if (!isInitialized || modalRoot == null)
        {
            return;
        }

        continueRequested = onContinueRequested;
        SetOverlayVisibility(true);
        PrepareOpenTransitionInitialState();
        StartOpenTransition();
        LogDebug("Show Victory overlay.");
    }

    public void Hide()
    {
        StopOpenTransition();
        isContinueTransitioning = false;
        continueRequested = null;
        SetOverlayVisibility(false);
        LogDebug("Hide Victory overlay.");
    }

    private void ApplyHeroImage()
    {
        if (heroImage == null)
        {
            return;
        }

        heroImage.scaleMode = ScaleMode.ScaleAndCrop;
        heroImage.image = heroTexture;
        heroImage.style.display = heroTexture != null ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
    }

    private void HandleContinueClicked()
    {
        if (areTransitionControlsLocked || isContinueTransitioning)
        {
            return;
        }

        LogDebug($"HandleContinueClicked callbackAssigned={continueRequested != null}.");
        Action callback = continueRequested;
        continueRequested = null;
        isContinueTransitioning = true;
        SetTransitionInteractionLock(true);

        globalTransitionController ??= HexGlobalUiTransitionController.ResolveShared(owner);
        globalTransitionController?.EnsureInitialized();
        bool started = globalTransitionController != null
            && globalTransitionController.PlayFadeToBlack(
                () => InvokeContinueCallback(callback),
                null,
                fadeBackOut: false);
        if (!started)
        {
            LogDebug("Global fade-to-black was unavailable. Continuing immediately.");
            InvokeContinueCallback(callback);
        }
    }

    private void InvokeContinueCallback(Action callback)
    {
        Hide();
        callback?.Invoke();
    }

    private void PrepareOpenTransitionInitialState()
    {
        StopOpenTransition();
        SetButtonInteractionEnabled(false);
        SetElementOpacity(scrimElement, 0f);
        SetElementOpacity(pageElement, 0f);
    }

    private void StartOpenTransition()
    {
        if (transitionPlayer == null || pageElement == null)
        {
            CompleteOpenTransition();
            return;
        }

        ReleaseTransitionProfile();
        activeTransitionProfile = HexUiTransitionProfile.CreateRuntimeProfile(
            simpleModalFadeProfile,
            "victory-simple-modal-fade",
            HexUiTransitionEasing.EaseOut,
            CreateDefaultSimpleModalFadeTracks());

        HexUiTransitionTargetSet targets = new HexUiTransitionTargetSet()
            .Register(ScrimTransitionTargetKey, scrimElement)
            .Register(ShellTransitionTargetKey, pageElement);

        bool started = transitionPlayer.Play(
            activeTransitionProfile,
            targets,
            CompleteOpenTransition,
            SetTransitionInteractionLock,
            message => LogDebug(message, true));
        if (!started)
        {
            CompleteOpenTransition();
        }
    }

    private void StopOpenTransition()
    {
        transitionPlayer?.Cancel(applyEndState: false);
        SetTransitionInteractionLock(false);
        ReleaseTransitionProfile();
    }

    private void CompleteOpenTransition()
    {
        SetElementOpacity(scrimElement, 1f);
        SetElementOpacity(pageElement, 1f);
        SetTransitionInteractionLock(false);
        ReleaseTransitionProfile();
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

    private void SetOverlayVisibility(bool visible)
    {
        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        hudDocumentController?.SetGameplayModalState(visible);

        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Hud, !visible);
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Context, !visible);
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Modal, visible);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Hud, false);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Context, false);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Modal, visible);
        }

        if (modalRoot != null)
        {
            modalRoot.style.display = visible ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
        }

        isOpen = visible;
        LogDebug($"SetOverlayVisibility visible={visible}.");
    }

    private void SetTransitionInteractionLock(bool locked)
    {
        areTransitionControlsLocked = locked;
        SetButtonInteractionEnabled(!locked);
    }

    private void SetButtonInteractionEnabled(bool enabled)
    {
        if (continueButton == null)
        {
            return;
        }

        continueButton.SetEnabled(enabled);
        continueButton.pickingMode = enabled ? UIE.PickingMode.Position : UIE.PickingMode.Ignore;
        continueButton.focusable = enabled;
    }

    private static void SetElementOpacity(UIE.VisualElement element, float opacity)
    {
        if (element != null)
        {
            element.style.opacity = Mathf.Clamp01(opacity);
        }
    }

    private void ReleaseTransitionProfile()
    {
        if (activeTransitionProfile != null)
        {
            UnityEngine.Object.Destroy(activeTransitionProfile);
            activeTransitionProfile = null;
        }
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

    private Texture2D ResolveVictoryHeroTexture()
    {
        if (owner is HexRunEndModalPresenter runEndPresenter)
        {
            return runEndPresenter.VictoryHeroImage;
        }

        return null;
    }

    private void LogDebug(string message, bool verbose = false)
    {
        if (owner is HexRunEndModalPresenter runEndPresenter)
        {
            runEndPresenter.LogDebug(message, verbose);
            return;
        }

        Debug.Log($"[GameplayUI:VictoryOverlay] {message}", owner);
    }
}

internal sealed class HexGameOverOverlayDocumentController
{
    private const string LayoutResourcePath = "UI/Modal/HexGameOverOverlay";
    private const string StyleSheetResourcePath = "UI/Modal/HexGameOverOverlayStyles";
    private const string RimouskiFontEditorAssetPath = "Assets/Art/Fonts/rimouski sb.otf";
    private const string BackgroundEditorAssetPath = "Assets/Art/Image/Placeholder/parallax-forest.png";

    private readonly MonoBehaviour owner;

    private UIE.VisualTreeAsset layoutAsset;
    private UIE.StyleSheet styleSheet;
    private Font rimouskiFont;
    private Texture2D backgroundTexture;
    private HexGameplayUiRootController gameplayUiRootController;
    private UIE.VisualElement modalMount;
    private UIE.VisualElement modalRoot;
    private UIE.VisualElement blackoutElement;
    private UIE.VisualElement backgroundElement;
    private UIE.VisualElement scrimElement;
    private UIE.VisualElement separatorElement;
    private UIE.VisualElement panelElement;
    private UIE.Label titleLabel;
    private UIE.Label descriptionLabel;
    private UIE.Button retryButton;
    private UIE.Button returnToTitleButton;
    private HexUiTransitionPlayer transitionPlayer;
    private HexUiTransitionTargetSet transitionTargets;
    private HexUiTransitionProfile transitionProfile;
    private HexGameOverTransitionSettings transitionSettings;
    private HexHudDocumentController hudDocumentController;
    private Action retryRequested;
    private bool isInitialized;
    private bool isOpen;
    private bool areTransitionControlsLocked;

    public HexGameOverOverlayDocumentController(MonoBehaviour owner)
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
        rimouskiFont ??= LoadRimouskiFont();
        backgroundTexture ??= LoadGameOverBackgroundTexture();
        transitionSettings = ResolveTransitionSettings();
        if (layoutAsset == null || styleSheet == null)
        {
            Debug.LogError("HexGameOverOverlayDocumentController could not load Game Over UI Toolkit assets.", owner);
            return;
        }

        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(owner);
        if (gameplayUiRootController == null)
        {
            Debug.LogError("HexGameOverOverlayDocumentController could not resolve the shared gameplay UI root.", owner);
            return;
        }

        gameplayUiRootController.EnsureInitialized();
        modalMount = gameplayUiRootController.RequestLayerMount(
            HexGameplayUiLayerId.Modal,
            "game-over-overlay-mount",
            "GameOverOverlay",
            false,
            owner);
        if (modalMount == null)
        {
            Debug.LogError("HexGameOverOverlayDocumentController could not bind to the shared modal layer.", owner);
            return;
        }

        modalMount.Clear();
        modalMount.styleSheets.Clear();
        modalMount.styleSheets.Add(styleSheet);
        layoutAsset.CloneTree(modalMount);

        modalRoot = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "GameOverOverlay");
        blackoutElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "GameOverBlackout");
        backgroundElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "GameOverBackground");
        scrimElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "GameOverScrim");
        separatorElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "GameOverSeparator");
        panelElement = UIE.UQueryExtensions.Q<UIE.VisualElement>(modalMount, "GameOverPanel");
        titleLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "GameOverTitle");
        descriptionLabel = UIE.UQueryExtensions.Q<UIE.Label>(modalMount, "GameOverDescription");
        retryButton = UIE.UQueryExtensions.Q<UIE.Button>(modalMount, "RetryButton");
        returnToTitleButton = UIE.UQueryExtensions.Q<UIE.Button>(modalMount, "ReturnToTitleButton");
        transitionPlayer = new HexUiTransitionPlayer(modalRoot);
        transitionTargets = CreateTransitionTargets();

        ApplyRimouskiFont(titleLabel);
        ApplyRimouskiFont(retryButton);
        ApplyRimouskiFont(returnToTitleButton);

        if (backgroundElement != null && backgroundTexture != null)
        {
            backgroundElement.style.backgroundImage = new UIE.StyleBackground(backgroundTexture);
        }

        if (retryButton != null)
        {
            retryButton.clicked += HandleRetryClicked;
        }

        if (modalRoot != null)
        {
            modalRoot.pickingMode = UIE.PickingMode.Position;
            modalRoot.style.display = UIE.DisplayStyle.None;
        }

        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Modal, false);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Modal, false);
        isInitialized = true;
        LogDebug(
            $"Initialized Game Over overlay. rootFound={modalRoot != null} backgroundFound={backgroundElement != null} retryFound={retryButton != null} returnFound={returnToTitleButton != null} backgroundTextureFound={backgroundTexture != null}.");
    }

    public void Show(Action onRetryRequested)
    {
        EnsureInitialized();
        if (!isInitialized || modalRoot == null)
        {
            return;
        }

        if (isOpen)
        {
            LogDebug("Show ignored because the Game Over overlay is already open.");
            return;
        }

        retryRequested = onRetryRequested;
        transitionSettings = ResolveTransitionSettings();
        if (titleLabel != null)
        {
            titleLabel.text = "Game Over";
        }

        if (retryButton != null)
        {
            retryButton.text = "Retry";
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.text = "Return to Title";
        }

        PrepareTransitionInitialState();
        SetOverlayVisibility(true);
        StartTransition();
        LogDebug($"Show Game Over overlay. retryAssigned={retryRequested != null}.");
    }

    public void Hide()
    {
        StopTransition();
        retryRequested = null;
        SetOverlayVisibility(false);
        LogDebug("Hide Game Over overlay.");
    }

    private void HandleRetryClicked()
    {
        if (areTransitionControlsLocked)
        {
            return;
        }

        LogDebug($"HandleRetryClicked callbackAssigned={retryRequested != null}.");
        Action callback = retryRequested;
        Hide();
        callback?.Invoke();
    }

    private void PrepareTransitionInitialState()
    {
        StopTransition();
        SetButtonInteractionEnabled(false);
        SetElementOpacity(blackoutElement, 0f);
        SetElementOpacity(backgroundElement, 0f);
        SetElementOpacity(scrimElement, 0f);
        SetElementOpacity(titleLabel, 0f);
        SetElementOpacity(separatorElement, 0f);
        SetElementOpacity(panelElement, 0f);
        SetElementOpacity(descriptionLabel, 0f);
        SetElementOpacity(retryButton, 0f);
        SetElementOpacity(returnToTitleButton, 0f);
    }

    private void StartTransition()
    {
        if (modalRoot == null || transitionPlayer == null || transitionTargets == null)
        {
            CompleteTransition();
            return;
        }

        ReleaseTransitionProfile();
        transitionProfile = transitionSettings != null
            ? transitionSettings.CreateRuntimeProfile()
            : HexGameOverTransitionSettings.CreateDefaultRuntimeProfile();

        bool started = transitionPlayer.Play(
            transitionProfile,
            transitionTargets,
            CompleteTransition,
            SetTransitionInteractionLock,
            message => LogDebug(message));
        if (!started)
        {
            CompleteTransition();
        }
    }

    private void StopTransition()
    {
        transitionPlayer?.Cancel(applyEndState: false);
        SetTransitionInteractionLock(false);
        ReleaseTransitionProfile();
    }

    private void CompleteTransition()
    {
        SetElementOpacity(blackoutElement, 1f);
        SetElementOpacity(backgroundElement, 1f);
        SetElementOpacity(scrimElement, 1f);
        SetElementOpacity(titleLabel, 1f);
        SetElementOpacity(separatorElement, 1f);
        SetElementOpacity(panelElement, 1f);
        SetElementOpacity(descriptionLabel, 1f);
        SetElementOpacity(retryButton, 1f);
        SetElementOpacity(returnToTitleButton, 1f);
        SetTransitionInteractionLock(false);
        ReleaseTransitionProfile();
    }

    private HexUiTransitionTargetSet CreateTransitionTargets()
    {
        return new HexUiTransitionTargetSet()
            .Register(HexGameOverTransitionSettings.BlackoutTargetKey, blackoutElement)
            .Register(HexGameOverTransitionSettings.BackgroundTargetKey, backgroundElement)
            .Register(HexGameOverTransitionSettings.ScrimTargetKey, scrimElement)
            .Register(HexGameOverTransitionSettings.TitleTargetKey, titleLabel)
            .Register(HexGameOverTransitionSettings.SeparatorTargetKey, separatorElement)
            .Register(HexGameOverTransitionSettings.PanelTargetKey, panelElement)
            .Register(HexGameOverTransitionSettings.DescriptionTargetKey, descriptionLabel)
            .Register(HexGameOverTransitionSettings.RetryButtonTargetKey, retryButton)
            .Register(HexGameOverTransitionSettings.ReturnToTitleButtonTargetKey, returnToTitleButton);
    }

    private static void SetElementOpacity(UIE.VisualElement element, float opacity)
    {
        if (element != null)
        {
            element.style.opacity = Mathf.Clamp01(opacity);
        }
    }

    private void SetTransitionInteractionLock(bool locked)
    {
        areTransitionControlsLocked = locked;
        SetButtonInteractionEnabled(!locked);
    }

    private void SetButtonInteractionEnabled(bool enabled)
    {
        if (retryButton != null)
        {
            retryButton.pickingMode = enabled ? UIE.PickingMode.Position : UIE.PickingMode.Ignore;
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.pickingMode = enabled ? UIE.PickingMode.Position : UIE.PickingMode.Ignore;
        }
    }

    private void ReleaseTransitionProfile()
    {
        if (transitionProfile != null)
        {
            UnityEngine.Object.Destroy(transitionProfile);
            transitionProfile = null;
        }
    }

    private void SetOverlayVisibility(bool visible)
    {
        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        hudDocumentController?.SetGameplayModalState(visible);

        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Hud, !visible);
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Context, !visible);
            gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.Modal, visible);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Hud, false);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Context, false);
            gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.Modal, visible);
        }

        if (modalRoot != null)
        {
            modalRoot.style.display = visible ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
        }

        isOpen = visible;
        LogDebug($"SetOverlayVisibility visible={visible}.");
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

    private static Texture2D LoadGameOverBackgroundTexture()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundEditorAssetPath);
#else
        return null;
#endif
    }

    private static HexGameOverTransitionSettings ResolveTransitionSettings()
    {
        return UnityEngine.Object.FindAnyObjectByType<HexGameOverTransitionSettings>();
    }

    private void LogDebug(string message, bool verbose = false)
    {
        if (owner is HexRunEndModalPresenter runEndPresenter)
        {
            runEndPresenter.LogDebug(message, verbose);
            return;
        }

        Debug.Log($"[GameplayUI:GameOverOverlay] {message}", owner);
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
