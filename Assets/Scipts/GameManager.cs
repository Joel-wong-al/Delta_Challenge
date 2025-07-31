using UnityEngine;

public class NewMonoBehaviourScript1 : MonoBehaviour
{
    [SerializeField] private GameObject thiefPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnInterval = 10f;

    private float spawnTimer;

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            Instantiate(thiefPrefab, spawnPoint.position, Quaternion.identity);
            spawnTimer = 0f;
        }
    }

}
