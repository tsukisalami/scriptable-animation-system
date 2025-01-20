using UnityEngine;

public class Destructible : MonoBehaviour
{
    public GameObject destroyedPrefab; // The destroyed prefab of the same object

    public void Destroy()
    {
        if (destroyedPrefab!= null)
        {
            Instantiate(destroyedPrefab, transform.position, transform.rotation);
            Destroy(gameObject);
        }
        else 
        {
            Debug.LogError("Destroyed prefab is not assigned for this object");
        }

    }

}

