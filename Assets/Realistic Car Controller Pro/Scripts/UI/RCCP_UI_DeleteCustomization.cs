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
/// UI delete customization button.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Modification/RCCP UI Delete Customization Button")]
public class RCCP_UI_DeleteCustomization : RCCP_UIComponent {

    /// <summary>
    /// Xóa TOÀN BỘ customization của xe:
    /// Paint (màu), Wheel, Upgrade, Spoiler, Siren, Customization, Decal, Neon.
    /// Nếu chỉ muốn xóa màu, hãy gọi DeleteColorOnly() thay thế.
    /// </summary>
    public void OnClick() {

        //  Finding the player vehicle.
        RCCP_CarController playerVehicle = RCCPSceneManager.activePlayerVehicle;

        //  If no player vehicle found, return.
        if (!playerVehicle)
            return;

        //  If player vehicle doesn't have the customizer component, return.
        if (!playerVehicle.Customizer)
            return;

        // Xóa toàn bộ: paint, wheel, upgrade, spoiler, siren, decal, neon, ...
        playerVehicle.Customizer.Delete();

    }

    /// <summary>
    /// Chỉ xóa phần màu sơn (Paint) của xe, giữ nguyên các cấu hình khác
    /// (wheel, upgrade, spoiler, siren, decal, neon, ...).
    /// </summary>
    public void DeleteColorOnly() {

        //  Finding the player vehicle.
        RCCP_CarController playerVehicle = RCCPSceneManager.activePlayerVehicle;

        //  If no player vehicle found, return.
        if (!playerVehicle)
            return;

        //  If player vehicle doesn't have the customizer component, return.
        if (!playerVehicle.Customizer)
            return;

        // Chỉ restore phần PaintManager (màu sơn), không ợnh hưởng các thành phần khác
        if (playerVehicle.Customizer.PaintManager)
            playerVehicle.Customizer.PaintManager.Restore();

    }

}
