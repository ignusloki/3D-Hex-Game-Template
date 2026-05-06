using System;
using System.Collections.Generic;
using UnityEngine;

public enum HexRunPhase
{
    Booting,
    MainMenu,
    PreparingGameplay,
    AwaitingPlayerInput,
    ResolvingMove,
    PitstopChoice,
    ActTransition,
    Victory,
    Defeat
}

public enum HexRunOutcome
{
    None,
    Victory,
    Defeat
}

[Serializable]
public struct HexRunCoordinates : IEquatable<HexRunCoordinates>
{
    [SerializeField] private bool hasValue;
    [SerializeField] private int row;
    [SerializeField] private int column;

    public HexRunCoordinates(int row, int column)
    {
        hasValue = true;
        this.row = row;
        this.column = column;
    }

    public bool HasValue => hasValue;
    public int Row => row;
    public int Column => column;

    public static HexRunCoordinates None => default;

    public static HexRunCoordinates FromHexCoordinates(HexCoordinates coordinates)
    {
        return new HexRunCoordinates(coordinates.Row, coordinates.Column);
    }

    public bool TryToHexCoordinates(out HexCoordinates coordinates)
    {
        if (!hasValue)
        {
            coordinates = default;
            return false;
        }

        coordinates = new HexCoordinates(row, column);
        return true;
    }

    public bool Equals(HexRunCoordinates other)
    {
        return hasValue == other.hasValue
            && (!hasValue || (row == other.row && column == other.column));
    }

    public override bool Equals(object obj)
    {
        return obj is HexRunCoordinates other && Equals(other);
    }

    public override int GetHashCode()
    {
        return hasValue ? HashCode.Combine(row, column) : 0;
    }

    public override string ToString()
    {
        return hasValue ? $"{row},{column}" : string.Empty;
    }
}

public readonly struct HexRunStateSnapshot
{
    public HexRunStateSnapshot(
        int currentActNumber,
        CaravanResourceSnapshot resources,
        IReadOnlyList<HexBoonDefinition> selectedBoons,
        HexNemesisArchetype lockedBoonFamily,
        HexRunCoordinates caravanCoordinates,
        HexRunCoordinates goalCoordinates,
        HexRunPhase phase,
        HexRunOutcome outcome,
        string outcomeReason,
        string pendingModalContext)
    {
        CurrentActNumber = Math.Max(1, currentActNumber);
        Resources = resources;
        SelectedBoons = selectedBoons ?? Array.Empty<HexBoonDefinition>();
        LockedBoonFamily = lockedBoonFamily;
        CaravanCoordinates = caravanCoordinates;
        GoalCoordinates = goalCoordinates;
        Phase = phase;
        Outcome = outcome;
        OutcomeReason = outcomeReason ?? string.Empty;
        PendingModalContext = pendingModalContext ?? string.Empty;
    }

    public int CurrentActNumber { get; }
    public CaravanResourceSnapshot Resources { get; }
    public IReadOnlyList<HexBoonDefinition> SelectedBoons { get; }
    public HexNemesisArchetype LockedBoonFamily { get; }
    public HexRunCoordinates CaravanCoordinates { get; }
    public HexRunCoordinates GoalCoordinates { get; }
    public HexRunPhase Phase { get; }
    public HexRunOutcome Outcome { get; }
    public string OutcomeReason { get; }
    public string PendingModalContext { get; }
    public bool HasFinished => Outcome != HexRunOutcome.None;
}

[Serializable]
public sealed class HexRunState
{
    [SerializeField] private int currentActNumber = 1;
    [SerializeField] private int food;
    [SerializeField] private int morale;
    [SerializeField] private int gold;
    [SerializeField] private List<HexBoonDefinition> selectedBoons = new();
    [SerializeField] private HexNemesisArchetype lockedBoonFamily = HexNemesisArchetype.None;
    [SerializeField] private HexRunCoordinates caravanCoordinates;
    [SerializeField] private HexRunCoordinates goalCoordinates;
    [SerializeField] private HexRunPhase phase = HexRunPhase.Booting;
    [SerializeField] private HexRunOutcome outcome = HexRunOutcome.None;
    [SerializeField] private string outcomeReason = string.Empty;
    [SerializeField] private string pendingModalContext = string.Empty;

    public int CurrentActNumber => currentActNumber;
    public CaravanResourceSnapshot Resources => new(food, morale, gold);
    public IReadOnlyList<HexBoonDefinition> SelectedBoons
    {
        get
        {
            if (selectedBoons == null)
            {
                return Array.Empty<HexBoonDefinition>();
            }

            return selectedBoons;
        }
    }
    public HexNemesisArchetype LockedBoonFamily => lockedBoonFamily;
    public HexRunCoordinates CaravanCoordinates => caravanCoordinates;
    public HexRunCoordinates GoalCoordinates => goalCoordinates;
    public HexRunPhase Phase => phase;
    public HexRunOutcome Outcome => outcome;
    public string OutcomeReason => outcomeReason ?? string.Empty;
    public string PendingModalContext => pendingModalContext ?? string.Empty;
    public bool HasFinished => outcome != HexRunOutcome.None;

    public void Initialize(
        int actNumber,
        CaravanResourceSnapshot resources,
        HexCoordinates caravanCoordinates,
        HexCoordinates goalCoordinates,
        HexNemesisArchetype lockedBoonFamily,
        IReadOnlyList<HexBoonDefinition> selectedBoons,
        HexRunPhase initialPhase)
    {
        SetCurrentActNumber(actNumber);
        SetResources(resources);
        SetCaravanCoordinates(caravanCoordinates);
        SetGoalCoordinates(goalCoordinates);
        SetLockedBoonFamily(lockedBoonFamily);
        SetSelectedBoons(selectedBoons);
        ResetOutcome();
        SetPendingModalContext(string.Empty);
        SetPhase(initialPhase);
    }

    public void InitializeSession(
        int actNumber,
        CaravanResourceSnapshot resources,
        HexNemesisArchetype lockedBoonFamily,
        IReadOnlyList<HexBoonDefinition> selectedBoons,
        HexRunPhase initialPhase)
    {
        SetCurrentActNumber(actNumber);
        SetResources(resources);
        ClearCaravanCoordinates();
        ClearGoalCoordinates();
        SetLockedBoonFamily(lockedBoonFamily);
        SetSelectedBoons(selectedBoons);
        ResetOutcome();
        SetPendingModalContext(string.Empty);
        SetPhase(initialPhase);
    }

    public void SetCurrentActNumber(int actNumber)
    {
        currentActNumber = Math.Max(1, actNumber);
    }

    public void SetResources(CaravanResourceSnapshot resources)
    {
        food = Math.Max(0, resources.Food);
        morale = Math.Max(0, resources.Morale);
        gold = Math.Max(0, resources.Gold);
    }

    public void SetSelectedBoons(IReadOnlyList<HexBoonDefinition> boons)
    {
        selectedBoons ??= new List<HexBoonDefinition>();
        selectedBoons.Clear();

        if (boons == null)
        {
            return;
        }

        for (int index = 0; index < boons.Count; index++)
        {
            HexBoonDefinition boon = boons[index];
            if (boon != null)
            {
                selectedBoons.Add(boon);
            }
        }
    }

    public bool TryAddSelectedBoon(HexBoonDefinition boon)
    {
        if (boon == null)
        {
            return false;
        }

        selectedBoons ??= new List<HexBoonDefinition>();
        for (int index = 0; index < selectedBoons.Count; index++)
        {
            HexBoonDefinition existingBoon = selectedBoons[index];
            if (existingBoon == null)
            {
                continue;
            }

            if (string.Equals(existingBoon.id, boon.id, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        selectedBoons.Add(boon);
        return true;
    }

    public void SetLockedBoonFamily(HexNemesisArchetype family)
    {
        lockedBoonFamily = family;
    }

    public void SetCaravanCoordinates(HexCoordinates coordinates)
    {
        caravanCoordinates = HexRunCoordinates.FromHexCoordinates(coordinates);
    }

    public void SetGoalCoordinates(HexCoordinates coordinates)
    {
        goalCoordinates = HexRunCoordinates.FromHexCoordinates(coordinates);
    }

    public void ClearCaravanCoordinates()
    {
        caravanCoordinates = HexRunCoordinates.None;
    }

    public void ClearGoalCoordinates()
    {
        goalCoordinates = HexRunCoordinates.None;
    }

    public void SetPhase(HexRunPhase phase)
    {
        this.phase = phase;
    }

    public void SetPendingModalContext(string context)
    {
        pendingModalContext = context ?? string.Empty;
    }

    public void Complete(HexRunOutcome outcome, string reason)
    {
        if (outcome == HexRunOutcome.None)
        {
            ResetOutcome();
            return;
        }

        this.outcome = outcome;
        outcomeReason = reason ?? string.Empty;
        phase = outcome == HexRunOutcome.Victory ? HexRunPhase.Victory : HexRunPhase.Defeat;
        pendingModalContext = outcome == HexRunOutcome.Victory ? "Victory" : "Defeat";
    }

    public void ResetOutcome()
    {
        outcome = HexRunOutcome.None;
        outcomeReason = string.Empty;
    }

    public HexRunStateSnapshot ToSnapshot()
    {
        HexBoonDefinition[] boonSnapshot = selectedBoons != null && selectedBoons.Count > 0
            ? selectedBoons.ToArray()
            : Array.Empty<HexBoonDefinition>();

        return new HexRunStateSnapshot(
            currentActNumber,
            Resources,
            boonSnapshot,
            lockedBoonFamily,
            caravanCoordinates,
            goalCoordinates,
            phase,
            outcome,
            outcomeReason,
            pendingModalContext);
    }
}
