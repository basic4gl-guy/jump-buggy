using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

[RequireComponent(typeof(CarController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(RacetrackProgressTracker))]
public class AICarController : MonoBehaviour
{
    public Racetrack Track;
    public Transform SteeringWheel;
    public RacetrackAIData RacetrackAIData;

    private CarController carController; // the car controller we want to use
    private Rigidbody rigidBody;
    private RacetrackProgressTracker tracker;

    [Header("Parameters")]
    public float RecenterTime = 1.0f;
    public float RecenterAngleRange = 20.0f;
    public float SteeringSpeedFactor = 300.0f;
    public float SteeringRate = 10.0f;
    public float SteeringLimit = 90.0f;
    public float SteeringSmooth = 0.1f;

    public float PreferredSpeed = 50.0f;
    public float MinMaxSpeedBuffer = 1.0f;

    [Header("Debugging")]
    public int DebugSegmentIndex;
    public float DebugVelocity;
    public float DebugMaxVelocity;
    public float DebugMinVelocity;
    public float DebugX;
    public float DebugAngle;

    private float prevInputX = 0.0f;

    private void Awake()
    {
        // get the car controller
        carController = GetComponent<CarController>();
        rigidBody = GetComponent<Rigidbody>();
        tracker = GetComponent<RacetrackProgressTracker>();
    }

    private void FixedUpdate()
    {
        if (carController == null || rigidBody == null) return;

        // Find racetrack
        var track = Track ?? Racetrack.Instance;
        if (track == null)
        {
            Debug.LogError("Racetrack not found");
            return;
        }

        // Find progress tracker
        if (tracker == null)
        {
            Debug.LogError("Racetrack progress tracker not found");
            return;
        }

        // Must be on racetrack
        if (!tracker.isAboveRoad)
            return;

        // Get car-relative-to-surface info
        var state = RacetrackUtil.GetCarState(rigidBody, track, tracker.currentCurve);

        // Debugging
        DebugSegmentIndex = state.SegmentIndex;
        DebugVelocity = state.Velocity.z;
        DebugMaxVelocity = 1000.0f;
        DebugMinVelocity = 0.0f;
        DebugX = state.Position.x;
        DebugAngle = state.Angle;

        // Calculate car input
        float inputX = 0.0f;
        float inputY = 0.0f;

        // Steering
        if (Mathf.Abs(state.Velocity.z) > 0.01f)
        {
            // Decide on target X
            float targetX = 0.0f;

            // Calculate angle required to get to targetX in RecenterTime
            Vector2 targetDir = new Vector2((targetX - state.Position.x) / RecenterTime, state.Velocity.z);
            float targetAng = Mathf.Atan2(targetDir.x, targetDir.y) * Mathf.Rad2Deg;
            targetAng = Mathf.Clamp(targetAng, -RecenterAngleRange, RecenterAngleRange);

            // Calculate direction to turn
            float angDelta = RacetrackUtil.LocalAngle(targetAng - state.Angle);

            // Calculate steering wheel input
            float steeringLimit = Mathf.Min(SteeringSpeedFactor / state.Velocity.z, 90.0f);
            inputX = Mathf.Clamp(angDelta / state.Velocity.z * SteeringRate, -steeringLimit, steeringLimit);
        }

        inputX = Mathf.Clamp(inputX, -SteeringLimit, SteeringLimit);
        inputX = Mathf.Lerp(inputX, prevInputX, SteeringSmooth);

        // Acceleration/braking
        inputY = 1.0f;
        if (RacetrackAIData != null)
        {
            var segData = RacetrackAIData.GetAIData(state.SegmentIndex);
            float targetVel;

            if (segData.MaxSpeed - segData.MinSpeed < MinMaxSpeedBuffer * 2.0f)
            {
                // No room for buffer, just aim for middle of range
                targetVel = (segData.MaxSpeed + segData.MinSpeed) / 2.0f;                
            }
            else
            {
                // Otherwise clamp preferred speed
                targetVel = Mathf.Clamp(PreferredSpeed, segData.MinSpeed + MinMaxSpeedBuffer, segData.MaxSpeed - MinMaxSpeedBuffer);
            }

            inputY = Mathf.Sign(targetVel - state.Velocity.z);      // TODO: Smoother input?

            // Debugging
            DebugMaxVelocity = segData.MaxSpeed;
            DebugMinVelocity = segData.MinSpeed;
        }

        // Feed input into car
        carController.Move(inputX / 90.0f, inputY, inputY, 0.0f);
        prevInputX = inputX;

        // Set steering wheel position
        if (SteeringWheel != null)
        {
            Vector3 r = SteeringWheel.localRotation.eulerAngles;
            SteeringWheel.localRotation = Quaternion.Euler(r.x, r.y, -inputX);
        }
    }
}