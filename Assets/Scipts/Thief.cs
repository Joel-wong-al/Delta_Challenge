/******************************************************************************
 * File: Thief.cs
 * Author: Javier
 * Created: [Insert Date]
 * Description: Controls the behavior of customer NPCs. Includes logic for navigating 
 *              to random shelves, showing predetermined warning signs, and determining 
 *              thief status based on warning count.
 ******************************************************************************/

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles customer NPC behavior including smart shelf navigation, warning system,
/// and thief determination based on predetermined warning counts.
/// </summary>
public class Thief : MonoBehaviour
{
    /// <summary>
    /// Reference to the NavMeshAgent for pathfinding.
    /// </summary>
    private NavMeshAgent myAgent;

    /// <summary>
    /// Array of shelf checkpoint positions to move between.
    /// </summary>
    private Transform[] shelfCheckpoints;

    /// <summary>
    /// Array of shelf landmark positions for customers to face when at shelves.
    /// </summary>
    private Transform[] shelfLandmarks;

    /// <summary>
    /// How long the customer spends at each shelf location.
    /// </summary>
    [SerializeField] private float shelfTime = 5f;

    /// <summary>
    /// Time interval between warning sign displays.
    /// </summary>
    [SerializeField] private float warningInterval = 8f;

    /// <summary>
    /// Reference to the Animator controlling customer animations.
    /// </summary>
    [SerializeField] private Animator animator;

    /// <summary>
    /// UI or 3D warning sign shown above the customer's head.
    /// </summary>
    [SerializeField] private GameObject warningSign;

    /// <summary>
    /// Total number of warning signs this customer will show during their visit.
    /// Predetermined at spawn: thieves = 3, regular customers = 0-2.
    /// </summary>
    [SerializeField] private int totalWarningsToShow;

    /// <summary>
    /// Speed of rotation when facing shelf landmarks (degrees per second).
    /// </summary>
    private float rotationSpeed = 180f;

    /// <summary>
    /// How close the customer must get to a shelf checkpoint before stopping.
    /// </summary>
    private float stopDistance = 0.5f;

    /// <summary>
    /// Current number of warning signs already displayed.
    /// </summary>
    private int currentWarningCount = 0;

    /// <summary>
    /// Whether this customer is actually a thief (3+ warnings makes them confirmed thief).
    /// </summary>
    public bool IsThief { get; private set; } = false;

    /// <summary>
    /// Whether the customer is currently displaying a warning sign.
    /// </summary>
    public bool IsShowingWarning { get; private set; } = false;

    /// <summary>
    /// Index of the current shelf checkpoint the customer is headed to.
    /// </summary>
    private int currentShelfIndex = 0;

    /// <summary>
    /// The current state of the customer ("Moving", "AtShelf", "Browsing").
    /// </summary>
    private string currentState;

    /// <summary>
    /// Timer for warning sign intervals.
    /// </summary>
    private float warningTimer = 0f;
    /// <summary>
    /// Initializes the NavMeshAgent component and sets up customer behavior.
    /// </summary>
    void Awake()
    {
        myAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogWarning($"No Animator component found on {gameObject.name}");
        }
    }

    /// <summary>
    /// Initializes customer with predetermined warning count and starts navigation.
    /// </summary>
    /// <param name="isThiefCustomer">Whether this customer should be a thief</param>
    public void Initialize(bool isThiefCustomer)
    {
        IsThief = isThiefCustomer;
        
        if (IsThief)
        {
            // Thieves always show exactly 3 warning signs
            totalWarningsToShow = 3;
            Debug.Log($"Customer {gameObject.name} initialized as THIEF - will show 3 warnings");
        }
        else
        {
            // Regular customers show 0-2 warning signs
            totalWarningsToShow = Random.Range(0, 3);
            Debug.Log($"Customer {gameObject.name} initialized as REGULAR CUSTOMER - will show {totalWarningsToShow} warnings");
        }
        
        // Find shelf checkpoints and landmarks if not assigned
        if (shelfCheckpoints == null || shelfCheckpoints.Length == 0)
        {
            GameObject[] checkpoints = GameObject.FindGameObjectsWithTag("ShelfCheckpoint");
            shelfCheckpoints = new Transform[checkpoints.Length];
            for (int i = 0; i < checkpoints.Length; i++)
            {
                shelfCheckpoints[i] = checkpoints[i].transform;
            }
        }
        
        if (shelfLandmarks == null || shelfLandmarks.Length == 0)
        {
            GameObject[] landmarks = GameObject.FindGameObjectsWithTag("ShelfLandmark");
            shelfLandmarks = new Transform[landmarks.Length];
            for (int i = 0; i < landmarks.Length; i++)
            {
                shelfLandmarks[i] = landmarks[i].transform;
            }
        }
        
        Debug.Log($"Initialize: Starting customer behavior - switching to Moving state");
        StartCoroutine(SwitchState("Moving"));
    }

    /// <summary>
    /// Starts the customer behavior with default initialization.
    /// </summary>
    void Start()
    {
        // Default initialization if not called externally
        if (totalWarningsToShow == 0 && !IsThief)
        {
            Initialize(Random.value < 0.3f); // 30% chance of being a thief
        }
    }

    /// <summary>
    /// Updates warning timer and handles warning sign display.
    /// </summary>
    void Update()
    {
        // Handle warning sign timer
        if (currentWarningCount < totalWarningsToShow)
        {
            warningTimer += Time.deltaTime;
            if (warningTimer >= warningInterval)
            {
                StartCoroutine(ShowWarningSign());
                warningTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Switches the customer's behavior state by starting the appropriate coroutine.
    /// </summary>
    /// <param name="newState">The new state to switch to.</param>
    IEnumerator SwitchState(string newState)
    {
        if (currentState == newState)
        {
            Debug.Log($"SwitchState: Already in state {newState}, ignoring");
            yield break;
        }

        Debug.Log($"SwitchState: Changing from '{currentState}' to '{newState}'");
        currentState = newState;

        if (newState == "Moving")
            StartCoroutine(Moving());
        else if (newState == "AtShelf")
            StartCoroutine(AtShelf());
        else if (newState == "Browsing")
            StartCoroutine(Browsing());
        else
            Debug.LogWarning($"SwitchState: Unknown state '{newState}'");
    }

    /// <summary>
    /// Moves the customer to a random shelf checkpoint.
    /// </summary>
    IEnumerator Moving()
    {
        if (shelfCheckpoints.Length == 0)
        {
            Debug.LogWarning("Moving: No shelf checkpoints found! Customer cannot navigate.");
            yield break;
        }

        // Check if NavMeshAgent is valid and on NavMesh
        if (myAgent == null)
        {
            Debug.LogError("Moving: NavMeshAgent is null!");
            yield break;
        }

        if (!myAgent.isOnNavMesh)
        {
            Debug.LogError($"Moving: {gameObject.name} is not on NavMesh! Make sure spawn point is on NavMesh.");
            yield break;
        }

        // Re-enable NavMeshAgent for movement
        if (!myAgent.enabled)
            myAgent.enabled = true;
            
        myAgent.isStopped = false;
        myAgent.updateRotation = true;

        // Set walking animation
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsIdle", false);
        }

        // Choose a random shelf checkpoint
        currentShelfIndex = Random.Range(0, shelfCheckpoints.Length);
        Transform destination = shelfCheckpoints[currentShelfIndex];
        
        myAgent.SetDestination(destination.position);

        // Stuck detection variables
        Vector3 lastPosition = transform.position;
        float stuckTimer = 0f;
        float stuckThreshold = 3f; // 3 seconds without movement = stuck
        float positionThreshold = 0.1f; // Minimum distance to consider as movement
        int maxRetries = 3;
        int retryCount = 0;

        while (currentState == "Moving")
        {
            if (!myAgent.pathPending)
            {
                float remainingDistance = myAgent.remainingDistance;
                
                // Check if reached destination
                if (remainingDistance <= stopDistance && remainingDistance > 0)
                {
                    Debug.Log($"Customer {gameObject.name} reached shelf {currentShelfIndex}");
                    StartCoroutine(SwitchState("AtShelf"));
                    yield break;
                }

                // Stuck detection during movement
                float distanceMoved = Vector3.Distance(transform.position, lastPosition);
                
                if (distanceMoved < positionThreshold)
                {
                    // Customer hasn't moved much, increment stuck timer
                    stuckTimer += 0.5f;
                    
                    if (stuckTimer >= stuckThreshold)
                    {
                        Debug.LogWarning($"Customer {gameObject.name} appears stuck! Attempting recovery...");
                        
                        // Try recovery methods
                        if (retryCount < maxRetries)
                        {
                            retryCount++;
                            
                            // Recovery method 1: Try a different destination
                            if (retryCount == 1)
                            {
                                currentShelfIndex = Random.Range(0, shelfCheckpoints.Length);
                                destination = shelfCheckpoints[currentShelfIndex];
                                Debug.Log($"Recovery attempt {retryCount}: Trying new destination (shelf {currentShelfIndex})");
                                myAgent.SetDestination(destination.position);
                            }
                            // Recovery method 2: Reset NavMeshAgent
                            else if (retryCount == 2)
                            {
                                Debug.Log($"Recovery attempt {retryCount}: Resetting NavMeshAgent");
                                myAgent.enabled = false;
                                yield return new WaitForSeconds(0.1f);
                                myAgent.enabled = true;
                                myAgent.SetDestination(destination.position);
                            }
                            // Recovery method 3: Teleport to destination
                            else if (retryCount == 3)
                            {
                                Debug.Log($"Recovery attempt {retryCount}: Teleporting to destination");
                                transform.position = destination.position;
                                StartCoroutine(SwitchState("AtShelf"));
                                yield break;
                            }
                            
                            stuckTimer = 0f; // Reset stuck timer after recovery attempt
                        }
                        else
                        {
                            // All recovery attempts failed, teleport to destination
                            Debug.LogError($"Customer {gameObject.name} stuck beyond recovery! Teleporting to destination.");
                            transform.position = destination.position;
                            StartCoroutine(SwitchState("AtShelf"));
                            yield break;
                        }
                    }
                }
                else
                {
                    // Customer is moving normally, reset stuck detection
                    stuckTimer = 0f;
                    retryCount = 0;
                    lastPosition = transform.position;
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// Customer arrives at shelf and faces the nearest shelf landmark.
    /// </summary>
    IEnumerator AtShelf()
    {
        // Stop the NavMeshAgent movement but keep it enabled
        if (myAgent != null)
        {
            myAgent.isStopped = true;
            myAgent.updateRotation = false;
        }

        // Set idle animation when stopping at shelf
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsIdle", true);
        }

        // Face the closest shelf landmark
        if (shelfLandmarks.Length > 0)
        {
            Transform closestLandmark = GetClosestLandmark();
            
            if (closestLandmark != null)
            {
                Vector3 lookDirection = (closestLandmark.position - transform.position).normalized;
                lookDirection.y = 0; // Keep horizontal rotation only
                
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    
                    // Smooth rotation towards the landmark
                    Quaternion startRotation = transform.rotation;
                    float rotationTime = 0f;
                    float rotationDuration = Quaternion.Angle(startRotation, targetRotation) / rotationSpeed;
                    
                    while (rotationTime < rotationDuration)
                    {
                        rotationTime += Time.deltaTime;
                        float t = rotationTime / rotationDuration;
                        transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                        yield return null;
                    }
                    
                    // Ensure we reach the exact target rotation
                    transform.rotation = targetRotation;
                }
            }
        }

        StartCoroutine(SwitchState("Browsing"));
    }

    /// <summary>
    /// Customer browses at the shelf for a period of time, then moves to next shelf.
    /// </summary>
    IEnumerator Browsing()
    {
        Debug.Log($"Customer browsing at shelf {currentShelfIndex}");
        
        // Set idle animation for browsing
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsIdle", true);
        }
        
        yield return new WaitForSeconds(shelfTime);
        
        // Move to next random shelf
        StartCoroutine(SwitchState("Moving"));
    }

    /// <summary>
    /// Displays a warning sign for a short duration.
    /// </summary>
    IEnumerator ShowWarningSign()
    {
        if (currentWarningCount >= totalWarningsToShow) yield break;

        currentWarningCount++;
        IsShowingWarning = true;
        
        if (warningSign != null)
            warningSign.SetActive(true);

        Debug.Log($"Customer {gameObject.name} showing warning {currentWarningCount}/{totalWarningsToShow}");

        yield return new WaitForSeconds(3f); // Show warning for 3 seconds

        IsShowingWarning = false;
        if (warningSign != null)
            warningSign.SetActive(false);

        // If this customer has shown 3 warnings, they are confirmed as a thief
        if (currentWarningCount >= 3)
        {
            Debug.Log($"Customer {gameObject.name} is now CONFIRMED THIEF (3+ warnings)");
        }
    }

    /// <summary>
    /// Finds the closest shelf landmark to the customer's current position.
    /// </summary>
    /// <returns>The closest shelf landmark Transform</returns>
    private Transform GetClosestLandmark()
    {
        if (shelfLandmarks.Length == 0) return null;

        Transform closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform landmark in shelfLandmarks)
        {
            if (landmark == null) continue;
            
            float distance = Vector3.Distance(transform.position, landmark.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = landmark;
            }
        }

        return closest;
    }

    /// <summary>
    /// Gets the current warning count for this customer.
    /// </summary>
    /// <returns>Number of warnings shown so far</returns>
    public int GetCurrentWarningCount()
    {
        return currentWarningCount;
    }

    /// <summary>
    /// Gets the total warnings this customer will show.
    /// </summary>
    /// <returns>Total predetermined warning count</returns>
    public int GetTotalWarningsToShow()
    {
        return totalWarningsToShow;
    }

    /// <summary>
    /// Checks if this customer is a confirmed thief (3+ warnings shown).
    /// </summary>
    /// <returns>True if customer has shown 3+ warnings</returns>
    public bool IsConfirmedThief()
    {
        return currentWarningCount >= 3;
    }
}
