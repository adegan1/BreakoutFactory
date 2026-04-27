using UnityEditor;

[CustomEditor(typeof(ItemDefinition))]
public class ItemDefinitionEditor : BreakoutDataEditorBase
{
    private SerializedProperty itemIdProperty;
    private SerializedProperty displayNameProperty;
    private SerializedProperty descriptionProperty;
    private SerializedProperty iconProperty;
    private SerializedProperty tintProperty;
    private SerializedProperty maxStackSizeProperty;
    private SerializedProperty baseValueProperty;

    private void OnEnable()
    {
        itemIdProperty = FindProperty("itemId");
        displayNameProperty = FindProperty("displayName");
        descriptionProperty = FindProperty("description");
        iconProperty = FindProperty("icon");
        tintProperty = FindProperty("tint");
        maxStackSizeProperty = FindProperty("maxStackSize");
        baseValueProperty = FindProperty("baseValue");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection("Identity", itemIdProperty, displayNameProperty, descriptionProperty);
        DrawSection("Visual", iconProperty, tintProperty);
        DrawSection("Balance", maxStackSizeProperty, baseValueProperty);

        serializedObject.ApplyModifiedProperties();
    }
}
