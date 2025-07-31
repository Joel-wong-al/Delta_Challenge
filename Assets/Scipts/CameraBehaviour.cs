using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
    [SerializeField] public Camera currentCam;// Reference to the current camera being used
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Make sure the cursor is visible
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

    }

    // Update is called once per frame
    void Update()
    {
        if (currentCam == null)
        {
            Debug.LogWarning("Current camera not set.");
            return;
        }
        Vector3 mousePos = Input.mousePosition; // Get the mouse position in screen coordinates
        mousePos.z = 10f; // Set a distance from the camera
        mousePos = currentCam.ScreenToWorldPoint(mousePos);// Convert to world coordinates
        Debug.DrawRay(transform.position, mousePos - transform.position, Color.blue);// Draw a ray from the player to the mouse position

        if (Input.GetMouseButtonDown(0))// Check if the left mouse button is pressed
        {
            Ray ray = currentCam.ScreenPointToRay(Input.mousePosition);//  Create a ray from the camera to the mouse position
            RaycastHit hit;// Check if the ray hits an object

            if (Physics.Raycast(ray, out hit, 100))// Check if the ray hits an object within 100 units
            {
                Debug.Log("Hit: " + hit.collider.name);
                // Add logic to interact with the object hit by the raycast
            }
        }
    }
}
