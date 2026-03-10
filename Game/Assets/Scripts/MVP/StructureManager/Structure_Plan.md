# Structure Manager Plan

## 1. Grid & Placement System (Hệ thống lưới và Đặt điểm)

- [x] Trạng thái: Done
- Quyết định:
  - Kế thừa cơ chế Grid-based (hít vào ô lưới) giống như Stardew Valley / Minecraft bằng cách tái sử dụng logic `CropTileSelector`.
  - Hỗ trợ công trình có kích thước đa dạng (1x1, 2x1, 2x2...).
  - Logic kiểm tra điều kiện đặt (Placement Validation) chia làm 2 lớp:
    1. **Kiểm tra vùng được phép đặt (Buildable Area):** Tận dụng lại hệ thống quét Tilemap ẩn (`TillableTilemap`) giống hệt cách cơ chế cuốc đất của Crop đang làm. Điều này giúp ngăn người chơi đặt công trình ra ngoài lề bản đồ hoặc lên trên mặt nước / địa hình không cho phép.
    2. **Kiểm tra chiếm dụng và va chạm chéo (Occupancy Check):** Sẽ **tái sử dụng hoàn toàn `WorldDataManager` hiện tại**. Dữ liệu lưới của bạn nằm chung trong `UnifiedChunkData` đã có sẵn luật cấu trúc dữ liệu chống trùng lặp (`Mutual Exclusion Rules`: một ô có Crop thì không thể có Structure và ngược lại). Bạn chỉ cần:
       - Trong bộ code của Structure (mới): Dùng `WorldDataManager.Instance.HasCropAtWorldPosition(pos)` và `WorldDataManager.Instance.HasStructureAtWorldPosition(pos)` lặp qua tọa độ lưới (theo kích thước NxM của công trình) để xác nhận ô trống.
       - Trong bộ code của Crop (cũ): Khối dữ liệu chung `UnifiedChunkData` đã sẵn sàng chặn mọi hành vi trồng hạt hay cuốc đất gạch chéo lên ô `HasStructure = true`. Do đó hoàn toàn an toàn mà không sợ Crop cũ vô tình phá hỏng không gian của Structure.

## 2. Instantiation & Networking (Khởi tạo và Đồng bộ Photon)

- [x] Trạng thái: Done
- Quyết định:
  - **Cách tiếp cận GameObject và Object Pooling**: Không cần dùng NetworkObject của Photon cho mỗi công trình. Toàn bộ các Structure sẽ được quản lý thông qua **Object Pooling** (sử dụng Pool sinh sẵn) để giảm chi phí Instantiate/Destroy liên tục khi tải/hủy Chunk.
  - **Đồng bộ qua Chunk (ChunkDataSyncManager)**: Do data được lưu trong `WorldDataManager` theo dạng Chunk, khi có một Structure mới được đặt hoặc bị phá hủy, ta sẽ cập nhật state thông qua các phương thức đã implement sẵn của `StructureDataModule` (thuộc `WorldDataManager`).
  - **Cơ chế Share Data**: Khi Player thao tác thành công (đặt/phá công trình), hệ thống cập nhật local thông qua `WorldDataManager` rồi gọi `ChunkDataSyncManager` để gửi `RaiseEvent` mang theo cấu trúc Data của Chunk đó (hoặc diff của thao tác) cho các Client khác trên mạng. Các Client nhận được Event từ `ChunkDataSyncManager` sẽ tự động trích xuất thông tin mới từ `StructureDataModule` và lấy GameObject từ **Object Pool** ra hiển thị (khi đặt) hoặc trả GameObject về **Object Pool** (khi phá hủy) tại toạ độ lưới. Giải pháp này giúp chia sẻ State nhẹ nhàng, tối ưu hiệu suất GC (Garbage Collector) và tự động đồng bộ khi có Client mới tham gia (tải lại toàn bộ Data Chunk hiện tại từ Server/Master).

## 3. Interaction & Functionality (Tương tác và Chức năng)

- [x] Trạng thái: Done
- Quyết định:
  - **Kiến trúc tương tác (IInteractable)**: Mọi công trình có thể tương tác sẽ gắn script kế thừa interface `IInteractable` và có vùng Trigger Collider. Khi người chơi bước vào vùng (lại gần), sẽ hiển thị UI gợi ý bấm nút (VD: "Nhấn [F] để tương tác"). Khi người chơi nhấn nút tương tác (phím F), hệ thống sẽ gọi hàm `Interact()`.
    - _Không tương tác_: Những công trình chỉ mang tính trang trí sẽ thiết lập `InteractionType = None` trong `StructureDataSO`, không có Trigger Collider và bỏ qua xử lý hiển thị UI tương tác.
  - **Instant Crafting (Lò nướng, Bàn chế tạo)**: Các công trình này là điểm tương tác tĩnh (Interactable Point). Khi người chơi nhấn phím tương tác, Presenter đọc tham số `InteractionType` tương ứng và gọi lệnh cho `UIManager` để mở UI giao dịch tương ứng (Crafting View, Furnace View), kết quả chế tạo xảy ra ngay lập tức.
  - **Máy móc tự động (Auto-machines: Vòi phun nước, Máy hái...)**: Chưa có định hướng

## 4. UI & Visual Feedback (Giao diện và Hiệu ứng)

- [x] Trạng thái: Done
- Quyết định:
  - **Chế độ đặt (Preview/Ghost Mode)**: Lấy GameObject từ **Object Pool** ở trạng thái mờ (Alpha color) hoặc đổi Shader sang dạng Blueprint (Màu Xanh/Màu Đỏ) làm bóng mờ (Ghost). Nó sẽ đi theo con chuột và snap dính vào lưới bằng lớp `CropTileSelector` đã có sẵn. Khi hủy chế độ đặt thì trả Ghost này về Pool.
  - **Hướng đặt (Rotation)**: Không cần tính năng xoay. Tất cả các Structure luôn hướng mặt về phía dưới (Front-facing).
  - **Tháo dỡ (Demolish)**: Người chơi phải cầm đúng Công cụ (Tool) tương ứng (VD: Rìu cho đồ gỗ, Cúp cho đồ đá) và thực hiện thao tác đánh vào công trình để phá vỡ. Khi thanh máu của công trình cạn, nó sẽ rớt lại thành Item nguyên liệu để cất vào túi.
    - **Ràng buộc quan trọng:** Chỉ có thể phá Rương (hoặc các khối có chứa đồ Storage) khi **bên trong không có bất kỳ vật phẩm nào**. Nếu rương còn đồ, đập sẽ không vỡ và hiện thông báo lỗi ("Hãy làm trống rương trước khi phá").
  - **Cơ chế ghép Rương (Double Chest Merging)**:
    - _Visual_: Khi đặt một cái rương, hệ thống check 4 ô xung quanh (Trên, Dưới, Trái, Phải). Nếu có rương đơn cùng loại, hai rương sẽ cập nhật Sprite thành dạng "ghép nối" (Rương trái + Rương phải).
    - _Data_: Gắn ID liên kết giữa 2 toạ độ Grid này trong `StructureDataModule` hoặc tự động search ô kế bên khi player click mở rương.
    - _UI/Interaction_: Mở 1 trong 2 nửa rương đều sẽ gọi lên chung 1 UI Inventory có sức chứa x2.
    - _Demolish_: Vì áp dụng luật "Rương phải trống mới được phá", nên khi phá 1 nửa của Double Chest, người chơi bắt buộc phải dọn sạch đồ thuộc phần slot đó (hoặc dọn sạch cả rương đôi). Vỡ 1 nửa thì nửa còn lại tự động biến về Sprite rương đơn, slot chứa đồ giảm đi một nửa.

## 5. Architecture & Implementation (Kiến trúc MVP)

- [x] Trạng thái: Done
- Quyết định: Toàn bộ hệ thống sẽ tuân thủ nghiêm ngặt **Kiến trúc MVP (Model - View - Presenter)** đang được sử dụng trong dự án. **Tuyệt đối không chỉnh sửa các file nằm ngoài thư mục `StructureManager` trừ khi có yêu cầu đặc biệt.**
  - **Ngoại lệ (Bypass rule)**: Được phép chỉnh sửa file `ChunkDataSyncManager.cs` để thêm các byte event mới (VD: `STRUCTURE_PLACED_EVENT`, `STRUCTURE_REMOVED_EVENT`...) và ghi thêm hàm `SyncChunkStructure` nhằm phục vụ việc phát event đồng bộ theo kiến trúc, không được xoá những gì đã có để trách xung đột tới những file khác
  - **Module quản lý Data (`StructureDataSO` và `StructureDataModule`)**:
    - Không cần tạo mới `StructureSaveLoadModule` vì `StructureDataModule` (thuộc kho lõi GameplayDataManager) đã được thiết kế hoàn thiện nhằm đóng vai trò serialize/deserialize/quản lý runtime state.
    - Sử dụng `UnifiedChunkData` và `StructureDataModule` (quản lý bởi `WorldDataManager`) làm nguồn chân lý duy nhất (Single Source of Truth) cho toàn bộ Client.
    - Tạo `StructureDataSO` (ScriptableObject) lưu định nghĩa thông tin tĩnh của mỗi máy móc: `StructureId`, `Prefab`, Kích thước phần thân `Width x Height`, có phải Storage không, và `UIType/InteractionType` (ví dụ: `None`, `Crafting`, `Storage`, `Smelting`) để biết công trình có mở UI nào hay không.
  - **View (`StructureView.cs`)**: Quản lý Unity GameObject, Event Input và Visual. Quản lý con "Ghost Prefab" (bóng mờ) chạy theo chuột. Đổi màu vật liệu Ghost sang Xanh/Đỏ tùy theo Presenter trả về. Lắng nghe phím bấm/Chuột trái để trigger Event báo lên Presenter. Các Prefab công trình cụ thể có thể implement thêm interface `IInteractable` hoặc delegate action mở View đặc thù từ Presenter.
  - **Presenter (`StructurePresenter.cs`)**: Cầu nối logic. Lắng nghe tọa độ chuột từ `View`, đưa xuống `Service` để validate tọa độ. Trả về kết quả Hợp lệ/Không hợp cho `View` để đổi màu chiếu. Khi có lệnh click đặt, nhờ `Service` ghi dữ liệu.
  - **Service (`StructureService.cs`)**: Chứa Business Logic cốt lõi. Khai thác `WorldDataManager` để check đè lấn. Khi đặt/phá hợp lệ, cập nhật vào `StructureDataModule`. Sau đó, trigger `ChunkDataSyncManager.SyncChunkStructure(chunkPos)` (hoặc hàm tương tự) để gửi RaiseEvent cho toàn mạng lưới Photon.
