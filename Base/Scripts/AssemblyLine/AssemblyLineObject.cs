using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AssemblyLineObject : MonoBehaviour
{
    [Serializable]
    private struct ObjectParameters
    {
        [SerializeField]
        public string name;
        [SerializeField]
        public GameObject prefab;
        [SerializeField]
        public int intValue;
        [SerializeField]
        public float floatValue;
    }

    [Serializable]
    private struct ObjectDetails
    {
        [SerializeField]
        public string assemblyName;
        [SerializeField]
        public string assemblyVersion;
        [SerializeField]
        public ObjectParameters[] parameters;
    }

    [SerializeField] private ObjectDetails data;
    [SerializeField] private bool objectLocked = false;

    // private interal variables
    internal Rigidbody rb;

    private void OnValidate()
    {
        if (objectLocked)
        {
            rb.isKinematic = true;
        } else
        {
            rb.isKinematic = false;
        }
    }


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void LockObject(Transform target)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        if (target == null)
        {
        }
        else
        {
            //this.transform.position = target.position;
            rb.MovePosition(target.position);
        }
    }

    public void ReleaseObject(float delay)
    {
        StartCoroutine(ReleaseAfterDelay(delay));
    }

    private IEnumerator ReleaseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null)
        {
            rb.MovePosition(this.transform.position);
            rb.isKinematic = false;
            rb.WakeUp();
            Physics.SyncTransforms();
        }
    }

    public void AddChildObject(GameObject prefab)
    {
        // Create a new child GameObject
        GameObject child = new GameObject(name);

        // Set the parent of the new GameObject
        child.transform.SetParent(this.transform);

        AssemblyLineObject objectData = child.AddComponent<AssemblyLineObject>();

        objectData.name = name;
    }

    public void AddChildPrefab(GameObject prefab)
    {
        prefab = Instantiate(prefab);

        prefab.transform.SetParent(this.transform, false);
    }

    public void RemoveChildObject(String name)
    {
        // Iterate through all the children of the current GameObject
        foreach (Transform child in transform)
        {
            // If the child has the same name as the input string, remove it
            if (child.GetComponent<AssemblyLineObject>().name == name)
            {
                // Destroy the child GameObject
                if (Application.isEditor)
                {
                    // If in the Editor, use Undo to make it undoable
                    UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
                }
                else
                {
                    // If in Play mode, simply destroy the object
                    Destroy(child.gameObject);
                }
                return; // Exit after removing the first match
            }
        }
    }
}
