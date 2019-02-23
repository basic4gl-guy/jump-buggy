using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontWheelAnimator : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Wheel object animated by the car. Typically hidden. This will be used to get wheel position input")]
    public Transform ReferenceWheel;

    // Expected scenegraph:
    //  Parent                  <-- Component should be added to this object
    //   +--Upper wishbone
    //   +--Lower wishbone
    //   +--Wheel upright
    //   |  +--Wheel
    //   +--Reference wheel
    // Parent should be mid way between where the wishbones attach to the car
    // Wheel should initially be level
    // Wheel upright should be at (h,0,0), where horizontal length of the wishbones
    // Wheel upright centre of rotation should be aligned with the socket joints, so that steering can be animated
    // by rotating it around its local Y axis.
    // The wheel should be at (?,0,0) so that it can be rotated around the local X axis.

    [Header("Animated pieces")]
    public Transform WheelUpright;
    public Transform Wheel;
    public Transform UpperWishbone;
    public Transform LowerWishbone;

    [Header("Parameters")]
    public float MaxDownAngle = 70.0f;
    public float MaxUpAngle = 70.0f;
    public float LerpFactor = 0.1f;

    private bool isConfigured;
    private float h;
    private float hsign;
    private float prevY = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        isConfigured = 
            ReferenceWheel != null &&
            WheelUpright != null &&
            Wheel != null &&
            UpperWishbone != null &&
            LowerWishbone != null;

        if (!isConfigured)
            Debug.LogError("FrontWheelAnimator is not fully configured");

        // Store horizontal distance to wheel when level
        float x = WheelUpright.transform.localPosition.x;
        h = Mathf.Abs(x);
        hsign = Mathf.Sign(x);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isConfigured) return;

        // Get wheel position from collider
        Vector3 refPos = ReferenceWheel.transform.localPosition;
        Vector3 refAngles = ReferenceWheel.transform.localRotation.eulerAngles;
        refAngles.y = RacetrackUtil.LocalAngle(refAngles.y);
        if (refAngles.y < -90.0f) refAngles.y += 180.0f;
        if (refAngles.y > 90.0f) refAngles.y -= 180.0f;

        // Calculate extension and corresponding Y position
        float y = refPos.y;

        // Clamp if outside possible range
        y = Mathf.Clamp(y, -h, h);
        y = Mathf.Lerp(prevY, y, LerpFactor);
        prevY = y;

        // Calculate suspension angle
        float ang = Mathf.Asin(y / h);
        float angDeg = ang * Mathf.Rad2Deg;

        // Clamp angles
        angDeg = Mathf.Clamp(angDeg, -MaxDownAngle, MaxUpAngle);
        ang = angDeg * Mathf.Deg2Rad;
        y = Mathf.Sin(ang) * h;

        // Animate wishbones
        Vector3 upperAngles = UpperWishbone.transform.localRotation.eulerAngles;
        UpperWishbone.transform.localRotation = Quaternion.Euler(upperAngles.x, upperAngles.y, angDeg * hsign);
        Vector3 lowerAngles = LowerWishbone.transform.localRotation.eulerAngles;
        LowerWishbone.transform.localRotation = Quaternion.Euler(lowerAngles.x, lowerAngles.y, angDeg * hsign);

        // Position wheel upright
        float x = Mathf.Cos(ang) * h * hsign;
        WheelUpright.transform.localPosition = new Vector3(x, y, 0.0f);

        // Steering and wheel rotation
        Vector3 uprightAngles = WheelUpright.transform.localRotation.eulerAngles;
        WheelUpright.transform.localRotation = Quaternion.Euler(uprightAngles.x, refAngles.y, uprightAngles.z);
        Vector3 wheelAngles = Wheel.transform.localRotation.eulerAngles;
        Wheel.transform.localRotation = Quaternion.Euler(refAngles.x, wheelAngles.y, wheelAngles.z);
    }
}
