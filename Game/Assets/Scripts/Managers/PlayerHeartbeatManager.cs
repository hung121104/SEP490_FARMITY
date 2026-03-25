using System.Collections;
using UnityEngine;

public class PlayerHeartbeatManager : MonoBehaviour
{
    private const float HeartbeatIntervalSeconds = 15f;
    private const float IdlePollSeconds = 1f;

    private static PlayerHeartbeatManager _instance;

    private Coroutine _heartbeatRoutine;
    private float _nextHeartbeatAt;
    private string _trackedToken;
    private bool _isPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (_instance != null) return;

        GameObject go = new GameObject("PlayerHeartbeatManager");
        _instance = go.AddComponent<PlayerHeartbeatManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (_heartbeatRoutine == null)
        {
            _heartbeatRoutine = StartCoroutine(HeartbeatLoop());
        }
    }

    private void OnDisable()
    {
        if (_heartbeatRoutine != null)
        {
            StopCoroutine(_heartbeatRoutine);
            _heartbeatRoutine = null;
        }
    }

    private IEnumerator HeartbeatLoop()
    {
        while (true)
        {
            if (_isPaused || !HasAuthenticatedSession())
            {
                _trackedToken = null;
                yield return new WaitForSecondsRealtime(IdlePollSeconds);
                continue;
            }

            string token = SessionManager.Instance.JwtToken;
            if (_trackedToken != token)
            {
                _trackedToken = token;
                _nextHeartbeatAt = Time.realtimeSinceStartup;
            }

            if (Time.realtimeSinceStartup < _nextHeartbeatAt)
            {
                float waitSeconds = Mathf.Max(0.25f, _nextHeartbeatAt - Time.realtimeSinceStartup);
                yield return new WaitForSecondsRealtime(waitSeconds);
                continue;
            }

            bool success = false;
            long statusCode = 0;
            PlayerHeartbeatResponse response = null;

            yield return PlayerHeartbeatApi.SendHeartbeat(
                token,
                (ok, status, payload, _) =>
                {
                    success = ok;
                    statusCode = status;
                    response = payload;
                });

            if (!success && statusCode == 401)
            {
                SessionManager.Instance.ClearSession();
                _trackedToken = null;
                yield return new WaitForSecondsRealtime(IdlePollSeconds);
                continue;
            }

            if (success && response != null)
            {
                Debug.Log($"[Heartbeat] ok legit={response.isLegit} activeMs={response.cumulativeHeartbeatMs}");
            }

            _nextHeartbeatAt = Time.realtimeSinceStartup + HeartbeatIntervalSeconds;
        }
    }

    private bool HasAuthenticatedSession()
    {
        return SessionManager.Instance != null && SessionManager.Instance.IsAuthenticated();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        _isPaused = pauseStatus;
    }
}
