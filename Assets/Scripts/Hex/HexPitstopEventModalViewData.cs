using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
