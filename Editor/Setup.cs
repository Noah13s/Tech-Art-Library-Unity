using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;

public class Setup
{
    [InitializeOnLoadMethod]
    public static async void InstallDependencies()
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
                Client.Add("https://github.com/gitusername/gitpackage.git");
            }
            if (package.source == PackageSource.Registry)
            { 
                text.AppendLine($"{package.name}: {package.version} [{package.resolvedPath}]");
            }
        }
        Debug.Log(text.ToString());
    }
}