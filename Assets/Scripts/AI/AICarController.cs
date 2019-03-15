using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

[RequireComponent(typeof(CarController))]
[RequireComponent(typeof(RacetrackCarState))]
public class AICarController : MonoBehaviour
{
    public Transform SteeringWheel;
    public RacetrackAIData RacetrackAIData;

    private CarController carController; // the car controller we want to use
    private RacetrackCarState carState;

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
    public float DebugDistToJump;

    private float prevInputX = 0.0f;

    private void Awake()
    {
        // get the car controller
        carController = GetComponent<CarController>();
        carState = GetComponent<RacetrackCarState>();
    }

    private void FixedUpdate()
    {
        if (carController == null || carState == null) return;

        // Must be on racetrack
        if (!carState.State.IsAboveRoad)
            return;

        // Get car-relative-to-surface info
        var state = carState.State;
        var track = state.Track;
        var nextCarState = carState.GetNextCarState();

        // Get segment AI data
        RacetrackAIData.SegmentAIData segData = RacetrackAIData != null ? RacetrackAIData.GetAIData(state.SegmentIndex) : null;

        // Debugging
        DebugSegmentIndex = state.SegmentIndex;
        DebugVelocity = state.Velocity.z;
        DebugX = state.Position.x;
        DebugAngle = state.Angle;
        if (segData != null)
        {
            DebugMaxVelocity = segData.MaxSpeed;
            DebugMinVelocity = segData.MinSpeed;
            DebugDistToJump = segData.DistToJump;
        }
        else
        {
            DebugMaxVelocity = 1000.0f;
            DebugMinVelocity = 0.0f;
            DebugDistToJump = 1000000.0f;
        }

        // Calculate car input
        float inputX = 0.0f;
        float inputY = 0.0f;

        // Steering
        float preventCollisionSpeed = 1000.0f;
        if (Mathf.Abs(state.Velocity.z) > 0.01f)
        {
            float targetX = Mathf.Clamp(state.Position.x, -2.0f, 2.0f);     // TODO: Comfortable X range variable

            if (segData != null && segData.DistToJump < 50.0f)
                targetX = state.Position.x;             // Attempt to line up car with jump

            // Attempt to drive around the car infront if necessary
            else if (nextCarState != null)
            {
                var nextState = nextCarState.State;
                Vector3 relPos = state.GetRelativePosition(nextState);
                Vector3 relVel = nextState.Velocity - state.Velocity;

                // Determine whether to avoid the other car
                bool avoid = false;
                float dist = relPos.z;
                if (dist > -3.5f)                               // TODO: Car length variable
                {
                    if (dist < 7.0f)                            // Car next to us
                        avoid = true;
                    else if (dist + relVel.z * 3.0f < 7.0f)     // Will catch up to car in 3 seconds
                        avoid = true;
                }

                if (avoid)
                {
                    // Determine room down left and right hand side
                    float roadLeft = -6.0f;
                    float roadRight = 6.0f;
                    float carLeft = Mathf.Min(roadRight, nextState.Position.x - 0.5f);            // TODO: Car width variable
                    float carRight = Mathf.Max(roadLeft, nextState.Position.x + 0.5f);           // TODO: Road width variable
                    float roomLeft = carLeft - roadLeft;
                    float roomRight = roadRight - carRight;
                    float targetLeft = (carLeft + roadLeft) / 2.0f;
                    float targetRight = (carRight + roadRight) / 2.0f;

                    // Choose which side to go down
                    bool goLeft;
                    if (roomRight < 1.5f)                                   // TODO: Room limit variable
                        goLeft = true;
                    else if (roomLeft < 1.5f)
                        goLeft = false;
                    else
                        goLeft = relPos.x > 0;

                    targetX = goLeft ? targetLeft : targetRight;

                    // Slow down if necessary to prevent a crash
                    if (dist + relVel.z * 2.0f < 7.0f && Mathf.Abs(relPos.x) < 1.5f)
                        preventCollisionSpeed = Mathf.Max(nextState.Velocity.z, 0.0f);
                }                
            }

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
        float targetVel = Mathf.Min(PreferredSpeed, preventCollisionSpeed);             // Start with preferred speed, reduced if necessary to prevent a collision
        if (segData != null)                                                            // Apply AI data min/max speeds
        {
            if (segData.MaxSpeed - segData.MinSpeed < MinMaxSpeedBuffer * 2.0f)
            {
                // No room for buffer, just aim for middle of range
                targetVel = (segData.MaxSpeed + segData.MinSpeed) / 2.0f;                
            }
            else
            {                
                // Clamp to speed range
                targetVel = Mathf.Clamp(targetVel, segData.MinSpeed + MinMaxSpeedBuffer, segData.MaxSpeed - MinMaxSpeedBuffer);
            }
        }

        // Accelerate or brake to seek target velocity
        inputY = Mathf.Sign(targetVel - state.Velocity.z);      // TODO: Smoother input?

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