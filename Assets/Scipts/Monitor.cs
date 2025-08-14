/******************************************************************************
 * File: Monitor.cs
 * Author: Javier, Zenon, Joel
 * Created: [Insert Date]
 * Description: Attached to in-game monitor/screen objects. Stores the camera index
 *              that this screen is linked to in the CameraSystem.
 ******************************************************************************/
using UnityEngine;

/// <summary>
/// Attach this to each in-game monitor/screen the player can interact with.
/// It defines which camera index to activate in the CameraSystem when clicked.
/// </summary>
public class Monitor : MonoBehaviour
{
    /// <summary>
    /// The index of the camera in the CameraSystem to switch to when this monitor is activated.
    /// </summary>
    public int cameraIndex;
}
