using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Curve : MonoBehaviour {

    public float Length = 10.0f;
    public Vector3 Angles = new Vector3();
    public MeshFilter MeshFilter;                 // TODO: "Mesh templates"
    public bool IsJump = false;
    public bool CanRespawn = true;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    /// <summary>
    /// Find the track
    /// </summary>
    public Track Track
    {
        get
        {
            if (transform.parent == null)
                throw new Exception("Curve (" + name + ") parent is not set. Parent should exist and have a 'Track' component.");
            var track = transform.parent.GetComponent<Track>();
            if (track == null)
                throw new Exception("Curve (" + name + ") parent does not have a 'Track' component.");
            return track;
        }
    }
}
