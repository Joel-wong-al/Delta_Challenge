/******************************************************************************
* File: MainMenuManager.cs
* Author: Javier, Zenon, Joel
* Created: 9 August 2025
* Description: Manages the main menu UI and handles user interactions.
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

    void Start()
    {
        // Store original camera rotation
        if (backgroundCamera != null)
        {
            originalRotation = backgroundCamera.transform.eulerAngles;
        }

        // Automatically assign button functions
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);
            
        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(StartTutorial);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
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
    }


    
    public void StartGame()
    {
        SceneManager.LoadScene(1); // Loads scene at index 1 in Build Settings
    }
    
    /// <summary>
    /// Loads the tutorial scene
    /// </summary>
    public void StartTutorial()
    {
        Debug.Log("Loading tutorial...");
        SceneManager.LoadScene(2); // Loads tutorial scene at index 2 in Build Settings
    }
    
    /// <summary>
    /// Quits the application
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
