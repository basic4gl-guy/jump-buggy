using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Maintains a list of car states sorted by their distance around the racetrack.
/// Driving AI can use this to:
///     * Determine the car's position
///     * Search ahead for other cars to avoid
/// </summary>
[RequireComponent(typeof(Racetrack))]
public class RacetrackCarStates : MonoBehaviour
{
    private Racetrack track;
    private readonly List<CarAndState> cars = new List<CarAndState>();
    private readonly CarDistanceComparer carDistanceComparer = new CarDistanceComparer();

    public void RegisterCar(Rigidbody car, CarState state)
    {
        RacetrackProgressTracker progressTracker = car.GetComponent<RacetrackProgressTracker>();
        cars.Add(new CarAndState { Car = car, State = state, ProgressTracker = progressTracker });
        if (progressTracker == null)
            Debug.LogWarning("RacetrackCarStates - A car added without a RacetrackProgressTracker component. Will not be able to track position on racetrack.");
    }

    public void UnregisterCar(Rigidbody car)
    {
        cars.RemoveAll(c => c.Car == car);
    }

    /// <summary>
    /// Return the state of a specific car
    /// </summary>
    public CarState GetState(Rigidbody car)
    {
        var entry = cars.SingleOrDefault(c => c.Car == car);
        return entry != null ? entry.State : null;
    }

    /// <summary>
    /// Return the state of the car infront of a specific car
    /// </summary>
    public CarState GetNextCarState(Rigidbody car)
    {
        if (cars.Count < 2) return null;

        // Find car
        var index = cars.FindIndex(c => c.Car == car);
        if (index == -1) return null;

        // Return next car
        index = (index + 1) % cars.Count;
        return cars[index].State;        
    }

    void Awake()
    {
        track = GetComponent<Racetrack>();   
    }

    void FixedUpdate()
    {
        // Recalculate car states
        foreach (var car in cars)
        {
            if (car.ProgressTracker != null)
            {
                int curveIndex = car.ProgressTracker.currentCurve;
                RacetrackUtil.GetCarState(car.Car, track, curveIndex, car.State);
            }
        }

        // Sort by distance down track
        cars.Sort(carDistanceComparer);
    }

    public class CarAndState
    {
        public Rigidbody Car { get; set; }
        public CarState State { get; set; }
        public RacetrackProgressTracker ProgressTracker { get; set; }
    }

    /// <summary>
    /// Sorts cars by their distance along the racetrack
    /// </summary>
    private class CarDistanceComparer : IComparer<CarAndState>
    {
        public int Compare(CarAndState x, CarAndState y)
        {
            int result = x.State.SegmentIndex - y.State.SegmentIndex;
            if (result == 0 && x.State.Position.z != y.State.Position.z)
                result = x.State.Position.z > y.State.Position.z ? 1 : 0;
            return result;
        }
    }
}
