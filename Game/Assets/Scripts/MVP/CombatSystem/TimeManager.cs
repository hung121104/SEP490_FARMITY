using UnityEngine;

/// <summary>
/// Manages game time scale for slow-motion effects
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [SerializeField] private float slowMotionScale = 0.2f;
    [SerializeField] private float slowMotionTransitionSpeed = 5f;

    private float targetTimeScale = 1f;
    private bool isSlowMotion = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Smoothly transition to target time scale
        Time.timeScale = Mathf.Lerp(Time.timeScale, targetTimeScale, slowMotionTransitionSpeed * Time.unscaledDeltaTime);
    }

    /// <summary>
    /// Enter slow motion mode
    /// </summary>
    public void EnterSlowMotion()
    {
        targetTimeScale = slowMotionScale;
        isSlowMotion = true;
    }

    /// <summary>
    /// Resume normal time
    /// </summary>
    public void ResumeNormalTime()
    {
        targetTimeScale = 1f;
        isSlowMotion = false;
    }

    public bool IsSlowMotion => isSlowMotion;
    public float SlowMotionScale => slowMotionScale;
}