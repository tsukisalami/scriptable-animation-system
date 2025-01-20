using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ScriptableObject), true)]
public class throwableDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ScriptableObject throwableData = (ScriptableObject)target;

        if (throwableData is FragGrenadeData)
        {
            // Display Frag Grenade specific properties
        }
        else if (throwableData is FlashbangGrenadeData)
        {
            // Display Flashbang specific properties
        }
        else if (throwableData is ImpactGrenadeData)
        {
            // Display Impact Grenade specific properties
        }
    }
}
