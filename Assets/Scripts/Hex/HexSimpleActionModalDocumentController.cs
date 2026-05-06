using System;
using System.Collections.Generic;
using UnityEngine;
using UIE = UnityEngine.UIElements;

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
            HexAudioUiBinder.BindButton(actionButton, owner);
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
