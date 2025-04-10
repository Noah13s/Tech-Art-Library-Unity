using UnityEngine;
using System.Diagnostics;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NodeServerRunner : MonoBehaviour
{
    [SerializeField] private string serverScriptPath;
    [SerializeField] private bool useSeparateShell = false; // Run process using shell execution.
    [SerializeField] private bool openInBrowser = false;      // Open a browser when server starts.
    [SerializeField] private string browserURL = "http://localhost:3000"; // URL to open.
    [SerializeField] private bool executeOnlyInEditor = true; // Execute only in Editor (when true)

    private Process nodeProcess;

#if UNITY_EDITOR
    [ContextMenu("Browse for server.js")]
    void BrowseForScript()
    {
        string path = EditorUtility.OpenFilePanel("Select server.js", "", "js");
        if (!string.IsNullOrEmpty(path))
        {
            serverScriptPath = path;
            EditorUtility.SetDirty(this);
        }
    }
#endif

    void Start()
    {
        // Check if the script should only run in the Editor.
        if (executeOnlyInEditor && !Application.isEditor)
        {
            UnityEngine.Debug.Log("NodeServerRunner is set to execute only in Editor. Skipping execution in build.");
            return;
        }

        if (string.IsNullOrEmpty(serverScriptPath))
        {
            UnityEngine.Debug.LogWarning("Node server path not set.");
            return;
        }

        nodeProcess = new Process();
        nodeProcess.StartInfo.FileName = "node";
        nodeProcess.StartInfo.Arguments = $"\"{serverScriptPath}\"";
        nodeProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(serverScriptPath);

        // Configure based on useSeparateShell parameter.
        if (useSeparateShell)
        {
            nodeProcess.StartInfo.UseShellExecute = true;
            nodeProcess.StartInfo.CreateNoWindow = false;
        }
        else
        {
            nodeProcess.StartInfo.UseShellExecute = false;
            nodeProcess.StartInfo.CreateNoWindow = true;
            nodeProcess.StartInfo.RedirectStandardOutput = true;
            nodeProcess.StartInfo.RedirectStandardError = true;
        }

        if (!useSeparateShell)
        {
            nodeProcess.OutputDataReceived += (_, e) => { if (e.Data != null) UnityEngine.Debug.Log("[Node] " + e.Data); };
            nodeProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) UnityEngine.Debug.LogError("[Node ERR] " + e.Data); };
        }

        nodeProcess.Start();
        if (!useSeparateShell)
        {
            nodeProcess.BeginOutputReadLine();
            nodeProcess.BeginErrorReadLine();
        }

        // Open in browser if the flag is set.
        if (openInBrowser)
        {
            Application.OpenURL(browserURL);
        }
    }

    void OnApplicationQuit() => KillNode();
#if UNITY_EDITOR
    void OnDisable()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            KillNode();
    }
#endif

    void KillNode()
    {
        if (nodeProcess != null && !nodeProcess.HasExited)
        {
            nodeProcess.Kill();
            nodeProcess.Dispose();
            UnityEngine.Debug.Log("Node server stopped.");
        }
    }
}
