using UnityEngine;
using UnityEditor;
using System.IO;
using Demo.Scripts.Runtime.Item;
using Ballistics;

public class MagazineManagerMigrationTool : EditorWindow
{
    private GameObject weaponPrefab;
    private string assetSavePath = "Assets/MagazineData";
    
    [MenuItem("Tools/Magazine Migration Tool")]
    public static void ShowWindow()
    {
        GetWindow<MagazineManagerMigrationTool>("Magazine Migration Tool");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Convert MagazineManager to MagazineData", EditorStyles.boldLabel);
        
        weaponPrefab = (GameObject)EditorGUILayout.ObjectField("Weapon Prefab", weaponPrefab, typeof(GameObject), false);
        assetSavePath = EditorGUILayout.TextField("Save Path", assetSavePath);
        
        EditorGUILayout.HelpBox("This tool will convert a MagazineManager component on a weapon to a MagazineData asset and assign it to the weapon's defaultMagazine field.", MessageType.Info);
        
        EditorGUI.BeginDisabledGroup(weaponPrefab == null);
        if (GUILayout.Button("Migrate Magazine"))
        {
            MigrateWeaponMagazine();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Batch Migration", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("Find and convert all weapon prefabs with MagazineManager components in your project.", MessageType.Info);
        
        if (GUILayout.Button("Find and Migrate All Weapons"))
        {
            MigrateAllWeapons();
        }
    }
    
    private void MigrateWeaponMagazine()
    {
        if (weaponPrefab == null) return;
        
        // Check if this is a weapon - use full namespace path
        Demo.Scripts.Runtime.Item.Weapon weapon = weaponPrefab.GetComponent<Demo.Scripts.Runtime.Item.Weapon>();
        if (weapon == null)
        {
            weapon = weaponPrefab.GetComponentInChildren<Demo.Scripts.Runtime.Item.Weapon>();
            if (weapon == null)
            {
                EditorUtility.DisplayDialog("Error", "No Weapon component found on this prefab!", "OK");
                return;
            }
        }
        
        // Find MagazineManager
        MagazineManager magazineManager = weaponPrefab.GetComponentInChildren<MagazineManager>();
        if (magazineManager == null)
        {
            EditorUtility.DisplayDialog("Error", "No MagazineManager component found on this prefab!", "OK");
            return;
        }
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(assetSavePath))
        {
            Directory.CreateDirectory(assetSavePath);
        }
        
        // Create MagazineData asset
        MagazineData magazineData = ScriptableObject.CreateInstance<MagazineData>();
        
        // Copy data from MagazineManager to MagazineData
        if (magazineManager.magazineData != null)
        {
            magazineData.reloadTime = magazineManager.magazineData.reloadTime;
            magazineData.ergonomics = magazineManager.magazineData.ergonomics;
            magazineData.weight = magazineManager.magazineData.weight;
            magazineData.caliber = magazineManager.magazineData.caliber;
            magazineData.ammoCount = magazineManager.magazineData.ammoCount;
            
            // Copy bullet types
            foreach (var bulletType in magazineManager.magazineData.bulletTypes)
            {
                magazineData.bulletTypes.Add(bulletType);
            }
        }
        
        // Save asset
        string assetPath = $"{assetSavePath}/{weaponPrefab.name}_Magazine.asset";
        AssetDatabase.CreateAsset(magazineData, assetPath);
        AssetDatabase.SaveAssets();
        
        // Assign to weapon
        string prefabPath = AssetDatabase.GetAssetPath(weaponPrefab);
        if (!string.IsNullOrEmpty(prefabPath))
        {
            Object prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            Demo.Scripts.Runtime.Item.Weapon prefabWeapon = ((GameObject)prefabRoot).GetComponentInChildren<Demo.Scripts.Runtime.Item.Weapon>();
            
            if (prefabWeapon != null)
            {
                prefabWeapon.defaultMagazine = magazineData;
                PrefabUtility.SaveAsPrefabAsset((GameObject)prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents((GameObject)prefabRoot);
            }
        }
        
        EditorUtility.DisplayDialog("Success", $"Magazine migrated successfully! Asset saved to {assetPath}", "OK");
    }
    
    private void MigrateAllWeapons()
    {
        // Find all weapon prefabs in the project
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int migratedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;
            
            Demo.Scripts.Runtime.Item.Weapon weapon = prefab.GetComponentInChildren<Demo.Scripts.Runtime.Item.Weapon>();
            if (weapon == null) continue;
            
            MagazineManager magazineManager = prefab.GetComponentInChildren<MagazineManager>();
            if (magazineManager == null) continue;
            
            // Found a weapon with MagazineManager, migrate it
            weaponPrefab = prefab;
            MigrateWeaponMagazine();
            migratedCount++;
        }
        
        EditorUtility.DisplayDialog("Batch Migration Complete", $"Migrated {migratedCount} weapons.", "OK");
    }
} 