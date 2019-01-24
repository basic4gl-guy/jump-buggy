using System;
using UnityEngine;

public class RacetrackCurve : MonoBehaviour {

    [Header("Shape")]
    [Range(1.0f, 200.0f)]
    public float Length = 10.0f;

    [RacetrackCurveAngles]
    public Vector3 Angles = new Vector3();

    [Header("Flags")]
    public bool IsJump = false;
    public bool CanRespawn = true;

    [Header("Meshes")]
    public RacetrackMeshTemplate Template;

    [HideInInspector]
    public int Index;

    // Use this for initialization
    void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		
	}

    /// <summary>
    /// Find the track
    /// </summary>
    public Racetrack Track
    {
        get
        {
            if (transform.parent == null)
                throw new Exception("Curve (" + name + ") parent is not set. Parent should exist and have a 'Track' component.");
            var track = transform.parent.GetComponent<Racetrack>();
            if (track == null)
                throw new Exception("Curve (" + name + ") parent does not have a 'Racetrack' component.");
            return track;
        }
    }
}
