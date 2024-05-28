using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))] // Ensure there is always an Image component
public class GenerateUIShape : MonoBehaviour
{
    public float a = 100f;  // Semi-major axis
    public float b = 50f;   // Semi-minor axis
    public Color shapeColor = Color.red;
    public bool autoPixelResolution = true;
    public int manualResolution = 100;

    private Image image;
    private Button button; // Reference to the Button component

    void OnValidate()
    {
        // Get the Image component
        image = GetComponent<Image>();

        // Ignore if the GameObject has a Button component
        button = GetComponent<Button>();
        if (button != null)
        {
            Debug.LogWarning("The GameObject contains a Button component. This script will not modify the appearance of the Button.");
            return;
        }

        // Set the size of the RectTransform
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(a * 2, b * 2);

        // Calculate texture resolution
        int textureResolution = autoPixelResolution ? Mathf.CeilToInt(Mathf.Max(a, b) * 2) : manualResolution;

        // Generate ellipse sprite
        image.sprite = GenerateEllipseSprite(a, b, textureResolution);
        image.color = shapeColor;
    }

    Sprite GenerateEllipseSprite(float a, float b, int resolution)
    {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;  // Ensure pixel-perfect rendering
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float normalizedX = (x / (float)resolution - 0.5f) * 2;
                float normalizedY = (y / (float)resolution - 0.5f) * 2;

                float value = Mathf.Pow(normalizedX * a, 2) / Mathf.Pow(a, 2) + Mathf.Pow(normalizedY * b, 2) / Mathf.Pow(b, 2);

                if (value <= 1)
                {
                    pixels[y * resolution + x] = new Color(1, 1, 1, 1);
                }
                else
                {
                    pixels[y * resolution + x] = new Color(0, 0, 0, 0);
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Rect rect = new Rect(0, 0, resolution, resolution);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        return Sprite.Create(texture, rect, pivot, pixelsPerUnit: resolution / (Mathf.Max(a, b) * 2));
    }

    void OnDestroy()
    {
        // Cleanup sprite texture
        if (image != null && image.sprite != null)
        {
            DestroyImmediate(image.sprite.texture);
            DestroyImmediate(image.sprite);
        }
    }
}
