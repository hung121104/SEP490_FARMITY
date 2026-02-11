using UnityEngine;
using TMPro;

/// <summary>
/// Component attached to WorldItem prefab to display individual world data
/// </summary>
public class WorldItemView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI worldNameText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI goldText;

    private WorldModel worldData;

    /// <summary>
    /// Initialize the world item with data
    /// </summary>
    public void SetWorldData(WorldModel world)
    {
        worldData = world;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (worldData == null)
        {
            Debug.LogError("WorldItemView: worldData is null!");
            return;
        }

        // Debug log to check data
        Debug.Log($"Displaying world: _id={worldData._id}, worldName='{worldData.worldName}', day={worldData.day}, gold={worldData.gold}");

        // Set world name
        if (worldNameText != null)
        {
            string displayName = string.IsNullOrEmpty(worldData.worldName) ? "Unnamed World" : worldData.worldName;
            worldNameText.text = displayName;
            Debug.Log($"Set world name text to: {displayName}");
        }

        // Set time information
        if (timeText != null)
        {
            timeText.text = $"Day {worldData.day}, {worldData.hour:D2}:{worldData.minute:D2}";
        }

        // Set gold amount
        if (goldText != null)
        {
            goldText.text = $"Gold: {worldData.gold}";
        }
    }

    public WorldModel GetWorldData() => worldData;
}
