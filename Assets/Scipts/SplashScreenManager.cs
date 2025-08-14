﻿/******************************************************************************
 * File: SplashScreenManager.cs
 * Author: Javier, Zenon, Joel
 * Created: 10 August 2025
 * Description: Manages the splash screen video, fade transitions, and skip logic
 *              for the game startup. Handles video playback,
 *              user skip input, and smooth scene transitions to the main menu.
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages the splash screen video, fade transitions, and skip logic for the Delta Challenge game startup.
/// Handles video playback, user skip input, and smooth scene transitions to the main menu.
/// </summary>
public class SplashScreenManager : MonoBehaviour
{
    // ===================== Video Player Settings =====================
    [Header("Video Player Settings")]
    [SerializeField] private VideoPlayer videoPlayer; ///< The VideoPlayer component for splash video playback.
    [SerializeField] private GameObject videoCanvas; ///< Canvas containing the video player UI.

    // ===================== Scene Management =====================
    [Header("Scene Management")]
    [SerializeField] private KeyCode skipKey = KeyCode.Space; ///< Key used to skip the splash screen.

    // ===================== Audio Settings =====================
    [Header("Audio Settings")]
    [SerializeField] private bool playAudio = true; ///< Whether to play audio from the splash video.
    [SerializeField] private AudioSource audioSource; ///< Optional separate AudioSource for video audio output.

    // ===================== Fade Settings =====================
    [Header("Fade Settings")]
    [SerializeField] private bool useFadeTransition = true; ///< If true, uses fade-in/out transitions.
    [SerializeField] private CanvasGroup fadeCanvasGroup; ///< CanvasGroup for controlling fade effect.
    [SerializeField] private float fadeInDuration = 1f; ///< Duration of fade-in at splash start.
    [SerializeField] private float fadeOutDuration = 1f; ///< Duration of fade-out before main menu.

    private bool hasVideoFinished = false; ///< True if the splash video has finished playing.
    private bool isTransitioning = false; ///< True if a transition to the main menu is in progress.


    /// <summary>
    /// Unity Start method. Initializes video player, sets up events, and starts splash screen sequence.
    /// </summary>
    void Start()
    {
        // Hide cursor during splash screen
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Auto-assign VideoPlayer if not set in inspector
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        // If no VideoPlayer is found, skip splash and go to main menu
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer not found! Please assign a VideoPlayer component.");
            LoadMainMenu();
            return;
        }

        // Configure video player and subscribe to events
        SetupVideoPlayer();

        // Start the splash screen sequence (with fade-in and video playback)
        StartCoroutine(PlaySplashScreen());
    }


    /// <summary>
    /// Unity Update method. Handles user skip input and checks for video completion.
    /// </summary>
    void Update()
    {
        // Allow skipping splash screen if not already transitioning
        if (!isTransitioning && Input.GetKeyDown(skipKey))
        {
            SkipToMainMenu();
        }

        // If video has finished, start transition to main menu
        if (!hasVideoFinished && videoPlayer != null && !videoPlayer.isPlaying && videoPlayer.frame > 0)
        {
            hasVideoFinished = true;
            StartCoroutine(TransitionToMainMenu());
        }
    }


    /// <summary>
    /// Configures the VideoPlayer component and subscribes to relevant events.
    /// </summary>
    private void SetupVideoPlayer()
    {
        if (videoPlayer == null) return;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        // Configure audio output
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
        // Subscribe to video events
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
    }


    /// <summary>
    /// Coroutine to play the splash screen video, including fade-in and video preparation.
    /// </summary>
    private IEnumerator PlaySplashScreen()
    {
        // Fade in splash screen if enabled
        if (useFadeTransition && fadeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeIn());
        }
        // Prepare and play the video if assigned
        if (videoPlayer.clip != null)
        {
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }
            videoPlayer.Play();
        }
        else
        {
            // No video assigned, wait briefly then transition
            yield return new WaitForSeconds(2f);
            StartCoroutine(TransitionToMainMenu());
        }
    }


    /// <summary>
    /// Handles the transition to the main menu, including fade-out and stopping video.
    /// </summary>
    private IEnumerator TransitionToMainMenu()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        // Stop video playback if still playing
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        // Fade out splash screen if enabled
        if (useFadeTransition && fadeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeOut());
        }
        // Load the main menu scene
        LoadMainMenu();
    }


    /// <summary>
    /// Skips the splash screen and immediately transitions to the main menu.
    /// </summary>
    private void SkipToMainMenu()
    {
        if (isTransitioning) return;
        StopAllCoroutines();
        StartCoroutine(TransitionToMainMenu());
    }


    /// <summary>
    /// Loads the main menu scene, using SceneTransitionManager if available.
    /// </summary>
    private void LoadMainMenu()
    {
        // Restore cursor for main menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        // Use smooth transition if available, otherwise load directly
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(1, 1f);
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }


    /// <summary>
    /// Coroutine to fade in the splash screen using the CanvasGroup alpha.
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
    /// Coroutine to fade out the splash screen using the CanvasGroup alpha.
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

    // ===================== Video Player Event Handlers =====================

    /// <summary>
    /// Called when the video is prepared and ready to play (optional handling).
    /// </summary>
    private void OnVideoPrepared(VideoPlayer vp) { /* Optionally handle video prepared */ }

    /// <summary>
    /// Called when the video finishes playing. Triggers transition if not already transitioning.
    /// </summary>
    private void OnVideoFinished(VideoPlayer vp) { if (!isTransitioning) StartCoroutine(TransitionToMainMenu()); }

    /// <summary>
    /// Called when a video error occurs. Proceeds to main menu.
    /// </summary>
    private void OnVideoError(VideoPlayer vp, string message) { StartCoroutine(TransitionToMainMenu()); }
}
