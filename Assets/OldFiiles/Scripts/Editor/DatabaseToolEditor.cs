using System.IO;
using UnityEditor;
using UnityEngine;
using System.Globalization;

public class DatabaseToolEditor : EditorWindow
{
    // UI Parameters
    private string csvFilePath = "";
    private string itemFilePath = "";
    private string prefabFilePath = "";
    private string spriteFilePath = "";
    private string audioFilePath = "";
    private string eventFilePath = "";

    [MenuItem("Tools/Database Tool")]
    public static void ShowWindow()
    {
        GetWindow<DatabaseToolEditor>("Database Tool");
    }

    private void OnGUI()
    {
        // Banner
        GUILayout.Label("Database Tool", EditorStyles.boldLabel);

        // Database File Capsule
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Database File", EditorStyles.helpBox);
        csvFilePath = EditorGUILayout.TextField("CSV File Path", csvFilePath);
        EditorGUILayout.EndVertical();

        // Loot Files Capsule
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Loot Files", EditorStyles.helpBox);
        itemFilePath = EditorGUILayout.TextField("Item File Path", itemFilePath);
        EditorGUILayout.EndVertical();

        // Additional Files Capsule
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Additional Files", EditorStyles.helpBox);
        prefabFilePath = EditorGUILayout.TextField("Prefab File Path", prefabFilePath);
        spriteFilePath = EditorGUILayout.TextField("Sprite File Path", spriteFilePath);
        audioFilePath = EditorGUILayout.TextField("Audio File Path", audioFilePath);
        //eventFilePath = EditorGUILayout.TextField("Event File Path", eventFilePath);
        EditorGUILayout.EndVertical();

        // Buttons Capsule
        EditorGUILayout.BeginVertical("box");
        if (GUILayout.Button("Clear Paths"))
        {
            ClearPaths();
        }
        if (GUILayout.Button("Auto-Fill Paths"))
        {
            AutoFillPaths();
        }
        if (GUILayout.Button("Generate Loot"))
        {
            GenerateItems();
        }
        EditorGUILayout.EndVertical();
    }

    private void ClearPaths()
    {
        csvFilePath = "";
        itemFilePath = "";
        prefabFilePath = "";
        spriteFilePath = "";
        audioFilePath = "";
        eventFilePath = "";
    }

    private void AutoFillPaths()
    {
        csvFilePath = "Assets/Data/Database.csv";
        itemFilePath = "Assets/Data/Items/";
        prefabFilePath = "Assets/Art/Prefabs/Items/";
        spriteFilePath = "Assets/Art/Sprites/Items/";
        audioFilePath = "Assets/Art/Audio/UseSounds/Items/";
        // eventFilePath = ""; // If you get an event file path in the future, set it here
    }

    private void GenerateItems()
    {
        // Lines that we want to read from CSV File
        string[] lines = File.ReadAllLines(csvFilePath);

        // Start reading CSV lines after skipping the header line (int i = 1).
        for (int i = 1; i < lines.Length; i++)
        {
            // Split data using a column (,) symbol
            string[] splitData = lines[i].Split(',');

            // Type of asset we want to generate
            Item item = ScriptableObject.CreateInstance<Item>();

            // Read CSV Data
            item.fileName = splitData[0];
            item.itemName = splitData[1];
            item.abbreviation = splitData[2];
            item.type = (Item.ItemType)System.Enum.Parse(typeof(Item.ItemType), splitData[3]);
            item.rarity = (Item.ItemRarity)System.Enum.Parse(typeof(Item.ItemRarity), splitData[4]);
            item.description = splitData[5];
                    
            // Read dimensions (width and height)
            item.width = int.Parse(splitData[6]);
            item.height = int.Parse(splitData[7]);

            item.weight = float.Parse(splitData[8], CultureInfo.InvariantCulture);
            item.value = float.Parse(splitData[9], CultureInfo.InvariantCulture);

            // Get additional files
            item.sprite = (Sprite)AssetDatabase.LoadAssetAtPath(Path.Combine(spriteFilePath, item.fileName + ".png"), typeof(Sprite));
            item.prefab = (GameObject)AssetDatabase.LoadAssetAtPath(Path.Combine(prefabFilePath, item.fileName + ".prefab"), typeof(GameObject));
            item.useSound = (AudioClip)AssetDatabase.LoadAssetAtPath(Path.Combine(audioFilePath, item.fileName + ".wav"), typeof(AudioClip));
            // item.consumeEvent = AssetDatabase.LoadAssetAtPath<GameEvent>(Path.Combine(eventFilePath, item.fileName + "_Consume.asset"));

            // Generate Items
            AssetDatabase.CreateAsset(item, $"{itemFilePath}{item.fileName}.asset");
        }

        AssetDatabase.SaveAssets(); // Save generated assets
    }
}
