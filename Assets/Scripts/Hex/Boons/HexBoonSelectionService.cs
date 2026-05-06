public static class HexBoonSelectionService
{
    public static HexBoonDefinition GetSelectedBoonDefinition()
    {
        System.Collections.Generic.IReadOnlyList<HexBoonDefinition> definitions = GetSelectedBoonDefinitions();
        return definitions.Count > 0 ? definitions[0] : null;
    }

    public static System.Collections.Generic.IReadOnlyList<HexBoonDefinition> GetSelectedBoonDefinitions()
    {
        return HexActTransitionService.GetSelectedBoons();
    }

    public static HexMapGenerationModifiers GetMapGenerationModifiers()
    {
        HexMapGenerationModifiers combinedModifiers = HexMapGenerationModifiers.None;
        System.Collections.Generic.IReadOnlyList<HexBoonDefinition> definitions = GetSelectedBoonDefinitions();
        for (int index = 0; index < definitions.Count; index++)
        {
            HexBoonDefinition definition = definitions[index];
            if (definition == null)
            {
                continue;
            }

            combinedModifiers = combinedModifiers.Combine(definition.GetMapGenerationModifiers());
        }

        return combinedModifiers;
    }
}
