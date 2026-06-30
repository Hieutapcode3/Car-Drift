using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RaceCheckpoint : MonoBehaviour
{
    [SerializeField] private int checkpointIndex;
    [SerializeField] private bool finishLine;

    public int CheckpointIndex => checkpointIndex;
    public bool FinishLine => finishLine;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        RaceVehicle vehicle = other.GetComponentInParent<RaceVehicle>();
        if (vehicle)
            RaceManager.Instance.VehiclePassedCheckpoint(vehicle, this);
    }
}
