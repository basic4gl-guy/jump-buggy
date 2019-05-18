using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Default runtime implementation (no Undo tracking etc)
/// </summary>
public sealed class RuntimeRacetrackHostServices : IRacetrackHostServices
{
    public static readonly RuntimeRacetrackHostServices Instance = new RuntimeRacetrackHostServices();

    private RuntimeRacetrackHostServices() { }

    public void DestroyObject(UnityEngine.Object o)
    {
        if (Application.isEditor)
            UnityEngine.Object.DestroyImmediate(o);
        else
            UnityEngine.Object.Destroy(o);
    }

    public void ObjectChanging(UnityEngine.Object o)
    {
    }

    public void ObjectCreated(UnityEngine.Object o)
    {
    }

    public void SetTransformParent(Transform transform, Transform parent)
    {
        transform.parent = parent;
    }

    public void GenerateSecondaryUVSet(Mesh mesh)
    {
    }
}
