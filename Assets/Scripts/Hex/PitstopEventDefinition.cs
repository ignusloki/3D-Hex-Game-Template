using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class PitstopResourceEffect
{
    public CaravanResourceType resourceType = CaravanResourceType.Food;
    public int amount = 1;
}

[System.Serializable]
public sealed class PitstopEventDefinition
{
    public PitstopKind kind = PitstopKind.Mill;
    public string title = "Supply Stop";
    [TextArea(2, 4)] public string description = "The caravan finds a small supply cache.";
    public bool hasRefuelPoint = true;
    public bool repeatable;
    public List<PitstopResourceEffect> firstVisitEffects = new();
    public List<PitstopResourceEffect> repeatVisitEffects = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            title = kind.ToString();
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            description = title;
        }

        firstVisitEffects ??= new List<PitstopResourceEffect>();
        repeatVisitEffects ??= new List<PitstopResourceEffect>();
        ValidateEffectList(firstVisitEffects);
        ValidateEffectList(repeatVisitEffects);
    }

    public IReadOnlyList<PitstopResourceEffect> GetEffects(bool isFirstVisit)
    {
        if (isFirstVisit)
        {
            return firstVisitEffects;
        }

        return repeatable ? repeatVisitEffects : System.Array.Empty<PitstopResourceEffect>();
    }

    public static List<PitstopEventDefinition> CreateDefaultSet()
    {
        return new List<PitstopEventDefinition>
        {
            new()
            {
                kind = PitstopKind.Mill,
                title = "Working Mill",
                description = "The millers trade flour and dried grain for the road ahead.",
                hasRefuelPoint = true,
                repeatable = false,
                firstVisitEffects = new List<PitstopResourceEffect>
                {
                    new() { resourceType = CaravanResourceType.Food, amount = 3 }
                }
            },
            new()
            {
                kind = PitstopKind.WallTower,
                title = "Guard Tower",
                description = "The watch hands share news of safer routes and steady the caravan's nerves.",
                hasRefuelPoint = false,
                repeatable = false,
                firstVisitEffects = new List<PitstopResourceEffect>
                {
                    new() { resourceType = CaravanResourceType.Morale, amount = 1 }
                }
            },
            new()
            {
                kind = PitstopKind.Mansion,
                title = "Roadside Estate",
                description = "A wealthy patron pays for fresh scouts and provisions.",
                hasRefuelPoint = false,
                repeatable = false,
                firstVisitEffects = new List<PitstopResourceEffect>
                {
                    new() { resourceType = CaravanResourceType.Gold, amount = 2 }
                }
            }
        };
    }

    private static void ValidateEffectList(List<PitstopResourceEffect> effects)
    {
        for (int index = effects.Count - 1; index >= 0; index--)
        {
            if (effects[index] == null || effects[index].amount == 0)
            {
                effects.RemoveAt(index);
            }
        }
    }
}
