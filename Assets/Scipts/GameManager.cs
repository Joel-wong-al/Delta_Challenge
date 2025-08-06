using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class DayRequirement
{
    public int thieves;
    public int score;
    
    public DayRequirement(int thieves, int score)
    {
        this.thieves = thieves;
        this.score = score;
    }
}

public class GameManager : MonoBehaviour
{
    [Header("Customer Spawning")]
    [SerializeField] private GameObject[] customerPrefabs; // Array of different customer models
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform exitPoint; // Where customers leave the store

    [Header("Customer Apprehension UI")]
    [SerializeField] private GameObject apprehensionPopup;
    [SerializeField] private TextMeshProUGUI instructionsText;

    [Header("Gameplay UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject endOfDayPanel;
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;

    // Gameplay Flow Variables
    [Header("Gameplay Settings")]
    [SerializeField] private float dayDuration = 240f; // 4 minutes per day
    [SerializeField] private float waveDuration = 60f; // 1 minute per wave
    [SerializeField] private float restDuration = 10f; // 10 seconds rest between waves
    [SerializeField] private int customersPerWave = 4; // Number of customers per wave
    [SerializeField] private float customerSpawnInterval = 5f; // Time between customer spawns in a wave

    // Game State
    private int currentDay = 1;
    private int currentWave = 1;
    private int playerScore = 0;
    private float dayTimer = 0f;
    private float waveTimer = 0f;
    private float restTimer = 0f;
    private bool isInWave = false;
    private bool isResting = false;
    private bool gameActive = false;
    private bool dayComplete = false;

    // Current wave/day tracking
    private List<GameObject> activeCustomers = new List<GameObject>();
    private List<string> thiefsCaughtToday = new List<string>();
    private List<string> thiefsEscapedToday = new List<string>();
    private int thiefCountForDay = 0;
    private int thievesSpawnedToday = 0;
    private int thievesCaughtToday = 0;
    
    // Thief wave distribution
    private Dictionary<int, int> thievesPerWave = new Dictionary<int, int>(); // wave -> thief count

    // Day requirements (day number -> required thieves, required score)
    private Dictionary<int, DayRequirement> dayRequirements = new Dictionary<int, DayRequirement>
    {
        {1, new DayRequirement(1, 0)},
        {2, new DayRequirement(2, 100)},
        {3, new DayRequirement(3, 150)},
        {4, new DayRequirement(4, 250)},
        {5, new DayRequirement(5, 300)}
    };

    // Customer interaction tracking
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

        // Initialize UI displays
        UpdateAllUI();
        
        // Hide end panels
        if (endOfDayPanel != null)
            endOfDayPanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Start the first day
        StartDay();
    }

    void Update()
    {
        // Handle keyboard input for apprehension decision (only when game is active)
        if (gameActive && awaitingPlayerDecision)
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

        // Handle day progression input (works even when game is not active)
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log($"G pressed - dayComplete: {dayComplete}");
            if (!dayComplete)
            {
                Debug.Log("Day is not complete yet, G key has no effect");
            }
        }
        
        // DEBUG: Press K to instantly end day and catch all thieves (works even when game is not active)
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("DEBUG: K pressed - Instantly ending day and catching all thieves");
            
            // Set score high enough to pass any day
            playerScore = 1000;
            
            // Mark all thieves as caught
            thievesCaughtToday = thievesSpawnedToday;
            for (int i = 1; i <= thievesSpawnedToday; i++)
            {
                if (!thiefsCaughtToday.Contains($"Thief #{i} (debug catch)"))
                {
                    thiefsCaughtToday.Add($"Thief #{i} (debug catch)");
                }
            }
            
            // Force end the day
            EndDay();
        }
        
        if (dayComplete && Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("G pressed - Day progression triggered");
            
            // Hide the end of day panel first
            if (endOfDayPanel != null)
                endOfDayPanel.SetActive(false);
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
                
            // Check if day was passed to determine action
            bool dayPassed = CheckDayRequirements();
            Debug.Log($"Day passed check: {dayPassed}");
            
            if (!dayPassed)
            {
                // Day failed, restart current day
                Debug.Log("Day failed - restarting current day");
                RestartDay();
            }
            else if (currentDay >= 5)
            {
                // Game complete, restart entire game
                Debug.Log("Game complete - restarting game");
                OnRestartGameButton();
            }
            else
            {
                // Day passed, go to next day
                Debug.Log($"Day passed - going from day {currentDay} to day {currentDay + 1}");
                NextDay();
            }
        }

        // Only run game logic when gameActive is true
        if (!gameActive) 
        {
            UpdateTimeDisplay(); // Still update time display for UI
            return;
        }

        // Update day timer only during active waves (not during rest periods)
        if (isInWave)
        {
            dayTimer += Time.deltaTime;
        }
        
        // Handle wave and rest timing
        if (isInWave)
        {
            HandleWaveUpdate();
        }
        else if (isResting)
        {
            HandleRestUpdate();
        }

        // Check if day is complete (only check during waves, not rest periods)
        if (isInWave && dayTimer >= dayDuration && !dayComplete)
        {
            EndDay();
        }

        // Update UI
        UpdateTimeDisplay();
    }

    #region Day Management

    /// <summary>
    /// Starts a new day with fresh parameters.
    /// </summary>
    private void StartDay()
    {
        Debug.Log($"=== STARTING DAY {currentDay} ===");
        
        // Reset day variables
        dayTimer = 0f;
        waveTimer = 0f;
        restTimer = 0f;
        currentWave = 1;
        isInWave = false;
        isResting = false;
        dayComplete = false;
        gameActive = true;

        // Clear tracking lists
        thiefsCaughtToday.Clear();
        thiefsEscapedToday.Clear();
        thievesSpawnedToday = 0;
        thievesCaughtToday = 0;

        // Configure day-specific settings (including thief count)
        ConfigureDaySettings();
        
        Debug.Log($"Day {currentDay} started: Target {thiefCountForDay} thieves, {customersPerWave} customers per wave, 4 waves total");
        
        // Randomly distribute thieves across waves
        DistributeThievesAcrossWaves();
        
        // Clear any remaining customers from previous day
        ClearAllCustomers();
        
        // Start first wave after brief delay
        StartCoroutine(StartWaveAfterDelay(2f));
        
        UpdateAllUI();
    }

    /// <summary>
    /// Configures day-specific settings to increase difficulty and variety.
    /// </summary>
    private void ConfigureDaySettings()
    {
        // Always 4 customers per wave - only thief count changes per day
        customersPerWave = 4;
        
        switch (currentDay)
        {
            case 1:
                // Day 1: Tutorial - Easy settings
                thiefCountForDay = 1;
                Debug.Log("Day 1: Tutorial mode - 1 thief");
                break;
                
            case 2:
                // Day 2: Slightly more challenging
                thiefCountForDay = 2;
                Debug.Log("Day 2: Increased challenge - 2 thieves");
                break;
                
            case 3:
                // Day 3: More thieves
                thiefCountForDay = 3;
                Debug.Log("Day 3: Higher difficulty - 3 thieves");
                break;
                
            case 4:
                // Day 4: High intensity
                thiefCountForDay = 4;
                Debug.Log("Day 4: High intensity - 4 thieves");
                break;
                
            case 5:
                // Day 5: Maximum challenge
                thiefCountForDay = 5;
                Debug.Log("Day 5: Maximum challenge - 5 thieves");
                break;
                
            default:
                // Fallback to standard settings
                thiefCountForDay = 1;
                Debug.LogWarning($"Day {currentDay}: Using default settings");
                break;
        }
        
        Debug.Log($"Day {currentDay} Configuration: {customersPerWave} customers/wave, {thiefCountForDay} thieves total");
    }

    /// <summary>
    /// Randomly distributes thieves across the 4 waves, with max 2 thieves per wave.
    /// </summary>
    private void DistributeThievesAcrossWaves()
    {
        thievesPerWave.Clear();
        
        // Initialize all waves with 0 thieves
        for (int wave = 1; wave <= 4; wave++)
        {
            thievesPerWave[wave] = 0;
        }
        
        int remainingThieves = thiefCountForDay;
        
        while (remainingThieves > 0)
        {
            // Get all waves that can still accommodate thieves (less than 2)
            List<int> availableWaves = new List<int>();
            for (int wave = 1; wave <= 4; wave++)
            {
                if (thievesPerWave[wave] < 2)
                {
                    availableWaves.Add(wave);
                }
            }
            
            if (availableWaves.Count == 0)
            {
                Debug.LogWarning($"Cannot fit all {thiefCountForDay} thieves with max 2 per wave!");
                break;
            }
            
            // Randomly select a wave and add a thief
            int randomWaveIndex = Random.Range(0, availableWaves.Count);
            int selectedWave = availableWaves[randomWaveIndex];
            thievesPerWave[selectedWave]++;
            remainingThieves--;
        }
        
        // Debug output
        string distribution = "Thief distribution: ";
        for (int wave = 1; wave <= 4; wave++)
        {
            distribution += $"Wave {wave}: {thievesPerWave[wave]} thieves";
            if (wave < 4) distribution += ", ";
        }
        Debug.Log(distribution);
    }

    /// <summary>
    /// Ends the current day and shows summary.
    /// </summary>
    private void EndDay()
    {
        Debug.Log($"=== DAY {currentDay} COMPLETE ===");
        
        dayComplete = true;
        gameActive = false;
        
        // Stop all wave activities immediately
        isInWave = false;
        isResting = false;
        
        // Stop all coroutines to prevent background activities
        StopAllCoroutines();
        
        // Clear any remaining customers
        ClearAllCustomers();
        
        // Check if day requirements are met
        bool dayPassed = CheckDayRequirements();
        
        if (dayPassed)
        {
            ShowEndOfDayPanel(true);
        }
        else
        {
            ShowEndOfDayPanel(false);
        }
    }

    /// <summary>
    /// Checks if the player met the requirements for the current day.
    /// </summary>
    /// <returns>True if day requirements are met</returns>
    private bool CheckDayRequirements()
    {
        if (!dayRequirements.ContainsKey(currentDay))
            return true; // No requirements defined

        var requirements = dayRequirements[currentDay];
        bool scoreRequirementMet = playerScore >= requirements.score;
        
        Debug.Log($"Day {currentDay} Requirements: Score >= {requirements.score} (Current: {playerScore}), " +
                  $"Thieves: {requirements.thieves} (Spawned: {thievesSpawnedToday}, Caught: {thievesCaughtToday})");
        
        return scoreRequirementMet;
    }

    /// <summary>
    /// Proceeds to the next day or ends the game.
    /// </summary>
    public void NextDay()
    {
        if (currentDay >= 5)
        {
            // Game complete!
            ShowGameComplete();
            return;
        }

        currentDay++;
        StartDay();
    }

    /// <summary>
    /// Restarts the current day.
    /// </summary>
    public void RestartDay()
    {
        // Reset score to beginning of day value (could be implemented with checkpoints)
        StartDay();
    }

    #endregion

    #region Wave Management

    /// <summary>
    /// Handles wave timing and customer spawning during active waves.
    /// </summary>
    private void HandleWaveUpdate()
    {
        waveTimer += Time.deltaTime;
        
        // Check if wave is complete
        if (waveTimer >= waveDuration)
        {
            EndWave();
            return;
        }
    }

    /// <summary>
    /// Handles rest period timing between waves.
    /// </summary>
    private void HandleRestUpdate()
    {
        restTimer += Time.deltaTime;
        
        if (restTimer >= restDuration)
        {
            StartNextWave();
        }
    }

    /// <summary>
    /// Starts a wave after a delay.
    /// </summary>
    private IEnumerator StartWaveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartWave();
    }

    /// <summary>
    /// Starts a new wave of customers.
    /// </summary>
    private void StartWave()
    {
        Debug.Log($"=== STARTING WAVE {currentWave} ===");
        
        // Debug: Show thief distribution for this wave
        int thievesForThisWave = thievesPerWave.ContainsKey(currentWave) ? thievesPerWave[currentWave] : 0;
        Debug.Log($"Wave {currentWave} should have {thievesForThisWave} thieves according to distribution");
        
        isInWave = true;
        isResting = false;
        waveTimer = 0f;
        
        // Start spawning customers for this wave
        StartCoroutine(SpawnWaveCustomers());
        
        UpdateAllUI();
    }

    /// <summary>
    /// Spawns customers for the current wave.
    /// </summary>
    private IEnumerator SpawnWaveCustomers()
    {
        int customersSpawned = 0;
        int thievesToSpawnThisWave = thievesPerWave.ContainsKey(currentWave) ? thievesPerWave[currentWave] : 0;
        
        // Track used customer prefab indices to avoid duplicates in the same wave
        List<int> usedPrefabIndices = new List<int>();
        
        Debug.Log($"Starting Wave {currentWave}: Planning to spawn {thievesToSpawnThisWave} thieves out of {customersPerWave} total customers");
        
        // If no thieves should spawn in this wave, spawn all regular customers
        if (thievesToSpawnThisWave == 0)
        {
            Debug.Log($"Wave {currentWave}: No thieves assigned - spawning {customersPerWave} regular customers");
            
            while (customersSpawned < customersPerWave && isInWave)
            {
                Debug.Log($"Spawning REGULAR customer in wave {currentWave} (customer {customersSpawned + 1}/{customersPerWave})");
                SpawnCustomer(false, usedPrefabIndices); // false = regular customer
                customersSpawned++;
                
                yield return new WaitForSeconds(customerSpawnInterval);
            }
        }
        else
        {
            // Create a list to determine which customer positions should be thieves
            List<bool> customerTypes = new List<bool>();
            
            // Add thieves for this wave
            for (int i = 0; i < thievesToSpawnThisWave; i++)
            {
                customerTypes.Add(true); // true = thief
            }
            
            // Fill remaining positions with regular customers
            for (int i = thievesToSpawnThisWave; i < customersPerWave; i++)
            {
                customerTypes.Add(false); // false = regular customer
            }
            
            // Shuffle the list to randomize thief positions within the wave
            for (int i = 0; i < customerTypes.Count; i++)
            {
                bool temp = customerTypes[i];
                int randomIndex = Random.Range(i, customerTypes.Count);
                customerTypes[i] = customerTypes[randomIndex];
                customerTypes[randomIndex] = temp;
            }
            
            // Debug: Show the planned spawn order
            string spawnOrder = "Spawn order: ";
            for (int i = 0; i < customerTypes.Count; i++)
            {
                spawnOrder += customerTypes[i] ? "T" : "R";
                if (i < customerTypes.Count - 1) spawnOrder += ", ";
            }
            Debug.Log(spawnOrder);
            
            // Spawn customers according to the randomized order
            while (customersSpawned < customersPerWave && isInWave)
            {
                bool shouldBeThief = customerTypes[customersSpawned];
                
                if (shouldBeThief)
                {
                    thievesSpawnedToday++;
                    Debug.Log($"Spawning THIEF #{thievesSpawnedToday} in wave {currentWave} (customer {customersSpawned + 1}/{customersPerWave})");
                }
                else
                {
                    Debug.Log($"Spawning REGULAR customer in wave {currentWave} (customer {customersSpawned + 1}/{customersPerWave})");
                }
                
                SpawnCustomer(shouldBeThief, usedPrefabIndices);
                customersSpawned++;
                
                yield return new WaitForSeconds(customerSpawnInterval);
            }
        }
        
        Debug.Log($"Wave {currentWave} spawning complete: {customersSpawned} customers spawned, {thievesSpawnedToday} total thieves so far");
    }

    /// <summary>
    /// Ends the current wave and starts rest period.
    /// </summary>
    private void EndWave()
    {
        Debug.Log($"=== WAVE {currentWave} COMPLETE ===");
        
        isInWave = false;
        
        // Make all customers leave the store
        StartCoroutine(MakeCustomersLeave());
        
        if (currentWave > 4 || dayTimer >= dayDuration - restDuration)
        {
            // Last wave of the day or no time for another wave
            return;
        }
        
        // Start rest period
        isResting = true;
        restTimer = 0f;
        
        UpdateAllUI();
    }

    /// <summary>
    /// Starts the next wave after rest period.
    /// </summary>
    private void StartNextWave()
    {
        currentWave++;
        StartWave();
    }

    /// <summary>
    /// Makes all active customers leave the store.
    /// </summary>
    private IEnumerator MakeCustomersLeave()
    {
        List<GameObject> customersToRemove = new List<GameObject>(activeCustomers);
        
        foreach (GameObject customer in customersToRemove)
        {
            if (customer != null)
            {
                // Make customer walk to exit
                StartCoroutine(MakeCustomerWalkToExit(customer));
            }
        }
        
        yield return null;
    }

    /// <summary>
    /// Makes a specific customer walk to the exit and then removes them.
    /// </summary>
    private IEnumerator MakeCustomerWalkToExit(GameObject customer)
    {
        if (customer == null || exitPoint == null) 
        {
            // If no exit point or customer is null, just remove immediately
            if (customer != null)
            {
                ProcessCustomerExit(customer);
            }
            yield break;
        }

        // Get the NavMeshAgent component to make customer walk to exit
        UnityEngine.AI.NavMeshAgent navAgent = customer.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            // Enable the agent and set destination
            navAgent.enabled = true;
            navAgent.isStopped = false;
            
            // Try to set destination, but handle cases where it might fail
            if (navAgent.SetDestination(exitPoint.position))
            {
                // Wait for customer to reach the exit or timeout after reasonable time
                float timeout = 15f; // 15 seconds max to reach exit
                float timer = 0f;
                Vector3 lastPosition = customer.transform.position;
                float stuckTimer = 0f;
                
                while (timer < timeout && customer != null)
                {
                    // Check if customer is close to exit point
                    if (Vector3.Distance(customer.transform.position, exitPoint.position) < 2f)
                    {
                        break;
                    }
                    
                    // Check if customer is stuck (not moving for 3 seconds)
                    if (Vector3.Distance(customer.transform.position, lastPosition) < 0.1f)
                    {
                        stuckTimer += Time.deltaTime;
                        if (stuckTimer > 3f)
                        {
                            customer.transform.position = exitPoint.position;
                            break;
                        }
                    }
                    else
                    {
                        stuckTimer = 0f;
                        lastPosition = customer.transform.position;
                    }
                    
                    timer += Time.deltaTime;
                    yield return null;
                }
            }
        }
        else
        {
            // No NavMeshAgent, just wait a brief moment for visual effect
            yield return new WaitForSeconds(1f);
        }
        
        // Process the customer exit (scoring, removal, etc.)
        if (customer != null)
        {
            ProcessCustomerExit(customer);
        }
    }

    /// <summary>
    /// Processes the customer exit (scoring and removal).
    /// </summary>
    private void ProcessCustomerExit(GameObject customer)
    {
        Thief thiefScript = customer.GetComponent<Thief>();
        if (thiefScript != null && thiefScript.IsThief && !thiefsCaughtToday.Contains(customer.name))
        {
            // Thief escaped - penalty
            playerScore -= 100;
            thiefsEscapedToday.Add($"Thief #{thievesSpawnedToday} (escaped)");
            Debug.Log($"Thief escaped! -100 points. Score: {playerScore}");
        }
        
        // Remove customer from tracking and destroy
        activeCustomers.Remove(customer);
        Destroy(customer);
        
        UpdateScoreDisplay();
    }

    #endregion
    #region Customer Management

    /// <summary>
    /// Spawns a new customer with predetermined thief/regular status.
    /// </summary>
    /// <param name="forceThief">Whether to force this customer to be a thief</param>
    /// <param name="usedPrefabIndices">List of prefab indices already used in this wave to avoid duplicates</param>
    private void SpawnCustomer(bool forceThief = false, List<int> usedPrefabIndices = null)
    {
        if (customerPrefabs == null || customerPrefabs.Length == 0 || spawnPoint == null)
        {
            Debug.LogWarning("No customer prefabs assigned or spawn point missing!");
            return;
        }

        // Select a customer prefab that hasn't been used in this wave
        int randomPrefabIndex;
        int attempts = 0;
        int maxAttempts = customerPrefabs.Length * 2; // Prevent infinite loops
        
        do
        {
            randomPrefabIndex = Random.Range(0, customerPrefabs.Length);
            attempts++;
            
            // If we've tried too many times or no used list provided, just use any prefab
            if (attempts >= maxAttempts || usedPrefabIndices == null)
            {
                break;
            }
        }
        while (usedPrefabIndices.Contains(randomPrefabIndex));
        
        // Add this prefab to the used list if provided
        if (usedPrefabIndices != null && !usedPrefabIndices.Contains(randomPrefabIndex))
        {
            usedPrefabIndices.Add(randomPrefabIndex);
        }
        
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
            // Initialize customer as thief or regular based on parameter
            customerScript.Initialize(forceThief);
            
            // Add to active customers list
            activeCustomers.Add(newCustomer);
            
            Debug.Log($"Spawned new customer ({selectedPrefab.name}): {(forceThief ? "THIEF" : "REGULAR")}");
        }
        else
        {
            Debug.LogWarning($"Customer prefab {selectedPrefab.name} is missing Thief component!");
        }
    }

    /// <summary>
    /// Clears all active customers from the store.
    /// </summary>
    private void ClearAllCustomers()
    {
        foreach (GameObject customer in activeCustomers)
        {
            if (customer != null)
                Destroy(customer);
        }
        activeCustomers.Clear();
    }

    #endregion

    #region Player Interaction

    /// <summary>
    /// Called by CameraSystem when a customer is clicked in CCTV view.
    /// Shows the apprehension decision popup.
    /// </summary>
    /// <param name="customer">The customer GameObject that was clicked</param>
    /// <param name="thiefScript">The Thief component of the customer</param>
    public void ShowCustomerApprehensionUI(GameObject customer, Thief thiefScript)
    {
        if (apprehensionPopup == null || !gameActive) return;

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
            bool isActualThief = currentThief.IsThief;
            int warningCount = currentThief.GetCurrentWarningCount();
            
            if (isCorrectDecision)
            {
                // Success - correctly apprehended a confirmed thief (3+ warnings)
                playerScore += 100;
                thievesCaughtToday++;
                thiefsCaughtToday.Add($"Thief #{thievesSpawnedToday} (confirmed, 3 warnings)");
                Debug.Log($"CORRECT! Apprehended confirmed thief. +100 points. Score: {playerScore}");
                ShowFeedback("CORRECT! Thief Apprehended! +100 points", Color.green);
            }
            else if (isActualThief && warningCount >= 1 && warningCount < 3)
            {
                // Apprehended actual thief but with insufficient warnings
                playerScore -= 50;
                thievesCaughtToday++;
                thiefsCaughtToday.Add($"Thief #{thievesSpawnedToday} (early arrest, {warningCount} warnings)");
                Debug.Log($"PARTIAL! Apprehended thief early ({warningCount} warnings). -50 points. Score: {playerScore}");
                ShowFeedback($"EARLY ARREST! Only {warningCount} warnings! -50 points", Color.yellow);
            }
            else if (!isActualThief && warningCount >= 1 && warningCount < 3)
            {
                // Apprehended innocent with some warnings  
                playerScore -= 50;
                Debug.Log($"WRONG! Apprehended innocent with {warningCount} warnings. -50 points. Score: {playerScore}");
                ShowFeedback($"WRONG! Innocent with {warningCount} warnings! -50 points", Color.yellow);
            }
            else
            {
                // Completely innocent customer (0 warnings)
                playerScore -= 100;
                Debug.Log($"WRONG! Apprehended innocent customer. -100 points. Score: {playerScore}");
                ShowFeedback("WRONG! Innocent Customer! -100 points", Color.red);
            }
            
            // Remove customer from store and tracking
            activeCustomers.Remove(currentCustomer);
            Destroy(currentCustomer);
        }

        UpdateScoreDisplay();
        HideApprehensionUI();
    }

    /// <summary>
    /// Called when player presses N to cancel/dismiss the apprehension decision.
    /// </summary>
    private void OnReleaseCustomer()
    {
        Debug.Log("Apprehension canceled - customer continues shopping");
        HideApprehensionUI();
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

    #endregion

    #region UI Management

    /// <summary>
    /// Updates all UI displays.
    /// </summary>
    private void UpdateAllUI()
    {
        UpdateScoreDisplay();
        UpdateDayDisplay();
        UpdateWaveDisplay();
        UpdateStatusDisplay();
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
    /// Updates the day display UI.
    /// </summary>
    private void UpdateDayDisplay()
    {
        if (dayText != null)
            dayText.text = $"Day {currentDay}/5";
    }

    /// <summary>
    /// Updates the wave display UI.
    /// </summary>
    private void UpdateWaveDisplay()
    {
        if (waveText != null)
        {
            if (isInWave)
                waveText.text = $"Wave {currentWave}/4";
            else if (isResting)
                waveText.text = $"Rest Period (Timer Paused)";
            else
                waveText.text = "Preparing...";
        }
    }

    /// <summary>
    /// Updates the status display UI.
    /// </summary>
    private void UpdateStatusDisplay()
    {
        if (statusText != null)
        {
            if (dayRequirements.ContainsKey(currentDay))
            {
                var req = dayRequirements[currentDay];
                statusText.text = $"Target: {req.score} pts | Thieves: {thievesSpawnedToday}/{req.thieves}";
            }
        }
    }

    /// <summary>
    /// Updates the time display UI.
    /// </summary>
    private void UpdateTimeDisplay()
    {
        if (timeText != null)
        {
            float timeRemaining = dayDuration - dayTimer;
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            
            // Convert to in-game time (12am to 9am = 9 hours over 4 minutes)
            float gameTimeProgress = dayTimer / dayDuration;
            float gameHour = 0f + (gameTimeProgress * 9f); // 12am = 0, 9am = 9
            int displayHour = Mathf.FloorToInt(gameHour);
            int displayMinute = Mathf.FloorToInt((gameHour - displayHour) * 60f);
            
            string period = "AM";
            int displayHour12 = displayHour;
            if (displayHour == 0) displayHour12 = 12;
            else if (displayHour > 12) { displayHour12 = displayHour - 12; period = "PM"; }
            
            timeText.text = $"{displayHour12:00}:{displayMinute:00} {period} | {minutes:00}:{seconds:00}";
        }
    }

    /// <summary>
    /// Shows the end of day panel with summary.
    /// </summary>
    /// <param name="dayPassed">Whether the player passed the day</param>
    private void ShowEndOfDayPanel(bool dayPassed)
    {
        Debug.Log($"=== SHOWING END OF DAY PANEL ===");
        Debug.Log($"Day passed: {dayPassed}");
        Debug.Log($"endOfDayPanel null: {endOfDayPanel == null}");
        Debug.Log($"summaryText null: {summaryText == null}");
        
        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(true);
            Debug.Log("End of day panel activated");
            
            if (summaryText != null)
            {
                string summary = GenerateDaySummary(dayPassed);
                summaryText.text = summary;
                Debug.Log("Summary text updated");
            }
            else
            {
                Debug.LogWarning("summaryText is null - assign it in the Inspector!");
            }
        }
        else
        {
            Debug.LogWarning("endOfDayPanel is null - assign it in the Inspector!");
            // Fallback: Show summary in console
            string summary = GenerateDaySummary(dayPassed);
            Debug.Log($"DAY SUMMARY (UI not assigned):\n{summary}");
        }
    }

    /// <summary>
    /// Generates the end-of-day summary text.
    /// </summary>
    /// <param name="dayPassed">Whether the player passed the day</param>
    /// <returns>Summary text</returns>
    private string GenerateDaySummary(bool dayPassed)
    {
        var req = dayRequirements.ContainsKey(currentDay) ? dayRequirements[currentDay] : new DayRequirement(0, 0);
        
        string summary = $"=== DAY {currentDay} SUMMARY ===\n\n";
        summary += $"Trust Fund Balance: {playerScore} points\n";
        summary += $"Required Score: {req.score} points\n\n";
        
        summary += $"Thieves Spawned: {thievesSpawnedToday}/{req.thieves}\n";
        summary += $"Thieves Caught: {thievesCaughtToday}\n\n";
        
        summary += "THIEVES CAUGHT TODAY:\n";
        if (thiefsCaughtToday.Count > 0)
        {
            foreach (string thief in thiefsCaughtToday)
            {
                summary += $"• {thief}\n";
            }
        }
        else
        {
            summary += "• None\n";
        }
        
        summary += "\nTHIEVES ESCAPED:\n";
        if (thiefsEscapedToday.Count > 0)
        {
            foreach (string thief in thiefsEscapedToday)
            {
                summary += $"• {thief}\n";
            }
        }
        else
        {
            summary += "• None\n";
        }
        
        summary += $"\n=== {(dayPassed ? "DAY PASSED!" : "DAY FAILED!")} ===\n";
        
        if (!dayPassed)
        {
            summary += "You must restart this day.\n\nPress G to restart day.";
        }
        else if (currentDay >= 5)
        {
            summary += "CONGRATULATIONS! You've completed all 5 days!\n\nPress G to restart game.";
        }
        else
        {
            summary += "Ready for the next day!\n\nPress G to continue to next day.";
        }
        
        return summary;
    }

    /// <summary>
    /// Shows the game complete screen.
    /// </summary>
    private void ShowGameComplete()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (gameOverText != null)
            {
                gameOverText.text = $"CONGRATULATIONS!\n\nYou've successfully completed all 5 days!\n\nFinal Score: {playerScore} points";
            }
        }
    }

    /// <summary>
    /// Shows feedback message to player (placeholder for future UI implementation).
    /// </summary>
    /// <param name="message">Feedback message</param>
    /// <param name="color">Message color</param>
    private void ShowFeedback(string message, Color color)
    {
        // TODO: Implement actual feedback UI (could be a popup or status message)
        Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>");
    }

    #endregion

    #region Public Button Methods (for UI)

    /// <summary>
    /// Button method to proceed to next day.
    /// </summary>
    public void OnNextDayButton()
    {
        if (endOfDayPanel != null)
            endOfDayPanel.SetActive(false);
            
        NextDay();
    }

    /// <summary>
    /// Button method to restart current day.
    /// </summary>
    public void OnRestartDayButton()
    {
        if (endOfDayPanel != null)
            endOfDayPanel.SetActive(false);
            
        RestartDay();
    }

    /// <summary>
    /// Button method to restart the entire game.
    /// </summary>
    public void OnRestartGameButton()
    {
        currentDay = 1;
        playerScore = 0;
        
        if (endOfDayPanel != null)
            endOfDayPanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
            
        StartDay();
    }

    #endregion
}
