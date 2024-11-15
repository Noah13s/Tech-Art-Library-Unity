using UnityEngine;

public class GyroscopeSensor : MonoBehaviour
{
    private Gyroscope gyro;

    void Start()
    {
        if (SystemInfo.supportsGyroscope)
        {
            gyro = Input.gyro;
            gyro.enabled = true;
        }
    }

    void Update()
    {
        // Log the gyroscope's rotation data
        if (gyro != null)
        {
            Vector3 rotationRate = gyro.rotationRate;
            Debug.Log("Gyroscope Rotation Rate: " + rotationRate);
        }
    }
}
