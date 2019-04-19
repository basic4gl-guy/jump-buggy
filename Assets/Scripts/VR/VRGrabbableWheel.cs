using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VRGrabbableWheel : VRGrabbable
{
    [Header("Grab parameters")]
    public float MaxRadius = 1.2f;
    public float GrabRadius = 1.0f;
    public float Depth = 0.1f;

    [Header("Constraints & Behaviour")]
    [Tooltip("Maximum angle turned in degrees. 0 = no restriction")]
    public float MaxTurnAngle = 0.0f;

    [Tooltip("Recenter rate when released in degrees per second")]
    public float RecenterRate = 100.0f;

    public override bool CanGrab(VRGrabPoint pt, out float dist)
    {
        float zDist = Mathf.Abs(pt.LocalPos.z);
        float radDist = new Vector3(pt.LocalPos.x, pt.LocalPos.y, 0.0f).magnitude;

        // Ideal grab point is on circle at GrabRadius
        dist = zDist + Mathf.Abs(radDist - GrabRadius);

        // Grab is valid if within parameters
        return zDist <= Depth && radDist <= MaxRadius;
    }

    private void FixedUpdate()
    {
        if (!IsGrabbed && RecenterRate > 0.0f)
        {
            // Move wheel back towards center
            float recenterDelta = Time.fixedDeltaTime * RecenterRate;            
            var angles = transform.localRotation.eulerAngles;
            angles.z = VRUtil.LocalAngle(angles.z);
            if (Mathf.Abs(angles.z) < recenterDelta)
                angles.z = 0.0f;
            else
                angles.z -= recenterDelta * Mathf.Sign(angles.z);
            transform.localRotation = Quaternion.Euler(angles);
        }
    }

    public override void Moved(List<VRMovedGrabPoint> grabs)
    {
        if (grabs.Count == 1)
        {
            // Rotate by difference between initial and current angle 
            // from center of steering wheel to pt.

            var grab = grabs.Single();
            var startPt = grab.InitialPt.LocalPos;
            var endPos = grab.CurrentPt.LocalPos;

            float startAng = Mathf.Atan2(startPt.x, startPt.y) * Mathf.Rad2Deg;
            float endAng = Mathf.Atan2(endPos.x, endPos.y) * Mathf.Rad2Deg;
            float delta = VRUtil.LocalAngle(endAng - startAng);

            TurnWheelBy(-delta);
        }
        else if (grabs.Count == 2)
        {
            // Rotate by difference between initial and current angle
            // between the two grip points.

            Vector3 startDif = grabs[1].InitialPt.LocalPos - grabs[0].InitialPt.LocalPos;
            Vector3 endDif = grabs[1].CurrentPt.LocalPos - grabs[0].CurrentPt.LocalPos;

            float startAng = Mathf.Atan2(startDif.x, startDif.y) * Mathf.Rad2Deg;
            float endAng = Mathf.Atan2(endDif.x, endDif.y) * Mathf.Rad2Deg;
            float delta = VRUtil.LocalAngle(endAng - startAng);

            // Rotate steering wheel
            TurnWheelBy(-delta);
        }
    }

    private void TurnWheelBy(float delta)
    {
        // Rotate steering wheel
        var angles = transform.localRotation.eulerAngles;
        angles.z = VRUtil.LocalAngle(angles.z + delta);

        // Constrain if necessary
        if (MaxTurnAngle > 0.0f)
            angles.z = Mathf.Clamp(angles.z, -MaxTurnAngle, MaxTurnAngle);

        // Update rotation
        transform.localRotation = Quaternion.Euler(angles);
    }
}
