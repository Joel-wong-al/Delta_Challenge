using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor utility to create a SceneTransitionManager prefab.
/// Use: GameObject > Scene Transition Manager
/// </summary>
public class CreateSceneTransitionManager
{
    [MenuItem("GameObject/Scene Transition Manager")]
    public static void CreateTransitionManager()
    {
        // Create the main GameObject
        GameObject transitionManagerGO = new GameObject("SceneTransitionManager");
        
        // Add the SceneTransitionManager component
        SceneTransitionManager manager = transitionManagerGO.AddComponent<SceneTransitionManager>();
        
        // Create Canvas
        GameObject canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transitionManagerGO.transform);
        
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Create Fade Image
        GameObject fadeImageGO = new GameObject("FadeImage");
        fadeImageGO.transform.SetParent(canvas.transform, false);
        
        Image fadeImage = fadeImageGO.AddComponent<Image>();
        fadeImage.color = Color.black;
        
        // Set image to fill screen
        RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Set initial alpha to 0 (transparent)
        Color initialColor = fadeImage.color;
        initialColor.a = 0f;
        fadeImage.color = initialColor;
        
        // Assign references using reflection (since fields are private)
        var fadeImageField = typeof(SceneTransitionManager).GetField("fadeImage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fadeImageField != null)
            fadeImageField.SetValue(manager, fadeImage);
            
        var fadeCanvasField = typeof(SceneTransitionManager).GetField("fadeCanvas", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fadeCanvasField != null)
            fadeCanvasField.SetValue(manager, canvas);
        
        // Select the created object
        Selection.activeGameObject = transitionManagerGO;
        
        Debug.Log("SceneTransitionManager created successfully!");
        Debug.Log("This GameObject will persist across all scenes and handle fade transitions.");
        Debug.Log("You can now use SceneTransitionManager.Instance.TransitionToScene() in your scripts.");
    }
}
#endif
