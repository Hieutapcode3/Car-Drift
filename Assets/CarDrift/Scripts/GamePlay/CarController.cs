using System.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

public class CarController : MonoBehaviour
{
    [Header("Car Selection Settings")]
    public CarType carType = CarType.Coupe;
    public bool autoLoadOnStart = true;

    [Header("Controller Settings")]
    public ControllerType controllerType = ControllerType.Player;
    public bool isMenuModel = false;

    [Header("AI Settings")]
    public AIDifficulty aiDifficulty = AIDifficulty.Medium;

    [Header("Damage Settings")]
    public bool isDamageable = true;

    [Header("Paint Settings")]
    public ColorParamSO colorParamSO;
    public CarColorType carColorType = CarColorType.Red;
    public bool useCustomColor = false;
    public bool randomColorForAI = true;
    [Header("Menu Rotation Settings")]
    public float rotationSpeed = 30f;
    public Vector3 rotationAxis = Vector3.up;
    public bool freezePhysicsInMenu = true;
    [Header("References")]
    [SerializeField] private TextMeshPro indexRacePref;
    [SerializeField] private MiniMapIcon miniMapIconPref;
    public RCCP_CarController carController;
    public RCCP_AI aiController;
    public RCCP_Damage damageController;

    [Header("Race Tracking Debug")]
    [ReadOnly] public int indexTargetPoint = 1;
    [ReadOnly] public int totalWaypointsPassed = 0;
    [ReadOnly] public int currentLapCount = 1;

    private TextMeshPro spawnedIndexRaceText;
    private MiniMapIcon spawnedMiniMapIcon;
    private GameObject currentCarInstance;
    private AsyncOperationHandle<GameObject> loadHandle;
    public TextMeshPro SpawnedIndexRaceText => spawnedIndexRaceText;
    public bool IsMenuModel
    {
        get
        {
            if (controllerType == ControllerType.Menu) return true;
            if (!isMenuModel) return false;
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return activeScene.Contains("Menu") || activeScene.Contains("Garage");
        }
    }

    #region Race Status Properties
    public int CurrentWaypointIndex
    {
        get
        {
            if (controllerType == ControllerType.AI && aiController != null)
                return aiController.currentWaypointIndex;

            if (RaceProgressTracker.Instance != null)
                return RaceProgressTracker.Instance.GetTargetWaypointIndex(this);

            return 0;
        }
    }

    public int CurrentLap
    {
        get
        {
            if (controllerType == ControllerType.AI && aiController != null)
                return aiController.lap + 1;

            if (RaceProgressTracker.Instance != null)
                return RaceProgressTracker.Instance.GetPlayerLap(this) + 1;

            return 1;
        }
    }

    public RCCP_Waypoint NextWaypoint
    {
        get
        {
            if (RaceProgressTracker.Instance != null && RaceProgressTracker.Instance.waypointsContainer != null)
            {
                var waypoints = RaceProgressTracker.Instance.waypointsContainer.waypoints;
                if (waypoints != null && waypoints.Count > 0)
                {
                    int idx = CurrentWaypointIndex % waypoints.Count;
                    return waypoints[idx];
                }
            }
            return null;
        }
    }

    public int CurrentRank
    {
        get
        {
            if (RaceProgressTracker.Instance != null)
                return RaceProgressTracker.Instance.GetRank(this);

            return 0;
        }
    }
    #endregion
    public Color? GetCurrentColor()
    {

        if (carColorType == CarColorType.Default)
            return null;

        if (colorParamSO != null)
        {
            return colorParamSO.GetColor(carColorType);
        }
        else if (ColorParamSO.Instance != null)
        {
            return ColorParamSO.Instance.GetColor(carColorType);
        }
        return Color.white;
    }
    [Button]
    public void ApplyCarColor()
    {
        if (carController == null)
            FetchReferences();

        if (carController != null && carController.Customizer != null)
        {
            carController.Customizer.autoSave = false;
        }

        if (carColorType == CarColorType.Default)
        {
            carController?.Customizer?.PaintManager?.Restore();
            return;
        }

        Color? targetColor = GetCurrentColor();
        if (targetColor == null)
            return;

        if (carController != null && carController.Customizer != null && carController.Customizer.PaintManager != null)
        {
            var paintManager = carController.Customizer.PaintManager;
            if (paintManager.paints == null || paintManager.paints.Length == 0)
            {
                paintManager.GetAllPainters();
            }
            paintManager.PaintWithoutSave(targetColor.Value);
        }
        else
        {
            Debug.LogWarning($"[CarController] Không thể đổi màu xe '{gameObject.name}'. Thiếu RCCP_Customizer hoặc PaintManager!");
        }
    }
    [Button]
    public void SetRandomColor()
    {
        if (controllerType != ControllerType.AI)
            return;

        System.Array values = System.Enum.GetValues(typeof(CarColorType));
        if (values.Length > 0)
        {
            CarColorType randomColor = (CarColorType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
            SetCarColor(randomColor);
        }
    }
    public void SetCarColor(CarColorType newColorType)
    {
        carColorType = newColorType;
        useCustomColor = newColorType != CarColorType.Default;
        ApplyCarColor();
    }
    private void OnEnable()
    {
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void Start()
    {
        if (autoLoadOnStart && currentCarInstance == null)
        {
            _ = LoadCarModelAsync(carType);
        }
        else
        {
            ApplyControlState();
        }
    }

    private void OnGameStateChanged(GameState state)
    {
        ApplyControlState();
    }

    private void OnDestroy()
    {
        UnloadCurrentCar();
    }

    [Button]
    public void TestLoadCarInEditor()
    {
        _ = LoadCarModelAsync(carType);
    }

    private int currentLoadRequestId = 0;

    public async Task LoadCarModelAsync(CarType newCarType)
    {
        carType = newCarType;
        string addressKey = carType.ToString();
        int thisRequestId = ++currentLoadRequestId;

        // Debug.Log($"[CarController] Đang tiến hành load model xe với Address Key: '{addressKey}'...");
        UnloadCurrentCar();
        try
        {
            var opHandle = Addressables.InstantiateAsync(addressKey, transform);
            GameObject instance = await opHandle.Task;

            // Nếu trong lúc chờ tải đã có một yêu cầu tải xe khác mới hơn
            if (thisRequestId != currentLoadRequestId)
            {
                if (instance != null)
                {
                    if (opHandle.IsValid())
                        Addressables.ReleaseInstance(instance);
                    else
                        Destroy(instance);
                }
                return;
            }

            loadHandle = opHandle;
            currentCarInstance = instance;

            if (loadHandle.Status == AsyncOperationStatus.Failed || currentCarInstance == null)
            {
                Debug.LogError($"[CarController] ❌ Load THẤT BẠI cho Address Key: '{addressKey}'. Vui lòng kiểm tra lại tên Key trong Addressables Groups!");
                return;
            }
            currentCarInstance.transform.localPosition = Vector3.zero;
            currentCarInstance.transform.localRotation = Quaternion.identity;
            currentCarInstance.transform.localScale = Vector3.one;
            // Debug.Log($"[CarController] ✅ Load THÀNH CÔNG model xe: '{addressKey}'!");
            FetchReferences();
            ApplyDamageSettings();
            ApplyControlState();
            ApplySavedCustomization();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CarController] ❌ Ngoại lệ khi load Addressable '{addressKey}': {ex.Message}");
        }
    }

    public void ApplySavedCustomization()
    {
        if (!Application.isPlaying) return;

        if (carController == null)
            FetchReferences();

        if (carController == null) return;

        RCCP_Customizer customizer = carController.Customizer;
        if (customizer == null)
        {
            customizer = carController.GetComponentInChildren<RCCP_Customizer>(true);
            if (customizer == null)
            {
                customizer = carController.gameObject.AddComponent<RCCP_Customizer>();
            }
        }

        if (customizer != null)
        {
            customizer.autoSave = false;
            customizer.Initialize();
            customizer.autoSave = false;

            CarDataSO carData = null;
            if (CarDatabaseSO.Instance != null)
            {
                carData = CarDatabaseSO.Instance.GetCarByType(carType);
                if (carData == null)
                {
                    carData = CarDatabaseSO.Instance.GetCarByIndex(CarSaveManager.GetSelectedCarIndex());
                }
            }

            if (carData != null)
            {
                string id = carData.carID;

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
                CustomConfigSO cfg = (CarDatabaseSO.Instance != null && CarDatabaseSO.Instance.customConfig != null)
                    ? CarDatabaseSO.Instance.customConfig
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
        }
    }
    public void SetCarType(CarType newCarType)
    {
        _ = LoadCarModelAsync(newCarType);
    }
    public void UnloadCurrentCar()
    {
        carController = null;
        aiController = null;
        damageController = null;

        if (spawnedIndexRaceText != null)
        {
            Destroy(spawnedIndexRaceText.gameObject);
            spawnedIndexRaceText = null;
        }

        if (spawnedMiniMapIcon != null)
        {
            Destroy(spawnedMiniMapIcon.gameObject);
            spawnedMiniMapIcon = null;
        }

        if (currentCarInstance != null)
        {
            currentCarInstance.SetActive(false);
            if (loadHandle.IsValid())
            {
                Addressables.ReleaseInstance(currentCarInstance);
            }
            else
            {
                Destroy(currentCarInstance);
            }
            currentCarInstance = null;
        }
        foreach (Transform child in transform)
        {
            if (child.GetComponent<RCCP_CarController>() != null || child.GetComponentInChildren<RCCP_CarController>() != null)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        cachedRigidbodies = null;
        savedVelocities.Clear();
        isPhysicsFrozen = false;
    }

    public void FetchReferences()
    {
        if (currentCarInstance != null)
        {
            carController = currentCarInstance.GetComponent<RCCP_CarController>();
            if (carController == null)
            {
                carController = currentCarInstance.GetComponentInChildren<RCCP_CarController>();
            }
        }

        if (carController == null)
        {
            foreach (Transform child in transform)
            {
                carController = child.GetComponent<RCCP_CarController>();
                if (carController != null)
                {
                    currentCarInstance = child.gameObject;
                    break;
                }
            }

            if (carController == null)
            {
                carController = GetComponentInChildren<RCCP_CarController>(true);
                if (carController != null && carController.gameObject != gameObject)
                {
                    currentCarInstance = carController.gameObject;
                }
            }

            if (carController == null)
            {
                carController = GetComponent<RCCP_CarController>();
            }
        }

        if (aiController == null)
        {
            aiController = GetComponentInChildren<RCCP_AI>(true);
        }

        if (aiController == null && controllerType == ControllerType.AI)
        {
            GetOrCreateAIController();
        }

        if (damageController == null)
        {
            damageController = GetComponentInChildren<RCCP_Damage>(true);
        }

        isPhysicsFrozen = false;
        CacheRigidbodies();

        if (indexRacePref != null && !IsMenuModel && controllerType == ControllerType.AI)
        {
            SpawnIndexRaceText();
        }

        if (miniMapIconPref != null && !IsMenuModel)
        {
            SpawnMiniMapIcon();
        }
    }
    public void SpawnIndexRaceText()
    {
        if (indexRacePref == null || IsMenuModel || controllerType != ControllerType.AI)
            return;
        Transform targetParent = null;
        if (carController != null)
            targetParent = carController.transform;
        else if (currentCarInstance != null)
            targetParent = currentCarInstance.transform;
        else
            targetParent = transform;

        if (spawnedIndexRaceText == null)
        {
            spawnedIndexRaceText = Instantiate(indexRacePref, targetParent);
            spawnedIndexRaceText.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            spawnedIndexRaceText.transform.localRotation = Quaternion.identity;

            if (!spawnedIndexRaceText.GetComponent<UIRotate>())
            {
                spawnedIndexRaceText.gameObject.AddComponent<UIRotate>();
            }
        }
        else
        {
            if (spawnedIndexRaceText.transform.parent != targetParent)
            {
                spawnedIndexRaceText.transform.SetParent(targetParent, false);
                spawnedIndexRaceText.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            }
        }
    }

    public void SpawnMiniMapIcon()
    {
        if (miniMapIconPref == null || IsMenuModel)
            return;

        Transform targetParent = null;
        if (carController != null)
            targetParent = carController.transform;
        else if (currentCarInstance != null)
            targetParent = currentCarInstance.transform;
        else
            targetParent = transform;

        if (spawnedMiniMapIcon == null)
        {
            spawnedMiniMapIcon = Instantiate(miniMapIconPref, targetParent);
            spawnedMiniMapIcon.transform.localPosition = new Vector3(0f, -200f, 0f);
            spawnedMiniMapIcon.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
        else
        {
            if (spawnedMiniMapIcon.transform.parent != targetParent)
            {
                spawnedMiniMapIcon.transform.SetParent(targetParent, false);
                spawnedMiniMapIcon.transform.localPosition = new Vector3(0f, -200f, 0f);
            }
        }

        bool isPlayer = controllerType == ControllerType.Player;
        spawnedMiniMapIcon.Init(isPlayer);
    }
    public void UpdateRaceRank(int rank)
    {
        if (IsMenuModel || controllerType != ControllerType.AI)
            return;

        if (spawnedIndexRaceText == null)
        {
            SpawnIndexRaceText();
        }

        if (spawnedIndexRaceText != null)
        {
            spawnedIndexRaceText.text = rank.ToString();
        }
    }

    public RCCP_AI GetOrCreateAIController()
    {
        if (aiController != null)
            return aiController;

        aiController = GetComponentInChildren<RCCP_AI>(true);
        if (aiController != null)
            return aiController;

        if (carController == null)
            FetchReferences();

        Transform parentTarget = transform;
        if (carController != null)
            parentTarget = carController.transform;
        RCCP_OtherAddons otherAddons = parentTarget.GetComponentInChildren<RCCP_OtherAddons>(true);
        Transform otherAddonsTransform = null;

        if (otherAddons != null)
        {
            otherAddonsTransform = otherAddons.transform;
        }
        else
        {
            Transform existingOtherAddonsObj = parentTarget.Find("OtherAddons");
            if (existingOtherAddonsObj != null)
            {
                otherAddons = existingOtherAddonsObj.GetComponent<RCCP_OtherAddons>();
                if (otherAddons == null)
                    otherAddons = existingOtherAddonsObj.gameObject.AddComponent<RCCP_OtherAddons>();
                otherAddonsTransform = existingOtherAddonsObj;
            }
            else
            {
                GameObject newOtherAddonsObj = new GameObject("OtherAddons");
                newOtherAddonsObj.transform.SetParent(parentTarget, false);
                otherAddons = newOtherAddonsObj.AddComponent<RCCP_OtherAddons>();
                otherAddonsTransform = newOtherAddonsObj.transform;
            }
        }

        Transform aiChildTransform = otherAddonsTransform.Find("AI");
        GameObject aiGo;

        if (aiChildTransform != null)
        {
            aiGo = aiChildTransform.gameObject;
        }
        else
        {
            aiGo = new GameObject("AI");
            aiGo.transform.SetParent(otherAddonsTransform, false);
        }

        aiController = aiGo.GetComponent<RCCP_AI>();
        if (aiController == null)
        {
            aiController = aiGo.AddComponent<RCCP_AI>();
        }

        return aiController;
    }

    public void ApplyDamageSettings()
    {
        if (damageController == null && carController != null)
        {
            damageController = carController.GetComponentInChildren<RCCP_Damage>(true);
        }

        if (!isDamageable || IsMenuModel)
        {
            if (damageController != null)
            {
                damageController.enabled = false;
            }
            if (carController != null)
            {
                RCCP_DetachablePart[] detachableParts = carController.GetComponentsInChildren<RCCP_DetachablePart>(true);
                foreach (RCCP_DetachablePart part in detachableParts)
                {
                    part.enabled = false;

                    if (part.TryGetComponent(out UnityEngine.Animations.ParentConstraint pc))
                    {
                        pc.constraintActive = false;
                        Destroy(pc);
                    }

                    if (part.TryGetComponent(out ConfigurableJoint cj))
                    {
                        cj.connectedBody = null;
                        Destroy(cj);
                    }

                    if (part.TryGetComponent(out Rigidbody partRb))
                    {
                        Destroy(partRb);
                    }
                }
            }
        }
        else
        {
            if (damageController != null)
                damageController.enabled = true;

            if (carController != null)
            {
                RCCP_DetachablePart[] detachableParts = carController.GetComponentsInChildren<RCCP_DetachablePart>(true);
                foreach (RCCP_DetachablePart part in detachableParts)
                {
                    part.enabled = true;
                }
            }
        }
    }

    public void ApplyControlState()
    {
        ApplyDamageSettings();

        if (IsMenuModel)
        {
            if (carController != null)
            {
                carController.SetCanControl(false);
            }

            SetKinematicAllParts(true);

            if (aiController != null)
            {
                aiController.enabled = false;
            }
            return;
        }
        bool isPlaying = GameManager.Instance == null || GameManager.Instance.GameState == GameState.Playing;
        bool isFinished = GameManager.Instance != null && (GameManager.Instance.GameState == GameState.Win || GameManager.Instance.GameState == GameState.Lose);
        bool isAIActive = isPlaying || isFinished;

        if (controllerType == ControllerType.AI)
        {
            GetOrCreateAIController();
            if (aiController != null)
            {
                if (aiController.waypointsContainer == null && RaceProgressTracker.Instance != null && RaceProgressTracker.Instance.waypointsContainer != null)
                {
                    aiController.waypointsContainer = RaceProgressTracker.Instance.waypointsContainer;
                }
                aiController.enabled = isPlaying;
                if (isPlaying)
                {
                    aiController.Reload();
                    ApplyAIDifficulty(aiDifficulty);
                }
            }
            if (carController != null)
            {
                carController.externalControl = true;
                carController.SetCanControl(isPlaying);
            }

            if (randomColorForAI)
            {
                SetRandomColor();
            }
        }
        else
        {
            if (aiController != null)
            {
                aiController.enabled = isFinished;
                if (isFinished)
                {
                    aiController.navigationMode = RCCP_AI.NavigationMode.FollowWaypoints;
                    if (RaceProgressTracker.Instance != null && RaceProgressTracker.Instance.waypointsContainer != null)
                    {
                        aiController.waypointsContainer = RaceProgressTracker.Instance.waypointsContainer;
                        aiController.currentWaypointIndex = indexTargetPoint; // Quan trọng: Gán điểm đến tiếp theo để AI không quay đầu
                    }
                }
            }

            if (carController != null)
            {
                carController.externalControl = !isPlaying;
                carController.SetCanControl(isPlaying || isFinished);

                if (RCCP_SceneManager.Instance != null)
                {
                    RCCP_SceneManager.Instance.RegisterPlayer(carController, isPlaying);
                    Debug.Log("Can controller: " + isPlaying);
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.SetMiniMapFollowPlayer();
                    }
                }
            }
        }

        FreezePhysics(!(isPlaying || isFinished));
    }

    public void ApplyAIDifficulty(AIDifficulty difficulty)
    {
        if (aiController == null)
            return;

        aiDifficulty = difficulty;

        switch (difficulty)
        {
            case AIDifficulty.Easy:
                aiController.limitSpeed = true;
                aiController.maximumSpeed = 70f;
                aiController.smoothedSteer = true;
                aiController.raycastLength = 10f;
                aiController.nextWaypointPassDistance = 20;
                break;

            case AIDifficulty.Medium:
                aiController.limitSpeed = true;
                aiController.maximumSpeed = 100f;
                aiController.smoothedSteer = true;
                aiController.raycastLength = 15f;
                aiController.nextWaypointPassDistance = 15;
                break;

            case AIDifficulty.Hard:
                aiController.limitSpeed = false;
                aiController.smoothedSteer = false;
                aiController.raycastLength = 25f;
                aiController.nextWaypointPassDistance = 10;
                break;
        }
    }

    private struct PhysicsState
    {
        public Vector3 velocity;
        public Vector3 angularVelocity;
    }

    private Rigidbody[] cachedRigidbodies;
    private readonly Dictionary<Rigidbody, PhysicsState> savedVelocities = new Dictionary<Rigidbody, PhysicsState>();
    private bool isPhysicsFrozen;

    public void CacheRigidbodies()
    {
        cachedRigidbodies = GetComponentsInChildren<Rigidbody>(true);
    }

    public void FreezePhysics(bool freeze)
    {
        if (isPhysicsFrozen == freeze) return;
        isPhysicsFrozen = freeze;

        if (carController == null)
            FetchReferences();

        Rigidbody mainRb = carController != null ? carController.Rigid : GetComponent<Rigidbody>();
        if (mainRb == null)
            mainRb = GetComponentInChildren<Rigidbody>();

        if (mainRb != null)
        {
            if (freeze)
            {
                mainRb.linearVelocity = Vector3.zero;
                mainRb.angularVelocity = Vector3.zero;
            }
        }
    }

    public void SetKinematicAllParts(bool kinematic)
    {
        if (cachedRigidbodies == null || cachedRigidbodies.Length == 0)
            CacheRigidbodies();

        for (int i = 0; i < cachedRigidbodies.Length; i++)
        {
            var rb = cachedRigidbodies[i];
            if (rb == null) continue;
            rb.isKinematic = kinematic;
            if (kinematic)
            {
                rb.useGravity = false;
            }
        }
    }

    private void Update()
    {
        if (IsMenuModel)
        {
            transform.Rotate(rotationAxis * (rotationSpeed * Time.deltaTime), Space.Self);
        }
    }

}


