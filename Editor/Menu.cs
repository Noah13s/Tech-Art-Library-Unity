using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Menu : MonoBehaviour
{
    // Add a menu item named "Do Something" to MyMenu in the menu bar.
    [MenuItem("Tech Art Library/Reload Domain")]
    static void DomainReload()
    {
        EditorUtility.RequestScriptReload();
    }

    [MenuItem("Tech Art Library/Force Sample Dependecy Import")]
    static void ForceImport()
    {
        Setup.ListSamples("com.n04h.techartlibrary");
    }

    [MenuItem("Tech Art Library/Test Debug.Log")]
    static void TestLog()
    {
        Debug.Log("msg");
    }


}
