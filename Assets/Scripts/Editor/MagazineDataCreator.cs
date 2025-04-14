using UnityEngine;
using UnityEditor;
using System.IO;
using Ballistics;

public class MagazineDataCreator : EditorWindow
{
    private string magazineName = "New Magazine";
    private float reloadTime = 2f;
    private float ergonomics = 1f;
    private float weight = 1f;
    private BulletCaliber caliber;
    private int ammoCount = 30;

    [MenuItem("Assets/Create/Weapon System/Magazine Data")]
    public static void ShowWindow()
    {
        GetWindow<MagazineDataCreator>("Create Magazine Data");
    }

    private void OnGUI()
    {
        GUILayout.Label("Magazine Data Creator", EditorStyles.boldLabel);
        
        magazineName = EditorGUILayout.TextField("Magazine Name", magazineName);
        reloadTime = EditorGUILayout.FloatField("Reload Time", reloadTime);
        ergonomics = EditorGUILayout.FloatField("Ergonomics", ergonomics);
        weight = EditorGUILayout.FloatField("Weight", weight);
        caliber = (BulletCaliber)EditorGUILayout.EnumPopup("Caliber", caliber);
        ammoCount = EditorGUILayout.IntField("Ammo Count", ammoCount);
        
        if (GUILayout.Button("Create Magazine Data"))
        {
            CreateMagazineData();
        }
    }

    private void CreateMagazineData()
    {
        // Create the magazine data asset
        MagazineData magazineData = ScriptableObject.CreateInstance<MagazineData>();
        
        // Set the magazine properties
        magazineData.reloadTime = reloadTime;
        magazineData.ergonomics = ergonomics;
        magazineData.weight = weight;
        magazineData.caliber = caliber;
        magazineData.ammoCount = ammoCount;
        
        // Create the asset file
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Magazine Data",
            magazineName,
            "asset",
            "Choose a location to save the magazine data"
        );
        
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(magazineData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the created asset
            Selection.activeObject = magazineData;
            
            Debug.Log("Magazine Data created at: " + path);
        }
    }
} 