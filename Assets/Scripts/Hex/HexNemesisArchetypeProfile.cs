using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "NemesisArchetypeProfile",
    menuName = "Hex/Nemesis Archetype Profile")]
public sealed class HexNemesisArchetypeProfile : ScriptableObject
{
    public HexNemesisArchetype archetype = HexNemesisArchetype.Hunter;

    [Header("Pace")]
    [FormerlySerializedAs("movesPerAction")]
    [Min(1)] public int caravanMovesPerActivation = 2;
    [Min(1)] public int stepsPerActivation = 1;

    [Header("Echo")]
    [Min(0)] public int minimumMaxActiveObstacles = 3;

    [Header("Corruptor")]
    [Min(0)] public int corruptionInfluenceRadius = 1;
    [Range(0f, 1f)] public float corruptionSpawnChanceBonus = 0.12f;
    [Min(0f)] public float corruptionCandidateWeightBonus = 1.25f;
    public Color destroyedPitstopTint = new(0.38f, 0.18f, 0.18f, 1f);
    public string destroyedPitstopDescription = "This stop has been ruined by corruption.";

    [Header("Visuals")]
    public GameObject actorPrefab;
    [Min(0f)] public float actorHeight = 0.6f;
    [Min(0.1f)] public float actorScale = 0.4f;
    [Min(0f)] public float corruptionHeight = 0.04f;
    [Min(0.1f)] public float corruptionScale = 0.5f;
    public Color actorColor = new(0.86f, 0.22f, 0.19f, 1f);
    public Color corruptionColor = new(0.24f, 0.09f, 0.09f, 0.92f);

    public void Validate()
    {
        caravanMovesPerActivation = Mathf.Max(1, caravanMovesPerActivation);
        stepsPerActivation = Mathf.Max(1, stepsPerActivation);
        minimumMaxActiveObstacles = Mathf.Max(0, minimumMaxActiveObstacles);
        corruptionInfluenceRadius = Mathf.Max(0, corruptionInfluenceRadius);
        corruptionSpawnChanceBonus = Mathf.Clamp01(corruptionSpawnChanceBonus);
        corruptionCandidateWeightBonus = Mathf.Max(0f, corruptionCandidateWeightBonus);
        actorHeight = Mathf.Max(0f, actorHeight);
        actorScale = Mathf.Max(0.1f, actorScale);
        corruptionHeight = Mathf.Max(0f, corruptionHeight);
        corruptionScale = Mathf.Max(0.1f, corruptionScale);
        destroyedPitstopDescription ??= string.Empty;
    }
}
