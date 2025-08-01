/******************************************************************************
* File: CameraBehaviour.cs
* Author: Javier
* Created: [Insert Date]
* Description: Handles raycasting from the currently active camera in a 
*              multi-camera surveillance system. Supports mouse interaction
*              detection and debug visualization for gameplay purposes.
******************************************************************************/

using UnityEngine;

/// <summary>
/// Controls player interaction using raycasting from the currently active camera.
/// Used primarily in a CCTV-style camera system where cameras are switched dynamically.
/// </summary>
public class CameraBehaviour : MonoBehaviour
{
    /// <summary>
    /// The camera currently being used for raycasting.
    /// This is set dynamically by the CameraSystem when the camera is switched.
    /// </summary>
    [SerializeField] public Camera currentCam;

    /// <summary>
    /// Initializes the script by unlocking and showing the cursor.
    /// </summary>
    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Handles raycasting from the current camera each frame.
    /// Draws a debug ray from the player's position to the mouse cursor.
    /// Detects mouse clicks on objects in the scene.
    /// </summary>
    void Update()
    {
        if (currentCam == null)
        {
            Debug.LogWarning("Current camera not set.");
            return;
        }

        // Convert mouse screen position to world space
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        mousePos = currentCam.ScreenToWorldPoint(mousePos);

        // Draw debug ray from this object's position to the mouse position in world space
        Debug.DrawRay(transform.position, mousePos - transform.position, Color.blue);

        // On left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            // Create a ray from the camera to the mouse cursor
            Ray ray = currentCam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Check if the ray hits a collider within 100 units
            if (Physics.Raycast(ray, out hit, 100))
            {
                Debug.Log("Hit: " + hit.collider.name);
                // TODO: Add object interaction logic here
            }
        }
    }
}
