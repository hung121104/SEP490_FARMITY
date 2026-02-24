using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    private float normalTimeScale = 1f;
    private float slowTimeScale = 0.3f;

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

    public void SetSlowMotion()
    {
        Time.timeScale = slowTimeScale;
    }

    public void SetNormalSpeed()
    {
        Time.timeScale = normalTimeScale;
    }

    public void SetTimeScale(float scale)
    {
        Time.timeScale = Mathf.Clamp01(scale);
    }

    public bool IsSlowMotion => Time.timeScale < normalTimeScale;
}