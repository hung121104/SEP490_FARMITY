using UnityEngine;

public class FishingBootstrapper : MonoBehaviour
{
    [Header("Dependencies ")]
    public FishingView fishingView;

    private FishingPresenter presenter;
    private FishingModel model;
    private IFishingService service;

    private void Awake()
    {
        model = new FishingModel();
    }

    private void Start()
    {
        InventoryGameView inventoryManager = FindAnyObjectByType<InventoryGameView>();

        if (inventoryManager == null)
        {
            Debug.LogError("[FishingBootstrapper] Cant find InventoryGameView! ");
            return;
        }

        IInventoryService globalInventory = inventoryManager.GetInventoryService();
        if (globalInventory == null)
        {
            Debug.LogError("[FishingBootstrapper] InventoryGameView.GetInventoryService() return null!");
            return;
        }

        service = new FishingService(globalInventory, model);
        presenter = new FishingPresenter(fishingView, service, model);

        
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
            presenter.HandleFishingRodUsed(targetPosition, toolData.itemID);
            
        }
    }
}