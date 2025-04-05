#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(BuildSystem))]
public class BuildSystemEditor : Editor
{
    private BuildSystem buildSystem;
    private SerializedProperty buildingEntriesProperty;
    private SerializedProperty buildMenuProperty;
    private SerializedProperty inventoryEventsProperty;
    private SerializedProperty playerCameraProperty;
    private SerializedProperty playerStateManagerProperty;
    
    private bool showReferences = true;
    private bool showBuildingEntries = true;
    
    private string newBuildingType = "";
    private GameObject newBuildingPrefab = null;
    private float newBuildingDistance = 2.0f;
    
    private void OnEnable()
    {
        buildSystem = (BuildSystem)target;
        
        // Find serialized properties
        buildingEntriesProperty = serializedObject.FindProperty("buildingEntries");
        buildMenuProperty = serializedObject.FindProperty("buildMenu");
        inventoryEventsProperty = serializedObject.FindProperty("inventoryEvents");
        playerCameraProperty = serializedObject.FindProperty("playerCamera");
        playerStateManagerProperty = serializedObject.FindProperty("playerStateManager");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Display header info
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Build System", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Show references
        showReferences = EditorGUILayout.Foldout(showReferences, "References", true);
        if (showReferences)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(buildMenuProperty);
            EditorGUILayout.PropertyField(inventoryEventsProperty);
            EditorGUILayout.PropertyField(playerCameraProperty);
            EditorGUILayout.PropertyField(playerStateManagerProperty);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Building Entries Section
        GUIStyle headerStyle = new GUIStyle(EditorStyles.foldout);
        headerStyle.fontStyle = FontStyle.Bold;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        showBuildingEntries = EditorGUILayout.Foldout(showBuildingEntries, "Building Types", true, headerStyle);
        
        if (showBuildingEntries)
        {
            // Add header labels
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Building Type", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel, GUILayout.Width(150)); 
            EditorGUILayout.LabelField("Distance", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Display the building entries
            for (int i = 0; i < buildingEntriesProperty.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                SerializedProperty entry = buildingEntriesProperty.GetArrayElementAtIndex(i);
                SerializedProperty buildingType = entry.FindPropertyRelative("buildingType");
                SerializedProperty prefab = entry.FindPropertyRelative("prefab");
                SerializedProperty distance = entry.FindPropertyRelative("placementDistance");
                
                // Use a more compact layout
                EditorGUILayout.PropertyField(buildingType, GUIContent.none, GUILayout.Width(150));
                EditorGUILayout.PropertyField(prefab, GUIContent.none, GUILayout.Width(150));
                EditorGUILayout.PropertyField(distance, GUIContent.none, GUILayout.Width(80));
                
                // Remove button
                if (GUILayout.Button("X", GUILayout.Width(24), GUILayout.Height(20)))
                {
                    buildingEntriesProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            // Add a new entry
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Add New Building Type", EditorStyles.boldLabel);
            
            // Create the "Add" form
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            newBuildingType = EditorGUILayout.TextField("Building Type", newBuildingType);
            newBuildingPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", newBuildingPrefab, typeof(GameObject), false);
            newBuildingDistance = EditorGUILayout.FloatField("Placement Distance", newBuildingDistance);
            
            EditorGUILayout.Space(5);
            
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newBuildingType));
            if (GUILayout.Button("Add Building Type"))
            {
                // Check if the building type already exists
                bool exists = false;
                for (int i = 0; i < buildingEntriesProperty.arraySize; i++)
                {
                    SerializedProperty entry = buildingEntriesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty buildingType = entry.FindPropertyRelative("buildingType");
                    
                    if (buildingType.stringValue.Equals(newBuildingType, System.StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
                
                if (!exists)
                {
                    // Add a new entry
                    buildingEntriesProperty.arraySize++;
                    SerializedProperty newEntry = buildingEntriesProperty.GetArrayElementAtIndex(buildingEntriesProperty.arraySize - 1);
                    newEntry.FindPropertyRelative("buildingType").stringValue = newBuildingType;
                    newEntry.FindPropertyRelative("prefab").objectReferenceValue = newBuildingPrefab;
                    newEntry.FindPropertyRelative("placementDistance").floatValue = newBuildingDistance;
                    
                    // Reset the form
                    newBuildingType = "";
                    newBuildingPrefab = null;
                }
                else
                {
                    EditorUtility.DisplayDialog("Building Type Exists", 
                        $"A building type with the name '{newBuildingType}' already exists.", "OK");
                }
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Apply changes
        serializedObject.ApplyModifiedProperties();
    }
}
#endif 