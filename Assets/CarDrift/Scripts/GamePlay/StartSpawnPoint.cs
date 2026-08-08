using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class EnemySpawnConfig
{
    public string name = "Enemy Spawn";
    public CarType carType = CarType.Truck;
    public AIDifficulty aiDifficulty = AIDifficulty.Medium;
    public Transform spawnTransform;
}

public class StartSpawnPoint : MonoBehaviour
{
    [Header("Player Settings")]
    public CarType playerCarType = CarType.Truck;
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

    private readonly List<CarController> spawnedCars = new List<CarController>();

    private async void Start()
    {
        if (spawnOnStart)
        {
            await SpawnAllVehiclesAsync();
        }
    }

    public async Task SpawnAllVehiclesAsync()
    {
        ClearSpawnedVehicles();

        bool originalRegister = false;
        if (RCCP_SceneManager.Instance != null)
        {
            originalRegister = RCCP_SceneManager.Instance.registerLastVehicleAsPlayer;
            RCCP_SceneManager.Instance.registerLastVehicleAsPlayer = false;
        }

        try
        {
            if (playerSpawnTransform != null)
            {
                CarController playerCar = CreateVehicleGameObject("Player_Car", playerSpawnTransform);
                ConfigureCar(playerCar, ControllerType.Player, playerCarType, AIDifficulty.Medium, false);
                spawnedCars.Add(playerCar);
            }
            else
            {
                Debug.LogWarning("[StartSpawnPoint] Chưa gán PlayerSpawnTransform!");
            }

            for (int i = 0; i < enemySpawnConfigs.Count; i++)
            {
                var config = enemySpawnConfigs[i];
                if (config.spawnTransform == null) continue;

                CarController enemyCar = CreateVehicleGameObject($"Enemy_Car_{i + 1}_{config.carType}", config.spawnTransform);
                ConfigureCar(enemyCar, ControllerType.AI, config.carType, config.aiDifficulty, true);
                spawnedCars.Add(enemyCar);
            }
            for (int i = 0; i < spawnedCars.Count; i++)
            {
                CarController car = spawnedCars[i];
                await car.LoadCarModelAsync(car.carType);
                await Task.Delay(2000);
            }
        }
        finally
        {
            if (RCCP_SceneManager.Instance != null)
                RCCP_SceneManager.Instance.registerLastVehicleAsPlayer = originalRegister;
        }
        var player = spawnedCars.Find(c => c.controllerType == ControllerType.Player);
        if (player != null && player.carController != null && RCCP_SceneManager.Instance != null)
        {
            bool isPlaying = GameManager.Instance != null && GameManager.Instance.GameState == GameState.Playing;
            RCCP_SceneManager.Instance.RegisterPlayer(player.carController, isPlaying);
        }
    }

    private void ConfigureCar(CarController car, ControllerType type, CarType cType, AIDifficulty diff, bool isAI)
    {
        car.autoLoadOnStart = false;
        car.isDamageable = false;
        car.controllerType = type;
        car.carType = cType;

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
                carType = (CarType)(i % System.Enum.GetValues(typeof(CarType)).Length),
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
