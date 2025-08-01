/******************************************************************************
 * File: Thief.cs
 * Author: Javier
 * Created: [Insert Date]
 * Description: Controls the behavior of a thief NPC. Includes logic for patrolling 
 *              between rest points, randomly deciding to steal, triggering 
 *              animations, and exposing a flag for player detection.
 ******************************************************************************/

using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Handles thief NPC behavior including patrolling, stealing decisions,
/// and interaction with the player's raycast system.
/// </summary>
public class Thief : MonoBehaviour
{
    /// <summary>
    /// Reference to the NavMeshAgent for pathfinding.
    /// </summary>
    private NavMeshAgent myAgent;

    /// <summary>
    /// Patrol points the thief will move between.
    /// </summary>
    [SerializeField] private Transform[] restPoints;

    /// <summary>
    /// How close the thief must get to a rest point before stopping.
    /// </summary>
    [SerializeField] private float stopDistance = 0.5f;

    /// <summary>
    /// How long the thief waits in Idle before moving again.
    /// </summary>
    [SerializeField] private float pauseTime = 3f;

    /// <summary>
    /// Reference to the Animator controlling thief animations.
    /// </summary>
    [SerializeField] private Animator animator;

    /// <summary>
    /// UI or 3D sign shown above the thief's head while stealing.
    /// </summary>
    [SerializeField] private GameObject stealingSign;

    /// <summary>
    /// Flag that indicates whether the thief is currently stealing.
    /// Used by the player raycasting system to determine if thief can be caught.
    /// </summary>
    public bool IsStealing { get; private set; } = false;

    /// <summary>
    /// Index of the current patrol point the thief is headed to.
    /// </summary>
    private int currentPoint = 0;

    /// <summary>
    /// The current state of the thief ("Idle", "Moving", "Stealing").
    /// </summary>
    private string currentState;

    /// <summary>
    /// Initializes the NavMeshAgent component.
    /// </summary>
    void Awake()
    {
        myAgent = GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// Starts the FSM by entering the Idle state.
    /// </summary>
    void Start()
    {
        StartCoroutine(SwitchState("Idle"));
    }

    /// <summary>
    /// Switches the thief's behavior state by starting the appropriate coroutine.
    /// </summary>
    /// <param name="newState">The new state to switch to.</param>
    IEnumerator SwitchState(string newState)
    {
        if (currentState == newState)
            yield break;

        currentState = newState;

        if (newState == "Idle")
            StartCoroutine(Idle());
        else if (newState == "Moving")
            StartCoroutine(Moving());
        else if (newState == "Stealing")
            StartCoroutine(Stealing());
    }

    /// <summary>
    /// Waits in place for a short period before resuming patrol.
    /// </summary>
    IEnumerator Idle()
    {
        yield return new WaitForSeconds(pauseTime);
        StartCoroutine(SwitchState("Moving"));
    }

    /// <summary>
    /// Moves the thief to the next rest point and randomly decides whether to steal.
    /// </summary>
    IEnumerator Moving()
    {
        if (restPoints.Length == 0)
            yield break;

        Transform destination = restPoints[currentPoint];
        myAgent.SetDestination(destination.position);
        animator.SetBool("isWalking", true);

        while (currentState == "Moving")
        {
            if (!myAgent.pathPending && myAgent.remainingDistance <= stopDistance)
            {
                animator.SetBool("isWalking", false);
                bool willSteal = Random.value < 0.4f;

                if (willSteal)
                {
                    Debug.Log("Stealing from shelf");
                    StartCoroutine(SwitchState("Stealing"));
                }
                else
                {
                    StartCoroutine(SwitchState("Idle"));
                }
                yield break; // Prevent further movement processing
            }

            yield return null;
        }
    }

    /// <summary>
    /// Triggers the stealing animation and sign, and resets pathing afterwards.
    /// </summary>
    IEnumerator Stealing()
    {
        IsStealing = true;
        animator.SetTrigger("steal");
        stealingSign.SetActive(true);

        yield return new WaitForSeconds(2f); // Duration of stealing

        IsStealing = false;
        stealingSign.SetActive(false);

        currentPoint = (currentPoint + 1) % restPoints.Length;
        StartCoroutine(SwitchState("Moving"));
    }
}
