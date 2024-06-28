using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum ChargeState
{
    Neutral = 0,
    Positive,
    Negative
}

[Serializable]
public struct Temperature
{
    public float Celcius;
    public float Kelvin;
    public float Fahrenheit;
}

public class GlobalPhysics : MonoBehaviour
{

}
