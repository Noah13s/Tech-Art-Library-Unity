using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class First_Person_Player : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private bool showCursor = false;
    [SerializeField]
    private bool airControl = false;

    [Header("Navigation")]
    [SerializeField]
    private float walkSpeed = 5.0f;
    [SerializeField]
    private float runSpeed = 10.0f;
    [SerializeField]
    private float jumpForce = 5.0f;
    [SerializeField]
    private float gravity = 9.807f;
    [SerializeField]
    private float lookSpeed = 2.0f;
    [SerializeField]
    private float lookXLimit = 80.0f;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    void Initalise()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;// If showCursor true .none else .Locked
        Cursor.visible = showCursor;
    }

    // Reinitialize on var changed
    private void OnValidate()
    {
        Initalise();
    }

    // Start is called before the first frame update
    void Start()
    {
        Initalise();
    }

    // Update is called once per frame
    void Update()
    {
        // Calculate movement
        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        float horizontalMovement = Input.GetAxis("Horizontal") * moveSpeed;
        float verticalMovement = Input.GetAxis("Vertical") * moveSpeed;

        // Check if the player is grounded
        if (characterController.isGrounded)
        {

            moveDirection = transform.TransformDirection(new Vector3(horizontalMovement, characterController.velocity.y, verticalMovement));

            if (Input.GetButton("Jump") && characterController.isGrounded)
            {
                moveDirection.y = jumpForce;
            }

        } else if (airControl)
        {
            moveDirection = transform.TransformDirection(new Vector3(horizontalMovement, characterController.velocity.y, verticalMovement));
        }
        // Camera rotation (looking around)
        float rotationY = Input.GetAxis("Mouse X") * lookSpeed;
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        transform.Rotate(0, rotationY, 0);
        Camera.main.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        // Apply gravity
        moveDirection.y -= gravity * Time.deltaTime;

        // Move the character
        characterController.Move(moveDirection * Time.deltaTime);
    }
}
