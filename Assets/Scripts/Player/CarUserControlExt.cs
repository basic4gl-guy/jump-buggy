using UnityStandardAssets.CrossPlatformInput;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;
using System;

[RequireComponent(typeof(CarController))]
public class CarUserControlExt : MonoBehaviour
{
    [Header("Animation")]
    public Transform SteeringWheel;

    [Header("Input")]
    public float HInputFactor = 0.75f;
    public float VInputFactor = 1.0f;

    [Header("Steering assist")]
    public bool EnableSteeringAssist = true;

    [Tooltip("Minimum car velocity before steering assist will activate. Units/s")]
    public float AssistMinVelocity = 1.0f;
    public float AssistMaxAngle = 20.0f;
    public float AssistMinAngle = 1.0f;

    [Tooltip("Steering assist turn rate. Degrees/s")]
    public float AssistTurnRate = 50.0f;

    private CarController m_Car; // the car controller we want to use
    private RacetrackCarState carState;

    private void Awake()
    {
        // get the car controller
        m_Car = GetComponent<CarController>();
        carState = GetComponent<RacetrackCarState>();
    }

    private void FixedUpdate()
    {
        // Get input
        float h;
        float v;
        GetInput(out h, out v);

        // Steering assist
        if (EnableSteeringAssist && carState != null)
        {
            h += GetSteeringAssistCorrection(carState.State);
            h = Mathf.Clamp(h, -1.0f, 1.0f);
        }

        // Set steering wheel rotation
        if (!VRUtil.Is6DOFVR() && SteeringWheel != null)
        {
            Vector3 r = SteeringWheel.localRotation.eulerAngles;
            SteeringWheel.localRotation = Quaternion.Euler(r.x, r.y, h * -90.0f);
        }

        // Pass to car
        m_Car.Move(h, v, v, 0f);
    }

    private float GetSteeringAssistCorrection(CarState state)
    {
        if (!state.IsAboveRoad || state.Velocity.z < AssistMinVelocity)
            return 0.0f;

        // Get angle relative to road
        float ang = RacetrackUtil.LocalAngle(state.Angle);
        if (Mathf.Abs(ang) > AssistMaxAngle)
            return 0.0f;

        float correction = -Mathf.Clamp(ang / AssistMinAngle, -1.0f, 1.0f);
        return correction * AssistTurnRate / 90.0f / state.Velocity.z;
        
        //if (Mathf.Abs(ang) < 30.0f)
        //{
        //    float correction;
        //    //Debug.Log(string.Format("Curve = {3} X = {0:0.0000}, XD = {1:0.0000}, Ang = {2:0.00}", state.Position.x, state.Velocity.x, state.Angle, state.Curve.Index));
        //    correction = -Mathf.Clamp(ang / 5.0f, -1.0f, 1.0f);
        //    correction *= 1.0f / state.Velocity.z;
        //    h += correction;
        //}
    }

    private void GetInput(out float h, out float v)
    {
        if (UnityEngine.XR.XRSettings.isDeviceActive && OVRPlugin.productName == "Oculus Go")
        {
            // Use controller orientation on Oculus Go
            GetVRControllerOrientationInput(out h, out v);
        }
        else
        {
            // For other platforms, use CrossPlatformInputManager
            h = CrossPlatformInputManager.GetAxis("Horizontal") * HInputFactor;
            v = CrossPlatformInputManager.GetAxis("Vertical") * VInputFactor;

            if (VRUtil.Is6DOFVR() && SteeringWheel != null)
            {
                var angles = SteeringWheel.localRotation.eulerAngles;
                h = -VRUtil.LocalAngle(angles.z) / 90.0f;
            }
        }
    }

    private void GetVRControllerOrientationInput(out float h, out float v)
    {
        // Controller orientation used for steering
        // Get Z axis
        float steer = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTrackedRemote).eulerAngles.z;
        if (steer > 180.0f) steer -= 360.0f;            // Convert to -180 to 180 range
        h = Mathf.Clamp(-steer / 90.0f, -1.0f, 1.0f);

        // Trigger accelerates. Click touch pad to brake.
        bool accel = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
        bool brake = OVRInput.Get(OVRInput.Button.PrimaryTouchpad);
        v = (accel ? 1.0f : 0.0f) - (brake ? 1.0f : 0.0f);
    }
}
