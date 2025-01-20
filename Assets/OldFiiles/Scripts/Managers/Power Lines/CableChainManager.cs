using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class CableChainManager : MonoBehaviour
{
    [System.Serializable]
    public class CableChain
    {
        public string chainID; // Unique ID for each chain
        public List<PowerTower> towers = new List<PowerTower>(); // List of towers in this chain
    }

    public List<CableChain> cableChains = new List<CableChain>();

    public void Awake()
    {
        GenerateAllCables();
    }

    private void OnValidate()
    {
        GenerateAllCables();
    }

    private void GenerateAllCables()
    {
        foreach (CableChain chain in cableChains)
        {
            GenerateCables(chain);
        }
    }

    // Generates cables for a specific cable chain
    public void GenerateCables(CableChain chain)
    {
        DeleteCables(chain);
        if (chain.towers.Count < 2) return; // Need at least two towers to form a chain

        for (int i = 0; i < chain.towers.Count - 1; i++)
        {
            PowerTower currentTower = chain.towers[i];
            PowerTower nextTower = chain.towers[i + 1];

            // Ensure both towers have the same number of cable points
            int pointsCount = Mathf.Min(currentTower.cablePoints.Length, nextTower.cablePoints.Length);

            for (int j = 0; j < pointsCount; j++)
            {
                // Set the start point of the current tower's rope to its waypoint
                currentTower.cablePoints[j].ropeObject.SetStartPoint(currentTower.cablePoints[j].waypoint, true);

                // Set the end point of the current tower's rope to the next tower's corresponding waypoint
                currentTower.cablePoints[j].ropeObject.SetEndPoint(nextTower.cablePoints[j].waypoint, true);
            }
        }
    }

    // Deletes all cables in a specific cable chain
    public void DeleteCables(CableChain chain)
    {
        foreach (var tower in chain.towers)
        {
            foreach (var cablePoint in tower.cablePoints)
            {
                if (cablePoint.ropeObject != null)
                {
                    // Optionally clear or reset the rope here
                    cablePoint.ropeObject.SetStartPoint(null);
                    cablePoint.ropeObject.SetEndPoint(null);
                }
            }
        }
    }

    // Flips the chain by reversing the order of towers in the chain
    public void FlipChain(CableChain chain)
    {
        chain.towers.Reverse();
        GenerateCables(chain); // Regenerate cables after flipping
    }
}
