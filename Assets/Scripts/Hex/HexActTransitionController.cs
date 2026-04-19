using UnityEngine;

[AddComponentMenu("Hex/Acts/Act Transition Controller")]
[DisallowMultipleComponent]
public sealed class HexActTransitionController : MonoBehaviour
{
    [SerializeField] private bool useSceneTransitionConfig = true;
    [SerializeField] private HexActTransitionConfigAsset transitionConfig;

    public bool UseSceneTransitionConfig => useSceneTransitionConfig;
    public HexActTransitionConfigAsset TransitionConfig => transitionConfig;

    private void Reset()
    {
        gameObject.name = "Act Transition System";
    }

    private void OnValidate()
    {
        transitionConfig?.OnValidateFromSceneController();
    }

    public HexActTransitionConfigAsset GetTransitionConfig()
    {
        if (!useSceneTransitionConfig || transitionConfig == null)
        {
            return null;
        }

        transitionConfig.OnValidateFromSceneController();
        return transitionConfig;
    }

    public string GetConfigurationSummary()
    {
        if (!useSceneTransitionConfig)
        {
            return "Scene override disabled. The runtime will use the default Resources config.";
        }

        if (transitionConfig == null)
        {
            return "No scene transition config assigned. The runtime will fall back to the default Resources config.";
        }

        string act1ProfileName = transitionConfig.act1Profile != null
            ? transitionConfig.act1Profile.GetResolvedDisplayName()
            : "Scene Defaults";
        string act2ProfileName = transitionConfig.act2Profile != null
            ? transitionConfig.act2Profile.GetResolvedDisplayName()
            : "Scene Defaults";
        string act3ProfileName = transitionConfig.act3Profile != null
            ? transitionConfig.act3Profile.GetResolvedDisplayName()
            : "Scene Defaults";
        return $"Acts enabled: {transitionConfig.enableActTransitions}. Profiles: A1={act1ProfileName}, A2={act2ProfileName}, A3={act3ProfileName}.";
    }
}
