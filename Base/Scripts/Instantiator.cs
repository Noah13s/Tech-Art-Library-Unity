using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instantiator : MonoBehaviour
{
    [SerializeField] GameObject objectToInstantiate;
    [SerializeField] Transform parentTransform;

    public void CustomInstantiate()
    {
        if (objectToInstantiate == null) { return; }
        if (parentTransform == null)
        {
            Instantiate(objectToInstantiate);
        }
        else
        {
            Instantiate(objectToInstantiate, parentTransform);
        }
    }

    public void CustomInstantiate(int numberOfInstances)
    {
        if (objectToInstantiate == null) { return; }
        for (int i = 0; i < numberOfInstances; i++)
        {
            if (parentTransform == null)
            {
                Instantiate(objectToInstantiate);
            }
            else
            {
                Instantiate(objectToInstantiate, parentTransform);
            }
        }
    }

    public void CustomInstantiate(float numberOfInstances)
    {
        if (objectToInstantiate == null) { return; }
        for (int i = 0; i < numberOfInstances; i++)
        {
            if (parentTransform == null)
            {
                Instantiate(objectToInstantiate);
            }
            else
            {
                Instantiate(objectToInstantiate, parentTransform);
            }
        }
    }

    public void CleanInstantiate(int numberOfInstances)
    {
        if (objectToInstantiate == null) { return; }
        CleanupParentChildren();
        for (int i = 0; i < numberOfInstances; i++)
        {
            if (parentTransform == null)
            {
                Instantiate(objectToInstantiate);
            }
            else
            {
                Instantiate(objectToInstantiate, parentTransform);
            }
        }
    }

    public void CleanInstantiate(float numberOfInstances)
    {
        if (objectToInstantiate == null) { return; }
        CleanupParentChildren();
        for (int i = 0; i < (int)numberOfInstances; i++)
        {
            if (parentTransform == null)
            {
                Instantiate(objectToInstantiate);
            }
            else
            {
                Instantiate(objectToInstantiate, parentTransform);
            }
        }
    }

    private void CleanupParentChildren()
    {
        if (parentTransform == null || parentTransform.childCount < 1) { return; }
        for (int i = 0; i < parentTransform.childCount; i++)
        {
            Destroy(parentTransform.GetChild(i).gameObject);            
        }
    }
}
