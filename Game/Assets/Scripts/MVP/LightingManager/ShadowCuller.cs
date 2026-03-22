using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Performance optimiser for dense shadow-caster scenes.
/// Disables ShadowCaster2D components that are outside the camera frustum
/// (plus a configurable buffer) and disables ALL casters at night when
/// shadows would be invisible anyway.
/// 
/// Usage:
///   • Static objects: call ShadowCuller.Register(shadowCaster) once (e.g. in Awake).
///   • Dynamic objects: call Register on spawn, Unregister on destroy.
/// </summary>
public class ShadowCuller : MonoBehaviour
{
    public static ShadowCuller Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private DayNightCycleConfig config;

    [Header("Performance")]
    [Tooltip("How often (seconds) to re-evaluate which casters are visible")]
    [SerializeField] private float cullInterval = 0.25f;

    private Camera _cam;
    private float _nextCullTime;

    // Registered casters
    private static readonly List<ShadowCaster2D> _casters = new List<ShadowCaster2D>();

    // ── Public registration API ──────────────────────────────────

    public static void Register(ShadowCaster2D caster)
    {
        if (caster != null && !_casters.Contains(caster))
            _casters.Add(caster);
    }

    public static void Unregister(ShadowCaster2D caster)
    {
        _casters.Remove(caster);
    }

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        if (Time.time < _nextCullTime || _cam == null || config == null)
            return;

        _nextCullTime = Time.time + cullInterval;

        bool nightMode = DayNightCycleManager.Instance != null
            && DayNightCycleManager.Instance.CurrentIntensity < config.shadowNightThreshold;

        if (nightMode)
        {
            DisableAll();
            return;
        }

        // Camera world-space rect + buffer
        float camH = _cam.orthographicSize;
        float camW = camH * _cam.aspect;
        Vector3 camPos = _cam.transform.position;
        float buf = config.shadowCullBuffer;

        float minX = camPos.x - camW - buf;
        float maxX = camPos.x + camW + buf;
        float minY = camPos.y - camH - buf;
        float maxY = camPos.y + camH + buf;

        // Clean up destroyed entries while iterating
        for (int i = _casters.Count - 1; i >= 0; i--)
        {
            var sc = _casters[i];
            if (sc == null) { _casters.RemoveAt(i); continue; }

            Vector3 p = sc.transform.position;
            bool inside = p.x >= minX && p.x <= maxX && p.y >= minY && p.y <= maxY;
            sc.enabled = inside;
        }
    }

    private void DisableAll()
    {
        for (int i = _casters.Count - 1; i >= 0; i--)
        {
            var sc = _casters[i];
            if (sc == null) { _casters.RemoveAt(i); continue; }
            sc.enabled = false;
        }
    }
}
