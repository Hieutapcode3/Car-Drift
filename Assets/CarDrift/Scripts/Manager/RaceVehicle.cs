using UnityEngine;

public class RaceVehicle : MonoBehaviour
{
    public bool isPlayer;
    public int CurrentLap { get; set; }
    public int NextCheckpointIndex { get; set; }
    public RCCP_CarController Car { get; private set; }

    private void Awake()
    {
        Car = GetComponentInParent<RCCP_CarController>();
    }
}
