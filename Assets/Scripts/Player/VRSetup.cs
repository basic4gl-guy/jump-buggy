using UnityEngine;

public class VRSetup : MonoBehaviour
{
    [Tooltip("Forcibly disable VR")]
    public bool disableVR = false;

    [Tooltip("Eye offset from origin. Applied for non-VR and 3DOF VR only. For 6DOF VR, set the 'Tracking origin type' to 'Floor level', and set 'Use position tracking'.")]
    public Vector3 EyeOffset = new Vector3(0.0f, 1.25f, 0.0f);

    [Header("Non VR mode")]
    [Tooltip("Force field of view in non-VR mode")]
    public float nonVRFieldOfView = 60.0f;

    [Tooltip("X axis rotation adjustment in non-VR mode")]
    public float nonVRRotation = 15.0f;    

    // Start is called before the first frame update
    void Start()
    {
        // Forcibly disable VR
        if (disableVR)
        {
            VRUtil.DisableVR();
        }

        // Set camera angle and FOV if not in VR mode
        if (!VRUtil.IsVR())
        {
            Camera.main.fieldOfView = nonVRFieldOfView;
            Vector3 angles = transform.localRotation.eulerAngles;
            transform.localRotation = Quaternion.Euler(angles.x + nonVRRotation, angles.y, angles.z);
        }

        // Adjust camera position if not in 6DOF VR mode (as by default it is at floor level)
        if (!VRUtil.Is6DOFVR())
        {
            transform.localPosition += EyeOffset;
        }
    }
}
