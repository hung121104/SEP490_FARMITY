using UnityEngine;

/// <summary>
/// View layer that listens for fishing rod usage
/// and triggers the fishing mini game.
/// </summary>
public class FishingViewSystem : MonoBehaviour
{
    [Header("References")]

    [SerializeField] private FishingService fishingService;

    [SerializeField] private FishingMiniGameController miniGame;

    void OnEnable()
    {
        UseToolService.OnFishingRodRequested += HandleFishingRequest;
    }

    void OnDisable()
    {
        UseToolService.OnFishingRodRequested -= HandleFishingRequest;
    }

    private void HandleFishingRequest(ToolData tool, Vector3 worldPosition)
    {
        Debug.Log("[FishingViewSystem] Fishing rod used");

        if (fishingService.StartFishing(worldPosition))
        {
            miniGame.StartMiniGame();
        }
    }
}