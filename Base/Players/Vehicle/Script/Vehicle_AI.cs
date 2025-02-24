using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Vehicle_AI : MonoBehaviour
{
    [SerializeField] Vehicle_Player targetVehicle;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnDrawGizmos()
    {
        Handles.color = Color.gray;
        Handles.DrawWireCube(transform.position, transform.localScale);
        Handles.DrawDottedLine(transform.position, transform.position + (transform.forward * 5f), 5f);
    }
}

#region Custom Editor
#if UNITY_EDITOR
[CustomEditor(typeof(Vehicle_AI))]
public class CustomEditorVehicle_AI : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Vehicle_AI playerScript = (Vehicle_AI)target;

        GUILayout.Space(10);

    }
}
#endif
#endregion