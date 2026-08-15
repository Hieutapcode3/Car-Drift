using UnityCommunity.UnitySingleton;
using UnityEngine;

public class GarageManager : MonoSingleton<GarageManager>
{
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

    protected override void Awake()
    {
        if (Instance != null && Instance != this && Instance.carDatabase == null && this.carDatabase != null)
        {
            Destroy(Instance.gameObject);
        }

        base.Awake();
        if (carDatabase == null)
        {
            carDatabase = CarDatabaseSO.Instance;
        }
        currentCarIndex = CarSaveManager.GetSelectedCarIndex();
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        EnsureGarageCarController();
        InitCameraOrbit();
        SpawnCar(currentCarIndex);
        OnCarChanged?.Invoke(CurrentCarData);
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || garageCamera == null) return;

        currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * orbitSmoothSpeed);

        Vector3 targetCenter = (spawnPoint != null ? spawnPoint.position : (garageCarController != null ? garageCarController.transform.position : Vector3.zero)) + Vector3.up * cameraHeight;
        Quaternion rotation = Quaternion.Euler(cameraPitch, currentYaw, 0f);
        Vector3 desiredPosition = targetCenter + rotation * (Vector3.back * cameraDistance);

        garageCamera.transform.position = desiredPosition;
        garageCamera.transform.LookAt(targetCenter);
    }

    private void InitCameraOrbit()
    {
        if (garageCamera == null)
        {
            // Auto find Camera that renders to RenderTexture or main camera
            Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in cams)
            {
                if (cam.targetTexture != null)
                {
                    garageCamera = cam;
                    break;
                }
            }
            if (garageCamera == null && Camera.main != null)
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
        targetYaw += deltaYaw;
    }

    public void ResetCameraOrbit(bool snap = false)
    {
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
        if (garageCarController != null) return;

        garageCarController = FindFirstObjectByType<CarController>();
        if (garageCarController == null)
        {
            if (carControllerPrefab != null)
            {
                garageCarController = Instantiate(carControllerPrefab, spawnPoint != null ? spawnPoint.position : Vector3.zero, spawnPoint != null ? spawnPoint.rotation : Quaternion.identity);
                garageCarController.gameObject.name = "Garage_CarController";
            }
            else
            {
                GameObject carGo = new GameObject("Garage_CarController");
                if (spawnPoint != null)
                {
                    carGo.transform.position = spawnPoint.position;
                    carGo.transform.rotation = spawnPoint.rotation;
                }
                garageCarController = carGo.AddComponent<CarController>();
            }
        }
    }

    public async void SpawnCar(int index)
    {
        if (!Application.isPlaying) return;

        CarDatabaseSO db = carDatabase != null ? carDatabase : CarDatabaseSO.Instance;
        if (db == null || db.cars == null || db.cars.Count == 0) return;

        currentCarIndex = Mathf.Clamp(index, 0, db.cars.Count - 1);
        CarDataSO data = CurrentCarData;
        if (data == null) return;

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

        await garageCarController.LoadCarModelAsync(data.carType);

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

            currentCustomizer.Initialize();
            ApplySavedUpgradesAndCustoms(data, garageCarController);
        }
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

            // Wheels
            int wheelIdx = CarSaveManager.GetCustomIndex(id, CustomType.Wheels);
            if (wheelIdx >= 0 && cfg.wheels != null && wheelIdx < cfg.wheels.Length)
            {
                if (customizer.WheelManager != null)
                {
                    customizer.WheelManager.UpdateWheelWithoutSave(wheelIdx);
                }
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
