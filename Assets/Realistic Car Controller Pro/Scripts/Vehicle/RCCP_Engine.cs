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

/// <summary>
/// Main power generator of the vehicle. Produces and transmits the generated power to the clutch.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP Engine")]
public class RCCP_Engine : RCCP_Component {

    /// <summary>
    /// If true, overrides the engine RPM with an externally provided value. All internal calculations will be skipped.
    /// </summary>
    public bool overrideEngineRPM = false;

    /// <summary>
    /// Indicates whether the engine is currently running.
    /// </summary>
    public bool engineRunning = true;

    /// <summary>
    /// Indicates whether the engine is in the process of starting (a brief delay).
    /// </summary>
    public bool engineStarting = false;

    /// <summary>
    /// Current engine RPM (revolutions per minute).
    /// </summary>
    [Min(0f)] public float engineRPM = 0f;

    /// <summary>
    /// Minimum engine RPM (typical idle speed).
    /// </summary>
    [Min(0f)] public float minEngineRPM = 750f;

    /// <summary>
    /// Maximum engine RPM (redline).
    /// </summary>
    [Min(0f)] public float maxEngineRPM = 7000f;

    /// <summary>
    /// Rate at which the engine freely accelerates when the drivetrain is disengaged (e.g., clutch in, neutral).
    /// </summary>
    public float engineAccelerationRate = .75f;

    /// <summary>
    /// How strongly the engine couples to the wheels when the clutch is engaged.
    /// </summary>
    public float engineCouplingToWheelsRate = 1.5f;

    /// <summary>
    /// Rate at which the engine RPM drops due to friction or no throttle input.
    /// </summary>
    public float engineDecelerationRate = .35f;

    /// <summary>
    /// Raw target RPM used internally for smoothing.
    /// </summary>
    [Min(0f)] internal float wantedEngineRPMRaw = 0f;

    /// <summary>
    /// Internal velocity for SmoothDamp usage on engine RPM.
    /// </summary>
    private float engineVelocity;

    /// <summary>
    /// Torque curve (normalized 0-1) for the engine. X axis is RPM, Y axis is normalized torque (0-1).
    /// </summary>
    public AnimationCurve NMCurve = new AnimationCurve(new Keyframe(750f, .8f), new Keyframe(4500f, 1f), new Keyframe(7000f, .85f));

    /// <summary>
    /// If true, automatically generates the NMCurve based on minEngineRPM, maxTorqueAtRPM, and maxEngineRPM when the script is reset.
    /// </summary>
    public bool autoCreateNMCurve = true;

    /// <summary>
    /// Desired RPM at which the engine produces peak torque (used if autoCreateNMCurve is true).
    /// </summary>
    [Min(0f)] public float maxTorqueAtRPM = 4500f;

    /// <summary>
    /// If true, cuts fuel input once RPM approaches maxEngineRPM to act as a rev limiter.
    /// </summary>
    public bool engineRevLimiter = true;

    /// <summary>
    /// Becomes true when the rev limiter is actively cutting fuel.
    /// </summary>
    public bool cutFuel = false;

    /// <summary>
    /// Enables forced induction simulation (turbo). If true, turboChargePsi is calculated each frame.
    /// </summary>
    public bool turboCharged = false;

    /// <summary>
    /// Current turbo pressure in PSI.
    /// </summary>
    [Min(0f)] public float turboChargePsi = 0f;

    /// <summary>
    /// Last frame’s PSI, used for blow-off detection.
    /// </summary>
    [Min(0f)] internal float turboChargePsi_Old = 0f;

    /// <summary>
    /// Max turbo boost (PSI) that can be reached at full throttle and high RPM.
    /// </summary>
    [Min(0f)] public float maxTurboChargePsi = 12f;

    /// <summary>
    /// Maximum torque multiplier from the turbo at max boost.
    /// </summary>
    [Min(0f)] public float turboChargerCoEfficient = 1.75f;

    /// <summary>
    /// True if the turbo is venting/blowing off due to sudden throttle closure.
    /// </summary>
    [HideInInspector] public bool turboBlowOut = false;

    /// <summary>
    /// Additional multiplier applied to the engine torque (e.g., from nitrous).
    /// </summary>
    private float multiplier = 1f;

    /// <summary>
    /// Engine friction factor. Higher values cause RPM to drop faster when throttle is released.
    /// </summary>
    [Range(0f, 1f)] public float engineFriction = .2f;

    /// <summary>
    /// Engine inertia factor. Lower values let the engine rev up/down more quickly.
    /// </summary>
    [Range(.01f, .5f)] public float engineInertia = .15f;

    private float sensitiveEngineInertia = .15f;

    /// <summary>
    /// Engine fuel input (0-1). Combined from player's throttle plus idle compensation.
    /// </summary>
    public float fuelInput = 0f;

    /// <summary>
    /// Idle compensation input, raised when RPM falls near or below minEngineRPM.
    /// </summary>
    public float idleInput = 0f;

    /// <summary>
    /// Maximum torque in Nm (the peak if the NMCurve is at 1).
    /// </summary>
    [Min(0f)] public float maximumTorqueAsNM = 300;

    /// <summary>
    /// Current produced torque in Nm.
    /// </summary>
    public float producedTorqueAsNM = 0f;

    /// <summary>
    /// Events for torque output, using a custom class.
    /// </summary>
    public RCCP_Event_Output outputEvent = new RCCP_Event_Output();
    public RCCP_Output output = new RCCP_Output();

    private void Update() {

        Inputs();

    }

    private void FixedUpdate() {

        RPM();
        TurboCharger();
        GenerateKW();
        FeedbackKW();
        Output();

    }

    /// <summary>
    /// Starts the engine if it’s currently off. Plays a delay, then sets engineRunning to true.
    /// </summary>
    public void StartEngine() {

        if (engineRunning || engineStarting)
            return;

        StartCoroutine(StartEngineDelayed());

    }

    /// <summary>
    /// Immediately stops the engine, setting engineRunning to false.
    /// </summary>
    public void StopEngine() {

        engineRunning = false;

    }

    /// <summary>
    /// Coroutine for engine start delay, simulating a brief ignition sequence.
    /// </summary>
    private IEnumerator StartEngineDelayed() {

        engineRunning = false;
        engineStarting = true;
        yield return new WaitForSeconds(1);
        engineStarting = false;
        engineRunning = true;

    }

    /// <summary>
    /// Calculates idleInput, fuelInput, and applies rev-limiter logic.
    /// </summary>
    private void Inputs() {

        if (overrideEngineRPM)
            return;

        // Raise idleInput if RPM is below (minEngineRPM + ~30% buffer).
        if (engineRPM <= minEngineRPM + (minEngineRPM / 10f))
            idleInput = 1f - Mathf.InverseLerp(minEngineRPM - (minEngineRPM / 10f), minEngineRPM + (minEngineRPM / 10f), wantedEngineRPMRaw);
        else
            idleInput = 0f;

        // Combine throttle with idle compensation.
        fuelInput = CarController.throttleInput_P + idleInput;
        fuelInput = Mathf.Clamp01(fuelInput);

        // If forcibly cutting fuel (e.g., rev-limiter or external reason).
        if (cutFuel)
            fuelInput = 0f;

        // If the engine is turned off, no fuel and no idle input.
        if (!engineRunning) {

            fuelInput = 0f;
            idleInput = 0f;

        }

    }

    /// <summary>
    /// Computes the current engine RPM, factoring in clutch engagement, wheel feedback, acceleration, and deceleration.
    /// </summary>
    private void RPM() {

        if (overrideEngineRPM)
            return;

        // Smoothly vary the engine inertia depending on throttle usage.
        sensitiveEngineInertia = Mathf.Lerp(sensitiveEngineInertia,
            Mathf.Lerp(engineInertia * 2f, engineInertia * .25f, fuelInput),
            Time.fixedDeltaTime * 2f);

        // 1) Free acceleration if clutch is in or gearbox is in neutral (CarController.gearInput_V is 0 => gear is out).
        wantedEngineRPMRaw += Mathf.Clamp01(CarController.clutchInput_V + (1f - CarController.gearInput_V))
                              * (fuelInput * maxEngineRPM)
                              * engineAccelerationRate
                              * Time.fixedDeltaTime;

        // 2) Engine speed moves toward wheel-driven RPM when clutch is engaged (1f - CarController.clutchInput_V).
        wantedEngineRPMRaw += (1f - CarController.clutchInput_V)
                              * CarController.gearInput_V
                              * (CarController.tractionWheelRPM2EngineRPM - wantedEngineRPMRaw)
                              * Time.fixedDeltaTime
                              * engineCouplingToWheelsRate
                              * 2f;

        // 3) Subtract deceleration factor. The higher the engineDecelerationRate, the quicker RPM falls under partial or zero throttle.
        wantedEngineRPMRaw -= engineDecelerationRate
                              * wantedEngineRPMRaw
                              * Time.fixedDeltaTime
                              * CarController.clutchInput_V;

        if (!engineRunning) {

            wantedEngineRPMRaw -= Mathf.Clamp01(CarController.clutchInput_V + (1f - CarController.gearInput_V))
                * engineDecelerationRate
                      * wantedEngineRPMRaw
                      * Time.fixedDeltaTime
                      * 5f;

        }

        //  Additionally, checking the exceeded engine rpm and applying the negative wheel feedback force.
        CheckEngineRPMForNegativeFeedback();

        // 4) Clamp final raw target RPM.
        wantedEngineRPMRaw = Mathf.Clamp(wantedEngineRPMRaw, 0f, maxEngineRPM + 100f);

        // 5) SmoothDamp final engine RPM for stability.
        engineRPM = Mathf.SmoothDamp(engineRPM, wantedEngineRPMRaw, ref engineVelocity, sensitiveEngineInertia);

        // Simple rev limiter logic:
        if (engineRevLimiter) {

            if (engineRPM >= maxEngineRPM)
                cutFuel = true;
            else if (engineRPM < maxEngineRPM * .995f)
                cutFuel = false;

        } else {

            cutFuel = false;

        }

    }

    /// <summary>
    /// Manages turbocharging based on engine RPM, throttle input, and fuel usage.
    /// </summary>
    private void TurboCharger() {

        // If engine or turbo is off, reset PSI to 0.
        if (!engineRunning || !turboCharged) {

            turboChargePsi = 0f;
            turboBlowOut = false;
            return;

        }

        // Basic factor from 0..1, climbing at higher RPM.
        float factor = Mathf.Clamp01(Mathf.Lerp(-1f, 1f, engineRPM / maxEngineRPM));

        // Calculate final PSI from throttle input and factor.
        turboChargePsi = Mathf.Lerp(0f, maxTurboChargePsi, fuelInput * factor);

        // Check if we quickly closed throttle, causing blow-off.
        if (fuelInput == 0 && turboChargePsi < turboChargePsi_Old)
            turboBlowOut = true;
        else
            turboBlowOut = false;

        turboChargePsi_Old = turboChargePsi;

    }

    /// <summary>
    /// Overrides the engine RPM to a specified value, bypassing internal calculations.
    /// </summary>
    /// <param name="targetRPM">Engine RPM to set.</param>
    public void OverrideRPM(float targetRPM) {

        engineRPM = targetRPM;

    }

    /// <summary>
    /// Generates torque (in Nm) based on the NMCurve and engine RPM, applying turbo multiplier if turboCharged.
    /// </summary>
    private void GenerateKW() {

        // Evaluate normalized curve at current engineRPM, then multiply by max torque and fuel input.
        producedTorqueAsNM = NMCurve.Evaluate(engineRPM) * maximumTorqueAsNM * fuelInput;

        producedTorqueAsNM *= multiplier;

        if (turboCharged)
            producedTorqueAsNM *= Mathf.Lerp(1f, turboChargerCoEfficient, turboChargePsi / maxTurboChargePsi);

        // Reset multiplier each frame unless changed externally (e.g., NOS).
        multiplier = 1f;

    }

    /// <summary>
    /// Reduces the engine torque if the wheel RPM is much greater than the engine RPM, simulating load feedback from the wheels.
    /// </summary>
    public void FeedbackKW() {

        producedTorqueAsNM *= Mathf.Lerp(1f, 0f, (CarController.tractionWheelRPM2EngineRPM - engineRPM) / (maxEngineRPM / 2f));

    }

    /// <summary>
    /// Multiplies engine torque with a certain factor (e.g., NOS). Resets to 1f after each frame in GenerateKW().
    /// </summary>
    /// <param name="multiplier">Factor to multiply torque by.</param>
    public void Multiply(float multiplier) {

        this.multiplier = multiplier;

    }

    /// <summary>
    /// Additionally, checking the exceeded engine rpm and applying the negative wheel feedback force.
    /// </summary>
    public void CheckEngineRPMForNegativeFeedback() {

        if (wantedEngineRPMRaw > maxEngineRPM) {

            float diff = wantedEngineRPMRaw - maxEngineRPM;
            float maxBrakeForceForNegativeFeedback = 4000f;
            float forceForNegativeFeedback = Mathf.InverseLerp(0f, 400f, diff) * maxBrakeForceForNegativeFeedback;
            forceForNegativeFeedback *= (1f - CarController.clutchInput_V) * CarController.gearInput_V;

            if (CarController.PoweredAxles != null && CarController.PoweredAxles.Count > 0) {

                for (int i = 0; i < CarController.PoweredAxles.Count; i++) {

                    RCCP_Axle axle = CarController.PoweredAxles[i];

                    if (!axle)
                        continue;

                    if (axle.leftWheelCollider && axle.leftWheelCollider.isActiveAndEnabled)
                        axle.leftWheelCollider.AddBrakeTorque(forceForNegativeFeedback / (float)CarController.PoweredAxles.Count);

                    if (axle.rightWheelCollider && axle.rightWheelCollider.isActiveAndEnabled)
                        axle.rightWheelCollider.AddBrakeTorque(forceForNegativeFeedback / (float)CarController.PoweredAxles.Count);

                }

            }

        }

    }

    /// <summary>
    /// Sends the produced torque to downstream components via the output event.
    /// </summary>
    private void Output() {

        if (output == null)
            output = new RCCP_Output();

        output.NM = producedTorqueAsNM;
        outputEvent.Invoke(output);

    }

    /// <summary>
    /// Resets state variables (e.g., no fuel cut, no turbo blowout) and initializes engineRPM based on whether engineRunning is true or false.
    /// </summary>
    public void Reload() {

        engineStarting = false;
        cutFuel = false;
        fuelInput = 0f;
        idleInput = 0f;
        turboBlowOut = false;
        turboChargePsi = 0f;
        turboChargePsi_Old = 0f;
        producedTorqueAsNM = 0f;

        if (engineRunning) {

            engineRPM = minEngineRPM;
            wantedEngineRPMRaw = engineRPM;

        } else {

            wantedEngineRPMRaw = 0f;
            engineRPM = 0f;

        }

    }

}
