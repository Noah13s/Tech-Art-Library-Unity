using UnityEngine;
using UnityEngine.UI;

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
    public bool addOutline = false;
    public Color outlineColor = Color.black;
    public float outlineSize = 5f; // Size of the outline

    private Image image;

    void Awake()
    {
        // Ensure the GameObject has an Image component
        image = GetComponent<Image>();

        // Set the size of the RectTransform
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);
    }

    void Start()
    {
        // Generate and apply the rounded rectangle sprite
        int textureResolution = autoPixelResolution ? Mathf.CeilToInt(Mathf.Max(width, height)) : manualResolution;
        image.sprite = GenerateRoundedRectSprite(width, height, roundness, textureResolution);
        image.color = Color.white;
    }

    Sprite GenerateRoundedRectSprite(float width, float height, float roundness, int resolution)
    {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Bilinear;  // Use bilinear filtering
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[resolution * resolution];

        float halfWidth = width / 2;
        float halfHeight = height / 2;
        float innerRoundness = roundness;
        float outlineRoundness = roundness - outlineSize;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float normalizedX = (x / (float)resolution - 0.5f) * width;
                float normalizedY = (y / (float)resolution - 0.5f) * height;

                bool insideShape = IsInside(normalizedX, normalizedY, halfWidth, halfHeight, innerRoundness);
                bool insideOutline = addOutline && IsInside(normalizedX, normalizedY, halfWidth - outlineSize, halfHeight - outlineSize, outlineRoundness);

                if (insideShape)
                {
                    // Apply outline color if inside outline area and addOutline is true
                    if (insideOutline)
                    {
                        pixels[y * resolution + x] = outlineColor;
                    }
                    else
                    {
                        pixels[y * resolution + x] = shapeColor; // Apply shape color
                    }
                }
                else
                {
                    pixels[y * resolution + x] = new Color(0, 0, 0, 0); // Transparent
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Rect rect = new Rect(0, 0, resolution, resolution);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        return Sprite.Create(texture, rect, pivot, pixelsPerUnit: resolution / Mathf.Max(width, height));
    }

    bool IsInside(float x, float y, float halfWidth, float halfHeight, float roundness)
    {
        if (Mathf.Abs(x) <= halfWidth - roundness && Mathf.Abs(y) <= halfHeight - roundness)
        {
            return true;
        }
        if (Mathf.Abs(x) <= halfWidth && Mathf.Abs(y) <= halfHeight)
        {
            float cornerX = Mathf.Max(Mathf.Abs(x) - (halfWidth - roundness), 0);
            float cornerY = Mathf.Max(Mathf.Abs(y) - (halfHeight - roundness), 0);
            if (cornerX * cornerX + cornerY * cornerY <= roundness * roundness)
            {
                return true;
            }
        }
        return false;
    }

    void OnDestroy()
    {
        // Cleanup sprite texture
        if (image != null && image.sprite != null)
        {
            Destroy(image.sprite.texture);
            Destroy(image.sprite);
        }
    }
}
