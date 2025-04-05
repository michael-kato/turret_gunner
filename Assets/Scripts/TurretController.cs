using Unity.VisualScripting;
using UnityEngine;

// Handles turret and camera rotation with clamped movement
public class TurretController : MonoBehaviour
{
    public Transform cameraTransform; // The camera
    public float cameraRotationSpeed = 100f; // Speed for rotating the camera with WASD
    public float mouseSensitivity = 3f; // Sensitivity for mouse-controlled turret rotation
    public float maxTurretRotationSpeed = 30f; // Maximum turret rotation speed (degrees per second)
    public float turretClamp = 45f; // Clamping range for turret rotation (degrees)
    public float cameraClamp = 15f; // Clamping range for camera X and Y rotation (degrees)
    
    private Transform _parent;
    
    void Start()
    {
        _parent = transform.parent;
    }

    
    void Update()
    {
        //transform.position = _parent.position;
        
        RotateTurret();
        RotateCamera();
    }

    private void RotateCamera()
    {
        Vector3 rots = cameraTransform.localRotation.eulerAngles;
        float cameraXRotation = rots.x; // (look up/down with WASD)
        float cameraYRotation = rots.y; // (look left/right with WASD)
        
        // Get keyboard input for WASD
        float rotateX = Input.GetAxis("Horizontal"); // A/D or Left/Right rotation (Y-axis)
        float rotateY = Input.GetAxis("Vertical");   // W/S or Up/Down rotation (X-axis)

        // Apply the keyboard input to the camera's rotation variables
        cameraYRotation += rotateX * cameraRotationSpeed * Time.deltaTime;
        cameraXRotation -= rotateY * cameraRotationSpeed * Time.deltaTime;

        // Clamp the camera's X and Y rotation to ±cameraClamp
        //cameraXRotation = Mathf.Clamp(cameraXRotation, -cameraClamp, cameraClamp);
        //cameraYRotation = Mathf.Clamp(cameraYRotation, -cameraClamp, cameraClamp);

        // Apply the clamped rotation to the camera
        cameraTransform.localRotation = Quaternion.Euler(cameraXRotation, cameraYRotation, 0f);
    }

    private void RotateTurret()
    {
        Vector3 rots = transform.localRotation.eulerAngles;

        float turretXRotation = rots.x; // Turret's vertical rotation (mouse)
        float turretYRotation = rots.y; // Turret's horizontal rotation (mouse)
        
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Clamp the mouse input for rotation speed
        mouseX = Mathf.Clamp(mouseX, -maxTurretRotationSpeed, maxTurretRotationSpeed );
        mouseY = Mathf.Clamp(mouseY, -maxTurretRotationSpeed, maxTurretRotationSpeed );

        // Apply the mouse input to the turret rotation variables
        turretYRotation += mouseX;
        turretXRotation -= mouseY;

        // Clamp the turret's X and Y rotation to ±turretClamp
        //turretXRotation = Mathf.Clamp(turretXRotation, -turretClamp, turretClamp);
        //turretYRotation = Mathf.Clamp(turretYRotation, -turretClamp, turretClamp);

        // Apply the clamped rotation to the turret
        transform.localRotation = Quaternion.Euler(turretXRotation, turretYRotation, rots.z);
    }
}