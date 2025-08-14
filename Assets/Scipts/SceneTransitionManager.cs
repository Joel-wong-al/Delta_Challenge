/******************************************************************************
 * File: SceneTransitionManager.cs
 * Author: Javier, Zenon, Joel
 * Created: [Insert Date]
 * Description: Handles scene transitions with fade effects and optional audio fading
 *              for the Delta Challenge game. Provides singleton access and smooth
 *              transitions between scenes and gameplay states.
 ******************************************************************************/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Handles scene transitions with fade effects and optional audio fading for the Delta Challenge game.
/// Provides singleton access and smooth transitions between scenes and gameplay states.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance for global access.
    /// </summary>
    public static SceneTransitionManager Instance { get; private set; }

    // ===================== Fade Settings =====================
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage; ///< UI Image used for fade overlay.
    [SerializeField] private Canvas fadeCanvas; ///< Canvas containing the fade image.
    [SerializeField] private float defaultFadeDuration = 1f; ///< Default duration for fade transitions.
    [SerializeField] private Color fadeColor = Color.black; ///< Color used for fade overlay.
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); ///< Curve for fade animation.

    // ===================== Audio Settings =====================
    [Header("Audio Settings")]
    [SerializeField] private bool fadeAudioDuringTransition = true; ///< If true, fades audio during transitions.
    [SerializeField] private float audioFadeDuration = 0.5f; ///< Duration for audio fade in/out.

    private bool isTransitioning = false; ///< True if a scene or fade transition is in progress.
    private Coroutine currentTransition; ///< Reference to the currently running transition coroutine.
    private AudioSource[] allAudioSources; ///< All audio sources in the scene for fading.
    private float[] originalAudioVolumes; ///< Original volumes of audio sources for restoration.

    

    /// <summary>
    /// Unity Awake method. Sets up singleton instance and initializes fade UI.
    /// </summary>
    void Awake()
    {
        // Singleton pattern - ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFadeUI();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    

    /// <summary>
    /// Unity Start method. Ensures fade UI is ready and not blocking input.
    /// </summary>
    void Start()
    {
        // Only fade out if we're not currently in a transition
        // This prevents interference when the manager persists across scenes
        if (fadeImage != null && !isTransitioning)
        {
            // Start with a clear screen (no fade effect on initial load)
            ClearFadeInstant();
        }

        // Ensure the fade canvas doesn't block interactions when not transitioning
        SetCanvasInteractable(false);
    }
    

    /// <summary>
    /// Initializes the fade UI components and ensures correct hierarchy and state.
    /// </summary>
    private void InitializeFadeUI()
    {
        // Create fade canvas if it doesn't exist or was destroyed
        if (fadeCanvas == null)
        {
            CreateFadeCanvas();
        }

        // Ensure canvas is always on top and properly configured
        if (fadeCanvas != null)
        {
            fadeCanvas.sortingOrder = 1000;
            fadeCanvas.planeDistance = 0.1f;

            // Ensure the fade canvas is a child of this persistent GameObject
            if (fadeCanvas.transform.parent != transform)
            {
                fadeCanvas.transform.SetParent(transform, false);
            }
        }

        // Set initial fade state (fully transparent)
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        }
    }
    

    /// <summary>
    /// Creates a fade canvas and image if they don't exist, and configures their properties.
    /// </summary>
    private void CreateFadeCanvas()
    {
        // Create canvas
        GameObject canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform);

        fadeCanvas = canvasGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // Make sure the fade canvas doesn't block interactions by default
        GraphicRaycaster raycaster = canvasGO.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
            raycaster.enabled = false;

        // Create fade image
        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(fadeCanvas.transform, false);

        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = fadeColor;

        // Make image fill the screen
        RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Set initial alpha to 0 (fully transparent)
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
    }
    
    #region Public Transition Methods
    
    /// <summary>
    /// Transitions to a new scene with fade effect.
    /// </summary>
    /// <param name="sceneIndex">Index of the scene to load</param>
    /// <param name="fadeDuration">Duration of fade effect</param>
    public void TransitionToScene(int sceneIndex, float fadeDuration = -1f)
    {
        if (isTransitioning) return;
        
        float duration = fadeDuration > 0 ? fadeDuration : defaultFadeDuration;
        StartCoroutine(SceneTransitionCoroutine(sceneIndex, duration));
    }
    
    /// <summary>
    /// Transitions to a new scene with fade effect.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load</param>
    /// <param name="fadeDuration">Duration of fade effect</param>
    public void TransitionToScene(string sceneName, float fadeDuration = -1f)
    {
        if (isTransitioning) return;
        
        float duration = fadeDuration > 0 ? fadeDuration : defaultFadeDuration;
        StartCoroutine(SceneTransitionCoroutine(sceneName, duration));
    }
    
    /// <summary>
    /// Performs a fade to black and back for day transitions or other events.
    /// </summary>
    /// <param name="duration">Total duration of the fade effect</param>
    /// <param name="holdTime">Time to hold the black screen</param>
    /// <param name="onFadeComplete">Callback when fade is complete</param>
    public void FadeTransition(float duration = -1f, float holdTime = 0.5f, System.Action onFadeComplete = null)
    {
        if (isTransitioning) return;
        
        float fadeDuration = duration > 0 ? duration : defaultFadeDuration;
        StartCoroutine(FadeTransitionCoroutine(fadeDuration, holdTime, onFadeComplete));
    }
    
    /// <summary>
    /// Instantly fades to black.
    /// </summary>
    public void FadeToBlackInstant()
    {
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        }
    }
    
    /// <summary>
    /// Instantly clears the fade.
    /// </summary>
    public void ClearFadeInstant()
    {
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        }
    }
    
    #endregion
    
    #region Coroutines
    
    /// <summary>
    /// Handles scene transition with fade effect.
    /// </summary>
    private IEnumerator SceneTransitionCoroutine(int sceneIndex, float duration)
    {
        isTransitioning = true;
        SetCanvasInteractable(true); // Enable canvas during transition
        
        // Ensure fade UI is properly set up before starting transition
        if (fadeImage == null || fadeCanvas == null)
        {
            InitializeFadeUI();
        }
        
        // Fade to black (fade in)
        yield return StartCoroutine(FadeIn(duration * 0.5f));
        
        // Store current fade state before loading scene
        float currentAlpha = fadeImage != null ? fadeImage.color.a : 1f;
        
        // Load new scene
        SceneManager.LoadScene(sceneIndex);
        
        // Wait for scene to load and initialize
        yield return null;
        yield return null;
        
        // Re-ensure fade UI exists after scene load and restore fade state
        if (fadeImage == null || fadeCanvas == null)
        {
            InitializeFadeUI();
        }
        
        // Restore the black screen state after scene load
        if (fadeImage != null)
        {
            Color blackColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, currentAlpha);
            fadeImage.color = blackColor;
        }
        
        // Re-enable canvas blocking during fade out
        SetCanvasInteractable(true);
        
        // Fade from black (fade out)
        yield return StartCoroutine(FadeOut(duration * 0.5f));
        
        SetCanvasInteractable(false); // Disable canvas after transition
        isTransitioning = false;
    }
    
    /// <summary>
    /// Handles scene transition with fade effect by scene name.
    /// </summary>
    private IEnumerator SceneTransitionCoroutine(string sceneName, float duration)
    {
        isTransitioning = true;
        SetCanvasInteractable(true); // Enable canvas during transition
        
        // Ensure fade UI is properly set up before starting transition
        if (fadeImage == null || fadeCanvas == null)
        {
            InitializeFadeUI();
        }
        
        // Fade to black (fade in)
        yield return StartCoroutine(FadeIn(duration * 0.5f));
        
        // Store current fade state before loading scene
        float currentAlpha = fadeImage != null ? fadeImage.color.a : 1f;
        
        // Load new scene
        SceneManager.LoadScene(sceneName);
        
        // Wait for scene to load and initialize
        yield return null;
        yield return null;
        
        // Re-ensure fade UI exists after scene load and restore fade state
        if (fadeImage == null || fadeCanvas == null)
        {
            InitializeFadeUI();
        }
        
        // Restore the black screen state after scene load
        if (fadeImage != null)
        {
            Color blackColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, currentAlpha);
            fadeImage.color = blackColor;
        }
        
        // Re-enable canvas blocking during fade out
        SetCanvasInteractable(true);
        
        // Fade from black (fade out)
        yield return StartCoroutine(FadeOut(duration * 0.5f));
        
        SetCanvasInteractable(false); // Disable canvas after transition
        isTransitioning = false;
    }
    
    /// <summary>
    /// Handles fade transition without changing scenes.
    /// </summary>
    private IEnumerator FadeTransitionCoroutine(float duration, float holdTime, System.Action onFadeComplete)
    {
        isTransitioning = true;
        SetCanvasInteractable(true); // Enable canvas during transition
        
        // Fade to black
        yield return StartCoroutine(FadeIn(duration * 0.5f));
        
        // Hold black screen
        yield return new WaitForSeconds(holdTime);
        
        // Execute callback at peak of fade
        onFadeComplete?.Invoke();
        
        // Fade back in
        yield return StartCoroutine(FadeOut(duration * 0.5f));
        
        SetCanvasInteractable(false); // Disable canvas after transition
        isTransitioning = false;
    }
    
    /// <summary>
    /// Fades to black (fade in).
    /// </summary>
    private IEnumerator FadeIn(float duration)
    {
        if (fadeImage == null) yield break;
        
        // Store and fade audio if enabled
        if (fadeAudioDuringTransition)
        {
            StartCoroutine(FadeAudio(false, audioFadeDuration));
        }
        
        // Use reliable time tracking
        float startTime = Time.unscaledTime;
        float endTime = startTime + duration;
        
        Color startColor = fadeImage.color;
        Color targetColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        
        while (Time.unscaledTime < endTime && fadeImage != null)
        {
            float currentTime = Time.unscaledTime;
            float elapsedTime = currentTime - startTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
            float curveValue = fadeCurve.Evaluate(normalizedTime);
            
            fadeImage.color = Color.Lerp(startColor, targetColor, curveValue);
            yield return null;
        }
        
        if (fadeImage != null)
        {
            fadeImage.color = targetColor;
        }
    }
    
    /// <summary>
    /// Fades from black (fade out).
    /// </summary>
    private IEnumerator FadeOut(float duration)
    {
        if (fadeImage == null) 
        {
            InitializeFadeUI();
            if (fadeImage == null) yield break;
        }
        
        // Use reliable time tracking
        float startTime = Time.unscaledTime;
        float endTime = startTime + duration;
        
        Color startColor = fadeImage.color;
        Color targetColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        
        while (Time.unscaledTime < endTime && fadeImage != null)
        {
            float currentTime = Time.unscaledTime;
            float elapsedTime = currentTime - startTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
            float curveValue = fadeCurve.Evaluate(normalizedTime);
            
            fadeImage.color = Color.Lerp(startColor, targetColor, curveValue);
            yield return null;
        }
        
        if (fadeImage != null)
        {
            fadeImage.color = targetColor;
        }
        
        // Restore audio if enabled
        if (fadeAudioDuringTransition)
        {
            StartCoroutine(FadeAudio(true, audioFadeDuration));
        }
    }
    
    /// <summary>
    /// Fades audio in or out.
    /// </summary>
    private IEnumerator FadeAudio(bool fadeIn, float duration)
    {
        // Get all audio sources in the scene
    allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        
        if (allAudioSources.Length == 0) yield break;
        
        // Store original volumes if fading out
        if (!fadeIn && originalAudioVolumes == null)
        {
            originalAudioVolumes = new float[allAudioSources.Length];
            for (int i = 0; i < allAudioSources.Length; i++)
            {
                originalAudioVolumes[i] = allAudioSources[i].volume;
            }
        }
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = elapsedTime / duration;
            
            for (int i = 0; i < allAudioSources.Length; i++)
            {
                if (allAudioSources[i] != null)
                {
                    if (fadeIn && originalAudioVolumes != null)
                    {
                        // Fade back to original volume
                        allAudioSources[i].volume = Mathf.Lerp(0f, originalAudioVolumes[i], normalizedTime);
                    }
                    else if (!fadeIn)
                    {
                        // Fade to silence
                        allAudioSources[i].volume = Mathf.Lerp(originalAudioVolumes[i], 0f, normalizedTime);
                    }
                }
            }
            
            yield return null;
        }
        
        // Clean up original volumes array after fade in
        if (fadeIn)
        {
            originalAudioVolumes = null;
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Enables or disables the fade canvas to prevent UI blocking.
    /// </summary>
    /// <param name="interactable">Whether the canvas should block interactions</param>
    private void SetCanvasInteractable(bool interactable)
    {
        if (fadeCanvas != null)
        {
            GraphicRaycaster raycaster = fadeCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = interactable;
            }
        }
    }
    
    /// <summary>
    /// Checks if a transition is currently in progress.
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
    
    /// <summary>
    /// Gets the current fade alpha value.
    /// </summary>
    public float GetFadeAlpha()
    {
        return fadeImage != null ? fadeImage.color.a : 0f;
    }
    
    #endregion
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
