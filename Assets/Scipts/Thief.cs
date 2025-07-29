using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Theif : MonoBehaviour
{
    NavMeshAgent myAgent;

    [SerializeField] Transform targetTransform;
    [SerializeField] Transform[] restPoints;
    [SerializeField] float stopDistance = 0.5f;
    [SerializeField] float pauseTime = 3f;

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
        else if (newState == "Chase")
            StartCoroutine(Chase());
    }

    IEnumerator Idle()
    {
        yield return new WaitForSeconds(pauseTime);

        if (targetTransform != null)
        {
            Debug.Log("Switching to Chase");
            StartCoroutine(SwitchState("Chase"));
        }
        else
        {
            StartCoroutine(SwitchState("Moving"));
        }
    }

    IEnumerator Moving()
    {
        if (restPoints.Length == 0)
            yield break;

        Transform destination = restPoints[currentPoint];
        myAgent.SetDestination(destination.position);

        while (currentState == "Moving")
        {
            if (targetTransform != null)
            {
                Debug.Log("Player detected during patrol – switching to Chase");
                StartCoroutine(SwitchState("Chase"));
                yield break;
            }

            if (!myAgent.pathPending && myAgent.remainingDistance <= stopDistance)
            {
                currentPoint = (currentPoint + 1) % restPoints.Length;
                StartCoroutine(SwitchState("Idle"));
                yield break;
            }

            yield return null;
        }
    }

    IEnumerator Chase()
    {
        while (currentState == "Chase")
        {
            if (targetTransform == null)
            {
                Debug.Log("Lost player – returning to Idle");
                StartCoroutine(SwitchState("Idle"));
                yield break;
            }

            myAgent.SetDestination(targetTransform.position);
            yield return null;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered detection zone");
            targetTransform = other.transform;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player left detection zone");
            targetTransform = null;
        }
    }
}
