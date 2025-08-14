/******************************************************************************
 * File: ShelfSystem.cs
 * Author: Javier, Zenon, Joel
 * Created: [Insert Date]
 * Description: Helper script for setting up shelf checkpoints and landmarks
 *              for the customer navigation system in Delta Challenge.
 ******************************************************************************/

using UnityEngine;

/// <summary>
/// Helper component for setting up shelf navigation system.
/// Attach to empty GameObjects to mark shelf checkpoints and landmarks for NPC navigation.
/// </summary>
public class ShelfSystem : MonoBehaviour
{
    // ===================== Shelf System Setup =====================
    [Header("Shelf System Setup")]
    [SerializeField] private bool isCheckpoint = true; ///< If true, this object is a shelf checkpoint for navigation
    [SerializeField] private bool isLandmark = false; ///< If true, this object is a shelf landmark for navigation

    // ===================== Visual Helpers (Editor Only) =====================
    [Header("Visual Helpers (Editor Only)")]
    [SerializeField] private Color gizmoColor = Color.blue; ///< Color for gizmo visualization
    [SerializeField] private float gizmoSize = 0.5f; ///< Size for gizmo visualization

    /// <summary>
    /// Unity Start method. Sets the appropriate tag for this shelf object.
    /// </summary>
    void Start()
    {
        // Set appropriate tags for navigation
        if (isCheckpoint)
        {
            gameObject.tag = "ShelfCheckpoint";
        }
        else if (isLandmark)
        {
            gameObject.tag = "ShelfLandmark";
        }
    }

    /// <summary>
    /// Draws gizmos in the editor for visualizing checkpoints and landmarks.
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        if (isCheckpoint)
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one * gizmoSize);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.2f);
        }
        else if (isLandmark)
        {
            Gizmos.DrawWireSphere(transform.position, gizmoSize * 0.5f);
            Gizmos.DrawRay(transform.position, Vector3.up);
        }
    }

    /// <summary>
    /// Draws highlighted gizmos when the object is selected in the editor.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        if (isCheckpoint)
        {
            Gizmos.DrawCube(transform.position, Vector3.one * gizmoSize);
        }
        else if (isLandmark)
        {
            Gizmos.DrawSphere(transform.position, gizmoSize * 0.5f);
        }
    }
}
