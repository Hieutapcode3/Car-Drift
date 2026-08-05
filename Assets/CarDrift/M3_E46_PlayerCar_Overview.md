# M3_E46 Player Car Overview

## Tong Quan

`M3_E46` la prefab xe player day du cua Realistic Car Controller Pro, khong chi la model 3D.

Prefab chinh:

`Assets/Realistic Car Controller Pro/Addons/Installed/Demo Content/Prefabs/Vehicles/M3_E46.prefab`

Prefab nay gom root dieu khien vat ly, cac module RCCP cho engine, gearbox, input, axle, wheel collider, den, audio, damage, camera, customization, va child model de render.

## Cac File/Asset Lien Quan

- Prefab xe: `Assets/Realistic Car Controller Pro/Addons/Installed/Demo Content/Prefabs/Vehicles/M3_E46.prefab`
- Model 3D: `Assets/Realistic Car Controller Pro/Addons/Installed/Demo Content/Models/Vehicles/E46_New/E46_New.FBX`
- Materials: `Assets/Realistic Car Controller Pro/Addons/Installed/Demo Content/Models/Vehicles/E46_New/Mat`
- Textures: `Assets/Realistic Car Controller Pro/Addons/Installed/Demo Content/Models/Vehicles/E46_New/Tex`
- Collider meshes: `Assets/Realistic Car Controller Pro/Addons/Installed/Demo Content/Models/Vehicles/E46_New/RCCP_ColliderMeshes_*`
- Wheel blur material: `Assets/Realistic Car Controller Pro/Materials/Wheel Blurs/M3_E46_WheelBlur.mat`

## Xe Duoc Spawn Trong Game Nhu The Nao

Trong project `CarDrift`, xe `M3_E46` duoc dai dien bang enum:

`Assets/CarDrift/Scripts/CoreScript/GlobalEnum.cs`

```csharp
M3_E46
```

`CarCatalog` map enum nay sang prefab name bang `carType.ToString()`, nen `CarType.M3_E46` se tro toi:

```text
Assets/Realistic Car Controller Pro/Addons/Installed/Demo Content/Prefabs/Vehicles/M3_E46.prefab
```

File lien quan:

- `Assets/CarDrift/Scripts/Manager/CarCatalog.cs`
- `Assets/CarDrift/Scripts/Manager/CarSpawner.cs`
- `Assets/CarDrift/Scripts/Manager/DataManager.cs`
- `Assets/CarDrift/Scripts/Manager/CarUpgradeApplier.cs`

Luon spawn:

```text
DataManager.SelectedCar
-> CarSpawner.SpawnSelectedCar()
-> CarSpawner.Spawn(carType, applySavedUpgrade)
-> RCCP.SpawnRCC(entry.prefab, position, rotation, registerAsPlayer, true, engineRunningOnSpawn)
-> CarUpgradeApplier.Apply(...)
```

## Root Cua Prefab M3_E46

Root object `M3_E46` co:

- `Rigidbody`
- `RCCP_CarController`

`RCCP_CarController` la controller trung tam cua xe.

File:

`Assets/Realistic Car Controller Pro/Scripts/Vehicle/RCCP_CarController.cs`

No gom du lieu tu cac module con:

- input
- engine
- clutch
- gearbox
- differential
- axles
- wheel colliders
- lights
- damage
- particles
- audio
- other addons
- customizer

Trong `FixedUpdate()`, controller doc input va tinh trang thai xe:

```text
PlayerInputs()
-> VehicleInputs()
-> lay du lieu Engine/Gearbox/Axles
-> tinh speed, RPM, torque, gear, slip
```

## Luong Hoat Dong Chinh

Luong dieu khien xe co ban:

```text
Input
-> RCCP_Input
-> RCCP_CarController
-> RCCP_Engine
-> RCCP_Clutch
-> RCCP_Gearbox
-> RCCP_Differential
-> RCCP_Axles
-> RCCP_Axle
-> RCCP_WheelCollider
-> Unity WheelCollider
-> Rigidbody
```

Noi ngan gon:

- `RCCP_Input` lay throttle, brake, steer, handbrake.
- `RCCP_CarController` gom input va trang thai.
- `RCCP_Engine` tinh RPM va torque.
- `RCCP_Gearbox`, `RCCP_Clutch`, `RCCP_Differential` truyen torque.
- `RCCP_Axle` chia torque/phanh/lai cho tung banh.
- `RCCP_WheelCollider` ap torque vao `WheelCollider`.
- `Rigidbody` cua root xe bi tac dong va xe di chuyen.

## Cac Child Gameplay Chinh

### RCCP_Inputs

File:

`Assets/Realistic Car Controller Pro/Scripts/Vehicle/RCCP_Input.cs`

Chuc nang:

- Lay input tu `RCCP_InputManager`.
- Luu `throttleInput`, `brakeInput`, `steerInput`, `handbrakeInput`, `clutchInput`, `nosInput`.
- Ho tro `OverrideInputs(...)` de AI hoac script ngoai dieu khien xe.
- Xu ly steering limiter, counter steering, auto reverse, throttle cut khi sang so.

Trong project nay, `CarDriftAI` cung dung `OverrideInputs(...)` de dieu khien xe AI.

### RCCP_Engine

File:

`Assets/Realistic Car Controller Pro/Scripts/Vehicle/RCCP_Engine.cs`

Chuc nang:

- Tinh `engineRPM`.
- Tinh `fuelInput`.
- Tinh torque dau ra `producedTorqueAsNM`.
- Xu ly rev limiter, turbo, engine inertia.
- Lay throttle tu `CarController.throttleInput_P`.

### RCCP_Clutch, RCCP_Gearbox, RCCP_Differential

Chuc nang:

- `RCCP_Clutch`: xu ly muc bam/nhả clutch.
- `RCCP_Gearbox`: quan ly so hien tai, so lui, so N, shift.
- `RCCP_Differential`: chia torque tu gearbox xuong cac banh duoc drive.

### RCCP_Axles

File:

`Assets/Realistic Car Controller Pro/Scripts/Vehicle/RCCP_Axles.cs`

Child:

- `RCCP_Axle_Front`
- `RCCP_Axle_Rear`

Moi axle co:

- `WheelCollider_L`
- `WheelCollider_R`

### RCCP_Axle

File:

`Assets/Realistic Car Controller Pro/Scripts/Vehicle/RCCP_Axle.cs`

Chuc nang:

- Nhan throttle/brake/steer/handbrake tu `CarController`.
- Tinh steer angle.
- Tinh motor torque va brake torque.
- Ap torque vao `RCCP_WheelCollider`.
- Xu ly anti-roll force giua banh trai/phai.

### RCCP_WheelCollider

File:

`Assets/Realistic Car Controller Pro/Scripts/Vehicle/RCCP_WheelCollider.cs`

Chuc nang:

- Boc Unity `WheelCollider`.
- Ap `motorTorque`, `brakeTorque`, `steerAngle`.
- Doc wheel hit, slip, ground material.
- Canh visual wheel model theo wheel collider.
- Xu ly skidmark, friction, ABS/TCS/ESP lien quan den banh.

## Cac Child Hieu Ung Va He Phu

### RCCP_Lights

Quan ly den xe:

- Headlight low/high
- Brake light
- Reverse light
- Indicator left/right

Ben duoi co nhieu object `RCCP_Light_*`, moi object gan script `RCCP_Light` va Light component.

### RCCP_Damage

Nhan collision tu `RCCP_CarController.OnCollisionEnter`.

Chuc nang:

- Xu ly bien dang/hu hong.
- Tuong tac voi detachable parts.

### RCCP_Particles

Quan ly cac particle lien quan den xe:

- khoi banh
- skid
- bui/dat
- hieu ung mat duong

### RCCP_Audio

Quan ly am thanh:

- tieng engine
- skid
- crash
- turbo/NOS neu co

### RCCP_LOD

Dieu khien level of detail theo khoang cach camera de giam chi phi render/logic.

### RCCP_Aero

Tinh cac luc khi dong hoc nhu downforce.

### RCCP_Stability

Quan ly cac he ho tro:

- ABS
- ESP
- TCS
- steering helper
- traction helper

## RCCP_OtherAddons

Child `RCCP_OtherAddons` la noi gom cac addon phu.

Ben duoi co:

- `RCCP_BodyTilt`: nghieng than xe theo luc.
- `RCCP_NOS`: nitro.
- `RCCP_Dashboard`: kim dong ho, UI/visual dashboard trong xe.
- `RCCP_ExteriorCameras`: camera mui xe va camera banh xe.
- `RCCP_Exhausts`: ong xa va lua ong xa.
- `RCCP_WheelBlur`: hieu ung banh quay nhanh.

## Camera Con Cua Xe

Trong `RCCP_ExteriorCameras` co:

- `RCCP_HoodCamera`
- `RCCP_WheelCamera`

Day la cac diem camera ma `RCCP_Camera` co the parent vao khi doi goc nhin.

Khi bam nut doi camera, `RCCP_Camera.ChangeCamera()` co the chuyen qua:

- TPS
- FPS/Hood
- Wheel
- Fixed
- Cinematic
- Top
- TruckTrailer

## Exhaust

Trong `RCCP_Exhausts` co 2 object:

- `RCCP_Exhaust`
- `RCCP_Exhaust (1)`

Moi cai co:

- script `RCCP_Exhaust`
- particle flame child `RCCExhaustFlame`
- light/particle/audio lien quan den lua ong xa

## Customization

Trong `RCCP_Customizer` co cay `Customizations`.

Ben duoi gom:

- `Spoilers`
- `Sirens`
- `Upgrades`
- `Paints`
- `Wheels`
- `Customization`
- `Decals`
- `Neons`

### Spoilers

Co nhieu child:

- `Spoiler_00`
- `Spoiler_01`
- ...
- `Spoiler_15`

Da so inactive. Khi chon/nang cap spoiler, manager se bat dung object can dung.

### Upgrades

Child `Upgrades` co:

- `Engine`
- `Handling`
- `Brake`
- `Upgrade_Speed`

Project `CarDrift` apply upgrade qua:

`Assets/CarDrift/Scripts/Manager/CarUpgradeApplier.cs`

No goi:

```csharp
upgradeManager.UpgradeEngineWithoutSave(accelerationLevel);
upgradeManager.UpgradeBrakeWithoutSave(brakeLevel);
upgradeManager.UpgradeHandlingWithoutSave(driftLevel);
upgradeManager.UpgradeSpeedWithoutSave(speedLevel);
```

### Paints

Dung de doi mau son xe.

### Decals

Co cac decal:

- front
- back
- left
- right

### Neons

Dung de bat/tat den gam.

## Model Va Collider

Prefab co child model rieng cung ten `M3_E46`.

Ben trong co:

- `Body`
- `Interior`
- `SteeringWheel`
- `Needle_RPM`
- `Needle_KM`
- `Headlights_Left`
- `Headlights_Right`
- `Brake_L`
- `Brake_R`
- `Indicator_*`
- `Glass`
- `Windows_Rear`
- `Dashboard`
- `Engine`
- `Exhausts`

Day la phan nhin thay cua xe.

Trong `Body` va cac part co nhieu `RCCP_Colliders_Collider_chunk_*`, day la mesh collider sinh tu model de va cham chinh xac hon.

## Detachable Parts

Prefab co cac part co the tach/hu hong:

- `Trunk`
- `Hood`
- `Door_R`
- `Door_L`
- `Bumper_R`
- `Bumper_F`

Moi part thuong co:

- `RCCP_DetachablePart`
- `Rigidbody`
- joint component
- child `COM`
- child `Model`
- collider mesh

Khi bi va cham/damage du manh, cac part nay co the bi tac dong rieng hoac tach khoi xe.

## Tom Tat Nhanh

`M3_E46.prefab` la mot xe RCCP hoan chinh:

- Root `M3_E46`: vat ly va controller tong.
- `RCCP_Inputs`: lay input player/AI.
- `RCCP_Engine/Gearbox/Differential`: tao va truyen torque.
- `RCCP_Axles/WheelColliders`: bien torque thanh chuyen dong.
- `RCCP_Lights/Audio/Particles/Damage`: hieu ung va damage.
- `RCCP_OtherAddons`: NOS, dashboard, exhaust, wheel blur, cameras.
- `RCCP_Customizer`: nang cap, son, spoiler, decal, neon.
- Child model `M3_E46`: mesh render, noi that, den, collider.
- Detachable parts: cua, nap capo, co xe, bumper.

Neu can sua gameplay xe, thuong xem cac module RCCP.
Neu can sua hinh anh, xem child model/material.
Neu can sua nang cap trong game, xem `CarUpgradeApplier` va `RCCP_Customizer`.
