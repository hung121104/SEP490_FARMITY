# Structure Manager Plan

## 1. Grid & Placement System (Hệ thống lưới và Đặt điểm)

- [x] Trạng thái: Done
- Quyết định:
  - Kế thừa cơ chế Grid-based (hít vào ô lưới) giống như Stardew Valley / Minecraft bằng cách tái sử dụng logic `CropTileSelector`.
  - Hỗ trợ công trình có kích thước đa dạng (1x1, 2x1, 2x2...).
  - Về việc check va chạm (Collision): Sẽ **tái sử dụng hoàn toàn `WorldDataManager` hiện tại của bạn**. Tôi thấy bạn đã dựng sẵn `UnifiedChunkData` và `StructureDataModule` lưu chung bản đồ ở dạng Chunk. Ta chỉ cần gọi `WorldDataManager.Instance.HasCropAtWorldPosition(pos)` và `HasStructureAtWorldPosition(pos)` và lặp qua các ô theo kích thước thực của công trình để check xem nó có hợp lệ để đặt hay không.

## 2. Instantiation & Networking (Khởi tạo và Đồng bộ Photon)

- [x] Trạng thái: Done
- Quyết định:
  - **Cách tiếp cận Hybrid (Tilemap + GameObject)**: Chấp nhận. Tilemap dùng riêng biệt cho các item có thuộc tính tĩnh (fence, paths, v.v), còn với các Structure tương tác như `Crafting Table` hay `Cooker` ta sẽ dùng `GameObject`.
  - **Nâng cấp Sync qua RaiseEvent**: Giải pháp này cực kỳ tối ưu vì bạn đang dùng Unity + Photon dạng Chunk (WorldDataManager). Thay vì dùng `PhotonNetwork.Instantiate` tạo NetworkObject tốn tài nguyên và tăng lượng view, sử dụng `RaiseEvent` (hay `ChunkDataSyncManager` hiện tại đang dùng RaiseEvent để gửi state theo chunk) là hợp lý nhất. Server/Client nào nhận Event sẽ tự spawn Prefab GameObject tương ứng từ Resources theo toạ độ Grid nhận được.

## 3. Interaction & Functionality (Tương tác và Chức năng)

- [x] Trạng thái: Done
- Quyết định:
  - **Instant Crafting (Lò nướng, Bàn chế tạo)**: Các công trình này là điểm tương tác tĩnh (Interactable Point). Khi click/tương tác sẽ mở UI giao dịch, kết quả chế tạo xảy ra ngay lập tức.
  - **Máy móc tự động (Auto-machines: Vòi phun nước, Máy hái...)**:

## 4. UI & Visual Feedback (Giao diện và Hiệu ứng)

- [x] Trạng thái: Done
- Quyết định:
  - **Chế độ đặt (Preview/Ghost Mode)**: Hoàn toàn kế thừa cơ chế vẽ Tile Preview của hệ thống Cuốc đất/Gieo hạt đang có. Khi cầm Structure Item trên tay, hiển thị Sprite mờ (màu Xanh nếu vị trí trống, màu Đỏ nếu bị kẹt/không hợp lệ) hít theo toạ độ Grid của chuột.
  - **Hướng đặt (Rotation)**: Không cần tính năng xoay. Tất cả các Structure luôn hướng mặt về phía dưới (Front-facing).
  - **Tháo dỡ (Demolish)**: Người chơi phải cầm đúng Công cụ (Tool) tương ứng (VD: Rìu cho đồ gỗ, Cúp cho đồ đá) và thực hiện thao tác đánh vào công trình để phá vỡ. Khi thanh máu của công trình cạn, nó sẽ rớt lại thành Item nguyên liệu để cất vào túi.
    - **Ràng buộc quan trọng:** Chỉ có thể phá Rương (hoặc các khối có chứa đồ Storage) khi **bên trong không có bất kỳ vật phẩm nào**. Nếu rương còn đồ, đập sẽ không vỡ và hiện thông báo lỗi ("Hãy làm trống rương trước khi phá").
  - **Cơ chế ghép Rương (Double Chest Merging)**:
    - _Visual_: Khi đặt một cái rương, hệ thống check 4 ô xung quanh (Trên, Dưới, Trái, Phải). Nếu có rương đơn cùng loại, hai rương sẽ cập nhật Sprite thành dạng "ghép nối" (Rương trái + Rương phải).
    - _Data_: Gắn ID liên kết giữa 2 toạ độ Grid này trong `StructureDataModule` hoặc tự động search ô kế bên khi player click mở rương.
    - _UI/Interaction_: Mở 1 trong 2 nửa rương đều sẽ gọi lên chung 1 UI Inventory có sức chứa x2.
    - _Demolish_: Vì áp dụng luật "Rương phải trống mới được phá", nên khi phá 1 nửa của Double Chest, người chơi bắt buộc phải dọn sạch đồ thuộc phần slot đó (hoặc dọn sạch cả rương đôi). Vỡ 1 nửa thì nửa còn lại tự động biến về Sprite rương đơn, slot chứa đồ giảm đi một nửa.
