/******************************************************************************
 * File: PlayerBehaviour.cs
 * Author: Javier, Zenon, Joel
 * Created: [Insert Date]
 * Description: Handles first-person player input for monitor interaction in Delta Challenge.
 *              Only allows clicking on monitors to switch cameras; no customer interaction.
 ******************************************************************************/

using UnityEngine;


/// <summary>
/// Handles first-person player input for monitor interaction. Only allows clicking on monitors to switch cameras.
/// </summary>
public class PlayerBehaviour : MonoBehaviour
{
    /// <summary>Camera used for raycasting (assign MainCamera in Inspector).</summary>
    [SerializeField] private Camera raycastCam;
    /// <summary>Maximum distance for raycast to detect monitors.</summary>
    [SerializeField] private float maxDistance = 100f;

    /// <summary>Reference to GameManager to check pause state.</summary>
    private GameManager gameManager;

    /// <summary>
    /// Unity Start method. Finds GameManager for pause state checking.
    /// </summary>
    void Start()
    {
        // First-person player only handles monitor clicking, no customer interaction
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager not found in PlayerBehaviour!");
        }
    }

    /// <summary>
    /// Unity Update method. Handles monitor clicking if not paused.
    /// </summary>
    void Update()
    {
        // Don't process input if the game is paused
        if (gameManager != null && gameManager.IsPaused())
        {
            return;
        }

        // Only handle monitor clicking - no customer interaction in first person
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = raycastCam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxDistance))
            {
                Monitor screen = hit.collider.GetComponent<Monitor>();
                if (screen != null)
                {
                    Debug.Log("Clicked monitor for camera index: " + screen.cameraIndex);
                    CameraSystem cameraSystem = FindFirstObjectByType<CameraSystem>();
                    if (cameraSystem != null)
                    {
                        cameraSystem.SwitchToMonitorCamera(screen.cameraIndex);
                    }
                }
            }
        }
    }
}
