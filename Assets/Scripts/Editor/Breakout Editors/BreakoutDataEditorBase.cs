using UnityEditor;

public abstract class BreakoutDataEditorBase : Editor
{
    protected SerializedProperty FindProperty(string propertyName)
    {
        return serializedObject.FindProperty(propertyName);
    }

    protected void DrawSection(string label, params SerializedProperty[] properties)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        for (int i = 0; i < properties.Length; i++)
        {
            EditorGUILayout.PropertyField(properties[i]);
        }
    }

    protected void DrawConditionalGroup(SerializedProperty toggleProperty, params SerializedProperty[] properties)
    {
        if (!toggleProperty.boolValue)
        {
            return;
        }

        EditorGUI.indentLevel++;
        for (int i = 0; i < properties.Length; i++)
        {
            EditorGUILayout.PropertyField(properties[i], includeChildren: true);
        }

        EditorGUI.indentLevel--;
    }
}