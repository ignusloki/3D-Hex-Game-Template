using UnityEngine;

[CreateAssetMenu(
    fileName = "ActAtmosphereConfig",
    menuName = "Hex/Visual/Act Atmosphere Config")]
public sealed class HexActAtmosphereConfigAsset : ScriptableObject
{
    public string displayName = "Act Atmosphere";
    [TextArea(2, 5)] public string description = string.Empty;
    public bool isEnabled = true;
    public HexActAtmosphereSettings act1Atmosphere = HexActAtmosphereSettings.CreateAct1Defaults();
    public HexActAtmosphereSettings act2Atmosphere = HexActAtmosphereSettings.CreateAct2Defaults();
    public HexActAtmosphereSettings act3Atmosphere = HexActAtmosphereSettings.CreateAct3Defaults();

    [SerializeField, HideInInspector] private int editorRevision;

    public int EditorRevision => editorRevision;

    private void OnValidate()
    {
        Validate();
        unchecked
        {
            editorRevision++;
            if (editorRevision < 0)
            {
                editorRevision = 1;
            }
        }
    }

    public void Validate()
    {
        displayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();
        description ??= string.Empty;
        act1Atmosphere ??= HexActAtmosphereSettings.CreateAct1Defaults();
        act2Atmosphere ??= HexActAtmosphereSettings.CreateAct2Defaults();
        act3Atmosphere ??= HexActAtmosphereSettings.CreateAct3Defaults();
        act1Atmosphere.Validate();
        act2Atmosphere.Validate();
        act3Atmosphere.Validate();
    }

    public HexActAtmosphereSettings GetAtmosphereForAct(int actNumber)
    {
        Validate();
        if (!isEnabled)
        {
            return null;
        }

        return Mathf.Clamp(actNumber, 1, 3) switch
        {
            2 => act2Atmosphere,
            3 => act3Atmosphere,
            _ => act1Atmosphere
        };
    }

    public string GetResolvedDisplayName()
    {
        return string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
