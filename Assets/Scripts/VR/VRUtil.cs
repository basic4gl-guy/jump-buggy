using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VRUtil
{
    private static bool isInitialised = false;
    private static bool isGo = false;
    private static bool isGearVR = false;

    private static void CheckInitialised()
    {
        // Initialise once, to prevent multiple string manipulations
        if (!isInitialised)
        {
            var vrProduct = OVRPlugin.productName.ToLower();
            isGo = vrProduct.StartsWith("oculus go");
            isGearVR = vrProduct.StartsWith("gear vr");
            isInitialised = true;
        }
    }

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
        CheckInitialised();
        return IsVR() && !isGo && !isGearVR;        // TODO: More robust method of detecting 6DOF?
    }

    public static bool IsVR()
    {
        return UnityEngine.XR.XRSettings.isDeviceActive;
    }

    public static bool IsOculusGoVR()
    {
        return IsVR() && isGo;
    }

    public static void DisableVR()
    {
        UnityEngine.XR.XRSettings.enabled = false;
    }
}
