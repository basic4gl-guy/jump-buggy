using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// A dictionary of AI cars.
/// AI cars can be fetched by name and difficulty
/// </summary>
public class AICarTypes : MonoBehaviour
{
    public AICarTypeEntry[] Entries;

    public AICarTypeEntry Get(string id)
    {
        return Entries.FirstOrDefault(e => e.UniqueID == id);
    }
}

/// <summary>
/// A single AI car entry
/// </summary>
[Serializable]
public class AICarTypeEntry
{
    /// <summary>
    /// Unique ID code
    /// </summary>
    public string UniqueID;

    /// <summary>
    /// AI driver name, e.g. "Fast Freddy"
    /// </summary>
    public string Name;

    // Car prefab links for each difficulty.
    // Each one should be a whole car prefab to instantiate.
    // AICarController component is returned, because every AI car will have one at the top level.
    public AICarController Easy;
    public AICarController Medium;
    public AICarController Hard;

    /// <summary>
    /// Get AI car for a specific difficulty setting
    /// </summary>
    /// <param name="difficulty">The difficulty setting</param>
    /// <returns>Corresponding car prefab</returns>
    public AICarController Get(AICarDifficulty difficulty)
    {
        switch (difficulty)
        {
            case AICarDifficulty.Easy:
                return Easy;
            case AICarDifficulty.Medium:
                return Medium;
            case AICarDifficulty.Hard:
                return Hard;
            default:
                throw new ArgumentOutOfRangeException("difficulty");        // (Should never happen)
        }
    }
}

public enum AICarDifficulty
{
    Easy,
    Medium,
    Hard
}
