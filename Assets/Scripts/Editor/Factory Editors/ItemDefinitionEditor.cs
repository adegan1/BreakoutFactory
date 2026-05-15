using UnityEditor;

[CustomEditor(typeof(ItemDefinition))]
public class ItemDefinitionEditor : BreakoutDataEditorBase
{
    private SerializedProperty itemIdProperty;
    private SerializedProperty displayNameProperty;
    private SerializedProperty descriptionProperty;
    private SerializedProperty iconProperty;
    private SerializedProperty tintProperty;
    private SerializedProperty baseValueProperty;
    private SerializedProperty isFusionProperty;
    private SerializedProperty isCompoundProperty;

    private void OnEnable()
    {
        itemIdProperty = FindProperty("itemId");
        displayNameProperty = FindProperty("displayName");
        descriptionProperty = FindProperty("description");
        iconProperty = FindProperty("icon");
        tintProperty = FindProperty("tint");
        baseValueProperty = FindProperty("baseValue");
        isFusionProperty = FindProperty("isFusion");
        isCompoundProperty = FindProperty("isCompound");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection("Identity", itemIdProperty, displayNameProperty, descriptionProperty);
        DrawSection("Visual", iconProperty, tintProperty);
        DrawSection("Balance", baseValueProperty);
        DrawSection("Composition", isFusionProperty, isCompoundProperty);

        serializedObject.ApplyModifiedProperties();
    }
}
