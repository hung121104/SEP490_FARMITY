using TMPro;
using UnityEngine;

/// <summary>
/// Displays in-game time rounded to every 5 minutes (e.g. "06h10", "18h45").
/// Only updates the text when the displayed value actually changes.
/// Assign a TextMeshProUGUI in the Inspector.
/// </summary>
public class TimeDisplayView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeLabel;

    private TimeManagerView _timeManager;
    private int _lastDisplayedHour = -1;
    private int _lastDisplayedMinute = -1;

    private void Awake()
    {
        _timeManager = FindFirstObjectByType<TimeManagerView>();
    }

    private void Update()
    {
        if (_timeManager == null || timeLabel == null) return;

        int hour = _timeManager.hour;
        int minute = ((int)_timeManager.minute / 5) * 5;

        if (hour == _lastDisplayedHour && minute == _lastDisplayedMinute) return;

        _lastDisplayedHour = hour;
        _lastDisplayedMinute = minute;
        timeLabel.text = $"{hour:D2}h{minute:D2}";
    }
}
