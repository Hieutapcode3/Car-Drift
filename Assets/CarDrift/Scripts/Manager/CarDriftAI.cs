using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RaceVehicle))]
public class CarDriftAI : MonoBehaviour
{
    [SerializeField] private RCCP_CarController car;
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private float waypointReachDistance = 8f;
    [SerializeField] private float targetSpeed = 80f;
    [SerializeField] private float cornerBrakeAngle = 35f;
    [SerializeField] private float driftAngle = 55f;

    private int waypointIndex;
    private RCCP_Inputs inputs = new RCCP_Inputs();

    private void Awake()
    {
        if (!car)
            car = GetComponentInParent<RCCP_CarController>();
    }

    private void OnEnable()
    {
        if (car && car.Inputs)
            car.Inputs.OverrideInputs(inputs);
    }

    private void OnDisable()
    {
        if (car && car.Inputs)
            car.Inputs.DisableOverrideInputs();
    }

    private void Update()
    {
        if (!car || !car.Inputs || waypoints.Count == 0)
            return;

        Transform target = waypoints[waypointIndex];
        Vector3 localTarget = car.transform.InverseTransformPoint(target.position);
        float steer = Mathf.Clamp(localTarget.x / Mathf.Max(1f, Mathf.Abs(localTarget.z)), -1f, 1f);
        float angle = Mathf.Abs(Vector3.SignedAngle(car.transform.forward, target.position - car.transform.position, Vector3.up));

        inputs.throttleInput = car.absoluteSpeed < targetSpeed ? 1f : 0.35f;
        inputs.brakeInput = angle > cornerBrakeAngle ? 0.45f : 0f;
        inputs.handbrakeInput = angle > driftAngle ? 0.65f : 0f;
        inputs.steerInput = steer;
        inputs.clutchInput = 0f;
        inputs.nosInput = 0f;

        car.Inputs.OverrideInputs(inputs);

        if (Vector3.Distance(car.transform.position, target.position) <= waypointReachDistance)
            waypointIndex = (waypointIndex + 1) % waypoints.Count;
    }
}
