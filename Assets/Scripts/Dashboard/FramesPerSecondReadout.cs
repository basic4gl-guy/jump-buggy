using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FramesPerSecondReadout : MonoBehaviour {

    public Text Readout;

	// Update is called once per frame
	void Update () {
        var readout = Readout ?? GetComponent<Text>();
        if (readout != null)
            readout.text = (1.0f / Time.deltaTime).ToString("0");
	}
}
