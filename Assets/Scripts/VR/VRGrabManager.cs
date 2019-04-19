using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VRGrabManager : MonoBehaviour
{
    // Loose singleton implementation
    public static VRGrabManager Instance { get; private set; }

    // Objects that can be grabbed
    private readonly List<VRGrabbable> grabbableObjects = new List<VRGrabbable>();

    // Objects that are currently being grabbed
    private readonly List<GrabbedObject> grabbedObjects = new List<GrabbedObject>();

    private void Awake()
    {
        Instance = this;
        grabbedObjects.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void FixedUpdate()
    {
        foreach (var obj in grabbedObjects)
        {
            // Get drag point info
            var movedPoints = obj.Grabs.Select(g => new VRMovedGrabPoint
            {
                // TODO: Include grabber?
                InitialPt = g.Pt,
                CurrentPt = GetGrabPt(g.Grabber, obj.Object)
            }).ToList();

            // Notify dragged object
            obj.Object.Moved(movedPoints);
        }
    }

    public void RegisterGrabbable(VRGrabbable grabbable)
    {
        if (!grabbableObjects.Contains(grabbable))
            grabbableObjects.Add(grabbable);
    }

    public void UnregisterGrabbable(VRGrabbable grabbable)
    {
        grabbableObjects.Remove(grabbable);
        grabbedObjects.RemoveAll(o => o.Object == grabbable);

        // TODO: Notify grabber of release?
    }

    public bool BeginGrab(VRGrabber grabber)
    {
        // TODO: Grab point offset?
        Vector3 worldGrabPoint = grabber.transform.position;
        Quaternion worldGrabRotation = grabber.transform.rotation;

        // Query each grabbable object.
        // Find closest grabbable one
        VRGrabbable nearestObject = null;
        VRGrabPoint nearestPt = null;
        float nearestDist = 0.0f;
        foreach (var obj in grabbableObjects)
        {
            // TODO: Handle already grabbed objects, based on allowed grab types

            VRGrabPoint pt = GetGrabPt(worldGrabPoint, worldGrabRotation, obj);

            float dist;
            if (obj.CanGrab(pt, out dist))
            {
                if (nearestObject == null || dist < nearestDist)
                {
                    nearestObject = obj;
                    nearestPt = pt;
                    nearestDist = dist;
                }
            }
        }

        // Found object to grab?
        if (nearestObject == null)
            return false;

        // Find grabbed object, or create new one
        var grabbedObj = grabbedObjects.FirstOrDefault(o => o.Object == nearestObject);
        if (grabbedObj == null)
        {
            grabbedObj = new GrabbedObject { Object = nearestObject };
            grabbedObjects.Add(grabbedObj);
        }

        grabbedObj.Grabs.Add(new Grab
        {
            Grabber = grabber,
            Pt = nearestPt
        });

        Debug.LogFormat("{0} ({1}) grabbed {2} ({3} grabs). {4} objects are grabbed", grabber.name, grabber.Controller, nearestObject.name, grabbedObj.Grabs.Count, grabbedObjects.Count);

        return true;
    }

    public void EndGrab(VRGrabber grabber)
    {
        // Remove grabber from all grabbed objects
        foreach (var obj in grabbedObjects)
        {
            if (obj.Grabs.Any(g => g.Grabber == grabber))
            {
                obj.Grabs.RemoveAll(g => g.Grabber == grabber);

                // Recalculate remaining grab points if required
                if (obj.Object.RecalcRemainingGrabPtsOnRelease)
                    foreach (var grab in obj.Grabs)
                        grab.Pt = GetGrabPt(grab.Grabber, obj.Object);
            }
        }

        // Remove grabbed objects that no longer have any grabs
        // TODO: Notify?
        grabbedObjects.RemoveAll(o => !o.Grabs.Any());

        Debug.LogFormat("{0} ({1}) released. {2} objects are grabbed", grabber.name, grabber.Controller, grabbedObjects.Count);
    }

    private static VRGrabPoint GetGrabPt(VRGrabber grabber, VRGrabbable obj)
    {
        return GetGrabPt(grabber.transform.position, grabber.transform.rotation, obj);
    }

    private static VRGrabPoint GetGrabPt(Vector3 worldGrabPoint, Quaternion worldGrabRotation, VRGrabbable obj)
    {
        return new VRGrabPoint
        {
            WorldPos = worldGrabPoint,
            WorldRotation = worldGrabRotation,
            LocalPos = obj.transform.InverseTransformPoint(worldGrabPoint),
            LocalRotation = Quaternion.Inverse(obj.transform.rotation) * worldGrabRotation
        };
    }

    /// <summary>
    /// Actively grabbed objects
    /// </summary>
    private class GrabbedObject
    {
        public VRGrabbable Object;
        public readonly List<Grab> Grabs = new List<Grab>();
    }

    private class Grab
    {
        public VRGrabber Grabber;
        public VRGrabPoint Pt;
    }
}

public class VRGrabPoint
{
    public Vector3 LocalPos;
    public Quaternion LocalRotation;
    public Vector3 WorldPos;
    public Quaternion WorldRotation;
}

public class VRMovedGrabPoint
{
    public VRGrabPoint InitialPt;
    public VRGrabPoint CurrentPt;
}