using AchievementManager.Presenter;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AchievementTrackerPresenter))]
public class AchievementTrackerPresenterEditor : Editor
{
    private bool showPendingItems = true;
    private bool autoRefresh = true;
    private float autoRefreshInterval = 0.5f;
    private float nextRepaintTime;
    private Vector2 pendingScroll;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AchievementTrackerPresenter tracker = (AchievementTrackerPresenter)target;

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Achievement Tracker Debug", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Runtime tracker state is available only in Play Mode.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        if (autoRefresh)
            TryAutoRepaint();

        AchievementTrackerPresenter.AchievementTrackerDebugSnapshot snapshot = tracker.GetDebugSnapshot();

        DrawStatus(snapshot);

        EditorGUILayout.Space(6f);
        DrawRuntimeSettings(snapshot);

        EditorGUILayout.Space(6f);
        DrawMemory(snapshot);

        EditorGUILayout.Space(6f);
        DrawLastFlush(snapshot);

        EditorGUILayout.Space(6f);
        DrawActions(tracker);

        EditorGUILayout.Space(6f);
        DrawPending(snapshot);

        EditorGUILayout.EndVertical();
    }

    private void DrawStatus(AchievementTrackerPresenter.AchievementTrackerDebugSnapshot snapshot)
    {
        EditorGUILayout.LabelField("State", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Initialized: {(snapshot.isInitialized ? "Yes" : "No")}");
        EditorGUILayout.LabelField($"Model Loaded: {(snapshot.modelLoaded ? "Yes" : "No")}");
        EditorGUILayout.LabelField($"Model Fetching: {(snapshot.modelFetching ? "Yes" : "No")}");
        EditorGUILayout.LabelField($"Achievements Cached: {snapshot.achievementCount}");
        EditorGUILayout.LabelField($"Local Counters: {snapshot.localCounterCount}");
        EditorGUILayout.LabelField($"Pending Updates: {snapshot.pendingCount}");
        EditorGUILayout.LabelField($"Flushing: {(snapshot.isFlushing ? "Yes" : "No")}");
        EditorGUILayout.LabelField($"Queued While Flushing: {(snapshot.queuedWhileFlushing ? "Yes" : "No")}");
        EditorGUILayout.LabelField($"Retry Attempt: {snapshot.retryAttempt}");
    }

    private void DrawRuntimeSettings(AchievementTrackerPresenter.AchievementTrackerDebugSnapshot snapshot)
    {
        EditorGUILayout.LabelField("Runtime Settings", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Debounce Delay: {snapshot.debounceDelay:0.00}s");
        EditorGUILayout.LabelField($"Autosave Interval: {snapshot.autosaveInterval:0.00}s");
        EditorGUILayout.LabelField($"Immediate Flush On Unlock Candidate: {(snapshot.flushImmediatelyOnUnlockCandidate ? "Yes" : "No")}");
        EditorGUILayout.LabelField($"Persist Pending Across Sessions: {(snapshot.persistPendingAcrossSessions ? "Yes" : "No")}");
        EditorGUILayout.LabelField($"Retry Base Delay: {snapshot.retryBaseDelay:0.00}s");
        EditorGUILayout.LabelField($"Retry Max Delay: {snapshot.retryMaxDelay:0.00}s");
    }

    private void DrawLastFlush(AchievementTrackerPresenter.AchievementTrackerDebugSnapshot snapshot)
    {
        EditorGUILayout.LabelField("Last Flush", EditorStyles.boldLabel);

        bool hasFlush = snapshot.lastFlushRealtime >= 0f;
        if (!hasFlush)
        {
            EditorGUILayout.LabelField("No flush recorded yet.");
            return;
        }

        float secondsAgo = Mathf.Max(0f, Time.realtimeSinceStartup - snapshot.lastFlushRealtime);
        EditorGUILayout.LabelField($"Source: {snapshot.lastFlushSource}");
        EditorGUILayout.LabelField($"When: {secondsAgo:0.0}s ago");
        EditorGUILayout.LabelField($"Submitted: {snapshot.lastFlushSubmittedCount}");
        EditorGUILayout.LabelField($"Succeeded: {snapshot.lastFlushSucceededCount}");
        EditorGUILayout.LabelField($"Failed: {snapshot.lastFlushFailedCount}");

        if (!string.IsNullOrEmpty(snapshot.lastFlushError))
            EditorGUILayout.HelpBox(snapshot.lastFlushError, MessageType.Warning);
    }

    private void DrawMemory(AchievementTrackerPresenter.AchievementTrackerDebugSnapshot snapshot)
    {
        EditorGUILayout.LabelField("Memory (Approx)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Pending Updates: {FormatBytes(snapshot.approxPendingBytes)}");
        EditorGUILayout.LabelField($"Local Counters: {FormatBytes(snapshot.approxCounterBytes)}");
        EditorGUILayout.LabelField($"Achievements Cache: {FormatBytes(snapshot.approxAchievementBytes)}");
        EditorGUILayout.LabelField($"Total Tracker Data: {FormatBytes(snapshot.approxTotalBytes)}");
        EditorGUILayout.HelpBox("Values are estimates based on in-memory data structures and string lengths, useful for relative tracking over time.", MessageType.None);
    }

    private void DrawActions(AchievementTrackerPresenter tracker)
    {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh"))
            Repaint();

        if (GUILayout.Button("Force Flush"))
            tracker.ForceFlush();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reconcile Buffered"))
            tracker.ReconcileBufferedProgressAfterLoad();

        if (GUILayout.Button("Log Snapshot"))
            tracker.DebugLogSnapshot();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3f);
        autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
        if (autoRefresh)
            autoRefreshInterval = Mathf.Clamp(EditorGUILayout.Slider("Refresh Every (s)", autoRefreshInterval, 0.2f, 2f), 0.2f, 2f);
    }

    private void DrawPending(AchievementTrackerPresenter.AchievementTrackerDebugSnapshot snapshot)
    {
        showPendingItems = EditorGUILayout.Foldout(showPendingItems, "Pending Update Details", true);
        if (!showPendingItems)
            return;

        if (snapshot.pendingItems == null || snapshot.pendingItems.Count == 0)
        {
            EditorGUILayout.HelpBox("No pending updates.", MessageType.Info);
            return;
        }

        pendingScroll = EditorGUILayout.BeginScrollView(pendingScroll, GUILayout.Height(260f));

        for (int i = 0; i < snapshot.pendingItems.Count; i++)
        {
            AchievementTrackerPresenter.PendingUpdateDebugItem item = snapshot.pendingItems[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{i + 1}. {item.achievementId} [req {item.requirementIndex}]", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Key: {item.key}");
            EditorGUILayout.LabelField($"Rule: {item.requirementType}" +
                                       (string.IsNullOrEmpty(item.requirementEntityId) ? string.Empty : $" ({item.requirementEntityId})"));
            EditorGUILayout.LabelField($"Progress local/server/pending/target: {item.localProgress}/{item.serverProgress}/{item.pendingProgress}/{item.target}");
            EditorGUILayout.LabelField($"Achieved: {(item.isAchieved ? "Yes" : "No")}");
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }

        EditorGUILayout.EndScrollView();
    }

    private void TryAutoRepaint()
    {
        if (Event.current != null && Event.current.type == EventType.Repaint)
            return;

        float now = (float)EditorApplication.timeSinceStartup;
        if (now < nextRepaintTime)
            return;

        nextRepaintTime = now + autoRefreshInterval;
        Repaint();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        float kb = bytes / 1024f;
        if (kb < 1024f)
            return $"{kb:0.00} KB ({bytes} B)";

        float mb = kb / 1024f;
        return $"{mb:0.000} MB ({bytes} B)";
    }
}
