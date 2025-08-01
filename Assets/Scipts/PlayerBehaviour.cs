using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{

    Camera cam;// Reference to the main camera
    [SerializeField] private Camera raycastCam; // Assign this to MainCamera in Inspector
    [SerializeField] private float maxDistance = 100f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;// Get the main camera in the scene
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mousePos = Input.mousePosition; // Get the mouse position in screen coordinates
        mousePos.z = 10f; // Set a distance from the camera
        mousePos = cam.ScreenToWorldPoint(mousePos);// Convert to world coordinates
        Debug.DrawRay(transform.position, mousePos - transform.position, Color.blue);// Draw a ray from the player to the mouse position

        if (Input.GetMouseButtonDown(0))// Check if the left mouse button is pressed
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);//  Create a ray from the camera to the mouse position
            RaycastHit hit;// Check if the ray hits an object

            if (Physics.Raycast(ray, out hit, 100))// Check if the ray hits an object within 100 units
            {
                if (hit.collider.CompareTag("Thief")) // Check if the object hit is tagged as "Thief"
                {
                    Thief thiefScript = hit.collider.GetComponent<Thief>();
                    if (thiefScript != null && thiefScript.IsStealing)
                    {
                        Debug.Log("Caught thief!");
                        Destroy(hit.collider.gameObject);
                    }
                }
            }
        }
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
                    CameraSystem cameraSystem = FindObjectOfType<CameraSystem>();
                    if (cameraSystem != null)
                    {
                        cameraSystem.SwitchToMonitorCamera(screen.cameraIndex);
                    }
                }
            }
        }
    }
}
