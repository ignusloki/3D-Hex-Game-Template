using UnityEngine;

public readonly struct CaravanResourceSnapshot
{
    public CaravanResourceSnapshot(int food, int morale, int gold)
    {
        Food = food;
        Morale = morale;
        Gold = gold;
    }

    public int Food { get; }
    public int Morale { get; }
    public int Gold { get; }

    public int GetAmount(CaravanResourceType resourceType)
    {
        return resourceType switch
        {
            CaravanResourceType.Food => Food,
            CaravanResourceType.Morale => Morale,
            CaravanResourceType.Gold => Gold,
            _ => 0
        };
    }
}

public sealed class CaravanResourceState
{
    public int Food { get; private set; }
    public int Morale { get; private set; }
    public int Gold { get; private set; }

    public bool IsDefeated => Food <= 0 || Morale <= 0;

    public void Initialize(int food, int morale, int gold)
    {
        Food = Mathf.Max(0, food);
        Morale = Mathf.Max(0, morale);
        Gold = Mathf.Max(0, gold);
    }

    public CaravanResourceSnapshot ToSnapshot()
    {
        return new CaravanResourceSnapshot(Food, Morale, Gold);
    }

    public int GetAmount(CaravanResourceType resourceType)
    {
        return resourceType switch
        {
            CaravanResourceType.Food => Food,
            CaravanResourceType.Morale => Morale,
            CaravanResourceType.Gold => Gold,
            _ => 0
        };
    }

    public void Spend(CaravanResourceType resourceType, int amount)
    {
        int clampedAmount = Mathf.Max(0, amount);
        switch (resourceType)
        {
            case CaravanResourceType.Food:
                Food = Mathf.Max(0, Food - clampedAmount);
                break;

            case CaravanResourceType.Morale:
                Morale = Mathf.Max(0, Morale - clampedAmount);
                break;

            case CaravanResourceType.Gold:
                Gold = Mathf.Max(0, Gold - clampedAmount);
                break;
        }
    }

    public string GetDefeatReason()
    {
        if (Food <= 0)
        {
            return "Food depleted";
        }

        if (Morale <= 0)
        {
            return "Morale collapsed";
        }

        return "The caravan cannot continue";
    }
}
