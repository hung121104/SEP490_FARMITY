using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class TimeManagerView : MonoBehaviourPunCallbacks
{
    // Photon Custom Properties keys
    private const string PROP_YEAR = "WorldYear";
    private const string PROP_MONTH = "WorldMonth";
    private const string PROP_DAY = "WorldDay";
    private const string PROP_HOUR = "WorldHour";
    private const string PROP_MINUTE = "WorldMinute";
    
    [Header("Network Settings")]
    [Tooltip("How often to sync time to other clients (seconds)")]
    public float syncInterval = 5f;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    // Time configuration
    private const int MinutesPerHour = 60;
    private const int HoursPerDay = 24;
    private const int DaysPerWeek = 7;
    private const int DaysPerMonth = 28;
    private const int MonthsPerYear = 4; // 4 seasons, each is a month
    private const int DaysPerYear = 112; // 4 * 28

    // Current time
    public int year = 1;
    public Season season = Season.Spring;
    public int month = 1; // 1-4
    public int week = 1; // 1-4 per month
    public int day = 1; // 1-7 per week, or 1-28 per month
    public int hour = 0; // 0-23
    public float minute = 0f; // 0-59.999...

    // Time speed: how many game hours per real second
    public float timeSpeed = 1f;
    
    private float nextSyncTime;

    // Events
    public delegate void TimeChangedHandler();
    public event TimeChangedHandler OnDayChanged;
    public event TimeChangedHandler OnWeekChanged;
    public event TimeChangedHandler OnMonthChanged;
    public event TimeChangedHandler OnSeasonChanged;
    public event TimeChangedHandler OnYearChanged;

    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("[TimeManager] Not connected to Photon network, running in offline mode");
            return;
        }
        
        // Load time from room properties if not master client
        if (!PhotonNetwork.IsMasterClient)
        {
            LoadTimeFromRoomProperties();
        }
        else
        {
            // Master client initializes time in room properties
            SyncTimeToRoomProperties();
        }
        
        nextSyncTime = Time.time + syncInterval;
    }

    void Update()
    {
        if (!PhotonNetwork.IsConnected)
        {
            // Offline mode - advance time normally
            AdvanceTime();
            return;
        }
        
        // Only Master Client advances time
        if (PhotonNetwork.IsMasterClient)
        {
            AdvanceTime();
            
            // Periodically sync to room properties
            if (Time.time >= nextSyncTime)
            {
                SyncTimeToRoomProperties();
                nextSyncTime = Time.time + syncInterval;
            }
        }
    }
    
    private void AdvanceTime()
    {
        // Advance time
        minute += Time.deltaTime * timeSpeed;

        if (minute >= MinutesPerHour)
        {
            minute -= MinutesPerHour;
            hour++;
            if (hour >= HoursPerDay)
            {
                hour = 0;
                AdvanceDay();
            }
        }
    }

    private void AdvanceDay()
    {
        day++;
        OnDayChanged?.Invoke();

        if (day > DaysPerWeek)
        {
            day = 1;
            week++;
            OnWeekChanged?.Invoke();

            if (week > DaysPerMonth / DaysPerWeek)
            {
                week = 1;
                month++;
                season = (Season)(month - 1);
                OnMonthChanged?.Invoke();
                OnSeasonChanged?.Invoke();

                if (month > MonthsPerYear)
                {
                    month = 1;
                    season = Season.Spring;
                    year++;
                    OnYearChanged?.Invoke();
                }
            }
        }
    }
    
    /// <summary>
    /// Sync current time to room custom properties (Master Client only)
    /// </summary>
    private void SyncTimeToRoomProperties()
    {
        if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
            return;
        
        Hashtable timeProps = new Hashtable
        {
            { PROP_YEAR, year },
            { PROP_MONTH, month },
            { PROP_DAY, day },
            { PROP_HOUR, hour },
            { PROP_MINUTE, minute }
        };
        
        PhotonNetwork.CurrentRoom.SetCustomProperties(timeProps);
        
        if (showDebugLogs)
            Debug.Log($"[TimeManager] Synced time: {GetCurrentTimeString()}");
    }
    
    /// <summary>
    /// Load time from room custom properties (for clients joining)
    /// </summary>
    private void LoadTimeFromRoomProperties()
    {
        if (!PhotonNetwork.InRoom)
            return;
        
        Hashtable props = PhotonNetwork.CurrentRoom.CustomProperties;
        
        if (props.ContainsKey(PROP_YEAR))
        {
            year = (int)props[PROP_YEAR];
            month = (int)props[PROP_MONTH];
            day = (int)props[PROP_DAY];
            hour = (int)props[PROP_HOUR];
            minute = (float)props[PROP_MINUTE];
            
            // Recalculate derived values
            season = (Season)(month - 1);
            week = ((day - 1) / DaysPerWeek) + 1;
            
            if (showDebugLogs)
                Debug.Log($"[TimeManager] Loaded time from room: {GetCurrentTimeString()}");
        }
    }
    
    /// <summary>
    /// Called when room properties change (for non-master clients)
    /// </summary>
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // If we're not master client, update our local time when room properties change
        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            bool timeUpdated = false;
            
            if (propertiesThatChanged.ContainsKey(PROP_YEAR))
            {
                year = (int)propertiesThatChanged[PROP_YEAR];
                timeUpdated = true;
            }
            if (propertiesThatChanged.ContainsKey(PROP_MONTH))
            {
                month = (int)propertiesThatChanged[PROP_MONTH];
                season = (Season)(month - 1);
                timeUpdated = true;
            }
            if (propertiesThatChanged.ContainsKey(PROP_DAY))
            {
                day = (int)propertiesThatChanged[PROP_DAY];
                week = ((day - 1) / DaysPerWeek) + 1;
                timeUpdated = true;
            }
            if (propertiesThatChanged.ContainsKey(PROP_HOUR))
            {
                hour = (int)propertiesThatChanged[PROP_HOUR];
                timeUpdated = true;
            }
            if (propertiesThatChanged.ContainsKey(PROP_MINUTE))
            {
                minute = (float)propertiesThatChanged[PROP_MINUTE];
                timeUpdated = true;
            }
            
            if (timeUpdated && showDebugLogs)
            {
                Debug.Log($"[TimeManager] Time updated from Master Client: {GetCurrentTimeString()}");
            }
        }
    }
    
    /// <summary>
    /// Called when this client becomes the Master Client
    /// </summary>
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (showDebugLogs)
                Debug.Log("[TimeManager] This client is now Master Client, taking over time management");
            
            // Initialize sync timer
            nextSyncTime = Time.time + syncInterval;
        }
    }

    public string GetCurrentTimeString()
    {
        return $"Year {year}, {season}, Month {month}, Week {week}, Day {day}, Hour {hour}, Minute {minute:F0}";
    }

    // Method to set time speed (Master Client only)
    public void SetTimeSpeed(float speed)
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[TimeManager] Only Master Client can change time speed");
            return;
        }
        
        timeSpeed = speed;
    }

    // Method to pause time (Master Client only)
    public void PauseTime()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[TimeManager] Only Master Client can pause time");
            return;
        }
        
        timeSpeed = 0f;
    }

    // Method to resume time (Master Client only)
    public void ResumeTime(float speed = 1f)
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[TimeManager] Only Master Client can resume time");
            return;
        }
        
        timeSpeed = speed;
    }

    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    public enum DayOfWeek
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    }
}
