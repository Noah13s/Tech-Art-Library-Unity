using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Vehicle_UI : MonoBehaviour
{
    [SerializeField] private Text speedometerValue;
    [SerializeField] private RectTransform speedometerArrow;
    [SerializeField] private Vehicle_Player vehicle;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (vehicle != null)
        {
            UpdateSpeedometer();
        }
    }

    void UpdateSpeedometer()
    {
        if (speedometerValue != null) { speedometerValue.text =  MathF.Round(vehicle.speedKMH).ToString(); }
        if (speedometerArrow != null)
        {
            float normalizedSpeed = Mathf.Clamp(vehicle.speedKMH / 100f, 0f, 1f); // Normalize speed
            float zRotation = Mathf.Lerp(0, -240, normalizedSpeed); // Map speed to rotation range
            speedometerArrow.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, zRotation);
        }
    }
}
