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

        CarController playerCar = null;
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
                playerCar = CreateVehicleGameObject("Player_Car", playerSpawnTransform);
                playerCar.autoLoadOnStart = false;
                playerCar.isDamageable = false;
                playerCar.controllerType = ControllerType.Player;
                playerCar.carType = playerCarType;
                playerCar.randomColorForAI = false;
                playerCar.useCustomColor = false;

                await playerCar.LoadCarModelAsync(playerCarType);
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
                enemyCar.autoLoadOnStart = false;
                enemyCar.isDamageable = false;
                enemyCar.controllerType = ControllerType.AI;
                enemyCar.carType = config.carType;
                enemyCar.aiDifficulty = config.aiDifficulty;

                await enemyCar.LoadCarModelAsync(config.carType);
                spawnedCars.Add(enemyCar);
            }
        }
        finally
        {
            if (RCCP_SceneManager.Instance != null)
                RCCP_SceneManager.Instance.registerLastVehicleAsPlayer = originalRegister;
        }
        if (playerCar != null && playerCar.carController != null)
        {
            if (RCCP_SceneManager.Instance != null)
            {
                RCCP_SceneManager.Instance.RegisterPlayer(playerCar.carController, true);
            }
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
        if (playerSpawnTransform == null)
        {
            Transform existingPlayer = transform.Find("PlayerSpawn");
            if (existingPlayer != null)
            {
                playerSpawnTransform = existingPlayer;
            }
            else
            {
                GameObject playerObj = new GameObject("PlayerSpawn");
                playerObj.transform.SetParent(transform);
                playerObj.transform.localPosition = Vector3.zero;
                playerObj.transform.localRotation = Quaternion.identity;
                playerSpawnTransform = playerObj.transform;
            }
        }
        // 2. Enemy Start Position Anchor
        if (enemyStartPos == null)
        {
            Transform existingStart = transform.Find("EnemyStartPos");
            if (existingStart != null)
            {
                enemyStartPos = existingStart;
            }
            else
            {
                GameObject startObj = new GameObject("EnemyStartPos");
                startObj.transform.SetParent(transform);
                startObj.transform.localPosition = new Vector3(3f, 0f, 0f);
                startObj.transform.localRotation = Quaternion.identity;
                enemyStartPos = startObj.transform;
            }
        }

        Transform enemyGroup = transform.Find("EnemySpawns");
        if (enemyGroup == null)
        {
            GameObject groupObj = new GameObject("EnemySpawns");
            groupObj.transform.SetParent(transform);
            groupObj.transform.localPosition = Vector3.zero;
            groupObj.transform.localRotation = Quaternion.identity;
            enemyGroup = groupObj.transform;
        }

        while (enemySpawnConfigs.Count < enemyCount)
        {
            int index = enemySpawnConfigs.Count + 1;
            enemySpawnConfigs.Add(new EnemySpawnConfig
            {
                name = $"Enemy Spawn {index}",
                carType = (CarType)(index % System.Enum.GetValues(typeof(CarType)).Length),
                aiDifficulty = AIDifficulty.Medium
            });
        }

        if (enemySpawnConfigs.Count > enemyCount)
        {
            enemySpawnConfigs.RemoveRange(enemyCount, enemySpawnConfigs.Count - enemyCount);
        }

        if (enemyCount == 0) return;

        // Lấy vị trí và hướng xoay mốc từ enemyStartPos
        Vector3 anchorPos = enemyStartPos.position;
        Quaternion anchorRot = enemyStartPos.rotation;

        // Sinh và tự động xếp vị trí cho tất cả các điểm spawn (EnemySpawn_1 -> EnemySpawn_N)
        for (int i = 0; i < enemySpawnConfigs.Count; i++)
        {
            string childName = $"EnemySpawn_{i + 1}";
            Transform child = enemyGroup.Find(childName);
            if (child == null)
            {
                GameObject childObj = new GameObject(childName);
                childObj.transform.SetParent(enemyGroup);
                child = childObj.transform;
            }

            // Xe 1 (i = 0): Đặt đúng vị trí enemyStartPos
            // Xe 2 (i = 1): Lùi 1 * backDistance, lệch Right (+) nếu startRight = true
            // Xe 3 (i = 2): Lùi 2 * backDistance, lệch Left (-)
            float sideSign = (i == 0) ? 0f : ((i % 2 != 0) ? (startRight ? 1f : -1f) : (startRight ? -1f : 1f));
            Vector3 targetPos = anchorPos - (anchorRot * Vector3.forward * (i * backDistance)) + (anchorRot * Vector3.right * (sideSign * sideOffset));

            child.position = targetPos;
            child.rotation = anchorRot;

            enemySpawnConfigs[i].name = $"Enemy Spawn {i + 1} ({enemySpawnConfigs[i].carType})";
            enemySpawnConfigs[i].spawnTransform = child;
        }
    }

    public void ClearSpawnPointsInEditor()
    {
        playerSpawnTransform = null;
        enemyStartPos = null;
        enemySpawnConfigs.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && enemySpawnConfigs != null && enemySpawnConfigs.Count > 0)
        {
            GenerateSpawnPointsInEditor();
        }
    }
#endif

    #endregion

    #region Gizmos Draw

    private void OnDrawGizmos()
    {
        if (playerSpawnTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(playerSpawnTransform.position + Vector3.up * 0.75f, new Vector3(2f, 1.5f, 4f));
            Gizmos.DrawRay(playerSpawnTransform.position + Vector3.up * 0.75f, playerSpawnTransform.forward * 3f);
        }

        if (enemyStartPos != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(enemyStartPos.position + Vector3.up * 0.75f, 0.5f);
            Gizmos.DrawRay(enemyStartPos.position + Vector3.up * 0.75f, enemyStartPos.forward * 4f);
        }

        for (int i = 0; i < enemySpawnConfigs.Count; i++)
        {
            var config = enemySpawnConfigs[i];
            if (config.spawnTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(config.spawnTransform.position + Vector3.up * 0.75f, new Vector3(2f, 1.5f, 4f));
                Gizmos.DrawRay(config.spawnTransform.position + Vector3.up * 0.75f, config.spawnTransform.forward * 3f);
            }
        }
    }

    #endregion
}
