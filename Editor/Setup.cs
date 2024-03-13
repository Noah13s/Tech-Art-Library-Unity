public class Setup
{
    [InitializeOnLoadMethod]
    public static async void InstallDependencies()
    {
        var value = Client.List(false, true);

        while (!value.IsCompleted)
            await Task.Delay(100);

        foreach (var item in value.Result)
        {
            if (item.name == "com.username.package")
                return;
        }

        Debug.LogWarning("The dependency 'com.username.package' is not installed! Installing...");
        Client.Add("https://github.com/gitusername/gitpackage.git");
    }
 