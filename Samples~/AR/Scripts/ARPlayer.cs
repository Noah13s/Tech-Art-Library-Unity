using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARPlayer : MonoBehaviour
{
    public GameObject indicator;
    
    [NonSerialized]
    public Camera camera;
    private GameObject indicatorInstance;
    // Start is called before the first frame update
    void Start()
    {
        camera = GetComponentInChildren<Camera>();
        indicatorInstance = Instantiate(indicator);
    }

    // Update is called once per frame
    void Update()
    {
        targetIndicator();
    }

    private void targetIndicator()
    {
        RaycastHit hit;
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit))
        {
            indicatorInstance.SetActive(true);
            indicatorInstance.transform.position = hit.point;
            indicatorInstance.transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal);
        } else
        {
            indicatorInstance.SetActive(false);
        }

    }
}
