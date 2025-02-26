using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private float lookSpeed = 1f;
    [SerializeField] private bool invertYAxis = false;

    // Input sensitivity
    [SerializeField] private float horizontalSensitivity = 1.0f;
    [SerializeField] private float verticalSensitivity = 1.0f;
    
    // Cinemachine component references
    private CinemachinePanTilt panTiltComponent;
    
    private void Start()
    {
        // If no camera was assigned in the inspector, try to find one
        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        }
        
        // Set initial camera settings
        if (cinemachineCamera != null)
        {
            // Get PanTilt component for input control
            panTiltComponent = cinemachineCamera.GetComponent<CinemachinePanTilt>();
            
            if (panTiltComponent == null)
            {
                // Add PanTilt component if not found
                panTiltComponent = cinemachineCamera.gameObject.AddComponent<CinemachinePanTilt>();
                Debug.Log("Added CinemachinePanTilt component to the camera");
            }
            
            SetupCameraInputs();
        }
        else
        {
            Debug.LogError("No CinemachineCamera found in the scene!");
        }
    }

    private void Update()
    {
        if (cinemachineCamera == null || panTiltComponent == null) return;
        
        // Handle camera rotation with mouse input
        float mouseX = Input.GetAxis("Mouse X") * horizontalSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity * (invertYAxis ? -1 : 1);
        
        // Apply rotation to the camera
        if (Input.GetMouseButton(1)) // Right mouse button held down
        {
            // Update the PanTilt values directly
            float currentPan = panTiltComponent.PanAxis.Value;
            float currentTilt = panTiltComponent.TiltAxis.Value;
            
            panTiltComponent.PanAxis.Value = currentPan + (mouseX * lookSpeed);
            panTiltComponent.TiltAxis.Value = currentTilt + (mouseY * lookSpeed);
        }
    }
    
    private void SetupCameraInputs()
    {
        if (panTiltComponent == null) return;

        // Set up wrapping for pan (horizontal)
        panTiltComponent.PanAxis.Wrap = true;
        panTiltComponent.TiltAxis.Wrap = false;
        
        // Adjust follow and look at target
        if (cinemachineCamera.Follow == null || cinemachineCamera.LookAt == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Transform lookAtTarget = player.transform.Find("LookAtTarget");
                if (lookAtTarget == null)
                {
                    lookAtTarget = player.transform;
                }
                
                cinemachineCamera.Follow = player.transform;
                cinemachineCamera.LookAt = lookAtTarget;
            }
        }
    }
}