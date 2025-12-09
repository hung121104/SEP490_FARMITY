using System;
using UnityEngine;

public abstract class Singleton<T> where T : class
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // Create even non-public parameterless constructors
                        _instance = (T)Activator.CreateInstance(typeof(T), nonPublic: true);
                    }
                }
            }
            return _instance;
        }
    }

    // Derived classes can implement their own protected/private ctor.
    protected Singleton() { }

    // Clear singleton on domain reload / play mode stop to avoid stale UnityEngine.Object references.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void OnSubsystemRegistration()
    {
        lock (_lock)
        {
            _instance = null;
        }
    }
}