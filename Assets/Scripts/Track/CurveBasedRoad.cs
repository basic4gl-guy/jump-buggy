using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class CurveBasedRoad : MonoBehaviour {

    public RoadType Type = RoadType.Game;

    [Header("Generation")]
    public float SegmentLength = 0.25f;
    public float MeshScale = 1.0f;
    public MeshFilter CollisionMesh;

    [Header("Preview")]
    public MeshFilter PreviewMesh;

    [Header("Debugging")]
    public MeshFilter OverrideMesh;

    [Header("Road")]
    public Support[] Supports;
    public Curve[] Curves = { new Curve { Length = 10.0f } };

    [Header("Runtime")]
    public float RespawnHeight = 0.75f;

    // Runtime info
    [HideInInspector]
    public CurveRuntimeInfo[] CurveInfos;

    public static CurveBasedRoad Instance;

    void Start ()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RebuildMeshes()
    {
        DeleteMeshes();
        var segments = this.GetSegments(SegmentLength).ToList();
        BuildRoadMeshes(segments);
        BuildSupportMeshes(segments);
        BuildCurveRuntimeInfo(segments);
    }

    /// <summary>
    /// Create copy of road to display as preview in menu screen
    /// </summary>
    public CurveBasedRoad CreatePreviewCopy()
    {
        var copyObject = new GameObject();
        copyObject.isStatic = false;
        var copy = copyObject.AddComponent<CurveBasedRoad>();
        copy.Type = RoadType.MenuPreview;
        copy.SegmentLength = SegmentLength;
        copy.PreviewMesh = PreviewMesh;
        copy.MeshScale = MeshScale;
        copy.Curves = this.Curves.Select(c => new Curve
            {
                Length = c.Length,
                Angles = c.Angles,
                SupportIndex = -1,
                Mesh = c.Mesh != null || c.LODGroup != null ? PreviewMesh : null
            }).ToArray();

        copy.gameObject.name = SceneManager.GetActiveScene().name + " preview";

        return copy;
    }

    public override int GetHashCode()
    {
        return Util.GetHash(
            SegmentLength,
            MeshScale,
            Util.GetArrayHash(Curves));
    }

    public void DeleteMeshes()
    {
        var children = GetComponentsInChildren<Transform>().Where(t => t.tag == "Generated").ToArray();
        foreach (var child in children)
        {
            if (Application.isEditor)
            {
                DestroyImmediate(child.gameObject);
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void BuildRoadMeshes(List<Segment> segments)
    {
        if (!segments.Any()) return;

        // Rotate meshes by 90 degrees to account for blender and unity coordinate system differences)
        Matrix4x4 meshTransform = Matrix4x4.Scale(new Vector3(MeshScale, MeshScale, MeshScale)) * Matrix4x4.Rotate(Quaternion.Euler(-90, 0, 0));

        var totalLength = segments.Count * SegmentLength;
        float meshZOffset = 0.0f;                                       // Offset from start of curves to current mesh

        // Fit meshes to curve segments
        while (meshZOffset < totalLength)
        {
            // Find segment where mesh starts
            // Segment will indicate what mesh to use
            int segIndex = Mathf.FloorToInt(meshZOffset / SegmentLength);
            Segment seg = segments[segIndex];

            if (seg.Mesh != null || (seg.LODGroup != null && OverrideMesh != null))
            {
                // Copy and warp single mesh

                // Instantiate mesh instance
                // Note: Don't try to simplify to "OverrideMesh ?? seg.Mesh". Unity appears to implement its own "null" which doesn't support the ?? operator.
                var meshFilter = Instantiate(OverrideMesh != null ? OverrideMesh : seg.Mesh, gameObject.transform, false);
                meshFilter.tag = "Generated";
                meshFilter.name += " Curves[" + seg.CurveIndex + "]";
                meshFilter.gameObject.isStatic = gameObject.isStatic;

                // Warp mesh around road cuves
                float meshLength = WarpMeshToRoadCurves(segments, meshFilter, meshZOffset, meshTransform);

                // Add gameplay content
                if (Type == RoadType.Game)
                {
                    // Create collision mesh
                    if (CollisionMesh != null)
                    {
                        // Copy collision mesh and warp to road curves
                        var collisionMesh = Instantiate(CollisionMesh, gameObject.transform, false);
                        WarpMeshToRoadCurves(segments, collisionMesh, meshZOffset, meshTransform);

                        // Create collider for new mesh
                        var collider = meshFilter.gameObject.AddComponent<MeshCollider>();
                        collider.sharedMesh = collisionMesh.sharedMesh;
                    }
                    else
                        meshFilter.gameObject.AddComponent<MeshCollider>();     // Add a default mesh collider. It will use the rendered mesh.

                    // Add RoadMeshInfo linking mesh back to generating curve(s)
                    int endSegIndex = Mathf.FloorToInt((meshZOffset + meshLength) / SegmentLength - 0.00001f);
                    Segment endSeg = segments[Math.Min(endSegIndex, segments.Count - 1)];
                    var roadInfo = meshFilter.gameObject.AddComponent<RoadMeshInfo>();
                    roadInfo.StartCurveIndex = seg.CurveIndex;
                    roadInfo.EndCurveIndex = endSeg.CurveIndex;

                    // Generate UVs for baked lighting
#if UNITY_EDITOR
                    Unwrapping.GenerateSecondaryUVSet(meshFilter.mesh);
#endif
                }

                // Move forward to next mesh
                meshZOffset += meshLength;
            }
            else
            if (seg.LODGroup != null)
            {
                // Copy LOD group and warp its meshes.
                // Note: This works, but does not result in any noticeable speedup in practice.
                // Therefore I recommend using single meshes instead.
                // I suspect the terrain is still the geometry bottleneck.

                // Instantiate LOD instance
                var lodGroup = Instantiate(seg.LODGroup, gameObject.transform, false);
                lodGroup.tag = "Generated";
                lodGroup.name += " Curves[" + seg.CurveIndex + "]";
                lodGroup.gameObject.isStatic = gameObject.isStatic;

                // Position LOD group at segment position.
                // Actual position doesn't really matter, because we transform all the vertices
                // to their world space position along the curve. However it's nice to keep the 
                // game object position close to where the vertices are.
                lodGroup.transform.position = seg.Position;
                lodGroup.transform.rotation = Quaternion.Euler(seg.Direction);

                // Clone LOD group and renderers, warping the meshes inside each renderer to the road curvature
                float firstMeshLength = 0.0f;
                MeshFilter firstMeshFilter = null;
                bool isFirstMesh = true;
                LOD[] lods = lodGroup.GetLODs().Select(lod =>
                {
                    Renderer[] renderers = lod.renderers.Select(renderer =>
                    {
                        var meshFilter = renderer.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                        {
                            float meshLength = WarpMeshToRoadCurves(segments, meshFilter, meshZOffset, meshTransform);
                            if (isFirstMesh)
                            {
                                firstMeshLength = meshLength;
                                firstMeshFilter = meshFilter;
                                isFirstMesh = false;
                            }
                        }

                        return renderer;
                    }).ToArray();

                    return new LOD(lod.screenRelativeTransitionHeight, renderers);
                }).ToArray();

                // Assign LODs to new group
                lodGroup.SetLODs(lods);

                // Add gameplay content
                firstMeshLength = Mathf.Max(firstMeshLength, 1.0f);
                if (Type == RoadType.Game)
                {
                    // Create collision mesh
                    if (CollisionMesh != null)
                    {
                        // Copy collision mesh and warp to road curves
                        var collisionMesh = Instantiate(CollisionMesh, gameObject.transform, false);
                        collisionMesh.tag = "Generated";
                        WarpMeshToRoadCurves(segments, collisionMesh, meshZOffset, meshTransform);

                        // Create collider for new mesh
                        var collider = lodGroup.gameObject.AddComponent<MeshCollider>();
                        collider.sharedMesh = collisionMesh.sharedMesh;
                    }
                    else if (firstMeshFilter != null)
                    {
                        // Create a mesh collider based on the first mesh (which should be in LOD0)
                        var collider = lodGroup.gameObject.AddComponent<MeshCollider>();
                        collider.sharedMesh = firstMeshFilter.sharedMesh;
                    }

                    // Add RoadMeshInfo linking LOD group back to generating curve(s)
                    int endSegIndex = Mathf.FloorToInt((meshZOffset + firstMeshLength) / SegmentLength - 0.00001f);
                    Segment endSeg = segments[Math.Min(endSegIndex, segments.Count - 1)];
                    var roadInfo = lodGroup.gameObject.AddComponent<RoadMeshInfo>();
                    roadInfo.StartCurveIndex = seg.CurveIndex;
                    roadInfo.EndCurveIndex = endSeg.CurveIndex;
                }

                meshZOffset += firstMeshLength;
            }
            else
            {
                meshZOffset = (segIndex + 1) * SegmentLength;
            }
        }
    }

    private float WarpMeshToRoadCurves(List<Segment> segments, MeshFilter meshFilter, float meshZOffset, Matrix4x4 meshTransform)
    {
        var mesh = meshFilter.mesh;

        // Lookup first segment
        int segIndex = Mathf.FloorToInt(meshZOffset / SegmentLength);
        Segment seg = segments[segIndex];

        // Position mesh at segment position.
        // Actual position doesn't really matter, because we transform all the vertices
        // to their world space position along the curve. However it's nice to keep the 
        // mesh instance position close to where the vertices are.
        meshFilter.transform.position = seg.Position;
        meshFilter.transform.rotation = Quaternion.Euler(seg.Direction);
        Matrix4x4 worldToMesh =  meshFilter.transform.localToWorldMatrix.inverse * meshFilter.transform.parent.localToWorldMatrix;      // To convert back to meshFilter space once world space position has been calculated.

        // Find length of mesh            
        float meshMaxZ = mesh.vertices.Max(v => meshTransform.MultiplyPoint(v).z);
        float meshMinZ = mesh.vertices.Min(v => meshTransform.MultiplyPoint(v).z);
        float meshLength = meshMaxZ - meshMinZ;

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
            var vertSeg = GetSegment(segments, vertSegIndex);

            // Calculate warped position
            Vector3 segPos = new Vector3(v.x, v.y, z - vertSegIndex * SegmentLength);       // Position in segment space
            Matrix4x4 segTransform = vertSeg.GetTransform(segPos.z);
            Vector3 worldPos = segTransform.MultiplyPoint(segPos);                      // => World space
            vertices[i] = worldToMesh.MultiplyPoint(worldPos);                          // => Mesh space

            // Warp normal
            Vector3 worldNorm = segTransform.MultiplyVector(n);                         // Normal in world space
            normals[i] = worldToMesh.MultiplyVector(worldNorm).normalized;              // => Mesh space
        }

        // Update vertices
        mesh.vertices = vertices;
        mesh.normals = normals;

        // Fix up bounding and collision volumes
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return meshLength;
    }

    private void BuildSupportMeshes(List<Segment> segments)
    {
        if (!segments.Any()) return;

        var totalLength = segments.Count * SegmentLength;
        float supportZOffset = 0.0f;                                    // Offset from start of curves to current mesh
        while (supportZOffset < totalLength)
        {
            // Find segment corresponding to Z offset
            int segIndex = Mathf.FloorToInt(supportZOffset / SegmentLength);
            Segment seg = segments[segIndex];

            // If no support specified -> Skip ahead until segment found with a support
            if (seg.SupportIndex < 0 || seg.SupportIndex >= Supports.Length)
            {
                supportZOffset = (segIndex + 1) * SegmentLength;
                continue;
            }
            var support = Supports[seg.SupportIndex];

            // Instantiate left support
            CreateSupport(segments, -support.HorizontalSpacing, supportZOffset, support.Mesh, support.Radius);

            // Instantiate right support
            if (support.HorizontalSpacing > 0.0f)
                CreateSupport(segments, support.HorizontalSpacing, supportZOffset, support.Mesh, support.Radius);

            // Advance to next support
            supportZOffset += Mathf.Max(support.Spacing, 0.1f);
        }
    }

    private void CreateSupport(List<Segment> segments, float x, float z, Transform mesh, float radius)
    {
        // Find segment
        int segIndex = Mathf.FloorToInt(z / SegmentLength);
        if (segIndex < 0 || segIndex >= segments.Count) return;
        Segment seg = segments[segIndex];

        // Generate mesh
        var support = Instantiate(mesh, gameObject.transform, false);
        support.tag = "Generated";
        support.name += " Curves[" + seg.CurveIndex + "]";
        support.gameObject.isStatic = gameObject.isStatic;

        // Calculate support position
        Vector3 segPos = new Vector3(x, 0.0f, z - segIndex * SegmentLength);                            // Position in segment space
        Matrix4x4 segTransform = seg.GetTransform(segPos.z);
        Vector3 worldPos = segTransform.MultiplyPoint(segPos);

        // Find minimum Y value around radius, to ensure pole doesn't poke through road.
        float minY = worldPos.y;
        if (radius > 0.0f)
        {
            for (int i = 0; i < 360; i += 45)
            {
                // Find edge point position
                float edgeX = x + Mathf.Sin(i * Mathf.Deg2Rad) * radius;
                float edgeZ = z + Mathf.Cos(i * Mathf.Deg2Rad) * radius;

                // Find corresponding segment
                int edgeSegIndex = Mathf.FloorToInt(edgeZ / SegmentLength);
                if (edgeSegIndex < 0 || edgeSegIndex >= segments.Count) continue;
                Segment edgeSeg = segments[edgeSegIndex];

                // Calculate edge position in world space
                Vector3 edgeSegPos = new Vector3(edgeX, 0.0f, edgeZ - edgeSegIndex * SegmentLength);    // Position in segment space
                Matrix4x4 edgeSegTransform = edgeSeg.GetTransform(edgeSegPos.z);
                Vector3 edgeWorldPos = edgeSegTransform.MultiplyPoint(edgeSegPos);
                if (edgeWorldPos.y < minY)
                    minY = edgeWorldPos.y;
            }
        }

        support.transform.localPosition = new Vector3(worldPos.x, minY, worldPos.z);
    }

    private void BuildCurveRuntimeInfo(List<Segment> segments)
    {
        // Runtime info for determining player progress along track, detecting when they've fallen off and respawning them.
        CurveInfos = new CurveRuntimeInfo[Curves.Length];
        int curveStartSeg = 0;
        for (int i = 0; i < Curves.Length; i++)
        {
            var curve = Curves[i];

            // Find end of curve
            int curveEndSeg = curveStartSeg;
            while (curveEndSeg < segments.Count() && segments[curveEndSeg].CurveIndex <= i)
                curveEndSeg++;

            // Find middle segment for sampling normal.
            // Choose respawn segment. Ideally 2 meters from the start of the curve (so that vehicle is fully above the curve)
            // but no further forward than the middle segment
            int midSeg = (curveStartSeg + curveEndSeg) / 2;
            int respawnSeg = Math.Min(curveStartSeg + Mathf.CeilToInt(2.0f / SegmentLength), midSeg);

            // Calculate normal in road local space
            Vector3 normal = segments[midSeg].GetTransform(0.0f).MultiplyVector(Vector3.up);

            // Calculate respawn point and direction vectors in road local space
            Matrix4x4 respawnTransform = segments[respawnSeg].GetTransform(0.0f);
            Vector3 respawn = respawnTransform.MultiplyPoint(new Vector3(0.0f, RespawnHeight, 0.0f));
            Vector3 respawnForward = respawnTransform.MultiplyVector(Vector3.forward);
            Vector3 respawnUp = respawnTransform.MultiplyVector(Vector3.up);

            // Write curve info in world space
            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            CurveInfos[i] = new CurveRuntimeInfo
            {
                Normal = localToWorld.MultiplyVector(normal).normalized,
                RespawnPosition = localToWorld.MultiplyPoint(respawn),
                RespawnRotation = Quaternion.LookRotation(
                    localToWorld.MultiplyVector(respawnForward).normalized,
                    localToWorld.MultiplyVector(respawnUp).normalized)
            };

            // Setup for next curve
            curveStartSeg = curveEndSeg;
        }
    }

    private IEnumerable<Segment> GetSegments(float segmentLength)
    {
        // Walk along curve in world space
        Vector3 pos = Vector3.zero;
        Vector3 dir = Vector3.forward;

        for (int i = 0; i < Curves.Length; i++)
        {
            Curve c = Curves[i];

            // Note: Y axis angle is relative. So value = 90 will turn 90 degrees to the right.
            // X and Z axes are absolute. So Z=45 will bank the road to 45 degrees by the end of the curve. Z=0 will return it to level etc.
            Vector3 dirDelta = new Vector3(
                Util.LocalAngle(c.Angles.x - dir.x), 
                c.Angles.y, 
                Util.LocalAngle(c.Angles.z - dir.z)
            ) / c.Length * segmentLength;
            Vector3 delta = Vector3.forward * segmentLength;

            // Generate segments
            for (float d = 0.0f; d < c.Length; d += segmentLength)
            {
                Segment segment = new Segment
                {
                    Position = pos,
                    Direction = dir,
                    DirectionDelta = dirDelta,
                    Length = segmentLength,
                    Mesh = c.Mesh,
                    LODGroup = c.LODGroup,
                    SupportIndex = c.SupportIndex,
                    CurveIndex = i
                };
                yield return segment;

                // Move to start of next segment
                pos += segment.SegmentToWorld.MultiplyVector(delta);
                dir += dirDelta;
            }
        }
    }

    private Segment GetSegment(List<Segment> segments, int i)
    {
        if (i < 0) return segments[0];
        if (i < segments.Count) return segments[i];

        // It's likely meshes won't exactly add up to the Z length of the curves, so the last one will overhang.
        // We allow for this by generating a virtual segment extruded from the last segment in the list.
        var lastSeg = segments[segments.Count - 1];
        return new Segment
        {
            Position = lastSeg.Position + lastSeg.SegmentToWorld.MultiplyVector(Vector3.forward * lastSeg.Length * (i - segments.Count)),
            Direction = lastSeg.Direction,
            DirectionDelta = Vector3.zero,
            Length = lastSeg.Length,
            Mesh = lastSeg.Mesh,
            LODGroup = lastSeg.LODGroup,
            CurveIndex = lastSeg.CurveIndex
        };
    }

    [Serializable]
    public class Curve
    {
        public float Length;
        public Vector3 Angles;
        public MeshFilter Mesh;
        public LODGroup LODGroup;
        public int SupportIndex = -1;
        public bool CanRespawn = true;

        public bool IsJump
        {
            get { return Mesh == null && LODGroup == null; }        // A curve is a "jump" if it has no associated meshes
        }

        public bool CanSpawnPlayer
        {
            get { return !IsJump && CanRespawn; }                   
        }
    }

    [Serializable]
    public class CurveRuntimeInfo
    {
        public Vector3 Normal;
        public Vector3 RespawnPosition;
        public Quaternion RespawnRotation;
    }

    [Serializable]
    public class Support
    {
        public Transform Mesh;
        public float Spacing;
        public float HorizontalSpacing;
        public float Radius;
    }

    public enum RoadType
    {
        Game,
        MenuPreview
    }

    private class Segment
    {
        public Vector3 Position;

        public Vector3 Direction;

        public Vector3 DirectionDelta;

        public float Length;

        public MeshFilter Mesh;

        public LODGroup LODGroup;

        public int SupportIndex;

        public int CurveIndex;

        public Matrix4x4 GetTransform(float z)
        {            
            Vector3 posSeg = new Vector3(0.0f, 0.0f, z);                // Position in segment space
            Vector3 posWorld = SegmentToWorld.MultiplyPoint(posSeg);    // Position in world space
            float f = z / Length;                                       // Fractional distance along segment
            Vector3 dir = Direction + f * DirectionDelta;               // Rotation at given distance
            
            return Matrix4x4.Translate(posWorld)                        // Translate to world space
                * Matrix4x4.Rotate(Quaternion.Euler(dir))               // Rotate around (0,0,z) (in segment space)
                * Matrix4x4.Translate(-posSeg);                         // Move (0,0,z) to centre in preparation for rotation
        }

        public Matrix4x4 SegmentToWorld
        {
            get
            {
                return Matrix4x4.Translate(Position) * Matrix4x4.Rotate(Quaternion.Euler(Direction));
            }
        }
    }
}
