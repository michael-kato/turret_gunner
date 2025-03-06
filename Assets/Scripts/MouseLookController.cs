using UnityEngine;

// Handles turret and camera rotation with clamped movement
public class TurretController : MonoBehaviour
{
    public Transform turretBase; // The body of the turret, rotates with the mouse
    public Transform cameraTransform; // The camera
    public float cameraRotationSpeed = 100f; // Speed for rotating the camera with WASD
    public float mouseSensitivity = 3f; // Sensitivity for mouse-controlled turret rotation
    public float maxTurretRotationSpeed = 30f; // Maximum turret rotation speed (degrees per second)
    public float turretClamp = 45f; // Clamping range for turret rotation (degrees)
    public float cameraClamp = 15f; // Clamping range for camera X and Y rotation (degrees)

    private float turretXRotation = 0f; // Turret's vertical rotation (mouse)
    private float turretYRotation = 0f; // Turret's horizontal rotation (mouse)
    private float cameraXRotation = 0f; // Camera's X rotation (look up/down with WASD)
    private float cameraYRotation = 0f; // Camera's Y rotation (look left/right with WASD)

    void Update()
    {
        // Handle WASD input to rotate the camera
        RotateCamera();

        // Handle mouse input to rotate the turret
        RotateTurret();
    }

    private void RotateCamera()
    {
        var trans = cameraTransform.localRotation;
        
        // Get keyboard input for WASD
        float rotateX = Input.GetAxis("Horizontal"); // A/D or Left/Right rotation (Y-axis)
        float rotateY = Input.GetAxis("Vertical");   // W/S or Up/Down rotation (X-axis)

        // Apply the keyboard input to the camera's rotation variables
        cameraYRotation += rotateX * cameraRotationSpeed * Time.deltaTime;
        cameraXRotation -= rotateY * cameraRotationSpeed * Time.deltaTime;

        // Clamp the camera's X and Y rotation to ±cameraClamp
        cameraXRotation = Mathf.Clamp(cameraXRotation, -cameraClamp, cameraClamp);
        cameraYRotation = Mathf.Clamp(cameraYRotation, -cameraClamp, cameraClamp);

        // Apply the clamped rotation to the camera
        cameraTransform.localRotation = Quaternion.Euler(cameraXRotation, cameraYRotation, 0f);
    }

    private void RotateTurret()
    {
        var trans = turretBase.localRotation;
        
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Clamp the mouse input for rotation speed
        mouseX = Mathf.Clamp(mouseX, -maxTurretRotationSpeed * Time.deltaTime, maxTurretRotationSpeed * Time.deltaTime);
        mouseY = Mathf.Clamp(mouseY, -maxTurretRotationSpeed * Time.deltaTime, maxTurretRotationSpeed * Time.deltaTime);

        // Apply the mouse input to the turret rotation variables
        turretYRotation += mouseX;
        turretXRotation -= mouseY;

        // Clamp the turret's X and Y rotation to ±turretClamp
        turretXRotation = Mathf.Clamp(turretXRotation, -turretClamp, turretClamp);
        turretYRotation = Mathf.Clamp(turretYRotation, -turretClamp, turretClamp);

        // Apply the clamped rotation to the turret
        turretBase.localRotation = Quaternion.Euler(turretXRotation, turretYRotation, 0f);
    }
}