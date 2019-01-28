using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Miscellaneous co-routine utilities
/// </summary>
public static class RacetrackCoroutineUtil
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
    /// Convert an action into a coroutine
    /// </summary>
    public static IEnumerator Do(Action action)
    {
        action();
        yield return null;
    }
}
