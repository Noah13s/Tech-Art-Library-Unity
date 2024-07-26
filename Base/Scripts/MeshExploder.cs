using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class TransformExploder : MonoBehaviour
{
    [Range(0, 1)]
    public float explosionValue = 0f; // 0 = assembled, 1 = fully exploded
    public ExplodeSettings explodeSettings;

    [System.Serializable]
    public struct ExplodeSettings
    {
        public float maxHorizontalDistance;
        public float maxVerticalDistance;
    }

    private struct TransformData
    {
        public Transform transform;
        public Vector3 originalPosition;
        public Vector3 explodedPosition;

        public TransformData(Transform transform, Vector3 center, ExplodeSettings settings)
        {
            this.transform = transform;
            originalPosition = transform.position;
            Vector3 direction = (transform.position - center).normalized;
            explodedPosition = originalPosition + new Vector3(direction.x * settings.maxHorizontalDistance, direction.y * settings.maxVerticalDistance, direction.z * settings.maxHorizontalDistance);
        }

        public void UpdateExplodedPosition(Vector3 center, ExplodeSettings settings)
        {
            Vector3 direction = (transform.position - center).normalized;
            explodedPosition = originalPosition + new Vector3(direction.x * settings.maxHorizontalDistance, direction.y * settings.maxVerticalDistance, direction.z * settings.maxHorizontalDistance);
        }
    }

    private List<TransformData> transformDataList = new List<TransformData>();
    private bool initialized = false;

    void Start()
    {
        InitializeTransforms();
    }

    void OnValidate()
    {
        if (explosionValue == 0f)
        {
            InitializeTransforms();
        }
        UpdateTransforms();
    }

    void InitializeTransforms()
    {
        transformDataList.Clear();
        Vector3 center = transform.position;
        AddChildTransforms(transform, center);
        initialized = true;
    }

    void AddChildTransforms(Transform parent, Vector3 center)
    {
        foreach (Transform child in parent)
        {
            transformDataList.Add(new TransformData(child, center, explodeSettings));
            if (child.childCount > 0)
            {
                AddChildTransforms(child, center); // Recursive call for nested children
            }
        }
    }

    void UpdateTransforms()
    {
        if (!initialized) return;

        Vector3 center = transform.position;
        foreach (var transformData in transformDataList)
        {
            if (explodeSettings.maxHorizontalDistance == 0f && explodeSettings.maxVerticalDistance == 0f)
            {
                transformData.transform.position = transformData.originalPosition;
            }
            else
            {
                transformData.UpdateExplodedPosition(center, explodeSettings);
                transformData.transform.position = Vector3.Lerp(transformData.originalPosition, transformData.explodedPosition, explosionValue);
            }
        }
    }
}
