using UnityEngine;

public class GrassSpawner : MonoBehaviour
{
    public GameObject grassPrefab; // Assign your grass blade prefab here
    public int rows = 10;
    public int columns = 10;
    public float spacing = 1.0f;

    void Start()
    {
        SpawnGrass();
    }

    void SpawnGrass()
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                Vector3 position = new Vector3(i * spacing, 0, j * spacing);
                Instantiate(grassPrefab, position, Quaternion.identity);
            }
        }
    }
}
