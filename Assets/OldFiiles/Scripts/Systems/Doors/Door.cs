using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    public float smooth = 2f;
    public bool isLocked;
    public bool flipOpenDirection = false;

    private Transform player;
    private Vector3 playerPosition;
    private Quaternion targetRotation;
    public int maxAngle = 90;

    private float defaultYRotation;
    private bool isOpen;
    private Vector3 forward;

    private float openAmount;
    private bool isAiming;
    private bool isPeeking;

    private Rigidbody rb;
    private int openDirection = 1;

    void Start()
    {

        player = GameObject.FindGameObjectWithTag("Player").transform;
        defaultYRotation = transform.eulerAngles.y;
        targetRotation = transform.rotation;

        forward = transform.forward; // Assuming the door's "forward" direction is its right vector

        openAmount = 1f;

        if (flipOpenDirection)
        {
            openDirection = -1;
        }
        else 
        {
            openDirection = 1;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isAiming = true;
        }
        else 
        {
            isAiming = false;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smooth * Time.deltaTime);
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        Debug.Log("Door is now " + (isOpen ? "open" : "closed"));

        if (isOpen)
        {
            float dot = Vector3.Dot(forward, (playerPosition - transform.position).normalized);

            targetRotation = Quaternion.AngleAxis(Mathf.Sign(dot) * Mathf.Sign(openDirection) * maxAngle * openAmount, Vector3.up) * transform.rotation;
        }
        else
        {
            targetRotation = Quaternion.Euler(0f, defaultYRotation, 0f);
        }
    }

    public void Interact()
    {
        playerPosition = player.position;

        if (!isLocked)
        {
            if (isAiming)
            {
                Peek();
            }
            else
            {
                ToggleDoor();
            }
        }   
        else
        {
            Debug.Log("Door is locked");
        }

    }

    public void Peek()
    {
        Debug.Log("Peeking");
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
    }


    public void Bash()
    {
        Debug.Log("Bashing door");
    }
}
