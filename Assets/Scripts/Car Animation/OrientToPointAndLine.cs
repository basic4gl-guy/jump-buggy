using UnityEngine;

public class OrientToPointAndLine : OrientObjectPoints
{
    [Tooltip("Object to align Reference Pt 1 to")]
    public Transform TargetPt;

    [Tooltip("Object defining center of line")]
    public Transform LineCenterPt;

    [Tooltip("Object defining another point on line")]
    public Transform LineOtherPt;

    protected float refDistanceWorld;

    protected override void Start()
    {
        base.Start();

        // Store original distance between points in world space
        refDistanceWorld = (ReferencePt1.position - ReferencePt2.position).magnitude;
    }

    protected override Vector3 GetTargetPt1()
    {
        // Orient point one to target point
        return TargetPt.position;
    }

    protected override Vector3 GetTargetPt2()
    {
        // Orient point 2 to line, preserving original distance between points (refDistance)
        // Essentially this is a line-sphere intersection, where the center is at the target point
        // and the radius is refDistance.

        // Using formula from: https://en.wikipedia.org/wiki/Line%E2%80%93sphere_intersection
        // (Because I'm too lazy to derive it again.)
        Vector3 o = LineCenterPt.position;
        Vector3 l = (LineOtherPt.position - o).normalized;
        Vector3 c = TargetPt.position;
        float r = refDistanceWorld;

        // Common sub expressions
        Vector3 i = o - c;
        float j = Vector3.Dot(l, i);

        // Square root term
        float sterm = j * j - (Vector3.Dot(i, i) - r * r);
        if (sterm < 0.0f)
        {
            Debug.LogError("Line does not intersect sphere");
            return LineOtherPt.position;        // Have to return something :-/
        }

        // Solve for 2 intersections
        float s = Mathf.Sqrt(sterm);
        float x1 = -j - s;
        float x2 = -j + s;

        // Use value closest to 0.
        // This will be the closest point to the "center" of the line.
        float x = Mathf.Abs(x1) < Mathf.Abs(x2) ? x1 : x2;
        return o + x * l;                       // Point on line
    }
}
