#define DEBUG_CARAI

using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

[RequireComponent(typeof(CarController))]
[RequireComponent(typeof(RacetrackCarState))]
public class AICarController : MonoBehaviour
{
    public Transform SteeringWheel;
    public RacetrackAIData RacetrackAIData;
    public CarMode Mode;

    private CarController carController; // the car controller we want to use
    private RacetrackCarState carState;

    private AICarIndividualParams aiParams;

#if DEBUG_CARAI
    [Header("Debugging")]
    public int DebugSegmentIndex;
    public float DebugVelocity;
    public float DebugMaxVelocity;
    public float DebugMinVelocity;
    public float DebugX;
    public float DebugAngle;
    public float DebugDistToJump;

    public float InputX;
    public float InputY;
#endif

    private float prevInputX = 0.0f;

    // Sometimes the car gets stuck.. for no obvious reason. Reversing slightly seems to fix this.
    private float stuckTimer = 0.0f;
    private bool isReversingBecauseStuck = false;

    private void Awake()
    {
        // get the car controller
        carController = GetComponent<CarController>();
        carState = GetComponent<RacetrackCarState>();

        aiParams = GetComponentInChildren<AICarIndividualParams>();
        if (aiParams == null)
            Debug.LogError("AICarController - Could not find AICarIndividualParams in children");
    }

    private void FixedUpdate()
    {
        if (carController == null || carState == null || aiParams == null) return;

        // Must be on racetrack
        if (!carState.State.IsAboveRoad)
            return;

        // Get car-relative-to-surface info
        var state = carState.State;
        var track = state.Track;
        var nextCarState = carState.GetNextCarState();

        // Get segment AI data
        RacetrackAIData.SegmentAIData segData = RacetrackAIData != null ? RacetrackAIData.GetAIData(state.SegmentIndex) : null;

#if DEBUG_CARAI
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
#endif

        // Calculate car input
        float inputX = 0.0f;
        float inputY = 0.0f;

        // Steering
        float preventCollisionSpeed = 1000.0f;
        if (Mathf.Abs(state.Velocity.z) > 0.01f && Mode != CarMode.Parked)
        {
            float targetX = Mathf.Clamp(state.Position.x, -aiParams.CenterXOffsetLimit, aiParams.CenterXOffsetLimit);     // TODO: Comfortable X range variable

            // Attempt to drive around the car infront if necessary
            if (nextCarState != null)
            {
                var nextState = nextCarState.State;
                Vector3 relPos = state.GetRelativePosition(nextState);
                Vector3 relVel = nextState.Velocity - state.Velocity;

                // Determine whether to avoid the other car
                bool avoid = false;
                float dist = relPos.z;
                if (dist > -aiParams.CarLength / 2.0f)
                {
                    if (dist < aiParams.CarLength)                           // Car next to us
                        avoid = true;
                    else if (dist + relVel.z * aiParams.CatchupDurationAvoidLimit < aiParams.CarLength)           // Will catch up to car in 3 seconds
                        avoid = true;
                }

                if (avoid)
                {
                    const float PassingRoom = 0.5f;                         // TODO: Make parameter

                    // Determine left and right hand side limits
                    float roadLeft = -aiParams.RoadWidth / 2.0f;
                    float roadRight = aiParams.RoadWidth / 2.0f;
                    float carLeft = Mathf.Min(roadRight, nextState.Position.x - aiParams.CarWidth / 2.0f);
                    float carRight = Mathf.Max(roadLeft, nextState.Position.x + aiParams.CarWidth / 2.0f);

                    // Subtract space to account for width of current car and required passing room
                    roadLeft += aiParams.CarWidth / 2.0f + PassingRoom;
                    roadRight -= aiParams.CarWidth / 2.0f + PassingRoom;
                    carRight += aiParams.CarWidth / 2.0f + PassingRoom;
                    carLeft -= aiParams.CarWidth / 2.0f + PassingRoom;

                    // Determine actual room
                    float roomLeft = carLeft - roadLeft;
                    float roomRight = roadRight - carRight;

                    // Choose which side to go down
                    bool goLeft;
                    if (roomRight < 0)
                        goLeft = true;
                    else if (roomLeft < 0)
                        goLeft = false;
                    else
                        goLeft = relPos.x > 0;

                    // Find limits based on side chosen. Clamp target X
                    float left = goLeft ? roadLeft : carRight;
                    float right = goLeft ? carLeft : roadRight;
                    targetX = Mathf.Clamp(targetX, left, right);

                    //float targetLeft = (carLeft + roadLeft) / 2.0f;
                    //float targetRight = (carRight + roadRight) / 2.0f;

                    //// Choose which side to go down
                    //bool goLeft;
                    //if (roomRight < aiParams.AvoidGapLimit)
                    //    goLeft = true;
                    //else if (roomLeft < aiParams.AvoidGapLimit)
                    //    goLeft = false;
                    //else
                    //    goLeft = relPos.x > 0;

                    //targetX = goLeft ? targetLeft : targetRight;

                    // Slow down if necessary to prevent a crash
                    if (dist + relVel.z * aiParams.CatchupDurationBrakeLimit < aiParams.CarLength && Mathf.Abs(relPos.x) < aiParams.BrakeXOffsetLimit)
                        preventCollisionSpeed = Mathf.Max(nextState.Velocity.z, 0.0f);
                }
            }

            // Calculate angle required to get to targetX in RecenterTime
            Vector2 targetDir = new Vector2((targetX - state.Position.x) / aiParams.RecenterTime, state.Velocity.z);
            float targetAng = Mathf.Atan2(targetDir.x, targetDir.y) * Mathf.Rad2Deg;
            targetAng = Mathf.Clamp(targetAng, -aiParams.RecenterAngleRange, aiParams.RecenterAngleRange);

            // Override angle if nearing a jump
            if (segData != null && segData.DistToJump < aiParams.StraightenForJumpDistance)
                targetAng = 0;
            targetAng += aiParams.SteeringAngleOffset;

            // Calculate direction to turn
            float angDelta = RacetrackUtil.LocalAngle(targetAng - state.Angle);

            // Calculate steering wheel input
            float steeringLimit = Mathf.Min(aiParams.SteeringSpeedFactor / state.Velocity.z, 90.0f);
            inputX = Mathf.Clamp(angDelta / state.Velocity.z * aiParams.SteeringRate, -steeringLimit, steeringLimit);
        }

        inputX = Mathf.Clamp(inputX, -aiParams.SteeringLimit, aiParams.SteeringLimit);
        inputX = Mathf.Lerp(inputX, prevInputX, aiParams.SteeringSmooth);

        // Acceleration/braking
        inputY = 1.0f;
        float preferredSpeed = Mode == CarMode.Driving ? aiParams.PreferredSpeed : 0.0f;
        float targetVel = Mathf.Min(preferredSpeed, preventCollisionSpeed);                     // Start with preferred speed, reduced if necessary to prevent a collision
        if (segData != null)                                                                    // Apply AI data min/max speeds
        {
            if (segData.MaxSpeed - segData.MinSpeed < aiParams.MinMaxSpeedBuffer * 2.0f)
            {
                // No room for buffer, just aim for middle of range
                targetVel = (segData.MaxSpeed + segData.MinSpeed) / 2.0f;
            }
            else
            {
                // Clamp to speed range
                targetVel = Mathf.Clamp(targetVel, segData.MinSpeed + aiParams.MinMaxSpeedBuffer, segData.MaxSpeed - aiParams.MinMaxSpeedBuffer);
            }
        }

        // Accelerate or brake to seek target velocity
        inputY = Mathf.Sign(targetVel - state.Velocity.z);      // TODO: Smoother input?

        // Stuck mitigation logic
        inputY = DoCarStuckLogic(state, inputY);

        // Feed input into car
        carController.Move(inputX / 90.0f, inputY, inputY, Mode == CarMode.Parked ? 1.0f : 0.0f);
        prevInputX = inputX;

#if DEBUG_CARAI
        InputX = inputX / 90.0f;
        InputY = inputY;
#endif

        // Set steering wheel position
        if (SteeringWheel != null)
        {
            Vector3 r = SteeringWheel.localRotation.eulerAngles;
            SteeringWheel.localRotation = Quaternion.Euler(r.x, r.y, -inputX);
        }
    }

    private float DoCarStuckLogic(CarState state, float inputY)
    {
        // Detect if the car is stuck for more than 1 second.
        stuckTimer += Time.fixedDeltaTime;
        if (isReversingBecauseStuck)
        {
            // Reverse for 0.2 seconds
            inputY = -1.0f;
            if (stuckTimer > 0.2f)
            {
                isReversingBecauseStuck = false;
                stuckTimer = 0.0f;
            }
        }
        else
        {
            if (stuckTimer > 1.0f)
            {
                // Car has been stuck for a full second. Trigger reverse logic
                isReversingBecauseStuck = true;
                stuckTimer = 0.0f;
                Debug.LogFormat("{0} is reversing because they appear to be stuck", gameObject.name);
            }
            else if (Mathf.Abs(state.Velocity.magnitude) > 0.001f || inputY == 0.0f)
            {
                // Car is moving, or not trying to move => not stuck
                stuckTimer = 0.0f;
            }
        }

        return inputY;
    }
}

public enum CarMode
{
    Driving,
    Stopping,
    Parked
}