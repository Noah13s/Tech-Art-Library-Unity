using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Master_Scene_Manager : MonoBehaviour
{
    [StyledString(12,1,1,1)]
    [SerializeField]
    private string currentScene;

    private static Master_Scene_Manager instance = null;
    public static Master_Scene_Manager Instance => instance;

    // Events for scene loading and unloading
    public event Action OnSceneLoadStart;
    public event Action OnSceneLoadComplete;
    public event Action OnSceneUnloadStart;
    public event Action OnSceneUnloadComplete;

    private Transition currentTransition;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            instance = this;
        }
        DontDestroyOnLoad(this.gameObject);
    }

    private void OnDrawGizmos()
    {
        currentScene = SceneManager.GetActiveScene().name;
    }

    public void LoadScene(string sceneName, Transition transition)
    {
        currentTransition = transition;
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        OnSceneLoadStart?.Invoke();

        if (currentTransition != null)
        {
            currentTransition.Setup();
            yield return StartCoroutine(currentTransition.PlayTransition(true));
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }

        if (currentTransition != null)
            yield return StartCoroutine(currentTransition.PlayTransition(false));

        OnSceneLoadComplete?.Invoke();
    }

    public void UnloadScene(string sceneName, Transition transition)
    {
        currentTransition = transition;
        StartCoroutine(UnloadSceneAsync(sceneName));
    }

    private IEnumerator UnloadSceneAsync(string sceneName)
    {
        OnSceneUnloadStart?.Invoke();

        if (currentTransition != null)
        {
            currentTransition.Setup();
            yield return StartCoroutine(currentTransition.PlayTransition(true));
        }

        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);

        while (!asyncUnload.isDone)
        {
            yield return null;
        }

        if (currentTransition != null)
            yield return StartCoroutine(currentTransition.PlayTransition(false));

        OnSceneUnloadComplete?.Invoke();
    }

    // Method for persisting data across scenes
    public void PersistAcrossScenes(GameObject obj)
    {
        DontDestroyOnLoad(obj);
    }

    // Error handling example
    private void HandleSceneLoadError(string sceneName)
    {
        Debug.LogError($"Failed to load scene: {sceneName}");
        // Implement additional error handling logic here
    }

    private void HandleSceneUnloadError(string sceneName)
    {
        Debug.LogError($"Failed to unload scene: {sceneName}");
        // Implement additional error handling logic here
    }
}
