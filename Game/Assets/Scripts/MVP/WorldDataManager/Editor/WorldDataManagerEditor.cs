    using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(WorldDataManager))]
public class WorldDataManagerEditor : Editor
{
    private bool showChunkData = true;
    private bool showEmptyChunks = false;
    private int selectedSectionFilter = -1; // -1 = all sections
    private Vector2 scrollPosition;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("World data only available during Play Mode", MessageType.Info);
            return;
        }

        WorldDataManager manager = (WorldDataManager)target;

        if (!manager.IsInitialized)
        {
            EditorGUILayout.HelpBox("WorldDataManager not initialized yet", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Runtime Debug Data", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        // Statistics
        WorldDataStats stats = manager.GetStats();
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Sections: {stats.TotalSections}");
        EditorGUILayout.LabelField($"Total Chunks: {stats.TotalChunks}");
        EditorGUILayout.LabelField($"Loaded Chunks: {stats.LoadedChunks}");
        EditorGUILayout.LabelField($"Chunks with Crops: {stats.ChunksWithCrops}");
        EditorGUILayout.LabelField($"Total Crops: {stats.TotalCrops}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Memory Usage: {stats.MemoryUsageMB:F3} MB");

        EditorGUILayout.Space(5);

        // Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh"))
        {
            Repaint();
        }
        if (GUILayout.Button("Log Stats to Console"))
        {
            manager.LogStats();
        }
        if (GUILayout.Button("Clear All Data"))
        {
            if (EditorUtility.DisplayDialog("Clear All Data", 
                "Are you sure you want to clear all crop data?", "Yes", "Cancel"))
            {
                manager.ClearAllData();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Chunk data display
        showChunkData = EditorGUILayout.Foldout(showChunkData, "Chunk Data Details", true);
        
        if (showChunkData)
        {
            EditorGUILayout.BeginHorizontal();
            showEmptyChunks = EditorGUILayout.Toggle("Show Empty Chunks", showEmptyChunks);
            
            // Section filter dropdown
            string[] sectionNames = new string[manager.sectionConfigs.Count + 1];
            sectionNames[0] = "All Sections";
            for (int i = 0; i < manager.sectionConfigs.Count; i++)
            {
                sectionNames[i + 1] = manager.sectionConfigs[i].SectionName;
            }
            selectedSectionFilter = EditorGUILayout.Popup("Filter:", selectedSectionFilter + 1, sectionNames) - 1;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Scrollable chunk list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));

            int displayedChunks = 0;
            
            for (int s = 0; s < manager.sectionConfigs.Count; s++)
            {
                if (selectedSectionFilter != -1 && selectedSectionFilter != s)
                    continue;

                var sectionConfig = manager.sectionConfigs[s];
                if (!sectionConfig.IsActive) continue;

                var section = manager.GetSection(sectionConfig.SectionId);
                if (section == null) continue;

                EditorGUILayout.LabelField($"━━━ {sectionConfig.SectionName} ━━━", EditorStyles.boldLabel);

                foreach (var chunkPair in section)
                {
                    Vector2Int chunkPos = chunkPair.Key;
                    CropChunkData chunk = chunkPair.Value;
                    int cropCount = chunk.GetCropCount();

                    if (!showEmptyChunks && cropCount == 0)
                        continue;

                    displayedChunks++;

                    // Chunk header
                    Color bgColor = cropCount > 0 ? new Color(0.3f, 0.6f, 0.3f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
                    GUI.backgroundColor = bgColor;
                    EditorGUILayout.BeginVertical("box");
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Chunk ({chunk.ChunkX}, {chunk.ChunkY})", EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField($"Crops: {cropCount}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Loaded: {chunk.IsLoaded}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Dirty: {chunk.IsDirty}", GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();

                    // World bounds
                    Vector3 worldMin = manager.ChunkToWorldPosition(chunkPos);
                    Vector3 worldMax = worldMin + new Vector3(manager.chunkSizeTiles, manager.chunkSizeTiles, 0);
                    EditorGUILayout.LabelField($"World: ({worldMin.x:F0}, {worldMin.y:F0}) to ({worldMax.x:F0}, {worldMax.y:F0})", 
                        EditorStyles.miniLabel);

                    // Crop details
                    if (cropCount > 0)
                    {
                        EditorGUI.indentLevel++;
                        var crops = chunk.GetAllCrops();
                        
                        foreach (var crop in crops)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"  🌱 Type {crop.CropTypeID}", GUILayout.Width(100));
                            EditorGUILayout.LabelField($"Stage: {crop.CropStage}", GUILayout.Width(80));
                            EditorGUILayout.LabelField($"Pos: ({crop.WorldX}, {crop.WorldY})", GUILayout.Width(150));
                            
                            // Button to highlight position in scene
                            if (GUILayout.Button("📍", GUILayout.Width(30)))
                            {
                                // Focus scene view on this position
                                Vector3 worldPos = new Vector3(crop.WorldX, crop.WorldY, 0);
                                SceneView.lastActiveSceneView.LookAt(worldPos);
                                Debug.Log($"Crop at ({crop.WorldX}, {crop.WorldY})");
                            }
                            
                            EditorGUILayout.EndHorizontal();
                        }
                        
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.Space(5);
            }

            if (displayedChunks == 0)
            {
                EditorGUILayout.HelpBox("No chunks to display. " + 
                    (showEmptyChunks ? "No chunks exist." : "Enable 'Show Empty Chunks' to see all."), 
                    MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();

        // Auto-refresh in play mode
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
