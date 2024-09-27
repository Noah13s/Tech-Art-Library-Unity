using UnityEngine;
using UnityEngine.Events;

public class PingPongEvent : MonoBehaviour
{
    public UnityEvent eventA;
    public UnityEvent eventB;

    private bool toggle = true;

    // Call this method to toggle between eventA and eventB
    public void TriggerPingPong()
    {
        if (toggle)
        {
            if (eventA != null)
            {
                eventA.Invoke();
            }
        }
        else
        {
            if (eventB != null)
            {
                eventB.Invoke();
            }
        }

        // Flip the toggle for the next invocation
        toggle = !toggle;
    }
}
