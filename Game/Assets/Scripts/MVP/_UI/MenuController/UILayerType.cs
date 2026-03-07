public enum UILayerType
{
    HUD,        // Always visible (hotbar, minimap) - not managed by open/close
    Panel,      // Main panels (Inventory, Crafting) - only one active at a time
    Popup,      // Stacked on top of panels (confirm dialog, tooltip)
    Overlay     // Full-screen overlay (loading, fade)
}
