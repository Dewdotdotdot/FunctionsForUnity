using System.Collections;
using UnityEditor;
using UnityEngine;

public class ReadOnlyInInspectorAttribute : PropertyAttribute { }

// Drawer
[CustomPropertyDrawer(typeof(ReadOnlyInInspectorAttribute))]
public class ReadOnlyInInspectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var originalColor = GUI.color;
        GUI.color = new Color(1f, .9f, .9f, 1f); 
        if (position.Contains(Event.current.mousePosition) ||
            GUI.GetNameOfFocusedControl() == property.propertyPath)
        {
            if (Event.current.isKey || Event.current.isMouse)
            {
                Event.current.Use();
            }
        }
        EditorGUI.PropertyField(position, property, label, true);
        GUI.color = originalColor;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUI.GetPropertyHeight(property, label, true);
}