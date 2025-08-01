/******************************************************************************
 * File: CameraSystem.cs
 * Author: Javier
 * Created: [Insert Date]
 * Description: Manages toggling between a set of CCTV-style cameras and the 
 *              main player camera. Handles switching, cooldowns, and updates
 *              the currently active camera for use in raycasting and interaction.
 ******************************************************************************/

using UnityEngine;

/// <summary>
/// Handles a multi-camera system for gameplay monitoring.
/// Supports toggling between CCTV cameras and switching with cooldown logic.
/// Updates the CameraBehaviour to ensure accurate raycasting from the active camera.
/// </summary>
public class CameraSystem : MonoBehaviour
{
    /// <summary>
    /// Array of CCTV cameras available for switching.
    /// </summary>
    [SerializeField] private GameObject[] Cameras;

    /// <summary>
    /// Index of the currently active CCTV camera.
    /// </summary>
    [SerializeField] private int CurrentCameraIndex;

    /// <summary>
    /// Key used to toggle camera view (on/off).
    /// </summary>
    [SerializeField] private KeyCode OpenCameras;

    /// <summary>
    /// Whether the CCTV system is currently open.
    /// </summary>
    [SerializeField] private bool CamerasOpen;

    /// <summary>
    /// The main player camera to return to when not viewing CCTV.
    /// </summary>
    [SerializeField] private GameObject MainCamera;

    /// <summary>
    /// Timer used to throttle how fast the player can switch between cameras.
    /// </summary>
    [SerializeField] private float CoolDownTimer;

    /// <summary>
    /// The time to wait between allowed camera switches.
    /// </summary>
    [SerializeField] private float CoolDownTime = 0.5f;

    /// <summary>
    /// Reference to the CameraBehaviour script that handles raycasting from the current camera.
    /// </summary>
    [SerializeField] private CameraBehaviour cameraBehaviour;

    /// <summary>
    /// Initializes camera states by disabling all CCTV cameras and enabling the main player camera.
    /// </summary>
    void Start()
    {
        for (int i = 0; i < Cameras.Length; i++)
        {
            Cameras[i].SetActive(false); // Initially deactivate all cameras
        }
        MainCamera.SetActive(true); // Start with the main camera enabled
    }

    /// <summary>
    /// Handles input for toggling the CCTV system and switching between cameras using horizontal input.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(OpenCameras))
        {
            CamerasOpen = !CamerasOpen;
            ShowCamera();
        }

        if (CoolDownTimer > 0)
        {
            if (Input.GetAxis("Horizontal") > 0)
            {
                Cameras[CurrentCameraIndex].SetActive(false);
                CurrentCameraIndex++;
                if (CurrentCameraIndex >= Cameras.Length)
                {
                    CurrentCameraIndex = 0; // Loop back to the first camera
                }
                GotoCamera(CurrentCameraIndex);
                CoolDownTimer = CoolDownTime;
            }
            else if (Input.GetAxis("Horizontal") < 0)
            {
                Cameras[CurrentCameraIndex].SetActive(false);
                CurrentCameraIndex--;
                if (CurrentCameraIndex < 0)
                {
                    CurrentCameraIndex = Cameras.Length - 1; // Loop back to the last camera
                }
                GotoCamera(CurrentCameraIndex);
                CoolDownTimer = CoolDownTime;
            }
        }
        else
        {
            CoolDownTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Activates the currently selected CCTV camera or returns to the main camera.
    /// </summary>
    private void ShowCamera()
    {
        if (CamerasOpen)
        {
            Cameras[CurrentCameraIndex].SetActive(true);
            MainCamera.SetActive(false);
        }
        else
        {
            Cameras[CurrentCameraIndex].SetActive(false);
            MainCamera.SetActive(true); // Show the main camera when cameras are closed
        }
    }

    /// <summary>
    /// Switches to a new camera based on index and updates CameraBehaviour to match.
    /// </summary>
    /// <param name="Progression">The index of the next camera to activate.</param>
    private void GotoCamera(int Progression)
    {
        Cameras[CurrentCameraIndex].SetActive(false);
        CurrentCameraIndex = Progression;
        ShowCamera();

        // Update currentCam in CameraBehaviour so raycasting works correctly
        Camera camComponent = Cameras[CurrentCameraIndex].GetComponent<Camera>();
        if (camComponent != null && cameraBehaviour != null)
        {
            cameraBehaviour.currentCam = camComponent;
        }
    }

    /// <summary>
    /// Activates a specific camera from the Cameras array using its index.
    /// Called by PlayerInteraction when a screen is clicked.
    /// </summary>
    /// <param name="index">Index of the camera to activate</param>
    public void ActivateCameraByIndex(int index)
    {
        if (index >= 0 && index < Cameras.Length)
        {
            Cameras[CurrentCameraIndex].SetActive(false);
            CurrentCameraIndex = index;
            Cameras[CurrentCameraIndex].SetActive(true);
            MainCamera.SetActive(false);
            CamerasOpen = true;

            Camera camComponent = Cameras[CurrentCameraIndex].GetComponent<Camera>();
            if (camComponent != null && cameraBehaviour != null)
            {
                cameraBehaviour.currentCam = camComponent;
            }
        }
    }
}