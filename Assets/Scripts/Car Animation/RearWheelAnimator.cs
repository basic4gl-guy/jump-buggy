using UnityEngine;

public class RearWheelAnimator : MonoBehaviour
{
    [Header("Input")]
    public Transform ReferenceLeftWheel;
    public Transform ReferenceRightWheel;

    [Header("Animated pieces")]
    public Transform LeftWheel;
    public Transform RightWheel;

    [Header("Parameters")]
    public float MaxY = 1000.0f;
    public float MinY = -1000.0f;
    public float LerpFactor = 0.1f;

    private bool isConfigured;
    private float prevly = 0.0f;
    private float prevry = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        isConfigured = 
            ReferenceLeftWheel != null &&
            ReferenceRightWheel != null &&
            LeftWheel != null &&
            RightWheel != null;
        if (!isConfigured)
            Debug.LogError("RearWheelAnimator is not fully configured");
    }

    // Update is called once per frame
    void Update()
    {
        if (!isConfigured) return;

        // Get spring extension
        float ly = ReferenceLeftWheel.localPosition.y;
        float ry = ReferenceRightWheel.localPosition.y;

        // Smooth animation
        ly = Mathf.Lerp(prevly, ly, LerpFactor);
        ry = Mathf.Lerp(prevry, ry, LerpFactor);
        prevly = ly;
        prevry = ry;

        // Clamp
        ly = Mathf.Clamp(ly, MinY, MaxY);
        ry = Mathf.Clamp(ry, MinY, MaxY);

        // Position wheels
        Vector3 lPos = LeftWheel.localPosition;
        LeftWheel.localPosition = new Vector3(lPos.x, ly, lPos.z);
        Vector3 rPos = RightWheel.localPosition;
        RightWheel.localPosition = new Vector3(rPos.x, ry, rPos.z);

        // Rotate wheels
        LeftWheel.rotation = ReferenceLeftWheel.rotation;
        RightWheel.rotation = ReferenceRightWheel.rotation;
    }
}
