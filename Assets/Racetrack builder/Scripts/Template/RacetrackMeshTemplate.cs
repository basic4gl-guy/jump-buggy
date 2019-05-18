using System.Collections.Generic;
using System.Linq;
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
    /// Get template space from subtree space transformation matrix
    /// </summary>
    /// <param name="subtree">Component in the subtree object</param>
    /// <returns>Corresponding transformation matrix</returns>
    public Matrix4x4 GetTemplateFromSubtreeMatrix(Component subtree)
    {
        // Note: Rotation and transformation of this object is effectively cancelled out.
        // However we multiply back in the scale factor, as this allows mesh templates to
        // be scaled easily which is useful.
        return Matrix4x4.Scale(transform.lossyScale) * RacetrackUtil.GetAncestorFromDescendentMatrix(this, subtree);
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
