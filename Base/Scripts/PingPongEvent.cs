using UnityEngine;
using UnityEngine.Events;

public class PingPongEvent : MonoBehaviour
{
    [SerializeField] private bool invert = false;
    public UnityEvent eventA;
    public UnityEvent eventB;

    private bool toggle = true;

    // Call this method to toggle between eventA and eventB
    public void TriggerPingPong()
    {
        if (toggle)
        {
            if (!invert) 
            {
                eventA?.Invoke();
            }
            else
            {
                eventB?.Invoke();
            }
        }
        else
        {
            if (!invert)
            {
                eventB?.Invoke();
            }
            else
            {
                eventA?.Invoke();
            }
        }

        // Flip the toggle for the next invocation
        toggle = !toggle;
    }

    public void TriggerA()
    {
        eventA?.Invoke();
    }

    public void TriggerB()
    {
        eventB?.Invoke();
    }

    public void SetToggleA()
    {
        toggle = true;
    }

    public void SettoggleB()
    {
        toggle = false;
    }
}
