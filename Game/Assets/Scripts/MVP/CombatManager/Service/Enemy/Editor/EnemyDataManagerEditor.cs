using System.Collections.Generic;
using CombatManager.Service;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyDataManager))]
public class EnemyDataManagerEditor : Editor
{
    private bool showRuntimeSummary = true;
    private bool showSectionSummary = true;
    private bool showActiveChunks = true;
    private bool showRuntimeEnemies = true;

    private bool showOnlyMaterialized;
    private string enemyIdFilter = string.Empty;
    private Vector2 activeChunksScroll;
    private Vector2 runtimeEnemiesScroll;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnemyDataManager manager = (EnemyDataManager)target;

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Enemy Runtime Inspector", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Runtime enemy data is visible only in Play Mode.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        DrawRuntimeSummary(manager);
        EditorGUILayout.Space(8f);
        DrawSectionSummary(manager);
        EditorGUILayout.Space(8f);
        DrawActiveChunks(manager);
        EditorGUILayout.Space(8f);
        DrawRuntimeEnemies(manager);

        EditorGUILayout.EndVertical();

        Repaint();
    }

    private void DrawRuntimeSummary(EnemyDataManager manager)
    {
        showRuntimeSummary = EditorGUILayout.Foldout(showRuntimeSummary, "Runtime Summary", true);
        if (!showRuntimeSummary)
            return;

        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("Chunk Activation Source", manager.FollowChunkLoadingManager ? "ChunkLoadingManager" : "Fallback Player Window");
        EditorGUILayout.LabelField("ChunkLoadingManager Bound", manager.HasChunkLoadingManager ? "Yes" : "No");
        EditorGUILayout.LabelField("Tracked Enemies", manager.RuntimeEnemyCount.ToString());
        EditorGUILayout.LabelField("Materialized Enemies", manager.GetMaterializedEnemyCount().ToString());
        EditorGUILayout.LabelField("Active Chunks", manager.ActiveChunkCount.ToString());
        EditorGUILayout.LabelField("Detected Players", manager.PlayerTargetCount.ToString());

        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh"))
            Repaint();

        if (GUILayout.Button("Log Runtime Snapshot"))
            LogSnapshot(manager);
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    private void DrawSectionSummary(EnemyDataManager manager)
    {
        showSectionSummary = EditorGUILayout.Foldout(showSectionSummary, "Section Data Summary", true);
        if (!showSectionSummary)
            return;

        Dictionary<int, int> countBySection = manager.GetEnemyCountBySection();
        WorldDataManager worldData = WorldDataManager.Instance;

        EditorGUI.indentLevel++;
        if (countBySection.Count == 0)
        {
            EditorGUILayout.HelpBox("No enemy runtime data tracked yet.", MessageType.Info);
            EditorGUI.indentLevel--;
            return;
        }

        List<int> sectionIds = new List<int>(countBySection.Keys);
        sectionIds.Sort();

        for (int i = 0; i < sectionIds.Count; i++)
        {
            int sectionId = sectionIds[i];
            int enemyCount = countBySection[sectionId];

            string sectionName = "Unknown";
            string boundsText = "N/A";

            if (worldData != null)
            {
                WorldSectionConfig config = worldData.GetSectionConfig(sectionId);
                if (config != null)
                {
                    sectionName = config.SectionName;
                    var (min, max) = config.GetWorldBounds(worldData.chunkSizeTiles);
                    boundsText = $"({min.x},{min.y}) to ({max.x},{max.y})";
                }
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Section {sectionId} - {sectionName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Tracked Enemies", enemyCount.ToString());
            EditorGUILayout.LabelField("World Bounds", boundsText);
            EditorGUILayout.EndVertical();
        }

        EditorGUI.indentLevel--;
    }

    private void DrawActiveChunks(EnemyDataManager manager)
    {
        showActiveChunks = EditorGUILayout.Foldout(showActiveChunks, "Active Chunks", true);
        if (!showActiveChunks)
            return;

        List<Vector2Int> chunks = manager.GetActiveChunksSnapshot();
        chunks.Sort((a, b) =>
        {
            int yCompare = b.y.CompareTo(a.y);
            return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
        });

        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Chunk Count", chunks.Count.ToString());

        if (chunks.Count == 0)
        {
            EditorGUILayout.HelpBox("No active chunks currently tracked.", MessageType.Info);
            EditorGUI.indentLevel--;
            return;
        }

        activeChunksScroll = EditorGUILayout.BeginScrollView(activeChunksScroll, GUILayout.Height(150f));
        for (int i = 0; i < chunks.Count; i++)
        {
            Vector2Int chunk = chunks[i];
            EditorGUILayout.LabelField($"({chunk.x}, {chunk.y})", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndScrollView();

        EditorGUI.indentLevel--;
    }

    private void DrawRuntimeEnemies(EnemyDataManager manager)
    {
        showRuntimeEnemies = EditorGUILayout.Foldout(showRuntimeEnemies, "Runtime Enemy Data", true);
        if (!showRuntimeEnemies)
            return;

        List<EnemyDataManager.EnemyRuntimeData> runtime = manager.GetRuntimeDataSnapshot();
        runtime.Sort((a, b) => string.CompareOrdinal(a.runtimeId, b.runtimeId));

        EditorGUI.indentLevel++;

        showOnlyMaterialized = EditorGUILayout.Toggle("Only Materialized", showOnlyMaterialized);
        enemyIdFilter = EditorGUILayout.TextField("EnemyId Filter", enemyIdFilter);

        runtimeEnemiesScroll = EditorGUILayout.BeginScrollView(runtimeEnemiesScroll, GUILayout.Height(260f));

        int displayed = 0;
        for (int i = 0; i < runtime.Count; i++)
        {
            EnemyDataManager.EnemyRuntimeData data = runtime[i];
            if (showOnlyMaterialized && !data.isMaterialized)
                continue;

            if (!string.IsNullOrWhiteSpace(enemyIdFilter) &&
                (data.enemyId == null || data.enemyId.IndexOf(enemyIdFilter, System.StringComparison.OrdinalIgnoreCase) < 0))
                continue;

            displayed++;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(data.enemyId, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("RuntimeId", data.runtimeId);
            EditorGUILayout.LabelField("Section", data.sectionId.ToString());
            EditorGUILayout.LabelField("Chunk", $"({data.chunkPos.x}, {data.chunkPos.y})");
            EditorGUILayout.LabelField("World Position", $"({data.position.x:F2}, {data.position.y:F2}, {data.position.z:F2})");
            EditorGUILayout.LabelField("Materialized", data.isMaterialized ? "Yes" : "No");
            EditorGUILayout.EndVertical();
        }

        if (displayed == 0)
            EditorGUILayout.HelpBox("No runtime enemies match current filters.", MessageType.Info);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.LabelField("Displayed", displayed.ToString());

        EditorGUI.indentLevel--;
    }

    private static void LogSnapshot(EnemyDataManager manager)
    {
        List<EnemyDataManager.EnemyRuntimeData> runtime = manager.GetRuntimeDataSnapshot();
        Debug.Log($"[EnemyDataManagerEditor] Runtime={runtime.Count}, Materialized={manager.GetMaterializedEnemyCount()}, ActiveChunks={manager.ActiveChunkCount}, Players={manager.PlayerTargetCount}");

        for (int i = 0; i < runtime.Count; i++)
        {
            EnemyDataManager.EnemyRuntimeData data = runtime[i];
            Debug.Log($"[EnemyDataManagerEditor] {data.enemyId} | {data.runtimeId} | section={data.sectionId} | chunk=({data.chunkPos.x},{data.chunkPos.y}) | pos=({data.position.x:F2},{data.position.y:F2}) | materialized={data.isMaterialized}");
        }
    }
}
