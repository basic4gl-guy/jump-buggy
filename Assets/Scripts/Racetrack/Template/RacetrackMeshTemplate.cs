using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RacetrackMeshTemplate : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerable<T> FindSubtrees<T>() where T: MonoBehaviour
    {
        return FindSubtrees<T>(gameObject);
    }

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
