using System.Collections.Generic;
using UnityCommunity.UnitySingleton;
using UnityEngine;

public class RaceProgressTracker : MonoSingleton<RaceProgressTracker>
{
    [Header("Settings")]
    public float updateInterval = 0.05f;
    public int totalLaps = 3;

    [Header("Rank Stabilization")]
    [Tooltip("Number of consecutive updates a rank must remain the same before updating the display text.")]
    public int rankStabilityFrames = 5;

    [Header("Player Waypoint")]
    [Tooltip("Distance (m) within which the player is considered to have 'passed' a waypoint.")]
    public float playerWpPassDistance = 10f;

    [Header("Post-Finish Settings")]
    public int stopAtWaypointAfterFinish = 10;

    [Header("References")]
    public RCCP_AIWaypointsContainer waypointsContainer;

    [Header("Debug (ReadOnly)")]
    [SerializeField] private float totalLapDistance;
    [SerializeField] private float totalRaceDistance;

    private float[] wpCumDist;
    private int wpCount;
    private float timer;

    private class LapTrackData
    {
        public int completedLaps = 0;
        public int currentTargetWpIdx = 1;
        public int totalWaypointsPassed = 0;
        public bool canCompleteLap = false;
    }

    private readonly Dictionary<CarController, LapTrackData> playerLapData
        = new Dictionary<CarController, LapTrackData>();

    private readonly HashSet<CarController> finishedCars = new HashSet<CarController>();
    public IReadOnlyCollection<CarController> FinishedCars => finishedCars;

    private class RankStabilityData
    {
        public int displayedRank = 0;
        public int pendingRank = 0;
        public int consecutiveCount = 0;
    }

    private readonly Dictionary<CarController, RankStabilityData> rankStability
        = new Dictionary<CarController, RankStabilityData>();

    public struct VehicleProgress
    {
        public CarController car;
        public int rank;
        public float totalPoints;
        public float completionPct;
    }

    private readonly List<VehicleProgress> rankings = new List<VehicleProgress>();
    public IReadOnlyList<VehicleProgress> Rankings => rankings;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (waypointsContainer == null)
        {
#if UNITY_2023_1_OR_NEWER
            waypointsContainer = FindFirstObjectByType<RCCP_AIWaypointsContainer>();
#else
            waypointsContainer = FindObjectOfType<RCCP_AIWaypointsContainer>();
#endif
        }

        BuildCumulativeDistanceTable();
    }

    private void FixedUpdate()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;
        UpdateAllRanks();
    }

    private void BuildCumulativeDistanceTable()
    {
        if (waypointsContainer == null
            || waypointsContainer.waypoints == null
            || waypointsContainer.waypoints.Count < 2)
        {
            Debug.LogWarning("[RaceProgressTracker] Need at least 2 waypoints in RCCP_AIWaypointsContainer.");
            return;
        }

        wpCount = waypointsContainer.waypoints.Count;
        wpCumDist = new float[wpCount + 1];
        wpCumDist[0] = 0f;

        for (int i = 1; i < wpCount; i++)
        {
            var wpA = waypointsContainer.waypoints[i - 1];
            var wpB = waypointsContainer.waypoints[i];
            float seg = (wpA != null && wpB != null)
                ? Vector3.Distance(wpA.transform.position, wpB.transform.position)
                : 0f;
            wpCumDist[i] = wpCumDist[i - 1] + seg;
        }

        var wpLast = waypointsContainer.waypoints[wpCount - 1];
        var wpFirst = waypointsContainer.waypoints[0];
        float closingSeg = (wpLast != null && wpFirst != null)
            ? Vector3.Distance(wpLast.transform.position, wpFirst.transform.position)
            : 0f;
        wpCumDist[wpCount] = wpCumDist[wpCount - 1] + closingSeg;

        totalLapDistance = wpCumDist[wpCount];
        totalRaceDistance = totalLapDistance * Mathf.Max(1, totalLaps);

        Debug.Log($"[RaceProgressTracker] 1 lap = {totalLapDistance:F1}m x {totalLaps} laps = {totalRaceDistance:F1}m");
    }

    private void UpdatePlayerTargetWaypoint(CarController car, Vector3 carPos, Vector3 carForward)
    {
        if (!playerLapData.TryGetValue(car, out LapTrackData d)) return;
        if (waypointsContainer == null || waypointsContainer.waypoints == null || wpCount == 0) return;

        var targetWp = waypointsContainer.waypoints[d.currentTargetWpIdx % wpCount];
        if (targetWp == null) return;

        float radius = (waypointsContainer != null && waypointsContainer.waypointRadius > 0) ? waypointsContainer.waypointRadius : playerWpPassDistance;
        float distSq = (carPos - targetWp.transform.position).sqrMagnitude;
        float threshSq = radius * radius;

        Vector3 toWp = targetWp.transform.position - carPos;
        bool passedWpPlane = Vector3.Dot(carForward, toWp) < 0f && distSq < (threshSq * 3.24f);

        if (distSq > threshSq && !passedWpPlane) return;

        int passedIdx = d.currentTargetWpIdx % wpCount;
        d.currentTargetWpIdx = (passedIdx + 1) % wpCount;
        d.totalWaypointsPassed++;

        if (passedIdx >= wpCount / 2)
            d.canCompleteLap = true;

        if (passedIdx == 0 && d.canCompleteLap)
        {
            d.completedLaps++;
            d.canCompleteLap = false;
            Debug.Log($"[RaceProgressTracker] 🏁 Player completed lap {d.completedLaps}/{totalLaps}");

            if (car.controllerType == ControllerType.Player && RCCP_UIManager.Instance != null)
            {
                RCCP_UIManager.Instance.SetLap(d.completedLaps + 1);
            }
        }
    }

    private float CalcDistanceInLap(Vector3 carPos, int targetWpIdx)
    {
        if (wpCumDist == null || waypointsContainer == null || waypointsContainer.waypoints == null || wpCount < 2)
            return 0f;

        int fromIdx = (targetWpIdx - 1 + wpCount) % wpCount;

        var wpA = waypointsContainer.waypoints[fromIdx];
        var wpB = waypointsContainer.waypoints[targetWpIdx];

        if (wpA == null || wpB == null) return wpCumDist[Mathf.Clamp(fromIdx, 0, wpCount)];

        Vector3 a = wpA.transform.position;
        Vector3 b = wpB.transform.position;
        Vector3 ab = b - a;
        float segLenSq = ab.sqrMagnitude;

        float t = 0f;
        if (segLenSq > 0.0001f)
        {
            t = Vector3.Dot(carPos - a, ab) / segLenSq;
            t = Mathf.Clamp01(t);
        }

        float startDist = wpCumDist[fromIdx];
        float endDist = (fromIdx == wpCount - 1 && targetWpIdx == 0) ? wpCumDist[wpCount] : wpCumDist[targetWpIdx];
        float segLen = endDist - startDist;

        return startDist + t * segLen;
    }

    private float CalcTotalPoints(CarController car, out int outWpIdx, out int outLaps, out float outDistInLap)
    {
        outWpIdx = 0;
        outLaps = 0;
        outDistInLap = 0f;

        if (wpCumDist == null || totalRaceDistance <= 0f)
            return car.transform.position.z;

        Transform activeTransform = (car.carController != null) ? car.carController.transform : car.transform;
        Vector3 carPos = activeTransform.position;
        Vector3 carForward = activeTransform.forward;
        int wpIdx;
        int laps;

        if (car.controllerType == ControllerType.AI && car.aiController != null)
        {
            laps = car.aiController.lap;
            wpIdx = Mathf.Clamp(car.aiController.currentWaypointIndex, 0, wpCount - 1);

            car.indexTargetPoint = wpIdx;
            car.totalWaypointsPassed = car.aiController.totalWaypointsPassed;
            car.currentLapCount = laps + 1;
        }
        else
        {
            UpdatePlayerTargetWaypoint(car, carPos, carForward);
            laps = playerLapData.TryGetValue(car, out var d) ? d.completedLaps : 0;
            wpIdx = playerLapData.TryGetValue(car, out var d2) ? d2.currentTargetWpIdx : 0;

            car.indexTargetPoint = wpIdx;
            car.totalWaypointsPassed = playerLapData.TryGetValue(car, out var d3) ? d3.totalWaypointsPassed : 0;
            car.currentLapCount = laps + 1;
        }

        outWpIdx = wpIdx;
        outLaps = laps;

        outDistInLap = CalcDistanceInLap(carPos, wpIdx);

        return laps * totalLapDistance + outDistInLap;
    }

    private void UpdateAllRanks()
    {
#if UNITY_2023_1_OR_NEWER
        CarController[] allCars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
#else
        CarController[] allCars = FindObjectsOfType<CarController>();
#endif
        if (allCars == null || allCars.Length == 0) return;

        foreach (var car in allCars)
        {
            if (car == null || car.controllerType == ControllerType.Menu) continue;

            if (car.controllerType == ControllerType.Player && !playerLapData.ContainsKey(car))
                playerLapData[car] = new LapTrackData();

            if (!rankStability.ContainsKey(car))
                rankStability[car] = new RankStabilityData();
        }

        rankings.Clear();

        foreach (var car in allCars)
        {
            if (car == null || !car.gameObject.activeInHierarchy) continue;
            if (car.controllerType == ControllerType.Menu) continue;

            float points = CalcTotalPoints(car, out int wpIdx, out int laps, out float distInLap);
            CheckVehicleFinish(car, laps, wpIdx);

            float pct = totalRaceDistance > 0f
                ? Mathf.Clamp01((laps * totalLapDistance + distInLap) / totalRaceDistance) * 100f
                : 0f;

            rankings.Add(new VehicleProgress
            {
                car = car,
                totalPoints = points,
                completionPct = pct
            });
        }

        rankings.Sort((a, b) => b.totalPoints.CompareTo(a.totalPoints));

        for (int i = 0; i < rankings.Count; i++)
        {
            var vp = rankings[i];
            vp.rank = i + 1;
            rankings[i] = vp;

            if (!rankStability.TryGetValue(vp.car, out var stability)) continue;

            if (stability.pendingRank == vp.rank)
            {
                stability.consecutiveCount++;
            }
            else
            {
                stability.pendingRank = vp.rank;
                stability.consecutiveCount = 1;
            }

            if (stability.consecutiveCount >= rankStabilityFrames && stability.displayedRank != vp.rank)
            {
                stability.displayedRank = vp.rank;
                vp.car.UpdateRaceRank(vp.rank);
            }
        }
    }

    public int GetRank(CarController car)
    {
        foreach (var vp in rankings)
            if (vp.car == car) return vp.rank;
        return 0;
    }

    public float GetCompletionPercent(CarController car)
    {
        foreach (var vp in rankings)
            if (vp.car == car) return vp.completionPct;
        return 0f;
    }

    public int GetPlayerLap(CarController car)
    {
        if (playerLapData.TryGetValue(car, out var d)) return d.completedLaps;
        return 0;
    }

    public int GetTargetWaypointIndex(CarController car)
    {
        if (car == null) return 0;

        if (car.controllerType == ControllerType.AI && car.aiController != null)
        {
            return car.aiController.currentWaypointIndex;
        }

        if (playerLapData.TryGetValue(car, out var d))
        {
            return d.currentTargetWpIdx;
        }

        return 0;
    }

    public bool IsVehicleFinished(CarController car)
    {
        return car != null && finishedCars.Contains(car);
    }

    private void CheckVehicleFinish(CarController car, int laps, int currentWpIdx)
    {
        bool hasFinished = laps >= totalLaps;

        if (hasFinished)
        {
            if (!finishedCars.Contains(car))
            {
                finishedCars.Add(car);
                if (RCCP_SceneManager.Instance != null && car.carController != null)
                {
                    RCCP_SceneManager.Instance.RegisterFinishedVehicle(car.carController);
                }

                if (car.controllerType == ControllerType.Player && GameManager.Instance != null)
                {
                    GameManager.Instance.OnFinish();
                }
            }

            if (car.controllerType == ControllerType.AI && car.aiController != null)
            {
                int targetStopWp = Mathf.Min(stopAtWaypointAfterFinish, wpCount > 0 ? wpCount - 1 : stopAtWaypointAfterFinish);

                if (car.aiController.navigationMode != RCCP_AI.NavigationMode.Off && currentWpIdx >= targetStopWp)
                {
                    car.aiController.navigationMode = RCCP_AI.NavigationMode.Off;
                    car.aiController.throttleInput = 0f;
                    car.aiController.brakeInput = 0f;
                    car.aiController.handbrakeInput = 1f;
                    car.aiController.steerInput = 0f;
                }
                else if (car.aiController.navigationMode == RCCP_AI.NavigationMode.Off)
                {
                    car.aiController.throttleInput = 0f;
                    car.aiController.brakeInput = 0f;
                    car.aiController.handbrakeInput = 1f;
                    car.aiController.steerInput = 0f;
                }
            }
        }
    }

    public int TotalParticipants => rankings.Count;
}
