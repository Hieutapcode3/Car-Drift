//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Scene manager that contains current player vehicle, current player camera, current player UI, current player character, recording/playing mechanim, and other vehicles as well.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/RCCP Scene Manager")]
public class RCCP_SceneManager : RCCP_Singleton<RCCP_SceneManager>
{

    /// <summary>
    /// Current active player vehicle.
    /// </summary>
    public RCCP_CarController activePlayerVehicle;

    /// <summary>
    /// Current active player camera as RCCP Camera.
    /// </summary>
    public RCCP_Camera activePlayerCamera;

    /// <summary>
    /// Current active UI canvas.
    /// </summary>
    public RCCP_UIManager activePlayerCanvas;

    /// <summary>
    /// Current active main camera.
    /// </summary>
    public Camera activeMainCamera;

    /// <summary>
    /// Last selected player vehicle.
    /// </summary>
    private RCCP_CarController lastActivePlayerVehicle;

    /// <summary>
    /// Registers the lastly spawned vehicle as player vehicle.
    /// </summary>
    public bool registerLastVehicleAsPlayer = true;

    /// <summary>
    /// Disables the UI when there is no any player vehicle.
    /// </summary>
    public bool disableUIWhenNoPlayerVehicle = false;

    /// <summary>
    /// Multithreading is supported on this platform?
    /// </summary>
    public static bool mutlithreadingSupported = false;

    /// <summary>
    /// All vehicles on the scene.
    /// </summary>
    public List<RCCP_CarController> allVehicles = new List<RCCP_CarController>();

    /// <summary>
    /// All terrains on the scene.
    /// </summary>
    public Terrain[] allTerrains;

    public class Terrains
    {

        //	Terrain data.
        public Terrain terrain;
        public TerrainData mTerrainData;
        public PhysicsMaterial terrainCollider;
        public int alphamapWidth;
        public int alphamapHeight;

        public float[,,] mSplatmapData;
        public float mNumTextures;

    }

    public Terrains[] terrains;
    [HideInInspector] public bool terrainsInitialized = false;

    private bool asyncAttempted = false;
    private bool asyncReceived = false;

    [Header("Race Rank Settings")]
    public bool autoUpdateRaceRanks = true;
    public float raceRankUpdateInterval = 0.05f;
    private float raceRankTimer = 0f;
    public void UpdateVehicleRanks()
    {
        CarController[] carControllers = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        if (carControllers == null || carControllers.Length == 0)
            return;

        RCCP_AIWaypointsContainer waypointsContainer = FindFirstObjectByType<RCCP_AIWaypointsContainer>();

        List<(CarController car, float progressScore)> vehicleScores = new List<(CarController, float)>();

        foreach (CarController car in carControllers)
        {
            if (car == null || car.IsMenuModel || !car.gameObject.activeInHierarchy)
                continue;

            float score = 0f;
            Vector3 carPos = car.transform.position;

            if (waypointsContainer != null && waypointsContainer.waypoints != null && waypointsContainer.waypoints.Count > 0)
            {
                int waypointCount = waypointsContainer.waypoints.Count;
                int currentWpIdx = 0;
                int lap = 0;

                RCCP_AI ai = car.aiController != null ? car.aiController : car.GetComponentInChildren<RCCP_AI>();

                if (ai != null)
                {
                    lap = ai.lap;
                    currentWpIdx = Mathf.Clamp(ai.currentWaypointIndex, 0, waypointCount - 1);
                }
                else
                {
                    float minSqDistance = float.MaxValue;
                    for (int i = 0; i < waypointCount; i++)
                    {
                        if (waypointsContainer.waypoints[i] == null) continue;
                        float sqDist = (carPos - waypointsContainer.waypoints[i].transform.position).sqrMagnitude;
                        if (sqDist < minSqDistance)
                        {
                            minSqDistance = sqDist;
                            currentWpIdx = i;
                        }
                    }
                }
                int nextWpIdx = (currentWpIdx + 1) % waypointCount;

                Vector3 currWpPos = waypointsContainer.waypoints[currentWpIdx] != null ? waypointsContainer.waypoints[currentWpIdx].transform.position : carPos;
                Vector3 nextWpPos = waypointsContainer.waypoints[nextWpIdx] != null ? waypointsContainer.waypoints[nextWpIdx].transform.position : currWpPos;

                Vector3 segment = nextWpPos - currWpPos;
                float segmentLengthSq = segment.sqrMagnitude;

                float segmentProgress = 0f;
                if (segmentLengthSq > 0.001f)
                {
                    Vector3 carToCurr = carPos - currWpPos;
                    segmentProgress = Mathf.Clamp01(Vector3.Dot(carToCurr, segment) / segmentLengthSq);
                }
                score = (lap * waypointCount * 1000f) + (currentWpIdx * 1000f) + (segmentProgress * 1000f);
            }
            else
            {
                float speed = car.carController != null ? car.carController.speed : 0f;
                score = car.transform.position.z + (speed * 0.1f);
            }

            vehicleScores.Add((car, score));
        }
        vehicleScores.Sort((a, b) => b.progressScore.CompareTo(a.progressScore));
        for (int i = 0; i < vehicleScores.Count; i++)
        {
            int rank = i + 1;
            vehicleScores[i].car.UpdateRaceRank(rank);
        }
    }

    private void Awake()
    {
        RCCP_Events.OnRCCPCameraSpawned += RCCP_Events_OnRCCPCameraSpawned;
        RCCP_Events.OnRCCPSpawned += RCCP_Events_OnRCCPSpawned;
        RCCP_Events.OnRCCPAISpawned += RCCP_Events_OnRCCPAISpawned;
        RCCP_Events.OnRCCPUISpawned += RCCP_Events_OnRCCPUISpawned;
        RCCP_Events.OnRCCPDestroyed += RCCP_Events_OnRCCPPlayerDestroyed;
        RCCP_Events.OnRCCPAIDestroyed += RCCP_Events_OnRCCPAIDestroyed;
        if (RCCPSettings.useTelemetry)
            Instantiate(RCCPSettings.RCCPTelemetry, Vector3.zero, Quaternion.identity);
        if (RCCPSettings.overrideFixedTimeStep)
            Time.fixedDeltaTime = RCCPSettings.fixedTimeStep;

        // Overriding FPS.
        if (RCCPSettings.overrideFPS)
            Application.targetFrameRate = RCCPSettings.maxFPS;

        if (RCCPSettings.autoSaveLoadInputRebind)
            RCCP_RebindSaveLoad.Load();

    }

    #region ONSPAWNED
    private void RCCP_Events_OnRCCPSpawned(RCCP_CarController RCCP)
    {

        if (!allVehicles.Contains(RCCP))
            allVehicles.Add(RCCP);
        if (registerLastVehicleAsPlayer)
            RegisterPlayer(RCCP);

    }

    private void RCCP_Events_OnRCCPAISpawned(RCCP_CarController AI)
    {

        if (!allVehicles.Contains(AI))
            allVehicles.Add(AI);

    }

    /// <summary>
    /// When RCCP Camera spawned.
    /// </summary>
    /// <param name="BCGCamera"></param>
    private void RCCP_Events_OnRCCPCameraSpawned(RCCP_Camera cam)
    {

        activePlayerCamera = cam;

    }

    /// <summary>
    /// When RCCP Canvas spawned.
    /// </summary>
    /// <param name="UI"></param>
    private void RCCP_Events_OnRCCPUISpawned(RCCP_UIManager UI)
    {

        activePlayerCanvas = UI;

    }

    #endregion

    #region ONDESTROYED

    /// <summary>
    /// When a vehicle destroyed.
    /// </summary>
    /// <param name="RCCP"></param>
    private void RCCP_Events_OnRCCPPlayerDestroyed(RCCP_CarController RCCP)
    {

        if (allVehicles.Contains(RCCP))
            allVehicles.Remove(RCCP);

    }

    /// <summary>
    /// When an ai vehicle destroyed.
    /// </summary>
    /// <param name="RCCP"></param>
    private void RCCP_Events_OnRCCPAIDestroyed(RCCP_CarController AI)
    {

        if (allVehicles.Contains(AI))
            allVehicles.Remove(AI);

    }

    #endregion

    private void Start()
    {

        //  Getting all terrains.
        StartCoroutine(GetAllTerrains());

        //  Checking mutlithreading.
        StartCoroutine(CheckMT());

#if BCG_URP
        Invoke(nameof(CheckURPCamera), .5f);
#endif

    }

#if BCG_URP
    private void CheckURPCamera() {

        if (activeMainCamera != null) {

            UnityEngine.Rendering.Universal.UniversalAdditionalCameraData cameraData = activeMainCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

            if (cameraData != null && cameraData.renderPostProcessing == false)
                cameraData.renderPostProcessing = true;

            if (cameraData == null)
                Debug.LogError("'UniversalAdditionalCameraData' component couldn't found on the RCCP_Camera! Please select the 'actual camera' of the RCCP_Camera in your editor, it will add the missing component to the camera. Otherwise you can't see the lensflares along with post processing effects.");

        }

    }
#endif

    private IEnumerator CheckMT()
    {

        if (!RCCPSettings.multithreading)
        {

            asyncAttempted = false;
            asyncReceived = false;
            mutlithreadingSupported = false;
            yield break;

        }

        asyncAttempted = false;
        asyncReceived = false;

        CheckingMT();

        float timer = 1f;

        while (timer > 0)
        {

            timer -= Time.deltaTime;
            yield return null;

        }

        if (asyncAttempted && asyncReceived)
            mutlithreadingSupported = true;
        else
            mutlithreadingSupported = false;

        if (!mutlithreadingSupported)
            Debug.LogWarning("Multithreading is disabled on this platform, async can't be used with it. Regular methods will be used.");

        yield return null;

    }

    private async void CheckingMT()
    {

        asyncAttempted = true;
        asyncReceived = false;

        await Task.Run(() => { });

        asyncReceived = true;

    }

    /// <summary>
    /// Getting all terrains.
    /// </summary>
    /// <returns></returns>
    public IEnumerator GetAllTerrains()
    {

        yield return new WaitForFixedUpdate();
        allTerrains = Terrain.activeTerrains;
        yield return new WaitForFixedUpdate();

        //  If terrains found...
        if (allTerrains != null && allTerrains.Length >= 1)
        {

            terrains = new Terrains[allTerrains.Length];

            for (int i = 0; i < allTerrains.Length; i++)
            {

                if (allTerrains[i].terrainData == null)
                {

                    Debug.LogError("Terrain data of the " + allTerrains[i].transform.name + " is missing! Check the terrain data...");
                    yield return null;

                }

            }

            //  Initializing terrains.
            for (int i = 0; i < terrains.Length; i++)
            {

                terrains[i] = new Terrains();
                terrains[i].terrain = allTerrains[i];
                terrains[i].mTerrainData = allTerrains[i].terrainData;
                terrains[i].terrainCollider = allTerrains[i].GetComponent<TerrainCollider>().sharedMaterial;
                terrains[i].alphamapWidth = allTerrains[i].terrainData.alphamapWidth;
                terrains[i].alphamapHeight = allTerrains[i].terrainData.alphamapHeight;

                terrains[i].mSplatmapData = allTerrains[i].terrainData.GetAlphamaps(0, 0, terrains[i].alphamapWidth, terrains[i].alphamapHeight);
                terrains[i].mNumTextures = terrains[i].mSplatmapData.Length / (terrains[i].alphamapWidth * terrains[i].alphamapHeight);

            }

            terrainsInitialized = true;

        }

    }

    private void Update()
    {
        if (autoUpdateRaceRanks)
        {
            raceRankTimer += Time.deltaTime;
            if (raceRankTimer >= raceRankUpdateInterval)
            {
                raceRankTimer = 0f;
                UpdateVehicleRanks();
            }
        }
        //  When player vehicle changed...
        if (activePlayerVehicle)
        {

            if (activePlayerVehicle != lastActivePlayerVehicle)
                RCCP_Events.Event_OnVehicleChanged();

            if (activePlayerVehicle != lastActivePlayerVehicle)
                RCCP_Events.Event_OnVehicleChangedToVehicle(activePlayerVehicle);

            lastActivePlayerVehicle = activePlayerVehicle;

        }

        //  Checking UI canvas.
        if (disableUIWhenNoPlayerVehicle && activePlayerCanvas)
            CheckCanvas();

        //  Getting main camera.
        if (Camera.main != null)
            activeMainCamera = Camera.main;

    }

    /// <summary>
    /// Registers the target vehicle as player vehicle.
    /// </summary>
    /// <param name="playerVehicle"></param>
    public void RegisterPlayer(RCCP_CarController playerVehicle)
    {

        activePlayerVehicle = playerVehicle;

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

    }

    /// <summary>
    /// Registers the target vehicle as player vehicle. Also sets controllable state of the vehicle.
    /// </summary>
    /// <param name="playerVehicle"></param>
    /// <param name="isControllable"></param>
    public void RegisterPlayer(RCCP_CarController playerVehicle, bool isControllable)
    {

        activePlayerVehicle = playerVehicle;
        activePlayerVehicle.SetCanControl(isControllable);

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

    }

    /// <summary>
    /// Registers the target vehicle as player vehicle. Also sets controllable state and engine state of the vehicle.
    /// </summary>
    /// <param name="playerVehicle"></param>
    /// <param name="isControllable"></param>
    /// <param name="engineState"></param>
    public void RegisterPlayer(RCCP_CarController playerVehicle, bool isControllable, bool engineState)
    {

        activePlayerVehicle = playerVehicle;
        activePlayerVehicle.SetCanControl(isControllable);
        activePlayerVehicle.SetEngine(engineState);

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

    }

    /// <summary>
    /// Deregisters the player vehicle.
    /// </summary>
    public void DeRegisterPlayer()
    {

        if (activePlayerVehicle)
            activePlayerVehicle.SetCanControl(false);

        activePlayerVehicle = null;

        if (activePlayerCamera)
            activePlayerCamera.RemoveTarget();

    }

    /// <summary>
    /// Checks UI canvas.
    /// </summary>
    public void CheckCanvas()
    {

        //if (!activePlayerVehicle || !activePlayerVehicle.canControl || !activePlayerVehicle.gameObject.activeInHierarchy || !activePlayerVehicle.enabled) {

        //    activePlayerCanvas.SetDisplayType(RCC_UIDashboardDisplay.DisplayType.Off);

        //    return;

        //}

        //if (activePlayerCanvas.displayType != RCC_UIDashboardDisplay.DisplayType.Customization)
        //    activePlayerCanvas.displayType = RCC_UIDashboardDisplay.DisplayType.Full;

    }

    ///<summary>
    /// Sets new behavior.
    ///</summary>
    public void SetBehavior(int behaviorIndex)
    {

        RCCPSettings.overrideBehavior = true;
        RCCPSettings.behaviorSelectedIndex = behaviorIndex;

        RCCP_Events.Event_OnBehaviorChanged();

    }

    public void SetMobileController(RCCP_Settings.MobileController mobileController)
    {

        RCCPSettings.mobileController = mobileController;

    }

    /// <summary>
    /// Changes current camera mode.
    /// </summary>
    public void ChangeCamera()
    {

        if (activePlayerCamera)
            activePlayerCamera.ChangeCamera();

    }

    /// <summary>
    /// Transport player vehicle the specified position and rotation.
    /// </summary>
    /// <param name="position">Position.</param>
    /// <param name="rotation">Rotation.</param>
    public void Transport(Vector3 position, Quaternion rotation)
    {

        if (activePlayerVehicle)
        {

            RigidbodyInterpolation interpolation = activePlayerVehicle.Rigid.interpolation;
            activePlayerVehicle.Rigid.interpolation = RigidbodyInterpolation.None;

            activePlayerVehicle.Rigid.linearVelocity = Vector3.zero;
            activePlayerVehicle.Rigid.angularVelocity = Vector3.zero;

            activePlayerVehicle.Rigid.MovePosition(position);
            activePlayerVehicle.Rigid.MoveRotation(rotation);

            activePlayerVehicle.Rigid.linearVelocity = Vector3.zero;
            activePlayerVehicle.Rigid.angularVelocity = Vector3.zero;

            activePlayerVehicle.Rigid.interpolation = interpolation;

            for (int i = 0; i < activePlayerVehicle.AllWheelColliders.Length; i++)
                activePlayerVehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

            RCCP_TrailerController trailer = activePlayerVehicle.ConnectedTrailer;

            if (trailer)
            {

                Rigidbody trailerRigid = trailer.GetComponent<Rigidbody>();

                if (trailerRigid)
                {

                    if (trailerRigid)
                    {

                        // Store original interpolation settings for trailer
                        RigidbodyInterpolation trailerInterpolation = trailerRigid.interpolation;
                        trailerRigid.interpolation = RigidbodyInterpolation.None;

                        // Calculate new trailer position and rotation relative to the vehicle
                        Vector3 trailerOffset = trailerRigid.transform.position - activePlayerVehicle.transform.position;
                        Quaternion trailerRelativeRotation = Quaternion.Inverse(activePlayerVehicle.transform.rotation) * trailerRigid.transform.rotation;

                        // Move trailer relative to the new vehicle position
                        trailerRigid.linearVelocity = Vector3.zero;
                        trailerRigid.angularVelocity = Vector3.zero;

                        trailerRigid.MovePosition(position + rotation * trailerOffset);
                        trailerRigid.MoveRotation(rotation * trailerRelativeRotation);

                        trailerRigid.linearVelocity = Vector3.zero;
                        trailerRigid.angularVelocity = Vector3.zero;

                        // Restore interpolation settings for trailer
                        trailerRigid.interpolation = trailerInterpolation;

                    }

                }

            }

            Physics.SyncTransforms();

        }

    }

    /// <summary>
    /// Transport target vehicle the specified position and rotation.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    public void Transport(RCCP_CarController vehicle, Vector3 position, Quaternion rotation)
    {

        if (vehicle)
        {

            RigidbodyInterpolation interpolation = vehicle.Rigid.interpolation;
            vehicle.Rigid.interpolation = RigidbodyInterpolation.None;

            vehicle.Rigid.linearVelocity = Vector3.zero;
            vehicle.Rigid.angularVelocity = Vector3.zero;

            vehicle.Rigid.MovePosition(position);
            vehicle.Rigid.MoveRotation(rotation);

            vehicle.Rigid.linearVelocity = Vector3.zero;
            vehicle.Rigid.angularVelocity = Vector3.zero;

            vehicle.Rigid.interpolation = interpolation;

            for (int i = 0; i < vehicle.AllWheelColliders.Length; i++)
                vehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

            RCCP_TrailerController trailer = vehicle.ConnectedTrailer;

            if (trailer)
            {

                Rigidbody trailerRigid = trailer.GetComponent<Rigidbody>();

                if (trailerRigid)
                {

                    if (trailerRigid)
                    {

                        // Store original interpolation settings for trailer
                        RigidbodyInterpolation trailerInterpolation = trailerRigid.interpolation;
                        trailerRigid.interpolation = RigidbodyInterpolation.None;

                        // Calculate new trailer position and rotation relative to the vehicle
                        Vector3 trailerOffset = trailerRigid.transform.position - vehicle.transform.position;
                        Quaternion trailerRelativeRotation = Quaternion.Inverse(vehicle.transform.rotation) * trailerRigid.transform.rotation;

                        // Move trailer relative to the new vehicle position
                        trailerRigid.linearVelocity = Vector3.zero;
                        trailerRigid.angularVelocity = Vector3.zero;

                        trailerRigid.MovePosition(position + rotation * trailerOffset);
                        trailerRigid.MoveRotation(rotation * trailerRelativeRotation);

                        trailerRigid.linearVelocity = Vector3.zero;
                        trailerRigid.angularVelocity = Vector3.zero;

                        // Restore interpolation settings for trailer
                        trailerRigid.interpolation = trailerInterpolation;

                    }

                }

            }

            Physics.SyncTransforms();

        }

    }

    public void Transport(RCCP_CarController vehicle, Vector3 position, Quaternion rotation, bool resetVelocity)
    {

        if (vehicle)
        {

            if (resetVelocity)
            {

                RigidbodyInterpolation interpolation = vehicle.Rigid.interpolation;
                vehicle.Rigid.interpolation = RigidbodyInterpolation.None;

                vehicle.Rigid.linearVelocity = Vector3.zero;
                vehicle.Rigid.angularVelocity = Vector3.zero;

                vehicle.Rigid.MovePosition(position);
                vehicle.Rigid.MoveRotation(rotation);

                vehicle.Rigid.linearVelocity = Vector3.zero;
                vehicle.Rigid.angularVelocity = Vector3.zero;

                vehicle.Rigid.interpolation = interpolation;

                for (int i = 0; i < vehicle.AllWheelColliders.Length; i++)
                    vehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

                Physics.SyncTransforms();

            }
            else
            {

                vehicle.Rigid.MovePosition(position);
                vehicle.Rigid.MoveRotation(rotation);

                Physics.SyncTransforms();

            }

        }

    }

    private void OnDisable()
    {

        if (RCCPSettings.autoSaveLoadInputRebind)
            RCCP_RebindSaveLoad.Save();

    }

    private void OnDestroy()
    {

        RCCP_Events.OnRCCPCameraSpawned -= RCCP_Events_OnRCCPCameraSpawned;
        RCCP_Events.OnRCCPSpawned -= RCCP_Events_OnRCCPSpawned;
        RCCP_Events.OnRCCPAISpawned -= RCCP_Events_OnRCCPAISpawned;
        RCCP_Events.OnRCCPUISpawned -= RCCP_Events_OnRCCPUISpawned;
        RCCP_Events.OnRCCPDestroyed -= RCCP_Events_OnRCCPPlayerDestroyed;
        RCCP_Events.OnRCCPAIDestroyed -= RCCP_Events_OnRCCPAIDestroyed;

    }

}
