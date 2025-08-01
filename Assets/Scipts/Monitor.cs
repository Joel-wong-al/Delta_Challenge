/******************************************************************************
 * File: CameraScreen.cs
 * Description: Attached to in-game screen objects. Stores the camera index
 *              that this screen is linked to in the CameraSystem.
 ******************************************************************************/
using UnityEngine;

/// <summary>
/// Attach this to each screen the player can click on.
/// It defines which camera index to activate when clicked.
/// </summary>
public class Monitor : MonoBehaviour
{
    /// <summary>
    /// The index of the camera in the CameraSystem to switch to.
    /// </summary>
    public int cameraIndex;
}
