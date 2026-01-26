using System;
using UnityEngine;

/// <summary>
/// Generic singleton helper that supports either plain C# singletons (created via Activator)
/// or MonoBehaviour singletons (created as a GameObject with AddComponent).
/// Place this file anywhere inside Assets so it gets compiled into the project.
/// </summary>
public abstract class Singleton<T> where T : class
{
    private static readonly object _lock = new object();
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance != null) return _instance;

            lock (_lock)
            {
                if (_instance != null) return _instance;

                var t = typeof(T);

                // If T is a MonoBehaviour, try to find an existing instance in scene first,
                // otherwise create a new GameObject and add the component.
                if (typeof(MonoBehaviour).IsAssignableFrom(t))
                {
                    // Try find existing instance in scene
                    var found = UnityEngine.Object.FindFirstObjectByType(t) as T;
                    if (found != null)
                    {
                        _instance = found;
                        return _instance;
                    }

                    // Create new GameObject and add component
                    var go = new GameObject(t.Name + "_Singleton");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _instance = go.AddComponent(t) as T;
                }
                else
                {
                    // Non-MonoBehaviour: use Activator to call non-public ctor
                    _instance = (T)Activator.CreateInstance(t, true);
                }

                return _instance;
            }
        }
    }

    // Allow subclasses to detect when they are created through this base if needed.
    protected Singleton() { }
}