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
/// Transmits power from Engine -> Clutch -> Gearbox to the axle based on differential settings. 
/// Different differential types (Open, Limited, FullLocked, Direct) affect how torque is split between the left and right wheels.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP Differential")]
public class RCCP_Differential : RCCP_Component {

    /// <summary>
    /// If true, an external script/class can override differential calculations and assign torque outputs manually.
    /// </summary>
    public bool overrideDifferential = false;

    /// <summary>
    /// Differential types:
    /// - Open: The RPM difference between both wheels decides which wheel gets more traction.
    /// - Limited: Similar to open, but slip between wheels is limited; higher LSD ratio = closer to locked.
    /// - FullLocked: Both wheels rotate at the same speed.
    /// - Direct: No slip; torque is directly split without slip calculations.
    /// </summary>
    public enum DifferentialType {
        Open,
        Limited,
        FullLocked,
        Direct
    }

    /// <summary>
    /// Selected type of differential for this vehicle.
    /// </summary>
    public DifferentialType differentialType = DifferentialType.Limited;

    /// <summary>
    /// Limited slip ratio, from 50 to 100%. A higher ratio means the differential approaches a locked state.
    /// </summary>
    [Range(50f, 100f)] public float limitedSlipRatio = 80f;

    /// <summary>
    /// Final drive ratio multiplier. Increasing this gives faster acceleration but reduces top speed.
    /// </summary>
    [Min(0.01f)] public float finalDriveRatio = 3.73f;

    /// <summary>
    /// Torque received from the upstream component (usually the gearbox).
    /// </summary>
    [Min(0f)] public float receivedTorqueAsNM = 0f;

    /// <summary>
    /// Total torque output from the differential, split between left and right wheels.
    /// </summary>
    [Min(0f)] public float producedTorqueAsNM = 0f;

    /// <summary>
    /// RPM of the left wheel.
    /// </summary>
    public float leftWheelRPM = 0f;

    /// <summary>
    /// RPM of the right wheel.
    /// </summary>
    public float rightWheelRPM = 0f;

    /// <summary>
    /// Overall slip ratio calculated between left and right wheels.
    /// </summary>
    public float wheelSlipRatio = 0f;

    /// <summary>
    /// Left wheel-specific slip ratio.
    /// </summary>
    public float leftWheelSlipRatio = 0f;

    /// <summary>
    /// Right wheel-specific slip ratio.
    /// </summary>
    public float rightWheelSlipRatio = 0f;

    /// <summary>
    /// Final torque output for the left wheel, after slip calculations.
    /// </summary>
    public float outputLeft = 0f;

    /// <summary>
    /// Final torque output for the right wheel, after slip calculations.
    /// </summary>
    public float outputRight = 0f;

    /// <summary>
    /// The axle that this differential will feed torque to. Must be assigned for correct operation.
    /// </summary>
    public RCCP_Axle connectedAxle;

    private void FixedUpdate() {

        // If overridden by external logic, skip internal differential calculations.
        if (overrideDifferential)
            return;

        // If there's no axle connected, return.
        if (!connectedAxle)
            return;

        Gears();
        Output();

    }

    /// <summary>
    /// Determines slip ratios and processes the final torque split for each wheel, based on the chosen differential type.
    /// </summary>
    private void Gears() {

        // Fetch wheel RPMs. Use absolute values, ignoring sign (direction).
        if (connectedAxle.leftWheelCollider && connectedAxle.leftWheelCollider.isActiveAndEnabled)
            leftWheelRPM = Mathf.Abs(connectedAxle.leftWheelCollider.WheelCollider.rpm);
        else
            leftWheelRPM = 0f;

        if (connectedAxle.rightWheelCollider && connectedAxle.rightWheelCollider.isActiveAndEnabled)
            rightWheelRPM = Mathf.Abs(connectedAxle.rightWheelCollider.WheelCollider.rpm);
        else
            rightWheelRPM = 0f;

        float sumRPM = leftWheelRPM + rightWheelRPM;
        float diffRPM = leftWheelRPM - rightWheelRPM;          // can be negative or positive

        // Calculate the slip ratio (difference over sum).
        // If sumRPM == 0, we'll treat wheelSlipRatio as 0, meaning no difference.
        if (sumRPM > 0f) {
            wheelSlipRatio = Mathf.InverseLerp(0f, sumRPM, Mathf.Abs(diffRPM));
        } else {
            wheelSlipRatio = 0f;
        }

        switch (differentialType) {

            case DifferentialType.Open:

                // If diffRPM == 0, no slip difference.
                if (Mathf.Approximately(diffRPM, 0f)) {
                    leftWheelSlipRatio = 0f;
                    rightWheelSlipRatio = 0f;
                }
                // If left is spinning faster:
                else if (diffRPM > 0f) {
                    leftWheelSlipRatio = wheelSlipRatio;    // left losing torque
                    rightWheelSlipRatio = -wheelSlipRatio;  // right gaining torque
                } else {  // right is spinning faster
                    leftWheelSlipRatio = -wheelSlipRatio;
                    rightWheelSlipRatio = wheelSlipRatio;
                }

                break;

            case DifferentialType.Limited:

                // LSD uses the limited slip ratio to reduce some of the difference.
                // E.g. LSD=80 => wheelSlipRatio *= (1 - 0.80) = 0.20 of original difference remains
                float scaledSlip = wheelSlipRatio * Mathf.Lerp(1f, 0f, (limitedSlipRatio / 100f));

                if (Mathf.Approximately(diffRPM, 0f)) {
                    leftWheelSlipRatio = 0f;
                    rightWheelSlipRatio = 0f;
                } else if (diffRPM > 0f) {
                    leftWheelSlipRatio = scaledSlip;
                    rightWheelSlipRatio = -scaledSlip;
                } else {
                    leftWheelSlipRatio = -scaledSlip;
                    rightWheelSlipRatio = scaledSlip;
                }

                break;

            case DifferentialType.FullLocked:

                // In a fully locked diff, both wheels are forced to rotate at the same speed (ideal spool).
                // Hence, we apply zero slip ratio for both.
                leftWheelSlipRatio = 0f;
                rightWheelSlipRatio = 0f;

                break;

            case DifferentialType.Direct:

                // "Direct" means no slip compensation; no difference is removed from either wheel. 
                // But effectively, that is also no slip ratio from the perspective of torque distribution.
                leftWheelSlipRatio = 0f;
                rightWheelSlipRatio = 0f;

                break;

        }

    }

    /// <summary>
    /// Allows external scripts to override the torque outputs for left and right wheels, skipping this component's internal logic.
    /// </summary>
    /// <param name="targetOutputLeft">Desired torque output for left wheel.</param>
    /// <param name="targetOutputRight">Desired torque output for right wheel.</param>
    public void OverrideDifferential(float targetOutputLeft, float targetOutputRight) {

        outputLeft = targetOutputLeft;
        outputRight = targetOutputRight;
        producedTorqueAsNM = outputLeft + outputRight;

        connectedAxle.isPower = true;
        connectedAxle.ReceiveOutput(targetOutputLeft, targetOutputRight);

    }

    /// <summary>
    /// Receives upstream torque (e.g., from the gearbox). If not overridden, the differential logic will process it in Output().
    /// </summary>
    /// <param name="output"></param>
    public void ReceiveOutput(RCCP_Output output) {

        if (overrideDifferential)
            return;

        receivedTorqueAsNM = output.NM;

    }

    /// <summary>
    /// Splits the received torque (based on finalDriveRatio and slip ratios) across the left and right wheels.
    /// </summary>
    private void Output() {

        // Multiply by finalDriveRatio for the final torque value.
        producedTorqueAsNM = receivedTorqueAsNM * finalDriveRatio;

        // Start by splitting the torque evenly.
        outputLeft = producedTorqueAsNM * 0.5f;
        outputRight = producedTorqueAsNM * 0.5f;

        // Then adjust each side by its slip ratio. 
        // If slip ratio is positive, we subtract some torque from that wheel 
        // and add it to the other wheel (conceptually).
        // But here we simply subtract from each side based on slip, 
        // since slip is negative on the "gain" side.
        outputLeft -= producedTorqueAsNM * leftWheelSlipRatio;
        outputRight -= producedTorqueAsNM * rightWheelSlipRatio;

        // Send calculated torque outputs to the axle.
        connectedAxle.isPower = true;
        connectedAxle.ReceiveOutput(outputLeft, outputRight);

    }

    /// <summary>
    /// Resets internal states for the differential, handy if reloading or resetting the vehicle.
    /// </summary>
    public void Reload() {

        leftWheelRPM = 0f;
        rightWheelRPM = 0f;
        wheelSlipRatio = 0f;
        leftWheelSlipRatio = 0f;
        rightWheelSlipRatio = 0f;
        outputLeft = 0f;
        outputRight = 0f;
        receivedTorqueAsNM = 0f;
        producedTorqueAsNM = 0f;

    }

}
