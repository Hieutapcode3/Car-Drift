using UnityEngine;

public static class CarSaveManager
{
    private const string KEY_SELECTED_CAR_INDEX = "Selected_Car_Index";

    public static int GetSelectedCarIndex()
    {
        return PlayerPrefs.GetInt(KEY_SELECTED_CAR_INDEX, 0);
    }

    public static void SetSelectedCarIndex(int index)
    {
        PlayerPrefs.SetInt(KEY_SELECTED_CAR_INDEX, index);
        PlayerPrefs.Save();
    }

    public static bool IsCarUnlocked(CarDataSO carData)
    {
        if (carData == null) return false;
        if (carData.isUnlockedByDefault) return true;

        string key = $"Car_{carData.carID}_Unlocked";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public static void UnlockCar(CarDataSO carData)
    {
        if (carData == null) return;
        string key = $"Car_{carData.carID}_Unlocked";
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    public static int GetUpgradeLevel(string carID, UpgradeType type)
    {
        string key = $"Car_{carID}_{type}_Level";
        return PlayerPrefs.GetInt(key, 0);
    }

    public static void SetUpgradeLevel(string carID, UpgradeType type, int level)
    {
        string key = $"Car_{carID}_{type}_Level";
        PlayerPrefs.SetInt(key, Mathf.Clamp(level, 0, UpgradeConfigSO.MAX_LEVEL));
        PlayerPrefs.Save();
    }

    public static int GetCustomIndex(string carID, CustomType type)
    {
        string key = $"Car_{carID}_{type}_Index";
        return PlayerPrefs.GetInt(key, -1);
    }

    public static void SetCustomIndex(string carID, CustomType type, int index)
    {
        string key = $"Car_{carID}_{type}_Index";
        PlayerPrefs.SetInt(key, index);
        PlayerPrefs.Save();
    }
}
