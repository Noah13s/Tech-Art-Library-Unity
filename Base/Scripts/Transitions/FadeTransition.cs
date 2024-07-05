using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeTransition : Transition
{
    private Image fadeImage;

    public override IEnumerator PlayTransition(bool isEntering)
    {
        if (isEntering)
        {
            // Create an Image for the fade effect
            fadeImage = new GameObject("FadeImage").AddComponent<Image>();
            fadeImage.transform.SetParent(transitionCanvas.transform, false);
            fadeImage.rectTransform.anchorMin = Vector2.zero;
            fadeImage.rectTransform.anchorMax = Vector2.one;
            fadeImage.color = new Color(0, 0, 0, 0); // Start fully transparent
        }

        float duration = 1.0f; // Duration of the fade
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = isEntering ? elapsedTime / duration : 1 - (elapsedTime / duration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, isEntering ? 1 : 0);

        // Cleanup the transition objects once the transition is done
        if (!isEntering)
        {
            Cleanup();
        }
    }
}
