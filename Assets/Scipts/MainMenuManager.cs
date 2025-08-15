/******************************************************************************
* File: MainMenuManager.cs
* Author: Javier, Zenon, Joel
* Created: 9 August 2025
* Description: Manages the main menu UI and handles user interactions, as well as handles the game tutorial.
******************************************************************************/

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField]
    private GameObject mainMenuUI;
    [SerializeField]
    private Button startButton;
    [SerializeField]
    private Button tutorialButton;
    [SerializeField]
    private Button quitButton;

    [Header("Tutorial Slideshow")]
    [SerializeField]
    private GameObject tutorialPanel; // Panel that contains the slideshow
    [SerializeField]
    private UnityEngine.UI.Image tutorialImage; // Image component to display slides
    [SerializeField]
    private Sprite[] tutorialSlides; // Array of tutorial images
    // Slide counter text removed

    private int currentSlideIndex = 0;
    private bool inTutorialMode = false;

    [Header("Background Camera Animation")]
    [SerializeField]
    private Camera backgroundCamera;
    [SerializeField]
    private bool enableCameraRocking = true;
    [SerializeField]
    private float rockingSpeed = 0.5f; // How fast the camera rocks
    [SerializeField]
    private float rockingAmount = 2f; // How much the camera rocks (in degrees)
    [SerializeField]
    private Vector3 rockingAxis = Vector3.forward; // Axis to rock around (Z = roll, X = pitch, Y = yaw)

    private Vector3 originalRotation;
    private float rockingTimer;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip startGameClip;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip mainMenuBgmClip;

    void Start()
    {
        // Store original camera rotation
        if (backgroundCamera != null)
        {
            originalRotation = backgroundCamera.transform.eulerAngles;
        }

        // Initialize tutorial slideshow
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        // Assign button functions
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);
            
        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(StartTutorial);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // Play main menu BGM if assigned
        if (bgmSource != null && mainMenuBgmClip != null)
        {
            bgmSource.clip = mainMenuBgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    void Update()
    {
        // Handle camera rocking animation
        if (enableCameraRocking && backgroundCamera != null)
        {
            rockingTimer += Time.deltaTime * rockingSpeed;
            
            // Create smooth rocking motion using sine wave
            float rockOffset = Mathf.Sin(rockingTimer) * rockingAmount;
            
            // Apply rocking to the specified axis
            Vector3 newRotation = originalRotation + (rockingAxis * rockOffset);
            backgroundCamera.transform.eulerAngles = newRotation;
        }

        // Handle tutorial slideshow navigation
        if (inTutorialMode && Input.GetKeyDown(KeyCode.G))
        {
            NextSlide();
        }

        // Handle ESC key to exit tutorial
        if (inTutorialMode && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitTutorial();
        }
    }


    
    public void StartGame()
    {
        // Play start game sound
        if (startGameClip != null && audioSource != null)
            audioSource.PlayOneShot(startGameClip);

        // Use smooth transition instead of direct scene loading
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(2, 1.5f); // GameScene with 1.5s fade
        }
        else
        {
            SceneManager.LoadScene(2); // Fallback to direct loading
        }
    }
    
    /// <summary>
    /// Shows the tutorial slideshow instead of loading tutorial scene
    /// </summary>
    public void StartTutorial()
    {
        if (tutorialSlides == null || tutorialSlides.Length == 0)
        {
            Debug.LogWarning("No tutorial slides assigned!");
            return;
        }

        // Hide menu buttons instead of entire UI to keep tutorial panel visible
        if (startButton != null)
            startButton.gameObject.SetActive(false);
        if (tutorialButton != null)
            tutorialButton.gameObject.SetActive(false);
        if (quitButton != null)
            quitButton.gameObject.SetActive(false);
        
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        // Reset to first slide
        currentSlideIndex = 0;
        inTutorialMode = true;
        
        // Display first slide
        DisplayCurrentSlide();
    }

    /// <summary>
    /// Advances to the next slide or exits tutorial if on last slide
    /// </summary>
    private void NextSlide()
    {
        currentSlideIndex++;
        
        if (currentSlideIndex >= tutorialSlides.Length)
        {
            // Reached the end, exit tutorial
            ExitTutorial();
        }
        else
        {
            // Display next slide
            DisplayCurrentSlide();
        }
    }

    /// <summary>
    /// Displays the current slide and updates UI elements
    /// </summary>
    private void DisplayCurrentSlide()
    {
        if (tutorialImage != null && tutorialSlides != null && currentSlideIndex < tutorialSlides.Length)
        {
            tutorialImage.sprite = tutorialSlides[currentSlideIndex];
        }
    }

    /// <summary>
    /// Exits the tutorial and returns to main menu
    /// </summary>
    private void ExitTutorial()
    {
        inTutorialMode = false;
        currentSlideIndex = 0;

        // Hide tutorial panel and show menu buttons again
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        
        if (startButton != null)
            startButton.gameObject.SetActive(true);
        if (tutorialButton != null)
            tutorialButton.gameObject.SetActive(true);
        if (quitButton != null)
            quitButton.gameObject.SetActive(true);
    }
    
    /// <summary>
    /// Quits the application
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
