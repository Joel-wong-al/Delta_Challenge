/******************************************************************************
 * File: ShelfSystem.cs
 * Author: Javier
 * Created: [Insert Date]
 * Description: Helper script for setting up shelf checkpoints and landmarks
 *              for the customer navigation system.
 ******************************************************************************/

using UnityEngine;

/// <summary>
/// Helper component for setting up shelf navigation system.
/// Attach to empty GameObjects to mark shelf checkpoints and landmarks.
/// </summary>
public class ShelfSystem : MonoBehaviour
{
    [Header("Shelf System Setup")]
    [SerializeField] private bool isCheckpoint = true;
    [SerializeField] private bool isLandmark = false;
    
    [Header("Visual Helpers (Editor Only)")]
    [SerializeField] private Color gizmoColor = Color.blue;
    [SerializeField] private float gizmoSize = 0.5f;

    void Start()
    {
        // Set appropriate tags
        if (isCheckpoint)
        {
            gameObject.tag = "ShelfCheckpoint";
        }
        else if (isLandmark)
        {
            gameObject.tag = "ShelfLandmark";
        }
    }

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
