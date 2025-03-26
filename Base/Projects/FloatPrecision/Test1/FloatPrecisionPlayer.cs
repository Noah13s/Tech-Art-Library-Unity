using UnityEngine;

public class FloatPrecisionPlayer : MonoBehaviour
{
    [SerializeField] GameObject world;
    [SerializeField] float moveSpeed = 10f; // Movement in simulation units per second.
    [SerializeField] float conversionFactor = 1e-6f; // Must match SimulationPlayerScaler.
    
    public Vector3 playerPosition = Vector3.zero;


    void Start()
    {

    }

    void Update()
    {
        // Rotate the container (which holds the player)
        if (Input.GetKey(KeyCode.W)) { transform.Rotate(-1, 0, 0, Space.Self); }
        if (Input.GetKey(KeyCode.S)) { transform.Rotate(1, 0, 0, Space.Self); }
        if (Input.GetKey(KeyCode.D)) { transform.Rotate(0, 1, 0, Space.Self); }
        if (Input.GetKey(KeyCode.A)) { transform.Rotate(0, -1, 0, Space.Self); }
        if (Input.GetKey(KeyCode.E)) { transform.Rotate(0, 0, -1, Space.Self); }
        if (Input.GetKey(KeyCode.Q)) { transform.Rotate(0, 0, 1, Space.Self); }

        // Update world offset when shift is pressed.
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            // Here, moveSpeed is in simulation units. Convert it to Unity units.
            playerPosition += transform.forward * moveSpeed * Time.deltaTime;
        }
    }
}
