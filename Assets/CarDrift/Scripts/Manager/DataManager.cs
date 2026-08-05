using System;
using System.Collections.Generic;
using UnityCommunity.UnitySingleton;
using UnityEngine;

[Serializable]
public class PlayerGameData
{
    public int coins = 5000;
    public CarType selectedCar = CarType.Coupe;
    public ControlScheme controlScheme = ControlScheme.Keyboard;
    public bool soundEnabled = true;
    public bool tireSmokeEnabled = true;
    public List<CarType> unlockedCars = new List<CarType>();
    public List<CarUpgradeData> carUpgrades = new List<CarUpgradeData>();
    public List<RaceRecordData> highScores = new List<RaceRecordData>();
}

[Serializable]
public class CarUpgradeData
{
    public CarType carType;
    [Range(1, 5)] public int speedLevel = 1;
    [Range(1, 5)] public int accelerationLevel = 1;
    [Range(1, 5)] public int brakeLevel = 1;
    [Range(1, 5)] public int driftLevel = 1;

    public CarUpgradeData(CarType carType)
    {
        this.carType = carType;
    }
}

[Serializable]
public class RaceRecordData
{
    public string mapId;
    public float bestTime;
    public int bestDriftScore;
}

public class DataManager : MonoSingleton<DataManager>
{
    private const string SaveKey = "CarDrift.PlayerData";

    [SerializeField] private PlayerGameData data = new PlayerGameData();

    public PlayerGameData Data => data;
    public int Coins => data.coins;
    public CarType SelectedCar => data.selectedCar;

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
        {
            DontDestroyOnLoad(gameObject);
            Load();
        }
    }

    public void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        data = string.IsNullOrEmpty(json) ? CreateDefaultData() : JsonUtility.FromJson<PlayerGameData>(json);
        ValidateData();
    }

    public void Save()
    {
        ValidateData();
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public void ResetData()
    {
        data = CreateDefaultData();
        Save();
    }

    public bool IsUnlocked(CarType carType)
    {
        return data.unlockedCars.Contains(carType);
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0)
            return true;

        if (data.coins < amount)
            return false;

        data.coins -= amount;
        Save();
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        data.coins += amount;
        Save();
    }

    public bool UnlockCar(CarType carType, int cost)
    {
        if (IsUnlocked(carType))
            return true;

        if (!SpendCoins(cost))
            return false;

        data.unlockedCars.Add(carType);
        GetOrCreateUpgrade(carType);
        Save();
        return true;
    }

    public bool SelectCar(CarType carType)
    {
        if (!IsUnlocked(carType))
            return false;

        data.selectedCar = carType;
        Save();
        return true;
    }

    public CarUpgradeData GetOrCreateUpgrade(CarType carType)
    {
        CarUpgradeData upgrade = data.carUpgrades.Find(x => x.carType == carType);
        if (upgrade != null)
            return upgrade;

        upgrade = new CarUpgradeData(carType);
        data.carUpgrades.Add(upgrade);
        return upgrade;
    }

    public bool UpgradeCar(CarType carType, CarUpgradeType upgradeType, int cost)
    {
        CarUpgradeData upgrade = GetOrCreateUpgrade(carType);
        int currentLevel = GetUpgradeLevel(upgrade, upgradeType);

        if (currentLevel >= 5 || !SpendCoins(cost))
            return false;

        SetUpgradeLevel(upgrade, upgradeType, currentLevel + 1);
        Save();
        return true;
    }

    public int GetUpgradeLevel(CarUpgradeData upgrade, CarUpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case CarUpgradeType.Speed:
                return upgrade.speedLevel;
            case CarUpgradeType.Acceleration:
                return upgrade.accelerationLevel;
            case CarUpgradeType.Brake:
                return upgrade.brakeLevel;
            case CarUpgradeType.Drift:
                return upgrade.driftLevel;
            default:
                return 1;
        }
    }

    public void SaveRaceResult(string mapId, float time, int driftScore)
    {
        RaceRecordData record = data.highScores.Find(x => x.mapId == mapId);
        if (record == null)
        {
            record = new RaceRecordData { mapId = mapId, bestTime = time, bestDriftScore = driftScore };
            data.highScores.Add(record);
        }
        else
        {
            if (record.bestTime <= 0f || time < record.bestTime)
                record.bestTime = time;

            if (driftScore > record.bestDriftScore)
                record.bestDriftScore = driftScore;
        }

        Save();
    }

    private void SetUpgradeLevel(CarUpgradeData upgrade, CarUpgradeType upgradeType, int level)
    {
        level = Mathf.Clamp(level, 1, 5);

        switch (upgradeType)
        {
            case CarUpgradeType.Speed:
                upgrade.speedLevel = level;
                break;
            case CarUpgradeType.Acceleration:
                upgrade.accelerationLevel = level;
                break;
            case CarUpgradeType.Brake:
                upgrade.brakeLevel = level;
                break;
            case CarUpgradeType.Drift:
                upgrade.driftLevel = level;
                break;
        }
    }

    private PlayerGameData CreateDefaultData()
    {
        PlayerGameData defaultData = new PlayerGameData();
        defaultData.unlockedCars.Add(defaultData.selectedCar);
        defaultData.carUpgrades.Add(new CarUpgradeData(defaultData.selectedCar));
        return defaultData;
    }

    private void ValidateData()
    {
        if (data == null)
            data = CreateDefaultData();

        if (data.unlockedCars == null)
            data.unlockedCars = new List<CarType>();

        if (data.carUpgrades == null)
            data.carUpgrades = new List<CarUpgradeData>();

        if (data.highScores == null)
            data.highScores = new List<RaceRecordData>();

        if (!data.unlockedCars.Contains(data.selectedCar))
            data.unlockedCars.Add(data.selectedCar);

        foreach (CarType carType in data.unlockedCars)
            GetOrCreateUpgrade(carType);

        foreach (CarUpgradeData upgrade in data.carUpgrades)
        {
            upgrade.speedLevel = Mathf.Clamp(upgrade.speedLevel, 1, 5);
            upgrade.accelerationLevel = Mathf.Clamp(upgrade.accelerationLevel, 1, 5);
            upgrade.brakeLevel = Mathf.Clamp(upgrade.brakeLevel, 1, 5);
            upgrade.driftLevel = Mathf.Clamp(upgrade.driftLevel, 1, 5);
        }
    }
}
