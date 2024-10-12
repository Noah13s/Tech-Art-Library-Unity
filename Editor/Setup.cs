using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;

public class Setup
{
    [InitializeOnLoadMethod]
    private static async void InstallDependencies()
    {
        await ListPackages();
        await ListSamples("com.n04h.techartlibrary"); // Replace with your package name
    }

    private static async Task ListPackages()
    {
        var listRequest = Client.List(false, true);

        while (!listRequest.IsCompleted)
            await Task.Delay(100);// or Thread.Sleep(100);

        if (listRequest.Error != null)
        {
            Debug.Log("Error: " + listRequest.Error.message);
            return;
        }

        var packages = listRequest.Result;
        var text = new StringBuilder("Packages:\n");
        foreach (var package in packages)
        {
            if (package.name == "com.username.package")
            {
                Debug.LogWarning("The dependency 'com.username.package' is not installed! Installing...");
                await ImportPackage("https://github.com/gitusername/gitpackage.git");
            }
            if (package.source == PackageSource.Registry)
            { 
                text.AppendLine($"{package.name}: {package.version} [{package.resolvedPath}]");
            }
        }
        //Debug.Log(text.ToString()); Prints the detected packages
    }

    public static async Task ListSamples(string packageName)
    {
        // Get the installed package info
        var listRequest = Client.List(false, true);
        while (!listRequest.IsCompleted)
            await Task.Delay(100);

        if (listRequest.Error != null)
        {
            Debug.LogError("Error: " + listRequest.Error.message);
            return;
        }

        var packages = listRequest.Result;
        var package = packages.FirstOrDefault(p => p.name == packageName);
        if (package == null)
        {
            Debug.LogError($"Package {packageName} not found.");
            return;
        }

        // Read the package.json file to find samples
        var manifestPath = Path.Combine(package.resolvedPath, "package.json");
        if (!File.Exists(manifestPath))
        {
            Debug.LogError($"Manifest file not found for package {packageName}.");
            return;
        }

        var manifestJson = File.ReadAllText(manifestPath);
        var manifest = JsonUtility.FromJson<PackageManifest>(manifestJson);

        if (manifest.samples == null || manifest.samples.Length == 0)
        {
            Debug.Log($"No samples found for package {packageName}.");
            return;
        }

        var text = new StringBuilder($"Samples for {packageName}:\n");
        foreach (var sample in manifest.samples)
        {
            var samplePath = Path.Combine("Assets/Samples", package.displayName, package.version, sample.displayName);
            var isImported = Directory.Exists(samplePath);
            text.AppendLine($"  {sample.displayName}: {(isImported ? "Imported" : "Not Imported")}");

            // Add switch statement for each sample
            switch (sample.displayName)
            {
                case "Example 1":

                case "VR":
                    break;
                case "AR":
                    if (isImported)
                    {
                        await ImportPackage("com.unity.xr.arfoundation", sample.displayName);
                        await ImportPackage("com.unity.render-pipelines.universal", sample.displayName);
                        await ImportPackage("com.unity.xr.arcore", sample.displayName);
                        await ImportPackage("com.unity.xr.interaction.toolkit", sample.displayName); 
                        await ImportPackage("com.unity.xr.core-utils", sample.displayName);

                    }
                    break;                    
                case "WebSocket":
                    if (isImported)
                    {
                        await ImportPackage("https://github.com/endel/NativeWebSocket.git#upm", sample.displayName); 
                        await ImportPackage("com.unity.cinemachine", sample.displayName);
                    }
                    break;                    
                case "SerialPort":
                    // These samples are already imported, no action needed
                    break;
                default:
                    Debug.Log($"Unknown sample '{sample.displayName}'. No action taken.");
                    break;
            }
        }

        Debug.Log(text.ToString());
    }

    private static async Task ImportPackage(string packageUrl, string sampleDisplayName="")
    {
        Debug.LogWarning($"The sample '{sampleDisplayName}' contains dependencies! Importing...");        
        var addRequest = Client.Add(packageUrl);
        while (!addRequest.IsCompleted)
            await Task.Delay(100); // or Thread.Sleep(100);

        if (addRequest.Error != null)
        {
            Debug.LogError("Error adding package: " + addRequest.Error.message);
        }
        else
        {
            //Debug.Log("Package added successfully: " + packageUrl);
        }
    }

    private static async Task RemovePackage(string packageUrl)
    {
        var removeRequest = Client.Remove(packageUrl);
        while (!removeRequest.IsCompleted)
            await Task.Delay(100); // or Thread.Sleep(100);

        if (removeRequest.Error != null)
        {
            Debug.LogError("Error removing package: " + removeRequest.Error.message);
        }
        else
        {
            Debug.Log("Package removed successfully: " + packageUrl);
        }
    }

    public static async void RemoveAllPackages()
    {
        var listRequest = Client.List(true); // List all packages (including built-in ones)
        while (!listRequest.IsCompleted)
            await Task.Delay(100);

        if (listRequest.Error != null)
        {
            Debug.LogError("Error listing packages: " + listRequest.Error.message);
            return;
        }

        var packages = listRequest.Result;
        foreach (var package in packages)
        {
            if (package.source == PackageSource.Registry || package.source == PackageSource.Git)
            {
                Debug.Log($"Removing package: {package.name}");
                var removeRequest = Client.Remove(package.name);
                while (!removeRequest.IsCompleted)
                    await Task.Delay(100);

                if (removeRequest.Error != null)
                {
                    Debug.LogError("Error removing package: " + removeRequest.Error.message);
                }
                else
                {
                    Debug.Log("Package removed successfully: " + package.name);
                }
            }
        }
        AssetDatabase.Refresh();
    }
}

[System.Serializable]
public class PackageManifest
{
    public Sample[] samples;
}

[System.Serializable]
public class Sample
{
    public string displayName;
    public string path;
}
