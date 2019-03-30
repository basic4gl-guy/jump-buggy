using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The main racetrack component.
/// Manages a collection of RacetrackCurve curves. Warps meshes around them to create 
/// the visual and physical racetrack model.
/// </summary>
[ExecuteInEditMode]
public class Racetrack : MonoBehaviour {

    private const int MaxSpacingGroups = 16;

    [Header("Parameters")]
    public float SegmentLength = 0.25f;
    public float RespawnHeight = 0.75f;
    public RacetrackMeshOverrunOption MeshOverrun = RacetrackMeshOverrunOption.Extrapolate;
    public float LoopYOffset = -0.001f;

    [Header("UI settings")]
    public bool ShowManipulationHandles = true;
    public bool ShowOnScreenButtons = true;

    /// <summary>
    /// Runtime information for each curve. Used by RacetrackProgressTracker.
    /// </summary>
    [HideInInspector]
    public CurveRuntimeInfo[] CurveInfos;

    /// <summary>
    /// Track building state at the start of each curve.
    /// Allows rebuilding meshes for just a single curve or sub-range.
    /// </summary>
    [HideInInspector]
    public BuildMeshesState[] CurveBuildMeshState;

    // Working

    /// <summary>
    /// Singleton instance
    /// </summary>
    public static Racetrack Instance;

    /// <summary>
    /// Host services
    /// </summary>
    private IRacetrackHostServices hostServices = RuntimeRacetrackHostServices.Instance;

    // Segments calculaged field
    [SerializeField]
    [HideInInspector]
    private int segmentsRequiredVersion = 1;

    private int segmentsVersion = 0;

    private int segmentsHighestVersion;

    /// <summary>
    /// Curves processed into a set of small linear segments
    /// </summary>
    private List<Segment> _segments;

    public List<Segment> Segments
    {
        get
        {
            // Regenerate segments if necessary
            if (segmentsVersion != segmentsRequiredVersion || _segments == null)
            {
                UpdateSegments();
                segmentsVersion = segmentsRequiredVersion;
                if (segmentsVersion > segmentsHighestVersion)
                    segmentsHighestVersion = segmentsVersion;
            }

            return _segments;
        }
    }

    public void InvalidateSegments()
    {
        // Generate a new version number
        hostServices.ObjectChanging(this);
        segmentsRequiredVersion = segmentsHighestVersion + 1;
    }

    private void Awake()
    {
        Instance = this;
        segmentsHighestVersion = segmentsRequiredVersion;
    }

    void Start()
    {
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Recreate all meshes
    /// </summary>
    public void CreateMeshes()
    {
        CreateMeshes(0, Curves.Count);
    }

    /// <summary>
    /// Recreate a range of meshes
    /// </summary>
    /// <param name="startCurveIndex">INCLUSIVE index of first curve</param>
    /// <param name="endCurveIndex">EXCLUSIVE index of last curve</param>
    public void CreateMeshes(int startCurveIndex, int endCurveIndex)
    {
        // Delete any previous meshes
        DeleteMeshes(startCurveIndex, endCurveIndex);

        // Create new meshes
        RepositionCurves();
        BuildMeshes(startCurveIndex, endCurveIndex);
    }

    public RacetrackCurve CreateCircuit()
    {
        // Link the last curve to the start of the track
        var curves = Curves;
        if (curves.Count < 1)
            throw new ApplicationException("Must have at least 1 curve to create a circuit");

        RacetrackCurve newCurve = null;
        if (MeshOverrun != RacetrackMeshOverrunOption.Loop)
        {
            // Switch to looped mode
            hostServices.ObjectChanging(this);
            MeshOverrun = RacetrackMeshOverrunOption.Loop;

            // Create a new curve to close the loop
            var obj = new GameObject();
            hostServices.ObjectCreated(obj);
            obj.transform.parent = transform;
            obj.name = "Curve";
            obj.isStatic = gameObject.isStatic;
            newCurve = obj.AddComponent<RacetrackCurve>();
            newCurve.Index = curves.Count;

            // Segments need recalculating
            InvalidateSegments();

            // Update curves array
            curves = Curves;

            // Ensure curves are positioned correctly
            RepositionCurves();
        }

        // Join last curve to first
        var first = curves.First();
        var last = curves.Last();
        last.Type = RacetrackCurveType.Bezier;
        last.EndPosition = first.transform.localPosition;

        // Line up angles
        var yAng = curves.Sum(c => c.Angles.y) - last.Angles.y;
        float lastCurveX = 0.0f;
        float lastCurveY = RacetrackUtil.LocalAngle(0.0f - yAng);
        float lastCurveZ = 0.0f;
        last.Angles = new Vector3(lastCurveX, lastCurveY, lastCurveZ);

        InvalidateSegments();

        // Update last curve meshes
        CreateMeshes(curves.Count - 2, curves.Count);

        // Return new curve created (or null if existing curve used)
        return newCurve;
    }

    /// <summary>
    /// Clear mesh templates from each curve
    /// </summary>
    public void RemoveTemplates()
    {
        var curves = Curves;
        foreach (var curve in curves)
        {
            hostServices.ObjectChanging(curve);
            curve.Template = null;
        }
        DeleteMeshes();
    }

    /// <summary>
    /// Add a curve to the end of the track.
    /// Copies previous curve settings. Automatically builds meshes.
    /// </summary>
    /// <returns>The new curve</returns>
    public RacetrackCurve AddCurve()
    {
        var curves = Curves;
        var lastCurve = curves.LastOrDefault();

        // Add curve to end of list
        var obj = new GameObject();
        hostServices.ObjectCreated(obj);
        obj.transform.parent = transform;
        obj.name = "Curve";
        obj.isStatic = gameObject.isStatic;
        var curve = obj.AddComponent<RacetrackCurve>();
        curve.Index = curves.Count;

        InvalidateSegments();

        ConfigureNewCurve(lastCurve, curve);

        // Switch back to extrapolate overrun mode
        hostServices.ObjectChanging(this);
        MeshOverrun = RacetrackMeshOverrunOption.Extrapolate;

        // Create curve meshes
        ScheduleCreateMeshes(curve, false);

        return curve;
    }

    public RacetrackCurve InsertCurveAfter(int index)
    {
        var curves = Curves;
        var lastCurve = curves[index];

        // Create new curve
        var obj = new GameObject();
        hostServices.ObjectCreated(obj);
        obj.transform.parent = transform;
        obj.name = "Curve";
        obj.isStatic = gameObject.isStatic;
        var curve = obj.AddComponent<RacetrackCurve>();

        InvalidateSegments();

        // Position immediately after previous curve
        curve.Index = index + 1;
        hostServices.SetTransformParent(curve.transform, curve.transform.parent);       // This seems to be required to get undo support for SetSiblingIndex
        obj.transform.SetSiblingIndex(curve.Index);

        ConfigureNewCurve(lastCurve, curve);

        // Create curve meshes
        ScheduleCreateMeshes(index - 1, curve.Index + 2);

        return curve;
    }

    private void ConfigureNewCurve(RacetrackCurve lastCurve, RacetrackCurve curve)
    {
        // Copy values from last curve
        if (lastCurve != null)
        {
            curve.Length = lastCurve.Length;
            curve.Angles = lastCurve.Angles;
            curve.IsJump = lastCurve.IsJump;
            curve.CanRespawn = lastCurve.CanRespawn;
            if (lastCurve.Type == RacetrackCurveType.Bezier)
            {
                // Create a straight arc curve to calculate the end position.
                // (Will then switch to bezier after)
                curve.Type = RacetrackCurveType.Arc;
                curve.Angles.y = 0.0f;
                curve.Length = 50;

                // Update segments will calculate EndPosition
                var temp = Segments;
            }
            curve.Type = lastCurve.Type;
        }
    }

    /// <summary>
    /// Delete meshes from all curves
    /// </summary>
    public void DeleteMeshes()
    {
        DeleteMeshes(0, Curves.Count);
    }

    /// <summary>
    /// Delete meshes from a range of curves
    /// </summary>
    /// <param name="startCurveIndex">INCLUSIVE index of first curve</param>
    /// <param name="endCurveIndex">EXCLUSIVE index of last curve</param>
    public void DeleteMeshes(int startCurveIndex, int endCurveIndex)
    {
        // Find generated meshes.
        // These have a RacetrackTemplateCopy component
        var children = Curves
            .Where(c => c.Index >= startCurveIndex && c.Index < endCurveIndex)
            .SelectMany(c => c.gameObject.GetComponentsInChildren<RacetrackTemplateCopy>())
            .ToList();

        // Delete them
        foreach (var child in children)
            hostServices.DestroyObject(child.gameObject);
    }

    /// <summary>
    /// Calculate each curve's position.
    /// Also builds the curve runtime information array.
    /// </summary>
    private void RepositionCurves()
    {
        // Position curve objects at the start of their curves
        var curves = Curves;
        hostServices.ObjectChanging(this);
        CurveInfos = new CurveRuntimeInfo[curves.Count];
        float curveZOffset = 0.0f;
        int index = 0;
        foreach (var curve in curves)
        {
            // Position curve at first segment
            int segIndex = Mathf.FloorToInt(curveZOffset / SegmentLength);
            var seg = GetSegment(segIndex);
            hostServices.ObjectChanging(curve.transform);
            GetSegmentTransform(seg, curve.transform);
            hostServices.ObjectChanging(curve);
            curve.Index = index;

            // Calculate curve runtime info
            var info = new CurveRuntimeInfo();
            info.IsJump = curve.IsJump;
            info.CanRespawn = curve.CanRespawn;
            info.zOffset = curveZOffset;

            // Calculate normal in track space
            int endSegIndex = Mathf.FloorToInt((curveZOffset + curve.Length) / SegmentLength);
            int midSegIndex = (segIndex + endSegIndex) / 2;
            info.Normal = GetSegment(midSegIndex).GetSegmentToTrack().MultiplyVector(Vector3.up);

            // Calculate respawn point and direction vectors in track space
            int respawnSeg = Math.Min(segIndex + Mathf.CeilToInt(2.0f / SegmentLength), midSegIndex);
            Matrix4x4 respawnTransform = GetSegment(respawnSeg).GetSegmentToTrack();
            info.RespawnPosition = respawnTransform.MultiplyPoint(Vector3.up * RespawnHeight);
            info.RespawnRotation = Quaternion.LookRotation(
                respawnTransform.MultiplyVector(Vector3.forward).normalized,
                respawnTransform.MultiplyVector(Vector3.up).normalized);

            CurveInfos[index] = info;

            // Move on to next curve
            index++;
            curveZOffset += curve.Length;
        }
    }

    ///// <summary>
    ///// Rebuild segments array
    ///// </summary>
    //public void UpdateSegments2()
    //{
    //    segments = GenerateSegments();
    //}

    /// <summary>
    /// Get segment array for single curve.
    /// Used by RacetrackCurveEditor to display curves in scene GUI
    /// </summary>
    /// <param name="curve">The racetrack curve</param>
    /// <returns>Enumerable of segments for the specified curve</returns>
    public IEnumerable<Segment> GetCurveSegments(RacetrackCurve curve)
    {
        // Find start of curve
        int i = 0;
        while (i < Segments.Count && Segments[i].Curve.Index < curve.Index)
            i++;

        // Return curve segments
        while (i < Segments.Count && Segments[i].Curve.Index == curve.Index)
        {
            yield return Segments[i];
            i++;
        }
    }

    /// <summary>
    /// Build meshes for all curves
    /// </summary>
    private void BuildMeshes()
    {
        BuildMeshes(0, Curves.Count);
    }

    /// <summary>
    /// Build meshes for range of curves
    /// </summary>
    /// <param name="startCurveIndex">INCLUSIVE start curve index</param>
    /// <param name="endCurveIndex">EXCLUSIVE end curve index</param>
    private void BuildMeshes(int startCurveIndex, int endCurveIndex)
    {
        if (!Segments.Any()) return;
        var curves = Curves;

        // Clamp curve range
        if (startCurveIndex < 0)
            startCurveIndex = 0;
        if (endCurveIndex > curves.Count)
            endCurveIndex = curves.Count;

        // Create/update build mesh state array in parallel
        hostServices.ObjectChanging(this);
        if (CurveBuildMeshState == null)
            CurveBuildMeshState = new BuildMeshesState[curves.Count];
        else
            Array.Resize(ref CurveBuildMeshState, curves.Count);

        // Fetch state from previous curve and unpack.
        BuildMeshesState state = startCurveIndex > 0 ? CurveBuildMeshState[startCurveIndex - 1] : new BuildMeshesState();
        var spacingGroupStates = state.SpacingGroups.Select(g => new SpacingGroupState
        {
            IsActive = g.IsActive,
            ZOffset = g.ZOffset
        }).ToArray();
        float meshZOffset = state.MeshZOffset;
        RacetrackMeshTemplate template = state.Template;

        // Work down the curve. Add meshes as we go.
        var totalLength = Segments.Count * SegmentLength;
        var curveIndex = startCurveIndex;
        while (meshZOffset < totalLength)
        {
            // Find segment where mesh starts
            int segIndex = Mathf.FloorToInt(meshZOffset / SegmentLength);
            var seg = Segments[segIndex];
            var curve = seg.Curve;

            // Store state at end of each curve
            while (seg.Curve.Index > curveIndex) {
                CurveBuildMeshState[curveIndex] = new BuildMeshesState
                {
                    SpacingGroups = spacingGroupStates.Select(s => new SpacingGroupBaseState
                    {
                        IsActive = s.IsActive,
                        ZOffset = s.ZOffset
                    }).ToArray(),
                    Template = template,
                    MeshZOffset = meshZOffset
                };
                curveIndex++;
            }

            // Stop if end curve reached
            if (curve.Index >= endCurveIndex) break;

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
                hostServices.ObjectCreated(templateCopyObj);
                templateCopyObj.transform.parent = curve.transform;
                templateCopyObj.isStatic = gameObject.isStatic;
                templateCopyObj.name = curve.name + " > " + template.name;
                GetSegmentTransform(seg, templateCopyObj.transform);

                // Add template copy component
                var templateCopy = templateCopyObj.AddComponent<RacetrackTemplateCopy>();
                templateCopy.Template = template;

                // Pass 1: Generate continuous meshes
                bool isFirstMesh = true;
                float mainMeshLength = 0.0f;
                foreach (var subtree in template.FindSubtrees<RacetrackContinuous>())
                {
                    // Duplicate subtree
                    var subtreeCopy = Instantiate(subtree);
                    subtreeCopy.transform.parent = templateCopyObj.transform;
                    subtreeCopy.gameObject.isStatic = gameObject.isStatic;
                    subtreeCopy.name += " Continuous";
                    GetSegmentTransform(seg, subtreeCopy.transform);

                    // Need to take into account relative position of continuous subtree within template object
                    // Note: The subtree's transformation is effectively cancelled out, however we multiply back in the scale (lossyScale)
                    // as this allows setting the scale on the template object itself, which is useful.
                    // (Otherwise you would have to create a sub-object in order to perform scaling)
                    Matrix4x4 templateFromSubtree = Matrix4x4.Scale(template.transform.lossyScale) * template.transform.localToWorldMatrix.inverse * subtree.transform.localToWorldMatrix;

                    // Clone and warp displayed meshes
                    var meshFilters = subtreeCopy.GetComponentsInChildren<MeshFilter>();
                    foreach (var mf in meshFilters)
                    {
                        mf.sharedMesh = CloneMesh(mf.sharedMesh);
                        Matrix4x4 subtreeFromMesh = subtreeCopy.transform.localToWorldMatrix.inverse * mf.transform.localToWorldMatrix;
                        Matrix4x4 templateFromMesh = templateFromSubtree * subtreeFromMesh;
                        Matrix4x4 meshFromWorld = mf.transform.localToWorldMatrix.inverse;
                        float meshLength = WarpMeshToCurves(mf.sharedMesh, meshZOffset, templateFromMesh, meshFromWorld);
#if UNITY_EDITOR && !UNITY_STANDALONE
                        Unwrapping.GenerateSecondaryUVSet(mf.sharedMesh);
#endif

                        // First continuous mesh is considered to be the main track surface,
                        // and determines the length of the template copy
                        if (isFirstMesh)
                        {
                            var surface = mf.gameObject.AddComponent<RacetrackSurface>();
                            int endSegIndex = Mathf.FloorToInt((meshZOffset + meshLength) / SegmentLength - 0.00001f);
                            Segment endSeg = Segments[Math.Min(endSegIndex, Segments.Count - 1)];
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
                        if (mc.sharedMesh == null) continue;
                        mc.sharedMesh = CloneMesh(mc.sharedMesh);
                        Matrix4x4 subtreeFromMesh = subtreeCopy.transform.localToWorldMatrix.inverse * mc.transform.localToWorldMatrix;
                        Matrix4x4 templateFromMesh = templateFromSubtree * subtreeFromMesh;
                        Matrix4x4 meshFromWorld = mc.transform.localToWorldMatrix.inverse;
                        float meshLength = WarpMeshToCurves(mc.sharedMesh, meshZOffset, templateFromMesh, meshFromWorld);
                    }
                };

                // Pass 2: Generate spaced meshes
                foreach (var subtree in template.FindSubtrees<RacetrackSpaced>())
                {
                    // Search up parent chain for spacing group
                    var spacingGroup = subtree.GetComponentsInParent<RacetrackSpacingGroup>(true).FirstOrDefault();
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

                        // Check track angle restrictions
                        float trackXAngle = Mathf.Abs(RacetrackUtil.LocalAngle(spaceSeg.Direction.x));
                        float trackZAngle = Mathf.Abs(RacetrackUtil.LocalAngle(spaceSeg.Direction.z));
                        if (trackXAngle > subtree.MaxXAngle || trackZAngle > subtree.MaxZAngle)
                        {
                            groupState.ZOffsetThisTemplate += spacingGroup.Spacing;                         // Outside angle restrictions. Skip creating this one
                            continue;                   
                        }

                        // Duplicate subtree
                        var subtreeCopy = Instantiate(subtree);
                        subtreeCopy.transform.parent = templateCopyObj.transform;
                        subtreeCopy.gameObject.isStatic = gameObject.isStatic;
                        subtreeCopy.name += " Spacing group " + spacingGroup.Index;

                        // Calculate local to track tranform matrix for subtree.
                        Matrix4x4 templateFromSubtree = Matrix4x4.Scale(template.transform.lossyScale) * template.transform.localToWorldMatrix.inverse * subtree.transform.localToWorldMatrix;
                        Matrix4x4 trackFromSubtree = 
                              spaceSeg.GetSegmentToTrack(spaceZOffset - spaceSegIndex * SegmentLength)      // Segment -> Track
                            * templateFromSubtree;                                                          // Subtree -> Segment

                        if (subtree.IsVertical)
                        {
                            // Disassemble matrix
                            Vector3 basisX = RacetrackUtil.ToVector3(trackFromSubtree.GetRow(0));
                            Vector3 basisY = RacetrackUtil.ToVector3(trackFromSubtree.GetRow(1));
                            Vector3 basisZ = RacetrackUtil.ToVector3(trackFromSubtree.GetRow(2));

                            // Align Y vector with global Y axis
                            basisY = Vector3.up * basisY.magnitude;

                            // Cross product to get X and Z (assuming matrix is orthogonal)
                            basisX = Vector3.Cross(basisY, basisZ).normalized * basisX.magnitude;
                            basisZ = Vector3.Cross(basisX, basisY).normalized * basisZ.magnitude;

                            // Recompose matrix
                            trackFromSubtree.SetColumn(0, RacetrackUtil.ToVector4(basisX));
                            trackFromSubtree.SetColumn(1, RacetrackUtil.ToVector4(basisY));
                            trackFromSubtree.SetColumn(2, RacetrackUtil.ToVector4(basisZ));
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

    /// <summary>
    /// Warp a single mesh along the racetrack curves
    /// </summary>
    /// <param name="mesh">The mesh to warp. Vertex, normal and tangent arrays will be cloned and modified.</param>
    /// <param name="meshZOffset">The distance along all curves where mesh begins</param>
    /// <param name="meshToTemplate">Transformation from mesh space to mesh template space</param>
    /// <param name="worldToMesh">Transformation from world space to mesh space</param>
    /// <returns>Length of mesh in template space</returns>
    private float WarpMeshToCurves(Mesh mesh, float meshZOffset, Matrix4x4 meshToTemplate, Matrix4x4 worldToMesh)
    {
        // Find length of mesh            
        float meshMaxZ = mesh.vertices.Max(v => meshToTemplate.MultiplyPoint(v).z);
        float meshMinZ = mesh.vertices.Min(v => meshToTemplate.MultiplyPoint(v).z);
        float meshLength = meshMaxZ - meshMinZ;

        // Lookup first segment
        int segIndex = Mathf.FloorToInt(meshZOffset / SegmentLength);
        Segment seg = Segments[segIndex];

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

    /// <summary>
    /// Get transformation for object positioned at a given segment
    /// </summary>
    /// <param name="seg">The segment</param>
    /// <param name="dstTransform">The transform to update</param>
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

    public void UpdateSegments()
    {
        // Re-index curves
        var curves = Curves;
        for (int i = 0; i < curves.Count; i++)
        {
            hostServices.ObjectChanging(curves[i]);
            curves[i].Index = i;
        }

        // Generate segments without deltas
        _segments = GenerateSegmentsEnumerable().ToList();

        // Calculate direction deltas
        for (int i = 0; i < _segments.Count - 1; i++)
            _segments[i].DirectionDelta = _segments[i + 1].Direction - _segments[i].Direction;
    }

    /// <summary>
    /// Generate segments from track curves
    /// </summary>
    /// <returns>Enumerable of segments for all curves</returns>
    private IEnumerable<Segment> GenerateSegmentsEnumerable()
    {
        // Find curves. Must be in immediate children.
        var curves = Curves;

        // Walk along curve in track space.
        Vector3 pos = Vector3.zero;
        Vector3 dir = Vector3.zero;
        Vector3 segPosDelta = Vector3.zero;

        Vector3 dirDelta = Vector3.zero;
        Vector3 posDelta = Vector3.forward * SegmentLength;
        foreach (var curve in curves)
        {
            // Calculate direction at the end of the curve
            Vector3 endDir = new Vector3(curve.Angles.x, dir.y + curve.Angles.y, curve.Angles.z);

            switch (curve.Type)
            {
                case RacetrackCurveType.Arc:
                    {
                        // Find delta to add to curve each segment
                        dirDelta = new Vector3(
                            RacetrackUtil.LocalAngle(curve.Angles.x - dir.x),
                            curve.Angles.y,
                            RacetrackUtil.LocalAngle(curve.Angles.z - dir.z)
                        ) / curve.Length * SegmentLength;

                        // Generate segments
                        for (float d = 0.0f; d < curve.Length; d += SegmentLength)
                        {
                            segPosDelta = Matrix4x4.Rotate(Quaternion.Euler(dir)).MultiplyVector(posDelta);

                            var segment = new Segment
                            {
                                Position = pos,
                                PositionDelta = segPosDelta,
                                Direction = dir,
                                DirectionDelta = dirDelta,
                                Length = SegmentLength,
                                Curve = curve
                            };
                            yield return segment;

                            // Advance to start of next segment
                            pos += segPosDelta;
                            dir += dirDelta;
                        }
                    }

                    hostServices.ObjectChanging(curve);
                    curve.EndPosition = pos;

                    break;

                case RacetrackCurveType.Bezier:
                    {
                        // Calculate end position
                        Vector3 endPos = curve.EndPosition;

                        // Calculate start and end tangental vectors
                        Vector3 startTangent = Matrix4x4.Rotate(Quaternion.Euler(dir)).MultiplyVector(Vector3.forward);
                        Vector3 endTangent = Matrix4x4.Rotate(Quaternion.Euler(endDir)).MultiplyVector(Vector3.forward);
                        float separationDist = (endPos - pos).magnitude;
                        float startTangentLength = separationDist * curve.StartControlPtDist;
                        float endTangentLength = separationDist * curve.EndControlPtDist;

                        // Create bezier curve from control points
                        Bezier bezier = new Bezier(
                            pos,
                            pos + startTangent * startTangentLength,
                            endPos - endTangent * endTangentLength,
                            endPos);

                        // Build distance lookup with t values for each segment length
                        float length;
                        var lookup = bezier.BuildDistanceLookup(0.0001f, SegmentLength, out length);
                        hostServices.ObjectChanging(curve);
                        curve.Length = length;

                        // Generate curve segments along bezier
                        float dirZDelta = (curve.Angles.z - dir.z) / length * SegmentLength;
                        foreach (var t in lookup)
                        {
                            // Get position and tangent
                            pos = bezier.GetPt(t);
                            Vector3 tangent = bezier.GetTangent(t);
                            segPosDelta = tangent * SegmentLength;

                            // Calculate corresponding euler angles
                            dir.y = Mathf.Atan2(segPosDelta.x, segPosDelta.z) * Mathf.Rad2Deg;
                            float xz = Mathf.Sqrt(segPosDelta.x * segPosDelta.x + segPosDelta.z * segPosDelta.z);
                            dir.x = -Mathf.Atan2(segPosDelta.y, xz) * Mathf.Rad2Deg;
                            dir.z += dirZDelta;

                            var segment = new Segment
                            {
                                Position = pos,
                                PositionDelta = segPosDelta,
                                Direction = dir,
                                DirectionDelta = Vector3.zero,  // TODO!
                                Length = SegmentLength,
                                Curve = curve
                            };
                            yield return segment;
                        }

                        // Update dir to end-of-curve direction.
                        // Otherwise it will be the direction based on the last sampled bezier tangent,
                        // which may not exactly align.
                        dir = endDir;
                    }
                    break;
            }
        }

        // Return final segment
        yield return new Segment
        {
            Position = pos,
            PositionDelta = segPosDelta,
            Direction = dir,
            DirectionDelta = dirDelta,
            Length = SegmentLength,
            Curve = curves.LastOrDefault()
        };
    }

    /// <summary>
    /// Get segment by index. Generate "virtual" segments to handle array overrun.
    /// </summary>
    /// <param name="i">Index into segments array</param>
    /// <returns>Corresponding segment.</returns>
    public Segment GetSegment(int i)
    {
        if (i < 0) return Segments[0];
        if (i < Segments.Count) return Segments[i];

        // It's likely meshes won't exactly add up to the Z length of the curves, so the last one will overhang.
        // Handle this
        switch (MeshOverrun)
        {
            case RacetrackMeshOverrunOption.Extrapolate:
                // We allow for this by generating a virtual segment extruded from the last segment in the list.
                var lastSeg = Segments.Last();
                return new Segment
                {
                    Position = lastSeg.Position + lastSeg.GetSegmentToTrack(0.0f).MultiplyVector(Vector3.forward * lastSeg.Length * (i - Segments.Count)),
                    PositionDelta = lastSeg.PositionDelta,
                    Direction = lastSeg.Direction,
                    DirectionDelta = Vector3.zero,
                    Length = lastSeg.Length,
                    Curve = lastSeg.Curve
                };

            case RacetrackMeshOverrunOption.Loop:
                var loopedSeg = Segments[i % Segments.Count];

                // Create a copy translated down slightly.
                // If meshes line up exactly it can lead to ugly Z fighting.
                return new Segment {
                    Position = loopedSeg.Position + new Vector3(0.0f, LoopYOffset, 0.0f),
                    PositionDelta = loopedSeg.PositionDelta,
                    Direction = loopedSeg.Direction,
                    DirectionDelta = loopedSeg.DirectionDelta,
                    Length = loopedSeg.Length,
                    Curve = Segments.Last().Curve
                };


            default:
                throw new ArgumentOutOfRangeException();            // This should never happen
        }
    }

    private List<RacetrackCurve> cachedCurves;

    /// <summary>
    /// Get list of curves in order.
    /// </summary>
    /// <remarks>
    /// Curve objects are immediate child objects with the RacetrackCurve component.
    /// (Typically they are added with the "Add Curve" editor buttons, rather than 
    /// created manually).
    /// </remarks>
    public List<RacetrackCurve> Curves
    {
        get
        {
            // Assume at runtime that the curves will not be modified, and we can cache them in a local field.
            // When game is not running (i.e. in editor) we always recalculate the curves
            if (cachedCurves == null || !Application.IsPlaying(gameObject))
                cachedCurves = Enumerable.Range(0, gameObject.transform.childCount)
                            .Select(i => gameObject.transform.GetChild(i).GetComponent<RacetrackCurve>())
                            .Where(c => c != null)
                            .ToList();
            return cachedCurves;
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

    private bool scheduleCreate;
    private int scheduleFrom;
    private int scheduleTo;
    private int scheduleBezier;

    public void ScheduleCreateMeshes(RacetrackCurve curve, bool isSingleCurveOnly)
    {
        var curves = Curves;
        scheduleCreate = true;
        scheduleFrom = curve.Index - 1;
        scheduleTo = isSingleCurveOnly ? curve.Index + 2 : curves.Count;

        // After updating a single curve, also update the next bezier curve
        scheduleBezier = -1;
        if (isSingleCurveOnly)
        {
            var nextBezier = curves.Skip(curve.Index + 1).FirstOrDefault(c => c.Type == RacetrackCurveType.Bezier);
            if (nextBezier)
                scheduleBezier = nextBezier.Index;
        }
    }

    private void ScheduleCreateMeshes(int fromIndex, int toIndex)
    {
        scheduleCreate = true;
        scheduleFrom = fromIndex;
        scheduleTo = toIndex;
        scheduleBezier = -1;
    }

    public void CreateScheduledMeshes()
    {
        // Rebuild scheduled curve(s)
        if (scheduleCreate)
        {
            try
            {
                CreateMeshes(scheduleFrom, scheduleTo);
                if (scheduleBezier >= 0)
                {
                    int bFrom = scheduleBezier - 1;
                    int bTo = scheduleBezier + 2;
                    if (bFrom < scheduleTo)         // Don't create meshes twice if ranges overlap
                        bFrom = scheduleTo;
                    if (bTo > bFrom)
                        CreateMeshes(bFrom, bTo);
                }
            }
            finally
            {
                scheduleCreate = false;
            }
        }
    }

    public void SetHostServices(IRacetrackHostServices services)
    {
        hostServices = services;
    }

    /// <summary>
    /// Track curves are broken down into tiny straight segments.
    /// </summary>
    public class Segment
    {
        public Vector3 Position;
        public Vector3 PositionDelta;       // Added to position to get next segment's position
        public Vector3 Direction;           // Direction as Euler angles
        public Vector3 DirectionDelta;      // Added to Direction to get next segment's direction (and used to lerp between them)
        public float Length;                // Copy of Racetrack.SegmentLength for convenience
        public RacetrackCurve Curve;        // Curve to which segment belongs

        /// <summary>
        /// Get matrix converting from segment space to racetrack space
        /// </summary>
        /// <param name="segZ">Z distance along segment. [0, Length]</param>
        /// <returns>A transformation matrix</returns>
        public Matrix4x4 GetSegmentToTrack(float segZ = 0.0f)
        {
            float f = segZ / Length;                                                            // Fractional distance along segment
            float adjZDir = Direction.z + DirectionDelta.z * f;                                 // Adjust Z axis rotation based on distance down segment
            Vector3 adjDir = new Vector3(Direction.x, Direction.y, adjZDir);
            return Matrix4x4.Translate(Position) * Matrix4x4.Rotate(Quaternion.Euler(adjDir));
        }
    }

    /// <summary>
    /// Spacing group persisted state.
    /// See RacetrackSpacingGroup class.
    /// </summary>
    [Serializable]
    public class SpacingGroupBaseState
    {
        public bool IsActive = false;
        public float ZOffset = 0.0f;
    }

    /// <summary>
    /// Spacing group working state
    /// </summary>
    public class SpacingGroupState : SpacingGroupBaseState
    {
        public bool IsActiveThisTemplate = false;
        public float ZOffsetThisTemplate = 0.0f;
    }

    /// <summary>
    /// Runtime information attached to a curve.
    /// Can be used to track player progress along racetrack, and respawn after crashes.
    /// See RacetracProgressTracker
    /// </summary>
    [Serializable]
    public class CurveRuntimeInfo
    {
        public Vector3 Normal;                  // Upward normal in middle of curve
        public Vector3 RespawnPosition;
        public Quaternion RespawnRotation;
        public bool IsJump;                     // Copy of RacetrackCurve.IsJump
        public bool CanRespawn;                 // Copy of RacetrackCurve.CanRespawn
        public float zOffset;
    }

    /// <summary>
    /// State for building meshes.
    /// </summary>
    /// <remarks>
    /// A copy of this state for each curve is stored in the Racetrack.CurveBuildMeshState array,
    /// so that meshes for a single curve (or range) can be built without having to rebuild the 
    /// whole track.
    /// </remarks>
    [Serializable]
    public class BuildMeshesState
    {
        public SpacingGroupBaseState[] SpacingGroups;
        public float MeshZOffset = 0.0f;
        public RacetrackMeshTemplate Template = null;

        public BuildMeshesState()
        {
            // Populate SpacingGroups array
            SpacingGroups = new SpacingGroupBaseState[MaxSpacingGroups];
            for (int i = 0; i < SpacingGroups.Length; i++)
                SpacingGroups[i] = new SpacingGroupBaseState();
        }
    }
}

public enum RacetrackMeshOverrunOption
{
    Extrapolate,
    Loop
}

/// <summary>
/// Services from the hosting environment.
/// Injected into Racetrack objects to provide access to editor functionality, particularly Undo tracking.
/// A cut down default implementation is used at runtime.
/// </summary>
public interface IRacetrackHostServices
{
    /// <summary>
    /// Handle newly created object
    /// </summary>
    void ObjectCreated(UnityEngine.Object o);

    /// <summary>
    /// Destroy a game object
    /// </summary>
    void DestroyObject(UnityEngine.Object o);

    /// <summary>
    /// An object is (/may be) about to change
    /// </summary>
    void ObjectChanging(UnityEngine.Object o);

    /// <summary>
    /// Set transformation's parent
    /// </summary>
    void SetTransformParent(Transform transform, Transform parent);
}

/// <summary>
/// Default runtime implementation (no Undo tracking etc)
/// </summary>
public sealed class RuntimeRacetrackHostServices : IRacetrackHostServices
{
    public static readonly RuntimeRacetrackHostServices Instance = new RuntimeRacetrackHostServices();

    private RuntimeRacetrackHostServices() { }

    public void DestroyObject(UnityEngine.Object o)
    {
        if (Application.isEditor)
            UnityEngine.Object.DestroyImmediate(o);
        else
            UnityEngine.Object.Destroy(o);
    }

    public void ObjectChanging(UnityEngine.Object o)
    {
    }

    public void ObjectCreated(UnityEngine.Object o)
    {
    }

    public void SetTransformParent(Transform transform, Transform parent)
    {
        transform.parent = parent;
    }
}