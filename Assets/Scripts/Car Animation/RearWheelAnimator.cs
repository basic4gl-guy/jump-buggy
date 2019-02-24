using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RearWheelAnimator : MonoBehaviour
{
    [Header("Input")]
    public Transform ReferenceLeftWheel;
    public Transform ReferenceRightWheel;

    [Header("Animated pieces")]
    public Transform Differential;
    public Transform DriveShaft;
    public Transform LeftWheel;
    public Transform RightWheel;

    [Header("Parameters")]
    public float MaxY = 1000.0f;
    public float MinY = -1000.0f;
    public float LerpFactor = 0.1f;

    private bool isConfigured;
    private float prevLExt = 0.0f;
    private float prevRExt = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        isConfigured = 
            ReferenceLeftWheel != null &&
            ReferenceRightWheel != null &&
            Differential != null &&
            LeftWheel != null &&
            RightWheel != null &&
            DriveShaft != null;
        if (!isConfigured)
            Debug.LogError("RearWheelAnimator is not fully configured");
    }

    // Update is called once per frame
    void Update()
    {
        if (!isConfigured) return;

        // Get spring extension
        float lExt = -ReferenceLeftWheel.localPosition.y;
        float rExt = -ReferenceRightWheel.localPosition.y;

        // Smooth animation
        lExt = Mathf.Lerp(prevLExt, lExt, LerpFactor);
        rExt = Mathf.Lerp(prevRExt, rExt, LerpFactor);
        prevLExt = lExt;
        prevRExt = rExt;

        // Clamp
        lExt = Mathf.Clamp(lExt, MinY, MaxY);
        rExt = Mathf.Clamp(rExt, MinY, MaxY);

        // Position wheels
        Vector3 lPos = LeftWheel.localPosition;
        LeftWheel.localPosition = new Vector3(lPos.x, -lExt, lPos.z);
        Vector3 rPos = RightWheel.localPosition;
        RightWheel.localPosition = new Vector3(rPos.x, -rExt, rPos.z);

        // Rotate wheels
        LeftWheel.rotation = ReferenceLeftWheel.rotation;
        RightWheel.rotation = ReferenceRightWheel.rotation;

        // Position differential between wheels
        Differential.position = (LeftWheel.position + RightWheel.position) / 2.0f;

        // Link drive shaft to differential
        Vector3 delta = Differential.localPosition - DriveShaft.localPosition;
        float ang = Mathf.Atan(delta.y / delta.z);
        float angDeg = ang * Mathf.Rad2Deg;

        Vector3 driveAngles = DriveShaft.localRotation.eulerAngles;
        DriveShaft.localRotation = Quaternion.Euler(angDeg, driveAngles.y, driveAngles.z);
    }
}
