using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Ballistics;

[CustomEditor(typeof(MagazineData))]
public class MagazineDataEditor : Editor
{
    private SerializedProperty reloadTimeProperty;
    private SerializedProperty ergonomicsProperty;
    private SerializedProperty weightProperty;
    private SerializedProperty caliberProperty;
    private SerializedProperty ammoCountProperty;
    private SerializedProperty bulletTypesProperty;
    
    // Sample bullets for display
    private List<BulletInfo> sampleBullets = new List<BulletInfo>();
    private bool showSampleBullets = false;

    private void OnEnable()
    {
        reloadTimeProperty = serializedObject.FindProperty("reloadTime");
        ergonomicsProperty = serializedObject.FindProperty("ergonomics");
        weightProperty = serializedObject.FindProperty("weight");
        caliberProperty = serializedObject.FindProperty("caliber");
        ammoCountProperty = serializedObject.FindProperty("ammoCount");
        bulletTypesProperty = serializedObject.FindProperty("bulletTypes");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Magazine Properties", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(reloadTimeProperty);
        EditorGUILayout.PropertyField(ergonomicsProperty);
        EditorGUILayout.PropertyField(weightProperty);
        EditorGUILayout.PropertyField(caliberProperty);
        EditorGUILayout.PropertyField(ammoCountProperty);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bullet Types", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(bulletTypesProperty, true);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sample Bullets", EditorStyles.boldLabel);
        
        showSampleBullets = EditorGUILayout.Foldout(showSampleBullets, "Show Sample Magazine");
        
        if (showSampleBullets)
        {
            if (sampleBullets.Count > 0)
            {
                EditorGUILayout.HelpBox($"Sample magazine contains {sampleBullets.Count} bullets.", MessageType.Info);
                
                if (GUILayout.Button("Clear Sample Bullets"))
                {
                    sampleBullets.Clear();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Sample magazine is empty.", MessageType.Warning);
                
                if (GUILayout.Button("Fill Sample Magazine"))
                {
                    FillSampleMagazine();
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
    
    private void FillSampleMagazine()
    {
        MagazineData magazineData = (MagazineData)target;
        MagazineExtensions.FillMagazineWithDefaultBullets(magazineData, sampleBullets);
        
        // Ensure the inspector refreshes
        Repaint();
    }
} 