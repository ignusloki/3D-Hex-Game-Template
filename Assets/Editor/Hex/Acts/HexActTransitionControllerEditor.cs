using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexActTransitionController))]
public sealed class HexActTransitionControllerEditor : Editor
{
    private Editor configEditor;
    private Editor act1ProfileEditor;
    private Editor act2ProfileEditor;
    private Editor act3ProfileEditor;
    private bool showConfigAsset = true;
    private bool showAct1Profile;
    private bool showAct2Profile = true;
    private bool showAct3Profile;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty useSceneTransitionConfig = serializedObject.FindProperty("useSceneTransitionConfig");
        SerializedProperty transitionConfig = serializedObject.FindProperty("transitionConfig");

        EditorGUILayout.PropertyField(useSceneTransitionConfig);
        EditorGUILayout.PropertyField(transitionConfig);

        HexActTransitionController controller = (HexActTransitionController)target;
        EditorGUILayout.Space(4f);
        EditorGUILayout.HelpBox(controller.GetConfigurationSummary(), MessageType.Info);

        if (transitionConfig.objectReferenceValue == null)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.HelpBox(
            "Editing the transition config or act profiles below changes the shared ScriptableObject assets used by this scene.",
            MessageType.None);

        showConfigAsset = EditorGUILayout.InspectorTitlebar(showConfigAsset, transitionConfig.objectReferenceValue);
        if (showConfigAsset)
        {
            CreateCachedEditor(transitionConfig.objectReferenceValue, null, ref configEditor);
            if (configEditor != null)
            {
                EditorGUI.indentLevel++;
                configEditor.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
        }

        HexActTransitionConfigAsset configAsset = controller.TransitionConfig;
        if (configAsset != null)
        {
            DrawProfileEditor("Act 1 Profile", configAsset.act1Profile, ref showAct1Profile, ref act1ProfileEditor);
            DrawProfileEditor("Act 2 Profile", configAsset.act2Profile, ref showAct2Profile, ref act2ProfileEditor);
            DrawProfileEditor("Act 3 Profile", configAsset.act3Profile, ref showAct3Profile, ref act3ProfileEditor);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawProfileEditor(string label, Object profileAsset, ref bool isExpanded, ref Editor cachedEditor)
    {
        if (profileAsset == null)
        {
            return;
        }

        EditorGUILayout.Space(6f);
        isExpanded = EditorGUILayout.InspectorTitlebar(isExpanded, profileAsset);
        if (!isExpanded)
        {
            return;
        }

        CreateCachedEditor(profileAsset, null, ref cachedEditor);
        if (cachedEditor == null)
        {
            return;
        }

        EditorGUI.indentLevel++;
        cachedEditor.OnInspectorGUI();
        EditorGUI.indentLevel--;
    }

    private void OnDisable()
    {
        DestroyCachedEditor(ref configEditor);
        DestroyCachedEditor(ref act1ProfileEditor);
        DestroyCachedEditor(ref act2ProfileEditor);
        DestroyCachedEditor(ref act3ProfileEditor);
    }

    private static void DestroyCachedEditor(ref Editor cachedEditor)
    {
        if (cachedEditor == null)
        {
            return;
        }

        DestroyImmediate(cachedEditor);
        cachedEditor = null;
    }
}
