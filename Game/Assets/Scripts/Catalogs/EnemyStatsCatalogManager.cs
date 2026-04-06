using System;
using System.Collections;
using System.Collections.Generic;
using CombatManager.Presenter;
using CombatManager.SO;
using CombatManager.Service;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Runtime enemy stats catalog loaded from server.
/// Unity keeps prefab/animation ownership while this catalog overrides gameplay tuning.
/// </summary>
public class EnemyStatsCatalogManager : MonoBehaviour
{
    public static EnemyStatsCatalogManager Instance { get; private set; }

    [SerializeField] private float refreshIntervalSeconds = 10f;

    private readonly Dictionary<string, EnemyStatsCatalogEntry> _entries =
        new Dictionary<string, EnemyStatsCatalogEntry>(StringComparer.OrdinalIgnoreCase);

    private bool _isRefreshing;
    private string _lastCatalogHash = string.Empty;
    private bool _bootstrapInProgress;
    private bool _hasBootstrapped;
    private string _bootstrappedWorldId = string.Empty;
    private bool _lastRefreshSucceeded;

    public bool IsReady { get; private set; }

    public static EnemyStatsCatalogManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        Instance = FindAnyObjectByType<EnemyStatsCatalogManager>();
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("EnemyStatsCatalogManager");
        Instance = go.AddComponent<EnemyStatsCatalogManager>();
        DontDestroyOnLoad(go);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator BootstrapAndRegisterIfHost(string worldId, string authToken)
    {
        string normalizedWorldId = (worldId ?? string.Empty).Trim();
        if (_hasBootstrapped &&
            string.Equals(_bootstrappedWorldId, normalizedWorldId, StringComparison.OrdinalIgnoreCase))
        {
            if (!_isRefreshing)
                StartCoroutine(PollRefreshLoop());
            yield break;
        }

        if (_bootstrapInProgress)
            yield break;

        _bootstrapInProgress = true;
        IsReady = false;

        yield return RefreshCatalogFromServer();
        if (!_lastRefreshSucceeded)
        {
            IsReady = false;
            _bootstrapInProgress = false;
            yield break;
        }

        if (Photon.Pun.PhotonNetwork.IsMasterClient)
        {
            yield return RegisterMissingFromSpawner(worldId, authToken);
            yield return RefreshCatalogFromServer();
            if (!_lastRefreshSucceeded)
            {
                IsReady = false;
                _bootstrapInProgress = false;
                yield break;
            }
        }

        IsReady = true;
        _hasBootstrapped = true;
        _bootstrappedWorldId = normalizedWorldId;
        _bootstrapInProgress = false;

        if (!_isRefreshing)
            StartCoroutine(PollRefreshLoop());
    }

    public bool TryGetEnemyStats(string enemyId, out EnemyStatsCatalogEntry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(enemyId))
            return false;

        return _entries.TryGetValue(enemyId, out entry) && entry != null;
    }

    private IEnumerator PollRefreshLoop()
    {
        _isRefreshing = true;

        while (this != null && gameObject != null)
        {
            yield return new WaitForSeconds(Mathf.Max(2f, refreshIntervalSeconds));
            yield return RefreshCatalogFromServer();
        }

        _isRefreshing = false;
    }

    private IEnumerator RefreshCatalogFromServer()
    {
        _lastRefreshSucceeded = false;

        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/game-data/enemy-stats/catalog";
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[EnemyStatsCatalogManager] Catalog fetch failed: {request.error}");
            yield break;
        }

        EnemyStatsCatalogResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<EnemyStatsCatalogResponse>(request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[EnemyStatsCatalogManager] Catalog parse failed: {ex.Message}");
            yield break;
        }

        if (response?.enemies == null)
            yield break;

        string hash = request.downloadHandler.text;
        if (hash == _lastCatalogHash)
        {
            _lastRefreshSucceeded = true;
            yield break;
        }

        _lastCatalogHash = hash;

        _entries.Clear();
        for (int i = 0; i < response.enemies.Count; i++)
        {
            EnemyStatsCatalogEntry entry = response.enemies[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.enemyId))
                continue;

            string normalized = entry.enemyId.Trim().ToLowerInvariant();
            entry.enemyId = normalized;
            _entries[normalized] = entry;
        }

        ApplyToKnownEnemyDefinitions();
        ApplyToActiveEnemies();
        _lastRefreshSucceeded = true;
        Debug.Log($"[EnemyStatsCatalogManager] Loaded {_entries.Count} enemy stat entries.");
    }

    private IEnumerator RegisterMissingFromSpawner(string worldId, string authToken)
    {
        EnemySpawnerManager spawner = EnemySpawnerManager.Instance;
        if (spawner == null)
        {
            Debug.LogWarning("[EnemyStatsCatalogManager] EnemySpawnerManager not found, skip register-missing.");
            yield break;
        }

        List<EnemyDataSO> definitions = spawner.GetUniqueEnemyDefinitions();
        if (definitions == null || definitions.Count == 0)
            yield break;

        List<EnemyStatsRegisterEntryPayload> missing = new List<EnemyStatsRegisterEntryPayload>();
        for (int i = 0; i < definitions.Count; i++)
        {
            EnemyDataSO so = definitions[i];
            if (so == null || string.IsNullOrWhiteSpace(so.enemyId))
                continue;

            string enemyId = so.enemyId.Trim().ToLowerInvariant();
            if (_entries.ContainsKey(enemyId))
                continue;

            missing.Add(EnemyStatsRegisterEntryPayload.FromSO(so));
        }

        if (missing.Count == 0)
            yield break;

        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/player-data/world/enemy-stats/register-missing";
        EnemyStatsRegisterBatchPayload payload = new EnemyStatsRegisterBatchPayload
        {
            worldId = worldId,
            entries = missing,
        };

        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrWhiteSpace(authToken))
            request.SetRequestHeader("Authorization", "Bearer " + authToken);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        request.certificateHandler = new AcceptAllCertificatesHandler();
#endif

        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[EnemyStatsCatalogManager] register-missing failed: {request.responseCode} {request.error}");
            yield break;
        }

        Debug.Log($"[EnemyStatsCatalogManager] register-missing completed for {missing.Count} new enemies.");
    }

    private void ApplyToActiveEnemies()
    {
        EnemyPresenter[] active = FindObjectsOfType<EnemyPresenter>(true);
        for (int i = 0; i < active.Length; i++)
        {
            EnemyPresenter presenter = active[i];
            if (presenter == null || !presenter.IsInitialized())
                continue;

            string enemyId = presenter.GetEnemyId();
            if (string.IsNullOrWhiteSpace(enemyId))
                continue;

            if (!TryGetEnemyStats(enemyId, out EnemyStatsCatalogEntry entry))
                continue;

            presenter.ApplyCatalogStatsOverride(entry, preserveHealthRatio: true);
        }
    }

    private void ApplyToKnownEnemyDefinitions()
    {
        EnemySpawnerManager spawner = EnemySpawnerManager.Instance;
        if (spawner == null)
            return;

        List<EnemyDataSO> definitions = spawner.GetUniqueEnemyDefinitions();
        for (int i = 0; i < definitions.Count; i++)
        {
            EnemyDataSO so = definitions[i];
            if (so == null || string.IsNullOrWhiteSpace(so.enemyId))
                continue;

            if (!TryGetEnemyStats(so.enemyId, out EnemyStatsCatalogEntry entry))
                continue;

            so.enemyName = string.IsNullOrWhiteSpace(entry.enemyName) ? so.enemyName : entry.enemyName;
            so.respawnDelaySeconds = Mathf.Max(0f, entry.respawnDelaySeconds);
            so.maxHealth = Mathf.Max(1, entry.maxHealth);
            so.damageAmount = Mathf.Max(1, entry.damageAmount);
            so.baseExp = Mathf.Max(1, entry.baseExp);
            so.knockbackForce = Mathf.Max(0f, entry.knockbackForce);
            so.enableOutOfCombatRegen = entry.enableOutOfCombatRegen;
            so.regenDelaySeconds = Mathf.Max(0f, entry.regenDelaySeconds);
            so.regenHpPerSecond = Mathf.Max(0f, entry.regenHpPerSecond);
            so.regenRequireNearGuardAnchor = entry.regenRequireNearGuardAnchor;
            so.regenGuardProximity = Mathf.Max(0f, entry.regenGuardProximity);
            so.moveSpeed = Mathf.Max(0f, entry.moveSpeed);
            so.chaseSpeed = Mathf.Max(0f, entry.chaseSpeed);
            so.wanderSpeed = Mathf.Max(0f, entry.wanderSpeed);
            so.wanderRange = Mathf.Max(0f, entry.wanderRange);
            so.enableSeparation = entry.enableSeparation;
            so.separationRadius = Mathf.Max(0f, entry.separationRadius);
            so.separationForce = Mathf.Max(0f, entry.separationForce);
            so.detectionRange = Mathf.Max(0f, entry.detectionRange);
            so.attackRange = Mathf.Max(0f, entry.attackRange);
            so.fieldOfViewAngle = Mathf.Clamp(entry.fieldOfViewAngle, 0f, 360f);
            so.guardDuration = Mathf.Max(0f, entry.guardDuration);
            so.guardLookDuration = Mathf.Max(0f, entry.guardLookDuration);
            so.damageThrottleTime = Mathf.Max(0f, entry.damageThrottleTime);
            so.useActiveAttack = entry.useActiveAttack;
            so.attackCooldown = Mathf.Max(0f, entry.attackCooldown);
            so.attackRecovery = Mathf.Max(0f, entry.attackRecovery);
            so.attackFrontDotThreshold = Mathf.Clamp(entry.attackFrontDotThreshold, -1f, 1f);
            so.knockbackDuration = Mathf.Max(0f, entry.knockbackDuration);
            so.squashPixels = Mathf.Max(0f, entry.squashPixels);
            so.stretchPixels = Mathf.Max(0f, entry.stretchPixels);
            so.waveDuration = Mathf.Max(0f, entry.waveDuration);
            so.flashDuration = Mathf.Max(0f, entry.flashDuration);
            so.flashCount = Mathf.Max(0, entry.flashCount);
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private sealed class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
#endif
}

[Serializable]
public class EnemyStatsCatalogResponse
{
    public List<EnemyStatsCatalogEntry> enemies;
}

[Serializable]
public class EnemyStatsCatalogEntry
{
    public string enemyId;
    public string enemyName;
    public float respawnDelaySeconds;
    public int maxHealth;
    public int damageAmount;
    public int baseExp;
    public float knockbackForce;
    public bool enableOutOfCombatRegen;
    public float regenDelaySeconds;
    public float regenHpPerSecond;
    public bool regenRequireNearGuardAnchor;
    public float regenGuardProximity;
    public float moveSpeed;
    public float chaseSpeed;
    public float wanderSpeed;
    public float wanderRange;
    public bool enableSeparation;
    public float separationRadius;
    public float separationForce;
    public float detectionRange;
    public float attackRange;
    public float fieldOfViewAngle;
    public float guardDuration;
    public float guardLookDuration;
    public float damageThrottleTime;
    public bool useActiveAttack;
    public float attackCooldown;
    public float attackRecovery;
    public float attackFrontDotThreshold;
    public float knockbackDuration;
    public float squashPixels;
    public float stretchPixels;
    public float waveDuration;
    public float flashDuration;
    public int flashCount;
    public string updatedAt;
}

[Serializable]
public class EnemyStatsRegisterBatchPayload
{
    public string worldId;
    public List<EnemyStatsRegisterEntryPayload> entries;
}

[Serializable]
public class EnemyStatsRegisterEntryPayload
{
    public string enemyId;
    public string enemyName;
    public float respawnDelaySeconds;
    public int maxHealth;
    public int damageAmount;
    public int baseExp;
    public float knockbackForce;
    public bool enableOutOfCombatRegen;
    public float regenDelaySeconds;
    public float regenHpPerSecond;
    public bool regenRequireNearGuardAnchor;
    public float regenGuardProximity;
    public float moveSpeed;
    public float chaseSpeed;
    public float wanderSpeed;
    public float wanderRange;
    public bool enableSeparation;
    public float separationRadius;
    public float separationForce;
    public float detectionRange;
    public float attackRange;
    public float fieldOfViewAngle;
    public float guardDuration;
    public float guardLookDuration;
    public float damageThrottleTime;
    public bool useActiveAttack;
    public float attackCooldown;
    public float attackRecovery;
    public float attackFrontDotThreshold;
    public float knockbackDuration;
    public float squashPixels;
    public float stretchPixels;
    public float waveDuration;
    public float flashDuration;
    public int flashCount;

    public static EnemyStatsRegisterEntryPayload FromSO(EnemyDataSO so)
    {
        return new EnemyStatsRegisterEntryPayload
        {
            enemyId = so.enemyId?.Trim().ToLowerInvariant() ?? string.Empty,
            enemyName = string.IsNullOrWhiteSpace(so.enemyName) ? so.enemyId : so.enemyName,
            respawnDelaySeconds = so.respawnDelaySeconds,
            maxHealth = so.maxHealth,
            damageAmount = so.damageAmount,
            baseExp = so.baseExp,
            knockbackForce = so.knockbackForce,
            enableOutOfCombatRegen = so.enableOutOfCombatRegen,
            regenDelaySeconds = so.regenDelaySeconds,
            regenHpPerSecond = so.regenHpPerSecond,
            regenRequireNearGuardAnchor = so.regenRequireNearGuardAnchor,
            regenGuardProximity = so.regenGuardProximity,
            moveSpeed = so.moveSpeed,
            chaseSpeed = so.chaseSpeed,
            wanderSpeed = so.wanderSpeed,
            wanderRange = so.wanderRange,
            enableSeparation = so.enableSeparation,
            separationRadius = so.separationRadius,
            separationForce = so.separationForce,
            detectionRange = so.detectionRange,
            attackRange = so.attackRange,
            fieldOfViewAngle = so.fieldOfViewAngle,
            guardDuration = so.guardDuration,
            guardLookDuration = so.guardLookDuration,
            damageThrottleTime = so.damageThrottleTime,
            useActiveAttack = so.useActiveAttack,
            attackCooldown = so.attackCooldown,
            attackRecovery = so.attackRecovery,
            attackFrontDotThreshold = so.attackFrontDotThreshold,
            knockbackDuration = so.knockbackDuration,
            squashPixels = so.squashPixels,
            stretchPixels = so.stretchPixels,
            waveDuration = so.waveDuration,
            flashDuration = so.flashDuration,
            flashCount = so.flashCount,
        };
    }
}
