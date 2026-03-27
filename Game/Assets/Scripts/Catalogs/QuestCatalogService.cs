using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Singleton MonoBehaviour — the client-side quest catalog.
/// Fetches quest template data from GET /game-data/quests/catalog and provides
/// typed lookups by questId.
///
/// Usage:
///   1. Add to a persistent GameObject in the DownLoadResource scene.
///   2. Await <see cref="IsReady"/> == true before calling any Get methods.
///   3. Use <see cref="GetQuest"/> or <see cref="GetAllQuests"/> to retrieve templates.
/// </summary>
public class QuestCatalogService : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static QuestCatalogService Instance { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly Dictionary<string, QuestCatalogData> _catalog = new();

    /// <summary>True once catalog JSON is fully parsed and indexed.</summary>
    public bool IsReady { get; private set; }

    private const int   MAX_RETRIES  = 3;
    private const float RETRY_DELAY  = 2f;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CatalogProgressManager.NotifyStarted();
        StartCoroutine(FetchCatalog());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Returns the quest template for the given questId, or null if not found.</summary>
    public QuestCatalogData GetQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return null;
        _catalog.TryGetValue(questId, out var data);
        return data;
    }

    /// <summary>Returns a copy of all quest templates loaded into the catalog.</summary>
    public List<QuestCatalogData> GetAllQuests()
        => new List<QuestCatalogData>(_catalog.Values);

    public void Retry()
    {
        if (IsReady) return;
        CatalogProgressManager.NotifyStarted();
        StartCoroutine(FetchCatalog());
    }

    // ── Loading ───────────────────────────────────────────────────────────────
    private IEnumerator FetchCatalog()
    {
        IsReady = false;
        _catalog.Clear();

        string url = $"{AppConfig.ApiBaseUrl}/game-data/quests/catalog";
        QuestCatalogResponse response = null;

        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"[QuestCatalogService] Attempt {attempt}/{MAX_RETRIES} failed: {request.error}");
                if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
                continue;
            }

            bool parseOk = false;
            try
            {
                response = JsonConvert.DeserializeObject<QuestCatalogResponse>(
                    request.downloadHandler.text);
                parseOk = response != null;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(
                    $"[QuestCatalogService] JSON parse error (attempt {attempt}): {ex.Message}");
            }

            if (parseOk) break;
            if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
        }

        if (response?.quests == null)
        {
            Debug.LogError($"[QuestCatalogService] All {MAX_RETRIES} attempts failed for {url}");
            CatalogProgressManager.NotifyFailed("Quest Catalog");
            yield break;
        }

        int loaded = 0;
        foreach (var quest in response.quests)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.questId))
            {
                Debug.LogWarning("[QuestCatalogService] Skipping entry with missing questId.");
                continue;
            }

            _catalog[quest.questId] = quest;
            loaded++;
        }

        IsReady = true;
        CatalogProgressManager.ReportProgress(1, 1, "Quest Catalog");
        CatalogProgressManager.NotifyCompleted();
        Debug.Log($"[QuestCatalogService] Catalog ready with {loaded} quest(s).");
    }

    // ── Response Wrapper ──────────────────────────────────────────────────────
    [System.Serializable]
    private class QuestCatalogResponse
    {
        public List<QuestCatalogData> quests;
    }
}

/// <summary>
/// Plain C# class matching the server-side Quest document shape.
/// Field names use Newtonsoft.Json's default case-insensitive matching,
/// so server fields NPCName → NPCName and Weight → Weight deserialize correctly.
/// </summary>
[System.Serializable]
public class QuestCatalogData
{
    public string questId;
    public string questName;
    public string description;

    /// <summary>Name of the NPC who gives this quest (server field: NPCName).</summary>
    [JsonProperty("NPCName")]
    public string NPCName;

    /// <summary>Sorting/priority weight (server field: Weight).</summary>
    [JsonProperty("Weight")]
    public float Weight;

    /// <summary>questId of the next quest in the chain, or null.</summary>
    public string nextQuestId;

    public QuestReward reward;

    /// <summary>
    /// Raw status string from the server: "inactive" | "active" | "completed" | "failed".
    /// Convert to <see cref="QuestStatus"/> via <see cref="ParseStatus"/> as needed.
    /// </summary>
    public string status;

    public List<QuestObjective> objectives;

    /// <summary>Maps the raw server status string to the local QuestStatus enum.</summary>
    public QuestStatus ParseStatus()
    {
        return status?.ToLowerInvariant() switch
        {
            "active"    => QuestStatus.Active,
            "completed" => QuestStatus.Completed,
            "failed"    => QuestStatus.TurnedIn,
            _           => QuestStatus.NotAccepted,
        };
    }
}
