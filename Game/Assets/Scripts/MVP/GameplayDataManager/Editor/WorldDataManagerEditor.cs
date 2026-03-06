using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(WorldDataManager))]
public class WorldDataManagerEditor : Editor
{
    private bool showChunkData = true;
    private bool showInventoryData = true;
    private bool showEmptyChunks = false;
    private bool showTilledOnly = true;
    private bool showCropsOnly = true;
    private int selectedSectionFilter = -1; // -1 = all sections
    private Vector2 scrollPosition;
    private Vector2 inventoryScrollPosition;

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
        
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("Crops", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField($"  Chunks with Crops: {stats.ChunksWithCrops}");
        EditorGUILayout.LabelField($"  Total Crops: {stats.TotalCrops}");
        EditorGUILayout.LabelField($"  Total Tilled Tiles: {stats.TotalTilledTiles}");
        
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("Structures", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField($"  Total Structures: {stats.TotalStructures}");
        
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("Inventory", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField($"  Cached Characters: {stats.InventoryCharacters}");
        EditorGUILayout.LabelField($"  Occupied Slots: {stats.InventoryOccupiedSlots}");
        EditorGUILayout.LabelField($"  Total Items: {stats.InventoryTotalItems}");
        
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField($"Memory Usage: {stats.MemoryUsageMB:F3} MB", EditorStyles.boldLabel);

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
                    UnifiedChunkData chunk = chunkPair.Value;
                    int totalTiles = chunk.tiles.Count;
                    
                    // Count tile states
                    int cropsWithTilled = 0;
                    int cropsOnly = 0;
                    int tilledOnly = 0;
                    int emptyTiles = 0;
                    
                    foreach (var slot in chunk.tiles.Values)
                    {
                        if (slot.HasCrop && slot.IsTilled) cropsWithTilled++;
                        else if (slot.HasCrop) cropsOnly++;
                        else if (slot.IsTilled) tilledOnly++;
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
                                state = $"Crop (Stage {tile.Crop.CropStage})";
                                textColor = new Color(0.4f, 1f, 0.4f);
                            }
                            else if (tile.HasCrop)
                            {
                                icon = "🌿";
                                state = $"Crop Only (Stage {tile.Crop.CropStage})";
                                textColor = new Color(0.6f, 1f, 0.6f);
                            }
                            else if (tile.HasStructure)
                            {
                                icon = "🏠";
                                state = $"Structure: {tile.Structure.StructureId}";
                                textColor = new Color(0.8f, 0.8f, 1f);
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
                                EditorGUILayout.LabelField($"ID: {tile.Crop.PlantId}", GUILayout.Width(70));
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
                                Debug.Log($"Tile at ({tile.WorldX}, {tile.WorldY}) - Tilled: {tile.IsTilled}, HasCrop: {tile.HasCrop}, HasStructure: {tile.HasStructure}" + 
                                         (tile.HasCrop ? $", PlantId: {tile.Crop.PlantId}, Stage: {tile.Crop.CropStage}" : "") +
                                         (tile.HasStructure ? $", StructureId: {tile.Structure.StructureId}" : ""));
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

        EditorGUILayout.Space(10);

        // Inventory data display
        showInventoryData = EditorGUILayout.Foldout(showInventoryData, "Inventory Data Details", true);
        
        if (showInventoryData)
        {
            EditorGUILayout.HelpBox(
                "📦 Cached character inventories\n" +
                "Shows all characters with cached inventory data in RAM", 
                MessageType.None);
            
            EditorGUILayout.Space(5);

            // Get all character IDs
            var charIds = manager.GetAllCharacterIds();
            
            if (charIds.Count == 0)
            {
                EditorGUILayout.HelpBox("No cached character inventories", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Cached Characters: {charIds.Count}", EditorStyles.boldLabel);
                
                // Scrollable character list
                inventoryScrollPosition = EditorGUILayout.BeginScrollView(inventoryScrollPosition, GUILayout.Height(300));

                foreach (var charId in charIds)
                {
                    var info = manager.GetCharacterInventoryDebugInfo(charId);
                    
                    if (!info.IsValid) continue;

                    // Character header with color based on occupied slots
                    float fillPercent = info.OccupiedSlots / 36f;
                    Color bgColor = fillPercent > 0.75f ? new Color(1f, 0.5f, 0.3f, 0.3f) : 
                                    fillPercent > 0.5f ? new Color(1f, 0.8f, 0.3f, 0.3f) :
                                    fillPercent > 0.25f ? new Color(0.5f, 1f, 0.5f, 0.3f) :
                                    new Color(0.3f, 0.3f, 0.3f, 0.2f);
                    
                    GUI.backgroundColor = bgColor;
                    EditorGUILayout.BeginVertical("box");
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"📦 {charId}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Slots: {info.OccupiedSlots}/36", GUILayout.Width(90));
                    EditorGUILayout.LabelField($"Items: {info.TotalItems}", GUILayout.Width(80));
                    
                    EditorGUILayout.EndHorizontal();

                    // Show some items if inventory has items
                    if (info.Items.Count > 0)
                    {
                        EditorGUI.indentLevel++;
                        
                        int displayCount = Mathf.Min(5, info.Items.Count);
                        for (int i = 0; i < displayCount; i++)
                        {
                            var item = info.Items[i];
                            EditorGUILayout.LabelField($"  [Slot {item.SlotIndex:D2}] {item.ItemId} x{item.Quantity}", 
                                EditorStyles.miniLabel);
                        }
                        
                        if (info.Items.Count > displayCount)
                        {
                            EditorGUILayout.LabelField($"  ... and {info.Items.Count - displayCount} more items", 
                                EditorStyles.miniLabel);
                        }
                        
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        EditorGUILayout.EndVertical();

        // Auto-refresh in play mode
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
