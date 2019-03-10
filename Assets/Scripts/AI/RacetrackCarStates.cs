using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Maintains a list of car states sorted by their distance around the racetrack.
/// Driving AI can use this to:
///     * Determine the car's position
///     * Search ahead for other cars to avoid
/// This component should be configured to update before the components that depend on it (e.g. AICarController)
/// </summary>
[RequireComponent(typeof(Racetrack))]
public class RacetrackCarStates : MonoBehaviour
{
    private Racetrack track;
    private readonly List<RacetrackCarState> carStates = new List<RacetrackCarState>();
    private readonly CarDistanceComparer carDistanceComparer = new CarDistanceComparer();

    public void RegisterCarState(RacetrackCarState carState)
    {
        carStates.Add(carState);
    }

    public void UnregisterCarState(RacetrackCarState carState)
    {
        carStates.Remove(carState);
    }

    /// <summary>
    /// Return the state of the car infront of a specific car
    /// </summary>
    public RacetrackCarState GetNextCarState(RacetrackCarState carState)
    {
        if (carStates.Count < 2) return null;

        // Find car
        var index = carStates.IndexOf(carState);
        if (index == -1) return null;

        // Return next car
        index = (index + 1) % carStates.Count;
        return carStates[index];
    }

    public Racetrack Track
    {
        get { return track; }
    }

    void Awake()
    {
        track = GetComponent<Racetrack>();   
    }

    void FixedUpdate()
    {
        // Recalculate car states
        foreach (var carState in carStates)
        {
            carState.UpdateState(track);
        }

        // Sort by distance down track
        carStates.Sort(carDistanceComparer);
    }

    /// <summary>
    /// Sorts cars by their distance along the racetrack
    /// </summary>
    private class CarDistanceComparer : IComparer<RacetrackCarState>
    {
        public int Compare(RacetrackCarState x, RacetrackCarState y)
        {
            int result = x.State.SegmentIndex - y.State.SegmentIndex;
            if (result == 0 && x.State.Position.z != y.State.Position.z)
                result = x.State.Position.z > y.State.Position.z ? 1 : 0;
            return result;
        }
    }
}
