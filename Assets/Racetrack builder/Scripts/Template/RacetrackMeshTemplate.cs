using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attached to an object to indicate it is a racetrack "template".
/// Template objects provide the meshes that will be wrapped around the race track curves
/// such as the road surface (see RacetrackContinuous). They can also provide objects to 
/// be periodically repeated such as support poles (see RacetrackSpacingGroup and RacetrackSpaced).
/// </summary>
public class RacetrackMeshTemplate : MonoBehaviour
{
    /// <summary>
    /// Search for subtrees with a specific component
    /// </summary>
    /// <typeparam name="T">Type of component to find</typeparam>
    /// <returns>Enumerable of subtrees</returns>
    public IEnumerable<T> FindSubtrees<T>() where T: MonoBehaviour
    {
        return FindSubtrees<T>(gameObject);
    }

    /// <summary>
    /// Search for subtrees with a specific component
    /// </summary>
    /// <typeparam name="T">Type of component to find</typeparam>
    /// <param name="o">Object to search from</param>
    /// <returns>Enumerable of subtrees</returns>
    private IEnumerable<T> FindSubtrees<T>(GameObject o) where T: MonoBehaviour
    {
        var component = o.GetComponent<T>();
        if (component != null)
        {
            yield return component;
        }
        else
        {
            // Recurse children
            for (int i = 0; i < o.transform.childCount; i++)
                foreach (var s in FindSubtrees<T>(o.transform.GetChild(i).gameObject))
                    yield return s;
        }
    }
}
