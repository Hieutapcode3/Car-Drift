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
/// UI paint button. 
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Modification/RCCP UI Color Button")]
public class RCCP_UI_Color : RCCP_UIComponent {

    /// <summary>
    /// Picked color type.
    /// </summary>
    public CarColorType colorType = CarColorType.Red;

    public void OnClick() {

        //  Finding the player vehicle.
        RCCP_CarController playerVehicle = RCCPSceneManager.activePlayerVehicle;

        //  If no player vehicle found, return.
        if (!playerVehicle)
            return;

        //  If player vehicle doesn't have the customizer component, return.
        if (!playerVehicle.Customizer)
            return;

        if (!playerVehicle.Customizer.PaintManager)
            return;

        //  Color.
        Color selectedColor = ColorParamSO.Instance != null ? ColorParamSO.Instance.GetColor(colorType) : Color.white;

        playerVehicle.Customizer.PaintManager.Paint(selectedColor);

    }

}
