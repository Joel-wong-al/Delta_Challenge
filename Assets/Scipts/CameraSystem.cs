using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    [SerializeField] private GameObject[] Cameras;

    [SerializeField] private int CurrentCameraIndex;

    [SerializeField] private KeyCode OpenCameras;

    [SerializeField] private bool CamerasOpen;

    [SerializeField] private GameObject MainCamera;

    [SerializeField] private float CoolDownTimer;

    [SerializeField] private float CoolDownTime = 0.5f;
    void Start()
    {
        for (int i = 0; i < Cameras.Length; i++)
        {
            Cameras[i].SetActive(false); // Initially deactivate all cameras
        }
        MainCamera.SetActive(true); // To get the camera it is currently On
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(OpenCameras))
        {
            CamerasOpen = !CamerasOpen;
            ShowCamera();
        }

        if (CoolDownTimer > 0)
        {
            if (Input.GetAxis("Horizontal") > 0)
            {
                Cameras[CurrentCameraIndex].SetActive(false);
                CurrentCameraIndex += CurrentCameraIndex + 1;
                if (CurrentCameraIndex >= Cameras.Length)
                {
                    CurrentCameraIndex = 0; // Loop back to the first camera
                }
                GotoCamera(CurrentCameraIndex);
                CoolDownTimer = CoolDownTime;
            }
            else if (Input.GetAxis("Horizontal") > 0)
            {
                Cameras[CurrentCameraIndex].SetActive(false);
                CurrentCameraIndex += CurrentCameraIndex - 1;
                if (CurrentCameraIndex < 0)
                {
                    CurrentCameraIndex = Cameras.Length - 1; // Loop back to the first camera
                }
                GotoCamera(CurrentCameraIndex);
                CoolDownTimer = CoolDownTime;
            }
        }
        else
        {
            CoolDownTimer -= Time.deltaTime;
        }



    }
    private void ShowCamera()
    {
        if (CamerasOpen)
        {
            Cameras[CurrentCameraIndex].SetActive(true);
            MainCamera.SetActive(false);
        }
        else
        {
            Cameras[CurrentCameraIndex].SetActive(false);
            MainCamera.SetActive(true); // Show the main camera when cameras are closed
        }
    }

    private void GotoCamera(int Progression)
    {

        Cameras[CurrentCameraIndex].SetActive(false);
        CurrentCameraIndex = Progression;
        ShowCamera();
    }
}