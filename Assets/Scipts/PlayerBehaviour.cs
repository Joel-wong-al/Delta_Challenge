using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    [SerializeField] private Camera raycastCam; // Assign this to MainCamera in Inspector
    [SerializeField] private float maxDistance = 100f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // First-person player only handles monitor clicking, no customer interaction
    }

    // Update is called once per frame
    void Update()
    {
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
