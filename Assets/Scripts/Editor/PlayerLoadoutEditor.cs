using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Demo.Scripts.Runtime.Item;
using Ballistics;

[CustomEditor(typeof(PlayerLoadout))]
public class PlayerLoadoutEditor : Editor
{
    // Category properties
    private SerializedProperty primaryWeaponsProperty;
    private SerializedProperty secondaryWeaponsProperty;
    private SerializedProperty throwablesProperty;
    private SerializedProperty specialEquipmentProperty;
    private SerializedProperty medicalEquipmentProperty;
    private SerializedProperty toolsProperty;

    // Performance optimization: Track when we need to update the display
    private float lastRepaintTime = 0f;
    private const float RepaintInterval = 0.5f; // Update every half second at most

    private void OnEnable()
    {
        // Find all category properties
        primaryWeaponsProperty = serializedObject.FindProperty("primaryWeapons");
        secondaryWeaponsProperty = serializedObject.FindProperty("secondaryWeapons");
        throwablesProperty = serializedObject.FindProperty("throwables");
        specialEquipmentProperty = serializedObject.FindProperty("specialEquipment");
        medicalEquipmentProperty = serializedObject.FindProperty("medicalEquipment");
        toolsProperty = serializedObject.FindProperty("tools");
        
        // Register for scene updates at a reasonable interval
        EditorApplication.update += OnEditorUpdate;
        
        // Register to the weapon events
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponFired += OnWeaponDataChanged;
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponReloaded += OnWeaponDataChanged;
    }
    
    private void OnDisable()
    {
        // Unregister events to prevent memory leaks
        EditorApplication.update -= OnEditorUpdate;
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponFired -= OnWeaponDataChanged;
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponReloaded -= OnWeaponDataChanged;
    }
    
    private void OnEditorUpdate()
    {
        // Only repaint occasionally to avoid performance issues
        if (Time.realtimeSinceStartup - lastRepaintTime > RepaintInterval)
        {
            lastRepaintTime = Time.realtimeSinceStartup;
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
    
    private void OnWeaponDataChanged()
    {
        // Force immediate repaint when weapon fires or reloads
        lastRepaintTime = Time.realtimeSinceStartup;
        Repaint();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw all categories
        EditorGUILayout.LabelField("Equipment Categories", EditorStyles.boldLabel);
        DrawCategory("Primary Weapons", primaryWeaponsProperty);
        DrawCategory("Secondary Weapons", secondaryWeaponsProperty);
        DrawCategory("Throwables", throwablesProperty);
        DrawCategory("Special Equipment", specialEquipmentProperty);
        DrawCategory("Medical Equipment", medicalEquipmentProperty);
        DrawCategory("Tools", toolsProperty);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCategory(string label, SerializedProperty categoryProperty)
    {
        if (categoryProperty == null) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        // Draw base properties
        EditorGUILayout.PropertyField(categoryProperty.FindPropertyRelative("categoryName"));
        EditorGUILayout.PropertyField(categoryProperty.FindPropertyRelative("currentIndex"));

        // Draw items table
        DrawItemsTable(categoryProperty);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawItemsTable(SerializedProperty categoryProperty)
    {
        // Get properties
        SerializedProperty itemsProperty = categoryProperty.FindPropertyRelative("items");
        SerializedProperty itemTypesProperty = categoryProperty.FindPropertyRelative("itemTypes");
        SerializedProperty itemCountsProperty = categoryProperty.FindPropertyRelative("itemCounts");

        if (itemsProperty == null || itemTypesProperty == null || itemCountsProperty == null)
            return;

        // Make sure array sizes match
        EnsureArraySizesMatch(itemsProperty, itemTypesProperty, itemCountsProperty);

        // Draw table header
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Item Type", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel, GUILayout.Width(180));
        EditorGUILayout.LabelField("Count", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        // Draw items
        for (int i = 0; i < itemsProperty.arraySize; i++)
        {
            DrawItemRow(i, itemsProperty, itemTypesProperty, itemCountsProperty);
        }

        // Add button
        if (GUILayout.Button("Add New Item"))
        {
            AddNewItem(itemsProperty, itemTypesProperty, itemCountsProperty);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawItemRow(int index, SerializedProperty itemsProperty, SerializedProperty itemTypesProperty, SerializedProperty itemCountsProperty)
    {
        EditorGUILayout.BeginHorizontal();

        // Get properties for this item
        SerializedProperty itemProperty = itemsProperty.GetArrayElementAtIndex(index);
        SerializedProperty itemTypeProperty = index < itemTypesProperty.arraySize ? 
            itemTypesProperty.GetArrayElementAtIndex(index) : null;
        SerializedProperty itemCountProperty = index < itemCountsProperty.arraySize ? 
            itemCountsProperty.GetArrayElementAtIndex(index) : null;

        // Item Type
        if (itemTypeProperty != null)
        {
            EditorGUILayout.PropertyField(itemTypeProperty, GUIContent.none, GUILayout.Width(100));
        }
        else
        {
            EditorGUILayout.LabelField("Unknown", GUILayout.Width(100));
        }

        // Item prefab
        EditorGUILayout.PropertyField(itemProperty, GUIContent.none, GUILayout.Width(180));

        // Item count (only if not a tool)
        if (itemTypeProperty != null && itemCountProperty != null)
        {
            ItemType itemType = (ItemType)itemTypeProperty.enumValueIndex;
            if (itemType != ItemType.Tool)
            {
                EditorGUILayout.PropertyField(itemCountProperty, GUIContent.none, GUILayout.Width(60));
            }
            else
            {
                EditorGUILayout.LabelField("", GUILayout.Width(60));
            }
        }
        else
        {
            EditorGUILayout.LabelField("", GUILayout.Width(60));
        }

        // Delete button
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            DeleteArrayElement(index, itemsProperty, itemTypesProperty, itemCountsProperty);
        }

        EditorGUILayout.EndHorizontal();

        // Show magazine data for weapons
        if (itemTypeProperty != null && itemProperty.objectReferenceValue != null)
        {
            ItemType itemType = (ItemType)itemTypeProperty.enumValueIndex;
            if (itemType == ItemType.Weapon)
            {
                ShowWeaponMagazineInfo(itemProperty, itemCountProperty);
            }
        }
    }

    private void ShowWeaponMagazineInfo(SerializedProperty itemProperty, SerializedProperty itemCountProperty)
    {
        Demo.Scripts.Runtime.Item.Weapon weapon = itemProperty.objectReferenceValue as Demo.Scripts.Runtime.Item.Weapon;
        
        if (weapon != null && weapon.defaultMagazine != null)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Magazine Data:", EditorStyles.boldLabel);
            
            string magName = weapon.defaultMagazine.name;
            EditorGUILayout.LabelField($"Magazine: {magName}");
            
            int ammoCount = weapon.defaultMagazine.ammoCount;
            EditorGUILayout.LabelField($"Capacity: {ammoCount} rounds");
            
            if (itemCountProperty != null)
            {
                int magCount = itemCountProperty.intValue;
                EditorGUILayout.LabelField($"Magazines: {magCount}");
            }
            
            // Add foldout for individual magazines
            EditorGUILayout.Space(5);
            
            PlayerLoadout targetLoadout = (PlayerLoadout)serializedObject.targetObject;
            ShowMagazinesInPlayMode(weapon, targetLoadout, itemProperty, itemCountProperty);
            
            EditorGUILayout.EndVertical();
            
            EditorGUI.indentLevel--;
        }
    }

    // Method to show magazine details in play mode
    private void ShowMagazinesInPlayMode(Demo.Scripts.Runtime.Item.Weapon weapon, PlayerLoadout targetLoadout, SerializedProperty itemProperty, SerializedProperty itemCountProperty)
    {
        // No more constant force repaint - this is now handled by the event system
        if (Application.isPlaying)
        {
            EditorUtility.SetDirty(targetLoadout);
        }
        
        // Prepare section header style
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.2f, 0.2f, 0.2f);
        
        // Show section header
        EditorGUILayout.LabelField("Individual Magazines", headerStyle);
        
        if (Application.isPlaying && targetLoadout != null)
        {
            // Find the category and item index for this weapon
            int categoryIndex = -1;
            int itemIndex = -1;
            
            // Find which category this weapon is in
            for (int i = 0; i < 6; i++)
            {
                var category = targetLoadout.GetCategory(i);
                if (category == null) continue;
                
                for (int j = 0; j < category.items.Count; j++)
                {
                    if (category.items[j] == itemProperty.objectReferenceValue)
                    {
                        categoryIndex = i;
                        itemIndex = j;
                        break;
                    }
                }
                
                if (categoryIndex >= 0) break;
            }
            
            if (categoryIndex >= 0 && itemIndex >= 0)
            {
                var category = targetLoadout.GetCategory(categoryIndex);
                
                // Check if we have magazine data
                if (category.weaponMagazineTemplates.Count > itemIndex &&
                    category.weaponMagazineBullets.Count > itemIndex)
                {
                    var magazineBullets = category.weaponMagazineBullets[itemIndex];
                    var magazineTemplates = category.weaponMagazineTemplates[itemIndex];
                    
                    // Get reference to actual weapon to check its current magazine bullets
                    Demo.Scripts.Runtime.Item.Weapon weaponInstance = null;
                    
                    if (targetLoadout.currentCategoryIndex == categoryIndex && 
                        category.currentIndex == itemIndex)
                    {
                        // Try to get the actual weapon instance from FPSController
                        var fpsController = targetLoadout.GetComponent<Demo.Scripts.Runtime.Character.FPSController>();
                        if (fpsController != null && fpsController._instantiatedWeapons != null && 
                            fpsController._activeWeaponIndex < fpsController._instantiatedWeapons.Count)
                        {
                            weaponInstance = fpsController._instantiatedWeapons[fpsController._activeWeaponIndex] as Demo.Scripts.Runtime.Item.Weapon;
                        }
                    }
                    
                    // Show loaded magazine if we have one (first magazine is considered loaded)
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Currently Loaded:", GUILayout.Width(120));
                    
                    // If we can access the weapon instance directly, show the actual bullets it has
                    if (weaponInstance != null)
                    {
                        System.Reflection.FieldInfo bulletField = typeof(Demo.Scripts.Runtime.Item.Weapon).GetField("bulletsInMag", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                        if (bulletField != null)
                        {
                            var actualBullets = bulletField.GetValue(weaponInstance) as List<BulletInfo>;
                            int bulletCount = actualBullets?.Count ?? 0;
                            
                            // Calculate fill percentage
                            float fillPercentage = 0;
                            if (magazineTemplates.Count > 0 && magazineTemplates[0] != null)
                            {
                                fillPercentage = (float)bulletCount / magazineTemplates[0].ammoCount;
                            }
                            
                            // Draw progress bar with actual bullet count
                            EditorGUI.ProgressBar(
                                EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                                fillPercentage,
                                $"{bulletCount} / {(magazineTemplates.Count > 0 ? magazineTemplates[0].ammoCount : 0)}"
                            );
                        }
                        else
                        {
                            // Fallback if we can't access the field
                            EditorGUILayout.LabelField("Cannot access magazine data", GUILayout.Height(20));
                        }
                    }
                    else if (magazineBullets.Count > 0)
                    {
                        // Use the magazine count from the loadout
                        // Calculate fill percentage
                        float fillPercentage = 0;
                        if (magazineTemplates.Count > 0 && magazineTemplates[0] != null)
                        {
                            fillPercentage = (float)magazineBullets[0].Count / magazineTemplates[0].ammoCount;
                        }
                        
                        // Draw progress bar for the loaded magazine
                        EditorGUI.ProgressBar(
                            EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                            fillPercentage,
                            $"{magazineBullets[0].Count} / {(magazineTemplates.Count > 0 ? magazineTemplates[0].ammoCount : 0)}"
                        );
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No loaded magazine", GUILayout.Height(20));
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Show spare magazines section
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Spare Magazines:", headerStyle);
                    
                    // Skip the first magazine as it's the loaded one
                    if (magazineBullets.Count > 1)
                    {
                        EditorGUILayout.BeginVertical();
                        
                        for (int i = 1; i < magazineBullets.Count; i++)
                        {
                            if (i < magazineTemplates.Count && magazineTemplates[i] != null)
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField($"Magazine {i}:", GUILayout.Width(80));
                                
                                // Calculate fill percentage
                                float fillPercentage = (float)magazineBullets[i].Count / magazineTemplates[i].ammoCount;
                                
                                // Draw progress bar
                                EditorGUI.ProgressBar(
                                    EditorGUILayout.GetControlRect(GUILayout.Height(18)),
                                    fillPercentage,
                                    $"{magazineBullets[i].Count} / {magazineTemplates[i].ammoCount}"
                                );
                                
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        
                        EditorGUILayout.EndVertical();
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No spare magazines", EditorStyles.label);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No magazine data available", MessageType.Info);
                }
            }
        }
        else
        {
            // In edit mode, show preview of how magazines will appear
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Currently Loaded:", GUILayout.Width(120));
            
            // Create a preview progress bar with the magazine capacity
            if (weapon != null && weapon.defaultMagazine != null)
            {
                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                    1.0f, // Full in edit mode
                    $"{weapon.defaultMagazine.ammoCount} / {weapon.defaultMagazine.ammoCount}"
                );
            }
            else
            {
                EditorGUILayout.LabelField("No magazine defined", GUILayout.Height(20));
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Preview of spare magazines
            if (itemCountProperty != null && itemCountProperty.intValue > 1)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Spare Magazines (Preview):", headerStyle);
                
                EditorGUILayout.BeginVertical();
                
                for (int i = 1; i < itemCountProperty.intValue; i++)
                {
                    if (weapon != null && weapon.defaultMagazine != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Magazine {i}:", GUILayout.Width(80));
                        
                        // In edit mode, all magazines are full
                        EditorGUI.ProgressBar(
                            EditorGUILayout.GetControlRect(GUILayout.Height(18)),
                            1.0f,
                            $"{weapon.defaultMagazine.ammoCount} / {weapon.defaultMagazine.ammoCount}"
                        );
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            else if (itemCountProperty != null)
            {
                EditorGUILayout.LabelField("No spare magazines", EditorStyles.label);
            }
        }
    }

    private void EnsureArraySizesMatch(params SerializedProperty[] properties)
    {
        if (properties.Length == 0) return;
        
        // Find the maximum array size
        int maxSize = 0;
        foreach (var prop in properties)
        {
            if (prop != null && prop.isArray && prop.arraySize > maxSize)
                maxSize = prop.arraySize;
        }
        
        // Ensure all arrays are the same size
        foreach (var prop in properties)
        {
            if (prop != null && prop.isArray)
                prop.arraySize = maxSize;
        }
    }

    private void AddNewItem(SerializedProperty itemsProperty, SerializedProperty itemTypesProperty, SerializedProperty itemCountsProperty)
    {
        // Increase array sizes
        itemsProperty.arraySize++;
        
        if (itemTypesProperty != null)
            itemTypesProperty.arraySize = itemsProperty.arraySize;
            
        if (itemCountsProperty != null)
            itemCountsProperty.arraySize = itemsProperty.arraySize;
        
        // Set default values
        int newIndex = itemsProperty.arraySize - 1;
        
        itemsProperty.GetArrayElementAtIndex(newIndex).objectReferenceValue = null;
        
        if (itemTypesProperty != null && newIndex < itemTypesProperty.arraySize)
            itemTypesProperty.GetArrayElementAtIndex(newIndex).enumValueIndex = 0;
            
        if (itemCountsProperty != null && newIndex < itemCountsProperty.arraySize)
            itemCountsProperty.GetArrayElementAtIndex(newIndex).intValue = 1;
    }

    private void DeleteArrayElement(int index, params SerializedProperty[] properties)
    {
        foreach (var property in properties)
        {
            if (property != null && property.isArray && index < property.arraySize)
            {
                property.DeleteArrayElementAtIndex(index);
            }
        }
    }
} 