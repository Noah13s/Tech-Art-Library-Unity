#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SaveManager : MonoBehaviour
{
    [SerializeField]
#if UNITY_EDITOR
    [Button("Open file", "OpenSaveFile")]
#endif
    private bool openFileButton;

    [SerializeField]
#if UNITY_EDITOR
    [Button("Save scene", "SaveToFile")]
#endif
    private bool saveSceneButton;

    [SerializeField]
#if UNITY_EDITOR
    [Button("Test", "CreateAndLoadNewScene")]
#endif
    private bool testButton;

    [SerializeField]
    private List<GameObject> prefabs; // List of prefabs to use during loading

    void Start()
    {

    }

    void Update()
    {

    }

    public void OpenSaveFile()
    {
        string path = EditorUtility.OpenFilePanel("Open save file", "", "json");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        // Read the JSON file
        string jsonContent = File.ReadAllText(path);
        Debug.Log(jsonContent);

        // Parse the JSON file
        GameObjectInfoWrapper gameObjectInfoWrapper = JsonUtility.FromJson<GameObjectInfoWrapper>(jsonContent);

        // Create and load a new empty scene
        CreateAndLoadNewScene();

        // Instantiate game objects based on the parsed JSON
        foreach (var gameObjectInfo in gameObjectInfoWrapper.gameObjects)
        {
            // Check if the prefab exists in the list
            GameObject prefab = prefabs.FirstOrDefault(p => p.name == gameObjectInfo.name);
            if (prefab != null)
            {
                // Instantiate prefab
                GameObject newGameObject = Instantiate(prefab);
                // Optionally, set prefab properties (e.g., Transform)
                ApplyTransform(newGameObject.transform, gameObjectInfo.transformInfo);
            }
            else
            {
                // Create a new GameObject if no prefab is found
                GameObject newGameObject = new GameObject(gameObjectInfo.name);
                ApplyTransform(newGameObject.transform, gameObjectInfo.transformInfo);

                foreach (var componentInfo in gameObjectInfo.components)
                {
                    var componentType = System.Type.GetType("UnityEngine." + componentInfo.type + ", UnityEngine");
                    if (componentType != null)
                    {
                        var component = newGameObject.AddComponent(componentType);
                        ApplyComponentProperties(component, componentInfo);
                    }
                    else
                    {
                        Debug.LogWarning($"Component {componentInfo.type} not found.");
                    }
                }
            }
        }
    }

    private static void ApplyTransform(Transform transform, TransformInfo transformInfo)
    {
        if (transformInfo != null)
        {
            transform.position = transformInfo.position;
            transform.rotation = Quaternion.Euler(transformInfo.rotation);
            transform.localScale = transformInfo.scale;
        }
    }

    private static void ApplyComponentProperties(Component component, ComponentInfo componentInfo)
    {
        if (componentInfo.properties != null)
        {
            foreach (var prop in componentInfo.properties)
            {
                var field = component.GetType().GetField(prop.name);
                if (field != null)
                {
                    field.SetValue(component, prop.value);
                }
            }
        }
    }

    private static void CreateAndLoadNewScene()
    {
        // Create a new empty scene
        if (Application.isPlaying)
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            Scene newScene = SceneManager.CreateScene("NewEmptyScene");
            SceneManager.UnloadSceneAsync(currentSceneIndex);
            SceneManager.SetActiveScene(newScene);
        }
        else
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
    }

    public void SaveToFile()
    {
        List<GameObjectInfo> gameObjectInfos = new List<GameObjectInfo>();

        foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
        {
            GameObjectInfo gameObjectInfo = new GameObjectInfo();
            gameObjectInfo.name = obj.name;

            // Save transform properties
            Transform transform = obj.transform;
            gameObjectInfo.transformInfo = new TransformInfo
            {
                position = transform.position,
                rotation = transform.rotation.eulerAngles,
                scale = transform.localScale
            };

            // Save component info
            List<ComponentInfo> componentInfos = new List<ComponentInfo>();
            foreach (Component component in obj.GetComponents<Component>())
            {
                ComponentInfo componentInfo = new ComponentInfo
                {
                    type = component.GetType().Name,
                    properties = new List<PropertyInfo>()
                };

                // Serialize component properties
                var fields = component.GetType().GetFields();
                foreach (var field in fields)
                {
                    if (field.IsPublic)
                    {
                        componentInfo.properties.Add(new PropertyInfo
                        {
                            name = field.Name,
                            value = field.GetValue(component)
                        });
                    }
                }

                componentInfos.Add(componentInfo);
            }

            gameObjectInfo.components = componentInfos;
            gameObjectInfos.Add(gameObjectInfo);
        }

        GameObjectInfoWrapper gameObjectInfoWrapper = new GameObjectInfoWrapper();
        gameObjectInfoWrapper.gameObjects = gameObjectInfos;

        string json = JsonUtility.ToJson(gameObjectInfoWrapper, true);

        string path = EditorUtility.SaveFilePanel("Save GameObject Names and Components", "", "GameObjectNamesAndComponents.json", "json");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        File.WriteAllText(path, json);

        EditorUtility.DisplayDialog("Success", "GameObject names and components saved to JSON file successfully!", "OK");
    }

    [System.Serializable]
    private class GameObjectInfo
    {
        public string name;
        public TransformInfo transformInfo;
        public List<ComponentInfo> components;
    }

    [System.Serializable]
    private class TransformInfo
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }

    [System.Serializable]
    private class ComponentInfo
    {
        public string type;
        public List<PropertyInfo> properties;
    }

    [System.Serializable]
    private class PropertyInfo
    {
        public string name;
        public object value;
    }

    [System.Serializable]
    private class GameObjectInfoWrapper
    {
        public List<GameObjectInfo> gameObjects;
    }
}
#endif
