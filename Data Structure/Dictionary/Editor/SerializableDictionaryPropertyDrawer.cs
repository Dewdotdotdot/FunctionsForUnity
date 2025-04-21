using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System;
using System.Linq;
using CustomDictionary.SerializableDictionary;

[CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
public class SerializableDictionaryPropertyDrawer : PropertyDrawer
{
    private ReorderableList list;
    private SerializedProperty keysProp;
    private SerializedProperty valuesProp;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (list == null) CreateList(property);
        list.DoList(position);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (list == null) CreateList(property);
        return list.GetHeight();
    }

    private void CreateList(SerializedProperty property)
    {
        keysProp = property.FindPropertyRelative("keys");
        valuesProp = property.FindPropertyRelative("values");

        list = new ReorderableList(property.serializedObject, keysProp, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, property.displayName),
            elementHeightCallback = index =>
            {
                var keyElem = keysProp.GetArrayElementAtIndex(index);
                var valueElem = valuesProp.GetArrayElementAtIndex(index);
                float keyHeight = EditorGUI.GetPropertyHeight(keyElem, GUIContent.none, false);
                float valHeight = EditorGUI.GetPropertyHeight(valueElem, GUIContent.none, true);
                return keyHeight + valHeight + EditorGUIUtility.standardVerticalSpacing;
            },
            drawElementCallback = (rect, index, active, focused) =>
            {
                var keyElem = keysProp.GetArrayElementAtIndex(index);
                var valueElem = valuesProp.GetArrayElementAtIndex(index);
                float keyHeight = EditorGUI.GetPropertyHeight(keyElem, GUIContent.none, false);
                float valHeight = EditorGUI.GetPropertyHeight(valueElem, GUIContent.none, true);

                // Draw key
                Rect keyRect = new Rect(rect.x, rect.y, rect.width, keyHeight);
                EditorGUI.PropertyField(keyRect, keyElem, GUIContent.none);

                // Draw value below key, indented
                Rect valRect = new Rect(rect.x + 15, rect.y + keyHeight + EditorGUIUtility.standardVerticalSpacing,
                                         rect.width - 15, valHeight);
                EditorGUI.PropertyField(valRect, valueElem, GUIContent.none, true);
            },
            onAddCallback = list =>
            {
                int idx = keysProp.arraySize;
                keysProp.arraySize++;
                valuesProp.arraySize++;

                Type dictType = fieldInfo.FieldType;
                Type keyType = dictType.GetGenericArguments()[0];
                Type valueType = dictType.GetGenericArguments()[1];

                // Unique key generation
                object uniqueKey = CreateUniqueKey(keyType);
                var newKeyProp = keysProp.GetArrayElementAtIndex(idx);
                SetPropertyValue(newKeyProp, uniqueKey);

                // Initialize new value based on its type
                var newValProp = valuesProp.GetArrayElementAtIndex(idx);
                InitializeValueProperty(newValProp, valueType);

                property.serializedObject.ApplyModifiedProperties();
            },
            onRemoveCallback = list =>
            {
                int idx = list.index;
                if (idx >= 0 && idx < keysProp.arraySize)
                {
                    keysProp.DeleteArrayElementAtIndex(idx);
                    valuesProp.DeleteArrayElementAtIndex(idx);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        };
    }

    private object CreateUniqueKey(Type keyType)
    {
        if (keyType == typeof(string))
        {
            const string baseName = "NewKey";
            int suffix = 0;
            string candidate;
            do
            {
                candidate = baseName + (suffix > 0 ? suffix.ToString() : "");
                suffix++;
            } while (KeyExists(candidate));
            return candidate;
        }
        if (keyType == typeof(int))
        {
            // Use incremental integers starting from 0
            var existing = Enumerable.Range(0, keysProp.arraySize)
                                     .Select(i => keysProp.GetArrayElementAtIndex(i).intValue);
            int next = existing.Any() ? existing.Max() + 1 : 0;
            return next;
        }
        if (keyType.IsEnum)
        {
            var names = Enum.GetNames(keyType);
            for (int i = 0; i < names.Length; i++)
            {
                if (!KeyExists(Enum.ToObject(keyType, i)))
                    return Enum.ToObject(keyType, i);
            }
            return Enum.ToObject(keyType, 0);
        }
        if (keyType == typeof(Guid))
        {
            Guid guid;
            do { guid = Guid.NewGuid(); }
            while (KeyExists(guid));
            return guid;
        }
        // Fallback for other structs/classes
        return Activator.CreateInstance(keyType);
    }

    private bool KeyExists(object candidate)
    {
        for (int i = 0; i < keysProp.arraySize; i++)
        {
            var elem = keysProp.GetArrayElementAtIndex(i);
            switch (elem.propertyType)
            {
                case SerializedPropertyType.String:
                    if ((string)candidate == elem.stringValue) return true;
                    break;
                case SerializedPropertyType.Integer:
                    if ((int)candidate == elem.intValue) return true;
                    break;
                case SerializedPropertyType.Enum:
                    if (((int)candidate) == elem.enumValueIndex) return true;
                    break;
                case SerializedPropertyType.ManagedReference:
                    if (elem.managedReferenceValue != null && elem.managedReferenceValue.Equals(candidate)) return true;
                    break;
                    // other types as needed
            }
        }
        return false;
    }

    private void SetPropertyValue(SerializedProperty prop, object value)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.String:
                prop.stringValue = value as string;
                break;
            case SerializedPropertyType.Integer:
                prop.intValue = Convert.ToInt32(value);
                break;
            case SerializedPropertyType.Enum:
                prop.enumValueIndex = (int)value;
                break;
            case SerializedPropertyType.ManagedReference:
                prop.managedReferenceValue = value;
                break;
                // add more as needed
        }
    }

    private void InitializeValueProperty(SerializedProperty prop, Type valueType)
    {
        if (prop.propertyType == SerializedPropertyType.ManagedReference)
        {
            prop.managedReferenceValue = Activator.CreateInstance(valueType);
        }
        else
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = default;
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = default;
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = default;
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = string.Empty;
                    break;
                case SerializedPropertyType.Enum:
                    prop.enumValueIndex = 0;
                    break;
                    // add more cases if needed
            }
        }
    }
}