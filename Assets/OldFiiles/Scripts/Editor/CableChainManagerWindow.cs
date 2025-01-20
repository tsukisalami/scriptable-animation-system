using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CableChainManagerWindow : EditorWindow
{
    private CableChainManager chainManager;
    private string newChainID = "NewChain";
    private CableChainManager.CableChain selectedChain;
    
    [MenuItem("Tools/Cable Chain Manager")]
    public static void ShowWindow()
    {
        GetWindow<CableChainManagerWindow>("Cable Chain Manager");
    }

    private void OnGUI()
    {
        GUILayout.Label("Cable Chain Manager", EditorStyles.boldLabel);

        chainManager = (CableChainManager)EditorGUILayout.ObjectField("Chain Manager", chainManager, typeof(CableChainManager), true);

        if (chainManager == null)
        {
            EditorGUILayout.HelpBox("Please assign a CableChainManager.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();

        GUILayout.Label("Create New Chain", EditorStyles.boldLabel);
        newChainID = EditorGUILayout.TextField("Chain ID", newChainID);

        if (GUILayout.Button("Create Chain"))
        {
            CreateNewChain();
        }

        EditorGUILayout.Space();

        GUILayout.Label("Existing Chains", EditorStyles.boldLabel);

        foreach (var chain in chainManager.cableChains)
        {
            if (GUILayout.Button("Select " + chain.chainID))
            {
                selectedChain = chain;
            }
        }

        if (selectedChain != null)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Selected Chain: " + selectedChain.chainID, EditorStyles.boldLabel);

            if (GUILayout.Button("Add Selected Towers"))
            {
                AddSelectedTowersToChain();
            }

            if (GUILayout.Button("Generate Cables"))
            {
                chainManager.GenerateCables(selectedChain);
            }

            if (GUILayout.Button("Delete Cables"))
            {
                chainManager.DeleteCables(selectedChain);
            }

            if (GUILayout.Button("Flip Chain"))
            {
                chainManager.FlipChain(selectedChain);
            }
        }
    }

    private void CreateNewChain()
    {
        if (chainManager != null)
        {
            var newChain = new CableChainManager.CableChain
            {
                chainID = newChainID
            };
            chainManager.cableChains.Add(newChain);
            EditorUtility.SetDirty(chainManager);
        }
    }

    private void AddSelectedTowersToChain()
    {
        if (selectedChain != null)
        {
            var selectedObjects = Selection.gameObjects;

            foreach (var obj in selectedObjects)
            {
                var tower = obj.GetComponent<PowerTower>();
                if (tower != null && !selectedChain.towers.Contains(tower))
                {
                    selectedChain.towers.Add(tower);
                }
            }

            selectedChain.towers.Sort((a, b) => Vector3.Distance(chainManager.transform.position, a.transform.position)
                .CompareTo(Vector3.Distance(chainManager.transform.position, b.transform.position)));

            EditorUtility.SetDirty(chainManager);
        }
    }
}
