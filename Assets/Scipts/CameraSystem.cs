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
using System.Collections.Generic;
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
    /// Reference to the GameManager for handling customer interactions.
    /// Found automatically at startup.
    /// </summary>
    private GameManager gameManager;

    /// <summary>
    /// Whether the player is currently viewing a monitor camera.
    /// </summary>
    private bool isViewingMonitor = false;

    /// <summary>
    /// Index of the currently active monitor camera (-1 if viewing main camera).
    /// </summary>
    private int currentMonitorIndex = -1;

    /// <summary>
    /// Dictionary to store glow objects for highlighted customers.
    /// </summary>
    private Dictionary<GameObject, GameObject> glowObjects = new Dictionary<GameObject, GameObject>();

    /// <summary>
    /// Material used for the glow effect around highlighted objects.
    /// </summary>
    [SerializeField] private Material glowMaterial;

    /// <summary>
    /// Currently highlighted object.
    /// </summary>
    private GameObject currentlyHighlighted;

    void Start()
    {
        // Find GameManager automatically
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("CameraSystem: No GameManager found in scene. Customer interactions will not work.");
        }

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

        // Always show ray when in CCTV mode
        if (isViewingMonitor)
        {
            ShowCCTVRay();
            HandleHighlighting();
        }

        // Handle raycasting when in CCTV mode
        if (isViewingMonitor && Input.GetMouseButtonDown(0))
        {
            HandleCCTVRaycast();
        }
    }

    /// <summary>
    /// Switches to a monitor camera by index. Called when player clicks on a Monitor object.
    /// </summary>
    /// <param name="monitorIndex">Index of the monitor camera to switch to</param>
    public void SwitchToMonitorCamera(int monitorIndex)
    {
        // Clear any active highlights before switching
        ClearAllHighlights();

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

        // Enable free cursor movement in CCTV view
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("Cursor unlocked for CCTV view");

        Debug.Log($"Switched to monitor camera: {monitorCameras[monitorIndex].name}");
    }

    /// <summary>
    /// Returns to the main player camera. Called when player presses F.
    /// </summary>
    public void ReturnToMainCamera()
    {
        // Clear any active highlights before switching
        ClearAllHighlights();

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

        // Restore cursor to first-person mode (locked and hidden)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("Cursor locked for first-person view");

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

    /// <summary>
    /// Continuously shows the ray from the active monitor camera following the mouse cursor.
    /// </summary>
    private void ShowCCTVRay()
    {
        if (currentMonitorIndex < 0 || currentMonitorIndex >= monitorCameras.Length)
            return;

        Camera activeCamera = monitorCameras[currentMonitorIndex];
        if (activeCamera == null) return;

        // Cast ray from the active monitor camera following mouse position
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        
        // Always draw the ray in red
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);
    }

    /// <summary>
    /// Handles raycasting from the active monitor camera when in CCTV view.
    /// Shows UI popup for customer apprehension decisions.
    /// </summary>
    private void HandleCCTVRaycast()
    {
        if (currentMonitorIndex < 0 || currentMonitorIndex >= monitorCameras.Length)
            return;

        Camera activeCamera = monitorCameras[currentMonitorIndex];
        if (activeCamera == null) return;

        // Cast ray from the active monitor camera
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            // Find the customer object - same logic as highlighting
            GameObject customerObject = null;
            GameObject hitObject = hit.collider.gameObject;
            
            // First check if the hit object itself is tagged as Customer
            if (hitObject.CompareTag("Customer"))
            {
                customerObject = hitObject;
            }
            else
            {
                // If not, check if any parent object is tagged as Customer
                Transform currentTransform = hitObject.transform.parent;
                while (currentTransform != null)
                {
                    if (currentTransform.CompareTag("Customer"))
                    {
                        customerObject = currentTransform.gameObject;
                        break;
                    }
                    currentTransform = currentTransform.parent;
                }
            }

            // If we found a customer, show the apprehension UI
            if (customerObject != null && gameManager != null)
            {
                Thief thiefScript = customerObject.GetComponent<Thief>();
                if (thiefScript != null)
                {
                    // Let GameManager handle the UI popup and decision
                    gameManager.ShowCustomerApprehensionUI(customerObject, thiefScript);
                }
            }
        }
    }

    /// <summary>
    /// Handles mouse-over highlighting for customers when in CCTV view.
    /// </summary>
    private void HandleHighlighting()
    {
        if (currentMonitorIndex < 0 || currentMonitorIndex >= monitorCameras.Length)
            return;

        Camera activeCamera = monitorCameras[currentMonitorIndex];
        if (activeCamera == null) return;

        // Cast ray from the active monitor camera
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Find the customer object - check if we hit it directly or if it's a parent
            GameObject customerObject = null;
            
            // First check if the hit object itself is tagged as Customer
            if (hitObject.CompareTag("Customer"))
            {
                customerObject = hitObject;
            }
            else
            {
                // If not, check if any parent object is tagged as Customer
                Transform currentTransform = hitObject.transform.parent;
                while (currentTransform != null)
                {
                    if (currentTransform.CompareTag("Customer"))
                    {
                        customerObject = currentTransform.gameObject;
                        break;
                    }
                    currentTransform = currentTransform.parent;
                }
            }
            
            if (customerObject != null)
            {
                // If this is a new object to highlight
                if (currentlyHighlighted != customerObject)
                {
                    // Remove highlight from previous object
                    RemoveHighlight();
                    
                    // Add highlight to new object
                    AddHighlight(customerObject);
                    currentlyHighlighted = customerObject;
                }
            }
            else
            {
                // Mouse is not over a customer, remove any existing highlight
                RemoveHighlight();
            }
        }
        else
        {
            // Ray didn't hit anything, remove any existing highlight
            RemoveHighlight();
        }
    }

    /// <summary>
    /// Adds glow effect around the specified object by creating a scaled duplicate.
    /// </summary>
    /// <param name="obj">Object to add glow effect to</param>
    private void AddHighlight(GameObject obj)
    {
        if (obj == null || glowMaterial == null) return;

        // Don't create duplicate glow if one already exists
        if (glowObjects.ContainsKey(obj)) return;

        // Create a glow object as a child of the target
        GameObject glowObject = new GameObject(obj.name + "_Glow");
        glowObject.transform.SetParent(obj.transform);
        glowObject.transform.localPosition = Vector3.zero;
        glowObject.transform.localRotation = Quaternion.identity;
        glowObject.transform.localScale = Vector3.one * 1.1f; // Slightly larger for glow effect

        // Get all renderers from the original object
        Renderer[] originalRenderers = obj.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer originalRenderer in originalRenderers)
        {
            // Skip if this renderer is on the root object (we want child renderers)
            if (originalRenderer.transform == obj.transform) continue;

            // Skip warning sign renderers - we don't want to highlight those
            if (originalRenderer.name.ToLower().Contains("warning") || 
                originalRenderer.transform.name.ToLower().Contains("warning") ||
                originalRenderer.name.ToLower().Contains("sign") ||
                originalRenderer.transform.name.ToLower().Contains("sign"))
            {
                continue;
            }

            // Create corresponding renderer on glow object
            GameObject rendererCopy = new GameObject(originalRenderer.name + "_GlowCopy");
            rendererCopy.transform.SetParent(glowObject.transform);
            
            // Copy transform relative to the glow object
            rendererCopy.transform.localPosition = obj.transform.InverseTransformPoint(originalRenderer.transform.position);
            rendererCopy.transform.localRotation = Quaternion.Inverse(obj.transform.rotation) * originalRenderer.transform.rotation;
            rendererCopy.transform.localScale = originalRenderer.transform.localScale;

            // Copy the appropriate renderer component
            if (originalRenderer is MeshRenderer meshRenderer)
            {
                MeshRenderer newMeshRenderer = rendererCopy.AddComponent<MeshRenderer>();
                MeshFilter originalMeshFilter = originalRenderer.GetComponent<MeshFilter>();
                if (originalMeshFilter != null)
                {
                    MeshFilter newMeshFilter = rendererCopy.AddComponent<MeshFilter>();
                    newMeshFilter.mesh = originalMeshFilter.mesh;
                }
                
                // Apply glow material to all material slots
                Material[] glowMaterials = new Material[meshRenderer.materials.Length];
                for (int i = 0; i < glowMaterials.Length; i++)
                {
                    glowMaterials[i] = glowMaterial;
                }
                newMeshRenderer.materials = glowMaterials;
                newMeshRenderer.enabled = true;
            }
            else if (originalRenderer is SkinnedMeshRenderer skinnedRenderer)
            {
                SkinnedMeshRenderer newSkinnedRenderer = rendererCopy.AddComponent<SkinnedMeshRenderer>();
                newSkinnedRenderer.sharedMesh = skinnedRenderer.sharedMesh;
                newSkinnedRenderer.bones = skinnedRenderer.bones;
                newSkinnedRenderer.rootBone = skinnedRenderer.rootBone;
                
                // Apply glow material to all material slots
                Material[] glowMaterials = new Material[skinnedRenderer.materials.Length];
                for (int i = 0; i < glowMaterials.Length; i++)
                {
                    glowMaterials[i] = glowMaterial;
                }
                newSkinnedRenderer.materials = glowMaterials;
                newSkinnedRenderer.enabled = true;
            }
        }

        // Store the glow object reference
        glowObjects[obj] = glowObject;
    }

    /// <summary>
    /// Removes glow effect from the currently highlighted object.
    /// </summary>
    private void RemoveHighlight()
    {
        if (currentlyHighlighted == null) return;

        // Destroy the glow object if it exists
        if (glowObjects.ContainsKey(currentlyHighlighted))
        {
            GameObject glowObject = glowObjects[currentlyHighlighted];
            if (glowObject != null)
            {
                DestroyImmediate(glowObject);
            }
            glowObjects.Remove(currentlyHighlighted);
        }

        currentlyHighlighted = null;
    }

    /// <summary>
    /// Clears all highlights when switching cameras or exiting CCTV view.
    /// </summary>
    private void ClearAllHighlights()
    {
        RemoveHighlight();
        
        // Clean up any remaining glow objects
        foreach (var kvp in glowObjects)
        {
            if (kvp.Value != null)
            {
                DestroyImmediate(kvp.Value);
            }
        }
        glowObjects.Clear();
    }
}