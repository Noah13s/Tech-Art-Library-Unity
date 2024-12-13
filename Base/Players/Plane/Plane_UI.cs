using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Plane_UI : MonoBehaviour
{
    [SerializeField] private Text altitude;
    [SerializeField] private Text speed;
    [SerializeField] private RectTransform speedometerArrow;
    [SerializeField] private Plane_Player vehicle;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (vehicle != null)
        {
            //UpdateSpeedometer();
        }
    }
    /*
    void UpdateSpeedometer()
    {
        if (altitude != null) { altitude.text = (MathF.Round(vehicle.altitudeMeters/10f)*10).ToString(); }
        if (speed != null) { speed.text = MathF.Round(vehicle.speedKMH).ToString(); }
        if (speedometerArrow != null)
        {
            float normalizedSpeed = Mathf.Clamp(vehicle.altitudeMeters / 50000f, 0f, 1f); // Normalize speed
            float zRotation = Mathf.Lerp(0, -240, normalizedSpeed); // Map speed to rotation range
            speedometerArrow.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, zRotation);
        }
    }*/
}
