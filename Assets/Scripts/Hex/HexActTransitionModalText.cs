internal static class HexActTransitionModalText
{
    public static string BuildSelectionHeaderTitle(int nextActNumber)
    {
        return nextActNumber > 0
            ? $"Choose one boon for Act {nextActNumber}"
            : "Choose one boon for the next act";
    }

    public static string BuildSelectionDescription(HexBoonDefinition boon)
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

    public static string BuildSelectionCardSummary(HexBoonDefinition boon)
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

    public static string NormalizeInlineText(string rawText)
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

    public static string NormalizeFlavorLine(string rawText)
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

    public static string NormalizeIntermissionBody(string rawText)
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

    public static string FormatSigned(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }

    public static string FormatArchetype(HexNemesisArchetype archetype)
    {
        return archetype switch
        {
            HexNemesisArchetype.Hunter => "Hunter",
            HexNemesisArchetype.Echo => "Echo",
            HexNemesisArchetype.Corruptor => "Corruptor",
            _ => "None"
        };
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
}
