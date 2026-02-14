using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(WorldDataManager))]
public class WorldDataManagerEditor : Editor
{
    private bool showChunkData = true;
    private bool showEmptyChunks = false;
    private bool showTilledOnly = true;
    private bool showCropsOnly = true;
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
        EditorGUILayout.LabelField($"Total Tilled Tiles: {stats.TotalTilledTiles}", EditorStyles.boldLabel);
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
            // Legend
            EditorGUILayout.HelpBox(
                "🌱 Crop with Tilled Ground (most common)\n" +
                "🌿 Crop without Tilled (unusual)\n" +
                "🟫 Tilled Ground Only (no crop)\n" +
                "⬜ Empty Tile (should not occur)", 
                MessageType.None);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            showEmptyChunks = EditorGUILayout.Toggle("Show Empty Chunks", showEmptyChunks);
            showTilledOnly = EditorGUILayout.Toggle("Show Tilled Only", showTilledOnly);
            showCropsOnly = EditorGUILayout.Toggle("Show Crops", showCropsOnly);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
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
                    int totalTiles = chunk.tiles.Count;
                    
                    // Count tile states
                    int cropsWithTilled = 0;
                    int cropsOnly = 0;
                    int tilledOnly = 0;
                    int emptyTiles = 0;
                    
                    foreach (var tile in chunk.tiles.Values)
                    {
                        if (tile.HasCrop && tile.IsTilled) cropsWithTilled++;
                        else if (tile.HasCrop) cropsOnly++;
                        else if (tile.IsTilled) tilledOnly++;
                        else emptyTiles++;
                    }

                    if (!showEmptyChunks && totalTiles == 0)
                        continue;

                    displayedChunks++;

                    // Chunk header with color based on content
                    Color bgColor = (cropsWithTilled > 0) ? new Color(0.3f, 0.6f, 0.3f, 0.3f) : 
                                    (tilledOnly > 0) ? new Color(0.6f, 0.5f, 0.3f, 0.3f) :
                                    new Color(0.3f, 0.3f, 0.3f, 0.2f);
                    GUI.backgroundColor = bgColor;
                    EditorGUILayout.BeginVertical("box");
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Chunk ({chunk.ChunkX}, {chunk.ChunkY})", EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField($"Tiles: {totalTiles}", GUILayout.Width(80));
                    if (cropsWithTilled > 0)
                        EditorGUILayout.LabelField($"🌱 {cropsWithTilled}", GUILayout.Width(60));
                    if (tilledOnly > 0)
                        EditorGUILayout.LabelField($"🟫 {tilledOnly}", GUILayout.Width(60));
                    if (cropsOnly > 0)
                        EditorGUILayout.LabelField($"🌿 {cropsOnly}", GUILayout.Width(60));
                    EditorGUILayout.LabelField($"Dirty: {(chunk.IsDirty ? "✓" : "✗")}", GUILayout.Width(70));
                    EditorGUILayout.EndHorizontal();

                    // World bounds
                    Vector3 worldMin = manager.ChunkToWorldPosition(chunkPos);
                    Vector3 worldMax = worldMin + new Vector3(manager.chunkSizeTiles, manager.chunkSizeTiles, 0);
                    EditorGUILayout.LabelField($"World: ({worldMin.x:F0}, {worldMin.y:F0}) to ({worldMax.x:F0}, {worldMax.y:F0})", 
                        EditorStyles.miniLabel);

                    // Tile details - unified display
                    if (totalTiles > 0)
                    {
                        EditorGUI.indentLevel++;
                        
                        foreach (var tilePair in chunk.tiles)
                        {
                            var tile = tilePair.Value;
                            
                            // Apply filters
                            if (!showTilledOnly && tile.IsTilled && !tile.HasCrop) continue;
                            if (!showCropsOnly && tile.HasCrop) continue;
                            
                            EditorGUILayout.BeginHorizontal();
                            
                            // Icon and state based on tile properties
                            string icon = "";
                            string state = "";
                            Color textColor = Color.white;
                            
                            if (tile.HasCrop && tile.IsTilled)
                            {
                                icon = "🌱";
                                state = $"Crop (Stage {tile.CropStage})";
                                textColor = new Color(0.4f, 1f, 0.4f);
                            }
                            else if (tile.HasCrop)
                            {
                                icon = "🌿";
                                state = $"Crop Only (Stage {tile.CropStage})";
                                textColor = new Color(0.6f, 1f, 0.6f);
                            }
                            else if (tile.IsTilled)
                            {
                                icon = "🟫";
                                state = "Tilled Only";
                                textColor = new Color(0.8f, 0.6f, 0.4f);
                            }
                            else
                            {
                                icon = "⬜";
                                state = "Empty (Error!)";
                                textColor = Color.red;
                            }
                            
                            GUI.color = textColor;
                            EditorGUILayout.LabelField($"  {icon} {state}", GUILayout.Width(160));
                            GUI.color = Color.white;
                            
                            if (tile.HasCrop)
                            {
                                EditorGUILayout.LabelField($"ID: {tile.CropTypeID}", GUILayout.Width(70));
                            }
                            else
                            {
                                GUILayout.Space(70);
                            }
                            
                            EditorGUILayout.LabelField($"Pos: ({tile.WorldX}, {tile.WorldY})", GUILayout.Width(130));
                            
                            // Button to highlight position in scene
                            if (GUILayout.Button("📍", GUILayout.Width(30)))
                            {
                                Vector3 worldPos = new Vector3(tile.WorldX, tile.WorldY, 0);
                                SceneView.lastActiveSceneView.LookAt(worldPos);
                                Debug.Log($"Tile at ({tile.WorldX}, {tile.WorldY}) - Tilled: {tile.IsTilled}, HasCrop: {tile.HasCrop}" + 
                                         (tile.HasCrop ? $", CropID: {tile.CropTypeID}, Stage: {tile.CropStage}" : ""));
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
