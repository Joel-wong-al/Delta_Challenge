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
    [SerializeField] private Transform[] shelfCheckpoints;

    /// <summary>
    /// Array of shelf landmark positions for customers to face when at shelves.
    /// </summary>
    [SerializeField] private Transform[] shelfLandmarks;

    /// <summary>
    /// How close the customer must get to a shelf checkpoint before stopping.
    /// Increased to handle navigation mesh issues and unreachable exact positions.
    /// </summary>
    [SerializeField] private float stopDistance = 2.5f;

    /// <summary>
    /// Speed of rotation when facing shelf landmarks (degrees per second).
    /// </summary>
    [SerializeField] private float rotationSpeed = 90f;

    /// <summary>
    /// How long the customer spends at each shelf location.
    /// </summary>
    [SerializeField] private float shelfTime = 5f;

    /// <summary>
    /// Time interval between warning sign displays.
    /// </summary>
    [SerializeField] private float warningInterval = 8f;

    /// <summary>
    /// Rotation offset to compensate for character model facing direction.
    /// Adjust this if your character model doesn't face forward by default.
    /// Common values: 0° (facing +Z), 90° (facing +X), 180° (facing -Z), 270° (facing -X)
    /// </summary>
    [SerializeField] private float characterFacingOffset = 0f;

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
            Debug.Log($"Initialize: Found {checkpoints.Length} shelf checkpoints");
            shelfCheckpoints = new Transform[checkpoints.Length];
            for (int i = 0; i < checkpoints.Length; i++)
            {
                shelfCheckpoints[i] = checkpoints[i].transform;
                Debug.Log($"Initialize: Checkpoint {i}: {checkpoints[i].name} at {checkpoints[i].transform.position}");
            }
        }
        
        if (shelfLandmarks == null || shelfLandmarks.Length == 0)
        {
            GameObject[] landmarks = GameObject.FindGameObjectsWithTag("ShelfLandmark");
            Debug.Log($"Initialize: Found {landmarks.Length} shelf landmarks");
            
            if (landmarks.Length == 0)
            {
                Debug.LogWarning("Initialize: NO SHELF LANDMARKS FOUND! Make sure you have GameObjects tagged 'ShelfLandmark'");
                
                // Try to find objects with ShelfSystem script that should be landmarks
                ShelfSystem[] shelfSystems = FindObjectsOfType<ShelfSystem>();
                Debug.Log($"Initialize: Found {shelfSystems.Length} ShelfSystem components");
                
                int landmarkCount = 0;
                foreach (ShelfSystem system in shelfSystems)
                {
                    // Check the ShelfSystem's settings
                    Debug.Log($"Initialize: ShelfSystem on {system.gameObject.name} - isLandmark check needed");
                    if (system.gameObject.CompareTag("ShelfLandmark"))
                    {
                        landmarkCount++;
                        Debug.Log($"Initialize: {system.gameObject.name} is properly tagged as ShelfLandmark");
                    }
                    else
                    {
                        Debug.Log($"Initialize: {system.gameObject.name} tag is '{system.gameObject.tag}' (should be 'ShelfLandmark' for landmarks)");
                    }
                }
                
                Debug.Log($"Initialize: Found {landmarkCount} properly tagged ShelfLandmark objects");
            }
            
            shelfLandmarks = new Transform[landmarks.Length];
            for (int i = 0; i < landmarks.Length; i++)
            {
                shelfLandmarks[i] = landmarks[i].transform;
                Debug.Log($"Initialize: Landmark {i}: {landmarks[i].name} at {landmarks[i].transform.position}");
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
        Debug.Log("Moving: Entered Moving state");
        
        if (shelfCheckpoints.Length == 0)
        {
            Debug.LogWarning("Moving: No shelf checkpoints found! Customer cannot navigate.");
            yield break;
        }

        // Re-enable NavMeshAgent for movement
        if (myAgent != null)
        {
            myAgent.enabled = true; // Re-enable if it was disabled
            myAgent.isStopped = false;
            myAgent.updateRotation = true; // Let agent handle rotation during movement
            Debug.Log("Moving: NavMeshAgent re-enabled for movement");
        }
        else
        {
            Debug.LogError("Moving: NavMeshAgent is null!");
            yield break;
        }

        // Set walking animation
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsIdle", false);
        }

        // Choose a random shelf checkpoint
        currentShelfIndex = Random.Range(0, shelfCheckpoints.Length);
        Transform destination = shelfCheckpoints[currentShelfIndex];
        
        Debug.Log($"Moving: Selected checkpoint {currentShelfIndex}: {destination.name} at {destination.position}");
        Debug.Log($"Moving: Customer current position: {transform.position}");
        
        myAgent.SetDestination(destination.position);
        if (animator != null)
            animator.SetBool("isWalking", true);

        Debug.Log($"Moving: Destination set, agent pathfinding...");

        while (currentState == "Moving")
        {
            if (myAgent.pathPending)
            {
                Debug.Log("Moving: Path still pending...");
            }
            else
            {
                float remainingDistance = myAgent.remainingDistance;
                float directDistance = Vector3.Distance(transform.position, destination.position);
                
                Debug.Log($"Moving: NavMesh distance: {remainingDistance}, Direct distance: {directDistance}, Stop distance: {stopDistance}");
                
                // Check both NavMesh distance and direct distance to handle navigation issues
                bool reachedByNavMesh = remainingDistance <= stopDistance && remainingDistance > 0;
                bool reachedByDistance = directDistance <= stopDistance;
                bool navMeshStuck = remainingDistance <= 0.1f && remainingDistance > 0; // Very close but not exactly zero
                
                if (reachedByNavMesh || reachedByDistance || navMeshStuck)
                {
                    Debug.Log($"Moving: Reached destination! (NavMesh: {reachedByNavMesh}, Direct: {reachedByDistance}, Stuck: {navMeshStuck})");
                    if (animator != null)
                        animator.SetBool("isWalking", false);
                    
                    StartCoroutine(SwitchState("AtShelf"));
                    yield break;
                }
                
                // Additional safety check - if customer hasn't moved much in a while, consider them "arrived"
                // This handles cases where NavMesh gets confused
                if (myAgent.velocity.magnitude < 0.1f && !myAgent.pathPending)
                {
                    Debug.Log($"Moving: Customer appears stuck (low velocity), considering arrival. Distance: {directDistance}");
                    if (directDistance <= stopDistance * 2f) // Double the normal range for stuck customers
                    {
                        Debug.Log("Moving: Stuck customer close enough - switching to AtShelf");
                        if (animator != null)
                            animator.SetBool("isWalking", false);
                        
                        StartCoroutine(SwitchState("AtShelf"));
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds instead of every frame
        }
    }

    /// <summary>
    /// Customer arrives at shelf and faces the nearest shelf landmark.
    /// </summary>
    IEnumerator AtShelf()
    {
        // Stop the NavMeshAgent completely
        if (myAgent != null)
        {
            myAgent.isStopped = true;
            myAgent.updateRotation = false;
            myAgent.enabled = false; // Completely disable to prevent interference
        }

        // Set idle animation when stopping at shelf
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsIdle", true);
        }

        // Wait a frame to ensure agent is fully stopped
        yield return null;

        // Face the closest shelf landmark
        if (shelfLandmarks.Length > 0)
        {
            Transform closestLandmark = GetClosestLandmark();
            
            if (closestLandmark != null)
            {
                Vector3 lookDirection = (closestLandmark.position - transform.position).normalized;
                lookDirection.y = 0; // Keep horizontal rotation only
                
                if (lookDirection != Vector3.zero && lookDirection.magnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    
                    // Apply character facing offset to compensate for model orientation
                    targetRotation *= Quaternion.Euler(0, characterFacingOffset, 0);
                    
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
