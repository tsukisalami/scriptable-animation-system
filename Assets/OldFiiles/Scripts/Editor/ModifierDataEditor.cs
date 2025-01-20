using UnityEditor;
using UnityEngine;
using Demo.Scripts.Runtime.AttachmentSystem;

[CustomEditor(typeof(ModifierData))]
public class ModifierDataEditor : Editor
{
    private SerializedProperty modifiersProperty;

    private void OnEnable()
    {
        modifiersProperty = serializedObject.FindProperty("modifiers");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Display each modifier with a remove button
        for (int i = 0; i < modifiersProperty.arraySize; i++)
        {
            var modifierProperty = modifiersProperty.GetArrayElementAtIndex(i);
            var modifierTypeProperty = modifierProperty.FindPropertyRelative("modifierType");
            var valueTypeProperty = modifierProperty.FindPropertyRelative("valueType");
            var valueProperty = modifierProperty.FindPropertyRelative("value");

            EditorGUILayout.PropertyField(modifierTypeProperty);
            EditorGUILayout.PropertyField(valueTypeProperty);
            EditorGUILayout.PropertyField(valueProperty);

            if (GUILayout.Button("Remove Modifier"))
            {
                modifiersProperty.DeleteArrayElementAtIndex(i);
            }

            EditorGUILayout.Space();
        }

        // Add a button to add new modifiers
        if (GUILayout.Button("Add Modifier"))
        {
            modifiersProperty.InsertArrayElementAtIndex(modifiersProperty.arraySize);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
