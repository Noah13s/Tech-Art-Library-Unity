using UnityEngine;

[RequireComponent(typeof(Animation))]
public class LegacyAnimationHandler : MonoBehaviour
{
    private Animation animationComponent;

    // Initialize the Animation component
    private void Awake()
    {
        animationComponent = GetComponent<Animation>();

        if (animationComponent == null)
        {
            Debug.LogError("No Animation component found on " + gameObject.name);
        }
    }

    // Play an animation clip by name
    public void PlayAnimation(string clipName)
    {
        if (animationComponent != null && animationComponent.GetClip(clipName) != null)
        {
            animationComponent.Play(clipName);
        }
        else
        {
            Debug.LogError("Animation Clip not found: " + clipName);
        }
    }

    // Play a specific AnimationClip directly
    public void PlayAnimationClip(AnimationClip clip)
    {
        if (clip != null)
        {
            animationComponent.AddClip(clip, clip.name);
            animationComponent.Play(clip.name);
        }
        else
        {
            Debug.LogError("Invalid AnimationClip provided");
        }
    }

    // Crossfade to another animation over time
    public void CrossFadeAnimation(string clipName, float fadeLength)
    {
        if (animationComponent != null && animationComponent.GetClip(clipName) != null)
        {
            animationComponent.CrossFade(clipName, fadeLength);
        }
        else
        {
            Debug.LogError("Animation Clip not found: " + clipName);
        }
    }

    // Pause the current animation
    public void PauseAnimation()
    {
        if (animationComponent != null)
        {
            animationComponent[animationComponent.clip.name].speed = 0;
        }
    }

    // Resume the current animation
    public void ResumeAnimation()
    {
        if (animationComponent != null)
        {
            animationComponent[animationComponent.clip.name].speed = 1;
        }
    }

    // Stop a specific animation
    public void StopAnimation(string clipName)
    {
        if (animationComponent != null && animationComponent.GetClip(clipName) != null)
        {
            animationComponent.Stop(clipName);
        }
    }

    // Set the speed of a specific animation
    public void SetAnimationSpeed(string clipName, float speed)
    {
        if (animationComponent != null && animationComponent.GetClip(clipName) != null)
        {
            animationComponent[clipName].speed = speed;
        }
        else
        {
            Debug.LogError("Animation Clip not found: " + clipName);
        }
    }

    // Check if a certain animation is playing
    public bool IsPlaying(string clipName)
    {
        if (animationComponent != null)
        {
            return animationComponent.IsPlaying(clipName);
        }
        return false;
    }

    // Get the current time of an animation
    public float GetAnimationTime(string clipName)
    {
        if (animationComponent != null && animationComponent.GetClip(clipName) != null)
        {
            return animationComponent[clipName].time;
        }
        else
        {
            Debug.LogError("Animation Clip not found: " + clipName);
            return 0f;
        }
    }

    // Add an AnimationClip dynamically
    public void AddAnimationClip(AnimationClip clip)
    {
        if (clip != null)
        {
            animationComponent.AddClip(clip, clip.name);
        }
        else
        {
            Debug.LogError("Invalid AnimationClip provided");
        }
    }

    // Remove an AnimationClip dynamically
    public void RemoveAnimationClip(string clipName)
    {
        if (animationComponent != null && animationComponent.GetClip(clipName) != null)
        {
            animationComponent.RemoveClip(clipName);
        }
        else
        {
            Debug.LogError("Animation Clip not found: " + clipName);
        }
    }
}
