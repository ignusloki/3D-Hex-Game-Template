using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerController))]
public sealed class PlayerControllerEditor : Editor
{
    private HexNemesisArchetype previewLockedFamily = HexNemesisArchetype.Hunter;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

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
                ((PlayerController)target).DebugOpenAct2ToAct3BoonPreview(previewLockedFamily);
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use the preview button.", MessageType.Info);
        }
    }
}
