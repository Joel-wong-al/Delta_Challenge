/******************************************************************************
* File: SlidingDoor.cs
* Author: Javier, Zenon, Joel
* Created: 9 August 2025
* Description: Controls the behavior of a sliding door, including opening and closing
*              based on customer proximity.
******************************************************************************/

using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    /// <summary>
    /// Transform of the left door panel.
    /// </summary>
    public Transform leftDoor;

    /// <summary>
    /// Transform of the right door panel.
    /// </summary>
    public Transform rightDoor;

    /// <summary>
    /// Local offset to move the left door when opening.
    /// </summary>
    public Vector3 leftOpenOffset = new Vector3(-1.5f, 0, 0);

    /// <summary>
    /// Local offset to move the right door when opening.
    /// </summary>
    public Vector3 rightOpenOffset = new Vector3(1.5f, 0, 0);

    /// <summary>
    /// Speed at which the doors open and close.
    /// </summary>
    public float moveSpeed = 2f;

    // Internal state for door positions
    private Vector3 leftClosedPos;   // Closed position of the left door
    private Vector3 rightClosedPos;  // Closed position of the right door
    private Vector3 leftOpenPos;     // Open position of the left door
    private Vector3 rightOpenPos;    // Open position of the right door

    /// <summary>
    /// Radius within which customers will trigger the door to open.
    /// </summary>
    public float doorOpenRadius = 3f;

    /// <summary>
    /// True if a customer is within the trigger radius.
    /// </summary>
    private bool isCustomerInTrigger = false;

    /// <summary>
    /// Unity Start method. Initializes door positions.
    /// </summary>
    void Start()
    {
        leftClosedPos = leftDoor.localPosition;
        rightClosedPos = rightDoor.localPosition;
        leftOpenPos = leftClosedPos + leftOpenOffset;
        rightOpenPos = rightClosedPos + rightOpenOffset;
    }

    /// <summary>
    /// Unity Update method. Checks for customers and animates doors.
    /// </summary>
    void Update()
    {
        // Backup detection method using distance checking
        CheckForCustomersNearby();

        if (isCustomerInTrigger)
        {
            // Open door smoothly
            leftDoor.localPosition = Vector3.Lerp(leftDoor.localPosition, leftOpenPos, Time.deltaTime * moveSpeed);
            rightDoor.localPosition = Vector3.Lerp(rightDoor.localPosition, rightOpenPos, Time.deltaTime * moveSpeed);
        }
        else
        {
            // Close door smoothly
            leftDoor.localPosition = Vector3.Lerp(leftDoor.localPosition, leftClosedPos, Time.deltaTime * moveSpeed);
            rightDoor.localPosition = Vector3.Lerp(rightDoor.localPosition, rightClosedPos, Time.deltaTime * moveSpeed);
        }
    }

    /// <summary>
    /// Detects if any customers are within the door's open radius.
    /// Uses distance checking for compatibility with NavMeshAgent.
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

        // Only update if state changed to avoid unnecessary updates
        if (customerFound != isCustomerInTrigger)
        {
            isCustomerInTrigger = customerFound;
        }
    }

    /// <summary>
    /// Draws the detection radius in the Scene view for debugging.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isCustomerInTrigger ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, doorOpenRadius);
    }
}

