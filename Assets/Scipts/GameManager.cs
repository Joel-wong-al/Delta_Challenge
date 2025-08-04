using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Customer Spawning")]
    [SerializeField] private GameObject[] customerPrefabs; // Array of different customer models
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnInterval = 15f;
    [SerializeField] private float thiefSpawnChance = 0.3f; // 30% chance of spawning a thief

    [Header("Customer Apprehension UI")]
    [SerializeField] private GameObject apprehensionPopup;
    [SerializeField] private TextMeshProUGUI instructionsText;

    [Header("Scoring System")]
    [SerializeField] private int playerScore = 0;
    [SerializeField] private TextMeshProUGUI scoreText;
    
    private float spawnTimer;
    private GameObject currentCustomer;
    private Thief currentThief;
    private bool awaitingPlayerDecision = false;

    void Start()
    {
        // Ensure popup is hidden at start
        if (apprehensionPopup != null)
            apprehensionPopup.SetActive(false);
            
        // Set up instructions text
        if (instructionsText != null)
            instructionsText.text = "Press Y to Apprehend, N to Release";

        // Initialize score display
        UpdateScoreDisplay();
    }

    void Update()
    {
        // Handle customer spawning
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnCustomer();
            spawnTimer = 0f;
        }

        // Handle keyboard input for apprehension decision
        if (awaitingPlayerDecision)
        {
            if (Input.GetKeyDown(KeyCode.Y))
            {
                OnApprehendCustomer();
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                OnReleaseCustomer();
            }
        }
    }

    /// <summary>
    /// Spawns a new customer with predetermined thief/regular status.
    /// Randomly selects from available customer prefabs.
    /// </summary>
    private void SpawnCustomer()
    {
        if (customerPrefabs == null || customerPrefabs.Length == 0 || spawnPoint == null)
        {
            Debug.LogWarning("No customer prefabs assigned or spawn point missing!");
            return;
        }

        // Randomly select a customer prefab
        int randomPrefabIndex = Random.Range(0, customerPrefabs.Length);
        GameObject selectedPrefab = customerPrefabs[randomPrefabIndex];
        
        if (selectedPrefab == null)
        {
            Debug.LogWarning($"Customer prefab at index {randomPrefabIndex} is null!");
            return;
        }

        GameObject newCustomer = Instantiate(selectedPrefab, spawnPoint.position, Quaternion.identity);
        Thief customerScript = newCustomer.GetComponent<Thief>();
        
        if (customerScript != null)
        {
            // Determine if this customer should be a thief
            bool isThief = Random.value < thiefSpawnChance;
            customerScript.Initialize(isThief);
            
            Debug.Log($"Spawned new customer ({selectedPrefab.name}): {(isThief ? "THIEF" : "REGULAR")}");
        }
        else
        {
            Debug.LogWarning($"Customer prefab {selectedPrefab.name} is missing Thief component!");
        }
    }

    /// <summary>
    /// Called by CameraSystem when a customer is clicked in CCTV view.
    /// Shows the apprehension decision popup.
    /// </summary>
    /// <param name="customer">The customer GameObject that was clicked</param>
    /// <param name="thiefScript">The Thief component of the customer</param>
    public void ShowCustomerApprehensionUI(GameObject customer, Thief thiefScript)
    {
        if (apprehensionPopup == null) return;

        // Store references for the decision
        currentCustomer = customer;
        currentThief = thiefScript;
        awaitingPlayerDecision = true;

        // Show the popup
        apprehensionPopup.SetActive(true);
    }

    /// <summary>
    /// Called when player presses Y to apprehend a customer.
    /// </summary>
    private void OnApprehendCustomer()
    {
        if (currentCustomer != null && currentThief != null)
        {
            bool isCorrectDecision = currentThief.IsConfirmedThief();
            
            if (isCorrectDecision)
            {
                // Success - correctly apprehended a confirmed thief (3+ warnings)
                playerScore += 100;
                Debug.Log($"CORRECT! Apprehended confirmed thief. +100 points. Score: {playerScore}");
                ShowFeedback("CORRECT! Thief Apprehended! +100 points", Color.green);
            }
            else
            {
                // Mistake - apprehended innocent customer or unconfirmed suspect
                int warningCount = currentThief.GetCurrentWarningCount();
                if (warningCount == 0)
                {
                    // Completely innocent customer
                    playerScore -= 50;
                    Debug.Log($"WRONG! Apprehended innocent customer. -50 points. Score: {playerScore}");
                    ShowFeedback("WRONG! Innocent Customer! -50 points", Color.red);
                }
                else
                {
                    // Suspicious but not confirmed thief (1-2 warnings)
                    playerScore -= 25;
                    Debug.Log($"WRONG! Apprehended unconfirmed suspect ({warningCount} warnings). -25 points. Score: {playerScore}");
                    ShowFeedback($"WRONG! Only {warningCount} warnings! -25 points", Color.yellow);
                }
            }
            
            // ALWAYS remove customer from store when apprehended (regardless of correct/wrong)
            Destroy(currentCustomer);
        }

        UpdateScoreDisplay();
        HideApprehensionUI();
    }

    /// <summary>
    /// Called when player presses N to cancel/dismiss the apprehension decision.
    /// No consequences - just closes the popup and lets customer continue.
    /// </summary>
    private void OnReleaseCustomer()
    {
        // N key simply cancels the action - no scoring, no removal
        Debug.Log("Apprehension canceled - customer continues shopping");
        HideApprehensionUI();
    }

    /// <summary>
    /// Updates the score display UI.
    /// </summary>
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {playerScore}";
    }

    /// <summary>
    /// Shows feedback message to player (placeholder for future UI implementation).
    /// </summary>
    /// <param name="message">Feedback message</param>
    /// <param name="color">Message color</param>
    private void ShowFeedback(string message, Color color)
    {
        // TODO: Implement actual feedback UI
        Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>");
    }

    /// <summary>
    /// Hides the apprehension popup and clears references.
    /// </summary>
    private void HideApprehensionUI()
    {
        if (apprehensionPopup != null)
            apprehensionPopup.SetActive(false);

        currentCustomer = null;
        currentThief = null;
        awaitingPlayerDecision = false;
    }
}
