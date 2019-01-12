using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Track : MonoBehaviour {

    public float SegmentLength = 0.25f;
    public float MeshScale = 3.0f;

    // Working
    private List<Segment> segments;

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public void CreateMeshes()
    {
        DeleteMeshes();
        segments = GenerateSegments().ToList();
        PositionCurves();
        BuildMeshes();
    }

    public void AddCurve()
    {
        var lastCurve = Curves.LastOrDefault();

        // Add curve to end of list
        var obj = new GameObject();
        obj.transform.parent = transform;
        obj.name = "Curve";
        obj.isStatic = gameObject.isStatic;
        var curve = obj.AddComponent<Curve>();

        // Copy values from last curve
        if (lastCurve != null)
        {
            curve.Length = lastCurve.Length;
            curve.Angles = lastCurve.Angles;
            curve.IsJump = lastCurve.IsJump;
            curve.CanRespawn = lastCurve.CanRespawn;
        }

        PositionCurves();
    }

    public void DeleteMeshes()
    {
        var children = Curves
            .SelectMany(c => c.gameObject.GetComponentsInChildren<MeshTemplateCopy>())
            .Where(t => t.gameObject.tag == "Generated")
            .ToList();

        // Delete them
        foreach (var child in children)
        {
            if (Application.isEditor)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
        }
    }

    public void PositionCurves()
    {
        // Position curve objects at the start of their curves
        var curves = Curves;
        float curveZOffset = 0.0f;
        foreach (var curve in curves)
        {
            var seg = GetSegment(Mathf.FloorToInt(curveZOffset / SegmentLength));
            GetSegmentTransform(seg, curve.transform);
            curveZOffset += curve.Length;
        }
    }

    private void BuildMeshes()
    {
        if (!segments.Any()) return;

        // Work down the curve. Add meshes as we go.
        var totalLength = segments.Count * SegmentLength;
        float meshZOffset = 0.0f;
        MeshFilter meshFilter = null;
        while (meshZOffset < totalLength)
        {
            // Find segment where mesh starts
            int segIndex = Mathf.FloorToInt(meshZOffset / SegmentLength);
            var seg = segments[segIndex];
            var curve = seg.Curve;

            // Look for mesh
            if (curve.MeshFilter != null)
                meshFilter = seg.Curve.MeshFilter;

            // Generate meshes
            if (!curve.IsJump && meshFilter != null)
            {
                // Create template copy object
                var templateCopyObj = new GameObject();
                templateCopyObj.transform.parent = curve.transform;
                templateCopyObj.isStatic = gameObject.isStatic;
                templateCopyObj.name = curve.name + " > " + meshFilter.name;
                templateCopyObj.tag = "Generated";
                GetSegmentTransform(seg, templateCopyObj.transform);
                var templateCopy = templateCopyObj.AddComponent<MeshTemplateCopy>();

                // Copy mesh and warp to road curves
                var meshFilterCopy = Instantiate(meshFilter);
                GetSegmentTransform(seg, meshFilterCopy.transform);
                meshFilterCopy.gameObject.isStatic = gameObject.isStatic;
                meshFilterCopy.transform.parent = templateCopyObj.transform;
                meshFilterCopy.sharedMesh = CloneMesh(meshFilterCopy.sharedMesh);
                Matrix4x4 worldToMesh = meshFilterCopy.transform.localToWorldMatrix.inverse;
                float meshLength = WarpMeshToCurves(meshFilterCopy.sharedMesh, meshZOffset, worldToMesh);
#if UNITY_EDITOR
                Unwrapping.GenerateSecondaryUVSet(meshFilterCopy.sharedMesh);
#endif
                var meshColliderCopy = meshFilterCopy.GetComponent<MeshCollider>();
                if (meshColliderCopy != null)
                {
                    meshColliderCopy.sharedMesh = CloneMesh(meshColliderCopy.sharedMesh);
                    WarpMeshToCurves(meshColliderCopy.sharedMesh, meshZOffset, worldToMesh);
                }

                // Advance to start of next template
                meshZOffset += Mathf.Max(meshLength, SegmentLength);
            }
            else
            {
                meshZOffset = (segIndex + 1) * SegmentLength;
            }
        }
    }

    private float WarpMeshToCurves(Mesh mesh, float meshZOffset, Matrix4x4 worldToMesh)
    {
        // Transform to apply to mesh
        Matrix4x4 meshTransform = Matrix4x4.Scale(new Vector3(MeshScale, MeshScale, MeshScale))
                                * Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0.0f, 0.0f));       // Account for Blender Y vs Z axis

        // Find length of mesh            
        float meshMaxZ = mesh.vertices.Max(v => meshTransform.MultiplyPoint(v).z);
        float meshMinZ = mesh.vertices.Min(v => meshTransform.MultiplyPoint(v).z);
        float meshLength = meshMaxZ - meshMinZ;

        // Lookup first segment
        int segIndex = Mathf.FloorToInt(meshZOffset / SegmentLength);
        Segment seg = segments[segIndex];

        // Warp vertices around road curve
        Debug.Assert(mesh.vertices.Length == mesh.normals.Length);
        Debug.Assert(mesh.vertices.Length == mesh.tangents.Length);
        var vertices = new Vector3[mesh.vertices.Length];
        var normals = new Vector3[mesh.normals.Length];
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            Vector3 v = meshTransform.MultiplyPoint(mesh.vertices[i]);
            Vector3 n = meshTransform.MultiplyVector(mesh.normals[i]);

            // z determines index in meshSegments array
            float z = v.z - meshMinZ + meshZOffset;                                         // Total Z along all curves
            var vertSegIndex = Mathf.FloorToInt(z / SegmentLength);
            var vertSeg = GetSegment(vertSegIndex);

            // Calculate warped position
            Vector3 segPos = new Vector3(v.x, v.y, z - vertSegIndex * SegmentLength);       // Position in segment space
            Matrix4x4 segToTrack = vertSeg.GetSegmentToTrack(segPos.z);
            Vector3 trackPos = segToTrack.MultiplyPoint(segPos);                            // => Track space
            Vector3 worldPos = transform.TransformPoint(trackPos);                          // => World space
            vertices[i] = worldToMesh.MultiplyPoint(worldPos);                              // => Mesh space

            // Warp normal
            Vector3 segNorm = n;                                                            // Normal in segment space
            Vector3 trackNorm = segToTrack.MultiplyVector(segNorm);                         // => Track space
            Vector3 worldNorm = transform.TransformVector(trackNorm);                       // => World space
            normals[i] = worldToMesh.MultiplyVector(worldNorm).normalized;                  // => Mesh space
        }

        // Replace mesh vertices and normals
        mesh.vertices = vertices;
        mesh.normals = normals;

        // Recalculate tangents and bounds
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        return meshLength;
    }

    private void GetSegmentTransform(Segment seg, Transform dstTransform)
    {
        // Set transform to position object at start of segment, with 
        // Y axis rotation set (but not X and Z).

        // Segment gives position and rotation in track space.
        var segmentToTrack = seg.GetSegmentToTrack(0.0f);
        var segPos = segmentToTrack.MultiplyPoint(Vector3.zero);
        var segForward = segmentToTrack.MultiplyVector(Vector3.forward);

        // Convert to world space
        var worldPos = transform.TransformPoint(segPos);
        var worldForward = transform.TransformVector(segForward);

        // Set transform
        dstTransform.position = worldPos;
        dstTransform.rotation = Quaternion.LookRotation(worldForward);
    }

    /// <summary>
    /// Generate segments from track curves
    /// </summary>
    /// <returns></returns>
    private IEnumerable<Segment> GenerateSegments()
    {
        // Find curves. Must be in immediate children.
        var curves = Curves;

        // Walk along curve in track space.
        Vector3 pos = Vector3.zero;
        Vector3 dir = Vector3.zero;

        Vector3 dirDelta = Vector3.zero;
        Vector3 posDelta = Vector3.forward * SegmentLength;
        foreach (var curve in curves)
        {
            // Find delta to add to curve each segment
            dirDelta = new Vector3(
                Util.LocalAngle(curve.Angles.x - dir.x),
                curve.Angles.y,
                Util.LocalAngle(curve.Angles.z - dir.z)
            ) / curve.Length * SegmentLength;

            // Generate segments
            for (float d = 0.0f; d < curve.Length; d += SegmentLength)
            {
                var segment = new Segment
                {
                    Position = pos,
                    Direction = dir,
                    DirectionDelta = dirDelta,
                    Length = SegmentLength,
                    Curve = curve
                };
                yield return segment;

                // Advance to start of next segment
                pos += segment.GetSegmentToTrack(0.0f).MultiplyVector(posDelta);
                dir += dirDelta;
            }
        }

        // Return final segment
        yield return new Segment
        {
            Position = pos,
            Direction = dir,
            DirectionDelta = dirDelta,
            Length = SegmentLength,
            Curve = curves.LastOrDefault()
        };
    }

    private Segment GetSegment(int i)
    {
        if (i < 0) return segments[0];
        if (i < segments.Count) return segments[i];

        // It's likely meshes won't exactly add up to the Z length of the curves, so the last one will overhang.
        // We allow for this by generating a virtual segment extruded from the last segment in the list.
        var lastSeg = segments[segments.Count - 1];
        return new Segment
        {
            Position = lastSeg.Position + lastSeg.GetSegmentToTrack(0.0f).MultiplyVector(Vector3.forward * lastSeg.Length * (i - segments.Count)),
            Direction = lastSeg.Direction,
            DirectionDelta = Vector3.zero,
            Length = lastSeg.Length,
            Curve = lastSeg.Curve
        };
    }

    private List<Curve> Curves
    {
        get
        {
            return Enumerable.Range(0, gameObject.transform.childCount)
                            .Select(i => gameObject.transform.GetChild(i).GetComponent<Curve>())
                            .Where(c => c != null)
                            .ToList();
        }
    }

    /// <summary>
    /// Shallow copy mesh object
    /// </summary>
    private static Mesh CloneMesh(Mesh src)
    {
        // Note: This doesn't copy all fields, just the ones we use
        var dst = new Mesh()
        {
            name = src.name + " clone",
            vertices = src.vertices,
            normals = src.normals,
            tangents = src.tangents,
            uv = src.uv,
            uv2 = src.uv2,
            uv3 = src.uv3,
            uv4 = src.uv4,
            uv5 = src.uv5,
            uv6 = src.uv6,
            uv7 = src.uv7,
            uv8 = src.uv8,
            triangles = src.triangles,
            colors = src.colors,
            colors32 = src.colors32,
            bounds = src.bounds,
            subMeshCount = src.subMeshCount,
            indexFormat = src.indexFormat,
            bindposes = src.bindposes,
            boneWeights = src.boneWeights,
            hideFlags = src.hideFlags            
        };
        for (int i = 0; i < src.subMeshCount; i++)
            dst.SetTriangles(src.GetTriangles(i), i);
        return dst;
    }

    /// <summary>
    /// Track curves are broken down into tiny straight segments.
    /// </summary>
    private class Segment
    {
        public Vector3 Position;
        public Vector3 Direction;
        public Vector3 DirectionDelta;
        public float Length;
        public Curve Curve;

        public Matrix4x4 GetSegmentToTrack(float segZ)
        {
            float f = segZ / Length;                                                            // Fractional distance along segment
            float adjZDir = Direction.z + DirectionDelta.z * f;                                 // Adjust Z axis rotation based on distance down segment
            Vector3 adjDir = new Vector3(Direction.x, Direction.y, adjZDir);
            return Matrix4x4.Translate(Position) * Matrix4x4.Rotate(Quaternion.Euler(adjDir));
        }
    }
}