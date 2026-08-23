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

        string resolvedServerScriptPath = ResolveServerScriptPath();
        if (string.IsNullOrEmpty(resolvedServerScriptPath) || !File.Exists(resolvedServerScriptPath))
        {
            UnityEngine.Debug.LogWarning($"Node server script was not found: {serverScriptPath}", this);
            return;
        }

        Process process = new Process();
        process.StartInfo.FileName = "node";
        process.StartInfo.Arguments = $"\"{resolvedServerScriptPath}\"";
        process.StartInfo.WorkingDirectory = Path.GetDirectoryName(resolvedServerScriptPath);

        // Configure based on useSeparateShell parameter.
        if (useSeparateShell)
        {
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = false;
        }
        else
        {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
        }

        if (!useSeparateShell)
        {
            process.OutputDataReceived += (_, e) => { if (e.Data != null) UnityEngine.Debug.Log("[Node] " + e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) UnityEngine.Debug.LogError("[Node ERR] " + e.Data); };
        }

        try
        {
            process.Start();
            nodeProcess = process;

            if (!useSeparateShell)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
        }
        catch (System.Exception exception)
        {
            process.Dispose();
            UnityEngine.Debug.LogError($"Failed to start the Node server: {exception.Message}", this);
            return;
        }

        // Open in browser if the flag is set.
        if (openInBrowser)
        {
            Application.OpenURL(browserURL);
        }
    }

    private string ResolveServerScriptPath()
    {
        if (string.IsNullOrWhiteSpace(serverScriptPath))
        {
            return null;
        }

        if (Path.IsPathRooted(serverScriptPath))
        {
            return Path.GetFullPath(serverScriptPath);
        }

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        return projectRoot == null ? null : Path.GetFullPath(Path.Combine(projectRoot, serverScriptPath));
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
        Process process = nodeProcess;
        nodeProcess = null;

        if (process == null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill();
                UnityEngine.Debug.Log("Node server stopped.");
            }
        }
        catch (System.InvalidOperationException)
        {
            // The process was never started or has already been disposed.
        }
        finally
        {
            process.Dispose();
        }
    }
}
