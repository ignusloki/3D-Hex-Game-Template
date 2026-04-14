using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexBoonSystemController))]
public sealed class HexBoonSystemControllerEditor : Editor
{
    private Editor boonEditor;
    private bool showBoonDefinition = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty applySelectedBoonAtRunStart = serializedObject.FindProperty("applySelectedBoonAtRunStart");
        SerializedProperty selectedBoon = serializedObject.FindProperty("selectedBoon");

        EditorGUILayout.PropertyField(applySelectedBoonAtRunStart);
        EditorGUILayout.PropertyField(selectedBoon);

        HexBoonSystemController controller = (HexBoonSystemController)target;
        EditorGUILayout.Space(4f);
        EditorGUILayout.HelpBox(controller.GetSelectionSummary(), MessageType.Info);

        if (selectedBoon.objectReferenceValue == null)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.HelpBox(
            "Editing the boon definition below changes the shared ScriptableObject asset used by this scene selection.",
            MessageType.None);

        showBoonDefinition = EditorGUILayout.InspectorTitlebar(showBoonDefinition, selectedBoon.objectReferenceValue);
        if (!showBoonDefinition)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        CreateCachedEditor(selectedBoon.objectReferenceValue, null, ref boonEditor);
        if (boonEditor == null)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUI.indentLevel++;
        boonEditor.OnInspectorGUI();
        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();
    }

    private void OnDisable()
    {
        if (boonEditor != null)
        {
            DestroyImmediate(boonEditor);
            boonEditor = null;
        }
    }
}
