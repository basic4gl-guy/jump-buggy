using System.Collections.Generic;
using UnityEngine;

public class VRGrabber : MonoBehaviour
{
    [Tooltip("Which controller to use. Use LTouch/RTouch for left/right hands")]
    public OVRInput.Controller Controller;

    public float GrabBegin = 0.55f;
    public float GrabEnd = 0.35f;

    internal float flex;
    internal bool isGrabbing;

    private void FixedUpdate()
    {
        if (VRGrabManager.Instance == null) return;

        // Detect grab/release
        var newFlex = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, Controller);

        // Beginning grab?
        if (newFlex >= GrabBegin && flex < GrabBegin)
        {
            isGrabbing = VRGrabManager.Instance.BeginGrab(this);
        }

        // Ending grab?
        if (isGrabbing && newFlex < GrabEnd && flex >= GrabEnd)
        {
            VRGrabManager.Instance.EndGrab(this);
            isGrabbing = false;
        }

        flex = newFlex;
    }
}
