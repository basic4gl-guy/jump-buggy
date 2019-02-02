using UnityStandardAssets.CrossPlatformInput;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

[RequireComponent(typeof(CarController))]
public class CarUserControlExt : MonoBehaviour
{
    public Transform SteeringWheel;

    private CarController m_Car; // the car controller we want to use

    private void Awake()
    {
        // get the car controller
        m_Car = GetComponent<CarController>();
    }

    private void FixedUpdate()
    {
        // Get input
        float h;
        float v;
        GetInput(out h, out v);

        // Set steering wheel position
        if (SteeringWheel != null)
        {
            Vector3 r = SteeringWheel.localRotation.eulerAngles;
            SteeringWheel.localRotation = Quaternion.Euler(r.x, r.y, h * -90.0f);
        }

        // Pass to car
        m_Car.Move(h, v, v, 0f);
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
            h = CrossPlatformInputManager.GetAxis("Horizontal");
            v = CrossPlatformInputManager.GetAxis("Vertical");
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
