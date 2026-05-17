//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// Main input manager of the RCCP. Receives inputs from the corresponding device and let the other components use them.
/// </summary>
public class RCCP_InputManager : RCCP_Singleton<RCCP_InputManager> {

    public RCCP_Inputs inputs = new RCCP_Inputs();

    public InputActionAsset inputActionsInstance = null;

    public bool overrideInputs = false;

    public delegate void onGearShiftedUp();
    public static event onGearShiftedUp OnGearShiftedUp;

    public delegate void onGearShiftedDown();
    public static event onGearShiftedDown OnGearShiftedDown;

    public delegate void onGearShiftedTo(int gearIndex);
    public static event onGearShiftedTo OnGearShiftedTo;

    public delegate void onGearShiftedToN();
    public static event onGearShiftedToN OnGearShiftedToN;

    public delegate void onGearToggle(RCCP_Gearbox.TransmissionType transmissionType);
    public static event onGearToggle OnGearToggle;

    public delegate void onAutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear semiAutomaticDNRPGear);
    public static event onAutomaticGear OnAutomaticGear;

    public delegate void onChangedCamera();
    public static event onChangedCamera OnChangedCamera;

    public delegate void onLookBackCamera(bool state);
    public static event onLookBackCamera OnLookBackCamera;

    public delegate void onHoldOrbitCamera(bool state);
    public static event onHoldOrbitCamera OnHoldOrbitCamera;

    public delegate void onPressedLowBeamLights();
    public static event onPressedLowBeamLights OnPressedLowBeamLights;

    public delegate void onPressedHighBeamLights();
    public static event onPressedHighBeamLights OnPressedHighBeamLights;

    public delegate void onPressedLeftIndicatorLights();
    public static event onPressedLeftIndicatorLights OnPressedLeftIndicatorLights;

    public delegate void onPressedRightIndicatorLights();
    public static event onPressedRightIndicatorLights OnPressedRightIndicatorLights;

    public delegate void onPressedIndicatorLights();
    public static event onPressedIndicatorLights OnPressedIndicatorLights;

    public delegate void onStartEngine();
    public static event onStartEngine OnStartEngine;

    public delegate void onStopEngine();
    public static event onStopEngine OnStopEngine;

    public delegate void onSteeringHelper();
    public static event onSteeringHelper OnSteeringHelper;

    public delegate void onTractionHelper();
    public static event onTractionHelper OnTractionHelper;

    public delegate void onAngularDragHelper();
    public static event onAngularDragHelper OnAngularDragHelper;

    public delegate void onABS();
    public static event onABS OnABS;

    public delegate void onESP();
    public static event onESP OnESP;

    public delegate void onTCS();
    public static event onTCS OnTCS;

    public delegate void onRecord();
    public static event onRecord OnRecord;

    public delegate void onReplay();
    public static event onReplay OnReplay;

    public delegate void onTrailerDetach();
    public static event onTrailerDetach OnTrailerDetach;

    public delegate void onOptions();
    public static event onOptions OnOptions;

    private void Awake() {

        // If there is already an instance and it is not this one, destroy this one.
        if (Instance != this) {

            Destroy(gameObject);
            return;

        }

        // Make this object persistent across scene loads
        DontDestroyOnLoad(gameObject);

        // Initialize your inputs
        inputs = new RCCP_Inputs();

    }

    private void OnEnable() {

        // Get the Input Actions from your RCCP_InputActions
        if (RCCP_InputActions.Instance != null) {

            inputActionsInstance = RCCP_InputActions.Instance.inputActions;

            // Enable the entire asset
            inputActionsInstance.Enable();

            // Subscribe all performed/canceled events here
            SubscribeDrivingMapEvents();
            SubscribeCameraMapEvents();
            SubscribeReplayMapEvents();

        }

    }

    private void OnDisable() {

        // Safely unsubscribe from all performed/canceled events here
        if (inputActionsInstance != null) {

            UnsubscribeDrivingMapEvents();
            UnsubscribeCameraMapEvents();
            UnsubscribeReplayMapEvents();

        }

    }

    private void Update() {

        // Re-create inputs if somehow null
        if (inputs == null)
            inputs = new RCCP_Inputs();

        // Receive inputs from the controller only if we're not overriding
        if (!overrideInputs) {

            if (!RCCPSettings.mobileControllerEnabled)
                inputs = KeyboardInputs();
            else
                inputs = MobileInputs();

        }

    }

    /// <summary>
    /// Keyboard inputs with old and new input system.
    /// Simply read input values from the action maps (already enabled in OnEnable).
    /// </summary>
    private RCCP_Inputs KeyboardInputs() {

        // If for some reason there's no action asset, just return what we have.
        if (inputActionsInstance == null)
            return inputs;

        // read from the actions...
        var drivingMap = inputActionsInstance.actionMaps[0];
        inputs.throttleInput = drivingMap.actions[0].ReadValue<float>();
        inputs.brakeInput = drivingMap.actions[1].ReadValue<float>();
        inputs.steerInput = drivingMap.actions[2].ReadValue<float>();
        inputs.handbrakeInput = drivingMap.actions[3].ReadValue<float>();
        inputs.nosInput = drivingMap.actions[12].ReadValue<float>();
        inputs.clutchInput = drivingMap.actions[22].ReadValue<float>();

        // cameraMap is index [1], but we only read the Mouse Input from there
        var cameraMap = inputActionsInstance.actionMaps[1];
        inputs.mouseInput = cameraMap.actions[0].ReadValue<Vector2>();

        return inputs;

    }

    /// <summary>
    /// Receiving mobile player inputs from RCCP_MobileInputs (attached to RCCP_Canvas).
    /// </summary>
    private RCCP_Inputs MobileInputs() {

        RCCP_MobileInputs mobileInputs = RCCP_MobileInputs.Instance;

        if (mobileInputs) {

            inputs.throttleInput = mobileInputs.throttleInput;
            inputs.brakeInput = mobileInputs.brakeInput;
            inputs.steerInput = mobileInputs.steerInput;
            inputs.handbrakeInput = mobileInputs.ebrakeInput;
            inputs.nosInput = mobileInputs.nosInput;

        }

        return inputs;

    }

    #region Input Overrides
    public void OverrideInputs(RCCP_Inputs overriddenInputs) {

        overrideInputs = true;
        inputs = overriddenInputs;

    }

    public void DisableOverrideInputs() {

        overrideInputs = false;

    }

    /// <summary>
    /// Returns player inputs.
    /// </summary>
    public RCCP_Inputs GetInputs() {

        return inputs;

    }
    #endregion

    #region Public Methods Called by the Rest of the Game
    public void GearShiftUp() {

        if (OnGearShiftedUp != null)
            OnGearShiftedUp();

    }

    public void GearShiftDown() {

        if (OnGearShiftedDown != null)
            OnGearShiftedDown();

    }

    public void GearShiftToN() {

        if (OnGearShiftedToN != null)
            OnGearShiftedToN();

    }

    public void ToggleGear(RCCP_Gearbox.TransmissionType transmissionType) {

        if (OnGearToggle != null)
            OnGearToggle(transmissionType);

    }

    public void AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear semiAutomaticDNRPGear) {

        if (OnAutomaticGear != null)
            OnAutomaticGear(semiAutomaticDNRPGear);

    }

    public void ChangeCamera() {

        if (OnChangedCamera != null)
            OnChangedCamera();

    }

    public void LowBeamHeadlights() {

        if (OnPressedLowBeamLights != null)
            OnPressedLowBeamLights();

    }

    public void HighBeamHeadlights() {

        if (OnPressedHighBeamLights != null)
            OnPressedHighBeamLights();

    }

    public void IndicatorLeftlights() {

        if (OnPressedLeftIndicatorLights != null)
            OnPressedLeftIndicatorLights();

    }

    public void IndicatorRightlights() {

        if (OnPressedRightIndicatorLights != null)
            OnPressedRightIndicatorLights();

    }

    public void Indicatorlights() {

        if (OnPressedIndicatorLights != null)
            OnPressedIndicatorLights();

    }

    public void LookBackCamera(bool state) {

        if (OnLookBackCamera != null)
            OnLookBackCamera(state);

    }

    public void HoldOrbitCamera(bool state) {

        if (OnHoldOrbitCamera != null)
            OnHoldOrbitCamera(state);

    }

    public void StartEngine() {

        if (OnStartEngine != null)
            OnStartEngine();

    }

    public void StopEngine() {

        if (OnStopEngine != null)
            OnStopEngine();

    }

    public void SteeringHelper() {

        if (OnSteeringHelper != null)
            OnSteeringHelper();

    }

    public void TractionHelper() {

        if (OnTractionHelper != null)
            OnTractionHelper();

    }

    public void AngularDragHelper() {

        if (OnAngularDragHelper != null)
            OnAngularDragHelper();

    }

    public void ABS() {

        if (OnABS != null)
            OnABS();

    }

    public void ESP() {

        if (OnESP != null)
            OnESP();

    }

    public void TCS() {

        if (OnTCS != null)
            OnTCS();

    }

    public void Record() {

        if (OnRecord != null)
            OnRecord();

    }

    public void Replay() {

        if (OnReplay != null)
            OnReplay();

    }

    public void TrailDetach() {

        if (OnTrailerDetach != null)
            OnTrailerDetach();

    }

    public void Options() {

        if (OnOptions != null)
            OnOptions();
    }
    #endregion

    #region Callbacks for Input System (Instance-based versions)
    // Driving map events
    private void GearShiftUp_performed(InputAction.CallbackContext ctx) => GearShiftUp();
    private void GearShiftDown_performed(InputAction.CallbackContext ctx) => GearShiftDown();
    private void NGear_performed(InputAction.CallbackContext ctx) => GearShiftToN();

    private void _1stGear_performed(InputAction.CallbackContext ctx) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(0);

    }

    private void _2ndGear_performed(InputAction.CallbackContext ctx) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(1);

    }

    private void _3rdGear_performed(InputAction.CallbackContext ctx) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(2);

    }

    private void _4thGear_performed(InputAction.CallbackContext ctx) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(3);

    }

    private void _5thGear_performed(InputAction.CallbackContext ctx) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(4);

    }

    private void _6thGear_performed(InputAction.CallbackContext ctx) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(5);

    }

    private void _RGear_performed(InputAction.CallbackContext ctx) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(-1);

    }

    private void TrailDetach_performed(InputAction.CallbackContext ctx) => TrailDetach();

    // Camera map events
    private void ChangeCamera_performed(InputAction.CallbackContext ctx) => ChangeCamera();
    private void LookBackCamera_performed(InputAction.CallbackContext ctx) => LookBackCamera(true);
    private void LookBackCamera_canceled(InputAction.CallbackContext ctx) => LookBackCamera(false);

    private void HoldOrbitCamera_performed(InputAction.CallbackContext ctx) => HoldOrbitCamera(true);
    private void HoldOrbitCamera_canceled(InputAction.CallbackContext ctx) => HoldOrbitCamera(false);

    // Lights/Engine in driving map
    private void StartEngine_performed(InputAction.CallbackContext ctx) => StartEngine();
    private void LowBeamHeadlights_performed(InputAction.CallbackContext ctx) => LowBeamHeadlights();
    private void HighBeamHeadlights_performed(InputAction.CallbackContext ctx) => HighBeamHeadlights();
    private void IndicatorLeftlights_performed(InputAction.CallbackContext ctx) => IndicatorLeftlights();
    private void IndicatorRightlights_performed(InputAction.CallbackContext ctx) => IndicatorRightlights();
    private void Indicatorlights_performed(InputAction.CallbackContext ctx) => Indicatorlights();

    // Debug map (record/replay)
    private void Record_performed(InputAction.CallbackContext ctx) => Record();
    private void Replay_performed(InputAction.CallbackContext ctx) => Replay();
    #endregion

    #region Subscribe / Unsubscribe Helpers

    private void SubscribeDrivingMapEvents() {

        // "Driving" might be your first action map (index 0):
        var drivingMap = inputActionsInstance.actionMaps[0];

        drivingMap.actions[4].performed += StartEngine_performed;            // "StartEngine"
        drivingMap.actions[5].performed += LowBeamHeadlights_performed;      // "LowBeam"
        drivingMap.actions[6].performed += HighBeamHeadlights_performed;     // "HighBeam"
        drivingMap.actions[7].performed += IndicatorRightlights_performed;
        drivingMap.actions[8].performed += IndicatorLeftlights_performed;
        drivingMap.actions[9].performed += Indicatorlights_performed;

        drivingMap.actions[10].performed += GearShiftUp_performed;           // "GearShiftUp"
        drivingMap.actions[11].performed += GearShiftDown_performed;         // "GearShiftDown"
        drivingMap.actions[13].performed += TrailDetach_performed;           // "TrailDetach"
        drivingMap.actions[14].performed += NGear_performed;                 // "N Gear"
        drivingMap.actions[15].performed += _1stGear_performed;              // "1st gear"
        drivingMap.actions[16].performed += _2ndGear_performed;
        drivingMap.actions[17].performed += _3rdGear_performed;
        drivingMap.actions[18].performed += _4thGear_performed;
        drivingMap.actions[19].performed += _5thGear_performed;
        drivingMap.actions[20].performed += _6thGear_performed;
        drivingMap.actions[21].performed += _RGear_performed;

    }

    private void UnsubscribeDrivingMapEvents() {

        var drivingMap = inputActionsInstance.actionMaps[0];

        drivingMap.actions[4].performed -= StartEngine_performed;
        drivingMap.actions[5].performed -= LowBeamHeadlights_performed;
        drivingMap.actions[6].performed -= HighBeamHeadlights_performed;
        drivingMap.actions[7].performed -= IndicatorRightlights_performed;
        drivingMap.actions[8].performed -= IndicatorLeftlights_performed;
        drivingMap.actions[9].performed -= Indicatorlights_performed;

        drivingMap.actions[10].performed -= GearShiftUp_performed;
        drivingMap.actions[11].performed -= GearShiftDown_performed;
        drivingMap.actions[13].performed -= TrailDetach_performed;
        drivingMap.actions[14].performed -= NGear_performed;
        drivingMap.actions[15].performed -= _1stGear_performed;
        drivingMap.actions[16].performed -= _2ndGear_performed;
        drivingMap.actions[17].performed -= _3rdGear_performed;
        drivingMap.actions[18].performed -= _4thGear_performed;
        drivingMap.actions[19].performed -= _5thGear_performed;
        drivingMap.actions[20].performed -= _6thGear_performed;
        drivingMap.actions[21].performed -= _RGear_performed;

    }

    private void SubscribeCameraMapEvents() {

        // "Camera" might be your second action map (index 1)
        var cameraMap = inputActionsInstance.actionMaps[1];

        cameraMap.actions[1].performed += ChangeCamera_performed;              // "ChangeCamera"
        cameraMap.actions[2].performed += LookBackCamera_performed;            // "LookBackCamera"
        cameraMap.actions[2].canceled += LookBackCamera_canceled;
        cameraMap.actions[4].performed += HoldOrbitCamera_performed;           // "OrbitCamera"
        cameraMap.actions[4].canceled += HoldOrbitCamera_canceled;

    }

    private void UnsubscribeCameraMapEvents() {

        var cameraMap = inputActionsInstance.actionMaps[1];

        cameraMap.actions[1].performed -= ChangeCamera_performed;
        cameraMap.actions[2].performed -= LookBackCamera_performed;
        cameraMap.actions[2].canceled -= LookBackCamera_canceled;
        cameraMap.actions[4].performed -= HoldOrbitCamera_performed;
        cameraMap.actions[4].canceled -= HoldOrbitCamera_canceled;

    }

    private void SubscribeReplayMapEvents() {

        // "Replay" might be your third action map (index 2)
        var replayMap = inputActionsInstance.actionMaps[2];

        replayMap.actions[0].performed += Record_performed;
        replayMap.actions[1].performed += Replay_performed;

    }

    private void UnsubscribeReplayMapEvents() {

        var replayMap = inputActionsInstance.actionMaps[2];

        replayMap.actions[0].performed -= Record_performed;
        replayMap.actions[1].performed -= Replay_performed;

    }

    #endregion

}
