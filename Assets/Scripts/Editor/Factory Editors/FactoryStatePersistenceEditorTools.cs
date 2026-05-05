using UnityEditor;
using UnityEngine;

public static class FactoryStatePersistenceEditorTools
{
    [MenuItem("Tools/Breakout Factory/Clear Saved Factory State")]
    private static void ClearSavedFactoryState()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Clear Saved Factory State",
            "This will delete the persisted factory save data. Your placed factory layout will be reset the next time the factory scene loads.\n\nContinue?",
            "Clear Save",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        FactoryStatePersistence.ClearSaveData();
        Debug.Log("FactoryStatePersistence: Saved factory state cleared from editor.");
    }
}

[CustomEditor(typeof(FactoryStatePersistence))]
public class FactoryStatePersistenceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Clear Saved Factory State"))
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear Saved Factory State",
                "This will delete the persisted factory save data. Your placed factory layout will be reset the next time the factory scene loads.\n\nContinue?",
                "Clear Save",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            FactoryStatePersistence.ClearSaveData();
            Debug.Log("FactoryStatePersistence: Saved factory state cleared from inspector.");
        }
    }
}
