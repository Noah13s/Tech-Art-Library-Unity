using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;



#if UNITY_EDITOR
using UnityEditor;
#endif

public enum TriggerType
{
    Tag,
    Gameobject,
    Any,
}

public enum ShapeType
{
    Sphere,
    Box,
    Capsule,
    Mesh,
}

public class Master_Event_Trigger : MonoBehaviour
{
    #region Variables

        #region BaseVars
        [Header("Base")]
        public ShapeType ShapeType;
        public float TriggerRadius = 0.5f;
        public Vector3 TriggerSize = new Vector3(1f, 1f, 1f);
        public float TriggerHeight = 1f;
        public Color DisplayColor = Color.blue;
        #endregion

        #region TriggerVars
        [Header("Trigger")]
        public TriggerType TriggerType;
        public List<string> Tags;
        public List<GameObject> GameObjects; // Change the type to List<GameObject>
        public bool EnterTrigger;
        #endregion

        #region Colliders
        private SphereCollider sphereCollider;
        private BoxCollider boxCollider;
        private CapsuleCollider capsuleCollider;

        private ShapeType? previousShapeType = null; // Variable to store the previous ShapeType "?" used to declare a nullable enum
        private Rigidbody rigidBody;
        #endregion


        [Header("UnityEvent")]
        public UnityEvent Event;
    #endregion

    #region OncollisionEvents
    void OnCollisionEnter(Collision other)
    {
        switch (TriggerType)
        {
            case TriggerType.Tag:
                foreach (var tag in Tags)
                {
                    if (other.gameObject.CompareTag(tag))
                    {
                        FireEvents();
                        break;
                    }
                }
                break;            
            case TriggerType.Gameobject:
                foreach (var gameObject in GameObjects)
                {
                    if (other.gameObject == gameObject)
                    {
                        FireEvents();
                        break;
                    }
                }
                break;            
            case TriggerType.Any:
                FireEvents();
                break;
        }
        Debug.Log("CollisionEvent");
    }

    void OnTriggerEnter(Collider other)
    {
        switch (TriggerType)
        {
            case TriggerType.Tag:
                foreach (var tag in Tags)
                {
                    if (other.gameObject.CompareTag(tag))
                    {
                        FireEvents();
                        break;
                    }
                }
                break;
            case TriggerType.Gameobject:
                foreach (var gameObject in GameObjects)
                {
                    if (other.gameObject == gameObject)
                    {
                        FireEvents();
                        break;
                    }
                }
                break;
            case TriggerType.Any:
                FireEvents();
                break;
        }
        Debug.Log("TriggerEvent");
    }
    #endregion

    // Fire the unity events
    private void FireEvents()
    {
        Debug.Log("EventsFired");
        Event.Invoke();
    }

    // Reconstruct trigger zone
    private IEnumerator UpdateCollidersCoroutine()
    {
        if (previousShapeType != ShapeType)
        {
            // Clear existing colliders
            if (sphereCollider != null) 
            { 
                Destroy(sphereCollider); 
                yield return null; 
            }
            if (boxCollider != null) 
            {
                Destroy(boxCollider); 
                yield return null; 
            }
            if (capsuleCollider != null) 
            { 
                Destroy(capsuleCollider); 
                yield return null; 
            }

            yield return null; // Wait for one frame before adding new colliders

            // ShapeType is changed, add the new collider
            switch (ShapeType)
            {
                case ShapeType.Sphere: 
                    sphereCollider = gameObject.AddComponent<SphereCollider>(); 
                    break;
                case ShapeType.Box: 
                    boxCollider = gameObject.AddComponent<BoxCollider>(); 
                    break;
                case ShapeType.Capsule: 
                    capsuleCollider = gameObject.AddComponent<CapsuleCollider>(); 
                    break;
                case ShapeType.Mesh: 
                    break;
            }
            if (rigidBody == null)
                rigidBody = gameObject.AddComponent<Rigidbody>();
            rigidBody.useGravity = false;
            // Update the previousShapeType for the next comparison
            previousShapeType = ShapeType;
        }

        UpdateData();
    }
    
    // Set the data
    private void UpdateData()
    {
        switch (ShapeType)
        {
            case ShapeType.Sphere:
                if (sphereCollider != null)
                {
                    sphereCollider.radius = TriggerRadius;
                    sphereCollider.isTrigger = EnterTrigger;
                }
                break;
            case ShapeType.Box:
                if (boxCollider != null)
                {
                    boxCollider.size = TriggerSize;
                    boxCollider.isTrigger = EnterTrigger;
                }
                break;
            case ShapeType.Capsule:
                if (capsuleCollider != null)
                {
                    capsuleCollider.height = TriggerHeight;
                    capsuleCollider.radius = TriggerRadius;
                    capsuleCollider.isTrigger = EnterTrigger;
                }
                break;
            case ShapeType.Mesh:
                break;

        }
    }

    // Visualize the trigger zones using DebugGizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = DisplayColor;
        switch (ShapeType)
        {
            case ShapeType.Sphere:
                Gizmos.DrawWireSphere(this.transform.position, TriggerRadius);
                break;
            case ShapeType.Box:
                Gizmos.DrawWireCube(this.transform.position, TriggerSize);
                break;
            case ShapeType.Capsule:
                Gizmos.DrawWireCube(this.transform.position, new Vector3(TriggerRadius*2, TriggerHeight, TriggerRadius*2));
                break;
            case ShapeType.Mesh:
                break;

        }
    }

    // Update script on value change
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(UpdateCollidersCoroutine());
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Master_Event_Trigger))]
public class Master_Event_Trigger_Editor : Editor
{
    SerializedProperty triggerTypeProperty;
    SerializedProperty tagProperty;
    SerializedProperty gameObjectsProperty; // Update the property name
    SerializedProperty triggerRadiusProperty;
    SerializedProperty DisplayColorProperty;
    SerializedProperty ShapeTypeProperty;
    SerializedProperty TriggerSizeProperty;
    SerializedProperty HeightProperty;
    SerializedProperty EnterTriggerProperty;
    SerializedProperty eventProperty;

    private void OnEnable()
    {
        // Initialize serialized properties
        triggerTypeProperty = serializedObject.FindProperty("TriggerType");
        tagProperty = serializedObject.FindProperty("Tags");
        gameObjectsProperty = serializedObject.FindProperty("GameObjects"); // Update the property name
        triggerRadiusProperty = serializedObject.FindProperty("TriggerRadius");
        DisplayColorProperty = serializedObject.FindProperty("DisplayColor");
        ShapeTypeProperty = serializedObject.FindProperty("ShapeType");
        TriggerSizeProperty = serializedObject.FindProperty("TriggerSize");
        HeightProperty = serializedObject.FindProperty("TriggerHeight");
        EnterTriggerProperty = serializedObject.FindProperty("EnterTrigger");
        eventProperty = serializedObject.FindProperty("Event");
    }

    public override void OnInspectorGUI()
    {
        // Update the serialized object
        serializedObject.Update();

        // Draw enum for ShapeType
        EditorGUILayout.PropertyField(ShapeTypeProperty, new GUIContent("Shape Type"));
        switch ((ShapeType)ShapeTypeProperty.enumValueIndex)
        {
            case ShapeType.Sphere:
                EditorGUILayout.PropertyField(triggerRadiusProperty, new GUIContent("Trigger Radius"));
                break;
            case ShapeType.Box:
                // Add any specific fields for Gameobject type if needed
                EditorGUILayout.PropertyField(TriggerSizeProperty, new GUIContent("Trigger Size"));
                break;
            case ShapeType.Capsule:
                // Add any specific fields for Any type if needed
                EditorGUILayout.PropertyField(triggerRadiusProperty, new GUIContent("Trigger Radius"));
                EditorGUILayout.PropertyField(HeightProperty, new GUIContent("Trigger Height"));
                break;
            case ShapeType.Mesh:
                // Add any specific fields for Any type if needed
                break;
        }

        EditorGUILayout.PropertyField(DisplayColorProperty, new GUIContent("Display Color"));

        // Draw enum for TriggerType
        EditorGUILayout.PropertyField(triggerTypeProperty, new GUIContent("Trigger Type"));
        EditorGUILayout.PropertyField(EnterTriggerProperty, new GUIContent("Overlap Trigger"));

        // show the appropriate field based on the TriggerType
        switch ((TriggerType)triggerTypeProperty.enumValueIndex)
        {
            case TriggerType.Tag:
                EditorGUILayout.PropertyField(tagProperty, new GUIContent("Tags"));
                break;
            case TriggerType.Gameobject:
                // Add any specific fields for Gameobject type if needed
                EditorGUILayout.PropertyField(gameObjectsProperty, new GUIContent("GameObjects"));
                break;
            case TriggerType.Any:
                // Add any specific fields for Any type if needed
                break;
        }

        GUILayout.Space(10); // Add some space between Trigger and UnityEvent sections

        // Draw UnityEvent section manually
        EditorGUILayout.PropertyField(eventProperty, new GUIContent("UnityEvent"), true);

        // Apply modifications to the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}

#endif
