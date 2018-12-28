using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCarController : MonoBehaviour {

    [Header("Meshes")]
    public Transform SteeringWheel;

    [Header("Editor Input")]
    public float EditorSteeringFactor = 1.0f;
    public float EditorAccelBrakeFactor = 0.01f;

    [Header("Wheels")]
    public WheelInfo[] Wheels = new WheelInfo[4];           // Frontx2 then rearx2
    public SpringInfo[] Springs = new[]
    {
        new SpringInfo { Length = 0.75f, K = 10000.0f, Damping = 1000.0f },       // Front springs
        new SpringInfo { Length = 1.0f, K = 10000.0f, Damping = 1000.0f }         // Rear springs
    };

    [Header("Physics")]
    public float SteeringToFrontWheelAngleFactor = 0.2f;
    public float TyreGripFriction = 0.7f;
    public float TyreSlideFriction = 0.3f;

    [Header("Handling")]
    public float BrakeForce = 1000.0f;
    public float AccelForce = 500.0f;

    // Components
    private Rigidbody rigidBody;

    // Current input state
    private float inputSteer = 0.0f;
    private float inputAccelBrake = 0.0f;           // Positive to accelerate. Negative to brake.

	// Use this for initialization
	void Start () {
        rigidBody = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
        GetInput();
	}

    private void FixedUpdate()
    {
        // Trace down from wheels to find collision with ground
        Vector3 springForceDirection = gameObject.transform.up;
        Vector3 rayDirection = -springForceDirection;

        var forces = new List<ForcePt>();

        // First pass - Spring forces. No friction.
        for (int i = 0; i < 4; i++)
        {
            var wheel = Wheels[i];
            if (!wheel.RayStartPos.gameObject.activeInHierarchy) continue;

            var spring = Springs[i / 2];
            var ray = new Ray(wheel.RayStartPos.position, rayDirection);
            var hits = Physics.RaycastAll(ray, spring.Length).Where(h => h.collider.tag != "Player");
            if (!hits.Any()) continue;

            // Find what was hit
            var hit = hits.OrderBy(h => h.distance).First();

            // Distance to hit gives spring extension
            // Spring midpoint is full extension / 2
            float springDisplacement = spring.Length - hit.distance;

            // Magnitude of spring force.
            float Fs = springDisplacement * spring.K;

            // Apply damping
            Vector3 vel = gameObject.transform.TransformVector(rigidBody.velocity);
            Fs -= vel.y * spring.Damping;

            // Find component of force in direction of surface normal.
            Fs *= Vector3.Dot(hit.normal, springForceDirection);

            // Spring can only push. Cannot pull.
            if (Fs > 0.0f)
            {
                // Add force to car in direction of surface normal
                forces.Add(new ForcePt
                {
                    Force = Fs * hit.normal,
                    Pt = hit.point
                });
            }
        }

        // Apply forces
        foreach (var force in forces)
            rigidBody.AddForceAtPosition(force.Force, force.Pt);

        // Second pass - Friction
        forces.Clear();
        for (int i = 0; i < 4; i++)
        {
            var wheel = Wheels[i];
            if (!wheel.RayStartPos.gameObject.activeInHierarchy) continue;

            var spring = Springs[i / 2];
            var ray = new Ray(wheel.RayStartPos.position, rayDirection);
            var hits = Physics.RaycastAll(ray, spring.Length).Where(h => h.collider.tag != "Player");
            if (!hits.Any()) continue;

            // Find what was hit
            var hit = hits.OrderBy(h => h.distance).First();

            // Distance to hit gives spring extension
            // Spring midpoint is full extension / 2
            float springDisplacement = spring.Length - hit.distance;

            // Magnitude of spring force.
            float Fs = springDisplacement * spring.K;

            Vector3 vel = gameObject.transform.TransformVector(rigidBody.velocity);
            Fs -= vel.y * spring.Damping;

            // Find component of force in direction of surface normal.
            Fs *= Vector3.Dot(hit.normal, springForceDirection);

            // Spring can only push. Cannot pull.
            if (Fs > 0.0f)
            {
                // Get wheel orientation
                Matrix4x4 wheelOrientation = transform.localToWorldMatrix;
                if (i < 2)
                {
                    // Turn front wheels based on steering
                    float steeringAngle = inputSteer * SteeringToFrontWheelAngleFactor;
                    wheelOrientation *= Matrix4x4.Rotate(Quaternion.Euler(0.0f, steeringAngle, 0.0f));
                }

                // Construct a basis matrix describing the wheel and surface collided with
                // Y vector is surface normal
                // Z vector lies on wheel plane and surface. I.e. the direction along the surface the wheel rolls.
                Vector3 basisY = hit.normal;
                Vector3 basisZ = Vector3.Cross(wheelOrientation.GetColumn(0), basisY).normalized;
                Vector3 basisX = Vector3.Cross(basisY, basisZ);
                Matrix4x4 C2W = new Matrix4x4();
                C2W.SetColumn(0, basisX);
                C2W.SetColumn(1, basisY);
                C2W.SetColumn(2, basisZ);
                C2W.SetColumn(3, new Vector4(hit.point.x, hit.point.y, hit.point.z, 1.0f));
                Matrix4x4 W2C = C2W.inverse;

                // Find velocity of car at collision point in collision space
                Vector3 velW = rigidBody.GetPointVelocity(hit.point);
                Vector3 velC = W2C.MultiplyVector(velW);

                // Apply friction to horizontal movement
                float absX = Mathf.Abs(velC.x);
                float sgnX = Mathf.Sign(velC.x);
                float frictionCoeff = absX > 0.1f    ? TyreSlideFriction
                                    : absX > 0.0001f ? TyreGripFriction
                                    : 0.0f;
                float frictionMagnitude = Fs * frictionCoeff;
                Vector3 FC = frictionMagnitude * -Mathf.Sign(velC.x) * basisX;

                // Apply acceleration/braking
                if (inputAccelBrake != 0.0f)
                {
                    float accel = Mathf.Sign(velC.z) == Mathf.Sign(inputAccelBrake) ? AccelForce : BrakeForce;
                    FC += Vector3.forward * AccelForce * inputAccelBrake;
                }

                // Apply force to collision point
                Vector3 FW = C2W.MultiplyVector(FC);
                rigidBody.AddForceAtPosition(FW, hit.point);
            }
        }

        // Apply forces
        //foreach (var force in forces)
        //    rigidBody.AddForceAtPosition(force.Force, force.Pt);

    }

    private void GetInput()
    {
        // Get input
        if (Application.isEditor)
        {
            //inputSteer = Mathf.Clamp(inputSteer + Input.GetAxis("Mouse X") * EditorSteeringFactor, -90.0f, 90.0f);
            //inputAccelBrake = Mathf.Clamp(inputAccelBrake + Input.GetAxis("Mouse Y") * EditorAccelBrakeFactor, -1.0f, 1.0f);
        }
        else
        {
            // Controller orientation used for steering
            // Get Z axis
            float steer = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTrackedRemote).eulerAngles.z;
            if (steer > 180.0f) steer -= 360.0f;            // Convert to -180 to 180 range
            inputSteer = Mathf.Clamp(-steer, -90.0f, 90.0f);

            // Trigger accelerates. Click touch pad to brake.
            bool accel = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
            bool brake = OVRInput.Get(OVRInput.Button.PrimaryTouchpad);
            inputAccelBrake = (accel ? 1.0f : 0.0f) - (brake ? 1.0f : 0.0f);
        }        

        Debug.LogFormat("Steer {0} accel/brake {1}", inputSteer, inputAccelBrake);

        if (SteeringWheel)
        {
            // Set Z axis of local rotation
            var angles = SteeringWheel.localEulerAngles;
            SteeringWheel.localEulerAngles = new Vector3(angles.x, angles.y, -inputSteer);
        }
    }

    [Serializable]
    public class WheelInfo
    {
        public Transform RayStartPos;
        public Transform WheelMesh;
    }

    [Serializable]
    public class SpringInfo
    {
        public float Length;
        public float K;                 // Spring constant
        public float Damping;           // Damping coefficient
    }

    private class ForcePt
    {
        public Vector3 Force;
        public Vector3 Pt;
    }
}
