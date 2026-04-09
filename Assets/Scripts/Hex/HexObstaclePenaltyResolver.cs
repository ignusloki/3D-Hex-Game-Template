public static class HexObstaclePenaltyResolver
{
    public static HexObstacleContactResult Resolve(HexObstacleInstance obstacle, CaravanResourceSnapshot resources)
    {
        if (obstacle == null || obstacle.Definition == null)
        {
            return HexObstacleContactResult.None;
        }

        HexObstacleDefinition definition = obstacle.Definition;
        if (definition.penaltyMode == HexObstaclePenaltyMode.FallbackDrainIfPrimaryUnavailable
            && resources.GetAmount(definition.primaryResource) <= 0)
        {
            return new HexObstacleContactResult(
                true,
                obstacle,
                definition.fallbackResource,
                definition.fallbackDrainAmount,
                true);
        }

        return new HexObstacleContactResult(
            true,
            obstacle,
            definition.primaryResource,
            definition.primaryDrainAmount,
            false);
    }
}
