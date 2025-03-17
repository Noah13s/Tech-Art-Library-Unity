using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LocalMultiplayer : MonoBehaviour
{
    public GridLayoutGroup gridLayout;
    public Transform gamepadList;
    public GameObject gamepadPrefab; // The prefab to instantiate

    private void Start()
    {
        DetectGamepads();

    }

    // Update is called once per frame
    void Update()
    {

        Debug.Log($"Gamepad: {Gamepad.all.Count}");
        
        if (gridLayout == null) { return; }

        Vector2 gridLayoutScreenSize = new Vector2(gridLayout.GetComponent<RectTransform>().rect.width, gridLayout.GetComponent<RectTransform>().rect.height);
        gridLayout.cellSize = new Vector2(gridLayoutScreenSize.x/gridLayout.constraintCount, gridLayoutScreenSize.y / gridLayout.constraintCount);
    }

    public void DetectGamepads()
    {
        // Clear existing gamepad UI elements
        //ClearChildren();

        // Loop through all connected gamepads
        foreach (var device in Gamepad.all)
        {
            Debug.Log($"Gamepad: {device.displayName}, Type: {device.displayName}");

            // Instantiate the prefab and parent it to the UI element
            Instantiate(gamepadPrefab, gamepadList);
        }
    }

    private void ClearChildren()
    {
        // Remove all children from the parent UI element
        foreach (Transform child in gamepadList)
        {
            Destroy(child.gameObject);
        }
    }
}
