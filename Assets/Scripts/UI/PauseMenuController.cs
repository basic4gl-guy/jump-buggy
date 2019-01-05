using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour {

    [Header("Wireup")]
    public GameObject[] ShowOnPause;
    public GameObject[] HideOnPause;
    public RoadProgressTracker Car;

    // Working 
    public bool IsPaused = false;
    private bool wasBackDown = false;
    private bool isBusy = false;

    private void Start()
    {
        ShowHideGameObjects();
    }

    void Update () {
        if (isBusy) return;

        bool isBackDown = OVRInput.Get(OVRInput.Button.Back) || Input.GetKey(KeyCode.Escape);
        if (isBackDown && !wasBackDown)
        {
            // Toggle paused state
            if (!IsPaused)
                Pause();
            else
                Unpause();
        }
        wasBackDown = isBackDown;

        if (IsPaused && Input.GetKey(KeyCode.R) && !isBusy)
            ResetCar();
        if (IsPaused && Input.GetKey(KeyCode.L) && !isBusy)
            RestartLevel();
        if (IsPaused && Input.GetKey(KeyCode.M) && !isBusy)
            MainMenu();
    }

    // Pause menu actions

    public void ResetCar()
    {
        if (isBusy) return;

        if (Car != null)
            Do(
                CoroutineUtils.FadeOut()
                    .Then(() =>
                    {
                        Unpause();
                        Car.PutCarOnRoad();
                    })
                    .Then(CoroutineUtils.FadeIn())
                );
    }

    public void RestartLevel()
    {
        if (isBusy) return;

        Do(
            CoroutineUtils.FadeOut()
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
            CoroutineUtils.FadeOut()
                .Then(() =>
                {
                    Unpause();
                    SceneManager.LoadScene("Main Menu");
                })
            );
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
