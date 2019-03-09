using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(RacetrackProgressTracker))]
public class RacetrackCarState : MonoBehaviour
{
    private Rigidbody car;
    private RacetrackProgressTracker progressTracker;
    private CarState state = new CarState();

    public RacetrackCarStates Track;

    private void Awake()
    {
        car = GetComponent<Rigidbody>();
        progressTracker = GetComponent<RacetrackProgressTracker>();
    }

    // Start is called before the first frame update
    void Start()
    {
        var cars = GetCars();
        if (cars == null) return;

        // Register this car
        cars.RegisterCar(car, state);
    }

    /// <summary>
    /// Get the current car's state
    /// </summary>
    public CarState State
    {
        get { return state; }
    }

    /// <summary>
    /// Get the car infront's state
    /// </summary>
    public CarState GetNextCarState()
    {
        var cars = GetCars();
        if (cars == null) return null;

        return cars.GetNextCarState(car);
    }

    /// <summary>
    /// Resolve the RacetrackCarStates object
    /// </summary>
    private RacetrackCarStates GetCars()
    {
        // Set explicitly?
        if (Track != null)
            return Track;

        // Otherwise look for component on Racetrack instance
        if (Racetrack.Instance == null)
        {
            Debug.LogError("RacetrackCarState - Could not find the Racetrack");
            return null;
        }

        var cars = Racetrack.Instance.GetComponent<RacetrackCarStates>();
        if (cars == null)
        {
            Debug.LogError("RacetrackCarState - Racetrack does not have a RacetrackCarStates component");
            return null;
        }

        return cars;
    }
}
