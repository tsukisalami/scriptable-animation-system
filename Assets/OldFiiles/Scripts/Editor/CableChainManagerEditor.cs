using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CableChainManager))]
public class CableChainManagerEditor : Editor
{
    private bool showGizmos = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CableChainManager manager = (CableChainManager)target;

        foreach (var chain in manager.cableChains)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Cable Chain ID: {chain.chainID}", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Generate Cables"))
            {
                manager.GenerateCables(chain);
            }

            if (GUILayout.Button("Delete Cables"))
            {
                manager.DeleteCables(chain);
            }

            if (GUILayout.Button("Flip Chain"))
            {
                manager.FlipChain(chain);
            }

            if (GUILayout.Button("Toggle Gizmos"))
            {
                ToggleTowerIndices(chain, ref showGizmos);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
    }

    private void ToggleTowerIndices(CableChainManager.CableChain chain, ref bool showGizmos)
    {
        showGizmos = !showGizmos;

        for (int i = 0; i < chain.towers.Count; i++)
        {
            var tower = chain.towers[i];
            tower.towerIndex = i + 1; // Set the index (1-based for readability)
            tower.showIndex = showGizmos; // Toggle the visibility
        }

        // Force the scene view to repaint so the changes are visible immediately
        SceneView.RepaintAll();
    }
}
