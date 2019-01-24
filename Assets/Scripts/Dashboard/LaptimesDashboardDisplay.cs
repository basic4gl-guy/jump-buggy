using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LaptimesDashboardDisplay : MonoBehaviour {

    public RacetrackProgressTracker Car;
    public Text LapCount;
    public Text LapTime;
    public Text BestLapTime;

	void Start () {		
	}
	
	void Update () {
        if (Car == null) return;
        if (LapCount != null) LapCount.text = (Car.lapCount + 1).ToString();                // Add 1 to show # of current lap (rather than # completed)
        if (LapTime != null) LapTime.text = FormatTime(Car.CurrentLapTime);
        if (BestLapTime != null) BestLapTime.text = FormatTime(Car.BestLapTime);		
	}

    /// <summary>
    /// Format floating point seconds as time (minutes:seconds:hundredths)
    /// </summary>
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60.0f);
        int seconds = Mathf.FloorToInt(time - minutes * 60.0f);
        float fraction = time - Mathf.Floor(time);
        int hundredths = Mathf.FloorToInt(fraction * 100.0f);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, hundredths);
    }
}
