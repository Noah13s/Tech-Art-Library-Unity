using UnityEngine;
using UnityEngine.UI;

public class RadialGradient : MonoBehaviour
{
    public int resolution = 512;  // Resolution of the gradient texture
    public Color innerColor = Color.white;  // Center color of the gradient
    public Color outerColor = Color.black;  // Outer color of the gradient
    public bool invertGradient = false;  // Flag to invert the gradient

    private RawImage rawImage;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        Texture2D gradientTexture = CreateRadialGradientTexture(resolution, innerColor, outerColor, invertGradient);
        rawImage.texture = gradientTexture;
    }

    private Texture2D CreateRadialGradientTexture(int size, Color centerColor, Color outerColor, bool invert)
    {
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2, size / 2);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size / 2);
                if (invert)
                    distance = 1 - distance;
                Color pixelColor = Color.Lerp(centerColor, outerColor, distance);
                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        return texture;
    }
}
