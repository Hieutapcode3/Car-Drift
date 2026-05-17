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
/// Connector between engine and the gearbox. Transmits the received power from the engine to the gearbox based on clutch input.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP Clutch")]
public class RCCP_Clutch : RCCP_Component {

    /// <summary>
    /// If true, overrides all calculations and uses a custom clutch input set externally.
    /// </summary>
    public bool overrideClutch = false;

    /// <summary>
    /// Current clutch input, clamped between 0 and 1. 
    /// - 0 means fully engaged clutch (no slip), 
    /// - 1 means clutch is fully pressed (full slip).
    /// </summary>
    [Range(0f, 1f)] public float clutchInput = 1f;

    /// <summary>
    /// Raw input used to gradually reach the actual clutch input. 
    /// Useful for smoothing transitions when using an automatic clutch.
    /// </summary>
    [Range(0f, 1f)] public float clutchInputRaw = 1f;

    /// <summary>
    /// How quickly the clutch input changes (higher inertia = slower changes). Only used when automaticClutch is true.
    /// </summary>
    [Range(0f, 1f)] public float clutchInertia = .125f;

    /// <summary>
    /// If true, clutch input is automatically calculated. If false, clutchInput_P from the player is used directly.
    /// </summary>
    public bool automaticClutch = true;

    /// <summary>
    /// If true, forces the clutch input to 1 (fully pressed).
    /// </summary>
    public bool forceToPressClutch = false;

    /// <summary>
    /// If true, clutch input is forced to 1 (fully pressed) while shifting gears.
    /// </summary>
    public bool pressClutchWhileShiftingGears = true;

    /// <summary>
    /// If true, clutch input is forced to 1 (fully pressed) while the handbrake is applied.
    /// </summary>
    public bool pressClutchWhileHandbraking = true;

    /// <summary>
    /// If engine RPM falls below this value, the clutch input will be increased to avoid stalling (when automaticClutch is true).
    /// </summary>
    [Min(0f)] public float engageRPM = 1000f;

    /// <summary>
    /// Torque received from the previous component (usually the engine).
    /// </summary>
    public float receivedTorqueAsNM = 0f;

    /// <summary>
    /// Torque delivered to the next component (usually the gearbox).
    /// </summary>
    public float producedTorqueAsNM = 0f;

    /// <summary>
    /// Output event with a custom output class, holding the torque.
    /// </summary>
    public RCCP_Event_Output outputEvent = new RCCP_Event_Output();
    public RCCP_Output output = new RCCP_Output();

    float velocity;

    private void FixedUpdate() {

        // Calculating clutch input based on engine RPM, speed, etc. (or player input if automaticClutch is false).
        Input();

        // Delivering torque to the gearbox or other connected component.
        Output();

    }

    /// <summary>
    /// Calculates the clutch input based on current settings.
    /// </summary>
    private void Input() {

        // If overrideClutch is true, the input is set externally; skip internal calculations.
        if (overrideClutch)
            return;

        // Automatic clutch logic.
        if (automaticClutch) {

            if (CarController.engineRPM > engageRPM) {

                if (CarController.throttleInput_V >= .1f)
                    clutchInputRaw = 0f;
                else if (CarController.absoluteSpeed > 10f)
                    clutchInputRaw = 0f;
                else
                    clutchInputRaw = 1f;

            } else {

                clutchInputRaw = 1f;

                if (CarController.engineRPM < (engageRPM * 1.2f))
                    clutchInputRaw = 1f;

            }

            // Clamp the raw input between 0 and 1.
            clutchInputRaw = Mathf.Clamp01(clutchInputRaw);

            // Force clutch if gearbox is shifting.
            if (pressClutchWhileShiftingGears && CarController.shiftingNow)
                clutchInputRaw = 1f;

            // Force clutch if handbrake is applied.
            if (pressClutchWhileHandbraking && CarController.handbrakeInput_V >= .75f)
                clutchInputRaw = 1f;

            // Smooth out the clutch input changes using SmoothDamp.
            clutchInput = Mathf.SmoothDamp(clutchInput, clutchInputRaw, ref velocity, clutchInertia * .5f);

            // Handle small deadzone to snap to 0 or 1 if the input is near extremes.
            if (clutchInputRaw > .95f && clutchInput > .95f)
                clutchInput = 1f;

            if (clutchInputRaw < .05f && clutchInput < .05f)
                clutchInput = 0f;

        } else {

            // If automatic clutch is disabled, get the clutch input directly from the player's input.
            clutchInput = CarController.clutchInput_P;

        }

        // If forced, override the clutch input to 1 (fully pressed).
        if (forceToPressClutch)
            clutchInput = 1f;

        // Ensure final clutch input is within [0,1].
        clutchInput = Mathf.Clamp01(clutchInput);

    }

    /// <summary>
    /// Overrides the internally calculated clutch input with a specific value.
    /// </summary>
    /// <param name="targetInput">Value between 0 and 1 (0 = fully engaged, 1 = fully pressed)</param>
    public void OverrideInput(float targetInput) {

        clutchInput = targetInput;
        clutchInputRaw = targetInput;

    }

    /// <summary>
    /// Called by the previous component in the drivetrain to deliver torque into this clutch.
    /// </summary>
    /// <param name="output"></param>
    public void ReceiveOutput(RCCP_Output output) {

        receivedTorqueAsNM = output.NM;

    }

    /// <summary>
    /// Outputs the torque after applying the clutch slip factor.
    /// </summary>
    private void Output() {

        if (output == null)
            output = new RCCP_Output();

        // If clutch is fully pressed (1), torque is near 0. If not pressed (0), full torque is passed through.
        producedTorqueAsNM = receivedTorqueAsNM * (1f - clutchInput);

        output.NM = producedTorqueAsNM;
        outputEvent.Invoke(output);

    }

    /// <summary>
    /// Resets essential clutch variables to their default states.
    /// </summary>
    public void Reload() {

        clutchInput = 1f;
        clutchInputRaw = 1f;
        receivedTorqueAsNM = 0f;
        producedTorqueAsNM = 0f;

    }

}
