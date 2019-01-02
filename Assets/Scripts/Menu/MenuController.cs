using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour {

    [Header("Wire up")]
    public TrackOverview[] TrackOverviews;
    public Text TrackName;
    public OVRScreenFade Fade;

    [Header("Parameters")]
    public float SpinSpeed = 45.0f;

    // Working state
    private int trackIndex = 0;
    private float angle = 0.0f;
    private bool isUIEnabled = true;

	// Use this for initialization
	void Start () {
        DisplaySelectedTrack();
	}

    // Update is called once per frame
    void Update () {
        if (!TrackOverviews.Any()) return;

        // Rotate model
        angle += SpinSpeed * Time.deltaTime;
        var track = TrackOverviews[trackIndex];
        var existingAngles = track.Model.transform.rotation.eulerAngles;
        track.Model.transform.localRotation = Quaternion.Euler(existingAngles.x, angle, existingAngles.z);
	}

    public void ShowNextTrack()
    {
        if (!IsUIEnabled) return;

        // Cycle next track
        trackIndex++;
        if (trackIndex >= TrackOverviews.Length)
            trackIndex = 0;

        // Display model
        DisplaySelectedTrack();
    }

    public void ShowPrevTrack()
    {
        if (!IsUIEnabled) return;

        // Cycle next track
        trackIndex--;
        if (trackIndex < 0)
            trackIndex = TrackOverviews.Length - 1;

        // Display model
        DisplaySelectedTrack();
    }

    public void GoRace()
    {
        if (!IsUIEnabled) return;
        isUIEnabled = false;                        // Disable UI while fadeout is playing
        var track = TrackOverviews[trackIndex];
        StartCoroutine(FadeOutThenLoadTrack(track));
    }

    private IEnumerator FadeOutThenLoadTrack(TrackOverview track)
    {
        // Perform fadeout
        if (Fade != null)
        {
            float elapsedTime = 0.0f;
            while (elapsedTime < Fade.fadeTime)
            {
                elapsedTime += Time.deltaTime;
                Fade.SetFadeLevel(Mathf.Clamp01(elapsedTime / Fade.fadeTime));
                yield return new WaitForEndOfFrame();
            }
        }

        // Load selected track
        SceneManager.LoadScene(track.SceneName);
    }

    private void DisplaySelectedTrack()
    {
        if (!TrackOverviews.Any()) return;

        // Set the selected track to active.
        // All others inactive.
        for (int i = 0; i < TrackOverviews.Length; i++)
            TrackOverviews[i].Model.gameObject.SetActive(i == trackIndex);

        // Display track name
        if (TrackName != null)
            TrackName.text = TrackOverviews[trackIndex].DisplayText;
    }

    private bool IsUIEnabled
    {
        get { return isUIEnabled && TrackOverviews.Any(); }
    }

    [Serializable]
    public class TrackOverview
    {
        public string DisplayText;
        public string SceneName;
        public Transform Model;
    }
}
