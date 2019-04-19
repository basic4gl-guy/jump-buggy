using System;
using System.Collections.Generic;
using UnityEngine;

public class VRGrabbable : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (VRGrabManager.Instance != null)
            VRGrabManager.Instance.RegisterGrabbable(this);
    }

    private void OnDestroy()
    {
        if (VRGrabManager.Instance != null)
            VRGrabManager.Instance.UnregisterGrabbable(this);
    }

    public virtual bool CanGrab(VRGrabPoint pt, out float dist)
    {
        // Override in descendent classes
        dist = 0.0f;
        return false;
    }

    public virtual void Moved(List<VRMovedGrabPoint> grabs)
    {
        // Override in descendent classes
        // Note: Array may be longer than "count". Extra elements should be ignored.
    }

    /// <summary>
    /// Whether to recalculate the "initial" position of the remaining drag points 
    /// when a grab is released.
    /// </summary>
    public virtual bool RecalcRemainingGrabPtsOnRelease {
        get { return true; }
    }
}
