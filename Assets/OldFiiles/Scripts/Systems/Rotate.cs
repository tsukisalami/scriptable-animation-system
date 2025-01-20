using UnityEngine;

public class Rotate : MonoBehaviour
{
    // Enum to select axis of rotation
    public enum RotationAxis { X, Y, Z }

    [Header("Rotation Settings")]
    public RotationAxis rotationAxis = RotationAxis.Y; // Default rotation axis
    public float rotationSpeed = 100.0f; // Default rotation speed
    public bool isRotating = true; // Toggle rotation on/off

    private Vector3 rotationVector;

    void Start()
    {
        // Initialize the rotation vector based on the selected axis
        SetRotationVector();
    }

    void Update()
    {
        if (isRotating)
        {
            RotateObject();
        }
    }

    void SetRotationVector()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
                rotationVector = Vector3.right;
                break;
            case RotationAxis.Y:
                rotationVector = Vector3.up;
                break;
            case RotationAxis.Z:
                rotationVector = Vector3.forward;
                break;
        }
    }

    void RotateObject()
    {
        transform.Rotate(rotationVector * rotationSpeed * Time.deltaTime);
    }

    // Public method to change the rotation axis at runtime
    public void SetRotationAxis(RotationAxis newAxis)
    {
        rotationAxis = newAxis;
        SetRotationVector();
    }

    // Public method to change the rotation speed at runtime
    public void SetRotationSpeed(float newSpeed)
    {
        rotationSpeed = newSpeed;
    }

    // Public method to toggle rotation on/off at runtime
    public void ToggleRotation(bool toggle)
    {
        isRotating = toggle;
    }
}
