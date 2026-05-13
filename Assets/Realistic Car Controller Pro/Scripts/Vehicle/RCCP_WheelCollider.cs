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
/// Manages a wheel's physics, alignment, slip calculations, friction, and special states (deflation, drift, etc.) 
/// using Unity WheelCollider. This component is designed to work in conjunction with the RCCP vehicle system.
/// </summary>
[RequireComponent(typeof(WheelCollider))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP WheelCollider")]
public class RCCP_WheelCollider : RCCP_Component {

    /// <summary>
    /// Backing field for WheelCollider component reference.
    /// </summary>
    private WheelCollider _wheelCollider;

    /// <summary>
    /// Actual wheelcollider component. Lazy-loaded on first access.
    /// </summary>
    public WheelCollider WheelCollider {

        get {

            if (_wheelCollider == null)
                _wheelCollider = GetComponent<WheelCollider>();

            return _wheelCollider;

        }

    }

    /// <summary>
    /// This wheel is connected to this axle. Defines axle grouping (front/rear or other).
    /// </summary>
    public RCCP_Axle connectedAxle;

    /// <summary>
    /// Information about what this wheel is currently hitting (if anything).
    /// </summary>
    public WheelHit wheelHit;

    /// <summary>
    /// If true, wheel models are aligned to WheelCollider orientation and position each frame.
    /// </summary>
    public bool alignWheels = true;

    /// <summary>
    /// Indicates whether this wheel is in contact with a surface.
    /// </summary>
    [Space()]
    public bool isGrounded = false;

    /// <summary>
    /// Indicates whether this wheel is currently slipping above a threshold (skidding).
    /// </summary>
    public bool isSkidding = false;

    /// <summary>
    /// Index of the ground material this wheel is on, used for slip thresholds, audio, etc.
    /// </summary>
    [Min(0)]
    public int groundIndex = 0;

    /// <summary>
    /// Motor torque applied to this wheel in Nm.
    /// </summary>
    [Space()]
    public float motorTorque = 0f;

    /// <summary>
    /// Brake torque applied to this wheel in Nm.
    /// </summary>
    public float brakeTorque = 0f;

    /// <summary>
    /// Steer input angle in degrees (before Ackermann corrections, if any).
    /// </summary>
    public float steerInput = 0f;

    /// <summary>
    /// Handbrake input in range [0..1].
    /// </summary>
    [Min(0f)]
    public float handbrakeInput = 0f;

    /// <summary>
    /// Transform reference for the visual representation (wheel model).
    /// </summary>
    [Space()]
    public Transform wheelModel;

    /// <summary>
    /// Approximate speed of the wheel derived from RPM, in km/h.
    /// </summary>
    public float wheelRPM2Speed = 0f;

    /// <summary>
    /// Width of the wheel used for skidmarks.
    /// </summary>
    [Space()]
    [Min(.1f)]
    public float width = .25f;

    /// <summary>
    /// Total rotation of the wheel (for spinning animation).
    /// </summary>
    private float wheelRotation = 0f;

    /// <summary>
    /// Camber angle, caster angle, and X offset to adjust wheel tilt and position.
    /// </summary>
    public float camber, caster, offset = 0f;

    /// <summary>
    /// Represents the 'temperature' or usage stress of this wheel. Increases with slip.
    /// </summary>
    [Space()]
    public float totalWheelTemp = 20f;

    /// <summary>
    /// Combined magnitude of forward and sideways slip, used for skids and audio.
    /// </summary>
    [Space()]
    public float totalSlip = 0f;

    /// <summary>
    /// Forward and sideways slip magnitudes extracted from the WheelCollider.
    /// </summary>
    public float wheelSlipAmountSideways, wheelSlipAmountForward = 0f;

    /// <summary>
    /// Current bump force used in collision/bump sound calculations.
    /// </summary>
    [HideInInspector]
    public float bumpForce, oldForce = 0f;

    /// <summary>
    /// Whether this wheel can generate skidmarks or not.
    /// </summary>
    [Space()]
    public bool drawSkid = true;

    /// <summary>
    /// Index of the last skidmark created by this wheel in the global SkidmarksManager.
    /// </summary>
    private int lastSkidmark = -1;

    /// <summary>
    /// Forward friction curve used by this wheel.
    /// </summary>
    private WheelFrictionCurve forwardFrictionCurve;

    /// <summary>
    /// Sideways friction curve used by this wheel.
    /// </summary>
    private WheelFrictionCurve sidewaysFrictionCurve;

    /// <summary>
    /// Default forward friction curve (backup for resetting).
    /// </summary>
    private WheelFrictionCurve forwardFrictionCurve_Def;

    /// <summary>
    /// Default sideways friction curve (backup for resetting).
    /// </summary>
    private WheelFrictionCurve sidewaysFrictionCurve_Def;

    /// <summary>
    /// AudioSource used for skid sound effects.
    /// </summary>
    private AudioSource skidAudioSource;

    /// <summary>
    /// Reference to the skid AudioClip for the current ground material.
    /// </summary>
    private AudioClip skidClip;

    /// <summary>
    /// Maximum volume for tire skid SFX based on current ground material.
    /// </summary>
    [Min(0f)]
    private float skidVolume = 0f;

    /// <summary>
    /// ESP traction cut factor fed by RCCP_Stability. Reduces motor torque if too high slip.
    /// </summary>
    private float cutTractionESP = 0f;

    /// <summary>
    /// TCS traction cut factor fed by RCCP_Stability. Reduces motor torque if too high forward slip.
    /// </summary>
    private float cutTractionTCS = 0f;

    /// <summary>
    /// ABS brake cut factor fed by RCCP_Stability. Reduces brake torque if slip is too high.
    /// </summary>
    [Min(0f)]
    private float cutBrakeABS = 0f;

    /// <summary>
    /// Indicates whether the wheel is deflated. Affects radius and friction stiffness.
    /// </summary>
    [Space()]
    public bool deflated = false;

    /// <summary>
    /// Scaling factor to reduce wheel radius when deflated.
    /// </summary>
    [Min(0f)]
    public float deflatedRadiusMultiplier = .8f;

    /// <summary>
    /// Scaling factor to reduce wheel friction stiffness when deflated.
    /// </summary>
    [Min(0f)]
    public float deflatedStiffnessMultiplier = .5f;

    /// <summary>
    /// Cached un-deflated radius. Used to restore radius on inflation.
    /// </summary>
    [Min(0f)]
    private float defRadius = -1f;

    /// <summary>
    /// If true, drift mode modifies friction curves to simulate drifting behavior.
    /// </summary>
    [Space()]
    public bool driftMode = false;

    /// <summary>
    /// Tunes the friction curves for best stability at high speed.
    /// </summary>
    public bool stableFrictionCurves = false;

    /// <summary>
    /// Minimum forward stiffness for drift mode.
    /// </summary>
    private readonly float minForwardStiffnessForDrift = .75f;

    /// <summary>
    /// Maximum forward stiffness for drift mode.
    /// </summary>
    private readonly float maxForwardStiffnessForDrift = 1.25f;

    /// <summary>
    /// Minimum sideways stiffness for drift mode.
    /// </summary>
    private readonly float minSidewaysStiffnessForDrift = .45f;

    /// <summary>
    /// Maximum sideways stiffness for drift mode.
    /// </summary>
    private readonly float maxSidewaysStiffnessForDrift = 1f;

    /// <summary>
    /// Distance between the front and rear axles, used in steering calculations (Ackermann).
    /// </summary>
    [Space()]
    public float wheelbase = 2.55f;

    /// <summary>
    /// Distance between the left and right wheels on an axle.
    /// </summary>
    public float trackWidth = 1.5f;

    /// <summary>
    /// Holds a smoothed or partially processed velocity-based value used in drift calculations.
    /// </summary>
    float sqrVel;

    /// <summary>
    /// Unity Awake method. Ensures the wheel model is assigned. Disables if missing.
    /// </summary>
    public override void Awake() {

        base.Awake();

        if (wheelModel == null) {

            Debug.LogError("Wheel model is not selected for " + transform.name + ". Disabling this wheelcollider.");
            enabled = false;
            return;

        }

        gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer);

    }

    /// <summary>
    /// Unity Start method. Configures wheel mass, obtains friction curves, creates audio source, and sets up a pivot transform for the wheel model.
    /// </summary>
    public override void Start() {

        base.Start();

        //  Increasing mass of the wheel for more stable handling. 
        //  In RCCPSettings, if useFixedWheelColliders is true, it sets mass based on the vehicle mass.
        if (RCCPSettings.useFixedWheelColliders) {

            WheelCollider.mass = CarController.Rigid.mass / 25f;
            WheelCollider.ConfigureVehicleSubsteps(10f, 7, 5);

        }

        //  Getting friction curves from the wheels.
        forwardFrictionCurve = WheelCollider.forwardFriction;
        sidewaysFrictionCurve = WheelCollider.sidewaysFriction;

        //  Getting default friction curves from the wheels for later resets.
        forwardFrictionCurve_Def = forwardFrictionCurve;
        sidewaysFrictionCurve_Def = sidewaysFrictionCurve;

        //	Creating a pivot at the correct position and rotation for the wheel model.
        GameObject newPivot = new GameObject("Pivot_" + wheelModel.transform.name);

        newPivot.transform.SetPositionAndRotation(RCCP_GetBounds.GetBoundsCenter(wheelModel.transform), transform.rotation);
        newPivot.transform.SetParent(wheelModel.transform.parent, true);

        //	Assigning the actual wheel model to the new pivot.
        wheelModel.SetParent(newPivot.transform, true);
        wheelModel = newPivot.transform;

        Invoke(nameof(CreateAudioSource), .02f);

    }

    /// <summary>
    /// Unity OnEnable method. Re-applies mass to the wheel if useFixedWheelColliders is active.
    /// </summary>
    public override void OnEnable() {

        base.OnEnable();

        //  Increasing mass of the wheel for more stable handling.
        if (RCCPSettings.useFixedWheelColliders) {

            WheelCollider.mass = CarController.Rigid.mass / 25f;

            //            Suggested Values Based on Use Case
            //Vehicle Type    Speed Threshold Steps Below Steps Above
            //Realistic Cars  5 - 10 m / s(18 - 36 km / h)   5 - 10    1 - 3
            //Arcade Cars 5 - 15 m / s(18 - 54 km / h)   3 - 5 1 - 2
            //Off - Road / Bumpy Terrain    3 - 8 m / s(11 - 28 km / h)    10 - 15   5 - 7
            //High - Speed Racing Cars  15 - 25 m / s(54 - 90 km / h)  3 - 6 1 - 2
            WheelCollider.ConfigureVehicleSubsteps(5f, 9, 6);

        }

    }

    /// <summary>
    /// Creating audiosource for skid SFX.
    /// </summary>
    private void CreateAudioSource() {

        if (skidAudioSource)
            return;

        if (CarController.Audio && CarController.Audio.audioMixer)
            skidAudioSource = RCCP_AudioSource.NewAudioSource(CarController.Audio.audioMixer, CarController.gameObject, "Skid Sound AudioSource", 3f, 50f, 0f, null, true, true, false);
        else
            skidAudioSource = RCCP_AudioSource.NewAudioSource(CarController.gameObject, "Skid Sound AudioSource", 3f, 50f, 0f, null, true, true, false);

        if (CarController.Audio && skidAudioSource) {

            if (CarController.Audio.transform.childCount > 0)
                skidAudioSource.transform.SetParent(CarController.Audio.transform.GetChild(0), true);
            else
                skidAudioSource.transform.SetParent(CarController.Audio.transform, true);

        }

    }

    /// <summary>
    /// Unity Update method. Optionally aligns the visual wheel model.
    /// </summary>
    private void Update() {

        if (alignWheels)
            WheelAlign();

    }

    /// <summary>
    /// Unity FixedUpdate method. Calculates speed from RPM, applies motor/brake torque, handles friction, skidmarks, etc.
    /// </summary>
    private void FixedUpdate() {

        //  If wheelcollider is not enabled yet, return.
        if (!WheelCollider.enabled)
            return;

        // Convert RPM to approximate speed (km/h).
        float circumference = 2.0f * Mathf.PI * WheelCollider.radius;

        if (Mathf.Abs(WheelCollider.rpm) > .01f)
            wheelRPM2Speed = (circumference * WheelCollider.rpm) * 60f / 1000f;
        else
            wheelRPM2Speed = 0f;

        MotorTorque();
        BrakeTorque();
        GroundMaterial();
        Frictions();
        SkidMarks();
        WheelTemp();
        Audio();

    }

    /// <summary>
    /// Aligning wheel model position and rotation to match the WheelCollider, accounting for camber/caster.
    /// </summary>
    private void WheelAlign() {

        // Return if no wheel model selected.
        if (!wheelModel)
            return;

        //  If wheelcollider is not enabled, hide or disable model. Otherwise show it.
        wheelModel.gameObject.SetActive(WheelCollider.enabled);

        //  If wheelcollider is not enabled yet, return.
        if (!WheelCollider.enabled)
            return;

        //  Positions and rotations of the wheel.
        Vector3 wheelPosition;
        Quaternion wheelRotation;

        //  Getting position and rotation from WheelCollider.
        WheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);

        //Increase the rotation value based on RPM.
        this.wheelRotation += WheelCollider.rpm * (360f / 60f) * Time.deltaTime;

        //	Assigning position and rotation to the wheel model.
        wheelModel.transform.SetPositionAndRotation(wheelPosition, transform.rotation * Quaternion.Euler(this.wheelRotation, WheelCollider.steerAngle, 0f));

        //	Adjust offset by X axis to simulate different rim offsets.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.position += (transform.right * offset);
        else
            wheelModel.transform.position -= (transform.right * offset);

        // Adjusting camber angle by Z axis.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.forward, -camber);
        else
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.forward, camber);

        // Adjusting caster angle by X axis.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.right, -caster);
        else
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.right, caster);

    }

    /// <summary>
    /// Converts world position to terrain splat map coordinates for checking terrain texture indexes.
    /// </summary>
    private Vector3 ConvertToSplatMapCoordinate(Terrain terrain, Vector3 playerPos) {

        Vector3 vecRet = new Vector3();
        Vector3 terPosition = terrain.transform.position;
        vecRet.x = ((playerPos.x - terPosition.x) / terrain.terrainData.size.x) * terrain.terrainData.alphamapWidth;
        vecRet.z = ((playerPos.z - terPosition.z) / terrain.terrainData.size.z) * terrain.terrainData.alphamapHeight;
        return vecRet;

    }

    /// <summary>
    /// Determines the appropriate ground material index by checking contact's PhysicMaterial or terrain texture.
    /// </summary>
    private void GroundMaterial() {

        isGrounded = WheelCollider.GetGroundHit(out wheelHit);

        //  If there are no contact points, set default index to 0.
        if (!isGrounded || wheelHit.point == Vector3.zero || wheelHit.collider == null) {

            groundIndex = 0;
            return;

        }

        // Contacted any physic material in Configurable Ground Materials yet?
        bool contactedWithAnyMaterialYet = false;

        //  Checking the material of the contact point in the RCCP_GroundMaterials ground frictions.
        for (int i = 0; i < RCCPGroundMaterials.frictions.Length; i++) {

            //  If there is one, assign the index of the material. 
            if (wheelHit.collider.sharedMaterial == RCCPGroundMaterials.frictions[i].groundMaterial) {

                contactedWithAnyMaterialYet = true;
                groundIndex = i;

            }

        }

        // If ground PhysicMaterial is not found among configured ground materials, check if we are on a terrain collider.
        if (!contactedWithAnyMaterialYet) {

            //  If terrains are not initialized yet, return.
            if (!RCCPSceneManager.terrainsInitialized) {

                groundIndex = 0;
                return;

            }

            //  Checking the material of the contact point in the RCCP_GroundMaterials terrain frictions.
            for (int i = 0; i < RCCPGroundMaterials.terrainFrictions.Length; i++) {

                if (wheelHit.collider.sharedMaterial == RCCPGroundMaterials.terrainFrictions[i].groundMaterial) {

                    RCCP_SceneManager.Terrains currentTerrain = null;

                    for (int l = 0; l < RCCPSceneManager.terrains.Length; l++) {

                        if (RCCPSceneManager.terrains[l].terrainCollider == RCCPGroundMaterials.terrainFrictions[i].groundMaterial) {
                            currentTerrain = RCCPSceneManager.terrains[l];
                            break;
                        }

                    }

                    //  Once we have that terrain, get exact position in the terrain map coordinate.
                    if (currentTerrain != null) {

                        Vector3 playerPos = transform.position;
                        Vector3 TerrainCord = ConvertToSplatMapCoordinate(currentTerrain.terrain, playerPos);
                        float comp = 0f;

                        //  Finding the right terrain texture around the hit position.
                        for (int k = 0; k < currentTerrain.mNumTextures; k++) {

                            if (comp < currentTerrain.mSplatmapData[(int)TerrainCord.z, (int)TerrainCord.x, k])
                                groundIndex = k;

                        }

                        //  Assign the index of the material based on splatmap indexes.
                        groundIndex = RCCPGroundMaterials.terrainFrictions[i].splatmapIndexes[groundIndex].index;

                    }

                }

            }

        }

    }

    /// <summary>
    /// Generates skidmarks for this wheel if slip exceeds threshold set by the current ground material.
    /// </summary>
    private void SkidMarks() {

        //  If drawing skids are not enabled, return.
        if (!drawSkid)
            return;

        // If slip is above the ground friction slip threshold...
        if (totalSlip > RCCPGroundMaterials.frictions[groundIndex].slip) {

            Vector3 skidPoint = wheelHit.point + (CarController.Rigid.linearVelocity * Time.deltaTime);

            //  If velocity is nonzero and the wheel is grounded, record a new skidmark.
            if (CarController.Rigid.linearVelocity.magnitude > .1f && isGrounded && wheelHit.normal != Vector3.zero && wheelHit.point != Vector3.zero && skidPoint != Vector3.zero && Mathf.Abs(skidPoint.magnitude) >= .1f)
                lastSkidmark = RCCP_SkidmarksManager.Instance.AddSkidMark(skidPoint, wheelHit.normal, totalSlip - RCCPGroundMaterials.frictions[groundIndex].slip, width, lastSkidmark, groundIndex);
            else
                lastSkidmark = -1;

        } else {

            //  Slip is not above threshold, reset last skidmark index.
            lastSkidmark = -1;

        }

    }

    /// <summary>
    /// Updates the wheel's temperature based on slip and cools it over time.
    /// </summary>
    private void WheelTemp() {

        if (isSkidding)
            totalWheelTemp += Time.deltaTime * 10f * totalSlip;

        totalWheelTemp -= Time.deltaTime * 1.5f;
        totalWheelTemp = Mathf.Clamp(totalWheelTemp, 20f, 125f);

    }

    /// <summary>
    /// Applies the accumulated motorTorque to the WheelCollider's motorTorque, factoring in traction cuts (ESP/TCS) and overtorque checks.
    /// </summary>
    private void MotorTorque() {

        float torque = motorTorque;
        bool positiveTorque = true;

        if (torque < -1)
            positiveTorque = false;

        // Cut traction for ESP.
        if (cutTractionESP != 0f) {

            torque -= Mathf.Clamp(torque * (Mathf.Abs(wheelSlipAmountSideways) * cutTractionESP), 0f, Mathf.Infinity);
            torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

        }

        // Cut traction for TCS if there's forward slip.
        if (cutTractionTCS != 0f && Mathf.Abs(wheelSlipAmountForward) > .05f) {

            if (Mathf.Sign(WheelCollider.rpm) >= 0) {

                torque -= Mathf.Clamp(torque * (Mathf.Abs(wheelSlipAmountForward) * cutTractionTCS), 0f, Mathf.Infinity);
                torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

            } else {

                torque += Mathf.Clamp(-torque * (Mathf.Abs(wheelSlipAmountForward) * cutTractionTCS), 0f, Mathf.Infinity);
                torque = Mathf.Clamp(torque, -Mathf.Infinity, 0f);

            }

        }

        // Ensure the sign is respected based on positive/negative torque.
        if (positiveTorque)
            torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);
        else
            torque = Mathf.Clamp(torque, -Mathf.Infinity, 0f);

        if (Mathf.Abs(torque) < 5f)
            torque = 0f;

        WheelCollider.motorTorque = torque;

        // Zero out if engine is off or speed limit is reached.
        if (CheckOvertorque())
            WheelCollider.motorTorque = 0f;

        cutTractionESP = 0f;
        cutTractionTCS = 0f;
        motorTorque = 0f;

    }

    /// <summary>
    /// Applies the accumulated brakeTorque to the WheelCollider's brakeTorque, factoring in ABS cuts.
    /// </summary>
    private void BrakeTorque() {

        float torque = brakeTorque;

        // ABS brake cut.
        if (cutBrakeABS != 0f) {

            torque -= Mathf.Clamp(torque * cutBrakeABS, 0f, Mathf.Infinity);
            torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

        }

        torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

        if (torque < 5f)
            torque = 0f;

        WheelCollider.brakeTorque = torque;

        cutBrakeABS = 0f;
        brakeTorque = 0f;

    }

    /// <summary>
    /// Manages friction curves and updates slip values. Also applies drift mode changes if enabled.
    /// </summary>
    private void Frictions() {

        //  If wheel is grounded, get forward and sideways slips. Otherwise, set them to 0.
        if (isGrounded) {

            wheelSlipAmountForward = Mathf.Lerp(wheelSlipAmountForward, wheelHit.forwardSlip, Time.deltaTime * 5f);
            wheelSlipAmountSideways = Mathf.Lerp(wheelSlipAmountSideways, wheelHit.sidewaysSlip, Time.deltaTime * 5f);
            totalSlip = Mathf.Lerp(totalSlip, (Mathf.Abs(wheelHit.forwardSlip) + Mathf.Abs(wheelHit.sidewaysSlip)), Time.deltaTime * 5f);

        } else {

            wheelSlipAmountForward = Mathf.Lerp(wheelSlipAmountForward, 0f, Time.deltaTime * 5f);
            wheelSlipAmountSideways = Mathf.Lerp(wheelSlipAmountSideways, 0f, Time.deltaTime * 5f);
            totalSlip = Mathf.Lerp(totalSlip, 0f, Time.deltaTime * 5f);

        }

        if (totalSlip >= RCCPGroundMaterials.frictions[groundIndex].slip)
            isSkidding = true;
        else
            isSkidding = false;

        // Setting stiffness of the forward and sideways friction curves.
        forwardFrictionCurve.stiffness = RCCPGroundMaterials.frictions[groundIndex].forwardStiffness;
        sidewaysFrictionCurve.stiffness = ((RCCPGroundMaterials.frictions[groundIndex].sidewaysStiffness * (1f - (handbrakeInput / 5f))) * connectedAxle.tractionHelpedSidewaysStiffness);
        handbrakeInput = 0f;

        //  If wheel is deflated, multiply the stiffness by the deflatedStiffnessMultiplier.
        if (deflated) {

            forwardFrictionCurve.stiffness *= deflatedStiffnessMultiplier;
            sidewaysFrictionCurve.stiffness *= deflatedStiffnessMultiplier;

        }

        //  If drift mode is active, apply drift adjustments to friction curves.
        if (driftMode)
            Drift();

        if (stableFrictionCurves)
            TuneFrictionCurves();

        // Setting new friction curves to wheels if they have changed significantly.
        if (!ApproximatelyEqualFriction(forwardFrictionCurve, WheelCollider.forwardFriction))
            WheelCollider.forwardFriction = forwardFrictionCurve;

        if (!ApproximatelyEqualFriction(sidewaysFrictionCurve, WheelCollider.sidewaysFriction))
            WheelCollider.sidewaysFriction = sidewaysFrictionCurve;

        // Also control wheel damping based on motor torque.
        if (Mathf.Abs(WheelCollider.motorTorque) < 100f)
            WheelCollider.wheelDampingRate = RCCPGroundMaterials.frictions[groundIndex].damp;
        else
            WheelCollider.wheelDampingRate = 0f;

    }

    private void TuneFrictionCurves() {

        float speedFactor = Mathf.InverseLerp(0f, 360f, CarController.absoluteSpeed);

        // Forward friction
        forwardFrictionCurve.extremumSlip = Mathf.Lerp(forwardFrictionCurve_Def.extremumSlip, forwardFrictionCurve_Def.extremumSlip * .95f, speedFactor);
        forwardFrictionCurve.extremumValue = Mathf.Lerp(forwardFrictionCurve_Def.extremumValue, forwardFrictionCurve_Def.extremumValue * 1.05f, speedFactor);
        forwardFrictionCurve.asymptoteSlip = Mathf.Lerp(forwardFrictionCurve_Def.asymptoteSlip, forwardFrictionCurve_Def.asymptoteSlip * .95f, speedFactor);
        forwardFrictionCurve.asymptoteValue = Mathf.Lerp(forwardFrictionCurve_Def.asymptoteValue, forwardFrictionCurve_Def.asymptoteValue * 1.05f, speedFactor);

        // Sideways friction
        sidewaysFrictionCurve.extremumSlip = Mathf.Lerp(sidewaysFrictionCurve_Def.extremumSlip, sidewaysFrictionCurve_Def.extremumSlip * .95f, speedFactor);
        sidewaysFrictionCurve.extremumValue = Mathf.Lerp(sidewaysFrictionCurve_Def.extremumValue, sidewaysFrictionCurve_Def.extremumValue * 1.05f, speedFactor);
        sidewaysFrictionCurve.asymptoteSlip = Mathf.Lerp(sidewaysFrictionCurve_Def.asymptoteSlip, sidewaysFrictionCurve_Def.asymptoteSlip * .95f, speedFactor);
        sidewaysFrictionCurve.asymptoteValue = Mathf.Lerp(sidewaysFrictionCurve_Def.asymptoteValue, sidewaysFrictionCurve_Def.asymptoteValue * 1.05f, speedFactor);

    }

    /// <summary>
    /// Checks if two WheelFrictionCurves are effectively the same (within a small epsilon).
    /// </summary>
    private bool ApproximatelyEqualFriction(WheelFrictionCurve a, WheelFrictionCurve b, float epsilon = 0.0001f) {

        return
            Mathf.Abs(a.extremumSlip - b.extremumSlip) < epsilon &&
            Mathf.Abs(a.extremumValue - b.extremumValue) < epsilon &&
            Mathf.Abs(a.asymptoteSlip - b.asymptoteSlip) < epsilon &&
            Mathf.Abs(a.asymptoteValue - b.asymptoteValue) < epsilon &&
            Mathf.Abs(a.stiffness - b.stiffness) < epsilon;

    }

    /// <summary>
    /// Manages the skid audio playback by monitoring total slip and applying volumes/pitches.
    /// Also calculates a bump force when the wheel hits large forces.
    /// </summary>
    private void Audio() {

        if (skidAudioSource) {

            // If total slip is high enough, play skid SFX.
            if (totalSlip > RCCPGroundMaterials.frictions[groundIndex].slip) {

                skidClip = RCCPGroundMaterials.frictions[groundIndex].groundSound;
                skidVolume = RCCPGroundMaterials.frictions[groundIndex].volume;

                if (skidAudioSource.clip != skidClip)
                    skidAudioSource.clip = skidClip;

                if (!skidAudioSource.isPlaying)
                    skidAudioSource.Play();

                if (CarController.Rigid.linearVelocity.magnitude > .1f) {

                    skidAudioSource.volume = Mathf.Lerp(skidAudioSource.volume, Mathf.Lerp(0f, skidVolume, totalSlip - RCCPGroundMaterials.frictions[groundIndex].slip), Time.fixedDeltaTime * 5f);
                    skidAudioSource.pitch = Mathf.Lerp(skidAudioSource.pitch, Mathf.Lerp(.7f, 1f, totalSlip - RCCPGroundMaterials.frictions[groundIndex].slip), Time.fixedDeltaTime * 5f);

                } else {

                    skidAudioSource.volume = Mathf.Lerp(skidAudioSource.volume, 0f, Time.fixedDeltaTime * 15f);
                    skidAudioSource.pitch = Mathf.Lerp(skidAudioSource.pitch, 1f, Time.fixedDeltaTime * 15f);

                }

            } else {

                skidAudioSource.volume = Mathf.Lerp(skidAudioSource.volume, 0f, Time.fixedDeltaTime * 15f);
                skidAudioSource.pitch = Mathf.Lerp(skidAudioSource.pitch, 1f, Time.fixedDeltaTime * 15f);

                if (skidAudioSource.volume <= .05f && skidAudioSource.isPlaying)
                    skidAudioSource.Stop();

            }

            if (skidAudioSource.volume < .02f)
                skidAudioSource.volume = 0f;

        }

        // Calculate bump force based on difference in hit force.
        bumpForce = wheelHit.force - oldForce;

        //	If bump force is high enough, you could play a bump SFX here.
        if ((bumpForce) >= 5000f) {
            // Example: Trigger bump sounds, apply random pitch, etc.
        }

        oldForce = wheelHit.force;

    }

    /// <summary>
    /// Sets the forward friction curves of the wheel. Allows customizing slip and grip levels.
    /// </summary>
    public void SetFrictionCurvesForward(float extremumSlip, float extremumValue, float asymptoteSlip, float asymptoteValue) {

        WheelFrictionCurve newCurve = new WheelFrictionCurve();
        newCurve.extremumSlip = extremumSlip;
        newCurve.extremumValue = extremumValue;
        newCurve.asymptoteSlip = asymptoteSlip;
        newCurve.asymptoteValue = asymptoteValue;
        forwardFrictionCurve = newCurve;

        forwardFrictionCurve_Def = forwardFrictionCurve;

    }

    /// <summary>
    /// Sets the sideways friction curves of the wheel. Allows customizing slip and grip levels.
    /// </summary>
    public void SetFrictionCurvesSideways(float extremumSlip, float extremumValue, float asymptoteSlip, float asymptoteValue) {

        WheelFrictionCurve newCurve = new WheelFrictionCurve();
        newCurve.extremumSlip = extremumSlip;
        newCurve.extremumValue = extremumValue;
        newCurve.asymptoteSlip = asymptoteSlip;
        newCurve.asymptoteValue = asymptoteValue;
        sidewaysFrictionCurve = newCurve;

        sidewaysFrictionCurve_Def = sidewaysFrictionCurve;

    }

    /// <summary>
    /// Applies steering angle to the wheel with built-in Ackermann angle corrections based on wheelbase and trackWidth.
    /// </summary>
    public void ApplySteering(float steeringAngle) {

        if (!WheelCollider.enabled)
            return;

        float avgAngleDeg = steeringAngle;
        float avgAngleRad = avgAngleDeg * Mathf.Deg2Rad;

        float radiusInside = wheelbase / Mathf.Tan(Mathf.Abs(avgAngleRad));
        float finalAngleDeg;

        bool turningRight = steeringAngle > 0f;
        bool turningLeft = steeringAngle < 0f;
        bool thisIsLeftWheel = transform.localPosition.x < 0f;

        if (turningRight) {

            if (thisIsLeftWheel) {

                float outsideAngleRad = Mathf.Atan(wheelbase / (radiusInside + trackWidth * 0.5f));
                finalAngleDeg = Mathf.Rad2Deg * outsideAngleRad;

            } else {

                float insideAngleRad = Mathf.Atan(wheelbase / (radiusInside - trackWidth * 0.5f));
                finalAngleDeg = Mathf.Rad2Deg * insideAngleRad;

            }

        } else if (turningLeft) {

            if (thisIsLeftWheel) {

                float insideAngleRad = Mathf.Atan(wheelbase / (radiusInside - trackWidth * 0.5f));
                finalAngleDeg = Mathf.Rad2Deg * insideAngleRad;

            } else {

                float outsideAngleRad = Mathf.Atan(wheelbase / (radiusInside + trackWidth * 0.5f));
                finalAngleDeg = Mathf.Rad2Deg * outsideAngleRad;

            }

            finalAngleDeg *= -1f;

        } else {

            finalAngleDeg = 0f;

        }

        WheelCollider.steerAngle = finalAngleDeg;

    }

    /// <summary>
    /// Adds motor torque (Nm) to be applied in the next FixedUpdate. Positive for forward, negative for reverse.
    /// </summary>
    public void AddMotorTorque(float torque) {

        if (!WheelCollider.enabled)
            return;

        motorTorque += torque;

    }

    /// <summary>
    /// Adds brake torque (Nm) to be applied in the next FixedUpdate.
    /// </summary>
    public void AddBrakeTorque(float torque) {

        if (!WheelCollider.enabled)
            return;

        brakeTorque += torque;

    }

    /// <summary>
    /// Adds handbrake torque (Nm) to be applied in the next FixedUpdate, also sets the handbrake input factor.
    /// </summary>
    public void AddHandbrakeTorque(float torque) {

        if (!WheelCollider.enabled)
            return;

        brakeTorque += torque;
        handbrakeInput += Mathf.Clamp01(torque / 1000f);

    }

    /// <summary>
    /// Cuts traction torque (ESP) to control slip. Larger values reduce more motor torque.
    /// </summary>
    public void CutTractionESP(float _cutTraction) {

        if (!WheelCollider.enabled)
            return;

        cutTractionESP = _cutTraction;

    }

    /// <summary>
    /// Cuts traction torque (TCS) for forward slip. Larger values reduce more motor torque.
    /// </summary>
    public void CutTractionTCS(float _cutTraction) {

        if (!WheelCollider.enabled)
            return;

        cutTractionTCS = _cutTraction;

    }

    /// <summary>
    /// Cuts brake torque (ABS) to control slip. Larger values reduce more brake torque.
    /// </summary>
    public void CutBrakeABS(float _cutBrake) {

        if (!WheelCollider.enabled)
            return;

        cutBrakeABS = _cutBrake;

    }

    /// <summary>
    /// Deflates the wheel, reducing radius and friction stiffness. Triggers events in CarController.
    /// </summary>
    public void Deflate() {

        if (!WheelCollider.enabled)
            return;

        if (deflated)
            return;

        deflated = true;

        if (defRadius == -1)
            defRadius = WheelCollider.radius;

        WheelCollider.radius = defRadius * deflatedRadiusMultiplier;

        CarController.Rigid.AddForceAtPosition(transform.right * UnityEngine.Random.Range(-1f, 1f) * 25f, transform.position, ForceMode.Acceleration);
        CarController.OnWheelDeflated();

    }

    /// <summary>
    /// Restores the wheel radius and friction after a deflation. Triggers events in CarController.
    /// </summary>
    public void Inflate() {

        if (!WheelCollider.enabled)
            return;

        if (!deflated)
            return;

        deflated = false;

        if (defRadius != -1)
            WheelCollider.radius = defRadius;

        CarController.OnWheelInflated();

    }

    /// <summary>
    /// Modifies friction curves to simulate drifting by lowering or raising friction based on sideways velocity.
    /// </summary>
    private void Drift() {

        // 1. Get local velocity and smoothly track sideways drift magnitude.
        Vector3 relativeVelocity = transform.InverseTransformDirection(CarController.Rigid.linearVelocity);
        sqrVel = Mathf.Lerp(sqrVel, (relativeVelocity.x * relativeVelocity.x) / 50f, Time.fixedDeltaTime * 5f);

        // 2. Incorporate forward slip if any.
        if (Mathf.Abs(wheelHit.forwardSlip) > 0f) {
            sqrVel += (Mathf.Abs(wheelHit.forwardSlip) * 0.5f);
        }

        sqrVel = Mathf.Max(sqrVel, 0f);

        // 3. Adjust forward friction differently for rear wheels (z < 0) vs front wheels (z >= 0).
        if (transform.localPosition.z < 0) {
            // Rear wheels
            forwardFrictionCurve.extremumValue = Mathf.Clamp(
                forwardFrictionCurve_Def.extremumValue - (sqrVel / 1f),
                minForwardStiffnessForDrift,
                maxForwardStiffnessForDrift
            );
            forwardFrictionCurve.asymptoteValue = Mathf.Clamp(
                forwardFrictionCurve_Def.asymptoteValue + (sqrVel / 1f),
                minForwardStiffnessForDrift,
                maxForwardStiffnessForDrift
            );
        } else {
            // Front wheels
            forwardFrictionCurve.extremumValue = Mathf.Clamp(
                forwardFrictionCurve_Def.extremumValue - (sqrVel / 0.5f),
                minForwardStiffnessForDrift / 2f,
                maxForwardStiffnessForDrift
            );
            forwardFrictionCurve.asymptoteValue = Mathf.Clamp(
                forwardFrictionCurve_Def.asymptoteValue - (sqrVel / 0.5f),
                minForwardStiffnessForDrift / 2f,
                maxForwardStiffnessForDrift
            );
        }

        // 4. Adjust sideways friction for drifting.
        sidewaysFrictionCurve.extremumValue = Mathf.Clamp(
            sidewaysFrictionCurve_Def.extremumValue - (sqrVel / 1f),
            minSidewaysStiffnessForDrift,
            maxSidewaysStiffnessForDrift
        );
        sidewaysFrictionCurve.asymptoteValue = Mathf.Clamp(
            sidewaysFrictionCurve_Def.asymptoteValue - (sqrVel / 1f),
            minSidewaysStiffnessForDrift,
            maxSidewaysStiffnessForDrift
        );
    }

    /// <summary>
    /// Checks if torque output should be zero, for example if engine is off or speed limit is reached.
    /// </summary>
    private bool CheckOvertorque() {

        if (!CarController.engineRunning)
            return true;

        if (CarController.speed > CarController.maximumSpeed)
            return true;

        if (Mathf.Abs(wheelRPM2Speed) > (CarController.Gearbox.TargetSpeeds[CarController.Gearbox.currentGear] * 1.02f))
            return true;

        return false;

    }

    /// <summary>
    /// Resets all runtime fields (torques, slip, audio) for this wheel. Useful when toggling the wheel on/off.
    /// </summary>
    public void Reload() {

        motorTorque = 0f;
        brakeTorque = 0f;
        steerInput = 0f;
        handbrakeInput = 0f;
        wheelRotation = 0f;
        cutTractionESP = 0f;
        cutTractionTCS = 0f;
        cutBrakeABS = 0f;
        bumpForce = 0f;
        oldForce = 0f;
        wheelRotation = 0f;
        lastSkidmark = -1;
        totalSlip = 0f;
        wheelSlipAmountForward = 0f;
        wheelSlipAmountSideways = 0f;
        skidVolume = 0f;

        if (skidAudioSource) {

            skidAudioSource.volume = 0f;
            skidAudioSource.pitch = 1f;

        }

    }

    /// <summary>
    /// Unity Reset method. Called when script is first added or component is reset in the Editor. 
    /// Initializes some default values for WheelCollider.
    /// </summary>
    private void Reset() {

        WheelCollider wc = GetComponent<WheelCollider>();

        //  Increasing mass of the wheel for more stable handling.
        if (RCCPSettings.useFixedWheelColliders)
            wc.mass = GetComponentInParent<RCCP_CarController>(true).Rigid.mass / 25f;

        wc.forceAppPointDistance = .1f;
        wc.suspensionDistance = .2f;

        JointSpring js = wc.suspensionSpring;
        js.spring = 50000f;
        js.damper = 3500f;
        wc.suspensionSpring = js;

        WheelFrictionCurve frictionCurveFwd = wc.forwardFriction;
        frictionCurveFwd.extremumSlip = .4f;
        wc.forwardFriction = frictionCurveFwd;

        WheelFrictionCurve frictionCurveSide = wc.sidewaysFriction;
        frictionCurveSide.extremumSlip = .35f;
        wc.sidewaysFriction = frictionCurveSide;

    }

    /// <summary>
    /// Aligns wheel model with wheel collider in the Editor. Adjusts WheelCollider radius and position based on the model mesh bounds.
    /// </summary>
    public void AlignWheel() {

        if (!WheelCollider.enabled)
            return;

        if (!wheelModel)
            return;

        transform.position = RCCP_GetBounds.GetBoundsCenter(wheelModel);
        transform.position += transform.up * (WheelCollider.suspensionDistance * (transform.root.localScale.y * (1f - WheelCollider.suspensionSpring.targetPosition)));
        WheelCollider.radius = RCCP_GetBounds.MaxBoundsExtent(wheelModel) / transform.root.localScale.y;

    }

    /// <summary>
    /// Detaches the wheel.
    /// </summary>
    public void DetachWheel() {

        if (!WheelCollider.enabled)
            return;

        if (wheelModel && !wheelModel.gameObject.activeSelf)
            return;

        GameObject clonedWheel = Instantiate(wheelModel.gameObject, wheelModel.transform.position, wheelModel.transform.rotation, null);
        clonedWheel.SetActive(true);
        clonedWheel.AddComponent<Rigidbody>();

        GameObject clonedMeshCollider = new GameObject("MeshCollider");
        clonedMeshCollider.transform.SetParent(clonedWheel.transform, false);
        clonedMeshCollider.transform.position = RCCP_GetBounds.GetBoundsCenter(clonedWheel.transform);
        MeshCollider mc = clonedMeshCollider.AddComponent<MeshCollider>();
        MeshFilter biggestMesh = RCCP_GetBounds.GetBiggestMesh(clonedWheel.transform);
        mc.sharedMesh = biggestMesh.mesh;
        mc.convex = true;

        clonedMeshCollider.layer = LayerMask.NameToLayer(RCCPSettings.RCCPDetachablePartLayer);

        foreach (Transform item in clonedMeshCollider.GetComponentsInChildren<Transform>(true)) {
            item.gameObject.layer = LayerMask.NameToLayer(RCCPSettings.RCCPDetachablePartLayer);
        }

        WheelCollider.enabled = false;

    }

    /// <summary>
    /// Repairs the wheel.
    /// </summary>
    public void OnRepair() {

        if (WheelCollider.enabled)
            return;

        WheelCollider.enabled = true;

        if (wheelModel)
            wheelModel.gameObject.SetActive(true);

        Inflate();

    }

}
