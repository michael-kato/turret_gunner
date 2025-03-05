using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f; // Mouse sensitivity, adjustable in the inspector
    public Transform turretBody;         // Reference to the player body (or the object the camera is attached to)
    
    private float xRotation = 0f;
    private float yRotation = 0f;
    
    void Start()
    {
        // Lock the cursor to the center of the screen and make it invisible
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Adjust the xRotation (vertical) to prevent over-rotation
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -45f, 45f); // Clamp to prevent flipping
        
        // Adjust the xRotation (vertical) to prevent over-rotation
        yRotation -= mouseX;
        yRotation = Mathf.Clamp(yRotation, -45f, 45f); // Clamp to prevent flipping

        // Rotate the camera up and down (vertical rotation)
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        
        turretBody.Rotate(Vector3.up * mouseX);
    }
}