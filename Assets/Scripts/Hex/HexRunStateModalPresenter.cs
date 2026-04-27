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
    private UIE.VisualElement modalOverlay;
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
    private TransitionScreenMode screenMode;
    private bool isInitialized;
    private bool isOpen;

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
        EnsureDefaultSelectedBoon();
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

        UpdateOverlayForScreen(showSelection);

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

    private void EnsureDefaultSelectedBoon()
    {
        if (selectedBoon != null)
        {
            for (int index = 0; index < boonOptions.Length; index++)
            {
                HexBoonDefinition option = boonOptions[index];
                if (option != null
                    && string.Equals(option.id, selectedBoon.id, StringComparison.OrdinalIgnoreCase)
                    && option.isEnabled)
                {
                    return;
                }
            }
        }

        selectedBoon = null;
        for (int index = 0; index < boonOptions.Length; index++)
        {
            HexBoonDefinition option = boonOptions[index];
            if (option == null || !option.isEnabled)
            {
                continue;
            }

            selectedBoon = option;
            return;
        }
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

        modalOverlay.style.backgroundColor = showSelection
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
    private HexGameOverOverlayDocumentController gameOverDocumentController;
    private HexGameplayUiRootController gameplayUiRootController;

    public bool IsOpen =>
        (documentController != null && documentController.IsOpen)
        || (gameOverDocumentController != null && gameOverDocumentController.IsOpen);

    public void ShowVictory(Action onRetryRequested)
    {
        LogDebug("ShowVictory requested.");
        gameOverDocumentController?.Hide();
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
        documentController?.Hide();
        EnsureGameOverView();
        gameOverDocumentController?.Show(onRetryRequested);
    }

    public void Hide()
    {
        LogDebug("Hide requested.");
        documentController?.Hide();
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

internal sealed class HexGameOverOverlayDocumentController
{
    private const string LayoutResourcePath = "UI/Modal/HexGameOverOverlay";
    private const string StyleSheetResourcePath = "UI/Modal/HexGameOverOverlayStyles";
    private const string RimouskiFontEditorAssetPath = "Assets/Art/Fonts/rimouski sb.otf";
    private const string BackgroundEditorAssetPath = "Assets/Art/Image/Placeholder/parallax-forest.png";
    private const float BlackoutFadeStart = 0f;
    private const float BlackoutFadeDuration = 1f;
    private const float BackgroundFadeStart = 1f;
    private const float BackgroundFadeDuration = 0.55f;
    private const float ScrimFadeStart = 1f;
    private const float ScrimFadeDuration = 0.55f;
    private const float TitleFadeStart = 1.35f;
    private const float TitleFadeDuration = 0.40f;
    private const float SeparatorFadeStart = 1.45f;
    private const float SeparatorFadeDuration = 0.35f;
    private const float PanelFadeStart = 1.60f;
    private const float PanelFadeDuration = 0.45f;
    private const float DescriptionFadeStart = 1.78f;
    private const float DescriptionFadeDuration = 0.30f;
    private const float RetryFadeStart = 1.92f;
    private const float RetryFadeDuration = 0.28f;
    private const float ReturnFadeStart = 2.02f;
    private const float ReturnFadeDuration = 0.28f;

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
    private UIE.IVisualElementScheduledItem transitionSchedule;
    private HexGameOverTransitionSettings transitionSettings;
    private HexHudDocumentController hudDocumentController;
    private Action retryRequested;
    private float transitionStartTime;
    private bool isInitialized;
    private bool isOpen;
    private bool isTransitionPlaying;

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
        if (isTransitionPlaying)
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
        if (modalRoot == null)
        {
            CompleteTransition();
            return;
        }

        isTransitionPlaying = true;
        transitionStartTime = Time.unscaledTime;
        transitionSchedule = modalRoot.schedule.Execute(UpdateTransition).Every(16);
        UpdateTransition();
    }

    private void StopTransition()
    {
        transitionSchedule?.Pause();
        transitionSchedule = null;
        isTransitionPlaying = false;
    }

    private void UpdateTransition()
    {
        float elapsed = Time.unscaledTime - transitionStartTime;
        HexGameOverTransitionSettings settings = transitionSettings;
        bool useEaseOut = settings == null || settings.useEaseOut;
        SetElementOpacity(blackoutElement, ResolveFadeOpacity(elapsed, settings, BlackoutFadeStart, s => s.blackoutFadeStart, BlackoutFadeDuration, s => s.blackoutFadeDuration, useEaseOut));
        SetElementOpacity(backgroundElement, ResolveFadeOpacity(elapsed, settings, BackgroundFadeStart, s => s.backgroundFadeStart, BackgroundFadeDuration, s => s.backgroundFadeDuration, useEaseOut));
        SetElementOpacity(scrimElement, ResolveFadeOpacity(elapsed, settings, ScrimFadeStart, s => s.scrimFadeStart, ScrimFadeDuration, s => s.scrimFadeDuration, useEaseOut));
        SetElementOpacity(titleLabel, ResolveFadeOpacity(elapsed, settings, TitleFadeStart, s => s.titleFadeStart, TitleFadeDuration, s => s.titleFadeDuration, useEaseOut));
        SetElementOpacity(separatorElement, ResolveFadeOpacity(elapsed, settings, SeparatorFadeStart, s => s.separatorFadeStart, SeparatorFadeDuration, s => s.separatorFadeDuration, useEaseOut));
        SetElementOpacity(panelElement, ResolveFadeOpacity(elapsed, settings, PanelFadeStart, s => s.panelFadeStart, PanelFadeDuration, s => s.panelFadeDuration, useEaseOut));
        SetElementOpacity(descriptionLabel, ResolveFadeOpacity(elapsed, settings, DescriptionFadeStart, s => s.descriptionFadeStart, DescriptionFadeDuration, s => s.descriptionFadeDuration, useEaseOut));
        SetElementOpacity(retryButton, ResolveFadeOpacity(elapsed, settings, RetryFadeStart, s => s.retryButtonFadeStart, RetryFadeDuration, s => s.retryButtonFadeDuration, useEaseOut));
        SetElementOpacity(returnToTitleButton, ResolveFadeOpacity(elapsed, settings, ReturnFadeStart, s => s.returnToTitleButtonFadeStart, ReturnFadeDuration, s => s.returnToTitleButtonFadeDuration, useEaseOut));

        if (elapsed >= ResolveCompleteTime(settings))
        {
            CompleteTransition();
        }
    }

    private void CompleteTransition()
    {
        transitionSchedule?.Pause();
        transitionSchedule = null;
        isTransitionPlaying = false;
        SetElementOpacity(blackoutElement, 1f);
        SetElementOpacity(backgroundElement, 1f);
        SetElementOpacity(scrimElement, 1f);
        SetElementOpacity(titleLabel, 1f);
        SetElementOpacity(separatorElement, 1f);
        SetElementOpacity(panelElement, 1f);
        SetElementOpacity(descriptionLabel, 1f);
        SetElementOpacity(retryButton, 1f);
        SetElementOpacity(returnToTitleButton, 1f);
        SetButtonInteractionEnabled(true);
    }

    private static float ResolveFadeOpacity(
        float elapsed,
        HexGameOverTransitionSettings settings,
        float fallbackStart,
        Func<HexGameOverTransitionSettings, float> startSelector,
        float fallbackDuration,
        Func<HexGameOverTransitionSettings, float> durationSelector,
        bool useEaseOut)
    {
        return EvaluateFade(
            elapsed,
            ResolveStart(settings, fallbackStart, startSelector),
            ResolveDuration(settings, fallbackDuration, durationSelector),
            useEaseOut);
    }

    private static float EvaluateFade(float elapsed, float start, float duration, bool useEaseOut)
    {
        if (elapsed <= start)
        {
            return 0f;
        }

        if (duration <= 0f)
        {
            return 1f;
        }

        float progress = Mathf.Clamp01((elapsed - start) / duration);
        return useEaseOut
            ? 1f - ((1f - progress) * (1f - progress))
            : progress;
    }

    private static float ResolveStart(HexGameOverTransitionSettings settings, float fallback, Func<HexGameOverTransitionSettings, float> selector)
    {
        return Mathf.Max(0f, settings == null ? fallback : selector(settings));
    }

    private static float ResolveDuration(HexGameOverTransitionSettings settings, float fallback, Func<HexGameOverTransitionSettings, float> selector)
    {
        return Mathf.Max(0f, settings == null ? fallback : selector(settings));
    }

    private static float ResolveCompleteTime(HexGameOverTransitionSettings settings)
    {
        if (settings != null)
        {
            return settings.ResolveCompleteTime();
        }

        float completeTime = BlackoutFadeStart + BlackoutFadeDuration;
        completeTime = Mathf.Max(completeTime, BackgroundFadeStart + BackgroundFadeDuration);
        completeTime = Mathf.Max(completeTime, ScrimFadeStart + ScrimFadeDuration);
        completeTime = Mathf.Max(completeTime, TitleFadeStart + TitleFadeDuration);
        completeTime = Mathf.Max(completeTime, SeparatorFadeStart + SeparatorFadeDuration);
        completeTime = Mathf.Max(completeTime, PanelFadeStart + PanelFadeDuration);
        completeTime = Mathf.Max(completeTime, DescriptionFadeStart + DescriptionFadeDuration);
        completeTime = Mathf.Max(completeTime, RetryFadeStart + RetryFadeDuration);
        completeTime = Mathf.Max(completeTime, ReturnFadeStart + ReturnFadeDuration);
        return completeTime;
    }

    private static void SetElementOpacity(UIE.VisualElement element, float opacity)
    {
        if (element != null)
        {
            element.style.opacity = Mathf.Clamp01(opacity);
        }
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
