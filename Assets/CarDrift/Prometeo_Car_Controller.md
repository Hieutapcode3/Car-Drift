# Hệ Thống PROMETEO - Car Controller

Hệ thống **PROMETEO - Car Controller** là một bộ điều khiển xe ô tô mã nguồn mở, đơn giản và trực quan dành cho Unity, được phát triển bởi **Mena**. Hệ thống này sử dụng các thành phần vật lý cơ bản của Unity như **Rigidbody** và **WheelCollider** để mô phỏng chuyển động, bẻ lái, phanh và cơ chế lướt bánh (drift).

---

## 1. Cấu Trúc Các File Trong Hệ Thống

Hệ thống bao gồm 3 file chính:
* [PrometeoCarController.cs](file:///d:/WorkSpace/Unity/GameDev/Car-Drift/Assets/PROMETEO%20-%20Car%20Controller/Scripts/PrometeoCarController.cs): Lớp xử lý logic vật lý, di chuyển, phanh và drift của xe.
* [PrometeoTouchInput.cs](file:///d:/WorkSpace/Unity/GameDev/Car-Drift/Assets/PROMETEO%20-%20Car%20Controller/Scripts/PrometeoTouchInput.cs): Lớp xử lý sự kiện tương tác bằng nút bấm cảm ứng (Mobile UI).
* [PrometeoEditor.cs](file:///d:/WorkSpace/Unity/GameDev/Car-Drift/Assets/PROMETEO%20-%20Car%20Controller/Editor/PrometeoEditor.cs): Lớp tùy biến giao diện Inspector trong Unity Editor giúp việc thiết lập xe trực quan hơn.

---

## 2. Chi Tiết Các Thành Phần Cấu Trúc

### A. Lớp `PrometeoCarController`
Là trung tâm điều khiển của xe, quản lý các nhóm thuộc tính sau:

#### Cấu Hình Xe (Car Setup)
* `maxSpeed` & `maxReverseSpeed`: Giới hạn tốc độ tiến và lùi tối đa (km/h).
* `accelerationMultiplier`: Hệ số gia tốc (tốc độ tăng tốc của xe).
* `maxSteeringAngle`: Góc bẻ lái tối đa của bánh xe trước.
* `steeringSpeed`: Tốc độ xoay bánh xe khi bẻ lái.
* `brakeForce`: Lực phanh khi nhấn giữ phanh thường.
* `decelerationMultiplier`: Tốc độ tự động giảm tốc khi người chơi buông ga.
* `handbrakeDriftMultiplier`: Hệ số giảm độ bám đường khi kéo phanh tay (quyết định độ trượt khi drift).
* `bodyMassCenter`: Tọa độ điểm trọng tâm của xe (giúp xe ổn định, không bị lật).

#### Bánh Xe (Wheels)
* Lưu trữ tham chiếu đến **WheelCollider** (xử lý vật lý) và **GameObject Mesh** (hiển thị 3D) cho 4 bánh xe:
  * Front Left (Trước - Trái)
  * Front Right (Trước - Phải)
  * Rear Left (Sau - Trái)
  * Rear Right (Sau - Phải)

#### Hiệu Ứng & Âm Thanh (Effects & Sounds)
* `RLWParticleSystem` & `RRWParticleSystem`: Hiệu ứng khói tỏa ra từ 2 bánh sau khi xe drift.
* `RLWTireSkid` & `RRWTireSkid`: Vệt đen lằn bánh xe (`TrailRenderer`) lưu lại trên mặt đường khi mất ma sát.
* `carEngineSound`: Âm thanh động cơ (tự động thay đổi cao độ/pitch dựa theo tốc độ xe).
* `tireScreechSound`: Âm thanh rít lốp khi xe bị drift hoặc khóa bánh.

#### Điều Khiển (Controls)
* Hỗ trợ 2 chế độ:
  1. **Bàn phím (Keyboard)**: Phím `W` (Tiến), `S` (Lùi), `A` (Rẽ trái), `D` (Rẽ phải), `Space` (Phanh tay).
  2. **Cảm ứng (Touch Controls)**: Liên kết với các nút UI di động sử dụng `PrometeoTouchInput`.

---

### B. Lớp `PrometeoTouchInput`
* Được gắn vào các nút bấm UI của thiết bị di động (nút ga, phanh, rẽ trái, rẽ phải, phanh tay).
* Lắng nghe sự kiện nhấn xuống (`ButtonDown()`) và nhả ra (`ButtonUp()`) để thay đổi cờ trạng thái `buttonPressed`, đồng thời thu nhỏ nút bấm để tạo phản hồi thị giác tốt hơn.

---

### C. Lớp `PrometeoEditor`
* Sử dụng `CustomEditor` để ghi đè giao diện Inspector mặc định của `PrometeoCarController`.
* Phân chia các cài đặt thành các nhóm rõ ràng (CAR SETUP, WHEELS, EFFECTS, UI, SOUNDS, TOUCH CONTROLS) bằng các thanh trượt (`IntSlider`, `Slider`) và nhóm ẩn/hiện (`BeginToggleGroup`).

---

## 3. Các Luồng Logic Và Vật Lý Chính

### 1. Luồng Tính Toán Tốc Độ (`Update`)
Tốc độ xe được tính dựa trên số vòng quay mỗi phút (RPM) và bán kính của bánh xe trước bên trái:
```csharp
carSpeed = (2 * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60) / 1000;
```
Đồng thời xác định vận tốc cục bộ của Rigidbody theo trục X (`localVelocityX` - dùng để phát hiện drift) và trục Z (`localVelocityZ` - dùng để phát hiện tiến/lùi).

### 2. Luồng Tiến/Lùi (`GoForward` / `GoReverse`)
* Khi nhấn ga tiến (`GoForward()`), script áp công suất mô-men xoắn (`motorTorque`) dương vào cả 4 bánh xe:
  ```csharp
  collider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
  ```
  Nếu xe vượt quá `maxSpeed`, mô-men xoắn sẽ trả về 0 để duy trì tốc độ ổn định.
* Tương tự khi lùi (`GoReverse()`), mô-men xoắn âm sẽ được áp vào các bánh xe.

### 3. Luồng Bẻ Lái (`TurnLeft` / `TurnRight` / `ResetSteeringAngle`)
* Khi rẽ trái/phải, trục bẻ lái `steeringAxis` (chạy từ -1 đến 1) sẽ thay đổi mượt mà theo thời gian.
* Góc bẻ lái của hai bánh trước (`frontLeftCollider.steerAngle` và `frontRightCollider.steerAngle`) được nội suy tuyến tính (`Mathf.Lerp`) hướng tới góc bẻ lái mục tiêu:
  ```csharp
  var steeringAngle = steeringAxis * maxSteeringAngle;
  frontCollider.steerAngle = Mathf.Lerp(frontCollider.steerAngle, steeringAngle, steeringSpeed);
  ```

### 4. Cơ Chế Drift & Phanh Tay (`Handbrake` & `RecoverTraction`)
Đây là cơ chế cốt lõi tạo nên trải nghiệm drift của Prometeo:
* **Khi kéo phanh tay (`Handbrake()`)**:
  * Trạng thái `isTractionLocked` được đặt thành `true`.
  * Tăng dần hệ số trượt ngang cực đại `extremumSlip` của tất cả các bánh xe dựa trên hệ số `handbrakeDriftMultiplier`. Việc tăng trị số trượt này làm giảm đáng kể độ bám đường (friction) của bánh xe đối với chuyển động ngang, khiến xe dễ bị trượt/văng đuôi khi bẻ cua gấp.
  * Kích hoạt âm thanh rít lốp (`tireScreechSound`) và bật chế độ vẽ vệt bánh xe (`TrailRenderer.emitting = true`).
* **Khi nhả phanh tay (`RecoverTraction()`)**:
  * Trạng thái `isTractionLocked` trở về `false`.
  * Giảm dần hệ số trượt ngang của lốp xe về giá trị mặc định ban đầu (`FLWextremumSlip`), giúp xe từ từ lấy lại độ bám đường ổn định.
