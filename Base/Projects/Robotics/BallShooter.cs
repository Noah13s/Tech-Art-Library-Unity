using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallShooter : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private float velocity;
    private GameObject ball;


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShootBall();
        }
    }

    void ShootBall()
    {
        if (ball != null)
        {
            Destroy(ball); // Destroy the previous ball if it exists
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Instantiate a new ball at the shooter's position and rotation
        ball = Instantiate(ballPrefab, Camera.main.ScreenToWorldPoint(Input.mousePosition+new Vector3(0,0,3f)), Camera.main.transform.rotation);
        
        // Get the Rigidbody component of the ball
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        
        // Set the velocity of the ball in the forward direction
        if (rb != null)
        {
            rb.velocity = ray.direction * velocity;
        }
    }
}
