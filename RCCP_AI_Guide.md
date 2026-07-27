# Hướng Dẫn Cấu Hình, Sửa Lỗi & Tùy Chỉnh AI Trong RCCP (Realistic Car Controller Pro)

Tài liệu hướng dẫn chi tiết cách thiết lập xe tự lái (AI) di chuyển theo đường Waypoint, sửa các lỗi phổ biến (xe chạy thẳng, không rẽ, không mở được tab Bake), tắt hư hỏng xe và thiết lập hệ thống độ khó cho AI trong dự án Unity sử dụng gói Realistic Car Controller Pro (RCCP).

---

## 📋 Mục Lục
1. [Cách Thiết Lập RCCP AI Theo Đường Waypoint](#1-cách-thiết-lập-rccp-ai-theo-đường-waypoint)
2. [Sửa Lỗi Xe Chỉ Chạy Thẳng / Không Rẽ Theo Waypoint](#2-sửa-lỗi-xe-chỉ-chạy-thẳng--không-rẽ-theo-waypoint)
3. [Cách Bake NavMesh Cho Scene Trống (Unity 2022+)](#3-cách-bake-navmesh-cho-scene-trống-unity-2022)
4. [Giải Thích Sự Khác Biệt Với Scene Demo (RCCP_Scene_CityNew_AIO)](#4-giải-thích-sự-khác-biệt-với-scene-demo-rccp_scene_citynew_aio)
5. [Cách Tắt Hư Hỏng / Móp Méoc Sát Thương Cho Xe](#5-cách-tắt-hư-hỏng--móp-méoc-sát-thương-cho-xe)
6. [Cấu Hình & Viết Code Quản Lý Độ Khó Cho AI (Easy / Medium / Hard)](#6-cấu-hình--viết-code-quản-lý-độ-khó-cho-ai-easy--medium--hard)

---

## 1. Cách Thiết Lập RCCP AI Theo Đường Waypoint

### Cách 1: Thiết lập trực tiếp trong Unity Editor
1. Chọn GameObject chiếc xe của bạn (nơi chứa component `RCCP_CarController`).
2. Tại bảng **Inspector**, bấm **Add Component** -> Tìm và chọn **`RCCP AI`** (`RCCP_AI.cs`).
3. Trong bảng cấu hình **RCCP AI**:
   - **Navigation Mode**: Chọn **`FollowWaypoints`**.
   - **Waypoints Container**: Kéo GameObject `AI WayPointsManager` (chứa `RCCP_AIWaypointsContainer`) từ Hierarchy vào ô này.

### Cách 2: Kích hoạt / Bật tắt AI bằng C# Code

```csharp
using UnityEngine;

public class AIVehicleSetup : MonoBehaviour
{
    public RCCP_CarController carController;
    public RCCP_AIWaypointsContainer waypointsManager;

    // Bật chế độ AI cho xe
    public void EnableAI()
    {
        RCCP_AI aiComponent = carController.GetComponent<RCCP_AI>();
        if (aiComponent == null)
        {
            aiComponent = carController.gameObject.AddComponent<RCCP_AI>();
        }

        aiComponent.waypointsContainer = waypointsManager;
        aiComponent.navigationMode = RCCP_AI.NavigationMode.FollowWaypoints;
        aiComponent.enabled = true;
    }

    // Tắt AI để trả lại quyền lái cho Người chơi (Player)
    public void DisableAI()
    {
        RCCP_AI aiComponent = carController.GetComponent<RCCP_AI>();
        if (aiComponent != null)
        {
            aiComponent.enabled = false;
        }

        if (carController.Inputs != null)
            carController.Inputs.overridePlayerInputs = false;
        
        carController.externalControl = false;
    }
}
```

---

## 2. Sửa Lỗi Xe Chỉ Chạy Thẳng / Không Rẽ Theo Waypoint

Nếu bạn đã thêm `RCCP_AI` nhưng xe chỉ nhấn ga chạy thẳng mà không chịu quay đầu/rẽ theo Waypoint, nguyên nhân thường do:

1. **Chưa gán `Waypoints Container`**: Ô `Waypoints Container` trên `RCCP_AI` bị `NULL` (để trống).
2. **Scene chưa được Bake NavMesh**: `RCCP_AI` sử dụng một `NavMeshAgent` ẩn tên là `"Navigator"` bên trong xe để tính góc lái. Nếu chưa Bake NavMesh, `NavMeshAgent` không tính được góc rẽ (`steerInput = 0`), khiến xe đi thẳng.
3. **Chỉ số Waypoint ban đầu bị xa**: Xe đang cố quay về Waypoint 0 nằm ở tít sau lưng.
4. **Khoảng cách nhận Waypoint quá nhỏ (`Next Waypoint Pass Distance`)**: Xe chạy quá nhanh lướt qua Waypoint mà không kịp nhận diện.
   - **Khắc phục**: Tăng `Next Waypoint Pass Distance` lên **10 – 15m**.
5. **Raycast tránh vật cản bị vướng (`Use Raycasts`)**: Tia raycast bị đâm vào sàn/khung xe làm AI bẻ lái sai.
   - **Khắc phục**: Tạm thời bỏ tích ô `Use Raycasts` để kiểm tra.

---

## 3. Cách Bake NavMesh Cho Scene Trống (Unity 2022+)

Trong Unity 2022+ / Unity 6, menu `Window -> AI -> Navigation` mặc định không có hoặc không có tab Bake. Bạn dùng 1 trong 2 cách sau:

### Cách 1: Dùng component `NavMesh Surface` (Khuyên dùng)
1. Trong Hierarchy, chọn mặt sàn / mặt đường (ví dụ GameObject `Plane`).
2. Ở cửa sổ **Inspector**, chọn **Add Component** -> Gõ tìm **`NavMesh Surface`**.
3. Nhấn trực tiếp nút **`Bake`** trên component `NavMesh Surface`.
4. Quan sát trên màn hình Scene xuất hiện dải màu xanh dương là đã Bake thành công.

### Cách 2: Cài package "AI Navigation" để mở cửa sổ Bake cũ
1. Vào **`Window -> Package Manager`**.
2. Đổi **`Packages: In Project`** thành **`Packages: Unity Registry`**.
3. Tìm từ khóa **`AI Navigation`** và chọn **Install**.
4. Sau khi cài xong, bạn có thể mở menu **`Window -> AI -> Navigation`** (hoặc `Navigation (Obsolete)`), chọn tab **Bake** -> nhấn nút **Bake**.

---

## 4. Giải Thích Sự Khác Biệt Với Scene Demo (RCCP_Scene_CityNew_AIO)

Trong scene demo `RCCP_Scene_CityNew_AIO`, xe tự động bám theo Waypoint mà không cần thêm thủ công vì:
1. **Prefab đã gắn sẵn `RCCP_AI`**: Xe demo của tác giả đã được thêm sẵn component `RCCP_AI` từ trước (ở `OtherAddons/RCCP_AI`).
2. **Code tự tìm Waypoint Manager**: Trong `RCCP_AI.cs` hàm `Start()`, code tự động gọi `FindFirstObjectByType<RCCP_AIWaypointsContainer>()` nếu chưa gán thủ công.
3. **Map đã được Bake sẵn**: Tác giả đã bake NavMesh và lưu thành file `NavMesh.asset` ngay bên trong Unity (không phải bake từ Blender).

---

## 5. Cách Tắt Hư Hỏng / Móp Méoc Sát Thương Cho Xe

Để xe đâm va **không bị móp méo khung xe hay rụng linh kiện**:

### Cách 1: Tắt / Xóa Component `RCCP Damage` (Nhanh & Triệt để nhất)
1. Chọn GameObject xe trong Hierarchy.
2. Tìm component **`RCCP Damage`** (trên xe hoặc ở `OtherAddons -> Damage`).
3. **Bỏ tích (Disable)** ô vuông nhỏ cạnh tên component hoặc chuột phải chọn **Remove Component**.

### Cách 2: Tắt thuộc tính gây hư hỏng trong `RCCP Damage`
- Đặt **`Damage Multiplier` = 0**.
- Hoặc đổi **`Damage Filter` = Nothing**.

### Cách 3: Tắt rụng linh kiện (Cản xe, cửa xe)
- Chọn các bộ phận con của xe và **bỏ tích (Disable)** component **`RCCP Detachable Part`**.

---

## 6. Cấu Hình & Viết Code Quản Lý Độ Khó Cho AI (Easy / Medium / Hard)

### Các thông số tùy chỉnh độ khó có sẵn trên `RCCP_AI`:

| Thông số | Dễ (Easy) | Trung Bình (Medium) | Khó (Hard) |
| :--- | :--- | :--- | :--- |
| **`limitSpeed`** | `true` | `true` | `false` |
| **`maximumSpeed`** | 60 - 70 km/h | 90 - 100 km/h | Tối đa (không giới hạn) |
| **`smoothedSteer`** | `true` (lái mượt, mạn) | `true` | `false` (lái gấp, bẻ cực nhanh) |
| **`raycastLength`** | 10m | 15m | 25m+ ("khôn" hơn, né xa) |
| **`nextWaypointPassDistance`**| 20m (ôm cua rộng) | 15m | 10m (ép sát đường cua) |

### Code mẫu script quản lý độ khó (`AIDifficultyController.cs`)

Tạo script C# mới tên `AIDifficultyController.cs` và gắn vào GameObject quản lý màn chơi:

```csharp
using UnityEngine;

public enum AIDifficulty { Easy, Medium, Hard }

public class AIDifficultyController : MonoBehaviour 
{
    [Header("Target AI Vehicle")]
    public RCCP_AI aiVehicle;

    [Header("Current Difficulty")]
    public AIDifficulty currentDifficulty = AIDifficulty.Medium;

    private void Start()
    {
        ApplyDifficulty(currentDifficulty);
    }

    public void ApplyDifficulty(AIDifficulty level)
    {
        if (aiVehicle == null)
            aiVehicle = FindFirstObjectByType<RCCP_AI>();

        if (aiVehicle == null)
        {
            Debug.LogWarning("[AIDifficultyController] Không tìm thấy RCCP_AI trong scene!");
            return;
        }

        currentDifficulty = level;

        switch (level)
        {
            case AIDifficulty.Easy:
                aiVehicle.limitSpeed = true;
                aiVehicle.maximumSpeed = 70f;       // Giới hạn 70 km/h
                aiVehicle.smoothedSteer = true;      // Lái mượt, phản ứng vừa phải
                aiVehicle.raycastLength = 10f;
                aiVehicle.nextWaypointPassDistance = 20;
                break;

            case AIDifficulty.Medium:
                aiVehicle.limitSpeed = true;
                aiVehicle.maximumSpeed = 100f;      // Giới hạn 100 km/h
                aiVehicle.smoothedSteer = true;
                aiVehicle.raycastLength = 15f;
                aiVehicle.nextWaypointPassDistance = 15;
                break;

            case AIDifficulty.Hard:
                aiVehicle.limitSpeed = false;       // Không giới hạn tốc độ (chạy hết ga)
                aiVehicle.smoothedSteer = false;     // Phản ứng bẻ lái cực nhanh
                aiVehicle.raycastLength = 25f;      // Phát hiện vật cản từ rất xa ("khôn" hơn)
                aiVehicle.nextWaypointPassDistance = 10;
                break;
        }

        Debug.Log($"[AIDifficultyController] Đã áp dụng độ khó: {level}");
    }
}
```
