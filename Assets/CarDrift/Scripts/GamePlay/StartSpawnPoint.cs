using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class EnemySpawnConfig
{
    public string name = "Enemy Spawn";
    public CarType carType = CarType.M3_E46;
    public AIDifficulty aiDifficulty = AIDifficulty.Hard;
    public Transform spawnTransform;
}

public class StartSpawnPoint : MonoBehaviour
{
    [Header("Player Settings")]
    public CarType playerCarType = CarType.M3_E46;
    public Transform playerSpawnTransform;

    [Header("Enemy Spawn Settings")]
    [Range(0, 10)]
    public int enemyCount = 3;
    public List<EnemySpawnConfig> enemySpawnConfigs = new List<EnemySpawnConfig>();

    [Header("Racing Grid Layout Settings")]
    [SerializeField] private Transform enemyStartPos;
    public float backDistance = 6f;
    public float sideOffset = 3f;
    public bool startRight = true;

    [Header("Prefab References")]
    public GameObject carControllerPrefab;

    [Header("Options")]
    public bool spawnOnStart = true;
    [Tooltip("Thời gian chờ giữa các lần sinh xe lần lượt (giây)")]
    public float spawnInterval = 2f;

    private readonly List<CarController> spawnedCars = new List<CarController>();
    private bool isSpawning = false;
    private Task currentSpawnTask = null;

    private async void Start()
    {
        if (spawnOnStart)
        {
            currentSpawnTask = SpawnAllVehiclesAsync();
            await currentSpawnTask;
        }
    }

    public async Task SpawnAllVehiclesAsync(System.Action<float, string> onProgress = null)
    {
        if (isSpawning)
        {
            if (currentSpawnTask != null)
            {
                await currentSpawnTask;
            }
            return;
        }
        isSpawning = true;

        ClearSpawnedVehicles();

        bool originalRegister = false;
        if (RCCP_SceneManager.Instance != null)
        {
            originalRegister = RCCP_SceneManager.Instance.registerLastVehicleAsPlayer;
            RCCP_SceneManager.Instance.registerLastVehicleAsPlayer = false;
        }

        try
        {
            onProgress?.Invoke(0.05f, "Preparing grid...");

            // Lấy loại xe Player từ dữ liệu đã chọn trong Garage / Save
            CarType selectedPlayerCarType = playerCarType;
            if (CarDatabaseSO.Instance != null)
            {
                int selectedIdx = CarSaveManager.GetSelectedCarIndex();
                CarDataSO selectedCarData = CarDatabaseSO.Instance.GetCarByIndex(selectedIdx);
                if (selectedCarData != null)
                {
                    selectedPlayerCarType = selectedCarData.carType;
                }
            }

            int validEnemyCount = 0;
            for (int i = 0; i < enemySpawnConfigs.Count; i++)
            {
                if (enemySpawnConfigs[i].spawnTransform != null)
                    validEnemyCount++;
            }
            int totalCars = (playerSpawnTransform != null ? 1 : 0) + validEnemyCount;
            int carsSpawnedCount = 0;
            int delayMs = Mathf.Max(0, Mathf.RoundToInt(spawnInterval * 1000f));
            float carSlotSize = totalCars > 0 ? 1f / totalCars : 1f;

            // 1. Spawn xe Player trước
            if (playerSpawnTransform != null)
            {
                int currentCarIdx = carsSpawnedCount;
                carsSpawnedCount++;
                float carBaseP = (float)currentCarIdx * carSlotSize;

                onProgress?.Invoke(carBaseP + 0.1f * carSlotSize, $"Preparing Player Car ({selectedPlayerCarType})...");

                CarController playerCar = CreateVehicleGameObject($"Player_Car_{selectedPlayerCarType}", playerSpawnTransform);
                ConfigureCar(playerCar, ControllerType.Player, selectedPlayerCarType, AIDifficulty.Medium, false);
                spawnedCars.Add(playerCar);

                onProgress?.Invoke(carBaseP + 0.35f * carSlotSize, $"Loading Player Car ({selectedPlayerCarType})...");
                await playerCar.LoadCarModelAsync(selectedPlayerCarType);

                onProgress?.Invoke(carBaseP + 0.65f * carSlotSize, $"Configuring Player Car...");

                // Chờ delay giữa các lần spawn xe kèm cập nhật % mượt mà
                if (validEnemyCount > 0 && delayMs > 0)
                {
                    int elapsed = 0;
                    int step = 100;
                    while (elapsed < delayMs)
                    {
                        int cur = Mathf.Min(step, delayMs - elapsed);
                        await Task.Delay(cur);
                        elapsed += cur;
                        float delayFrac = (float)elapsed / delayMs;
                        float p = carBaseP + (0.65f + delayFrac * 0.35f) * carSlotSize;
                        onProgress?.Invoke(p, $"Spawning Player Car ({selectedPlayerCarType})...");
                    }
                }
                else
                {
                    onProgress?.Invoke(carBaseP + 1.0f * carSlotSize, $"Player Car Ready");
                }
            }
            else
            {
                Debug.LogWarning("[StartSpawnPoint] Chưa gán PlayerSpawnTransform!");
            }

            // 2. Spawn lần lượt từng xe Enemy
            int enemyIndex = 0;
            for (int i = 0; i < enemySpawnConfigs.Count; i++)
            {
                var config = enemySpawnConfigs[i];
                if (config.spawnTransform == null) continue;

                enemyIndex++;
                int currentCarIdx = carsSpawnedCount;
                carsSpawnedCount++;
                float carBaseP = (float)currentCarIdx * carSlotSize;

                onProgress?.Invoke(carBaseP + 0.1f * carSlotSize, $"Preparing Opponent {enemyIndex} ({config.carType})...");

                CarController enemyCar = CreateVehicleGameObject($"Enemy_Car_{enemyIndex}_{config.carType}", config.spawnTransform);
                ConfigureCar(enemyCar, ControllerType.AI, config.carType, config.aiDifficulty, true);
                spawnedCars.Add(enemyCar);

                onProgress?.Invoke(carBaseP + 0.35f * carSlotSize, $"Loading Opponent {enemyIndex} ({config.carType})...");
                await enemyCar.LoadCarModelAsync(config.carType);

                onProgress?.Invoke(carBaseP + 0.65f * carSlotSize, $"Configuring Opponent {enemyIndex}...");

                // Chờ delay 2s cho đến chiếc cuối cùng kèm cập nhật % mượt mà
                if (enemyIndex < validEnemyCount && delayMs > 0)
                {
                    int elapsed = 0;
                    int step = 100;
                    while (elapsed < delayMs)
                    {
                        int cur = Mathf.Min(step, delayMs - elapsed);
                        await Task.Delay(cur);
                        elapsed += cur;
                        float delayFrac = (float)elapsed / delayMs;
                        float p = carBaseP + (0.65f + delayFrac * 0.35f) * carSlotSize;
                        onProgress?.Invoke(p, $"Spawning Opponent {enemyIndex} ({config.carType})...");
                    }
                }
                else
                {
                    onProgress?.Invoke(carBaseP + 1.0f * carSlotSize, $"Opponent {enemyIndex} Ready");
                }
            }

            onProgress?.Invoke(0.99f, "Finalizing grid setup...");
        }
        finally
        {
            if (RCCP_SceneManager.Instance != null)
                RCCP_SceneManager.Instance.registerLastVehicleAsPlayer = originalRegister;
            isSpawning = false;
        }

        var player = spawnedCars.Find(c => c.controllerType == ControllerType.Player);
        if (player != null && player.carController != null && RCCP_SceneManager.Instance != null)
        {
            bool isPlaying = GameManager.Instance != null && GameManager.Instance.GameState == GameState.Playing;
            RCCP_SceneManager.Instance.RegisterPlayer(player.carController, isPlaying);
        }

        onProgress?.Invoke(1.0f, "Loading ...");
    }

    private void ConfigureCar(CarController car, ControllerType type, CarType cType, AIDifficulty diff, bool isAI)
    {
        car.autoLoadOnStart = false;
        car.isDamageable = false;
        car.controllerType = type;
        car.carType = cType;
        car.isMenuModel = false;

        if (isAI)
        {
            car.aiDifficulty = diff;
            car.randomColorForAI = true;
        }
        else
        {
            car.randomColorForAI = false;
            car.useCustomColor = false;
        }
    }

    private CarController CreateVehicleGameObject(string name, Transform spawnTransform)
    {
        GameObject carGo;

        if (carControllerPrefab != null)
        {
            carGo = Instantiate(carControllerPrefab, spawnTransform.position, spawnTransform.rotation);
            carGo.name = name;

            // Xóa triệt để các model xe con mặc định đính kèm sẵn trong Prefab trước khi load Addressable mới
            for (int i = carGo.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = carGo.transform.GetChild(i);
                if (child.GetComponent<RCCP_CarController>() != null || child.GetComponentInChildren<RCCP_CarController>() != null)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }
        }
        else
        {
            carGo = new GameObject(name);
            carGo.transform.position = spawnTransform.position;
            carGo.transform.rotation = spawnTransform.rotation;
        }

        CarController carController = carGo.GetComponent<CarController>();
        if (carController == null)
        {
            carController = carGo.AddComponent<CarController>();
        }

        return carController;
    }

    public void ClearSpawnedVehicles()
    {
        foreach (var car in spawnedCars)
        {
            if (car != null)
            {
                car.UnloadCurrentCar();
                Destroy(car.gameObject);
            }
        }
        spawnedCars.Clear();
    }

    #region Editor Tools

    public void GenerateSpawnPointsInEditor()
    {
        if (enemyStartPos == null)
        {
            Debug.LogError("[StartSpawnPoint] Cần gán EnemyStartPos làm vị trí gốc để tạo lưới xuất phát!");
            return;
        }

        enemySpawnConfigs.Clear();

        Transform parentObj = transform.Find("EnemySpawnPoints");
        if (parentObj != null)
        {
            DestroyImmediate(parentObj.gameObject);
        }

        GameObject newParent = new GameObject("EnemySpawnPoints");
        newParent.transform.SetParent(transform, false);

        Vector3 startPos = enemyStartPos.position;
        Quaternion startRot = enemyStartPos.rotation;
        Vector3 forwardDir = enemyStartPos.forward;
        Vector3 rightDir = enemyStartPos.right;

        int sideMultiplier = startRight ? 1 : -1;

        var availableCarTypes = (CarType[])System.Enum.GetValues(typeof(CarType));

        for (int i = 0; i < enemyCount; i++)
        {
            int row = i / 2;
            int col = i % 2;

            int currentSideMultiplier = (col == 0) ? sideMultiplier : -sideMultiplier;

            Vector3 spawnPos = startPos
                - (forwardDir * (row * backDistance))
                + (rightDir * (currentSideMultiplier * (sideOffset / 2f)));

            GameObject spObj = new GameObject($"SpawnPoint_Enemy_{i + 1}");
            spObj.transform.position = spawnPos;
            spObj.transform.rotation = startRot;
            spObj.transform.SetParent(newParent.transform, true);

            EnemySpawnConfig config = new EnemySpawnConfig
            {
                name = $"Enemy {i + 1}",
                carType = availableCarTypes.Length > 0 ? availableCarTypes[i % availableCarTypes.Length] : CarType.Sedan,
                aiDifficulty = AIDifficulty.Medium,
                spawnTransform = spObj.transform
            };

            enemySpawnConfigs.Add(config);
        }

        Debug.Log($"[StartSpawnPoint] ✅ Đã tạo thành công {enemyCount} Spawn Point chuẩn Racing Grid!");
    }

    public void ClearSpawnPointsInEditor()
    {
        enemySpawnConfigs.Clear();

        Transform parentObj = transform.Find("EnemySpawnPoints");
        if (parentObj != null)
        {
            DestroyImmediate(parentObj.gameObject);
        }

        Debug.Log("[StartSpawnPoint] 🗑️ Đã xóa toàn bộ điểm Spawn trong Editor!");
    }

    private void OnDrawGizmos()
    {
        if (playerSpawnTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(playerSpawnTransform.position + Vector3.up * 1f, new Vector3(2f, 1.5f, 4.5f));
            Gizmos.DrawRay(playerSpawnTransform.position + Vector3.up * 1f, playerSpawnTransform.forward * 3f);
        }

        if (enemySpawnConfigs != null)
        {
            for (int i = 0; i < enemySpawnConfigs.Count; i++)
            {
                var cfg = enemySpawnConfigs[i];
                if (cfg != null && cfg.spawnTransform != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(cfg.spawnTransform.position + Vector3.up * 1f, new Vector3(2f, 1.5f, 4.5f));
                    Gizmos.DrawRay(cfg.spawnTransform.position + Vector3.up * 1f, cfg.spawnTransform.forward * 2.5f);
                }
            }
        }
    }

    #endregion
}
