using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(RMF_RadialMenuElement))]
public class RMF_RadialMenuElementEditor : Editor
{
    SerializedProperty elementType;
    SerializedProperty rt;
    SerializedProperty parentRM;
    SerializedProperty button;
    SerializedProperty label;
    SerializedProperty buildingType;
    SerializedProperty targetSubmenu;
    SerializedProperty angleMin;
    SerializedProperty angleMax;
    SerializedProperty angleOffset;
    SerializedProperty active;
    SerializedProperty assignedIndex;
    
    private bool showGeneralSettings = true;
    private bool showBuildingSettings = false;
    private bool showSubmenuSettings = false;
    
    // Cache for building types
    private List<string> availableBuildingTypes = new List<string>();
    private string[] buildingTypeOptions;
    private int selectedBuildingTypeIndex = -1;
    
    void OnEnable()
    {
        // General properties
        elementType = serializedObject.FindProperty("elementType");
        rt = serializedObject.FindProperty("rt");
        parentRM = serializedObject.FindProperty("parentRM");
        button = serializedObject.FindProperty("button");
        label = serializedObject.FindProperty("label");
        
        // Building properties
        buildingType = serializedObject.FindProperty("buildingType");
        
        // Sub-menu properties
        targetSubmenu = serializedObject.FindProperty("targetSubmenu");
        
        // Technical properties
        angleMin = serializedObject.FindProperty("angleMin");
        angleMax = serializedObject.FindProperty("angleMax");
        angleOffset = serializedObject.FindProperty("angleOffset");
        active = serializedObject.FindProperty("active");
        assignedIndex = serializedObject.FindProperty("assignedIndex");
        
        // Populate building types from BuildSystem
        RefreshBuildingTypes();
    }
    
    private void RefreshBuildingTypes()
    {
        availableBuildingTypes.Clear();
        
        // Find all BuildSystem instances in the scene
        BuildSystem[] buildSystems = Object.FindObjectsOfType<BuildSystem>();
        if (buildSystems != null && buildSystems.Length > 0)
        {
            foreach (var buildSystem in buildSystems)
            {
                // Add entries from the buildingEntries list
                foreach (var entry in buildSystem.buildingEntries)
                {
                    if (!string.IsNullOrEmpty(entry.buildingType) && !availableBuildingTypes.Contains(entry.buildingType))
                    {
                        availableBuildingTypes.Add(entry.buildingType);
                    }
                }
            }
        }
        
        // If no building types were found, add placeholders
        if (availableBuildingTypes.Count == 0)
        {
            availableBuildingTypes.Add("Radio");
            availableBuildingTypes.Add("FOB");
            availableBuildingTypes.Add("AmmoCrate");
        }
        
        // Sort alphabetically
        availableBuildingTypes.Sort();
        
        // Convert to array for the popup menu
        buildingTypeOptions = availableBuildingTypes.ToArray();
        
        // Find the current index
        string currentBuildingType = buildingType.stringValue;
        selectedBuildingTypeIndex = availableBuildingTypes.IndexOf(currentBuildingType);
        if (selectedBuildingTypeIndex < 0 && availableBuildingTypes.Count > 0)
        {
            selectedBuildingTypeIndex = 0;
        }
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        RMF_RadialMenuElement menuElement = (RMF_RadialMenuElement)target;
        
        // General settings
        showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "General Settings", true);
        if (showGeneralSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(elementType);
            EditorGUILayout.PropertyField(button);
            EditorGUILayout.PropertyField(label);
            EditorGUI.indentLevel--;
        }
        
        // Show different settings based on element type
        switch (menuElement.elementType)
        {
            case RMF_RadialMenuElement.ElementType.Building:
                showBuildingSettings = EditorGUILayout.Foldout(showBuildingSettings, "Building Settings", true);
                if (showBuildingSettings)
                {
                    EditorGUI.indentLevel++;
                    
                    // Building type dropdown
                    EditorGUILayout.LabelField("Building Type", EditorStyles.boldLabel);
                    
                    // Refresh button
                    if (GUILayout.Button("Refresh Building Types"))
                    {
                        RefreshBuildingTypes();
                    }
                    
                    // Show dropdown
                    int newIndex = EditorGUILayout.Popup("Select Building Type", selectedBuildingTypeIndex, buildingTypeOptions);
                    if (newIndex != selectedBuildingTypeIndex && newIndex >= 0 && newIndex < availableBuildingTypes.Count)
                    {
                        selectedBuildingTypeIndex = newIndex;
                        buildingType.stringValue = availableBuildingTypes[newIndex];
                    }
                    
                    // Also show manual entry field
                    EditorGUILayout.PropertyField(buildingType, new GUIContent("Manual Building Type"));
                    
                    EditorGUI.indentLevel--;
                }
                break;
                
            case RMF_RadialMenuElement.ElementType.Folder:
                showSubmenuSettings = EditorGUILayout.Foldout(showSubmenuSettings, "Sub-Menu Settings", true);
                if (showSubmenuSettings)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(targetSubmenu);
                    EditorGUI.indentLevel--;
                }
                break;
                
            case RMF_RadialMenuElement.ElementType.Back:
                showSubmenuSettings = EditorGUILayout.Foldout(showSubmenuSettings, "Previous Menu Settings", true);
                if (showSubmenuSettings)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(targetSubmenu, new GUIContent("Parent Menu"));
                    EditorGUI.indentLevel--;
                }
                break;
        }
        
        // Show technical properties only in debug mode
        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Info", EditorStyles.boldLabel);
            
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(rt);
                EditorGUILayout.PropertyField(parentRM);
                EditorGUILayout.PropertyField(angleMin);
                EditorGUILayout.PropertyField(angleMax);
                EditorGUILayout.PropertyField(angleOffset);
                EditorGUILayout.PropertyField(active);
                EditorGUILayout.PropertyField(assignedIndex);
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
} 