using UnityEngine;

public class GrassPlacement : MonoBehaviour
{
    public GameObject grassPrefab;
    public int grassCount = 1000;
    public float areaSize = 100f;
    public int seed = 12345;

    public void PlaceGrass()
    {
        ClearGrass();

        Random.InitState(seed);
        Vector3 center = transform.position;
        for (int i = 0; i < grassCount; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(-areaSize / 2, areaSize / 2),
                0,
                Random.Range(-areaSize / 2, areaSize / 2)
            );
            Instantiate(grassPrefab, center + position, Quaternion.identity, transform);
        }
    }

    public void ClearGrass()
    {
        // Clear existing grass
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}
