using UnityEngine;

public class FishingGameSetup : MonoBehaviour
{
    [Header("References")]
    public FishDatabase fishDatabase; // Kéo thả file ScriptableObject vào đây trên Inspector
    public FishingView fishingView;   // Kéo thả GameObject chứa FishingView vào đây

    private FishingPresenter presenter;

    void Start()
    {
        // Lấy InventoryService hiện tại trong game của bạn (tùy cách bạn setup, ví dụ qua ServiceLocator hoặc Singleton)
        // Giả sử: IInventoryService inventoryService = ServiceLocator.Get<IInventoryService>();
        // Ở đây tôi dùng biến giả định để bạn dễ hình dung:
        IInventoryService inventoryService = null; // BẠN CẦN GÁN INVENTORY THẬT VÀO ĐÂY

        // 1. Tạo Model
        FishingModel model = new FishingModel();

        // 2. Tạo Service
        IFishingService service = new FishingService(fishDatabase, model, inventoryService);

        // 3. Tạo Presenter để kết nối tất cả
        presenter = new FishingPresenter(fishingView, service, model);
    }

    void OnDestroy()
    {
        // Dọn dẹp event khi chuyển scene
        presenter?.Dispose();
    }
}