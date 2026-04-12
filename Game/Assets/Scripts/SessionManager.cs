using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    private static SessionManager _instance;
    private const string DefaultAuthSceneName = "AuthScene";
    private bool _authRedirectInProgress;
    
    public static SessionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SessionManager");
                _instance = go.AddComponent<SessionManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Session data
    [SerializeField] private string jwtToken;
    [SerializeField] private string userId;
    [SerializeField] private string username;

    public string JwtToken => jwtToken;
    public string UserId => userId;
    public string Username => username;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetAuthenticationData(string token, string userIdValue, string usernameValue)
    {
        jwtToken = token;
        userId = userIdValue;
        username = usernameValue;
        Debug.Log($"Session data stored for user: {username}");
    }

    public void ClearSession()
    {
        jwtToken = null;
        userId = null;
        username = null;
        Debug.Log("Session cleared");
    }

    public void HandleJwtExpired(string reason = null)
    {
        if (_authRedirectInProgress)
            return;

        _authRedirectInProgress = true;
        Debug.LogWarning($"[SessionManager] JWT expired. Redirecting to {DefaultAuthSceneName}. Reason: {reason ?? "unknown"}");

        ClearSession();

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == DefaultAuthSceneName)
            return;

        SceneManager.LoadScene(DefaultAuthSceneName);
    }

    public bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(jwtToken) && !string.IsNullOrEmpty(userId);
    }

    void OnApplicationQuit()
    {
        ClearSession();
    }
}
