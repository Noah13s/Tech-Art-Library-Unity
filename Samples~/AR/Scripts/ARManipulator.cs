using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class ARManipulator : MonoBehaviour
{
    public GameObject stateGameobject;
    public GameObject validateGameobject;
    public Sprite locked;
    public Sprite unlocked;
    private GameObject targetObject;
    private bool manipulating;
    private ARPlayer player;
    private enum ManipulationMode { None, Move, Rotate, Scale }
    private ManipulationMode currentMode = ManipulationMode.None;

    private Vector2 initialTouchPosition;
    private Vector3 initialObjectPosition;
    private Vector3 initialObjectScale;
    private Quaternion initialObjectRotation;

    private float initialFingerDistance;
    private Vector3 initialScale;

    private ARInputActions inputActions;

    private void Awake()
    {
        player = GetComponentInParent<ARPlayer>();
        inputActions = new ARInputActions();
        EnhancedTouchSupport.Enable();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += OnFingerDown;
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp += OnFingerUp;
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove += OnFingerMove;
    }

    private void OnDisable()
    {
        inputActions.Disable();
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= OnFingerDown;
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp -= OnFingerUp;
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove -= OnFingerMove;
        EnhancedTouchSupport.Disable();
    }

    public void Move()
    {
        if (SetTargetObject())
        {
            currentMode = ManipulationMode.Move;
            changeState(true);
        }
    }

    public void Rotate()
    {
        if (SetTargetObject())
        {
            currentMode = ManipulationMode.Rotate;
            changeState(true);
        }
    }

    public void Scale()
    {
        if (SetTargetObject())
        {
            currentMode = ManipulationMode.Scale;
            changeState(true);
        }
    }

    public void Validate()
    {
        currentMode = ManipulationMode.None;
        changeState(false);
    }

    public void Delete()
    {
        RaycastHit hit;
        if (Physics.Raycast(player.camera.transform.position, player.camera.transform.forward, out hit))
        {
            if (hit.collider.gameObject.CompareTag("MovableObject"))
            {
                Destroy(hit.collider.gameObject);
                if (currentMode != ManipulationMode.None) 
                {
                    changeState(false);
                }
            }
        }
    }

    private bool SetTargetObject()
    {
        RaycastHit hit;
        if (Physics.Raycast(player.camera.transform.position, player.camera.transform.forward, out hit))
        {
            if (hit.collider.gameObject.CompareTag("MovableObject"))
            {
                targetObject = hit.collider.gameObject;
                return true;
            }
        }
        return false;
    }

    private void changeState(bool newState)
    {
        manipulating = newState;
        if (newState)
        {
            stateGameobject.GetComponent<Image>().sprite = locked;
            validateGameobject.SetActive(true);
        }
        else
        {
            stateGameobject.GetComponent<Image>().sprite = unlocked;
            validateGameobject.SetActive(false);
        }
    }

    private void OnFingerDown(Finger finger)
    {
        if (manipulating && targetObject != null)
        {
            initialTouchPosition = finger.screenPosition;
            initialObjectPosition = targetObject.transform.position;
            initialObjectScale = targetObject.transform.localScale;
            initialObjectRotation = targetObject.transform.rotation;

            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers.Count == 2)
            {
                initialFingerDistance = Vector2.Distance(
                    UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers[0].screenPosition,
                    UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers[1].screenPosition
                );
                initialScale = targetObject.transform.localScale;
            }
        }
    }

    private void OnFingerUp(Finger finger)
    {
        if (manipulating && UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers.Count == 0)
        {
            Validate();
        }
    }

    private void OnFingerMove(Finger finger)
    {
        if (manipulating && targetObject != null)
        {
            HandleTouchInput();
        }
    }

    private void HandleTouchInput()
    {
        if (manipulating && targetObject != null)
        {
            Vector2 touchPosition = UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers[0].screenPosition;
            switch (currentMode)
            {
                case ManipulationMode.Move:
                    MoveObject(touchPosition);
                    break;
                case ManipulationMode.Rotate:
                    RotateObject(touchPosition);
                    break;
                case ManipulationMode.Scale:
                    ScaleObject();
                    break;
            }
        }
    }

    private void MoveObject(Vector2 touchPosition)
    {
        Vector2 touchDelta = touchPosition - initialTouchPosition;
        if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers.Count == 1)
        {
            // Move on X and Z axes
            Vector3 newPosition = initialObjectPosition + new Vector3(touchDelta.x, 0, touchDelta.y) * 0.01f; // Adjust sensitivity
            targetObject.transform.position = newPosition;
        }
        else if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers.Count == 2)
        {
            // Move up and down on Y axis based on vertical movement of two fingers
            float verticalDelta = touchDelta.y;
            Vector3 newPosition = initialObjectPosition + new Vector3(0, verticalDelta, 0) * 0.01f; // Adjust sensitivity
            targetObject.transform.position = newPosition;
        }
    }

    private void RotateObject(Vector2 touchPosition)
    {
        Vector2 touchDelta = touchPosition - initialTouchPosition;
        float rotationSpeed = 0.2f;
        float rotationY = touchDelta.x * rotationSpeed;
        targetObject.transform.Rotate(Vector3.up, rotationY, Space.World);
    }

    private void ScaleObject()
    {
        if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers.Count == 2)
        {
            float currentFingerDistance = Vector2.Distance(
                UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers[0].screenPosition,
                UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers[1].screenPosition
            );

            float scaleFactor = currentFingerDistance / initialFingerDistance;
            Vector3 newScale = initialScale * scaleFactor;
            targetObject.transform.localScale = newScale;
        }
    }
}