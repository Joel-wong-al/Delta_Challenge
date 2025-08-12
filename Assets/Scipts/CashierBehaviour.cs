using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CashierBehaviour : MonoBehaviour
{
    [Header("Cashier Settings")]
    [SerializeField] private Transform startPosition; // Optional: where cashier returns to
    [SerializeField] private float apprehendRange = 1.2f; // Distance at which cashier apprehends customer
    
    [Header("Animation")]
    [SerializeField] private Animator animator; // Optional: for walking animations
    
    // Runtime variables
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isMoving = false;
    private GameManager gameManager;
    private NavMeshAgent navAgent;
    private Queue<(GameObject, Thief)> apprehensionQueue = new Queue<(GameObject, Thief)>();
    
    void Start()
    {
        // Store original position
        if (startPosition != null)
        {
            originalPosition = startPosition.position;
            originalRotation = startPosition.rotation;
            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }
        else
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
        }
        
        // Find GameManager reference
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("CashierBehaviour: GameManager not found!");
        }
        
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            Debug.LogError("CashierBehaviour: No NavMeshAgent found! Please add one to the cashier GameObject.");
        }
        else
        {
            navAgent.stoppingDistance = 0.1f;
            navAgent.updateRotation = true;
        }
    }
    
    /// <summary>
    /// Move cashier to apprehend a specific customer
    /// </summary>
    public void MoveToApprehendCustomer(GameObject customer, Thief thief)
    {
        if (customer == null || thief == null)
        {
            Debug.LogWarning("CashierBehaviour: Cannot start apprehension - invalid parameters");
            return;
        }
        apprehensionQueue.Enqueue((customer, thief));
        if (!isMoving)
        {
            StartCoroutine(ProcessApprehensionQueue());
        }
    }
    
    private IEnumerator ProcessApprehensionQueue()
    {
        while (apprehensionQueue.Count > 0)
        {
            var (customer, thief) = apprehensionQueue.Dequeue();
            yield return StartCoroutine(ApprehendCustomerCoroutine(customer, thief));
        }
    }
    
    /// <summary>
    /// Coroutine to handle the full apprehension process
    /// </summary>
    private IEnumerator ApprehendCustomerCoroutine(GameObject customer, Thief thief)
    {
        if (customer == null || thief == null)
            yield break;
            
        isMoving = true;
        
        // Store apprehension data before moving (in case customer gets destroyed)
        bool isCorrectDecision = thief.IsConfirmedThief();
        bool isActualThief = thief.IsThief;
        int warningCount = thief.GetCurrentWarningCount();
        int thievesSpawnedToday = gameManager != null ? gameManager.GetThievesSpawnedToday() : 0;
        
        // Start walking animation if animator exists
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsIdle", false);
        }
        
        // Move to customer (real-time follow)
        yield return StartCoroutine(MoveToCustomer(customer));
        
        // Stop walking animation
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsIdle", true);
        }
        
        // Process the apprehension if customer still exists
        if (customer != null && gameManager != null)
        {
            gameManager.ProcessApprehension(customer, thief, isCorrectDecision, isActualThief, warningCount, thievesSpawnedToday);
        }
        
        // Wait a brief moment at the customer
        yield return new WaitForSeconds(0.5f);
        
        // Start walking animation for return journey
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsIdle", false);
        }
        
        // Return to original position
        yield return StartCoroutine(MoveToPosition(originalPosition));

        // Restore original rotation
        transform.rotation = originalRotation;

        // Stop walking animation
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsIdle", true);
        }

        isMoving = false;
    }
    
    /// <summary>
    /// Move smoothly to a target position
    /// </summary>
    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        if (navAgent == null)
        {
            Debug.LogError("CashierBehaviour: No NavMeshAgent found! Cannot move cashier.");
            yield break;
        }
        navAgent.isStopped = false;
        navAgent.SetDestination(targetPosition);
        while (Vector3.Distance(transform.position, targetPosition) > navAgent.stoppingDistance + 0.05f)
        {
            // Face movement direction
            Vector3 lookDirection = navAgent.velocity;
            lookDirection.y = 0;
            if (lookDirection.magnitude > 0.1f)
                transform.rotation = Quaternion.LookRotation(lookDirection);
            yield return null;
        }
        navAgent.isStopped = true;
        navAgent.ResetPath();
        // Snap to target position for precision
        transform.position = targetPosition;
    }
    
    /// <summary>
    /// Replace MoveToPosition for customer with a real-time follow
    /// </summary>
    private IEnumerator MoveToCustomer(GameObject customer)
    {
        if (navAgent == null || customer == null)
            yield break;
        navAgent.isStopped = false;
        while (customer != null && Vector3.Distance(transform.position, customer.transform.position) > apprehendRange)
        {
            navAgent.SetDestination(customer.transform.position);
            // Face movement direction
            Vector3 lookDirection = navAgent.velocity;
            lookDirection.y = 0;
            if (lookDirection.magnitude > 0.1f)
                transform.rotation = Quaternion.LookRotation(lookDirection);
            yield return null;
        }
        navAgent.isStopped = true;
        navAgent.ResetPath();
    }
    
    /// <summary>
    /// Check if cashier is currently moving
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }
    
    /// <summary>
    /// Get the cashier's original/home position
    /// </summary>
    public Vector3 GetOriginalPosition()
    {
        return originalPosition;
    }
    
    /// <summary>
    /// Set a new home position for the cashier
    /// </summary>
    public void SetOriginalPosition(Vector3 newPosition)
    {
        originalPosition = newPosition;
    }
    
    /// <summary>
    /// Immediately return cashier to original position (no animation)
    /// </summary>
    public void ReturnToOriginalPosition()
    {
        if (!isMoving)
        {
            transform.position = originalPosition;
            transform.rotation = Quaternion.identity;
        }
    }
}
