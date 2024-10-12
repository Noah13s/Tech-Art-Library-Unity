//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.11.0
//     from Packages/com.n04h.techartlibrary/Base/Players/FPS/Scripts/FPS_Controls.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @FPS_Controls: IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @FPS_Controls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""FPS_Controls"",
    ""maps"": [
        {
            ""name"": ""FPS_Action_Map"",
            ""id"": ""ad5492f3-8439-44a8-8ee9-41691c9ab66c"",
            ""actions"": [
                {
                    ""name"": ""Forward"",
                    ""type"": ""Button"",
                    ""id"": ""04e74356-1e26-4a86-9eeb-3d78e0bc2f51"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Backwards"",
                    ""type"": ""Button"",
                    ""id"": ""d62d328b-3bce-49e6-83bd-6ffe0df43010"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Right"",
                    ""type"": ""Button"",
                    ""id"": ""421019d2-db7c-496e-965d-195def4d6c66"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Left"",
                    ""type"": ""Button"",
                    ""id"": ""aaba9300-5351-4a35-b40c-fed2db7ad4ea"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Front/Back"",
                    ""type"": ""Value"",
                    ""id"": ""e251d93f-2f4f-4495-bfbe-dd07c73aedc6"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Sideways"",
                    ""type"": ""Value"",
                    ""id"": ""aaec1c49-dcde-44fe-a822-d55d04d273e3"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Jump"",
                    ""type"": ""Button"",
                    ""id"": ""8f1d36d0-70d1-4a75-bfb9-a5486c13fe2a"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Look"",
                    ""type"": ""Value"",
                    ""id"": ""16fe58d6-9c5a-47d0-9523-63cd6003c135"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Interact"",
                    ""type"": ""Button"",
                    ""id"": ""c5b6ddab-c68f-4250-be48-a1c0389ccee3"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""d1951aa4-1b81-413f-b246-a7db660d80e1"",
                    ""path"": ""<Keyboard>/z"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Forward"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5ba30651-0e48-4de5-82a7-d9f660df4042"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Backwards"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""042bb664-9b06-49bb-9359-2f9e16cc2fd2"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Right"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""45dda3e7-b1e0-4e8a-8da5-962205ae8a77"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Left"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""cc7e4f64-3e55-4505-80d5-99593b10740e"",
                    ""path"": ""<Joystick>/stick/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Front/Back"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d5dc517a-812b-4374-8f9c-dd32331b91d4"",
                    ""path"": ""<Joystick>/stick/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Sideways"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""381d774b-34ab-4100-9b5a-5615373518a8"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e4d11cef-9497-4070-882a-d08c597d7c5e"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5083dd48-f4b3-4f88-bc83-08df2125d6fb"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Interact"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // FPS_Action_Map
        m_FPS_Action_Map = asset.FindActionMap("FPS_Action_Map", throwIfNotFound: true);
        m_FPS_Action_Map_Forward = m_FPS_Action_Map.FindAction("Forward", throwIfNotFound: true);
        m_FPS_Action_Map_Backwards = m_FPS_Action_Map.FindAction("Backwards", throwIfNotFound: true);
        m_FPS_Action_Map_Right = m_FPS_Action_Map.FindAction("Right", throwIfNotFound: true);
        m_FPS_Action_Map_Left = m_FPS_Action_Map.FindAction("Left", throwIfNotFound: true);
        m_FPS_Action_Map_FrontBack = m_FPS_Action_Map.FindAction("Front/Back", throwIfNotFound: true);
        m_FPS_Action_Map_Sideways = m_FPS_Action_Map.FindAction("Sideways", throwIfNotFound: true);
        m_FPS_Action_Map_Jump = m_FPS_Action_Map.FindAction("Jump", throwIfNotFound: true);
        m_FPS_Action_Map_Look = m_FPS_Action_Map.FindAction("Look", throwIfNotFound: true);
        m_FPS_Action_Map_Interact = m_FPS_Action_Map.FindAction("Interact", throwIfNotFound: true);
    }

    ~@FPS_Controls()
    {
        UnityEngine.Debug.Assert(!m_FPS_Action_Map.enabled, "This will cause a leak and performance issues, FPS_Controls.FPS_Action_Map.Disable() has not been called.");
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // FPS_Action_Map
    private readonly InputActionMap m_FPS_Action_Map;
    private List<IFPS_Action_MapActions> m_FPS_Action_MapActionsCallbackInterfaces = new List<IFPS_Action_MapActions>();
    private readonly InputAction m_FPS_Action_Map_Forward;
    private readonly InputAction m_FPS_Action_Map_Backwards;
    private readonly InputAction m_FPS_Action_Map_Right;
    private readonly InputAction m_FPS_Action_Map_Left;
    private readonly InputAction m_FPS_Action_Map_FrontBack;
    private readonly InputAction m_FPS_Action_Map_Sideways;
    private readonly InputAction m_FPS_Action_Map_Jump;
    private readonly InputAction m_FPS_Action_Map_Look;
    private readonly InputAction m_FPS_Action_Map_Interact;
    public struct FPS_Action_MapActions
    {
        private @FPS_Controls m_Wrapper;
        public FPS_Action_MapActions(@FPS_Controls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Forward => m_Wrapper.m_FPS_Action_Map_Forward;
        public InputAction @Backwards => m_Wrapper.m_FPS_Action_Map_Backwards;
        public InputAction @Right => m_Wrapper.m_FPS_Action_Map_Right;
        public InputAction @Left => m_Wrapper.m_FPS_Action_Map_Left;
        public InputAction @FrontBack => m_Wrapper.m_FPS_Action_Map_FrontBack;
        public InputAction @Sideways => m_Wrapper.m_FPS_Action_Map_Sideways;
        public InputAction @Jump => m_Wrapper.m_FPS_Action_Map_Jump;
        public InputAction @Look => m_Wrapper.m_FPS_Action_Map_Look;
        public InputAction @Interact => m_Wrapper.m_FPS_Action_Map_Interact;
        public InputActionMap Get() { return m_Wrapper.m_FPS_Action_Map; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(FPS_Action_MapActions set) { return set.Get(); }
        public void AddCallbacks(IFPS_Action_MapActions instance)
        {
            if (instance == null || m_Wrapper.m_FPS_Action_MapActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_FPS_Action_MapActionsCallbackInterfaces.Add(instance);
            @Forward.started += instance.OnForward;
            @Forward.performed += instance.OnForward;
            @Forward.canceled += instance.OnForward;
            @Backwards.started += instance.OnBackwards;
            @Backwards.performed += instance.OnBackwards;
            @Backwards.canceled += instance.OnBackwards;
            @Right.started += instance.OnRight;
            @Right.performed += instance.OnRight;
            @Right.canceled += instance.OnRight;
            @Left.started += instance.OnLeft;
            @Left.performed += instance.OnLeft;
            @Left.canceled += instance.OnLeft;
            @FrontBack.started += instance.OnFrontBack;
            @FrontBack.performed += instance.OnFrontBack;
            @FrontBack.canceled += instance.OnFrontBack;
            @Sideways.started += instance.OnSideways;
            @Sideways.performed += instance.OnSideways;
            @Sideways.canceled += instance.OnSideways;
            @Jump.started += instance.OnJump;
            @Jump.performed += instance.OnJump;
            @Jump.canceled += instance.OnJump;
            @Look.started += instance.OnLook;
            @Look.performed += instance.OnLook;
            @Look.canceled += instance.OnLook;
            @Interact.started += instance.OnInteract;
            @Interact.performed += instance.OnInteract;
            @Interact.canceled += instance.OnInteract;
        }

        private void UnregisterCallbacks(IFPS_Action_MapActions instance)
        {
            @Forward.started -= instance.OnForward;
            @Forward.performed -= instance.OnForward;
            @Forward.canceled -= instance.OnForward;
            @Backwards.started -= instance.OnBackwards;
            @Backwards.performed -= instance.OnBackwards;
            @Backwards.canceled -= instance.OnBackwards;
            @Right.started -= instance.OnRight;
            @Right.performed -= instance.OnRight;
            @Right.canceled -= instance.OnRight;
            @Left.started -= instance.OnLeft;
            @Left.performed -= instance.OnLeft;
            @Left.canceled -= instance.OnLeft;
            @FrontBack.started -= instance.OnFrontBack;
            @FrontBack.performed -= instance.OnFrontBack;
            @FrontBack.canceled -= instance.OnFrontBack;
            @Sideways.started -= instance.OnSideways;
            @Sideways.performed -= instance.OnSideways;
            @Sideways.canceled -= instance.OnSideways;
            @Jump.started -= instance.OnJump;
            @Jump.performed -= instance.OnJump;
            @Jump.canceled -= instance.OnJump;
            @Look.started -= instance.OnLook;
            @Look.performed -= instance.OnLook;
            @Look.canceled -= instance.OnLook;
            @Interact.started -= instance.OnInteract;
            @Interact.performed -= instance.OnInteract;
            @Interact.canceled -= instance.OnInteract;
        }

        public void RemoveCallbacks(IFPS_Action_MapActions instance)
        {
            if (m_Wrapper.m_FPS_Action_MapActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(IFPS_Action_MapActions instance)
        {
            foreach (var item in m_Wrapper.m_FPS_Action_MapActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_FPS_Action_MapActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public FPS_Action_MapActions @FPS_Action_Map => new FPS_Action_MapActions(this);
    public interface IFPS_Action_MapActions
    {
        void OnForward(InputAction.CallbackContext context);
        void OnBackwards(InputAction.CallbackContext context);
        void OnRight(InputAction.CallbackContext context);
        void OnLeft(InputAction.CallbackContext context);
        void OnFrontBack(InputAction.CallbackContext context);
        void OnSideways(InputAction.CallbackContext context);
        void OnJump(InputAction.CallbackContext context);
        void OnLook(InputAction.CallbackContext context);
        void OnInteract(InputAction.CallbackContext context);
    }
}
