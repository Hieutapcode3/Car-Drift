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
    public float playerWpPassDistance = 20f;

    [Header("References")]
    public RCCP_AIWaypointsContainer waypointsContainer;

    [Header("Debug (ReadOnly)")]
    [SerializeField] private float totalLapDistance;
    [SerializeField] private float totalRaceDistance;

    private float[] wpCumDist;
    private int wpCount;
    private float timer;

    // Per-player lap & waypoint tracking (no RCCP_AI)
    private class LapTrackData
    {
        public int completedLaps = 0;
        public int currentTargetWpIdx = 0; // Next waypoint player is heading toward (mirrors RCCP_AI.currentWaypointIndex)
        public bool canCompleteLap = false;
    }

    private readonly Dictionary<CarController, LapTrackData> playerLapData
        = new Dictionary<CarController, LapTrackData>();

    // Per-AI rank stabilization
    private class RankStabilityData
    {
        public int displayedRank = 0;    // Rank currently shown in text
        public int pendingRank = 0;      // Rank computed this update
        public int consecutiveCount = 0; // How many updates pendingRank has stayed the same
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
            waypointsContainer = FindFirstObjectByType<RCCP_AIWaypointsContainer>();

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

    // Advance the player's target waypoint when close enough — mirrors how RCCP_AI tracks currentWaypointIndex
    private void UpdatePlayerTargetWaypoint(CarController car, Vector3 carPos)
    {
        if (!playerLapData.TryGetValue(car, out LapTrackData d)) return;

        var targetWp = waypointsContainer.waypoints[d.currentTargetWpIdx];
        if (targetWp == null) return;

        float distSq = (carPos - targetWp.transform.position).sqrMagnitude;
        float threshSq = playerWpPassDistance * playerWpPassDistance;

        if (distSq > threshSq) return;

        int passedIdx = d.currentTargetWpIdx;
        d.currentTargetWpIdx = (passedIdx + 1) % wpCount;

        // Crossed into the second half → allow lap completion
        if (passedIdx >= wpCount / 2)
            d.canCompleteLap = true;

        // Crossed back to waypoint 0 (finish line)
        if (passedIdx == wpCount - 1 && d.canCompleteLap)
        {
            d.completedLaps++;
            d.canCompleteLap = false;
            Debug.Log($"[RaceProgressTracker] Player completed lap {d.completedLaps}/{totalLaps}");
        }
    }

    private float CalcTotalPoints(CarController car, out int outWpIdx, out int outLaps)
    {
        outWpIdx = 0;
        outLaps = 0;

        if (wpCumDist == null || totalRaceDistance <= 0f)
            return car.transform.position.z;

        Vector3 carPos = car.transform.position;
        int wpIdx;
        int laps;

        if (car.aiController != null)
        {
            // AI: read directly from RCCP_AI — already points to the next (target) waypoint
            laps = car.aiController.lap;
            wpIdx = Mathf.Clamp(car.aiController.currentWaypointIndex, 0, wpCount - 1);
        }
        else
        {
            // Player: advance sequential target waypoint, same convention as RCCP_AI
            UpdatePlayerTargetWaypoint(car, carPos);
            laps = playerLapData.TryGetValue(car, out var d) ? d.completedLaps : 0;
            wpIdx = playerLapData.TryGetValue(car, out var d2) ? d2.currentTargetWpIdx : 0;
        }

        outWpIdx = wpIdx;
        outLaps = laps;

        int nextIdx = (wpIdx + 1) % wpCount;
        var nextWp = waypointsContainer.waypoints[nextIdx];
        float distToNext = nextWp != null
            ? Vector3.Distance(carPos, nextWp.transform.position)
            : 0f;

        float lapScore = laps * 100_000_000f;
        float wpScore = wpIdx * 100_000f;
        float distScore = Mathf.Max(0f, 100_000f - distToNext);

        return lapScore + wpScore + distScore;
    }

    private void UpdateAllRanks()
    {
        CarController[] allCars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        if (allCars == null || allCars.Length == 0) return;

        // Register new players and AI cars
        foreach (var car in allCars)
        {
            if (car == null || car.controllerType == ControllerType.Menu) continue;

            if (car.controllerType == ControllerType.Player && !playerLapData.ContainsKey(car))
                playerLapData[car] = new LapTrackData();

            if (car.controllerType == ControllerType.AI && !rankStability.ContainsKey(car))
                rankStability[car] = new RankStabilityData();
        }

        rankings.Clear();

        foreach (var car in allCars)
        {
            if (car == null || !car.gameObject.activeInHierarchy) continue;
            if (car.controllerType == ControllerType.Menu) continue;

            float points = CalcTotalPoints(car, out int wpIdx, out int laps);

            float distInLap = (wpCumDist != null && wpIdx < wpCumDist.Length)
                ? wpCumDist[wpIdx] : 0f;
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

        // Sort descending: highest totalPoints = rank 1
        rankings.Sort((a, b) => b.totalPoints.CompareTo(a.totalPoints));

        // Assign computed ranks and apply stabilized display for AI
        for (int i = 0; i < rankings.Count; i++)
        {
            var vp = rankings[i];
            vp.rank = i + 1;
            rankings[i] = vp;

            if (vp.car.controllerType != ControllerType.AI) continue;
            if (!rankStability.TryGetValue(vp.car, out var stability)) continue;

            if (stability.pendingRank == vp.rank)
            {
                stability.consecutiveCount++;
            }
            else
            {
                // New rank computed — reset stability counter
                stability.pendingRank = vp.rank;
                stability.consecutiveCount = 1;
            }

            // Only push new rank to text once it's been stable long enough
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

    public int TotalParticipants => rankings.Count;
}
