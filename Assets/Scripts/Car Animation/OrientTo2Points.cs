using UnityEngine;

/// <summary>
/// Sets the orientation of the current object so that two reference points align with two target points.
/// Points are defined by objects, as the local origin of the object.
/// </summary>
public class OrientTo2Points : MonoBehaviour
{
    [Tooltip("Point inside this object's subtree to align to 'Target Pt 1'")]
    public Transform ReferencePt1;

    [Tooltip("Point inside this object's subtree to align to 'Target Pt 2'")]
    public Transform ReferencePt2;

    [Tooltip("Point outside this object's subtree to align 'Reference Pt 1' to")]
    public Transform TargetPt1;

    [Tooltip("Point outside this object's subtree to align 'Reference Pt 2' to")]
    public Transform TargetPt2;

    public OrientType Orient;

    public OtherAxisType PrimaryOtherAxis = OtherAxisType.Y;

    private Matrix4x4 refToPoint;
    private Matrix4x4 originalLocalTransform;
    private float refDistance;

    // Start is called before the first frame update
    void Start()
    {
        // Store original local transform as a matrix
        originalLocalTransform = transform.parent.worldToLocalMatrix * transform.localToWorldMatrix;

        // Find reference point positions in local space
        Matrix4x4 worldToLocal = transform.parent.worldToLocalMatrix;
        Vector3 r1Local = worldToLocal.MultiplyPoint(ReferencePt1.position);
        Vector3 r2Local = worldToLocal.MultiplyPoint(ReferencePt2.position);
        refDistance = (r2Local - r1Local).magnitude;

        // Define "point space" such that point 1 is the origin (0,0,0) and point 2 is at (0,0,1)
        // Calculate reference space to point space matrix
        Matrix4x4 pointToRef = ComputePointToLocal(r1Local, r2Local, OrientType.AnchorPt1);
        refToPoint = pointToRef.inverse;
    }

    // Update is called once per frame
    void Update()
    {
        // Find taret point positions in parent transform local space
        Matrix4x4 worldToLocal = transform.parent.worldToLocalMatrix;
        Vector3 t1Local = worldToLocal.MultiplyPoint(TargetPt1.position);
        Vector3 t2Local = worldToLocal.MultiplyPoint(TargetPt2.position);

        // Define "point space" such that point 1 is the origin (0,0,0) and point 2 is at (0,0,1)
        // Calculate point space to target space matrix
        Matrix4x4 pointToTarget = ComputePointToLocal(t1Local, t2Local, Orient);

        // Transform from reference space into point space, then from point space into target space
        Matrix4x4 T = pointToTarget * refToPoint;

        // Set this object's local transform relative to the parent
        SetTransformFromMatrix(transform, T * originalLocalTransform);
    }

    private Matrix4x4 ComputePointToLocal(Vector3 p1, Vector3 p2, OrientType orient)
    {
        // Calculate basis vectors
        // Origin is p1.
        // Z vector is from p1 to p2.
        // Use cross product to get remaining vectors
        Vector3 o = p1;
        Vector3 z = p2 - p1;

        // Adjust origin and z vector based on orientation type
        if (orient != OrientType.Stretch)
        {
            float targetDistance = z.magnitude;
            z = z.normalized * refDistance;
            switch (orient)
            {
                case OrientType.AnchorPt2:
                    o = p2 - z;
                    break;
                case OrientType.Center:
                    o = p1 + z.normalized * (targetDistance - refDistance) * 0.5f;
                    break;
            }
        }

        Vector3 x;
        Vector3 y;
        if (PrimaryOtherAxis == OtherAxisType.Y)
        {
            x = Vector3.Cross(Vector3.up, z).normalized * refDistance;
            y = Vector3.Cross(z, x).normalized * refDistance;
        }
        else
        {
            y = Vector3.Cross(z, Vector3.right).normalized * refDistance;
            x = Vector3.Cross(y, z).normalized * refDistance;
        }

        // Build matrix
        Matrix4x4 M = new Matrix4x4();
        M.SetColumn(0, x.ToVector4());
        M.SetColumn(1, y.ToVector4());
        M.SetColumn(2, z.ToVector4());
        M.SetColumn(3, o.ToVector4(1.0f));

        return M;
    }

    private void SetTransformFromMatrix(Transform t, Matrix4x4 M)
    {
        // Extract axis vectors
        Vector3 x = M.GetColumn(0).ToVector3();
        Vector3 y = M.GetColumn(1).ToVector3();
        Vector3 z = M.GetColumn(2).ToVector3();
        Vector3 p = M.GetColumn(3).ToVector3();

        // Update transform
        t.localPosition = p;
        t.localRotation = Quaternion.LookRotation(z, y);
        t.localScale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
    }

    public enum OrientType
    {
        AnchorPt1,
        AnchorPt2,
        Stretch,
        Center
    }

    public enum OtherAxisType
    {
        X,
        Y
    }
}
