using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Track : MonoBehaviour {

    private const int MaxSpacingGroups = 100;

    // Parameters
    public float SegmentLength = 0.25f;

    // Runtime info
    [HideInInspector]
    public CurveRuntimeInfo[] CurveInfos;

    // Working
    private List<Segment> segments;

    public static Track Instance;

    void Start()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void CreateMeshes()
    {
        DeleteMeshes();
        segments = GenerateSegments().ToList();
        PositionCurves();
        BuildMeshes();
    }

    public Curve AddCurve()
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

        // Reposition curves
        segments = GenerateSegments().ToList();
        PositionCurves();

        return curve;
    }

    public void DeleteMeshes()
    {
        var children = Curves
            .SelectMany(c => c.gameObject.GetComponentsInChildren<TemplateCopy>())
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
        CurveInfos = new CurveRuntimeInfo[curves.Count];
        float curveZOffset = 0.0f;
        int index = 0;
        foreach (var curve in curves)
        {
            // Position curve at first segment
            int segIndex = Mathf.FloorToInt(curveZOffset / SegmentLength);
            var seg = GetSegment(segIndex);
            GetSegmentTransform(seg, curve.transform);
            curve.Index = index;

            // Calculate curve runtime info
            var info = new CurveRuntimeInfo();

            // Calculate normal in track space
            int endSegIndex = Mathf.FloorToInt((curveZOffset + curve.Length) / SegmentLength);
            int midSegIndex = (segIndex + endSegIndex) / 2;
            info.Normal = GetSegment(midSegIndex).GetSegmentToTrack().MultiplyVector(Vector3.up);

            // Calculate respawn point and direction vectors in track space
            int respawnSeg = Math.Min(segIndex + Mathf.CeilToInt(2.0f / SegmentLength), midSegIndex);
            Matrix4x4 respawnTransform = GetSegment(respawnSeg).GetSegmentToTrack();
            info.RespawnPosition = respawnTransform.MultiplyPoint(Vector3.zero);
            info.RespawnRotation = Quaternion.LookRotation(
                respawnTransform.MultiplyVector(Vector3.forward).normalized,
                respawnTransform.MultiplyVector(Vector3.up).normalized);

            CurveInfos[index] = info;

            index++;
            curveZOffset += curve.Length;
        }
    }

    private void BuildMeshes()
    {
        if (!segments.Any()) return;

        var spacingGroupStates = new SpacingGroupState[MaxSpacingGroups];
        for (int i = 0; i < MaxSpacingGroups; i++)
            spacingGroupStates[i] = new SpacingGroupState();

        // Work down the curve. Add meshes as we go.
        var totalLength = segments.Count * SegmentLength;
        float meshZOffset = 0.0f;
        Template template = null;
        while (meshZOffset < totalLength)
        {
            // Find segment where mesh starts
            int segIndex = Mathf.FloorToInt(meshZOffset / SegmentLength);
            var seg = segments[segIndex];
            var curve = seg.Curve;

            // Look for mesh template
            if (curve.Template != null)
                template = curve.Template;

            // Initialise spacing groups for this template
            foreach (var group in spacingGroupStates)
                group.IsActiveThisTemplate = false;

            // Generate meshes
            if (!curve.IsJump && template != null)
            {
                // Create template copy object
                var templateCopyObj = new GameObject();
                templateCopyObj.transform.parent = curve.transform;
                templateCopyObj.isStatic = gameObject.isStatic;
                templateCopyObj.name = curve.name + " > " + template.name;
                templateCopyObj.tag = "Generated";
                GetSegmentTransform(seg, templateCopyObj.transform);

                // Add template copy component
                var templateCopy = templateCopyObj.AddComponent<TemplateCopy>();
                templateCopy.Template = template;

                // Pass 1: Generate continuous meshes
                bool isFirstMesh = true;
                float mainMeshLength = 0.0f;
                foreach (var subtree in template.FindSubtrees<ContinuousMesh>())
                {
                    // Duplicate subtree
                    var subtreeCopy = Instantiate(subtree);
                    subtreeCopy.transform.parent = templateCopyObj.transform;
                    subtreeCopy.gameObject.isStatic = gameObject.isStatic;
                    subtreeCopy.name += " Continuous";
                    GetSegmentTransform(seg, subtreeCopy.transform);

                    // Need to take into account relative position of continuous subtree within template object
                    Matrix4x4 templateFromSubtree = template.transform.localToWorldMatrix.inverse * subtree.transform.localToWorldMatrix;

                    // Clone and warp displayed meshes
                    var meshFilters = subtreeCopy.GetComponentsInChildren<MeshFilter>();
                    foreach (var mf in meshFilters)
                    {
                        mf.sharedMesh = CloneMesh(mf.sharedMesh);
                        Matrix4x4 subtreeFromMesh = subtreeCopy.transform.localToWorldMatrix.inverse * mf.transform.localToWorldMatrix;
                        Matrix4x4 templateFromMesh = templateFromSubtree * subtreeFromMesh;
                        Matrix4x4 meshFromWorld = mf.transform.localToWorldMatrix.inverse;
                        float meshLength = WarpMeshToCurves(mf.sharedMesh, meshZOffset, templateFromMesh, meshFromWorld);
#if UNITY_EDITOR
                        Unwrapping.GenerateSecondaryUVSet(mf.sharedMesh);
#endif

                        // First continuous mesh is considered to be the main track surface,
                        // and determines the length of the template copy
                        if (isFirstMesh)
                        {
                            var surface = mf.gameObject.AddComponent<TrackSurface>();
                            int endSegIndex = Mathf.FloorToInt((meshZOffset + meshLength) / SegmentLength - 0.00001f);
                            Segment endSeg = segments[Math.Min(endSegIndex, segments.Count - 1)];
                            surface.StartCurveIndex = seg.Curve.Index;
                            surface.EndCurveIndex = endSeg.Curve.Index;
                            mainMeshLength = meshLength;
                            isFirstMesh = false;
                        }
                    }

                    // Clone and warp mesh colliders
                    var meshColliders = subtreeCopy.GetComponentsInChildren<MeshCollider>();
                    foreach (var mc in meshColliders)
                    {
                        mc.sharedMesh = CloneMesh(mc.sharedMesh);
                        Matrix4x4 subtreeFromMesh = subtreeCopy.transform.localToWorldMatrix.inverse * mc.transform.localToWorldMatrix;
                        Matrix4x4 templateFromMesh = templateFromSubtree * subtreeFromMesh;
                        Matrix4x4 meshFromWorld = mc.transform.localToWorldMatrix.inverse;
                        float meshLength = WarpMeshToCurves(mc.sharedMesh, meshZOffset, templateFromMesh, meshFromWorld);
                    }
                };

                // Pass 2: Generate spaced meshes
                foreach (var subtree in template.FindSubtrees<Spaced>())
                {
                    // Search up parent chain for spacing group
                    var spacingGroup = subtree.GetComponentInParent<SpacingGroup>();
                    if (spacingGroup == null)
                    {
                        Debug.LogError("Cannot find spacing group for spaced template component: " + subtree.name);
                        continue;
                    }

                    // Validate
                    if (spacingGroup.Index < 0 || spacingGroup.Index >= MaxSpacingGroups)
                    {
                        Debug.LogError("Invalid spacing group " + spacingGroup.Index + " found in template: " + template.name);
                        continue;
                    }
                    if (spacingGroup.Spacing < SegmentLength)
                    {
                        Debug.LogError("Spacing too small in spacing group, in template: " + template.name);
                        continue;
                    }

                    // Activate spacing group
                    var groupState = spacingGroupStates[spacingGroup.Index];
                    groupState.IsActiveThisTemplate = true;
                    if (!groupState.IsActive)
                        groupState.ZOffset = meshZOffset;

                    // Walk spacing group forward to current curve
                    groupState.ZOffsetThisTemplate = groupState.ZOffset;
                    while (groupState.ZOffsetThisTemplate + spacingGroup.SpacingBefore < meshZOffset)
                        groupState.ZOffsetThisTemplate += spacingGroup.Spacing;

                    // Generate spaced objects for current curve
                    while (groupState.ZOffsetThisTemplate + spacingGroup.SpacingBefore < meshZOffset + mainMeshLength)
                    {
                        float spaceZOffset = groupState.ZOffsetThisTemplate + spacingGroup.SpacingBefore;
                        var spaceSegIndex = Mathf.FloorToInt(spaceZOffset / SegmentLength);
                        var spaceSeg = GetSegment(spaceSegIndex);

                        // Duplicate subtree
                        var subtreeCopy = Instantiate(subtree);
                        subtreeCopy.transform.parent = templateCopyObj.transform;
                        subtreeCopy.gameObject.isStatic = gameObject.isStatic;
                        subtreeCopy.name += " Spacing group " + spacingGroup.Index;

                        // Calculate local to track tranform matrix for subtree.
                        Matrix4x4 templateFromSubtree = template.transform.localToWorldMatrix.inverse * subtree.transform.localToWorldMatrix;
                        Matrix4x4 trackFromSubtree = 
                              spaceSeg.GetSegmentToTrack(spaceZOffset - spaceSegIndex * SegmentLength)      // Segment -> Track
                            * templateFromSubtree;                                                          // Subtree -> Segment

                        if (subtree.IsVertical)
                        {
                            // Disassemble matrix
                            Vector3 basisX = ToVector3(trackFromSubtree.GetRow(0));
                            Vector3 basisY = ToVector3(trackFromSubtree.GetRow(1));
                            Vector3 basisZ = ToVector3(trackFromSubtree.GetRow(2));

                            // Align Y vector with global Y axis
                            basisY = Vector3.up * basisY.magnitude;

                            // Cross product to get X and Z (assuming matrix is orthogonal)
                            basisX = Vector3.Cross(basisY, basisZ).normalized * basisX.magnitude;
                            basisZ = Vector3.Cross(basisX, basisY).normalized * basisZ.magnitude;

                            // Recompose matrix
                            trackFromSubtree.SetColumn(0, ToVector4(basisX));
                            trackFromSubtree.SetColumn(1, ToVector4(basisY));
                            trackFromSubtree.SetColumn(2, ToVector4(basisZ));
                        }

                        // Get local to world transform matrix for subtree.
                        Matrix4x4 subtreeTransform = transform.localToWorldMatrix                           // Track -> World
                            * trackFromSubtree;                                                             // Subtree -> Track

                        // Calculate local transform. Essentially subtree->template space transform
                        Matrix4x4 localTransform = templateCopyObj.transform.localToWorldMatrix.inverse     // World -> template
                            * subtreeTransform;                                                             // Subtree -> World

                        // Use to set transform
                        subtreeCopy.transform.localPosition = localTransform.MultiplyPoint(Vector3.zero);
                        subtreeCopy.transform.localRotation = localTransform.rotation;
                        subtreeCopy.transform.localScale    = localTransform.lossyScale;                        

                        // Move on to next
                        groupState.ZOffsetThisTemplate += spacingGroup.Spacing;
                    }
                }

                // Move forward to start of next mesh template
                meshZOffset += mainMeshLength;
            }

            // Update spacing group active flags
            foreach (var group in spacingGroupStates)
            {
                group.IsActive = group.IsActiveThisTemplate;
                group.ZOffset = group.ZOffsetThisTemplate;
            }

            // Ensure Z offset is advanced at least to the next segment.
            // (Otherwise 0 length templates would cause an infinite loop)
            meshZOffset = Mathf.Max(meshZOffset, (segIndex + 1) * SegmentLength);
        }
    }

    private float WarpMeshToCurves(Mesh mesh, float meshZOffset, Matrix4x4 meshToTemplate, Matrix4x4 worldToMesh)
    {
        // Find length of mesh            
        float meshMaxZ = mesh.vertices.Max(v => meshToTemplate.MultiplyPoint(v).z);
        float meshMinZ = mesh.vertices.Min(v => meshToTemplate.MultiplyPoint(v).z);
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
            Vector3 v = meshToTemplate.MultiplyPoint(mesh.vertices[i]);
            Vector3 n = meshToTemplate.MultiplyVector(mesh.normals[i]);

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

    public List<Curve> Curves
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

    private Vector3 ToVector3(Vector4 v)
    {
        return new Vector3(v.x, v.y, v.z);
    }

    private Vector4 ToVector4(Vector3 v, float w = 0.0f)
    {
        return new Vector4(v.x, v.y, v.z, w);
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

        public Matrix4x4 GetSegmentToTrack(float segZ = 0.0f)
        {
            float f = segZ / Length;                                                            // Fractional distance along segment
            float adjZDir = Direction.z + DirectionDelta.z * f;                                 // Adjust Z axis rotation based on distance down segment
            Vector3 adjDir = new Vector3(Direction.x, Direction.y, adjZDir);
            return Matrix4x4.Translate(Position) * Matrix4x4.Rotate(Quaternion.Euler(adjDir));
        }
    }

    private class SpacingGroupState
    {
        public bool IsActive = false;
        public bool IsActiveThisTemplate = false;
        public float ZOffset = 0.0f;
        public float ZOffsetThisTemplate = 0.0f;
    }

    [Serializable]
    public class CurveRuntimeInfo
    {
        public Vector3 Normal;
        public Vector3 RespawnPosition;
        public Quaternion RespawnRotation;
    }
}