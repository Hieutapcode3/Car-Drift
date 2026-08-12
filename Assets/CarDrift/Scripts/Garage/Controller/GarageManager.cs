using UnityCommunity.UnitySingleton;
using UnityEngine;

public class GarageManager : MonoSingleton<GarageManager>
{
    [Header("Database Reference")]
    public CarDatabaseSO carDatabase;

    [Header("Spawn Settings")]
    public Transform spawnPoint;

    [Header("State (ReadOnly)")]
    [SerializeField] private int currentCarIndex = 0;
    private GameObject currentCarInstance;
    private RCCP_CarController currentRCCPCar;
    private RCCP_Customizer currentCustomizer;

    public int CurrentCarIndex => currentCarIndex;
    public CarDataSO CurrentCarData => carDatabase != null ? carDatabase.GetCarByIndex(currentCarIndex) : null;
    public RCCP_CarController CurrentRCCPCar => currentRCCPCar;
    public RCCP_Customizer CurrentCustomizer => currentCustomizer;

    protected override void Awake()
    {
        base.Awake();
        currentCarIndex = CarSaveManager.GetSelectedCarIndex();
    }

    private void Start()
    {
        SpawnCar(currentCarIndex);
    }

    public void ShowCar(int index)
    {
        if (carDatabase == null || carDatabase.cars == null || carDatabase.cars.Count == 0) return;
        currentCarIndex = Mathf.Clamp(index, 0, carDatabase.cars.Count - 1);
        SpawnCar(currentCarIndex);
    }

    public void NextCar()
    {
        if (carDatabase == null || carDatabase.cars == null || carDatabase.cars.Count == 0) return;
        int next = (currentCarIndex + 1) % carDatabase.cars.Count;
        ShowCar(next);
    }

    public void PreviousCar()
    {
        if (carDatabase == null || carDatabase.cars == null || carDatabase.cars.Count == 0) return;
        int prev = (currentCarIndex - 1 + carDatabase.cars.Count) % carDatabase.cars.Count;
        ShowCar(prev);
    }

    private void SpawnCar(int index)
    {
        if (currentCarInstance != null)
        {
            Destroy(currentCarInstance);
            currentCarInstance = null;
            currentRCCPCar = null;
            currentCustomizer = null;
        }

        CarDataSO data = carDatabase != null ? carDatabase.GetCarByIndex(index) : null;
        if (data == null || data.carPrefab == null) return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        currentCarInstance = Instantiate(data.carPrefab, pos, rot);
        if (spawnPoint != null)
            currentCarInstance.transform.SetParent(spawnPoint, true);

        // Khóa toàn bộ Rigidbody trên xe mô hình trong Garage
        Rigidbody[] allRbs = currentCarInstance.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in allRbs)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        CarController wrapper = currentCarInstance.GetComponentInChildren<CarController>(true);
        if (wrapper != null)
        {
            wrapper.controllerType = ControllerType.Menu;
            wrapper.isMenuModel = true;
            wrapper.ApplyControlState();
        }

        currentRCCPCar = currentCarInstance.GetComponentInChildren<RCCP_CarController>(true);
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

            ApplySavedUpgradesAndCustoms(data);
        }
    }

    public void ApplySavedUpgradesAndCustoms(CarDataSO data)
    {
        if (data == null || currentCustomizer == null) return;

        string id = data.carID;

        // Upgrades
        int engineLvl = CarSaveManager.GetUpgradeLevel(id, UpgradeType.Engine);
        int handlingLvl = CarSaveManager.GetUpgradeLevel(id, UpgradeType.Handling);
        int brakeLvl = CarSaveManager.GetUpgradeLevel(id, UpgradeType.Brake);
        int speedLvl = CarSaveManager.GetUpgradeLevel(id, UpgradeType.Speed);

        if (currentCustomizer.UpgradeManager != null)
        {
            currentCustomizer.UpgradeManager.UpgradeEngineWithoutSave(engineLvl);
            currentCustomizer.UpgradeManager.UpgradeHandlingWithoutSave(handlingLvl);
            currentCustomizer.UpgradeManager.UpgradeBrakeWithoutSave(brakeLvl);
            currentCustomizer.UpgradeManager.UpgradeSpeedWithoutSave(speedLvl);
        }

        // Customizations
        if (carDatabase != null && carDatabase.customConfig != null)
        {
            CustomConfigSO cfg = carDatabase.customConfig;

            // Paint
            int paintIdx = CarSaveManager.GetCustomIndex(id, CustomType.Paint);
            if (paintIdx >= 0 && cfg.paints != null && paintIdx < cfg.paints.Length)
            {
                if (currentCustomizer.PaintManager != null)
                    currentCustomizer.PaintManager.PaintWithoutSave(cfg.paints[paintIdx].color);
            }

            // Wheels
            int wheelIdx = CarSaveManager.GetCustomIndex(id, CustomType.Wheels);
            if (wheelIdx >= 0 && cfg.wheels != null && wheelIdx < cfg.wheels.Length)
            {
                if (currentCustomizer.WheelManager != null)
                {
                }
            }

            // Spoiler
            int spoilerIdx = CarSaveManager.GetCustomIndex(id, CustomType.Spoiler);
            if (spoilerIdx >= 0 && cfg.spoilers != null && spoilerIdx < cfg.spoilers.Length)
            {

            }

            // Neon
            int neonIdx = CarSaveManager.GetCustomIndex(id, CustomType.Neon);
            if (neonIdx >= 0 && cfg.neons != null && neonIdx < cfg.neons.Length)
            {
                if (currentCustomizer.NeonManager != null)
                {
                    Material mat = cfg.neons[neonIdx].neonMat;
                    if (mat != null)
                        currentCustomizer.NeonManager.UpgradeWithoutSave(mat);
                    else
                        currentCustomizer.NeonManager.Restore();
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
            ApplySavedUpgradesAndCustoms(data);
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
                if (cfg.spoilers != null && itemIndex >= 0 && itemIndex < cfg.spoilers.Length)
                    priceGold = cfg.spoilers[itemIndex].priceGold;
                break;
            case CustomType.Neon:
                if (cfg.neons != null && itemIndex >= 0 && itemIndex < cfg.neons.Length)
                    priceGold = cfg.neons[itemIndex].priceGold;
                break;
        }

        if (CurrencyManager.SpendGold(priceGold))
        {
            CarSaveManager.SetCustomIndex(id, type, itemIndex);
            ApplySavedUpgradesAndCustoms(data);
            return true;
        }

        return false;
    }
}
