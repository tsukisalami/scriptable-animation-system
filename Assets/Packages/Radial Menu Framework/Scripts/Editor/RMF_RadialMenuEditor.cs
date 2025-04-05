using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEngine.UI;

[CustomEditor(typeof(RMF_RadialMenu))]
public class RMF_RadialMenuEditor : Editor
{
    private SerializedProperty rt;
    private SerializedProperty buildSystem;
    private SerializedProperty useGamepad;
    private SerializedProperty useLazySelection;
    private SerializedProperty useSelectionFollower;
    private SerializedProperty selectionFollowerContainer;
    private SerializedProperty textLabel;
    private SerializedProperty elements;
    private SerializedProperty globalOffset;
    private SerializedProperty hideAtStart;
    
    private ReorderableList elementList;
    private bool showGeneralSettings = true;
    private bool showInteractionSettings = true;
    private bool showElementSettings = true;
    private bool showAppearanceSettings = true;
    
    private void OnEnable()
    {
        rt = serializedObject.FindProperty("rt");
        buildSystem = serializedObject.FindProperty("buildSystem");
        useGamepad = serializedObject.FindProperty("useGamepad");
        useLazySelection = serializedObject.FindProperty("useLazySelection");
        useSelectionFollower = serializedObject.FindProperty("useSelectionFollower");
        selectionFollowerContainer = serializedObject.FindProperty("selectionFollowerContainer");
        textLabel = serializedObject.FindProperty("textLabel");
        elements = serializedObject.FindProperty("elements");
        globalOffset = serializedObject.FindProperty("globalOffset");
        hideAtStart = serializedObject.FindProperty("hideAtStart");
        
        SetupElementList();
    }
    
    private void SetupElementList()
    {
        elementList = new ReorderableList(serializedObject, elements, true, true, true, true);
        
        elementList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Menu Elements");
        };
        
        elementList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty element = elements.GetArrayElementAtIndex(index);
            
            // Get the element's label if possible
            string elementName = "Element " + index;
            
            if (element.objectReferenceValue != null)
            {
                RMF_RadialMenuElement menuElement = element.objectReferenceValue as RMF_RadialMenuElement;
                if (menuElement != null && !string.IsNullOrEmpty(menuElement.label))
                {
                    elementName = menuElement.label;
                }
            }
            
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                element,
                new GUIContent(elementName)
            );
        };
        
        elementList.onAddCallback = (ReorderableList list) => {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.objectReferenceValue = null;
        };
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        RMF_RadialMenu radialMenu = (RMF_RadialMenu)target;
        
        EditorGUILayout.Space();
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.fontSize = 14;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        // Title
        EditorGUILayout.LabelField("Radial Menu Framework", headerStyle);
        EditorGUILayout.Space();
        
        // General settings
        showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "General Settings", true);
        if (showGeneralSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(buildSystem);
            EditorGUILayout.PropertyField(hideAtStart);
            EditorGUI.indentLevel--;
        }
        
        // Interaction settings
        showInteractionSettings = EditorGUILayout.Foldout(showInteractionSettings, "Interaction Settings", true);
        if (showInteractionSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useGamepad);
            EditorGUILayout.PropertyField(useLazySelection);
            EditorGUILayout.PropertyField(useSelectionFollower);
            
            if (useSelectionFollower.boolValue)
            {
                EditorGUILayout.PropertyField(selectionFollowerContainer);
            }
            
            EditorGUILayout.PropertyField(textLabel);
            EditorGUI.indentLevel--;
        }
        
        // Menu elements
        showElementSettings = EditorGUILayout.Foldout(showElementSettings, "Menu Elements", true);
        if (showElementSettings)
        {
            EditorGUILayout.Space();
            elementList.DoLayoutList();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Back Button"))
            {
                AddBackButtonToMenu();
            }
            
            if (GUILayout.Button("Create Sub-Menu"))
            {
                CreateNewSubmenu();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        // Appearance settings
        showAppearanceSettings = EditorGUILayout.Foldout(showAppearanceSettings, "Appearance Settings", true);
        if (showAppearanceSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(globalOffset);
            EditorGUI.indentLevel--;
        }
        
        serializedObject.ApplyModifiedProperties();
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Hierarchical radial menus support: Building elements, Folder elements (sub-menus), and Back buttons.", MessageType.Info);
    }
    
    private void AddBackButtonToMenu()
    {
        RMF_RadialMenu menu = (RMF_RadialMenu)target;
        
        // Create a back button prefab - this should be customized based on your actual prefabs
        GameObject backButtonObj = new GameObject("Back Button");
        backButtonObj.transform.SetParent(menu.transform);
        
        // Add required components
        RectTransform rt = backButtonObj.AddComponent<RectTransform>();
        UnityEngine.UI.Button button = backButtonObj.AddComponent<UnityEngine.UI.Button>();
        RMF_RadialMenuElement element = backButtonObj.AddComponent<RMF_RadialMenuElement>();
        
        // Configure the RectTransform
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;
        
        // Configure the element
        element.elementType = RMF_RadialMenuElement.ElementType.Back;
        element.button = button;
        element.label = "Back";
        
        // Add to the menu's elements list
        int index = menu.elements.Count;
        SerializedProperty elementsArray = serializedObject.FindProperty("elements");
        elementsArray.arraySize++;
        elementsArray.GetArrayElementAtIndex(index).objectReferenceValue = element;
        
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
    
    private void CreateNewSubmenu()
    {
        RMF_RadialMenu currentMenu = (RMF_RadialMenu)target;
        
        // Create a new menu game object as a sibling to the current menu
        GameObject submenuObj = new GameObject("Sub Menu");
        submenuObj.transform.SetParent(currentMenu.transform.parent);
        
        // Add the required components
        RectTransform rt = submenuObj.AddComponent<RectTransform>();
        CanvasGroup cg = submenuObj.AddComponent<CanvasGroup>();
        RMF_RadialMenu submenu = submenuObj.AddComponent<RMF_RadialMenu>();
        
        // Configure the RectTransform
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(400, 400); // Default size
        
        // Initialize properties from the parent menu
        submenu.useGamepad = currentMenu.useGamepad;
        submenu.useLazySelection = currentMenu.useLazySelection;
        submenu.useSelectionFollower = currentMenu.useSelectionFollower;
        submenu.selectionFollowerContainer = currentMenu.selectionFollowerContainer;
        submenu.textLabel = currentMenu.textLabel;
        submenu.buildSystem = currentMenu.buildSystem;
        submenu.globalOffset = currentMenu.globalOffset;
        submenu.hideAtStart = true;
        
        // Create a back button for the submenu
        GameObject backButtonObj = new GameObject("Back Button");
        backButtonObj.transform.SetParent(submenu.transform);
        
        // Add required components to the back button
        RectTransform backRt = backButtonObj.AddComponent<RectTransform>();
        UnityEngine.UI.Button backButton = backButtonObj.AddComponent<UnityEngine.UI.Button>();
        RMF_RadialMenuElement backElement = backButtonObj.AddComponent<RMF_RadialMenuElement>();
        
        // Configure the back button
        backRt.localScale = Vector3.one;
        backRt.anchoredPosition = Vector2.zero;
        
        backElement.elementType = RMF_RadialMenuElement.ElementType.Back;
        backElement.button = backButton;
        backElement.label = "Back";
        backElement.targetSubmenu = currentMenu;
        
        // Add the back button to the submenu's elements list
        submenu.elements = new List<RMF_RadialMenuElement>() { backElement };
        
        // Create a folder element in the current menu that points to the new submenu
        GameObject folderObj = new GameObject("Folder Element");
        folderObj.transform.SetParent(currentMenu.transform);
        
        // Add required components to the folder
        RectTransform folderRt = folderObj.AddComponent<RectTransform>();
        UnityEngine.UI.Button folderButton = folderObj.AddComponent<UnityEngine.UI.Button>();
        RMF_RadialMenuElement folderElement = folderObj.AddComponent<RMF_RadialMenuElement>();
        
        // Configure the folder element
        folderRt.localScale = Vector3.one;
        folderRt.anchoredPosition = Vector2.zero;
        
        folderElement.elementType = RMF_RadialMenuElement.ElementType.Folder;
        folderElement.button = folderButton;
        folderElement.label = "Sub Menu";
        folderElement.targetSubmenu = submenu;
        
        // Add to the current menu's elements list
        int index = currentMenu.elements.Count;
        SerializedProperty elementsArray = serializedObject.FindProperty("elements");
        elementsArray.arraySize++;
        elementsArray.GetArrayElementAtIndex(index).objectReferenceValue = folderElement;
        
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(submenu);
        
        // Select the new submenu in the hierarchy
        Selection.activeGameObject = submenuObj;
    }
} 