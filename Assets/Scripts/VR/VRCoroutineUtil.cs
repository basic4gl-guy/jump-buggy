using System;
using System.Collections;
using UnityEngine;

public static class VRCoroutineUtil
{
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

    private static IEnumerator Fade(Func<float, float> fadeFn, float fadeTime = 0.0f)
    {
        // Look for fader
        var fade = GameObject.FindObjectOfType<OVRScreenFade>();
        if (fade != null)
        {
            // Use fadeout default time if not specified
            if (fadeTime == 0.0f)
                fadeTime = fade.fadeTime;

            return RacetrackCoroutineUtil.LerpUnscaled(fadeTime, t => fade.SetFadeLevel(fadeFn(t)));
        }
        else
        {
            Debug.LogWarning("CoroutineUtils.Fade: No OVRScreenFade object found.");
            return RacetrackCoroutineUtil.EmptyCoroutine();
        }
    }
}
