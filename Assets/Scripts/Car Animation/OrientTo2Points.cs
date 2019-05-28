using UnityEngine;

/// <summary>
/// Sets the orientation of the current object so that two reference points align with two target points.
/// Points are defined by objects, as the local origin of the object.
/// </summary>
public class OrientTo2Points : OrientObjectPoints
{
    [Tooltip("Point outside this object's subtree to align 'Reference Pt 1' to")]
    public Transform TargetPt1;

    [Tooltip("Point outside this object's subtree to align 'Reference Pt 2' to")]
    public Transform TargetPt2;

    public OrientType Orient;

    protected override OrientType GetOrientType()
    {
        return Orient;
    }

    protected override Vector3 GetTargetPt1()
    {
        return TargetPt1.position;
    }

    protected override Vector3 GetTargetPt2()
    {
        return TargetPt2.position;
    }
}
