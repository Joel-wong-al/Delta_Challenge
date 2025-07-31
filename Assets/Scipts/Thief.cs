using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Thief : MonoBehaviour
{
    NavMeshAgent myAgent;

    [SerializeField] Transform[] restPoints;
    [SerializeField] float stopDistance = 0.5f;
    [SerializeField] float pauseTime = 3f;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject stealingSign;

    public bool IsStealing { get; private set; } = false;

    private int currentPoint = 0;
    private string currentState;

    void Awake()
    {
        myAgent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        StartCoroutine(SwitchState("Idle"));
    }

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

    IEnumerator Idle()
    {
        yield return new WaitForSeconds(pauseTime);
        StartCoroutine(SwitchState("Moving"));

    }

    IEnumerator Moving()
    {
        if (restPoints.Length == 0)
            yield break;

        Transform destination = restPoints[currentPoint];
        myAgent.SetDestination(destination.position);
        animator.SetBool("isWalking", true);
        bool willSteal = false;

        while (currentState == "Moving")
        {
            if (!myAgent.pathPending && myAgent.remainingDistance <= stopDistance)
            {
                animator.SetBool("isWalking", false);
                willSteal = Random.value < 0.4f;

                if (willSteal)
                {
                    Debug.Log("Stealing from shelf");
                    StartCoroutine(SwitchState("Stealing"));
                }
                else
                {
                    StartCoroutine(SwitchState("Idle"));
                }
                yield break; // prevent looping after switch
            }

            yield return null;
        }
    }
    IEnumerator Stealing()
    {
        IsStealing = true; //  Start stealing state

        animator.SetTrigger("steal");
        stealingSign.SetActive(true);

        yield return new WaitForSeconds(2f); // Stealing duration

        IsStealing = false; //  Stop stealing state
        stealingSign.SetActive(false);

        currentPoint = (currentPoint + 1) % restPoints.Length;
        StartCoroutine(SwitchState("Moving"));
    }


}
