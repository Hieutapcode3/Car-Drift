using System.Collections;
using System.Collections.Generic;
using UnityCommunity.UnitySingleton;
using UnityEngine;

public class RaceManager : MonoSingleton<RaceManager>
{
    [SerializeField] private string mapId = "default";
    [SerializeField] private int totalLaps = 3;
    [SerializeField] private float countdownSeconds = 3f;
    [SerializeField] private int baseWinReward = 500;
    [SerializeField] private int driftRewardDivider = 10;
    [SerializeField] private List<RaceCheckpoint> checkpoints = new List<RaceCheckpoint>();
    [SerializeField] private List<RaceVehicle> vehicles = new List<RaceVehicle>();

    public RaceState State { get; private set; } = RaceState.Waiting;
    public float RaceTime { get; private set; }
    public int DriftScore { get; private set; }
    public int PlayerLap { get; private set; }

    private RaceVehicle playerVehicle;

    private void Start()
    {
        if (checkpoints.Count == 0)
            checkpoints.AddRange(FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None));

        if (vehicles.Count == 0)
            vehicles.AddRange(FindObjectsByType<RaceVehicle>(FindObjectsSortMode.None));

        playerVehicle = vehicles.Find(x => x.isPlayer);
        StartRace();
    }

    private void Update()
    {
        if (State != RaceState.Racing)
            return;

        RaceTime += Time.deltaTime;
        UpdateDriftScore();
    }

    public void StartRace()
    {
        if (State == RaceState.Racing || State == RaceState.Countdown)
            return;

        StartCoroutine(CountdownRoutine());
    }

    public void VehiclePassedCheckpoint(RaceVehicle vehicle, RaceCheckpoint checkpoint)
    {
        if (State != RaceState.Racing || vehicle == null || checkpoint == null)
            return;

        if (checkpoint.CheckpointIndex != vehicle.NextCheckpointIndex)
            return;

        vehicle.NextCheckpointIndex++;

        if (vehicle.NextCheckpointIndex >= checkpoints.Count)
        {
            vehicle.NextCheckpointIndex = 0;
            vehicle.CurrentLap++;

            if (vehicle.isPlayer)
                PlayerLap = vehicle.CurrentLap;

            if (vehicle.CurrentLap >= totalLaps)
                FinishRace(vehicle.isPlayer);
        }
    }

    private IEnumerator CountdownRoutine()
    {
        State = RaceState.Countdown;
        SetVehiclesControl(false);

        float remaining = countdownSeconds;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        RaceTime = 0f;
        DriftScore = 0;
        State = RaceState.Racing;
        SetVehiclesControl(true);
    }

    private void FinishRace(bool playerWon)
    {
        if (State == RaceState.Finished)
            return;

        State = RaceState.Finished;
        SetVehiclesControl(false);

        if (playerWon)
        {
            int reward = baseWinReward + Mathf.Max(0, DriftScore / Mathf.Max(1, driftRewardDivider));
            DataManager.Instance.AddCoins(reward);
            DataManager.Instance.SaveRaceResult(mapId, RaceTime, DriftScore);
            HUDSystem.Instance.Show<WinPanel>();
        }
        else
        {
            HUDSystem.Instance.Show<LosePanel>();
        }
    }

    private void SetVehiclesControl(bool canControl)
    {
        foreach (RaceVehicle vehicle in vehicles)
        {
            if (vehicle && vehicle.Car)
                vehicle.Car.SetCanControl(canControl);
        }
    }

    private void UpdateDriftScore()
    {
        if (!playerVehicle || !playerVehicle.Car)
            return;

        RCCP_CarController car = playerVehicle.Car;
        bool drifting = car.handbrakeInput_P > 0.1f || car.handbrakeInput_V > 0.1f;

        if (drifting && car.absoluteSpeed > 15f)
            DriftScore += Mathf.RoundToInt(car.absoluteSpeed * Time.deltaTime);
    }
}
