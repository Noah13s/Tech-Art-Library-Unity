using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SlideTransition : Transition
{
    private RectTransform slidePanel;
    private Vector2 startPos;
    private Vector2 targetPos;

    private Image slideImage;

    public override IEnumerator PlayTransition(bool isEntering)
    {

        if (isEntering)
        {
            // Create panel for slide effect if not already created
            if (slidePanel == null)
            {
                GameObject panelObject = new GameObject("SlidePanel");
                slidePanel = panelObject.AddComponent<RectTransform>();
                slidePanel.SetParent(transitionCanvas.transform, false);
                slidePanel.anchorMin = Vector2.zero;
                slidePanel.anchorMax = Vector2.one;
                slidePanel.sizeDelta = Vector2.zero;

                // Set panel color to black and opaque
                slideImage = slidePanel.gameObject.AddComponent<Image>();
                slideImage.color = Color.black;
            }

            startPos = new Vector2(-Screen.width, 0);
            targetPos = Vector2.zero;

            // Set initial position based on entering or exiting
            slidePanel.anchoredPosition = startPos;

            float duration = 1.0f; // Duration of the slide
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                slidePanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

                yield return null;
            }

            slidePanel.anchoredPosition = targetPos; // Ensure final position
        }
        else
        {
            // If exiting, just move the panel out of view
            if (slidePanel != null)
            {
                startPos = Vector2.zero;
                targetPos = new Vector2(Screen.width, 0);

                // Set initial position
                slidePanel.anchoredPosition = startPos;

                float duration = 1.0f; // Duration of the slide
                float elapsedTime = 0f;

                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / duration;
                    slidePanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

                    yield return null;
                }

                slidePanel.anchoredPosition = targetPos; // Ensure final position
            }
        }

        // Uncomment if you want to destroy the slidePanel after transition (optional)
        if (!isEntering)
        {
            Cleanup();
        }
    }    
}
