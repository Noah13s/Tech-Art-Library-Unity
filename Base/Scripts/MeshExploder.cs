using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class TransformExploder : MonoBehaviour
{
    [Range(0, 1)]
    public float explosionValue = 0f; // 0 = assembled, 1 = fully exploded
    public ExplodeSettings explodeSettings;
    public Transform targetTransform; // The target transform to move away from
    [SerializeField]
#if UNITY_EDITOR
    [Button("Store original state", "StoreOriginalPos")]
#endif
    private bool button;
#if UNITY_EDITOR
    [ConditionalVisibility("initialized")]
    [StyledString(20, 0, 1, 0)]
#endif
    public string init1 = "true";
#if UNITY_EDITOR
    [ConditionalVisibility("initialized", true)]
    [StyledString(20, 1, 0, 0)]
#endif
    public string init2 = "false";

    [System.Serializable]
    public struct ExplodeSettings
    {
        public float maxHorizontalDistance;
        public float maxVerticalDistance;
    }

    private struct TransformData
    {
        public Transform transform;
        public Vector3 originalPosition; // Absolute world position
        public Vector3 explodedPosition; // Absolute world position

        public TransformData(Transform transform, Vector3 center, ExplodeSettings settings)
        {
            this.transform = transform;
            this.originalPosition = transform.position; // Store absolute world position
            Vector3 direction = (this.originalPosition - center).normalized;
            this.explodedPosition = this.originalPosition + new Vector3(
                direction.x * settings.maxHorizontalDistance,
                direction.y * settings.maxVerticalDistance,
                direction.z * settings.maxHorizontalDistance
            );
        }

        public void UpdateExplodedPosition(Vector3 center, ExplodeSettings settings)
        {
            Vector3 direction = (this.originalPosition - center).normalized;
            this.explodedPosition = this.originalPosition + new Vector3(
                direction.x * settings.maxHorizontalDistance,
                direction.y * settings.maxVerticalDistance,
                direction.z * settings.maxHorizontalDistance
            );
        }
    }

    private List<TransformData> transformDataList = new List<TransformData>();
    [SerializeField]
    [ReadOnly]
    private bool initialized = false;

    public void StoreOriginalPos()
    {
        transformDataList.Clear();
        Vector3 center = targetTransform ? targetTransform.position : transform.position;
        AddChildTransforms(transform, center);
        initialized = true; // Mark as initialized after storing original positions
    }

    void Start()
    {
        // Do not initialize here to ensure StoreOriginalPos is used first
    }

    void OnValidate()
    {
        // Reinitialize transforms if explosionValue is set to 0 or if not initialized
        if (explosionValue == 0f || !initialized)
        {
            InitializeTransforms();
        }
        UpdateTransforms();
    }

    void InitializeTransforms()
    {
        if (!initialized) return; // Only initialize if not already initialized
        Vector3 center = targetTransform ? targetTransform.position : transform.position;
        AddChildTransforms(transform, center);
    }

    void AddChildTransforms(Transform parent, Vector3 center)
    {
        foreach (Transform child in parent)
        {
            if (!transformDataList.Exists(data => data.transform == child))
            {
                transformDataList.Add(new TransformData(child, center, explodeSettings));
            }
            if (child.childCount > 0)
            {
                AddChildTransforms(child, center); // Recursive call for nested children
            }
        }
    }

    void UpdateTransforms()
    {
        if (!initialized) return;

        Vector3 center = targetTransform ? targetTransform.position : transform.position;
        foreach (var transformData in transformDataList)
        {
            if (explodeSettings.maxHorizontalDistance == 0f && explodeSettings.maxVerticalDistance == 0f)
            {
                transformData.transform.position = transformData.originalPosition;
            }
            else
            {
                // Update exploded position
                transformData.UpdateExplodedPosition(center, explodeSettings);
                // Interpolate between original and exploded position
                transformData.transform.position = Vector3.Lerp(transformData.originalPosition, transformData.explodedPosition, explosionValue);
            }
        }
    }
}
