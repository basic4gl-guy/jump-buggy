using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VRUtil
{
    /// <summary>
    /// Convert angle to equivalent angle between [-180, 180)
    /// </summary>
    public static float LocalAngle(float angle)
    {
        // TODO: Optimise for large angles
        while (angle >= 180.0f)
            angle -= 360.0f;
        while (angle < -180.0f)
            angle += 360.0f;
        return angle;
    }

    public static bool Is6DOFVR()
    {
        // VR not active => False
        if (!IsVR())
            return false;

        // Oculus Go and Gear VR are only 3DOF
        // TODO: More robust way to distinguish 6DOF from 3DOF?
        var vrProduct = OVRPlugin.productName.ToLower();
        if (vrProduct.StartsWith("oculus go") || vrProduct.StartsWith("gear vr"))
            return false;

        return true;
    }

    public static bool IsVR()
    {
        return UnityEngine.XR.XRSettings.isDeviceActive;
    }

    public static void DisableVR()
    {
        UnityEngine.XR.XRSettings.enabled = false;
    }
}
