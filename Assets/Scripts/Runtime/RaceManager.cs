using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages a single race.
/// Singleton cross-scene object
/// </summary>
public class RaceManager : MonoBehaviour
{
    // Parameters
    public int Laps = 3;

    public AICarDifficulty AICarDifficulty = AICarDifficulty.Medium;

    [Tooltip("ID codes of AI cars. Must match cars defined in AI Car Types")]
    public string[] AICarIDs;

    [Tooltip("Player position in starting grid. 1 = 1st, 2 = 2nd, etc")]
    public int PlayerStartGridPosition;

    public float StartGridZSpacing = 5.0f;
    public float StartGridXSpacing = 4.0f;
    public float StartGridY = 0.5f;
    public float StartGridZOffset = 5.0f;

    // Internal state
    private static RaceManager instance;

    private Racetrack racetrack;

    private RacetrackAIData racetrackAIData;

    private RacetrackCarStates carStates;

    private AICarTypes aiCarTypes;

    private CarInfo playerCar;

    private CarInfo[] aiCars = null;

    private RaceStateType state;

    private bool isInitialised = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Setup as the persistent instance
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void FixedUpdate()
    {
        if (state == RaceStateType.InProgress)
        {
            foreach (var car in AllCars())
            {
                // Check if car has completed race
                if (car.finalPosition == null && car.progressTracker.lapCount >= Laps)
                {
                    // Set final position
                    car.finalPosition = AllCars().Max(c => c.finalPosition ?? 0) + 1;
                    Debug.LogFormat("{0} has finished in position {1}", car, car.finalPosition);
                    car.Mode = CarMode.Stopping;
                }
            }

            // Is race complete?
            if (AllCars().All(c => c.finalPosition != null))
            {
                state = RaceStateType.Completed;
                RaceHasCompleted();
            }
        }
    }

    // Initialise on on level load

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnLevelLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLevelLoaded;
    }

    /// <summary>
    /// Start (or restart) race
    /// </summary>
    public void StartRace()
    {
        Debug.Log("StartRace()");
        if (!isInitialised) return;
        if (state == RaceStateType.PreRaceSequence)
        {
            Debug.LogError("Race is already starting");
            return;
        }

        SetupForRace();

        // Run the pre-race sequence
        state = RaceStateType.PreRaceSequence;
        StartCoroutine(StartRaceSequence().Then(() =>
        {
            // Enable the cars
            foreach (var car in AllCars())
            {
                car.Mode = CarMode.Driving;
                car.body.velocity = car.carObj.transform.TransformVector(Vector3.forward * 1.0f);
                car.progressTracker.isTimerRunning = true;
            }

            // Switch to in progress
            state = RaceStateType.InProgress;
            RaceHasStarted();
        }));
        PreRaceSequenceStarted();
    }

    /// <summary>
    /// Create/position cars for race
    /// </summary>
    public void SetupForRace()
    {
        Debug.Log("SetupForRace()");
        if (!isInitialised) return;

        // Setup for race
        if (aiCars == null)
            CreateAICars();
        WireupCars();
        PositionCarsOnStartGrid();
    }

    private IEnumerator StartRaceSequence()
    {
        // TODO: Animated pre-race sequence
        yield return new WaitForSeconds(5.0f);
    }

    private void PreRaceSequenceStarted()
    {
        Debug.Log("Pre race sequence has started");
    }

    private void RaceHasStarted()
    {
        Debug.Log("Race has started");
    }

    private void RaceHasCompleted()
    {
        Debug.Log("Race has completed");
    }    

    private void OnLevelLoaded(Scene arg0, LoadSceneMode arg1)
    {
        InitRaceManager();
        StartRace();
    }

    private void InitRaceManager()
    {
        // Reset
        isInitialised = false;
        aiCars = null;
        state = RaceStateType.NotStarted;

        // Find required objects
        playerCar = null;
        var playerCarController = GameObject.FindObjectsOfType<CarUserControlExt>().FirstOrDefault();
        if (playerCarController != null)
        {
            var carInfo = new CarInfo(playerCarController.gameObject, null);
            if (carInfo.Validate())
                playerCar = carInfo;
        }
        racetrack = GameObject.FindObjectsOfType<Racetrack>().FirstOrDefault();
        carStates = null;
        if (racetrack != null)
            carStates = racetrack.GetComponent<RacetrackCarStates>();
        racetrackAIData = GameObject.FindObjectsOfType<RacetrackAIData>().FirstOrDefault();
        aiCarTypes = GameObject.FindObjectsOfType<AICarTypes>().FirstOrDefault();

        if (playerCar == null) Debug.Log("RaceManager - Could not find player car");
        if (racetrack == null) Debug.Log("RaceManager - Could not find racetrack");
        if (racetrack != null && carStates == null) Debug.Log("RaceManager - Racetrack does not have a RacetrackCarStates component");
        if (racetrackAIData == null) Debug.Log("RaceManager - Could not find racetrack AI data");
        if (aiCarTypes == null) Debug.Log("RaceManager - Could not find AI car types");

        isInitialised = 
            playerCar != null && 
            racetrack != null && 
            carStates != null && 
            racetrackAIData != null && 
            aiCarTypes != null;
    }

    private void CreateAICars()
    {
        aiCars = new CarInfo[AICarIDs.Length];
        for (int i = 0; i < AICarIDs.Length; i++)
        {
            var aiCarType = aiCarTypes.Get(AICarIDs[i]);
            if (aiCarType == null)
            {
                Debug.LogErrorFormat("AI car type '{0}' not found (AI difficulty: {1})", AICarIDs[i], AICarDifficulty);
                continue;
            }

            // Create object
            var aiCarPrefab = aiCarType.Get(AICarDifficulty);
            var carObj = Instantiate(aiCarPrefab).gameObject;

            // Find components
            var carInfo = new CarInfo(carObj, aiCarType);
            if (!carInfo.Validate())
            {
                GameObject.Destroy(carObj);
                continue;
            }

            aiCars[i] = carInfo;
        }
    }

    private void WireupCars()
    {
        foreach (var car in AllCars())
        {
            car.carState.Track = carStates;
            if (car.aiCarController != null)
            {
                car.aiCarController.RacetrackAIData = racetrackAIData;
            }
        }
    }

    private void PositionCarsOnStartGrid()
    {
        int playerIndex = PlayerStartGridPosition - 1;
        if (playerIndex < 0)
            playerIndex = 0;
        if (playerIndex > aiCars.Length)
            playerIndex = aiCars.Length;

        for (int i = 0; i < aiCars.Length + 1; i++)
        {
            // Determine which car to place
            CarInfo car;
            if (i < playerIndex)
            {
                if (aiCars[i] == null) continue;
                car = aiCars[i];
            }
            else if (i == playerIndex)
            {
                car = playerCar;
            }
            else
            {
                if (aiCars[i - 1] == null) continue;
                car = aiCars[i - 1];
            }

            // Calculate position to place car
            int pos = aiCars.Length - i;
            float z = pos * StartGridZSpacing + StartGridZOffset;
            float x = (pos & 1) == 0 ? -StartGridXSpacing : StartGridXSpacing;

            // Find corresponding position in racetrack space
            float segIndex = Mathf.Floor(z / racetrack.SegmentLength);
            float segZ = z - segIndex * racetrack.SegmentLength;
            var seg = racetrack.GetSegment((int)segIndex);
            Matrix4x4 segTransform = seg.GetSegmentToTrack(segZ);
            Vector3 position = segTransform.MultiplyPoint(new Vector3(x, StartGridY, 0.0f));
            Quaternion rotation = Quaternion.LookRotation(
                segTransform.MultiplyVector(Vector3.forward).normalized,
                segTransform.MultiplyVector(Vector3.up).normalized);

            // Position car in world space
            car.carObj.transform.position = racetrack.transform.TransformPoint(position);
            car.carObj.transform.rotation = racetrack.transform.rotation * rotation;

            // Kill all momentum
            car.body.velocity = Vector3.zero;
            car.body.angularVelocity = Vector3.zero;

            // Reset racetrack progress
            car.progressTracker.currentCurve = seg.Curve.Index;
            car.progressTracker.lapCount = 0;
            car.progressTracker.CurrentLapTime = 0.0f;
            car.progressTracker.BestLapTime = 0.0f;
            car.progressTracker.LastLapTime = 0.0f;
            car.progressTracker.offRoadTimer = 0.0f;
            car.progressTracker.isAboveRoad = false;
            car.progressTracker.isTimerRunning = false;
        }
    }

    private IEnumerable<CarInfo> AllCars()
    {
        yield return playerCar;
        if (aiCars != null)
            foreach (var aiCar in aiCars)
                if (aiCar != null)
                    yield return aiCar;
    }

    private class CarInfo
    {
        public CarInfo(GameObject carObj, AICarTypeEntry aiCarType)
        {
            this.carObj = carObj;
            this.aiCarType = aiCarType;
            if (carObj != null)
            {
                body = carObj.GetComponent<Rigidbody>();
                progressTracker = carObj.GetComponent<RacetrackProgressTracker>();
                carState = carObj.GetComponent<RacetrackCarState>();
                aiCarController = carObj.GetComponent<AICarController>();
                playerCarController = carObj.GetComponent<CarUserControlExt>();
            }
        }

        public GameObject carObj;
        public AICarTypeEntry aiCarType;                    // AI car information. Null for player car.

        // Components
        public Rigidbody body;
        public RacetrackProgressTracker progressTracker;
        public RacetrackCarState carState;
        public AICarController aiCarController;             // Optional for player car. Required for AI cars.
        public CarUserControlExt playerCarController;       // Required for player car.

        // State
        public int? finalPosition;

        public bool Validate()
        {
            if (carObj == null)
                Debug.LogErrorFormat("Missing Car game object");
            else
            {
                // Check for common objects
                if (body == null)
                    Debug.LogErrorFormat("Car game object {0} has no RigidBody", carObj);
                if (progressTracker == null)
                    Debug.LogErrorFormat("Car game object {0} has no RacetrackProgressTracker", carObj);
                if (carState == null)
                    Debug.LogErrorFormat("Car game object {0} has no RacetrackCarState", carObj);

                // AI controlled cars 
                if (aiCarType != null)
                {
                    if (aiCarController == null)
                        Debug.LogErrorFormat("Car game object {0} has no RacetrackCarState", carObj);
                }
                else
                {
                    if (playerCarController == null)
                        Debug.LogErrorFormat("Player car game object {0} has no CarUserControllerExt", carObj);
                }
            }

            return carObj != null && 
                body != null && 
                progressTracker != null &&
                carState != null &&
                (aiCarType == null || aiCarController != null) &&
                (aiCarType != null || playerCarController != null);
        }

        public CarMode Mode
        {
            get
            {
                return playerCarController != null ? playerCarController.Mode : aiCarController.Mode;
            }
            set
            {
                if (playerCarController != null)
                    playerCarController.Mode = value;
                if (aiCarController != null)
                    aiCarController.Mode = value;
            }
        }

        public override string ToString()
        {
            if (aiCarType == null)
                return "Player";
            else
                return aiCarType.Name;
        }
    }
}

public enum RaceStateType
{
    NotStarted,
    PreRaceSequence,
    InProgress,
    Completed
}
