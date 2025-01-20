using UnityEngine;
using GogoGaga.OptimizedRopesAndCables;

public class PowerTower : MonoBehaviour
{
    [System.Serializable]
    public class CablePoint
    {
        public Transform waypoint; // The waypoint transform
        public Rope ropeObject;    // The Rope object associated with this waypoint
    }

    public CablePoint[] cablePoints;
    public int towerIndex = -1;   // Index in the cable chain
    public bool showIndex = false; // Toggle to show/hide index

    private void OnDrawGizmos()
    {
        if (showIndex && towerIndex >= 0)
        {
            // Display the tower index as a label above the tower
            Gizmos.color = Color.white;
            Gizmos.DrawIcon(transform.position + Vector3.up * 2, "number" + towerIndex, true);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, towerIndex.ToString(), new GUIStyle { fontSize = 20, fontStyle = FontStyle.Bold, normal = new GUIStyleState { textColor = Color.yellow } });
        }
    }
}
