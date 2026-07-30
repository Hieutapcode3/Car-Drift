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

    [Header("Prefab References")]
    [Tooltip("Prefab CarController để Instantiate xe Player và Enemy")]
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

        for (int i = 0; i < enemySpawnConfigs.Count; i++)
        {
            string childName = $"EnemySpawn_{i + 1}";
            Transform child = enemyGroup.Find(childName);
            if (child == null)
            {
                GameObject childObj = new GameObject(childName);
                childObj.transform.SetParent(enemyGroup);
                childObj.transform.localPosition = new Vector3((i + 1) * 3f, 0f, 0f);
                childObj.transform.localRotation = Quaternion.identity;
                child = childObj.transform;
            }
            enemySpawnConfigs[i].name = $"Enemy Spawn {i + 1} ({enemySpawnConfigs[i].carType})";
            enemySpawnConfigs[i].spawnTransform = child;
        }
    }

    public void ClearSpawnPointsInEditor()
    {
        playerSpawnTransform = null;
        enemySpawnConfigs.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

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
