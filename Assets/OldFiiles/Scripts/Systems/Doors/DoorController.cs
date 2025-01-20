using UnityEngine;
using TMPro;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Transform Camera;
    [SerializeField] private float MaxUseDistance = 5f;
    [SerializeField] private LayerMask UseLayers;
    public Ray ray;


    private void Update()
    {
        if (Physics.Raycast(Camera.position, Camera.forward, out RaycastHit hit, MaxUseDistance, UseLayers)
            && hit.collider.TryGetComponent<Door>(out Door door))
        {
            if (door.isLocked)
            {
                Debug.Log("Door is locked");
            }
            else
            {   
                Debug.Log("Door is unlocked");
            }
        }
    }

    public void OnUse()
    {
        if (Physics.Raycast(Camera.position, Camera.forward, out RaycastHit hit, MaxUseDistance, UseLayers))
        {
            if (hit.collider.TryGetComponent<Door>(out Door door))
            {   
                door.Interact();
                Debug.Log("Door is toggled");
            }

            if (hit.collider.TryGetComponent<Generator>(out Generator generator))
            {
                generator.Interact();
                Debug.Log("Generator is toggled");
            }
        }
    }

    public void OnBash()
    {
        if (Physics.Raycast(Camera.position, Camera.forward, out RaycastHit hit, MaxUseDistance, UseLayers)
        && hit.collider.TryGetComponent<Door>(out Door door))
        {
            door.Bash();
        }
    
    }
 
}