using UnityEngine;

public class FishingBootstrapper : MonoBehaviour
{
    [Header("Dependencies ")]
    public FishingView fishingView;
    public FishDatabase fishDatabase;

    private FishingPresenter presenter;

    private void Awake()
    {
        InventoryGameView inventoryManager = FindAnyObjectByType<InventoryGameView>();

        if (inventoryManager == null)
        {
            Debug.LogError("Chưa kéo InventoryGameView ra Scene!");
            return;
        }

        IInventoryService globalInventory = inventoryManager.GetInventoryService();
        FishingModel model = new FishingModel();

        
        IFishingService service = new FishingService(fishDatabase, globalInventory, model);

        presenter = new FishingPresenter(fishingView, service, model);

        Debug.Log("🎣 Hệ thống câu cá MVP đã được lắp ráp thành công!");
    }

    private void OnEnable()
    {
        UseToolService.OnFishingRodRequested += HandlePlayerUsedFishingRod;
    }

    private void OnDisable()
    {
        UseToolService.OnFishingRodRequested -= HandlePlayerUsedFishingRod;
    }

    private void HandlePlayerUsedFishingRod(ToolData toolData, Vector3 targetPosition)
    {
        if (presenter != null)
        {
            presenter.HandleFishingRodUsed(targetPosition);
        }
    }
}