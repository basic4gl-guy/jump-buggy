using System;
using System.Collections;
using UnityEngine;

public static class CoroutineUtils
{
    /// <summary>
    /// Perform two coroutines sequentially
    /// </summary>
    public static IEnumerator Then(this IEnumerator first, IEnumerator second)
    {
        return (new[] { first, second }).GetEnumerator();
    }

    /// <summary>
    /// Perform coroutine, then action
    /// </summary>
    public static IEnumerator Then(this IEnumerator first, Action second)
    {
        return first.Then(Do(second));
    }

    public static IEnumerator EmptyCoroutine()
    {
        return (new object[0]).GetEnumerator();
    }

    /// <summary>
    /// Lerp from 0 to 1 using unscaled time.
    /// </summary>
    public static IEnumerator LerpUnscaled(float duration, Action<float> action)
    {
        float elapsedTime = 0.0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            action(Mathf.Clamp01(elapsedTime / duration));
            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// Fade in
    /// </summary>
    public static IEnumerator FadeIn(float fadeTime = 0.0f)
    {
        return Fade(t => 1.0f - t, fadeTime);
    }

    /// <summary>
    /// Fadeout coroutine
    /// </summary>
    /// <remarks>
    /// Requires an OVRScreenFade object somewhere in the scene
    /// </remarks>
    public static IEnumerator FadeOut(float fadeTime = 0.0f)
    {
        return Fade(t => t, fadeTime);
    }

    /// <summary>
    /// Convert an action into a coroutine
    /// </summary>
    public static IEnumerator Do(Action action)
    {
        action();
        yield return null;
    }

    private static IEnumerator Fade(Func<float, float> fadeFn, float fadeTime = 0.0f)
    {
        // Look for fader
        var fade = GameObject.FindObjectOfType<OVRScreenFade>();
        if (fade != null)
        {
            // Use fadeout default time if not specified
            if (fadeTime == 0.0f)
                fadeTime = fade.fadeTime;

            return LerpUnscaled(fadeTime, t => fade.SetFadeLevel(fadeFn(t)));
        }
        else
        {
            Debug.LogWarning("CoroutineUtils.Fade: No OVRScreenFade object found.");
            return EmptyCoroutine();
        }
    }
}
