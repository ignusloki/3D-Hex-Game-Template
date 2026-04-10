using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class PitstopEncounterOption
{
    public string label = "Take the offer";
    [TextArea(2, 4)] public string outcomeText = "The caravan accepts the terms.";
    public List<PitstopResourceEffect> resourceEffects = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            label = "Continue";
        }

        if (string.IsNullOrWhiteSpace(outcomeText))
        {
            outcomeText = label;
        }

        resourceEffects ??= new List<PitstopResourceEffect>();
        for (int index = resourceEffects.Count - 1; index >= 0; index--)
        {
            if (resourceEffects[index] == null || resourceEffects[index].amount == 0)
            {
                resourceEffects.RemoveAt(index);
            }
        }
    }
}

[CreateAssetMenu(menuName = "Hex/Pitstop Encounter", fileName = "PitstopEncounter_")]
public sealed class PitstopEncounterAsset : ScriptableObject
{
    public string eventId = "pitstop-encounter";
    public PitstopKind pitstopKind = PitstopKind.Mill;
    [Min(0.1f)] public float selectionWeight = 1f;
    public string title = "Unexpected Offer";
    [TextArea(4, 8)] public string description = "A short roadside event interrupts the caravan's rest.";
    public List<PitstopEncounterOption> options = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            eventId = name;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = pitstopKind.ToString();
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            description = title;
        }

        selectionWeight = Mathf.Max(0.1f, selectionWeight);
        options ??= new List<PitstopEncounterOption>();
        for (int index = options.Count - 1; index >= 0; index--)
        {
            if (options[index] == null)
            {
                options.RemoveAt(index);
                continue;
            }

            options[index].Validate();
        }
    }
}
