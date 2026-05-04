using UnityEngine;
using UnityEngine.UIElements;

public static class HexAudioUiBinder
{
    public static void BindButton(
        Button button,
        MonoBehaviour owner,
        HexSfxId hoverSfx = HexSfxId.UiHover,
        HexSfxId clickSfx = HexSfxId.UiClick,
        bool bindHover = true,
        bool bindClick = true)
    {
        BindClickable(button, owner, hoverSfx, clickSfx, bindHover, bindClick);
    }

    public static void BindClickable(
        VisualElement element,
        MonoBehaviour owner,
        HexSfxId hoverSfx = HexSfxId.UiHover,
        HexSfxId clickSfx = HexSfxId.UiClick,
        bool bindHover = true,
        bool bindClick = true)
    {
        if (element == null)
        {
            return;
        }

        if (bindHover)
        {
            element.RegisterCallback<PointerEnterEvent>(_ => PlaySfx(owner, hoverSfx));
        }

        if (bindClick)
        {
            element.RegisterCallback<ClickEvent>(_ => PlaySfx(owner, clickSfx));
        }
    }

    private static void PlaySfx(MonoBehaviour owner, HexSfxId sfxId)
    {
        HexAudioSystem audioSystem = HexAudioSystem.ResolveShared(owner);
        if (audioSystem != null)
        {
            audioSystem.PlaySfx(sfxId);
        }
    }
}
