/******************************************************************************
 * File: GameManager.cs
 * Author: Javier, Zenon, Joel
 * Created: [Insert Date]
 * Description: Central game controller for Delta Challenge. Manages game flow,
 *              day and wave progression, customer and thief spawning, UI updates,
 *              cutscenes, pause system, and player/camera management.
 ******************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using StarterAssets;
using UnityEngine.Rendering;


/// <summary>
/// Data structure for daily requirements (number of thieves and minimum score).
/// </summary>
[System.Serializable]
public class DayRequirement
{
    /// <summary>Number of thieves required for the day.</summary>
    public int thieves;
    /// <summary>Minimum score required for the day.</summary>
    public int score;
    
    public DayRequirement(int thieves, int score)
    {
        this.thieves = thieves;
        this.score = score;
    }
}


/// <summary>
/// Main game manager for Delta Challenge. Handles all game state, day and wave progression,
/// customer and thief spawning, UI, cutscenes, pause, and player/camera management.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ===================== Customer Spawning =====================
    [Header("Customer Spawning")]
    [SerializeField] private GameObject[] customerPrefabs; ///< Array of different customer models
    [SerializeField] private Transform spawnPoint; ///< Customer spawn location
    [SerializeField] private Transform exitPoint; ///< Where customers leave the store

    // ===================== Customer Apprehension UI =====================
    [Header("Customer Apprehension UI")]
    [SerializeField] private GameObject apprehensionPopup; ///< Apprehension popup UI
    [SerializeField] private TextMeshProUGUI instructionsText; ///< Instructions for apprehension

    // ===================== Gameplay UI =====================
    [Header("Gameplay UI")]
    [SerializeField] private TextMeshProUGUI dayText; ///< Day display
    [SerializeField] private TextMeshProUGUI timeText; ///< Timer display
    [SerializeField] private TextMeshProUGUI dayTimeText; ///< Day time display
    [SerializeField] private TextMeshProUGUI waveText; ///< Wave display
    [SerializeField] private TextMeshProUGUI trustFundText; ///< Trust fund numerical indicator
    [SerializeField] private TextMeshProUGUI thiefText; ///< Thief numerical indicator
    [SerializeField] private GameObject endOfDayPanel; ///< End of day summary panel
    [SerializeField] private TextMeshProUGUI summaryText; ///< End of day summary text
    [SerializeField] private GameObject crosshairUI; ///< Crosshair to hide/show during camera switching

    // ===================== UI Background Elements =====================
    [Header("UI Background Elements")]
    [SerializeField] private GameObject dayWaveUIBackground; ///< Background panel for day and wave text
    [SerializeField] private GameObject timeUIBackground; ///< Background panel for time text
    [SerializeField] private GameObject dayTimeUIBackground; ///< Background panel for day time text
    [SerializeField] private GameObject trustFundThiefUIBackground; ///< Background panel shared by trust fund and thief text

    // ===================== Cutscene =====================
    [Header("Cutscene")]
    [SerializeField] private Camera cutsceneCamera; ///< Camera for escaped thieves cutscene
    [SerializeField] private Transform prisonCellSpawnPoint; ///< Where escaped thieves appear in prison cell
    [SerializeField] private TextMeshProUGUI cutsceneText; ///< Text UI for cutscene narration
    [SerializeField] private GameObject cutsceneBackgroundPanel; ///< Background panel for cutscene text
    [SerializeField] private float cameraMovementDistance = 1.1f; ///< How far the camera moves back

    // ===================== Cashier NPC =====================
    [Header("Cashier NPC")]
    [SerializeField] private CashierBehaviour cashierBehaviour; ///< Reference to the cashier NPC script

    // ===================== Post Processing =====================
    [Header("Post Processing")]
    [SerializeField] private Volume cctvVolume; ///< Volume with CCTV post-processing effects
    [SerializeField] private Volume firstPersonVolume; ///< Volume with first-person post-processing effects

    // ===================== Lighting =====================
    [Header("Lighting")]
    [SerializeField] private Light directionalLight; ///< Sun light that rotates during the day

    // ===================== Pause Menu =====================
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel; ///< Pause menu panel
    [SerializeField] private UnityEngine.UI.Button resumeButton; ///< Resume button
    [SerializeField] private UnityEngine.UI.Button restartButton; ///< Restart day button
    [SerializeField] private UnityEngine.UI.Button mainMenuButton; ///< Main menu button

    // ===================== Player References =====================
    [Header("Player References")]
    [SerializeField] private GameObject playerObject; ///< Reference to the player GameObject
    [SerializeField] private Transform playerSpawnPoint; ///< Where the player should spawn/respawn
    [SerializeField] private CameraSystem cameraSystem; ///< Reference to the camera system
    [SerializeField] private PlayerBehaviour playerBehaviour; ///< Reference to the player behaviour

    // ===================== Gameplay Flow Variables =====================
    [Header("Gameplay Settings")]
    [SerializeField] private float dayDuration = 240f; ///< 4 minutes per day
    [SerializeField] private float waveDuration = 60f; ///< 1 minute per wave
    [SerializeField] private float restDuration = 10f; ///< 10 seconds rest between waves
    [SerializeField] private int customersPerWave = 4; ///< Number of customers per wave
    [SerializeField] private float customerSpawnInterval = 5f; ///< Time between customer spawns in a wave

    // ===================== Game State =====================
    private int currentDay = 1; ///< Current day number
    private int currentWave = 1; ///< Current wave number
    private int playerScore = 0; ///< Player's trust fund score
    private float dayTimer = 0f; ///< Timer for the day
    private float waveTimer = 0f; ///< Timer for the current wave
    private float restTimer = 0f; ///< Timer for rest period
    private bool isInWave = false; ///< Is a wave currently active?
    private bool isResting = false; ///< Is the game in a rest period?
    private bool gameActive = false; ///< Is the game currently active?
    private bool dayComplete = false; ///< Has the day been completed?
    private bool gameCompleted = false; ///< Track if all 5 days are completed
    private bool isPaused = false; ///< Is the game currently paused?
    
    // ===================== Preparation phase =====================
    private bool isInPreparation = false; ///< Is the game in the preparation phase?
    private float preparationTimer = 0f; ///< Timer for preparation phase
    private float preparationDuration = 8f; ///< 8 seconds preparation time
    
    // ===================== Cursor state management =====================
    private CursorLockMode previousCursorLockState; ///< Previous cursor lock state
    private bool previousCursorVisible; ///< Previous cursor visibility

    // ===================== Current wave/day tracking =====================
    private List<GameObject> activeCustomers = new List<GameObject>(); ///< List of active customers
    private List<string> thiefsCaughtToday = new List<string>(); ///< List of caught thieves today
    private List<string> thiefsEscapedToday = new List<string>(); ///< List of escaped thieves today
    private List<int> disabledCameras = new List<int>(); ///< Track cameras that are offline
    private int thiefCountForDay = 0; ///< Number of thieves for the day
    private int thievesSpawnedToday = 0; ///< Number of thieves spawned today
    private int thievesCaughtToday = 0; ///< Number of thieves caught today
    
    // ===================== Thief wave distribution =====================
    private Dictionary<int, int> thievesPerWave = new Dictionary<int, int>(); ///< wave -> thief count

    // ===================== Day requirements (day number -> required thieves, required score) =====================
    private Dictionary<int, DayRequirement> dayRequirements = new Dictionary<int, DayRequirement>
    {
        {1, new DayRequirement(1, 0)},
        {2, new DayRequirement(2, 100)},
        {3, new DayRequirement(3, 150)},
        {4, new DayRequirement(4, 250)},
        {5, new DayRequirement(5, 300)}
    };

    // ===================== Customer interaction tracking =====================
    private GameObject currentCustomer; ///< Currently selected customer
    private Thief currentThief; ///< Currently selected thief
    private bool awaitingPlayerDecision = false; ///< Is the player making an apprehension decision?
    private bool isSpeedBoostActive = false; ///< Track if 10x speed is active

    // ===================== Final break period after last wave =====================
    private bool isFinalBreak = false; ///< Is the game in the final break period?

    void Start()
    {
        // Ensure popup is hidden at start
        if (apprehensionPopup != null)
            apprehensionPopup.SetActive(false);
            
        // Set up instructions text
        if (instructionsText != null)
            instructionsText.text = "Press Y to Apprehend \n Press N to Cancel";

        // Initialize UI displays
    UpdateUI();
        
        // Hide end panels
        if (endOfDayPanel != null)
            endOfDayPanel.SetActive(false);

        // Initialize pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            
            // Set up button listeners
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeButton);
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartDayButton);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuButton);
        }

        // Initialize post-processing effects (start with first-person)
        EnableFirstPersonEffects();

        // Start the first day
        StartDay();
    }

    void Update()
    {
        // Handle pause menu input (ESC key)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        // Handle keyboard input for apprehension decision (only when game is active and not paused)
        if (gameActive && !isPaused && awaitingPlayerDecision)
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

        // Handle day progression input (works even when game is not active, but not when paused)
        if (!isPaused && Input.GetKeyDown(KeyCode.G))
        {
            if (!dayComplete)
            {
            }
        }
        
    // DEBUG: Press K to toggle 10x speed (works even when game is not active, but not when paused)
        if (!isPaused && Input.GetKeyDown(KeyCode.K))
        {
            isSpeedBoostActive = !isSpeedBoostActive;
            
            if (isSpeedBoostActive)
            {
                Time.timeScale = 10f;
            }
            else
            {
                Time.timeScale = 1f;
            }
        }
        
        // DEBUG: Press J to instantly end day and catch all thieves (works even when game is not active, but not when paused)
        if (!isPaused && Input.GetKeyDown(KeyCode.J))
        {
            
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
        
        if (!isPaused && dayComplete && Input.GetKeyDown(KeyCode.G))
        {
            
            // Hide the end of day panel first
            if (endOfDayPanel != null)
                endOfDayPanel.SetActive(false);
                
            // Check if day was passed to determine action
            bool dayPassed = CheckDayRequirements();
            
            if (!dayPassed)
            {
                // Day failed, restart current day
                RestartDay();
            }
            else if (gameCompleted)
            {
                // Game completed, go to main menu with transition
                
                // Ensure cursor is unlocked for main menu
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.FadeTransition(2f, 1f, () => {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                    });
                }
                else
                {
                    // Fallback if no transition manager
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                }
                return;
            }
            else
            {
                // Day passed, go to next day
                NextDay();
            }
        }

        // Handle preparation phase
        if (isInPreparation)
        {
            preparationTimer += Time.deltaTime;
            
            if (preparationTimer >= preparationDuration)
            {
                // Preparation phase complete, start the actual game
                isInPreparation = false;
                gameActive = true;
                
                StartCoroutine(StartWaveAfterDelay(1f));
            }
            
            UpdateUI(); // Update all UI during preparation
            return; // Don't run normal game logic during preparation
        }

        // Only run game logic when gameActive is true and not paused
        if (!gameActive || isPaused) 
        {
            UpdateUI(); // Still update all UI
            return;
        }

        // Update day timer during active gameplay (not during preparation)
        // Only increment in-game timer during waves (not during breaks)
        if (!isInPreparation && gameActive && isInWave)
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

        // Update UI
    UpdateUI();
        UpdateSunRotation();
    }

    #region Day Management

    /// <summary>
    /// Starts a new day with fresh parameters.
    /// </summary>
    private void StartDay()
    {
        
        // Reset day variables
        dayTimer = 0f;
        waveTimer = 0f;
        restTimer = 0f;
        currentWave = 1;
        isInWave = false;
        isResting = false;
        dayComplete = false;
        gameActive = true;
        isFinalBreak = false;

        // Reset score to 0 at the beginning of each day
        playerScore = 0;

        // Clear tracking lists and disabled cameras from previous day
        thiefsCaughtToday.Clear();
        thiefsEscapedToday.Clear();
        disabledCameras.Clear(); // Clear disabled cameras each day
        thievesSpawnedToday = 0;
        thievesCaughtToday = 0;
        
        // Restore all camera texture objects at start of each day
        if (cameraSystem != null)
            cameraSystem.RestoreAllCameraTextures();

        // Update UI immediately with new day values
    UpdateUI();

        // Reset sun to starting position (night time) for the new day
        ResetSunPosition();

        // Configure day-specific settings (including thief count)
        ConfigureDaySettings();
        
        
        // Randomly distribute thieves across waves
        DistributeThievesAcrossWaves();
        
        // Clear any remaining customers from previous day
        ClearAllCustomers();
        
        // Respawn player at starting position and reset camera
        RespawnPlayer();
        
        // Start preparation phase instead of immediately starting the first wave
        StartPreparationPhase();
        
        // Disable a random camera for days 4 and 5
        DisableRandomCamera();
        
    UpdateUI();
    }

    /// <summary>
    /// Starts the preparation phase before the day begins.
    /// </summary>
    private void StartPreparationPhase()
    {
        isInPreparation = true;
        preparationTimer = 0f;
        gameActive = false; // Prevent normal game logic from running
        
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
                break;
                
            case 2:
                // Day 2: Slightly more challenging
                thiefCountForDay = 2;
                break;
                
            case 3:
                // Day 3: More thieves
                thiefCountForDay = 3;
                break;
                
            case 4:
                // Day 4: High intensity
                thiefCountForDay = 4;
                break;
                
            case 5:
                // Day 5: Maximum challenge
                thiefCountForDay = 5;
                break;
                
            default:
                // Fallback to standard settings
                thiefCountForDay = 1;
                break;
        }
        
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
                break;
            }
            // Randomly select a wave and add a thief
            int randomWaveIndex = Random.Range(0, availableWaves.Count);
            int selectedWave = availableWaves[randomWaveIndex];
            thievesPerWave[selectedWave]++;
            remainingThieves--;
        }
    }

    /// <summary>
    /// Ends the current day and shows summary.
    /// </summary>
    private void EndDay()
    {
        
        dayComplete = true;
        gameActive = false;
        
        // Reset speed boost and time scale
        isSpeedBoostActive = false;
        Time.timeScale = 1f;

        // Stop all wave activities immediately
        isInWave = false;
        isResting = false;

        // Ensure all customers leave the store at end of day
        if (activeCustomers != null && activeCustomers.Count > 0)
        {
            StartCoroutine(MakeCustomersLeave());
        }

        // If any thieves escaped, play the cutscene before showing end-of-day panel
        if (thiefsEscapedToday != null && thiefsEscapedToday.Count > 0)
        {
            StartCoroutine(PlayEscapedThievesCutsceneCoroutine());
        }
        else
        {
            ShowSummaryPanelAfterCutscene();
        }
    }

    // Plays a cutscene for escaped thieves, then shows the summary panel
    private IEnumerator PlayEscapedThievesCutsceneCoroutine()
    {
        
        // Switch to cutscene camera if available
        Camera originalCamera = null;
        Vector3 originalCameraPosition = Vector3.zero;
        
        if (cutsceneCamera != null)
        {
            // Store original camera state
            originalCamera = Camera.main;
            if (originalCamera != null)
                originalCamera.gameObject.SetActive(false);
            
            // Enable cutscene camera and store its starting position
            cutsceneCamera.gameObject.SetActive(true);
            originalCameraPosition = cutsceneCamera.transform.position;
            
            // Hide crosshair during cutscene
            if (crosshairUI != null)
                crosshairUI.SetActive(false);
        }
        
        // Show cutscene background panel
        if (cutsceneBackgroundPanel != null)
            cutsceneBackgroundPanel.SetActive(true);
        
        // Hide non-gameplay UI elements during cutscene (keep Game UI visible)
        // Hide crosshair during cutscene
        if (crosshairUI != null)
            crosshairUI.SetActive(false);
            
        // Hide apprehension popup if visible
        if (apprehensionPopup != null)
            apprehensionPopup.SetActive(false);
            
        // Hide pause menu if visible
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
            
        // Hide UI background panels during cutscene
        if (dayWaveUIBackground != null)
            dayWaveUIBackground.SetActive(false);
        if (timeUIBackground != null)
            timeUIBackground.SetActive(false);
        if (dayTimeUIBackground != null)
            dayTimeUIBackground.SetActive(false);
        if (trustFundThiefUIBackground != null)
            trustFundThiefUIBackground.SetActive(false);
        
        // Spawn escaped thief models in prison cell
        List<GameObject> prisonThieves = new List<GameObject>();
        if (prisonCellSpawnPoint != null && customerPrefabs != null && customerPrefabs.Length > 0)
        {
            for (int i = 0; i < thiefsEscapedToday.Count; i++)
            {
                // Pick a random customer prefab for the escaped thief
                GameObject thiefPrefab = customerPrefabs[Random.Range(0, customerPrefabs.Length)];
                // Position thieves in a semi-circle or grid pattern inside the prison cell
                Vector3 spawnPos;
                if (thiefsEscapedToday.Count == 1)
                {
                    // Single thief at center
                    spawnPos = prisonCellSpawnPoint.position;
                }
                else if (thiefsEscapedToday.Count <= 3)
                {
                    // Line them up horizontally with spacing
                    float spacing = 1.5f;
                    float offset = (thiefsEscapedToday.Count - 1) * spacing * 0.5f;
                    spawnPos = prisonCellSpawnPoint.position + new Vector3(i * spacing - offset, 0, 0);
                }
                else
                {
                    // Arrange in a 2-row grid for more thieves
                    int row = i / 2; // 2 thieves per row
                    int col = i % 2;
                    float xSpacing = 1.5f;
                    float zSpacing = 1.2f;
                    float xOffset = xSpacing * 0.5f; // Center the row of 2
                    spawnPos = prisonCellSpawnPoint.position + new Vector3(col * xSpacing - xOffset, 0, row * zSpacing);
                }
                GameObject prisonThief = Instantiate(thiefPrefab, spawnPos, Quaternion.identity);
                // Make thief face the camera
                if (cutsceneCamera != null)
                {
                    Vector3 lookDirection = cutsceneCamera.transform.position - prisonThief.transform.position;
                    lookDirection.y = 0; // Keep on same Y level
                    if (lookDirection != Vector3.zero)
                        prisonThief.transform.rotation = Quaternion.LookRotation(lookDirection);
                }
                // Disable the thief AI script to prevent movement
                Thief thiefScript = prisonThief.GetComponent<Thief>();
                if (thiefScript != null)
                    thiefScript.enabled = false;
                // Disable NavMeshAgent if present to prevent movement
                UnityEngine.AI.NavMeshAgent navAgent = prisonThief.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                    navAgent.enabled = false;
                prisonThieves.Add(prisonThief);
            }
        }
        
        // Show the cutscene text
        if (cutsceneText != null)
        {
            cutsceneText.text = "These thieves weren't apprehended but were eventually caught by the police";
            cutsceneText.gameObject.SetActive(true);
        }
        
        // Start camera movement backwards (slower and shorter)
        float movementDuration = 6f; // 6 seconds for slower movement
        float elapsedTime = 0f;
        
        while (elapsedTime < movementDuration && cutsceneCamera != null)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / movementDuration;
            
            // Smooth easing for more cinematic movement
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            // Move camera backwards smoothly (shorter distance now)
            Vector3 targetPosition = originalCameraPosition + cutsceneCamera.transform.forward * -cameraMovementDistance;
            cutsceneCamera.transform.position = Vector3.Lerp(originalCameraPosition, targetPosition, easedProgress);
            
            yield return null;
        }
        
        // Hold the final position for a moment
        yield return new WaitForSeconds(1f);
        
        // Clean up prison thieves
        foreach (GameObject thief in prisonThieves)
        {
            if (thief != null)
                Destroy(thief);
        }
        
        // Hide cutscene UI
        if (cutsceneText != null)
            cutsceneText.gameObject.SetActive(false);
        if (cutsceneBackgroundPanel != null)
            cutsceneBackgroundPanel.SetActive(false);
        
        // Restore non-gameplay UI elements after cutscene (Game UI stays visible)
        // Restore crosshair
        if (crosshairUI != null)
            crosshairUI.SetActive(true);
            
        // Restore UI background panels after cutscene
        if (dayWaveUIBackground != null)
            dayWaveUIBackground.SetActive(true);
        if (timeUIBackground != null)
            timeUIBackground.SetActive(true);
        if (dayTimeUIBackground != null)
            dayTimeUIBackground.SetActive(true);
        if (trustFundThiefUIBackground != null)
            trustFundThiefUIBackground.SetActive(true);
        
        // Switch back to original camera
        if (cutsceneCamera != null)
        {
            cutsceneCamera.gameObject.SetActive(false);
            
            // Restore original camera position
            cutsceneCamera.transform.position = originalCameraPosition;
            
            if (originalCamera != null)
                originalCamera.gameObject.SetActive(true);
            
            // Restore crosshair
            if (crosshairUI != null)
                crosshairUI.SetActive(true);
        }
        
        
        // Show the summary panel after cutscene
        ShowSummaryPanelAfterCutscene();
    }

    // Shows the summary panel after the cutscene (or immediately if no cutscene)
    private void ShowSummaryPanelAfterCutscene()
    {
        ClearAllCustomers();
        bool dayPassed = CheckDayRequirements();
        if (currentDay >= 5)
        {
            if (dayPassed)
            {
                gameCompleted = true;
                ShowEndOfDayPanel(true);
            }
            else
            {
                ShowEndOfDayPanel(false);
            }
        }
        else if (dayPassed)
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
        
        
        return scoreRequirementMet;
    }

    /// <summary>
    /// Proceeds to the next day or ends the game.
    /// </summary>
    public void NextDay()
    {
        if (gameCompleted)
        {
            // Game was completed, this should not happen as the button should load main menu
            return;
        }
        if (currentDay >= 5)
        {
            // This should not happen anymore since completion is handled in EndDay()
            return;
        }

        // Use smooth transition for day change
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.FadeTransition(2f, 1f, () => {
                currentDay++;
                StartDay();
            });
        }
        else
        {
            // Fallback without transition
            currentDay++;
            StartDay();
        }
    }

    /// <summary>
    /// Restarts the current day.
    /// </summary>
    public void RestartDay()
    {
        // Use smooth transition for day restart
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.FadeTransition(2f, 1f, () => {
                StartDay();
            });
        }
        else
        {
            // Fallback without transition
            StartDay();
        }
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

        if (isFinalBreak)
        {
            // During final break, just wait for rest duration then end the day directly
            if (restTimer >= restDuration)
            {
                isResting = false;
                isFinalBreak = false;
                // Directly end the day after final break completes
                EndDay();
            }
        }
        else if (restTimer >= restDuration)
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
    // int thievesForThisWave = thievesPerWave.ContainsKey(currentWave) ? thievesPerWave[currentWave] : 0;
        
        isInWave = true;
        isResting = false;
        waveTimer = 0f;
        
        // Start spawning customers for this wave
        StartCoroutine(SpawnWaveCustomers());
        
    UpdateUI();
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
        
        // If no thieves should spawn in this wave, spawn all regular customers
        if (thievesToSpawnThisWave == 0)
        {
            while (customersSpawned < customersPerWave && isInWave)
            {
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
            // Spawn customers according to the randomized order
            while (customersSpawned < customersPerWave && isInWave)
            {
                bool shouldBeThief = customerTypes[customersSpawned];
                if (shouldBeThief)
                {
                    thievesSpawnedToday++;
                }
                SpawnCustomer(shouldBeThief, usedPrefabIndices);
                customersSpawned++;
                yield return new WaitForSeconds(customerSpawnInterval);
            }
        }
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
        
        if (currentWave >= 4)
        {
            // Last wave of the day - start final break
            Debug.Log("Starting final break after wave 4");
            isFinalBreak = true;
            isResting = true;
            restTimer = 0f;
            UpdateUI();
            return;
        }
        
        // Start rest period
        isResting = true;
        restTimer = 0f;
        
    UpdateUI();
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
                // Get the Thief component and force the customer to exit
                Thief thiefScript = customer.GetComponent<Thief>();
                if (thiefScript != null)
                {
                    // Override current behavior and force exit
                    thiefScript.ForceExit(exitPoint);
                }
                
                // Make customer walk to exit (this will now work properly since ForceExit was called)
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

        // Get the NavMeshAgent component
        UnityEngine.AI.NavMeshAgent navAgent = customer.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
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
                    Debug.Log($"Customer {customer.name} reached exit successfully");
                    break;
                }
                
                // Check if customer is stuck (not moving for 3 seconds)
                if (Vector3.Distance(customer.transform.position, lastPosition) < 0.1f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > 3f)
                    {
                        Debug.Log($"Customer {customer.name} appears stuck, teleporting to exit");
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
            
            if (timer >= timeout)
            {
                Debug.Log($"Customer {customer.name} timed out, forcing to exit");
                customer.transform.position = exitPoint.position;
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
            Debug.Log($"Thief escaped! -100 points. Trust Fund: {playerScore}");
            
            // Update UI immediately to reflect the new score
            UpdateUI();
        }
        
        // Remove customer from tracking and destroy
        activeCustomers.Remove(customer);
        
        // Safely destroy customer by disabling NavMeshAgent first
        UnityEngine.AI.NavMeshAgent navAgent = customer.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.enabled = false;
        }
        
        // Stop any running coroutines on the Thief component
        Thief thiefComponent = customer.GetComponent<Thief>();
        if (thiefComponent != null)
        {
            thiefComponent.StopAllCoroutines();
        }
        
        Destroy(customer);
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
            {
                // Disable NavMeshAgent to prevent errors when destroying
                UnityEngine.AI.NavMeshAgent navAgent = customer.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                {
                    navAgent.enabled = false;
                }
                
                // Stop any running coroutines on the Thief component
                Thief thiefComponent = customer.GetComponent<Thief>();
                if (thiefComponent != null)
                {
                    thiefComponent.StopAllCoroutines();
                }
                
                Destroy(customer);
            }
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

        // Reset the popup text to show apprehension options (in case it was changed by camera offline popup)
        TextMeshProUGUI popupText = apprehensionPopup.GetComponentInChildren<TextMeshProUGUI>();
        if (popupText != null)
        {
            popupText.text = "Press Y to Apprehend \n Press N to Cancel";
        }

        // Show the popup
        apprehensionPopup.SetActive(true);
    }

    /// <summary>
    /// Called when player presses Y to apprehend a customer.
    /// </summary>
    private void OnApprehendCustomer()
    {
        if (currentCustomer != null && currentThief != null && cashierBehaviour != null)
        {
            // Always queue the apprehension with the cashier, even if busy
            cashierBehaviour.MoveToApprehendCustomer(currentCustomer, currentThief);
        }
        HideApprehensionUI();
    }
    
    /// <summary>
    /// Fallback method to process apprehension without cashier movement
    /// </summary>
    private void ProcessApprehensionDirectly()
    {
        if (currentCustomer != null && currentThief != null)
        {
            bool isCorrectDecision = currentThief.IsConfirmedThief();
            bool isActualThief = currentThief.IsThief;
            int warningCount = currentThief.GetCurrentWarningCount();
            
            ProcessApprehension(currentCustomer, currentThief, isCorrectDecision, isActualThief, warningCount, thievesSpawnedToday);
        }
    }
    
    /// <summary>
    /// Public method called by CashierBehaviour to process apprehension
    /// </summary>
    public void ProcessApprehension(GameObject customer, Thief thief, bool isCorrectDecision, bool isActualThief, int warningCount, int thievesSpawnedNumber)
    {
        if (customer == null || thief == null) return;
        
        if (isCorrectDecision)
        {
            // Success - correctly apprehended a confirmed thief (3+ warnings)
            playerScore += 100;
            thievesCaughtToday++;
            thiefsCaughtToday.Add($"Thief #{thievesSpawnedNumber} (confirmed, 3 warnings)");
            Debug.Log($"CORRECT! Apprehended confirmed thief. +100 points. Trust Fund: {playerScore}");
            ShowFeedback("CORRECT! Thief Apprehended! +100 points", Color.green);
            
            // Update UI immediately to reflect the new thief count
            UpdateUI();
        }
        else if (isActualThief && warningCount >= 1 && warningCount < 3)
        {
            // Apprehended actual thief but with insufficient warnings
            playerScore -= 50;
            thievesCaughtToday++;
            thiefsCaughtToday.Add($"Thief #{thievesSpawnedNumber} (early arrest, {warningCount} warnings)");
            Debug.Log($"PARTIAL! Apprehended thief early ({warningCount} warnings). -50 points. Trust Fund: {playerScore}");
            ShowFeedback($"EARLY ARREST! Only {warningCount} warnings! -50 points", Color.yellow);
            
            // Update UI immediately to reflect the new thief count
            UpdateUI();
        }
        else if (!isActualThief && warningCount >= 1 && warningCount < 3)
        {
            // Apprehended innocent with some warnings  
            playerScore -= 50;
            Debug.Log($"WRONG! Apprehended innocent with {warningCount} warnings. -50 points. Trust Fund: {playerScore}");
            ShowFeedback($"WRONG! Innocent with {warningCount} warnings! -50 points", Color.yellow);
            
            // Update UI immediately to reflect the new score
            UpdateUI();
        }
        else
        {
            // Completely innocent customer (0 warnings)
            playerScore -= 100;
            Debug.Log($"WRONG! Apprehended innocent customer. -100 points. Trust Fund: {playerScore}");
            ShowFeedback("WRONG! Innocent Customer! -100 points", Color.red);
            
            // Update UI immediately to reflect the new score
            UpdateUI();
        }
        
        // Remove customer from store and tracking
        activeCustomers.Remove(customer);
        
        // Safely destroy customer by disabling NavMeshAgent first
        UnityEngine.AI.NavMeshAgent navAgent = customer.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.enabled = false;
        }
        
        // Stop any running coroutines on the Thief component
        Thief thiefComponent = customer.GetComponent<Thief>();
        if (thiefComponent != null)
        {
            thiefComponent.StopAllCoroutines();
        }
        
        Destroy(customer);
    }
    
    /// <summary>
    /// Get the current number of thieves spawned today (for CashierBehaviour)
    /// </summary>
    public int GetThievesSpawnedToday()
    {
        return thievesSpawnedToday;
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
        /// Updates all UI displays in a consolidated way.
        /// </summary>
        private void UpdateUI()
        {
            // Day
            if (dayText != null)
                dayText.text = $"Day {currentDay}/5";

            // Wave
            if (waveText != null)
            {
                if (isInPreparation)
                {
                    int remainingSeconds = Mathf.CeilToInt(preparationDuration - preparationTimer);
                    waveText.text = $"Waiting: {remainingSeconds}s";
                }
                else if (dayComplete)
                    waveText.text = "Day Complete";
                else if (isInWave)
                    waveText.text = $"Wave {currentWave}/4";
                else if (isResting)
                    waveText.text = "Rest";
                else
                    waveText.text = "Preparing";
            }

            // Time
            if (timeText != null)
            {
                if (isInWave)
                {
                    float waveTimeRemaining = waveDuration - waveTimer;
                    int minutes = Mathf.FloorToInt(waveTimeRemaining / 60f);
                    int seconds = Mathf.FloorToInt(waveTimeRemaining % 60f);
                    timeText.text = $"{minutes:00}:{seconds:00}";
                }
                else if (isResting)
                {
                    float restTimeRemaining = restDuration - restTimer;
                    int restSeconds = Mathf.FloorToInt(restTimeRemaining);
                    timeText.text = $"{restSeconds}s";
                }
                else
                {
                    timeText.text = "Preparing";
                }
            }

            // Day Time
            if (dayTimeText != null)
            {
                float gameTimeProgress = dayTimer / dayDuration;
                float gameHour = 0f + (gameTimeProgress * 9f);
                int displayHour = Mathf.FloorToInt(gameHour);
                int displayMinute = Mathf.FloorToInt((gameHour - displayHour) * 60f);
                string period = "AM";
                int displayHour12 = displayHour;
                if (displayHour == 0) displayHour12 = 12;
                else if (displayHour > 12) { displayHour12 = displayHour - 12; period = "PM"; }
                dayTimeText.text = $"{displayHour12:00}:{displayMinute:00} {period}";

                if (!dayComplete && gameActive && displayHour == 9 && displayMinute >= 1)
                {
                    EndDay();
                }
            }

            // Trust Fund
            if (trustFundText != null)
            {
                if (dayRequirements.ContainsKey(currentDay))
                {
                    var requirements = dayRequirements[currentDay];
                    trustFundText.text = $"{playerScore}/{requirements.score}";
                }
                else
                {
                    trustFundText.text = $"{playerScore}/0";
                }
            }

            // Thief
            if (thiefText != null)
            {
                if (dayRequirements.ContainsKey(currentDay))
                {
                    var requirements = dayRequirements[currentDay];
                    thiefText.text = $"{thievesCaughtToday}/{requirements.thieves}";
                }
                else
                {
                    thiefText.text = $"{thievesCaughtToday}/0";
                }
            }
        }

    /// <summary>
    /// Resets the directional light to its starting position (night time) at the beginning of each day.
    /// </summary>
    private void ResetSunPosition()
    {
        if (directionalLight != null)
        {
            // Set to night time position (-90 degrees, pointing down)
            directionalLight.transform.rotation = Quaternion.Euler(-90f, 30f, 0f);
            
            // Set to night time intensity
            directionalLight.intensity = 0.2f;
            
            Debug.Log("Sun position reset to night time for new day");
        }
    }

    /// <summary>
    /// Updates the directional light rotation to simulate day progression from night to morning.
    /// </summary>
    private void UpdateSunRotation()
    {
        if (directionalLight != null)
        {
            // Calculate progress through the day (0 = start of day, 1 = end of day)
            float dayProgress = dayTimer / dayDuration;
            
            // Rotate from night to morning (roughly -90 degrees to +30 degrees on X-axis)
            // Night starts at -90° (pointing down/dark), morning ends at +30° (sun is up)
            float startAngle = -90f; // Night - sun below horizon
            float endAngle = 30f;    // Morning - sun is up
            float currentAngle = Mathf.Lerp(startAngle, endAngle, dayProgress);
            
            // Apply rotation to the directional light
            directionalLight.transform.rotation = Quaternion.Euler(currentAngle, 30f, 0f);
            
            // Optional: Adjust light intensity based on sun position
            // Darker at night, brighter as sun rises
            float minIntensity = 0.2f; // Night intensity
            float maxIntensity = 1.0f; // Day intensity
            float intensityProgress = Mathf.Clamp01((currentAngle + 90f) / 120f); // Normalize angle to 0-1
            directionalLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, intensityProgress);
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
        summary += $"Required Trust Fund: {req.score} points\n\n";
        
        summary += $"Thieves Spawned: {thievesSpawnedToday}/{req.thieves}\n";
        summary += $"Thieves Caught: {thievesCaughtToday}\n";
        summary += $"Thieves Escaped: {thiefsEscapedToday.Count}\n\n";
        
        if (dayPassed)
        {
            summary += $"=== <color=green>DAY PASSED!</color> ===\n";
        }
        else
        {
            summary += $"=== <color=red>DAY FAILED!</color> ===\n";
        }
        
        if (!dayPassed)
        {
            summary += "You must restart this day.\n\nPress G to restart day.";
        }
        else if (currentDay >= 5)
        {
            summary += "CONGRATULATIONS! You've completed all 5 days!\n\nPress G to return to Main Menu.";
        }
        else
        {
            summary += "Ready for the next day!\n\nPress G to continue to next day.";
        }
        
        return summary;
    }

    /// <summary>
    /// Shows feedback message to player (placeholder for future UI implementation).
    /// </summary>
    /// <param name="message">Feedback message</param>
    /// <param name="color">Message color</param>
    private void ShowFeedback(string message, Color color)
    {
        
        Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>");
    }

    #endregion

    #region Camera Management

    /// <summary>
    /// Disables a random camera for days 4 and 5.
    /// </summary>
    private void DisableRandomCamera()
    {
        if (currentDay < 4 || cameraSystem == null) return;

        // Get total number of cameras from the camera system
        int totalCameras = cameraSystem.GetMonitorCameraCount();
        if (totalCameras <= 0) return;

        // Pick a random camera (disabled cameras list should already be cleared in StartDay)
        int randomCamera = Random.Range(0, totalCameras);
        disabledCameras.Add(randomCamera);
        
        // Deactivate the texture object for the disabled camera
        cameraSystem.SetCameraTextureActive(randomCamera, false);
        
        ShowFeedback($"Security Camera {randomCamera + 1} has malfunctioned!", Color.red);
        Debug.Log($"Camera {randomCamera + 1} has been disabled for Day {currentDay}");
    }

    /// <summary>
    /// Restores all cameras to working condition.
    /// </summary>
    private void RestoreAllCameras()
    {
        disabledCameras.Clear();
        if (apprehensionPopup != null)
            apprehensionPopup.SetActive(false);
        
        // Restore all camera texture objects to active
        if (cameraSystem != null)
            cameraSystem.RestoreAllCameraTextures();
        
        Debug.Log("All cameras restored to working condition");
    }

    /// <summary>
    /// Checks if a camera is currently disabled.
    /// </summary>
    /// <param name="cameraIndex">Index of the camera to check</param>
    /// <returns>True if the camera is disabled</returns>
    public bool IsCameraDisabled(int cameraIndex)
    {
        return disabledCameras.Contains(cameraIndex);
    }

    /// <summary>
    /// Shows the camera offline popup message using the apprehension popup.
    /// </summary>
    public void ShowCameraOfflinePopup()
    {
        // Don't show if we're already waiting for a player decision
        if (awaitingPlayerDecision) return;
        
        if (apprehensionPopup != null)
        {
            // Find the text component in the popup and set it to "Camera Offline"
            TextMeshProUGUI popupText = apprehensionPopup.GetComponentInChildren<TextMeshProUGUI>();
            if (popupText != null)
            {
                popupText.text = "Camera Offline";
            }
            
            apprehensionPopup.SetActive(true);
            // Auto-hide after 2 seconds
            StartCoroutine(HideCameraOfflinePopupAfterDelay(2f));
        }
    }

    /// <summary>
    /// Hides the camera offline popup after a delay.
    /// </summary>
    private System.Collections.IEnumerator HideCameraOfflinePopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (apprehensionPopup != null)
            apprehensionPopup.SetActive(false);
    }

    #endregion

    #region Pause System

    /// <summary>
    /// Pauses the game and shows the pause menu.
    /// </summary>
    private void PauseGame()
    {
        if (dayComplete) return; // Don't allow pause when day is complete
        
        isPaused = true;
        Time.timeScale = 0f; // Pause all time-based operations
        
        // Save current cursor state
        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        
        // Unlock and show cursor for pause menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable player controls
        DisablePlayerControls();
        
        // Disable camera switching
        DisableCameraSwitching();
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
            
        Debug.Log("Game paused - all controls disabled");
    }

    /// <summary>
    /// Resumes the game and hides the pause menu.
    /// </summary>
    private void ResumeGame()
    {
        isPaused = false;
        
        // Restore time scale based on speed boost state
    Time.timeScale = isSpeedBoostActive ? 10f : 1f;
        
        // Restore previous cursor state
        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisible;
        
        // Re-enable player controls
        EnablePlayerControls();
        
        // Re-enable camera switching
        EnableCameraSwitching();
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
            
        Debug.Log("Game resumed - all controls enabled");
    }

    /// <summary>
    /// Disables all player movement and camera controls during pause.
    /// </summary>
    private void DisablePlayerControls()
    {
        if (playerObject == null)
        {
            // Try to find the player object automatically
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                Debug.LogWarning("Player object not found for control disabling!");
                return;
            }
        }

        // Disable FirstPersonController
        var fpController = playerObject.GetComponent<FirstPersonController>();
        if (fpController != null)
        {
            fpController.enabled = false;
        }

        // Disable StarterAssetsInputs
        var starterInputs = playerObject.GetComponent<StarterAssetsInputs>();
        if (starterInputs != null)
        {
            starterInputs.enabled = false;
        }

        // Disable any other movement components as needed
        var characterController = playerObject.GetComponent<CharacterController>();
        if (characterController != null)
        {
            // Don't disable CharacterController as it might cause physics issues
            // Just let FirstPersonController being disabled handle the movement
        }

        Debug.Log("Player controls disabled");
    }

    /// <summary>
    /// Re-enables all player movement and camera controls after pause.
    /// </summary>
    private void EnablePlayerControls()
    {
        if (playerObject == null) return;

        // Re-enable FirstPersonController
        var fpController = playerObject.GetComponent<FirstPersonController>();
        if (fpController != null)
        {
            fpController.enabled = true;
        }

        // Re-enable StarterAssetsInputs
        var starterInputs = playerObject.GetComponent<StarterAssetsInputs>();
        if (starterInputs != null)
        {
            starterInputs.enabled = true;
        }

        Debug.Log("Player controls enabled");
    }

    /// <summary>
    /// Disables camera switching during pause.
    /// </summary>
    private void DisableCameraSwitching()
    {
        if (cameraSystem != null)
        {
            cameraSystem.enabled = false;
        }

        if (playerBehaviour != null)
        {
            playerBehaviour.enabled = false;
        }

        Debug.Log("Camera switching disabled");
    }

    /// <summary>
    /// Re-enables camera switching after pause.
    /// </summary>
    private void EnableCameraSwitching()
    {
        if (cameraSystem != null)
        {
            cameraSystem.enabled = true;
        }

        if (playerBehaviour != null)
        {
            playerBehaviour.enabled = true;
        }

        Debug.Log("Camera switching enabled");
    }

    /// <summary>
    /// Public method to check if the game is currently paused.
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }

    /// <summary>
    /// Respawns the player at the designated spawn point and resets camera to main view.
    /// </summary>
    private void RespawnPlayer()
    {
        if (playerObject == null)
        {
            // Try to find the player object automatically
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                Debug.LogWarning("Player object not found for respawning!");
                return;
            }
        }

        if (playerSpawnPoint == null)
        {
            Debug.LogWarning("Player spawn point not assigned! Player position will not be reset.");
            return;
        }

        // Disable CharacterController temporarily to allow position change
        var characterController = playerObject.GetComponent<CharacterController>();
        bool wasControllerEnabled = false;
        if (characterController != null)
        {
            wasControllerEnabled = characterController.enabled;
            characterController.enabled = false;
        }

        // Reset player position and rotation
        playerObject.transform.position = playerSpawnPoint.position;
        playerObject.transform.rotation = playerSpawnPoint.rotation;

        // Re-enable CharacterController
        if (characterController != null)
        {
            characterController.enabled = wasControllerEnabled;
        }

        // Ensure player is active
        if (!playerObject.activeInHierarchy)
        {
            playerObject.SetActive(true);
        }

        // Return to main camera view if currently viewing monitors
        if (cameraSystem == null)
        {
            cameraSystem = FindFirstObjectByType<CameraSystem>();
        }
        
        if (cameraSystem != null)
        {
            Debug.Log("Calling ReturnToMainCamera from respawn...");
            cameraSystem.ReturnToMainCamera();
            Debug.Log("Camera returned to main view");
        }
        else
        {
            Debug.LogWarning("CameraSystem not found for camera reset!");
        }

        Debug.Log($"Player respawned at position: {playerSpawnPoint.position}");
    }

    /// <summary>
    /// Hides the crosshair UI (called when switching to monitor view).
    /// </summary>
    public void HideCrosshair()
    {
        if (crosshairUI != null)
        {
            crosshairUI.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the crosshair UI (called when returning to main camera view).
    /// </summary>
    public void ShowCrosshair()
    {
        if (crosshairUI != null)
        {
            crosshairUI.SetActive(true);
        }
    }

    /// <summary>
    /// Enables CCTV post-processing effects and disables first-person effects.
    /// </summary>
    public void EnableCCTVEffects()
    {
        if (cctvVolume != null)
        {
            cctvVolume.enabled = true;
            Debug.Log("CCTV post-processing effects enabled");
        }

        if (firstPersonVolume != null)
        {
            firstPersonVolume.enabled = false;
            Debug.Log("First-person post-processing effects disabled");
        }
    }

    /// <summary>
    /// Enables first-person post-processing effects and disables CCTV effects.
    /// </summary>
    public void EnableFirstPersonEffects()
    {
        if (firstPersonVolume != null)
        {
            firstPersonVolume.enabled = true;
            Debug.Log("First-person post-processing effects enabled");
        }

        if (cctvVolume != null)
        {
            cctvVolume.enabled = false;
            Debug.Log("CCTV post-processing effects disabled");
        }
    }

    /// <summary>
    /// Button method for resuming the game from pause menu.
    /// </summary>
    public void OnResumeButton()
    {
        ResumeGame();
    }

    /// <summary>
    /// Button method for returning to main menu from pause menu.
    /// </summary>
    public void OnMainMenuButton()
    {
        Debug.Log("Main Menu button pressed");
        
        // Properly restore game state before loading main menu
        if (isPaused)
        {
            // Don't call ResumeGame() as it will restore game cursor state
            // Instead, manually reset what we need
            isPaused = false;
            Time.timeScale = 1f; // Resume normal time
        }
        
        // Ensure cursor is properly set for main menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Use smooth transition instead of direct scene loading
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(1, 1f); // Main menu with 1s fade
        }
        else
        {
            // Fallback to direct loading
            UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        }
    }

    #endregion

    #region Public Button Methods (for UI)


    /// <summary>
    /// Button method to restart current day.
    /// </summary>
    public void OnRestartDayButton()
    {
        // Ensure all controls are properly restored before restarting
        if (isPaused)
        {
            ResumeGame(); // This handles all control restoration properly
        }
        
        if (endOfDayPanel != null)
            endOfDayPanel.SetActive(false);
            
        RestartDay();
    }

    /// <summary>
    /// Button method to restart the entire game.
    /// </summary>
    public void OnRestartGameButton()
    {
        // Ensure all controls are properly restored before restarting
        if (isPaused)
        {
            ResumeGame(); // This handles all control restoration properly
        }
        
        currentDay = 1;
        playerScore = 0;
        gameCompleted = false; // Reset completion flag
        
        // Restore all cameras when restarting the game
        RestoreAllCameras();
        
        if (endOfDayPanel != null)
            endOfDayPanel.SetActive(false);
        
        // Restore all cameras to working condition
        RestoreAllCameras();
            
        StartDay();
    }

    #endregion
}
