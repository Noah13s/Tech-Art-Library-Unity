using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Menu : MonoBehaviour
{
    // Add a menu item named "Do Something" to MyMenu in the menu bar.
    [MenuItem("Tech Art Library/Misc/Reload Domain")]
    static void DomainReload()
    {
        EditorUtility.RequestScriptReload();
    }

    [MenuItem("Tech Art Library/Misc/Force Sample Dependency Import")]
    static void ForceImport()
    {
        Setup.ListSamples("com.n04h.techartlibrary");
    }

    [MenuItem("Tech Art Library/Misc/Clear All Packages")]
    static void ClearPackages()
    {
        Setup.RemoveAllPackages();
    }

    [MenuItem("Tech Art Library/Misc/Test Debug.Log")]
    static void TestLog()
    {
        Debug.Log("msg");
    }


}
