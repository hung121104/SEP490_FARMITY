# Catalog Progress UI Setup Guide

## Overview
The catalog loading process now displays real-time progress with a progress bar and percentage text. The system automatically tracks progress across all catalog services (Items, Plants, Recipes, Materials, Resources, and Skins).

## Components

### 1. **CatalogProgressManager** (Static/Utility)
- **Location**: `Assets/Scripts/Catalogs/CatalogProgressManager.cs`
- **Purpose**: Centralized progress tracking for all catalogs
- **Events**:
  - `OnProgressChanged(float progress)` - 0 to 1, fired continuously
  - `OnCatalogStarted()` - Fired when first catalog starts
  - `OnCatalogCompleted()` - Fired when all catalogs finish
- **Weighted Progress**: Each catalog contributes a percentage to overall progress
  - Item Catalog: 25%
  - Plant Catalog: 25%
  - Recipe Catalog: 15%
  - Material Catalog: 20%
  - Resource Catalog: 10%
  - Skin Catalog: 5%

### 2. **ResourceDownloadProgress** (UI Component)
- **Location**: `Assets/Scripts/MVP/_UI/DownloadResource/ResourceDownloadProgress.cs`
- **Purpose**: Displays progress bar and percentage text to the user
- **Features**:
  - Smooth progress bar animation
  - Real-time percentage display
  - Fade in/out animations
  - Subscribes to CatalogProgressManager events

## Setup Instructions

### Step 1: Create UI Canvas (if not exists)
1. In your scene, create a **Canvas** (Right-click Scene → UI → Canvas)
2. Name it `LoadingCanvas` or similar

### Step 2: Add Progress Bar (Slider)
1. In the Canvas, create a **Panel** for the background
   - Position at top/center of screen
   - Size: 400×60 pixels (adjust as needed)
   - Color: Dark semi-transparent (e.g., 0,0,0 at 0.8 alpha)

2. Inside the Panel, create a **Slider** component
   - Change Slider image to use a `Horizontal` layout
   - Customize colors:
     - Background: Gray
     - Fill: Green (or your preferred color)
     - Handle: White
   - Disable the Handle (`Transform` → Set scale to 0)

2. Inside Slider, create a **TextMeshPro** text element for the percentage
   - Position in the center of the Slider
   - Font size: 24
   - Color: White
   - Text: "Loading..."

### Step 3: Add ResourceDownloadProgress Script
1. Select the **Panel** GameObject
2. Add Component → `ResourceDownloadProgress`
3. In the Inspector:
   - **Progress Bar**: Drag the Slider component
   - **Progress Text**: Drag the TextMeshPro text element
   - **Canvas Group**: Select the Panel's CanvasGroup (add if missing)

### Step 4: Hide By Default (Optional)
1. Set the Canvas Group `Alpha` to 0 initially
   - The script will fade it in automatically when loading starts
2. Or disable the Canvas entirely

## Example Hierarchy
```
Canvas (LoadingCanvas)
├── CanvasGroup (Alpha = 0 initially)
├── Panel (LoadingBackground)
│   ├── Slider (ProgressBar)
│   │   ├── Background
│   │   ├── Fill Area
│   │   │   └── Fill
│   │   └── Handle Slide Area
│   │       └── Handle
│   └── TextMeshPro (ProgressPercentage)
```

## How It Works

### During Catalog Loading:
1. When any catalog service starts, it calls `CatalogProgressManager.NotifyStarted()`
2. As items/sprites download, the service calls `CatalogProgressManager.ReportProgress(current, total, "Catalog Name")`
3. The manager calculates aggregate progress based on weights
4. `ResourceDownloadProgress` receives `OnProgressChanged(progress)` events
5. Progress bar smoothly animates to the new value
6. Percentage text updates in real-time
7. Canvas fades in automatically

### When All Catalogs Finish:
1. Last catalog calls `CatalogProgressManager.NotifyCompleted()`
2. Progress bar reaches 100%
3. `OnCatalogCompleted()` event fires
4. Canvas automatically fades out after 1 second

## Code Examples

### Subscribing to Progress Events (Optional)
```csharp
private void OnEnable()
{
    CatalogProgressManager.OnProgressChanged += HandleProgressUpdate;
    CatalogProgressManager.OnCatalogCompleted += HandleLoadingComplete;
}

private void OnDisable()
{
    CatalogProgressManager.OnProgressChanged -= HandleProgressUpdate;
    CatalogProgressManager.OnCatalogCompleted -= HandleLoadingComplete;
}

private void HandleProgressUpdate(float progress)
{
    Debug.Log($"Loading: {progress * 100}%");
}

private void HandleLoadingComplete()
{
    Debug.Log("All catalogs loaded!");
}
```

### Viewing Progress in Console
All catalogs log their progress:
```
[ItemCatalogService] Catalog ready with 12 item(s).
[PlantCatalogService] Catalog ready with 8 plant(s).
[MaterialCatalogService] All material sheets ready.
[SkinCatalogManager] Catalog ready.
```

## Customization

### Change Animation Speed
In `ResourceDownloadProgress.cs`, adjust `PROGRESS_LERP_SPEED`:
```csharp
private const float PROGRESS_LERP_SPEED = 5f; // Increase for faster animation
```

### Change Fade-Out Duration
In `FadeOutAfterDelay()` coroutine, adjust `fadeDuration`:
```csharp
float fadeDuration = 0.5f; // Increase for slower fade
```

### Adjust Catalog Weights
In `CatalogProgressManager.cs`, modify `CATALOG_WEIGHTS` dictionary:
```csharp
{ "Item Catalog", 0.25f },     // 25% weight
{ "Plant Catalog", 0.25f },    // 25% weight
// ... adjust as needed
```

## Troubleshooting

### Progress Bar Not Showing
- Ensure Canvas Group is properly assigned in Inspector
- Check that Canvas Group `Alpha` is set correctly
- Verify Slider and Text components are assigned

### Progress Not Updating
- Check Script Execution Order (Window → Script Execution Order)
- Ensure ResourceDownloadProgress is in same scene as catalog services
- Verify all catalogs are active in the scene

### Text Not Showing
- Ensure TextMeshPro text element is properly assigned in Inspector
- Check that TMP font asset is assigned in the TextMeshPro component
- Verify text color is not transparent or same as background

## Performance Notes
- Progress updates are lightweight (no per-frame allocations)
- Smooth animation uses Lerp and is only active during loading
- All events are unsubscribed in OnDisable to prevent memory leaks
