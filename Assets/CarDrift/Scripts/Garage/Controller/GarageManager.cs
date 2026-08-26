using UnityEngine;

public class GarageManager : MonoBehaviour
{
    public static GarageManager Instance { get; private set; }

    [Header("Database Reference")]
    public CarDatabaseSO carDatabase;

    [Header("Spawn Settings")]
    public Transform spawnPoint;
    [SerializeField] private CarController carControllerPrefab;
    [SerializeField] private CarController garageCarController;

    [Header("Camera Orbit Settings")]
    public Camera garageCamera;
    [SerializeField] private float cameraDistance = 5.5f;
    [SerializeField] private float cameraHeight = 1.1f;
    [SerializeField] private float cameraPitch = 10f;
    [SerializeField] private float orbitSmoothSpeed = 12f;

    [Header("State (ReadOnly)")]
    [SerializeField] private int currentCarIndex = 0;
    private RCCP_CarController currentRCCPCar;
    private RCCP_Customizer currentCustomizer;

    private float targetYaw = 0f;
    private float currentYaw = 0f;
    private float defaultYaw = 0f;
    private bool isOrbitInitialized = false;

    [Header("Menu Loading Settings")]
    [Tooltip("Hiển thị LoadingPanel khi khởi chạy MenuScene lần đầu")]
    [SerializeField] private bool showLoadingOnStart = true;
    public static bool hasCompletedInitialMenuLoading = false;

    public int CurrentCarIndex => currentCarIndex;
    public CarDataSO CurrentCarData
    {
        get
        {
            CarDatabaseSO db = carDatabase != null ? carDatabase : CarDatabaseSO.Instance;
            return (db != null && db.cars != null && currentCarIndex >= 0 && currentCarIndex < db.cars.Count)
                ? db.cars[currentCarIndex]
                : null;
        }
    }

    public CarController CurrentCarController => garageCarController;
    public RCCP_CarController CurrentRCCPCar => currentRCCPCar;
    public RCCP_Customizer CurrentCustomizer => currentCustomizer;

    public static event System.Action<CarDataSO> OnCarChanged;

    public static bool IsInGarageScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return sceneName.Contains("Menu") || sceneName.Contains("Garage");
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (carDatabase == null)
        {
            carDatabase = CarDatabaseSO.Instance;
        }
        currentCarIndex = CarSaveManager.GetSelectedCarIndex();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private async void Start()
    {
        if (!Application.isPlaying) return;
        if (!IsInGarageScene()) return;

        if (showLoadingOnStart && !hasCompletedInitialMenuLoading)
        {
            await InitializeMenuSceneWithLoadingAsync();
        }
        else
        {
            EnsureGarageCarController();
            InitCameraOrbit();
            SpawnCar(currentCarIndex);
            OnCarChanged?.Invoke(CurrentCarData);
        }
    }

    public async System.Threading.Tasks.Task InitializeMenuSceneWithLoadingAsync()
    {
        LoadingPanel loadingPanel = null;
        if (HUDSystem.Instance != null)
        {
            loadingPanel = HUDSystem.Instance.Show<LoadingPanel>();
            HUDSystem.Instance.Hide<MenuPanel>();
        }

        try
        {
            loadingPanel?.SetProgress(0.05f, "Initializing Game Assets...");

            // 1. Khởi tạo Addressables hệ thống
            var initHandle = UnityEngine.AddressableAssets.Addressables.InitializeAsync();
            while (!initHandle.IsDone)
            {
                float p = 0.05f + (initHandle.PercentComplete * 0.35f);
                loadingPanel?.SetProgress(p, "Loading Game Assets...");
                await System.Threading.Tasks.Task.Yield();
            }

            loadingPanel?.SetProgress(0.40f, "Loading Garage Database...");

            // 2. Khởi tạo Database & Setup Camera Orbit
            if (carDatabase == null)
            {
                carDatabase = CarDatabaseSO.Instance;
            }
            EnsureGarageCarController();
            InitCameraOrbit();

            // 3. Tải Model xe đã chọn trong Garage với tiến trình %
            await SpawnCarAsync(currentCarIndex, (p, msg) =>
            {
                float totalProgress = 0.40f + (p * 0.55f); // 40% -> 95%
                loadingPanel?.SetProgress(totalProgress, msg);
            });

            loadingPanel?.SetProgress(1.0f, "Ready!");
            if (loadingPanel != null)
            {
                await loadingPanel.WaitForVisualProgressAsync();
            }

            await System.Threading.Tasks.Task.Delay(150);
            hasCompletedInitialMenuLoading = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GarageManager] Lỗi trong quá trình khởi tạo MenuScene: {ex.Message}");
        }
        finally
        {
            if (HUDSystem.Instance != null)
            {
                HUDSystem.Instance.Hide<LoadingPanel>();
                HUDSystem.Instance.Show<MenuPanel>();
            }
            OnCarChanged?.Invoke(CurrentCarData);
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || !IsInGarageScene() || garageCamera == null || spawnPoint == null || garageCarController == null) return;

        currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * orbitSmoothSpeed);

        Vector3 targetCenter = spawnPoint.position + Vector3.up * cameraHeight;
        Quaternion rotation = Quaternion.Euler(cameraPitch, currentYaw, 0f);
        Vector3 desiredPosition = targetCenter + rotation * (Vector3.back * cameraDistance);

        garageCamera.transform.position = desiredPosition;
        garageCamera.transform.LookAt(targetCenter);
    }

    private void InitCameraOrbit()
    {
        if (!IsInGarageScene()) return;

        if (garageCamera == null)
        {
            // Only find Camera specifically named GarageCamera or MainCamera in Garage scene
            GameObject gCam = GameObject.Find("GarageCamera") ?? GameObject.Find("Garage_Camera");
            if (gCam != null)
            {
                garageCamera = gCam.GetComponent<Camera>();
            }
            else if (Camera.main != null && spawnPoint != null)
            {
                garageCamera = Camera.main;
            }
        }

        if (garageCamera != null)
        {
            Vector3 center = (spawnPoint != null ? spawnPoint.position : Vector3.zero) + Vector3.up * cameraHeight;
            Vector3 dir = garageCamera.transform.position - center;
            cameraDistance = Mathf.Max(2f, new Vector3(dir.x, 0f, dir.z).magnitude);
            if (cameraDistance > 0.01f)
            {
                defaultYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + 180f;
            }
            targetYaw = defaultYaw;
            currentYaw = defaultYaw;
            isOrbitInitialized = true;
        }
    }

    public void RotateCameraOrbit(float deltaYaw)
    {
        if (!IsInGarageScene()) return;
        targetYaw += deltaYaw;
    }

    public void ResetCameraOrbit(bool snap = false)
    {
        if (!IsInGarageScene()) return;
        targetYaw = defaultYaw;
        if (snap)
        {
            currentYaw = defaultYaw;
        }
        if (garageCarController != null && spawnPoint != null)
        {
            garageCarController.transform.position = spawnPoint.position;
            garageCarController.transform.rotation = spawnPoint.rotation;
        }
    }

    private void EnsureGarageCarController()
    {
        if (!IsInGarageScene()) return;
        if (garageCarController != null) return;

        GameObject existingGarageCar = GameObject.Find("Garage_CarController");
        if (existingGarageCar != null)
        {
            garageCarController = existingGarageCar.GetComponent<CarController>();
        }

        if (garageCarController == null && spawnPoint != null)
        {
            if (carControllerPrefab != null)
            {
                garageCarController = Instantiate(carControllerPrefab, spawnPoint.position, spawnPoint.rotation);
                garageCarController.gameObject.name = "Garage_CarController";
            }
            else
            {
                GameObject carGo = new GameObject("Garage_CarController");
                carGo.transform.position = spawnPoint.position;
                carGo.transform.rotation = spawnPoint.rotation;
                garageCarController = carGo.AddComponent<CarController>();
            }
        }
    }

    private int currentSpawnRequestId = 0;

    public void SpawnCar(int index)
    {
        _ = SpawnCarAsync(index);
    }

    public async System.Threading.Tasks.Task SpawnCarAsync(int index, System.Action<float, string> onProgress = null)
    {
        if (!Application.isPlaying) return;
        if (!IsInGarageScene()) return;

        CarDatabaseSO db = carDatabase != null ? carDatabase : CarDatabaseSO.Instance;
        if (db == null || db.cars == null || db.cars.Count == 0) return;

        currentCarIndex = Mathf.Clamp(index, 0, db.cars.Count - 1);
        CarDataSO data = CurrentCarData;
        if (data == null) return;

        int thisSpawnId = ++currentSpawnRequestId;

        onProgress?.Invoke(0.1f, $"Preparing {data.carName}...");

        EnsureGarageCarController();
        if (garageCarController == null) return;

        // Reset rotation to default spawn orientation
        if (spawnPoint != null)
        {
            garageCarController.transform.position = spawnPoint.position;
            garageCarController.transform.rotation = spawnPoint.rotation;
        }

        ResetCameraOrbit(true);

        garageCarController.autoLoadOnStart = false;
        garageCarController.controllerType = ControllerType.Menu;
        garageCarController.isMenuModel = true;

        onProgress?.Invoke(0.35f, $"Loading Model ({data.carType})...");
        await garageCarController.LoadCarModelAsync(data.carType);

        // Nếu trong lúc chờ load đã có yêu cầu spawn xe mới hơn thì bỏ qua
        if (thisSpawnId != currentSpawnRequestId)
        {
            return;
        }

        onProgress?.Invoke(0.8f, $"Configuring {data.carName}...");

        // Khóa toàn bộ Rigidbody trên xe mô hình trong Garage
        Rigidbody[] allRbs = garageCarController.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in allRbs)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        currentRCCPCar = garageCarController.carController;
        if (currentRCCPCar == null)
        {
            currentRCCPCar = garageCarController.GetComponentInChildren<RCCP_CarController>(true);
        }

        if (currentRCCPCar != null)
        {
            currentRCCPCar.SetCanControl(false);
            currentCustomizer = currentRCCPCar.Customizer;
            if (currentCustomizer == null)
            {
                currentCustomizer = currentRCCPCar.gameObject.GetComponentInChildren<RCCP_Customizer>(true);
                if (currentCustomizer == null)
                {
                    currentCustomizer = currentRCCPCar.gameObject.AddComponent<RCCP_Customizer>();
                }
            }

            currentCustomizer.autoSave = false;
            currentCustomizer.Initialize();
            currentCustomizer.autoSave = false;
            ApplySavedUpgradesAndCustoms(data, garageCarController);
        }

        onProgress?.Invoke(1.0f, $"Loaded {data.carName}");
    }

    public void ShowCar(int index)
    {
        if (!Application.isPlaying) return;
        CarDatabaseSO db = carDatabase != null ? carDatabase : CarDatabaseSO.Instance;
        if (db == null || db.cars == null || db.cars.Count == 0) return;

        currentCarIndex = Mathf.Clamp(index, 0, db.cars.Count - 1);
        SpawnCar(currentCarIndex);
        OnCarChanged?.Invoke(CurrentCarData);
    }

    public void NextCar()
    {
        if (!Application.isPlaying) return;
        CarDatabaseSO db = carDatabase != null ? carDatabase : CarDatabaseSO.Instance;
        if (db == null || db.cars == null || db.cars.Count == 0) return;

        int next = (currentCarIndex + 1) % db.cars.Count;
        ShowCar(next);
    }

    public void PreviousCar()
    {
        if (!Application.isPlaying) return;
        CarDatabaseSO db = carDatabase != null ? carDatabase : CarDatabaseSO.Instance;
        if (db == null || db.cars == null || db.cars.Count == 0) return;

        int prev = (currentCarIndex - 1 + db.cars.Count) % db.cars.Count;
        ShowCar(prev);
    }

    public void ApplySavedUpgradesAndCustoms(CarDataSO data, CarController controller = null)
    {
        if (!Application.isPlaying) return;
        if (data == null) return;
        if (controller == null) controller = CurrentCarController;
        if (controller == null) return;

        RCCP_Customizer customizer = controller.carController != null ? controller.carController.Customizer : null;
        if (customizer == null)
        {
            customizer = controller.GetComponentInChildren<RCCP_Customizer>(true);
            if (customizer == null && controller.carController != null)
            {
                customizer = controller.carController.gameObject.AddComponent<RCCP_Customizer>();
            }
        }

        if (customizer == null) return;

        customizer.Initialize();

        string id = data.carID;

        // Upgrades
        int engineLvl = CarSaveManager.GetUpgradeLevel(id, UpgradeType.Engine);
        int handlingLvl = CarSaveManager.GetUpgradeLevel(id, UpgradeType.Handling);
        int brakeLvl = CarSaveManager.GetUpgradeLevel(id, UpgradeType.Brake);
        int speedLvl = CarSaveManager.GetUpgradeLevel(id, UpgradeType.Speed);

        if (customizer.UpgradeManager != null)
        {
            customizer.UpgradeManager.UpgradeEngineWithoutSave(engineLvl);
            customizer.UpgradeManager.UpgradeHandlingWithoutSave(handlingLvl);
            customizer.UpgradeManager.UpgradeBrakeWithoutSave(brakeLvl);
            customizer.UpgradeManager.UpgradeSpeedWithoutSave(speedLvl);
        }

        // Customizations
        CustomConfigSO cfg = (carDatabase != null && carDatabase.customConfig != null)
            ? carDatabase.customConfig
            : CustomConfigSO.Instance;

        if (cfg != null)
        {
            // Paint
            int paintIdx = CarSaveManager.GetCustomIndex(id, CustomType.Paint);
            if (paintIdx >= 0 && cfg.paints != null && paintIdx < cfg.paints.Length)
            {
                if (customizer.PaintManager != null)
                    customizer.PaintManager.PaintWithoutSave(cfg.paints[paintIdx].color);
            }
            else
            {
                if (customizer.PaintManager != null)
                    customizer.PaintManager.Restore();
            }

            // Wheels
            int wheelIdx = CarSaveManager.GetCustomIndex(id, CustomType.Wheels);
            if (wheelIdx >= 0 && cfg.wheels != null && wheelIdx < cfg.wheels.Length)
            {
                if (customizer.WheelManager != null)
                {
                    customizer.WheelManager.UpdateWheelWithoutSave(wheelIdx);
                }
            }
            else
            {
                if (customizer.WheelManager != null)
                    customizer.WheelManager.Restore();
            }

            // Spoiler
            int spoilerIdx = CarSaveManager.GetCustomIndex(id, CustomType.Spoiler);
            if (spoilerIdx >= 0 && cfg.spoilers != null)
            {
                int spoilerConfigIdx = spoilerIdx;
                for (int i = 0; i < cfg.spoilers.Length; i++)
                {
                    if (cfg.spoilers[i].indexInConfig == spoilerIdx)
                    {
                        spoilerConfigIdx = i;
                        break;
                    }
                }
                if (customizer.SpoilerManager != null)
                {
                    customizer.SpoilerManager.UpgradeWithoutSave(spoilerConfigIdx);
                }
            }
            else
            {
                if (customizer.SpoilerManager != null)
                    customizer.SpoilerManager.Restore();
            }

            // Neon
            int neonIdx = CarSaveManager.GetCustomIndex(id, CustomType.Neon);
            if (neonIdx >= 0 && cfg.neons != null && neonIdx < cfg.neons.Length)
            {
                if (customizer.NeonManager != null)
                {
                    Material mat = cfg.neons[neonIdx].neonMat;
                    if (mat != null)
                        customizer.NeonManager.UpgradeWithoutSave(mat);
                    else
                        customizer.NeonManager.Restore();
                }
            }
            else
            {
                if (customizer.NeonManager != null)
                    customizer.NeonManager.Restore();
            }
        }
    }

    public bool BuyCurrentCar()
    {
        CarDataSO data = CurrentCarData;
        if (data == null) return false;
        if (CarSaveManager.IsCarUnlocked(data)) return true;

        if (CurrencyManager.SpendGold(data.unlockCostGold))
        {
            CarSaveManager.UnlockCar(data);
            CarSaveManager.SetSelectedCarIndex(currentCarIndex);
            return true;
        }

        return false;
    }

    public void SelectCurrentCar()
    {
        CarDataSO data = CurrentCarData;
        if (data == null) return;
        if (!CarSaveManager.IsCarUnlocked(data)) return;

        CarSaveManager.SetSelectedCarIndex(currentCarIndex);
    }

    public bool UpgradeCurrentCar(UpgradeType type)
    {
        CarDataSO data = CurrentCarData;
        if (data == null || carDatabase == null || carDatabase.upgradeConfig == null) return false;
        if (!CarSaveManager.IsCarUnlocked(data)) return false;

        string id = data.carID;
        int currentLvl = CarSaveManager.GetUpgradeLevel(id, type);
        if (currentLvl >= UpgradeConfigSO.MAX_LEVEL) return false; // Already maxed

        // Next level data (index = currentLvl)
        UpgradeLevelData nextLevelData = carDatabase.upgradeConfig.GetUpgradeData(type, currentLvl);

        if (CurrencyManager.SpendCurrency(nextLevelData.costGold, nextLevelData.costSilver))
        {
            int newLvl = currentLvl + 1;
            CarSaveManager.SetUpgradeLevel(id, type, newLvl);
            ApplySavedUpgradesAndCustoms(data, CurrentCarController);
            return true;
        }

        return false;
    }

    public bool CustomCurrentCar(CustomType type, int itemIndex)
    {
        CarDataSO data = CurrentCarData;
        if (data == null || carDatabase == null || carDatabase.customConfig == null) return false;
        if (!CarSaveManager.IsCarUnlocked(data)) return false;

        string id = data.carID;
        int priceGold = 0;

        CustomConfigSO cfg = carDatabase.customConfig;
        switch (type)
        {
            case CustomType.Wheels:
                if (cfg.wheels != null && itemIndex >= 0 && itemIndex < cfg.wheels.Length)
                    priceGold = cfg.wheels[itemIndex].priceGold;
                break;
            case CustomType.Paint:
                if (cfg.paints != null && itemIndex >= 0 && itemIndex < cfg.paints.Length)
                    priceGold = cfg.paints[itemIndex].priceGold;
                break;
            case CustomType.Spoiler:
                if (cfg.spoilers != null)
                {
                    for (int i = 0; i < cfg.spoilers.Length; i++)
                    {
                        if (cfg.spoilers[i].indexInConfig == itemIndex)
                        {
                            priceGold = cfg.spoilers[i].priceGold;
                            break;
                        }
                    }
                    if (priceGold == 0 && itemIndex >= 0 && itemIndex < cfg.spoilers.Length)
                    {
                        priceGold = cfg.spoilers[itemIndex].priceGold;
                    }
                }
                break;
            case CustomType.Neon:
                if (cfg.neons != null && itemIndex >= 0 && itemIndex < cfg.neons.Length)
                    priceGold = cfg.neons[itemIndex].priceGold;
                break;
        }

        if (CurrencyManager.SpendGold(priceGold))
        {
            CarSaveManager.UnlockCustom(type, itemIndex);
            CarSaveManager.SetCustomIndex(id, type, itemIndex);
            ApplySavedUpgradesAndCustoms(data, CurrentCarController);
            return true;
        }

        return false;
    }
}
