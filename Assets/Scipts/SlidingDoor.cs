using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    public Transform leftDoor;
    public Transform rightDoor;
    public Vector3 leftOpenOffset = new Vector3(-1.5f, 0, 0);
    public Vector3 rightOpenOffset = new Vector3(1.5f, 0, 0);
    public float moveSpeed = 2f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;
    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    public float doorOpenRadius = 3f; // Distance to detect customers
    private bool isCustomerInTrigger = false;

    void Start()
    {
        leftClosedPos = leftDoor.localPosition;
        rightClosedPos = rightDoor.localPosition;
        leftOpenPos = leftClosedPos + leftOpenOffset;
        rightOpenPos = rightClosedPos + rightOpenOffset;
    }

    void Update()
    {
        // Backup detection method using distance checking
        CheckForCustomersNearby();
        
        if (isCustomerInTrigger)
        {
            // Open door
            leftDoor.localPosition = Vector3.Lerp(leftDoor.localPosition, leftOpenPos, Time.deltaTime * moveSpeed);
            rightDoor.localPosition = Vector3.Lerp(rightDoor.localPosition, rightOpenPos, Time.deltaTime * moveSpeed);
        }
        else
        {
            // Close door
            leftDoor.localPosition = Vector3.Lerp(leftDoor.localPosition, leftClosedPos, Time.deltaTime * moveSpeed);
            rightDoor.localPosition = Vector3.Lerp(rightDoor.localPosition, rightClosedPos, Time.deltaTime * moveSpeed);
        }
    }

    /// <summary>
    /// Backup method to detect customers using distance checking
    /// Works better with NavMeshAgent than trigger collisions
    /// </summary>
    private void CheckForCustomersNearby()
    {
        GameObject[] customers = GameObject.FindGameObjectsWithTag("Customer");
        bool customerFound = false;
        
        foreach (GameObject customer in customers)
        {
            if (customer != null)
            {
                float distance = Vector3.Distance(transform.position, customer.transform.position);
                if (distance <= doorOpenRadius)
                {
                    customerFound = true;
                    break;
                }
            }
        }
        
        // Only update if state changed to avoid spam
        if (customerFound != isCustomerInTrigger)
        {
            isCustomerInTrigger = customerFound;
        }
    }

    /// <summary>
    /// Draw the detection radius in the Scene view for debugging
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isCustomerInTrigger ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, doorOpenRadius);
    }
}

