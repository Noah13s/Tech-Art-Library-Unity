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
                if (eventA != null) { eventA.Invoke(); }
            }
            else
            {
                if (eventB != null) { eventB.Invoke(); }                ;
            }
        }
        else
        {
            if (!invert)
            {
                if (eventB != null) { eventB.Invoke(); }
            }
            else
            {
                if (eventA != null) { eventA.Invoke(); }
            }
        }

        // Flip the toggle for the next invocation
        toggle = !toggle;
    }

    public void TriggerA()
    {
        if (eventA != null) { eventA.Invoke(); }
    }

    public void TriggerB()
    {
        if (eventB != null) { eventB.Invoke(); }
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
