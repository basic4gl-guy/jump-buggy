using UnityEngine;

[RequireComponent(typeof(OVRManager))]
public class VRSetup : MonoBehaviour
{
    // Forcibly disable VR. (Useful for testing.)
    public bool disableVR = false;    
    public Vector3 floorLocalPosition = new Vector3(0.0f, 0.0f, 0.0f);

    // Start is called before the first frame update
    void Start()
    {
        // Forcibly disable VR
        if (disableVR)
            UnityEngine.XR.XRSettings.enabled = false;

        // Set floor level tracking if 6DOF VR is active
        if (Is6DOFVR())
        {
            var ovr = GetComponent<OVRManager>();
            if (ovr != null)
            {
                ovr.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
                ovr.usePositionTracking = true;
                transform.localPosition = floorLocalPosition;
            }
            else
                Debug.LogError("OVRManager component not found.");
        }
    }

    private bool Is6DOFVR()
    {
        // VR not active => False
        if (!UnityEngine.XR.XRSettings.isDeviceActive)
            return false;

        // Oculus Go and Gear VR are only 3DOF
        // TODO: More robust way to distinguish 6DOF from 3DOF?
        var vrProduct = OVRPlugin.productName.ToLower();
        if (vrProduct.StartsWith("oculus go") || vrProduct.StartsWith("gear vr"))
            return false;

        return true;
    }
}
