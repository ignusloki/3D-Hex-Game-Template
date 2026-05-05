using System;
using UnityEngine;
using UIE = UnityEngine.UIElements;

internal sealed class HexActTransitionBoonCardView
{
    public UIE.VisualElement Root;
    public UIE.Image ArtImage;
    public HexBoonDefinition Boon;
    public bool IsHovered;
    public bool IsFocused;
}

internal static class HexActTransitionModalElementFactory
{
    public static HexActTransitionBoonCardView CreateBoonCard(
        HexBoonDefinition boon,
        MonoBehaviour owner,
        Action<HexBoonDefinition> onClicked,
        Action<HexBoonDefinition, bool> onHovered,
        Action<HexBoonDefinition, bool> onFocused)
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
        HexAudioUiBinder.BindClickable(root, owner, clickSfx: HexSfxId.BoonSelect);

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

        root.RegisterCallback<UIE.ClickEvent>(_ => onClicked?.Invoke(boon));
        root.RegisterCallback<UIE.PointerEnterEvent>(_ => onHovered?.Invoke(boon, true));
        root.RegisterCallback<UIE.PointerLeaveEvent>(_ => onHovered?.Invoke(boon, false));
        root.RegisterCallback<UIE.FocusInEvent>(_ => onFocused?.Invoke(boon, true));
        root.RegisterCallback<UIE.FocusOutEvent>(_ => onFocused?.Invoke(boon, false));
        root.RegisterCallback<UIE.KeyDownEvent>(evt =>
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.Space)
            {
                return;
            }

            onClicked?.Invoke(boon);
            evt.StopPropagation();
        });

        root.SetEnabled(boon.isEnabled);

        return new HexActTransitionBoonCardView
        {
            Root = root,
            ArtImage = artImage,
            Boon = boon
        };
    }

    public static UIE.VisualElement CreateResourceMiniModule(string label, string value)
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

    public static UIE.VisualElement CreateSelectionGlossaryRow(HexBoonKeywordPresentationData keyword)
    {
        UIE.VisualElement row = new();
        row.AddToClassList("act-transition-glossary-row");

        UIE.Label termLabel = new(keyword.label.Trim());
        termLabel.AddToClassList("act-transition-glossary-term");

        UIE.Label descriptionLabel = new(
            string.IsNullOrWhiteSpace(keyword.explanation)
                ? string.Empty
                : HexActTransitionModalText.NormalizeInlineText(keyword.explanation));
        descriptionLabel.AddToClassList("act-transition-glossary-text");

        row.Add(termLabel);
        row.Add(descriptionLabel);
        return row;
    }

    public static void SetDetailLabel(UIE.Label label, string text)
    {
        if (label == null)
        {
            return;
        }

        bool hasText = !string.IsNullOrWhiteSpace(text);
        label.text = hasText ? text.Trim() : string.Empty;
        label.style.display = hasText ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
    }

    public static void SetLabelText(UIE.Label label, string text)
    {
        if (label == null)
        {
            return;
        }

        label.text = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
    }
}
