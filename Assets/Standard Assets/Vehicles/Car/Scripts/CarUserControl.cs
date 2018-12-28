using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof (CarController))]
    public class CarUserControl : MonoBehaviour
    {
        public float InputHFactor = 0.1f;
        public float InputVFactor = 0.1f;
        public Transform SteeringWheel;

        private CarController m_Car; // the car controller we want to use

        private float m_h = 0.0f;
        private float m_v = 0.0f;

        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
        }


        private void FixedUpdate()
        {
            float h;
            float v;

            // Get input
            if (Application.isEditor)
            {
                m_h = Mathf.Clamp(m_h + Input.GetAxis("Mouse X") * InputHFactor, -1.0f, 1.0f);
                m_v = Mathf.Clamp(m_v + Input.GetAxis("Mouse Y") * InputVFactor, -1.0f, 1.0f);
                h = m_h;
                v = m_v;
            }
            else
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

            if (SteeringWheel != null)
            {
                Vector3 r = SteeringWheel.localRotation.eulerAngles;
                SteeringWheel.localRotation = Quaternion.Euler(r.x, r.y, h * -90.0f);
            }

//            // pass the input to the car!
//            float h = CrossPlatformInputManager.GetAxis("Horizontal");
//            float v = CrossPlatformInputManager.GetAxis("Vertical");
//#if !MOBILE_INPUT
//            float handbrake = CrossPlatformInputManager.GetAxis("Jump");
//            m_Car.Move(h, v, v, handbrake);
//#else
            m_Car.Move(h, v, v, 0f);
//#endif
        }
    }
}
