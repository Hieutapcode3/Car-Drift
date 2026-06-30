using UnityEngine;

public static class CarUpgradeApplier
{
    public static void Apply(RCCP_CarController car, CarUpgradeData upgrade)
    {
        if (!car || upgrade == null)
            return;

        RCCP_Customizer customizer = car.Customizer;
        if (!customizer)
            customizer = car.gameObject.AddComponent<RCCP_Customizer>();

        RCCP_VehicleUpgrade_UpgradeManager upgradeManager = customizer.UpgradeManager;
        if (!upgradeManager)
        {
            GameObject upgradeRoot = new GameObject("CarDrift Upgrade Manager");
            upgradeRoot.transform.SetParent(car.transform, false);
            upgradeManager = upgradeRoot.AddComponent<RCCP_VehicleUpgrade_UpgradeManager>();
        }

        int speedLevel = ToRccpLevel(upgrade.speedLevel);
        int accelerationLevel = ToRccpLevel(upgrade.accelerationLevel);
        int brakeLevel = ToRccpLevel(upgrade.brakeLevel);
        int driftLevel = ToRccpLevel(upgrade.driftLevel);

        upgradeManager.UpgradeEngineWithoutSave(accelerationLevel);
        upgradeManager.UpgradeBrakeWithoutSave(brakeLevel);
        upgradeManager.UpgradeHandlingWithoutSave(driftLevel);
        upgradeManager.UpgradeSpeedWithoutSave(speedLevel);
    }

    private static int ToRccpLevel(int playerLevel)
    {
        return Mathf.Clamp(playerLevel, 1, 5) - 1;
    }
}
