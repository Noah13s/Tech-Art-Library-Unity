using UnityEngine;
using System;

public class SplitScreenGrid : MonoBehaviour
{
    public Camera[] cameras;

    private void Start()
    {
        if (cameras == null || cameras.Length == 0)
        {
            Debug.LogError("No cameras assigned to SplitScreenGrid script.");
            return;
        }

        UpdateSplitScreen();
    }

    private void UpdateSplitScreen()
    {
        if (cameras == null || cameras.Length == 0) return; //Early exit if no cameras

        int numCameras = cameras.Length;
        int gridWidth = (int)Math.Ceiling(Math.Sqrt(numCameras));
        int gridHeight = (int)Math.Ceiling((float)numCameras / gridWidth);


        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float cellWidth = screenWidth / gridWidth;
        float cellHeight = screenHeight / gridHeight;

        int cameraIndex = 0;
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (cameraIndex < cameras.Length)
                {
                    Camera cam = cameras[cameraIndex];
                    cam.rect = new Rect(x * cellWidth / screenWidth, (gridHeight - 1 - y) * cellHeight / screenHeight, cellWidth / screenWidth, cellHeight / screenHeight);
                    cam.enabled = true;
                    cameraIndex++;
                }
                else
                {
                    break;
                }
            }
        }

        for (int i = cameraIndex; i < cameras.Length; i++)
        {
            cameras[i].enabled = false;
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateSplitScreen();
    }

    [ContextMenu("UpdateSplitscreen")]
    public void UpdateCameraSetup()
    {
        UpdateSplitScreen();
    }

    //Helper function to set cameras from other scripts
    public void SetCameras(Camera[] newCameras)
    {
        cameras = newCameras;
        UpdateSplitScreen();
    }
}