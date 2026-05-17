//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(RCCP_WheelCollider))]
[CanEditMultipleObjects]
public class RCCP_WheelColliderEditor : Editor {

    RCCP_WheelCollider prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    // Serialized properties
    SerializedProperty alignWheels;
    SerializedProperty connectedAxle;
    SerializedProperty wheelModel;
    SerializedProperty offset;
    SerializedProperty camber;
    SerializedProperty caster;
    SerializedProperty width;
    SerializedProperty drawSkid;
    SerializedProperty deflated;
    SerializedProperty deflatedRadiusMultiplier;
    SerializedProperty deflatedStiffnessMultiplier;
    SerializedProperty driftMode;
    SerializedProperty wheelbase;
    SerializedProperty trackWidth;

    SerializedProperty isGrounded;
    SerializedProperty isSkidding;
    SerializedProperty groundIndex;
    SerializedProperty motorTorque;
    SerializedProperty brakeTorque;
    SerializedProperty steerInput;
    SerializedProperty handbrakeInput;
    SerializedProperty wheelRPM2Speed;
    SerializedProperty totalSlip;
    SerializedProperty wheelSlipAmountForward;
    SerializedProperty wheelSlipAmountSideways;
    SerializedProperty totalWheelTemp;
    SerializedProperty bumpForce;

    bool showStatistics = true;     // Toggle for runtime statistics foldout.

    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

        // Fetch the SerializedObject and find properties by name.
        alignWheels = serializedObject.FindProperty("alignWheels");
        connectedAxle = serializedObject.FindProperty("connectedAxle");
        wheelModel = serializedObject.FindProperty("wheelModel");
        offset = serializedObject.FindProperty("offset");
        camber = serializedObject.FindProperty("camber");
        caster = serializedObject.FindProperty("caster");
        width = serializedObject.FindProperty("width");
        drawSkid = serializedObject.FindProperty("drawSkid");
        deflated = serializedObject.FindProperty("deflated");
        deflatedRadiusMultiplier = serializedObject.FindProperty("deflatedRadiusMultiplier");
        deflatedStiffnessMultiplier = serializedObject.FindProperty("deflatedStiffnessMultiplier");
        driftMode = serializedObject.FindProperty("driftMode");
        wheelbase = serializedObject.FindProperty("wheelbase");
        trackWidth = serializedObject.FindProperty("trackWidth");

        // Runtime stats
        isGrounded = serializedObject.FindProperty("isGrounded");
        isSkidding = serializedObject.FindProperty("isSkidding");
        groundIndex = serializedObject.FindProperty("groundIndex");
        motorTorque = serializedObject.FindProperty("motorTorque");
        brakeTorque = serializedObject.FindProperty("brakeTorque");
        steerInput = serializedObject.FindProperty("steerInput");
        handbrakeInput = serializedObject.FindProperty("handbrakeInput");
        wheelRPM2Speed = serializedObject.FindProperty("wheelRPM2Speed");
        totalSlip = serializedObject.FindProperty("totalSlip");
        wheelSlipAmountForward = serializedObject.FindProperty("wheelSlipAmountForward");
        wheelSlipAmountSideways = serializedObject.FindProperty("wheelSlipAmountSideways");
        totalWheelTemp = serializedObject.FindProperty("totalWheelTemp");
        bumpForce = serializedObject.FindProperty("bumpForce");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_WheelCollider)target;
        serializedObject.Update();
        GUI.skin = skin;

        //DrawDefaultInspector();

        // Main properties
        EditorGUILayout.PropertyField(
            connectedAxle,
            new GUIContent("Connected Axle", "Specify which axle this wheel belongs to (front, rear, etc.).")
        );
        EditorGUILayout.PropertyField(
            wheelModel,
            new GUIContent("Wheel Model", "The visual transform representing this wheel.")
        );
        EditorGUILayout.PropertyField(
            alignWheels,
            new GUIContent("Align Wheels", "If true, the visual wheel model will match the WheelCollider position/rotation every frame.")
        );
        EditorGUILayout.Space();

        // Wheel Setup
        EditorGUILayout.LabelField("Wheel Setup", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(
            offset,
            new GUIContent("Wheel Offset", "Horizontal offset of the wheel for visual adjustment (e.g., rim offset).")
        );
        EditorGUILayout.PropertyField(
            camber,
            new GUIContent("Camber Angle", "Tilt of the wheel relative to the vertical axis.")
        );
        EditorGUILayout.PropertyField(
            caster,
            new GUIContent("Caster Angle", "Front-to-back tilt of the wheel's steering pivot axis.")
        );
        EditorGUILayout.PropertyField(
            width,
            new GUIContent("Wheel Width", "Width of the wheel used for skids and visual representation.")
        );
        EditorGUILayout.Space();

        // Deflation Settings
        EditorGUILayout.LabelField("Deflation Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(
            deflated,
            new GUIContent("Deflated", "Indicates if the wheel is currently deflated.")
        );
        EditorGUILayout.PropertyField(
            deflatedRadiusMultiplier,
            new GUIContent("Deflated Radius Multiplier", "Scale factor for the wheel radius when deflated.")
        );
        EditorGUILayout.PropertyField(
            deflatedStiffnessMultiplier,
            new GUIContent("Deflated Stiffness Multiplier", "Scale factor for the wheel friction stiffness when deflated.")
        );
        EditorGUILayout.Space();

        // Additional Options
        EditorGUILayout.LabelField("Additional Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(
            drawSkid,
            new GUIContent("Draw Skidmarks", "If enabled, this wheel will generate skidmarks under high slip conditions.")
        );
        EditorGUILayout.PropertyField(
            driftMode,
            new GUIContent("Drift Mode", "If enabled, friction curves will be modified to allow drifting behavior.")
        );
        EditorGUILayout.Space();

        // Steering / Dimensions
        EditorGUILayout.LabelField("Steering / Dimensions", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(
            wheelbase,
            new GUIContent("Wheelbase", "Distance between the front and rear axles, used for Ackermann steering.")
        );
        EditorGUILayout.PropertyField(
            trackWidth,
            new GUIContent("Track Width", "Distance between the left and right wheels on this axle.")
        );
        EditorGUILayout.Space();

        // Statistics foldout
        showStatistics = EditorGUILayout.Foldout(showStatistics, "Runtime Statistics");
        if (showStatistics) {
            if (Application.isPlaying) {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("Ground Status", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    isGrounded,
                    new GUIContent("Is Grounded", "Is the wheel currently in contact with a surface?")
                );
                EditorGUILayout.PropertyField(
                    isSkidding,
                    new GUIContent("Is Skidding", "Is the wheel slipping above a certain threshold?")
                );
                EditorGUILayout.PropertyField(
                    groundIndex,
                    new GUIContent("Ground Material Index", "Index of the ground material currently under the wheel.")
                );

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Input / Forces", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    motorTorque,
                    new GUIContent("Motor Torque", "Motor torque applied to this wheel (Nm).")
                );
                EditorGUILayout.PropertyField(
                    brakeTorque,
                    new GUIContent("Brake Torque", "Brake torque applied to this wheel (Nm).")
                );
                EditorGUILayout.PropertyField(
                    steerInput,
                    new GUIContent("Steer Input", "Raw steering angle input for this wheel (degrees).")
                );
                EditorGUILayout.PropertyField(
                    handbrakeInput,
                    new GUIContent("Handbrake Input", "Handbrake input ranging from 0 to 1.")
                );

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Slip & Speed", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    wheelRPM2Speed,
                    new GUIContent("Wheel Speed (km/h)", "Approximate wheel speed calculated from RPM.")
                );
                EditorGUILayout.PropertyField(
                    totalSlip,
                    new GUIContent("Total Slip", "Combined slip magnitude (forward + sideways).")
                );
                EditorGUILayout.PropertyField(
                    wheelSlipAmountForward,
                    new GUIContent("Forward Slip", "Magnitude of longitudinal slip.")
                );
                EditorGUILayout.PropertyField(
                    wheelSlipAmountSideways,
                    new GUIContent("Sideways Slip", "Magnitude of lateral slip.")
                );

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Temperature & Bump", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    totalWheelTemp,
                    new GUIContent("Wheel Temperature", "Thermal buildup or stress level of the wheel.")
                );
                EditorGUILayout.PropertyField(
                    bumpForce,
                    new GUIContent("Bump Force", "Calculated difference in collision force for impact/bump sounds.")
                );

                EditorGUI.indentLevel--;
            } else {
                EditorGUILayout.HelpBox("Runtime statistics are available in Play Mode only.", MessageType.Info);
            }
        }

        if (BehaviorSelected())
            GUI.color = Color.red;

        GUI.color = guiColor;

        CheckMisconfig();

        if (!EditorUtility.IsPersistent(prop)) {

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;

            if (!EditorApplication.isPlaying && prop.connectedAxle != null && prop.connectedAxle.autoAlignWheelColliders)
                prop.AlignWheel();

        }

        if (BehaviorSelected())
            EditorGUILayout.HelpBox("Settings with red labels and frictions of the wheelcolliders will be overridden by the selected behavior in RCCP_Settings", MessageType.None);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

    private void CheckMisconfig() {

        bool completeSetup = true;
        errorMessages.Clear();

        if (!prop.connectedAxle)
            errorMessages.Add("Axle not selected");

        if (!prop.wheelModel)
            errorMessages.Add("Wheel model not selected");

        if (errorMessages.Count > 0)
            completeSetup = false;

        prop.completeSetup = completeSetup;

        if (!completeSetup)
            EditorGUILayout.HelpBox("Errors found!", MessageType.Error, true);

        GUI.color = Color.red;

        for (int i = 0; i < errorMessages.Count; i++) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(errorMessages[i]);
            EditorGUILayout.EndVertical();

        }

        GUI.color = guiColor;

    }

    private bool BehaviorSelected() {

        bool state = RCCP_Settings.Instance.overrideBehavior;

        if (prop.GetComponentInParent<RCCP_CarController>(true).ineffectiveBehavior)
            state = false;

        return state;

    }

}
