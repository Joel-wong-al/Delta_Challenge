using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashScreenManager : MonoBehaviour
{
    [Header("Video Player Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject videoCanvas; // Canvas containing the video player
    
    [Header("Scene Management")]
    [SerializeField] private int mainMenuSceneIndex = 1; // Index of the main menu scene (MainMenu.unity)
    [SerializeField] private bool allowSkip = true; // Allow player to skip the splash screen
    [SerializeField] private KeyCode skipKey = KeyCode.Space; // Key to skip the splash screen
    
    [Header("Audio Settings")]
    [SerializeField] private bool playAudio = true; // Whether to play video audio
    [SerializeField] private AudioSource audioSource; // Optional separate audio source
    
    [Header("Fade Settings")]
    [SerializeField] private bool useFadeTransition = true;
    [SerializeField] private CanvasGroup fadeCanvasGroup; // Canvas group for fade effect
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;
    
    private bool hasVideoFinished = false;
    private bool isTransitioning = false;
    
    void Start()
    {
        // Ensure cursor is hidden during splash screen
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Initialize video player if not assigned
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
            
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer not found! Please assign a VideoPlayer component.");
            LoadMainMenu();
            return;
        }
        
        // Set up video player
        SetupVideoPlayer();
        
        // Start the splash screen sequence
        StartCoroutine(PlaySplashScreen());
    }
    
    void Update()
    {
        // Allow skipping if enabled
        if (allowSkip && !isTransitioning && Input.GetKeyDown(skipKey))
        {
            Debug.Log("Splash screen skipped by user");
            SkipToMainMenu();
        }
        
        // Check if video has finished playing
        if (!hasVideoFinished && videoPlayer != null && !videoPlayer.isPlaying && videoPlayer.frame > 0)
        {
            hasVideoFinished = true;
            StartCoroutine(TransitionToMainMenu());
        }
    }
    
    /// <summary>
    /// Sets up the video player with appropriate settings.
    /// </summary>
    private void SetupVideoPlayer()
    {
        if (videoPlayer == null) return;
        
        // Configure video player settings
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        
        // Set up audio
        if (playAudio && audioSource != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }
        else if (playAudio)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }
        
        // Subscribe to events
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
        
        Debug.Log("Video player configured successfully");
    }
    
    /// <summary>
    /// Starts the splash screen sequence with optional fade-in.
    /// </summary>
    private IEnumerator PlaySplashScreen()
    {
        Debug.Log("Starting splash screen sequence");
        
        // Fade in if enabled
        if (useFadeTransition && fadeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeIn());
        }
        
        // Prepare and play the video
        if (videoPlayer.clip != null)
        {
            videoPlayer.Prepare();
            
            // Wait for video to be prepared
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }
            
            videoPlayer.Play();
            Debug.Log("Video playback started");
        }
        else
        {
            Debug.LogWarning("No video clip assigned to VideoPlayer. Transitioning to main menu immediately.");
            yield return new WaitForSeconds(2f); // Show for 2 seconds even without video
            StartCoroutine(TransitionToMainMenu());
        }
    }
    
    /// <summary>
    /// Handles the transition to the main menu with optional fade-out.
    /// </summary>
    private IEnumerator TransitionToMainMenu()
    {
        if (isTransitioning) yield break;
        
        isTransitioning = true;
        Debug.Log("Starting transition to main menu");
        
        // Stop video playback
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        // Fade out if enabled
        if (useFadeTransition && fadeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeOut());
        }
        
        // Load main menu scene
        LoadMainMenu();
    }
    
    /// <summary>
    /// Skips the splash screen and goes directly to main menu.
    /// </summary>
    private void SkipToMainMenu()
    {
        if (isTransitioning) return;
        
        StopAllCoroutines();
        StartCoroutine(TransitionToMainMenu());
    }
    
    /// <summary>
    /// Loads the main menu scene.
    /// </summary>
    private void LoadMainMenu()
    {
        Debug.Log($"Loading main menu scene (index: {mainMenuSceneIndex})");
        
        // Restore cursor state
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Use smooth transition if available, otherwise direct load
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(mainMenuSceneIndex, 1f);
        }
        else
        {
            // Load the main menu scene directly
            SceneManager.LoadScene(mainMenuSceneIndex);
        }
    }
    
    /// <summary>
    /// Fades in the splash screen.
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;
        
        fadeCanvasGroup.alpha = 0f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// Fades out the splash screen.
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;
        
        fadeCanvasGroup.alpha = 1f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / fadeOutDuration));
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 0f;
    }
    
    #region Video Player Events
    
    /// <summary>
    /// Called when the video is prepared and ready to play.
    /// </summary>
    private void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log($"Video prepared: {vp.clip.name}, Duration: {vp.clip.length:F2} seconds");
    }
    
    /// <summary>
    /// Called when the video reaches its end.
    /// </summary>
    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("Video playback finished");
        hasVideoFinished = true;
        
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToMainMenu());
        }
    }
    
    /// <summary>
    /// Called when a video error occurs.
    /// </summary>
    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"Video playback error: {message}");
        
        // On error, proceed to main menu after a brief delay
        StartCoroutine(DelayedMainMenuLoad());
    }
    
    /// <summary>
    /// Loads main menu after a delay when an error occurs.
    /// </summary>
    private IEnumerator DelayedMainMenuLoad()
    {
        yield return new WaitForSeconds(2f);
        LoadMainMenu();
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Clean up video player events
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }
}
