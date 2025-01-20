using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GrassPlacement))]
public class GrassPlacementEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GrassPlacement grassPlacement = (GrassPlacement)target;
        if (GUILayout.Button("Place Grass"))
        {
            grassPlacement.PlaceGrass();
        }

        if (GUILayout.Button("Clear Grass"))
        {
            grassPlacement.ClearGrass();
        }
    }
}
