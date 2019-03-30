using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AICarIndividualParams))]
public class AICarIndividualParamsWander : WanderingParams<AICarIndividualParams>
{
    [Header("Dimensions")]
    public WanderingParam CarLength;
    public WanderingParam CarWidth;
    public WanderingParam RoadWidth;

    [Header("Steering")]
    public WanderingParam RecenterTime;
    public WanderingParam RecenterAngleRange;
    public WanderingParam SteeringSpeedFactor;
    public WanderingParam SteeringRate;
    public WanderingParam SteeringLimit;
    public WanderingParam SteeringSmooth;
    public WanderingParam CenterXOffsetLimit;
    public WanderingParam StraightenForJumpDistance;
    public WanderingParam SteeringAngleOffset;

    [Header("Speed")]
    public WanderingParam PreferredSpeed;
    public WanderingParam MinMaxSpeedBuffer;

    [Header("Avoiding other cars")]
    public WanderingParam CatchupDurationAvoidLimit;
    public WanderingParam CatchupDurationBrakeLimit;
    public WanderingParam AvoidGapLimit;
    public WanderingParam BrakeXOffsetLimit;

    protected override ParamMapping[] GetMappings()
    {
        return new[]
        {
            new ParamMapping("CarLength", CarLength, p => p.CarLength, (p, v) => p.CarLength = v),
            new ParamMapping("CarWidth", CarWidth, p => p.CarWidth, (p, v) => p.CarWidth = v),
            new ParamMapping("RoadWidth", RoadWidth, p => p.RoadWidth, (p, v) => p.RoadWidth = v),
            new ParamMapping("RecenterTime", RecenterTime, p => p.RecenterTime, (p, v) => p.RecenterTime = v),
            new ParamMapping("RecenterAngleRange", RecenterAngleRange, p => p.RecenterAngleRange, (p, v) => p.RecenterAngleRange = v),
            new ParamMapping("SteeringSpeedFactor", SteeringSpeedFactor, p => p.SteeringSpeedFactor, (p, v) => p.SteeringSpeedFactor = v),
            new ParamMapping("SteeringRate", SteeringRate, p => p.SteeringRate, (p, v) => p.SteeringRate = v),
            new ParamMapping("SteeringLimit", SteeringLimit, p => p.SteeringLimit, (p, v) => p.SteeringLimit = v),
            new ParamMapping("SteeringSmooth", SteeringSmooth, p => p.SteeringSmooth, (p, v) => p.SteeringSmooth = v),
            new ParamMapping("CenterXOffsetLimit", CenterXOffsetLimit, p => p.CenterXOffsetLimit, (p, v) => p.CenterXOffsetLimit = v),
            new ParamMapping("StraightenForJumpDistance", StraightenForJumpDistance, p => p.StraightenForJumpDistance, (p, v) => p.StraightenForJumpDistance = v),
            new ParamMapping("SteeringAngleOffset", SteeringAngleOffset, p => p.SteeringAngleOffset, (p, v) => p.SteeringAngleOffset = v),
            new ParamMapping("PreferredSpeed", PreferredSpeed, p => p.PreferredSpeed, (p, v) => p.PreferredSpeed = v),
            new ParamMapping("MinMaxSpeedBuffer", MinMaxSpeedBuffer, p => p.MinMaxSpeedBuffer, (p, v) => p.MinMaxSpeedBuffer = v),
            new ParamMapping("CatchupDurationAvoidLimit", CatchupDurationAvoidLimit, p => p.CatchupDurationAvoidLimit, (p, v) => p.CatchupDurationAvoidLimit = v),
            new ParamMapping("CatchupDurationBrakeLimit", CatchupDurationBrakeLimit, p => p.CatchupDurationBrakeLimit, (p, v) => p.CatchupDurationBrakeLimit = v),
            new ParamMapping("AvoidGapLimit", AvoidGapLimit, p => p.AvoidGapLimit, (p, v) => p.AvoidGapLimit = v),
            new ParamMapping("BrakeXOffsetLimit", BrakeXOffsetLimit, p => p.BrakeXOffsetLimit, (p, v) => p.BrakeXOffsetLimit = v)
        };
    }
}
