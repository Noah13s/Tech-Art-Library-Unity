using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class GenerateUIRoundedRect : MonoBehaviour
{
    public float width = 200f;    // Width of the rectangle
    public float height = 100f;   // Height of the rectangle
    [Range(0f, 100f)]
    public float roundness = 20f; // Radius of the rounded corners
    public Color shapeColor = Color.red;
    public bool autoPixelResolution = true;
    public int manualResolution = 100;

    private Image image;

    void OnValidate()
    {
        // Ensure the GameObject has an Image component
        image = GetComponent<Image>();

        // Set the size of the RectTransform
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);

        // Calculate texture resolution
        int textureResolution = autoPixelResolution ? Mathf.CeilToInt(Mathf.Max(width, height)) : manualResolution;

        // Generate rounded rectangle sprite
        image.sprite = GenerateRoundedRectSprite(width, height, roundness, textureResolution);
        image.color = shapeColor;
    }

    Sprite GenerateRoundedRectSprite(float width, float height, float roundness, int resolution)
    {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;  // Ensure pixel-perfect rendering
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float normalizedX = (x / (float)resolution - 0.5f) * width;
                float normalizedY = (y / (float)resolution - 0.5f) * height;

                bool inside = false;

                if (Mathf.Abs(normalizedX) <= width / 2 - roundness && Mathf.Abs(normalizedY) <= height / 2 - roundness)
                {
                    inside = true;
                }
                else if (Mathf.Abs(normalizedX) <= width / 2 && Mathf.Abs(normalizedY) <= height / 2)
                {
                    float cornerX = Mathf.Max(Mathf.Abs(normalizedX) - (width / 2 - roundness), 0);
                    float cornerY = Mathf.Max(Mathf.Abs(normalizedY) - (height / 2 - roundness), 0);
                    if (cornerX * cornerX + cornerY * cornerY <= roundness * roundness)
                    {
                        inside = true;
                    }
                }

                pixels[y * resolution + x] = inside ? new Color(1, 1, 1, 1) : new Color(0, 0, 0, 0);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Rect rect = new Rect(0, 0, resolution, resolution);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        return Sprite.Create(texture, rect, pivot, pixelsPerUnit: resolution / Mathf.Max(width, height));
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
