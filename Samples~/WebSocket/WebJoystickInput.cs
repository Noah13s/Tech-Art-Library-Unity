using UnityEngine;

public class WebJoystickInput : MonoBehaviour
{
    public Cinemachine.CinemachineFreeLook freeLookCamera;
    private float Xvalue;
    private float Yvalue;
    private bool JoystickUpdate = false;
    void Start()
    {
    }

    void OnDestroy()
    {
    }
    private void Update()
    {
        if (JoystickUpdate)
        {
            UpdateCameraInput(Xvalue, Yvalue);
        }
    }

    public void OnWebSocketMessage(string message)
    {
        
        Debug.Log(message);
        // Parse Joystick data
        if (message.StartsWith("JoystickPos:"))
        {
            string[] data = message.Substring("JoystickPos:".Length).Split(',');

            if (data.Length == 2)
            {

                JoystickUpdate = true;
                // Parse x and y values
                Xvalue = float.Parse(data[0]) * 0.01f;
                Yvalue = float.Parse(data[1]) * 0.0001f;

                // Use the joystick data as input for the Cinemachine FreeLook Camera
                UpdateCameraInput(Xvalue, Yvalue);

                if ((float.Parse(data[0]) == 0) && (float.Parse(data[1]) == 0))
                {
                    JoystickUpdate = false;
                }
            }
        }
    }

    void UpdateCameraInput(float x, float y)
    {
        // Assuming you have a Cinemachine FreeLook Camera component attached to this GameObject
        if (freeLookCamera != null)
        {
            // Adjust the input values according to your preferences
            freeLookCamera.m_XAxis.Value += x; // Add x to the current X-axis value
            freeLookCamera.m_YAxis.Value += y; // Add y to the current Y-axis value
        }
    }
}
