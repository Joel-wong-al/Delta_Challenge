using UnityEngine;

/// <summary>
/// Debug utility to help troubleshoot cursor and scene transition issues.
/// Press F1 to unlock cursor, F2 to check transition manager status.
/// </summary>
public class DebugHelper : MonoBehaviour
{
    void Update()
    {
        // F1: Force unlock cursor (emergency fix)
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("F1 pressed - Force unlocking cursor");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log($"Cursor state: visible={Cursor.visible}, lockState={Cursor.lockState}");
        }
        
        // F2: Check SceneTransitionManager status
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("F2 pressed - Checking SceneTransitionManager status");
            if (SceneTransitionManager.Instance == null)
            {
                Debug.LogWarning("SceneTransitionManager.Instance is NULL - transitions will use direct loading");
            }
            else
            {
                Debug.Log($"SceneTransitionManager found. IsTransitioning: {SceneTransitionManager.Instance.IsTransitioning()}");
                Debug.Log($"Fade Alpha: {SceneTransitionManager.Instance.GetFadeAlpha()}");
            }
        }
        
        // F3: Force load main menu (emergency scene switch)
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("F3 pressed - Force loading main menu");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        }
        
        // F4: Force load game scene (emergency scene switch)
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Debug.Log("F4 pressed - Force loading game scene");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            UnityEngine.SceneManagement.SceneManager.LoadScene(2);
        }
    }
    
    void OnGUI()
    {
        // Show debug info in top-left corner
        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== DEBUG INFO ===");
        GUILayout.Label($"Cursor Visible: {Cursor.visible}");
        GUILayout.Label($"Cursor Lock: {Cursor.lockState}");
        GUILayout.Label($"Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        
        if (SceneTransitionManager.Instance != null)
        {
            GUILayout.Label($"Transition Manager: OK");
            GUILayout.Label($"Is Transitioning: {SceneTransitionManager.Instance.IsTransitioning()}");
        }
        else
        {
            GUILayout.Label("Transition Manager: MISSING");
        }
        
        GUILayout.Label("");
        GUILayout.Label("HOTKEYS:");
        GUILayout.Label("F1 = Unlock Cursor");
        GUILayout.Label("F2 = Check Transition Manager");
        GUILayout.Label("F3 = Force Main Menu");
        GUILayout.Label("F4 = Force Game Scene");
        GUILayout.EndArea();
    }
}
