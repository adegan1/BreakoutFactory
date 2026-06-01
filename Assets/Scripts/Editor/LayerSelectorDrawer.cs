using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LayerSelectorAttribute))]
public class LayerSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        EditorGUI.EndProperty();
    }
}
