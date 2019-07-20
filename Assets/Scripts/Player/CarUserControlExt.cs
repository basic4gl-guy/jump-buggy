using UnityStandardAssets.CrossPlatformInput;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;
using System;

[RequireComponent(typeof(CarController))]
public class CarUserControlExt : MonoBehaviour
{
    public CarMode Mode = CarMode.Driving;

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
        float park = 0.0f;
        GetInput(out h, out v);

        if (Mode == CarMode.Parked)
        {
            park = 1.0f;
            v = 0.0f;
        }

        // Steering assist
        if (EnableSteeringAssist && carState != null)
        {
            h += GetSteeringAssistCorrection(carState.State);
            h = Mathf.Clamp(h, -1.0f, 1.0f);
        }

        // Set steering wheel rotation
        if (SteeringWheel != null && !UseWheelRotation)
        {
            Vector3 r = SteeringWheel.localRotation.eulerAngles;
            SteeringWheel.localRotation = Quaternion.Euler(r.x, r.y, h * -90.0f);
        }

        // Pass to car
        m_Car.Move(h, v, v, park);              // Note: Applying handbrake slows the car down even after the handbrake input is stopped (!?)
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
        if (VRUtil.IsOculusGoVR())
        {
            // Use controller orientation on Oculus Go
            GetOculusGoInput(out h, out v);
        }        
        else if (VRUtil.Is6DOFVR())
        {
            // If the steering wheel exists and has been grabbed at least once, default to steering wheel steering
            if (UseWheelRotation)
            {
                var angles = SteeringWheel.localRotation.eulerAngles;
                h = -VRUtil.LocalAngle(angles.z) / 90.0f;
            }
            else
                // Otherwise get right controller tilt value
                h = GetTiltSteering(OVRInput.Controller.RTouch);

            var brake = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
            var accel = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            v = accel - brake;
        }
        else 
        {
            // For other platforms, use CrossPlatformInputManager
            h = CrossPlatformInputManager.GetAxis("Horizontal") * HInputFactor;
            v = CrossPlatformInputManager.GetAxis("Vertical") * VInputFactor;
        }
    }

    private void GetOculusGoInput(out float h, out float v)
    {
        h = GetTiltSteering(OVRInput.Controller.RTrackedRemote);

        // Trigger accelerates. Click touch pad to brake.
        bool accel = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
        bool brake = OVRInput.Get(OVRInput.Button.PrimaryTouchpad);
        v = (accel ? 1.0f : 0.0f) - (brake ? 1.0f : 0.0f);
    }

    private static float GetTiltSteering(OVRInput.Controller controller)
    {
        float h;
        // Controller orientation used for steering
        // Get Z axis
        float steer = OVRInput.GetLocalControllerRotation(controller).eulerAngles.z;
        if (steer > 180.0f) steer -= 360.0f;            // Convert to -180 to 180 range
        h = Mathf.Clamp(-steer / 90.0f, -1.0f, 1.0f);
        return h;
    }

    private bool UseWheelRotation
    {
        get
        {
            if (SteeringWheel == null) return false;

            // Use wheel rotation if it has been grabbed at least once
            var grab = SteeringWheel.GetComponent<VRGrabbable>();
            return grab.WasGrabbed;
        }
    }
}
