/******************************************************************************
 * File: CameraSystem.cs
 * Author: Javier
 * Created: [Insert Date]
 * Description: Manages switching between the main player camera and monitor cameras.
 *              Players can click on Monitor objects to view their respective cameras,
 *              and press ESC to return to the main camera view.
 ******************************************************************************/

using UnityEngine;
using StarterAssets;
#if CINEMACHINE_TIMELINE
using Cinemachine;
#endif

/// <summary>
/// Handles switching between the main player camera and monitor cameras.
/// Players click on Monitor objects to switch views and press ESC to return to main view.
/// </summary>
public class CameraSystem : MonoBehaviour
{
    /// <summary>
    /// The main player camera that players return to when pressing ESC.
    /// </summary>
    [SerializeField] private Camera mainCamera;

    /// <summary>
    /// Array of monitor cameras that can be switched to.
    /// </summary>
    [SerializeField] private Camera[] monitorCameras;

    /// <summary>
    /// Reference to the player GameObject to disable input components when viewing monitors.
    /// </summary>
    [SerializeField] private GameObject playerObject;

    /// <summary>
    /// Whether the player is currently viewing a monitor camera.
    /// </summary>
    private bool isViewingMonitor = false;

    /// <summary>
    /// Index of the currently active monitor camera (-1 if viewing main camera).
    /// </summary>
    private int currentMonitorIndex = -1;

    void Start()
    {
        // Ensure main camera is active at start
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
        }

        // Ensure all monitor cameras are inactive at start
        for (int i = 0; i < monitorCameras.Length; i++)
        {
            if (monitorCameras[i] != null)
            {
                monitorCameras[i].gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        // Check for F key to return to main camera
        if (Input.GetKeyDown(KeyCode.F) && isViewingMonitor)
        {
            ReturnToMainCamera();
        }
    }

    /// <summary>
    /// Switches to a monitor camera by index. Called when player clicks on a Monitor object.
    /// </summary>
    /// <param name="monitorIndex">Index of the monitor camera to switch to</param>
    public void SwitchToMonitorCamera(int monitorIndex)
    {
        // Check if the index is valid
        if (monitorIndex < 0 || monitorIndex >= monitorCameras.Length)
        {
            Debug.LogWarning($"CameraSystem: Invalid monitor index {monitorIndex}");
            return;
        }

        // Check if the camera exists
        if (monitorCameras[monitorIndex] == null)
        {
            Debug.LogWarning($"CameraSystem: Monitor camera at index {monitorIndex} is null");
            return;
        }

        // Deactivate main camera
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(false);
        }

        // Deactivate any currently active monitor camera
        if (isViewingMonitor && currentMonitorIndex >= 0 && currentMonitorIndex < monitorCameras.Length)
        {
            if (monitorCameras[currentMonitorIndex] != null)
            {
                monitorCameras[currentMonitorIndex].gameObject.SetActive(false);
            }
        }

        // Activate the selected monitor camera
        monitorCameras[monitorIndex].gameObject.SetActive(true);
        currentMonitorIndex = monitorIndex;
        isViewingMonitor = true;

        // Lock camera movement by disabling the player object
        if (playerObject != null)
        {
            Debug.Log($"Stopping player movement and disabling player object: {playerObject.name}");
            
            // Stop all movement before disabling the player
            StopPlayerMovement();
            
            // Then disable the GameObject
            playerObject.SetActive(false);
            Debug.Log("Player GameObject disabled - camera should be locked now");
        }
        else
        {
            Debug.LogError("PlayerObject reference is not set! Camera movement will not be locked.");
        }

        Debug.Log($"Switched to monitor camera: {monitorCameras[monitorIndex].name}");
    }

    /// <summary>
    /// Returns to the main player camera. Called when player presses F.
    /// </summary>
    public void ReturnToMainCamera()
    {
        // Deactivate current monitor camera if viewing one
        if (isViewingMonitor && currentMonitorIndex >= 0 && currentMonitorIndex < monitorCameras.Length)
        {
            if (monitorCameras[currentMonitorIndex] != null)
            {
                monitorCameras[currentMonitorIndex].gameObject.SetActive(false);
            }
        }

        // Activate main camera
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
        }

        // Re-enable the player object
        if (playerObject != null)
        {
            Debug.Log($"Re-enabling player object: {playerObject.name}");
            playerObject.SetActive(true);
            
            // Clear any residual input after re-enabling
            StopPlayerMovement();
            
            Debug.Log("Player GameObject re-enabled - camera should be unlocked now");
        }
        else
        {
            Debug.LogError("PlayerObject reference is not set!");
        }

        // Reset state
        isViewingMonitor = false;
        currentMonitorIndex = -1;

        Debug.Log("Returned to main camera");
    }

    /// <summary>
    /// Gets whether the player is currently viewing a monitor camera.
    /// </summary>
    /// <returns>True if viewing a monitor camera, false if viewing main camera</returns>
    public bool IsViewingMonitor()
    {
        return isViewingMonitor;
    }

    /// <summary>
    /// Gets the currently active camera.
    /// </summary>
    /// <returns>The currently active camera component</returns>
    public Camera GetCurrentCamera()
    {
        if (isViewingMonitor && currentMonitorIndex >= 0 && currentMonitorIndex < monitorCameras.Length)
        {
            return monitorCameras[currentMonitorIndex];
        }
        return mainCamera;
    }

    /// <summary>
    /// Stops all player movement by clearing input values and physics momentum.
    /// </summary>
    private void StopPlayerMovement()
    {
        if (playerObject == null) return;

        Debug.Log("Stopping player movement...");

        // Clear StarterAssets input values
        var starterInput = playerObject.GetComponent<StarterAssetsInputs>();
        if (starterInput != null)
        {
            starterInput.move = Vector2.zero;
            starterInput.look = Vector2.zero;
            starterInput.jump = false;
            starterInput.sprint = false;
            Debug.Log("Cleared StarterAssetsInputs");
        }

        // Stop CharacterController movement
        var characterController = playerObject.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.Move(Vector3.zero);
            Debug.Log("Stopped CharacterController movement");
        }

        // Stop any Rigidbody movement (if present)
        var rigidbody = playerObject.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            Debug.Log("Stopped Rigidbody movement");
        }

        Debug.Log("Player movement stopped");
    }
}