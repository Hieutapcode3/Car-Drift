using System;
using System.Collections.Generic;
using UnityEngine;

public static class CarSaveManager
{
    private const string KEY_SELECTED_CAR_INDEX = "Selected_Car_Index";

    public static event Action OnCustomizationUpdated;

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
        NotifyCustomizationUpdated();
    }

    public static bool IsCustomUnlocked(CustomType type, int itemIndex, int priceGold = 0)
    {
        if (priceGold <= 0) return true;
        string key = $"Custom_{type}_{itemIndex}_Unlocked";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public static void UnlockCustom(CustomType type, int itemIndex)
    {
        string key = $"Custom_{type}_{itemIndex}_Unlocked";
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        NotifyCustomizationUpdated();
    }

    public static void NotifyCustomizationUpdated()
    {
        OnCustomizationUpdated?.Invoke();
    }
    public static void ClearAllCustomData(IList<CarDataSO> cars, CustomConfigSO customConfig)
    {
        if (customConfig == null) return;
        CustomType[] types = (CustomType[])Enum.GetValues(typeof(CustomType));

        int maxItems = 0;
        if (customConfig.wheels != null) maxItems = Mathf.Max(maxItems, customConfig.wheels.Length);
        if (customConfig.paints != null) maxItems = Mathf.Max(maxItems, customConfig.paints.Length);
        if (customConfig.spoilers != null) maxItems = Mathf.Max(maxItems, customConfig.spoilers.Length);
        if (customConfig.neons != null) maxItems = Mathf.Max(maxItems, customConfig.neons.Length);

        foreach (CustomType type in types)
        {
            for (int i = 0; i < maxItems; i++)
            {
                string unlockKey = $"Custom_{type}_{i}_Unlocked";
                PlayerPrefs.DeleteKey(unlockKey);
            }
        }
        if (cars != null)
        {
            foreach (CarDataSO car in cars)
            {
                if (car == null) continue;
                foreach (CustomType type in types)
                {
                    string indexKey = $"Car_{car.carID}_{type}_Index";
                    PlayerPrefs.DeleteKey(indexKey);
                }
            }
        }

        PlayerPrefs.Save();
        NotifyCustomizationUpdated();
    }
}
