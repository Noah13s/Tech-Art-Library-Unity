using System.Collections;
using UnityEngine;

public abstract class Transition : MonoBehaviour
{
    protected Canvas transitionCanvas;

    public virtual void Setup()
    {
        // Create a new Canvas for transitions
        GameObject canvasObject = new GameObject("TransitionCanvas");
        transitionCanvas = canvasObject.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 1000; // Ensure it's on top
        DontDestroyOnLoad(canvasObject); // Set the canvas object as DontDestroyOnLoad
    }

    // Abstract method for playing the transition
    public abstract IEnumerator PlayTransition(bool isEntering);

    // Method to clean up transition objects
    public virtual void Cleanup()
    {
        if (transitionCanvas != null)
        {
            Destroy(transitionCanvas.gameObject);
            transitionCanvas = null;
        }
    }
}
