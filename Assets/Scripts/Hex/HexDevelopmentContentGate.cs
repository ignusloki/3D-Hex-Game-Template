using UnityEngine;

public static class HexDevelopmentContentGate
{
    public static bool AllowsDevelopmentOnlyContent => Debug.isDebugBuild;

    public static bool CanUseMockQuestMarkers(bool requested)
    {
        return requested && AllowsDevelopmentOnlyContent;
    }

    public static bool CanUseGeneratedPlaceholderVisuals(bool requested = true)
    {
        return requested && AllowsDevelopmentOnlyContent;
    }
}
