using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Base class for components that make another component's parameters "wander"
/// </summary>
/// <typeparam name="T">Type of other component</typeparam>
public abstract class WanderingParams<T> : MonoBehaviour where T: MonoBehaviour
{
    public WanderWave[] Waves;

    public float Factor = 1.0f;

    private T component;

    private ParamMapping[] mappings;

    private float time = 0.0f;

    private void Awake()
    {
        // Find component
        component = GetComponent<T>();
        if (component == null)
        {
            Debug.LogError("WanderingParams - Could not find component of type: " + (typeof(T)).Name);
            return;
        }

        // Get parameter mappings
        mappings = GetMappings();

        // Set initial values
        foreach (var mapping in mappings)
        {
            float initialValue = mapping.Get(component);
            mapping.Wander.Init(initialValue);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        time += Time.fixedDeltaTime;

        // "Wander" each param
        foreach (var mapping in mappings)
        {
            // Sum waves and clamp
            float paramTime = time + mapping.Wander.TimeOffset;
            float delta = Waves.Sum(w => w.GetValue(paramTime));
            delta = Mathf.Clamp(delta, -1.0f, 1.0f) * Factor;

            // Update parameter
            float value = mapping.Wander.GetValue(delta);
            mapping.Set(component, value);
        }
    }

    protected abstract ParamMapping[] GetMappings();

    protected class ParamMapping
    {
        public string Name { get; private set; }                // For debugging
        public WanderingParam Wander { get; private set; }
        public Func<T, float> Get { get; private set; }
        public Action<T, float> Set { get; private set; }

        public ParamMapping(string name, WanderingParam wander, Func<T, float> get, Action<T, float> set)
        {
            Name = name;
            Wander = wander;
            Get = get;
            Set = set;
        }
    }
}

/// <summary>
/// A sine wave, that can be summed to make a parameter "wander"
/// </summary>
[Serializable]
public class WanderWave
{
    public float Period;
    public float Amplitude;

    [NonSerialized]
    public float TimeOffset;

    public float GetValue(float time)
    {
        time += TimeOffset;
        return Mathf.Sin(time * (Mathf.PI * 2.0f) / Period) * Amplitude;
    }
}

/// <summary>
/// A floating point parameter that can wander over time
/// </summary>
[Serializable]
public class WanderingParam
{
    /// <summary>
    /// Base value or offset
    /// </summary>
    public float Base;

    /// <summary>
    /// Whether "Base" is an absolute value or relative
    /// </summary>
    public bool IsRelative = true;

    /// <summary>
    /// Range which param can "wander"
    /// </summary>
    public float Range = 10.0f;

    /// <summary>
    /// True if range is a percentage of the initial value
    /// </summary>
    public bool IsPercentage = true;

    private float initialValue;         // Initial value from actual property

    [NonSerialized]
    public float TimeOffset;            // Added to the time parameter

    public void Init(float initialValue)
    {
        // Store initial value
        this.initialValue = initialValue;

        // Generate random time offset
        TimeOffset = UnityEngine.Random.value * 10000.0f;
    }

    public float GetValue(float delta)
    {
        // Calculate effective base value
        float value = Base;
        if (IsRelative)
            value += initialValue;

        // Calculate wandering range
        float range = Range;
        if (IsPercentage)
            range = (range * initialValue) / 100.0f;

        // Return result
        return value + delta * range;
    }
}
