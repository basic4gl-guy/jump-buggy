using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour {

    [Header("Wireup")]
    public GameObject[] ShowOnPause;
    public GameObject[] HideOnPause;
    public RacetrackProgressTracker Car;
    public Toggle AutoResetToggle;

    // Working 
    public bool IsPaused = false;
    private bool wasBackDown = false;
    private bool isBusy = false;
    private bool wasADown = false;

    private void Start()
    {
        ShowHideGameObjects();
    }

    void Update () {
        if (isBusy) return;

        bool isBackDown = OVRInput.Get(OVRInput.Button.Back) || OVRInput.Get(OVRInput.Button.Start) || Input.GetKey(KeyCode.Escape);
        if (isBackDown && !wasBackDown)
        {
            // Toggle paused state
            if (!IsPaused)
                Pause();
            else
                Unpause();
        }
        wasBackDown = isBackDown;

        if (IsPaused)
        {
            if (Input.GetKey(KeyCode.R) && !isBusy)
                ResetCar();
            if (Input.GetKey(KeyCode.L) && !isBusy)
                RestartLevel();
            if (Input.GetKey(KeyCode.M) && !isBusy)
                MainMenu();
            bool aDown = Input.GetKey(KeyCode.A);
            if (aDown && !wasADown)
                AutoResetToggle.isOn = !AutoResetToggle.isOn;
            wasADown = aDown;
        }
    }

    // Pause menu actions

    public void ResetCar()
    {
        if (isBusy) return;

        if (Car != null)
            Do(
                VRCoroutineUtil.FadeOut()
                    .Then(() =>
                    {
                        Unpause();
                        Car.PutCarOnRoad();
                    })
                    .Then(VRCoroutineUtil.FadeIn())
                );
    }

    public void RestartLevel()
    {
        if (isBusy) return;

        Do(
            VRCoroutineUtil.FadeOut()
                .Then(() =>
                {
                    Unpause();
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);     // Reload current scene
                })
            );
    }

    public void MainMenu()
    {
        if (isBusy) return;

        Do(
            VRCoroutineUtil.FadeOut()
                .Then(() =>
                {
                    Unpause();
                    SceneManager.LoadScene("Main Menu");
                })
            );
    }

    public void SetAutoReset(bool value)
    {
        if (Car != null)
        {
            Car.AutoReset = value;
        }
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0.0f;
        ShowHideGameObjects();
    }

    public void Unpause()
    {
        IsPaused = false;
        Time.timeScale = 1.0f;
        ShowHideGameObjects();
    }

    private void Do(IEnumerator coroutine)
    {
        if (isBusy) throw new Exception("Parallel pause menu coroutines not allowed");

        isBusy = true;
        StartCoroutine(coroutine.Then(() => isBusy = false));       
    }

    private void ShowHideGameObjects()
    {
        if (ShowOnPause != null)
            foreach (var obj in ShowOnPause)
                obj.SetActive(IsPaused);

        if (HideOnPause != null)
            foreach (var obj in HideOnPause)
                obj.SetActive(!IsPaused);
    }
}
