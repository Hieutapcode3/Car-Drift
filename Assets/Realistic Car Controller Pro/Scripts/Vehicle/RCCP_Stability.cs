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
/// Manages various stability systems for the vehicle:
/// - ABS (Anti-skid Braking System),
/// - ESP (Electronic Stability Program),
/// - TCS (Traction Control System),
/// plus steering, traction, and angular-drag helpers.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Stability")]
public class RCCP_Stability : RCCP_Component {

    public RCCP_Axles AxleManager;
    public RCCP_Axle frontAxle;
    public RCCP_Axle rearAxle;

    /// <summary>
    /// Collection of axles that provide power to wheels.
    /// </summary>
    public List<RCCP_Axle> poweredAxles = new List<RCCP_Axle>();

    /// <summary>
    /// Collection of axles used for steering.
    /// </summary>
    public List<RCCP_Axle> steeringAxles = new List<RCCP_Axle>();

    /// <summary>
    /// Collection of axles used for braking.
    /// </summary>
    public List<RCCP_Axle> brakedAxles = new List<RCCP_Axle>();

    /// <summary>
    /// Enable / disable ABS.
    /// </summary>
    public bool ABS = true;

    /// <summary>
    /// Enable / disable ESP.
    /// </summary>
    public bool ESP = true;

    /// <summary>
    /// Enable / disable TCS.
    /// </summary>
    public bool TCS = true;

    /// <summary>
    /// ABS threshold. If slip * brakeInput exceeds this, ABS will engage to reduce brake torque.
    /// </summary>
    [Range(.01f, .5f)] public float engageABSThreshold = .35f;

    /// <summary>
    /// ESP threshold. If sideways slip exceeds this, ESP will engage by applying brakes to stabilize the vehicle.
    /// </summary>
    [Range(.01f, .5f)] public float engageESPThreshold = .5f;

    /// <summary>
    /// TCS threshold. If forward slip on powered wheels exceeds this, TCS will engage to reduce motor torque.
    /// </summary>
    [Range(.01f, .5f)] public float engageTCSThreshold = .35f;

    /// <summary>
    /// How strongly ABS reduces brake torque.
    /// </summary>
    [Range(0f, 1f)] public float ABSIntensity = 1f;

    /// <summary>
    /// How strongly ESP brakes wheels to correct over/under steering.
    /// </summary>
    [Range(0f, 1f)] public float ESPIntensity = .75f;

    /// <summary>
    /// How strongly TCS reduces torque to wheels if slipping.
    /// </summary>
    [Range(0f, 1f)] public float TCSIntensity = 1f;

    /// <summary>
    /// True if ABS is currently engaged on at least one wheel.
    /// </summary>
    public bool ABSEngaged = false;

    /// <summary>
    /// True if ESP is currently engaged to help stabilize the vehicle.
    /// </summary>
    public bool ESPEngaged = false;

    /// <summary>
    /// True if TCS is currently engaged to reduce excessive wheel slip under power.
    /// </summary>
    public bool TCSEngaged = false;

    /// <summary>
    /// If true, adds a small force to reduce oversteer or understeer (steering helper).
    /// </summary>
    public bool steeringHelper = true;

    /// <summary>
    /// If true, reduces front tire stiffness if the vehicle is skidding significantly (traction helper).
    /// </summary>
    public bool tractionHelper = true;

    /// <summary>
    /// If true, increases angular drag as speed increases (angular drag helper).
    /// </summary>
    public bool angularDragHelper = false;

    /// <summary>
    /// If true, limits maximum drift angle. On extreme angles, angular velocity is damped.
    /// </summary>
    public bool driftAngleLimiter = false;

    /// <summary>
    /// Max allowed drift angle in degrees before it’s partially corrected.
    /// </summary>
    [Range(0f, 90f)] public float maxDriftAngle = 35f;

    /// <summary>
    /// How quickly the angular velocity is damped if the drift angle exceeds maxDriftAngle.
    /// </summary>
    [Range(0f, 10f)] public float driftAngleCorrectionFactor = 5f;

    /// <summary>
    /// Strength factor for steeringHelper.
    /// </summary>
    [Range(0f, 1f)] public float steerHelperStrength = .025f;

    /// <summary>
    /// Strength factor for tractionHelper.
    /// </summary>
    [Range(0f, 1f)] public float tractionHelperStrength = .05f;

    /// <summary>
    /// Strength factor for angularDragHelper.
    /// </summary>
    [Range(0f, 1f)] public float angularDragHelperStrength = .075f;

    public float groundedFactor = 0f;
    private bool dampingAngularVelocity = false;

    public override void Start() {

        AxleManager = CarController.AxleManager;
        frontAxle = CarController.FrontAxle;
        rearAxle = CarController.RearAxle;
        poweredAxles = CarController.PoweredAxles;
        steeringAxles = CarController.SteeredAxles;
        brakedAxles = CarController.BrakedAxles;

    }

    private void FixedUpdate() {

        if (CarController.IsGrounded)
            groundedFactor += Time.deltaTime * .5f;
        else
            groundedFactor = 0f;

        if (CarController.handbrakeInput_V > .5f)
            groundedFactor = .35f;

        groundedFactor = Mathf.Clamp01(groundedFactor);

        if (ESP)
            UpdateESP();

        if (TCS)
            UpdateTCS();

        if (ABS)
            UpdateABS();

        if (steeringHelper)
            SteerHelper();

        if (tractionHelper)
            TractionHelper();

        if (angularDragHelper)
            AngularDragHelper();

        if (driftAngleLimiter)
            LimitDriftAngle();

        if (RCCPSettings.SelectedBehaviorType != null && RCCPSettings.SelectedBehaviorType.driftMode)
            Drift();

    }

    /// <summary>
    /// Additional drift behavior if the behavior type is set to driftMode. 
    /// Adds forces for drifting effects.
    /// </summary>
    private void Drift() {

        float rearWheelSlipAmountForward = 0f;
        float rearWheelSlipAmountSideways = 0f;

        // 1. Get average slip on rear wheels (forward & sideways).
        //    We'll then square these slips (pSlip) below to amplify higher slip values.
        if (rearAxle) {

            rearWheelSlipAmountForward =
                (rearAxle.leftWheelCollider.wheelSlipAmountForward
               + rearAxle.rightWheelCollider.wheelSlipAmountForward) * 0.5f;

            rearWheelSlipAmountSideways =
                (rearAxle.leftWheelCollider.wheelSlipAmountSideways
               + rearAxle.rightWheelCollider.wheelSlipAmountSideways) * 0.5f;

        }

        // 2. "pSlip" = slip^2 * sign(slip) => This scales small slips down, 
        //    but big slips up, preserving the original sign (direction).
        float pRearWheelSlipAmountForward =
            (rearWheelSlipAmountForward * rearWheelSlipAmountForward)
            * Mathf.Sign(rearWheelSlipAmountForward);

        float pRearWheelSlipAmountSideways =
            (rearWheelSlipAmountSideways * rearWheelSlipAmountSideways)
            * Mathf.Sign(rearWheelSlipAmountSideways);

        // 3. Determine where we'll apply forces:
        //    If there's a special center of mass (COM) transform (from AeroDynamics), use it.
        Transform comTransform = transform;
        RCCP_AeroDynamics aeroDynamics = CarController.AeroDynamics;

        if (aeroDynamics && aeroDynamics.COM != null)
            comTransform = aeroDynamics.COM;

        // 4. Add a small torque around the car's local Y-axis to enhance drift. 
        //    'CarController.direction' is presumably +1 for forward, -1 for reverse.
        //    'CarController.steerInput_P' is your steering input. 
        CarController.Rigid.AddRelativeTorque(
            Vector3.up
            * CarController.steerInput_P
            * CarController.throttleInput_P
            * CarController.direction
            * 1f
            * groundedFactor,
            ForceMode.Acceleration
        );

        // 5a. Add a forward force (in world space) to exaggerate drifting:
        //    - Scaled by 3500f * Abs(sidewaysSlip^2) 
        //    - Also scaled by clamp01( Abs(forwardSlip^2 * 10f) ), 
        //      meaning the forward slip must be significant for max effect.
        //    - 'CarController.direction' flips this force if in reverse.
        CarController.Rigid.AddForceAtPosition(
            transform.forward
            * 2500f
            * Mathf.Abs(pRearWheelSlipAmountSideways)
            * Mathf.Clamp01(Mathf.Abs(pRearWheelSlipAmountForward * 4f))
            * CarController.direction
            * groundedFactor,
            comTransform.position,
            ForceMode.Force
        );

        // 5b. Add a sideways force (in world space) to push the car laterally:
        //    - Scaled by 1000f * pRearWheelSlipAmountSideways
        //    - Also scaled by clamp01(Abs( clamp(forwardSlip, 0.5f, 1f) * 10f ))
        //      -> ensures some forward slip is needed and it's at least 0.5f
        //    - 'CarController.direction' flips if reverse.
        CarController.Rigid.AddForceAtPosition(
            transform.right
            * 1500f
            * pRearWheelSlipAmountSideways
            * Mathf.Clamp01(Mathf.Abs(Mathf.Clamp(pRearWheelSlipAmountForward, 0f, 1f) * 4f))
            * CarController.direction
            * groundedFactor,
            comTransform.position,
            ForceMode.Force
        );

    }

    /// <summary>
    /// Manages the ABS logic, reducing brake torque if a braked wheel is slipping above engageABSThreshold.
    /// </summary>
    private void UpdateABS() {

        ABSEngaged = false;

        if (AxleManager == null)
            return;

        if (brakedAxles == null || brakedAxles.Count < 1)
            return;

        for (int i = 0; i < brakedAxles.Count; i++) {

            if ((Mathf.Abs(brakedAxles[i].leftWheelCollider.wheelSlipAmountForward) * brakedAxles[i].brakeInput) >= engageABSThreshold) {

                brakedAxles[i].leftWheelCollider.CutBrakeABS(ABSIntensity);
                ABSEngaged = true;

            }

            if ((Mathf.Abs(brakedAxles[i].rightWheelCollider.wheelSlipAmountForward) * brakedAxles[i].brakeInput) >= engageABSThreshold) {

                brakedAxles[i].rightWheelCollider.CutBrakeABS(ABSIntensity);
                ABSEngaged = true;

            }

        }

    }

    /// <summary>
    /// Manages ESP logic, detecting oversteer and understeer by comparing 
    /// front/rear sideways slip. Applies brake torque on specific wheels 
    /// to stabilize the vehicle.
    /// </summary>
    private void UpdateESP() {

        ESPEngaged = false;

        // Early out if front or rear axle is missing
        if (!frontAxle || !rearAxle)
            return;

        // Sum the sideways slip for each axle
        float frontSlip = frontAxle.leftWheelCollider.wheelSlipAmountSideways
                        + frontAxle.rightWheelCollider.wheelSlipAmountSideways;
        float rearSlip = rearAxle.leftWheelCollider.wheelSlipAmountSideways
                       + rearAxle.rightWheelCollider.wheelSlipAmountSideways;

        // Check if slips exceed your threshold
        bool underSteering = Mathf.Abs(frontSlip) >= engageESPThreshold;
        bool overSteering = Mathf.Abs(rearSlip) >= engageESPThreshold;

        // If either condition is met, ESP is engaged
        if (underSteering || overSteering)
            ESPEngaged = true;

        // -----------------------------------------------------------
        // 1. Understeer Correction
        // If front wheels are skidding (underSteering), 
        // brake front wheels proportionally to frontSlip sign.
        // -----------------------------------------------------------
        if (underSteering && frontAxle.isBrake) {

            // If frontSlip is negative => we apply torque to the left wheel
            //   via Mathf.Clamp(-frontSlip, 0f, ∞)
            // If frontSlip is positive => we apply torque to the right wheel
            //   via Mathf.Clamp(frontSlip, 0f, ∞)
            // Adjust the 0.1f scaling and ESPIntensity to your preference.
            frontAxle.leftWheelCollider.AddBrakeTorque(
                frontAxle.maxBrakeTorque * (ESPIntensity * 0.1f)
                * Mathf.Clamp(-frontSlip, 0f, Mathf.Infinity)
            );

            frontAxle.rightWheelCollider.AddBrakeTorque(
                frontAxle.maxBrakeTorque * (ESPIntensity * 0.1f)
                * Mathf.Clamp(frontSlip, 0f, Mathf.Infinity)
            );

        }

        // -----------------------------------------------------------
        // 2. Oversteer Correction
        // If rear wheels are skidding (overSteering),
        // brake rear wheels proportionally to rearSlip sign.
        // -----------------------------------------------------------
        if (overSteering && rearAxle.isBrake) {

            rearAxle.leftWheelCollider.AddBrakeTorque(
                rearAxle.maxBrakeTorque * (ESPIntensity * 0.2f)
                * Mathf.Clamp(-rearSlip, 0f, Mathf.Infinity)
            );

            rearAxle.rightWheelCollider.AddBrakeTorque(
                rearAxle.maxBrakeTorque * (ESPIntensity * 0.2f)
                * Mathf.Clamp(rearSlip, 0f, Mathf.Infinity)
            );

        }

    }

    /// <summary>
    /// Manages TCS logic, reducing motor torque if the powered wheels 
    /// are slipping beyond engageTCSThreshold (in forward or reverse).
    /// </summary>
    private void UpdateTCS() {

        TCSEngaged = false;

        if (poweredAxles == null || poweredAxles.Count < 1)
            return;

        // If the vehicle isn't moving forward or backward (direction == 0),
        // we can skip TCS. Or handle differently if desired.
        if (CarController.direction == 0)
            return;

        // For each powered axle, check forward slip. If it exceeds threshold,
        // and the sign of slip matches the car's direction, reduce torque.
        for (int i = 0; i < poweredAxles.Count; i++) {

            // Left wheel
            float leftSlip = poweredAxles[i].leftWheelCollider.wheelSlipAmountForward;

            if (Mathf.Abs(leftSlip) >= engageTCSThreshold && Mathf.Sign(leftSlip) == CarController.direction) {

                poweredAxles[i].leftWheelCollider.CutTractionTCS(TCSIntensity);
                TCSEngaged = true;

            }

            // Right wheel
            float rightSlip = poweredAxles[i].rightWheelCollider.wheelSlipAmountForward;

            if (Mathf.Abs(rightSlip) >= engageTCSThreshold && Mathf.Sign(rightSlip) == CarController.direction) {

                poweredAxles[i].rightWheelCollider.CutTractionTCS(TCSIntensity);
                TCSEngaged = true;

            }

        }

    }


    /// <summary>
    /// Steering helper. Applies small forces to help align velocity and steering direction. 
    /// Helps correct minor over/understeer at moderate speeds.
    /// </summary>
    private void SteerHelper() {

        // ---------------------------------------------------------------------
        // 1. Basic Conditions to Run SteerHelper
        // ---------------------------------------------------------------------
        if (!CarController.IsGrounded)
            return;                 // Don't apply steer help if car is airborne.

        if (CarController.Rigid.isKinematic)
            return;                 // Skip if the Rigidbody is not being simulated normally.

        // ---------------------------------------------------------------------
        // 2. Compute a "corrected" steerHelperStrength
        //    This is an arbitrary scaling you can tweak for game feel.
        // ---------------------------------------------------------------------
        float correctedSteerHelper = steerHelperStrength * 0.25f * groundedFactor;

        // ---------------------------------------------------------------------
        // 3. Convert current velocity from world space -> local space
        //    localVelocity.x = sideways velocity
        //    localVelocity.z = forward/back velocity (assuming Z is forward).
        // ---------------------------------------------------------------------
        Vector3 localVelocity = transform.InverseTransformDirection(CarController.Rigid.linearVelocity);

        // ---------------------------------------------------------------------
        // 4. Reduce the sideways velocity by some fraction
        //    This "pulls" the velocity vector more in line with forward direction.
        // ---------------------------------------------------------------------
        localVelocity.x *= (1f - correctedSteerHelper);

        // ---------------------------------------------------------------------
        // 5. Convert the localVelocity back to world space
        //    and apply it directly to the Rigidbody.
        // ---------------------------------------------------------------------
        CarController.Rigid.linearVelocity = Vector3.Lerp(CarController.Rigid.linearVelocity, transform.TransformDirection(localVelocity), groundedFactor);

        // ---------------------------------------------------------------------
        // 6. Apply a small lateral force to reduce lateral velocity
        //    and to assist steering in the direction of the wheels.
        // ---------------------------------------------------------------------

        // a) Push against existing side velocity:
        //    -transform.right * localVelocity.x tries to cancel out leftover lateral motion.
        CarController.Rigid.AddForce(
            -transform.right * localVelocity.x * correctedSteerHelper * .25f * groundedFactor,
            ForceMode.VelocityChange
        );

        // b) Nudge the velocity toward the steering direction:
        //    transform.right * steerAngle portion to encourage turning in the direction of the wheels.
        //    InverseLerp(10f, 60f, absoluteSpeed) scales the effect up as speed goes from 10 to 60.
        CarController.Rigid.AddForce(
            transform.right
            * correctedSteerHelper
            * (CarController.steerAngle / 40f)
            * Mathf.InverseLerp(10f, 60f, CarController.absoluteSpeed)
            * 1f  // (This *1f is redundant, but doesn’t hurt.)
            * groundedFactor,
            ForceMode.VelocityChange
        );

        // ---------------------------------------------------------------------
        // 7. Determine whether the car is moving forward or backward 
        //    so we can invert some yaw corrections if in reverse.
        // ---------------------------------------------------------------------
        int direction = localVelocity.z < 0 ? -1 : 1;

        // ---------------------------------------------------------------------
        // 8. Tweak the car's angular velocity
        //    We scale down the current yaw rate (angularVelocity.y),
        //    then reassign it to the Rigidbody.
        // ---------------------------------------------------------------------
        Vector3 angularVelocity = CarController.Rigid.angularVelocity;
        angularVelocity.y *= (1f - (correctedSteerHelper * 0.1f));
        CarController.Rigid.angularVelocity = Vector3.Lerp(CarController.Rigid.angularVelocity, angularVelocity, groundedFactor);

        // ---------------------------------------------------------------------
        // 9. Add small torques around Y to further stabilize or enhance steering.
        //    a) Damp existing yaw: 
        //       Multiply angularVelocity.y by correctedSteerHelper, then invert if car is in reverse.
        // ---------------------------------------------------------------------
        CarController.Rigid.AddRelativeTorque(
            Vector3.up * angularVelocity.y * correctedSteerHelper * 0.125f * -direction * groundedFactor,
            ForceMode.VelocityChange
        );

        //    b) Add torque to align with steering angle:
        //       This helps the car yaw in the direction of the steering, scaled by speed and direction.
        CarController.Rigid.AddRelativeTorque(
            Vector3.up
            * correctedSteerHelper
            * (CarController.steerAngle / 40f)
            * Mathf.InverseLerp(0f, 80f, CarController.absoluteSpeed)
            * 0.75f
            * direction
            * groundedFactor,
            ForceMode.VelocityChange
        );

    }

    /// <summary>
    /// Traction helper. Reduces front axle lateral stiffness if the vehicle’s 
    /// lateral slip or angular velocity is high, preventing spins.
    /// </summary>
    private void TractionHelper() {

        // 1. Basic checks
        if (!CarController.IsGrounded)
            return;                 // Don't apply traction help if car is airborne.

        if (CarController.Rigid.isKinematic)
            return;                 // Skip if the Rigidbody is not being simulated normally.

        if (!frontAxle)
            return;

        // 2. Grab the car's velocity and remove any vertical component
        Vector3 velocity = CarController.Rigid.linearVelocity;
        velocity -= transform.up * Vector3.Dot(velocity, transform.up);

        // Optional: Early out if velocity is nearly zero to avoid undefined directions
        if (velocity.sqrMagnitude < 0.0001f) {

            frontAxle.tractionHelpedSidewaysStiffness = 1f;
            return;

        }

        // Normalize to keep only direction
        velocity.Normalize();

        // 3. Calculate the angle between the car's forward vector and the velocity direction
        float crossDot = Vector3.Dot(Vector3.Cross(transform.forward, velocity), transform.up);
        // Clamp to avoid domain errors in Asin if crossDot slightly exceeds [-1..1]
        crossDot = Mathf.Clamp(crossDot, -1f, 1f);

        float angle = -Mathf.Asin(crossDot);

        // 4. Get the yaw (angular velocity around Y axis)
        float angularVelo = CarController.Rigid.angularVelocity.y;

        // 5. Decide whether to reduce front-axle grip
        //    Check if the angle sign is "opposite" the steerAngle sign (angle * steerAngle < 0).
        if (angle * frontAxle.steerAngle < 0) {

            // Example approach: never drop below a certain minimum grip
            // Adjust minGrip as you prefer (0.2 = 20% of normal stiffness).
            float minGrip = 0.2f;

            // The higher the angular velocity, the more we reduce stiffness
            float clampFactor = Mathf.Clamp01(tractionHelperStrength * Mathf.Abs(angularVelo));

            // Lerp from 1f (full grip) down to minGrip based on clampFactor
            frontAxle.tractionHelpedSidewaysStiffness =
                Mathf.Lerp(1f, minGrip, clampFactor);

        } else {

            // If angles aren't conflicting, restore full grip
            frontAxle.tractionHelpedSidewaysStiffness = 1f;

        }

    }

    /// <summary>
    /// Angular drag helper. Gradually increases Rigidbody’s angularDrag 
    /// at higher speeds for more stability, but scales it down while 
    /// the player is actually steering in the same direction of the 
    /// car's turn. Uses a calculated steering scale instead of a fixed factor.
    /// </summary>
    private void AngularDragHelper() {

        if (CarController.Rigid.isKinematic)
            return;

        float baseDrag = 0f;
        float maxDrag = 10f;

        float speedFactor = (CarController.absoluteSpeed * angularDragHelperStrength) / 1000f;

        if (!CarController.IsGrounded)
            speedFactor *= 4f;

        speedFactor = Mathf.Clamp01(speedFactor);

        float targetAngularDrag = Mathf.Lerp(baseDrag, maxDrag, speedFactor);

        float steerDifference = Mathf.Abs(CarController.steerInput_V) - Mathf.Abs(CarController.steerInput_P);
        steerDifference *= 100f;
        steerDifference = Mathf.Clamp01(steerDifference);

        if (steerDifference > 0.05f) {

            float steerAmount = Mathf.Clamp01((steerDifference - 0.1f) / (1f - 0.1f));
            float maxSteerDragReduction = 0.6f;
            float steeringDragScale = 1f - (maxSteerDragReduction * steerAmount * 1f);

            targetAngularDrag *= steeringDragScale;

        }

        // Finally apply the computed drag
        CarController.Rigid.angularDamping = Mathf.Lerp(CarController.Rigid.angularDamping, targetAngularDrag, Time.fixedDeltaTime * 2f);

    }


    /// <summary>
    /// Limits the maximum drift angle if it exceeds maxDriftAngle 
    /// by damping angular velocity.
    /// </summary>
    private void LimitDriftAngle() {

        // 1. Acquire current velocity and forward direction
        Vector3 velocity = CarController.Rigid.linearVelocity;
        Vector3 forward = transform.forward;

        // 2. Compute the signed angle (in degrees) between 'forward' and 'velocity',
        //    using Vector3.up as the axis. This effectively measures the yaw angle
        //    relative to the car's forward direction.
        float angle = Vector3.SignedAngle(forward, velocity, Vector3.up);

        // 3. If the absolute drift angle is beyond the desired maxDriftAngle,
        //    we damp the car's angular velocity, pulling it back toward zero rotation.
        if (Mathf.Abs(angle) > maxDriftAngle) {

            CarController.Rigid.angularVelocity = Vector3.Lerp(
                CarController.Rigid.angularVelocity,
                Vector3.zero,
                Time.fixedDeltaTime * driftAngleCorrectionFactor * groundedFactor
            );

        }

    }

    public void OnCollision(Collision collision) {

        if (dampingAngularVelocity)
            return;

        // If we have no contact points for some reason, just bail out
        if (collision.contactCount == 0) return;

        // Compute the average normal of all contact points
        Vector3 averageNormal = Vector3.zero;

        foreach (ContactPoint contact in collision.contacts)
            averageNormal += contact.normal;

        averageNormal /= collision.contactCount;

        // Compare dot product with Vector3.up
        // If it's reasonably close to 1.0, the surface is likely the ground
        float upDot = Vector3.Dot(averageNormal, Vector3.up);

        // You can tweak the threshold depending on your slope limits.
        // If your ground is perfectly horizontal, a threshold around 0.7-0.8 might suffice.
        if (upDot > 0.5f) {

            dampingAngularVelocity = true;

            float dampFactor = Mathf.Lerp(.95f, .6f, Mathf.InverseLerp(0f, 240f, CarController.absoluteSpeed));

            // This indicates we're likely hitting something "like" the ground
            // Damp the angular velocity
            CarController.Rigid.angularVelocity *= dampFactor;

            StartCoroutine(DampedAngularVelocityAfterCollision());

        }

    }

    private IEnumerator DampedAngularVelocityAfterCollision() {

        yield return new WaitForFixedUpdate();
        dampingAngularVelocity = false;

    }

    public void Reload() {

        ABSEngaged = false;
        ESPEngaged = false;
        TCSEngaged = false;
        dampingAngularVelocity = false;

    }

}
