using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CaravanMetricsController))]
public sealed class CaravanMetricsControllerEditor : Editor
{
    private HexNemesisArchetype previewLockedFamily = HexNemesisArchetype.Hunter;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CaravanMetricsController metricsController = (CaravanMetricsController)target;

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Run End Debug", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Open run-end overlays directly in Play Mode without finishing or losing the run.",
            MessageType.None);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Open Victory Modal"))
            {
                metricsController.DebugOpenVictoryModal();
            }

            if (GUILayout.Button("Open Defeat Modal"))
            {
                metricsController.DebugOpenDefeatModal();
            }
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Act Transition Debug", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Open the Act 2 -> Act 3 boon selection screen directly in Play Mode without finishing the run.",
            MessageType.None);

        previewLockedFamily = (HexNemesisArchetype)EditorGUILayout.EnumPopup("Locked Family", previewLockedFamily);
        if (previewLockedFamily == HexNemesisArchetype.None)
        {
            previewLockedFamily = HexNemesisArchetype.Hunter;
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Open Act 2 -> Act 3 Boon Screen"))
            {
                metricsController.DebugOpenAct2ToAct3BoonPreview(previewLockedFamily);
            }
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Quest Debug", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Open the temporary quest marker modal preview.",
            MessageType.None);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Open Mock Quest Modal"))
            {
                metricsController.DebugOpenMockQuestModal();
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use the debug buttons.", MessageType.Info);
        }
    }
}
