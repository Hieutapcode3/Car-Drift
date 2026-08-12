using System;
using UnityEngine;

public enum UpgradeType
{
    Engine,
    Handling,
    Brake,
    Speed
}

[Serializable]
public struct UpgradeLevelData
{
    public int level;
    public int costGold;
    public int costSilver;
    public float statBonus;
}

[CreateAssetMenu(fileName = "UpgradeConfig", menuName = "CarDrift/Upgrade Config")]
public class UpgradeConfigSO : ScriptableObject
{
    public const int MAX_LEVEL = 5;

    [Header("Engine Upgrades (Level 1..5)")]
    public UpgradeLevelData[] engineLevels = new UpgradeLevelData[MAX_LEVEL];

    [Header("Handling Upgrades (Level 1..5)")]
    public UpgradeLevelData[] handlingLevels = new UpgradeLevelData[MAX_LEVEL];

    [Header("Brake Upgrades (Level 1..5)")]
    public UpgradeLevelData[] brakeLevels = new UpgradeLevelData[MAX_LEVEL];

    [Header("Speed Upgrades (Level 1..5)")]
    public UpgradeLevelData[] speedLevels = new UpgradeLevelData[MAX_LEVEL];

    public UpgradeLevelData GetUpgradeData(UpgradeType type, int levelIndex)
    {
        // levelIndex: 0-based for level 1 (index 0) to level 5 (index 4)
        int idx = Mathf.Clamp(levelIndex, 0, MAX_LEVEL - 1);
        switch (type)
        {
            case UpgradeType.Engine:
                return engineLevels != null && idx < engineLevels.Length ? engineLevels[idx] : default;
            case UpgradeType.Handling:
                return handlingLevels != null && idx < handlingLevels.Length ? handlingLevels[idx] : default;
            case UpgradeType.Brake:
                return brakeLevels != null && idx < brakeLevels.Length ? brakeLevels[idx] : default;
            case UpgradeType.Speed:
                return speedLevels != null && idx < speedLevels.Length ? speedLevels[idx] : default;
            default:
                return default;
        }
    }

    private void Reset()
    {
        InitializeDefaultValues();
    }

    [ContextMenu("Populate Default Levels")]
    public void InitializeDefaultValues()
    {
        engineLevels = CreateDefaultArray("Engine", 20f);
        handlingLevels = CreateDefaultArray("Handling", 0.1f);
        brakeLevels = CreateDefaultArray("Brake", 200f);
        speedLevels = CreateDefaultArray("Speed", 10f);
    }

    private UpgradeLevelData[] CreateDefaultArray(string typeName, float baseBonus)
    {
        UpgradeLevelData[] arr = new UpgradeLevelData[MAX_LEVEL];
        for (int i = 0; i < MAX_LEVEL; i++)
        {
            arr[i] = new UpgradeLevelData
            {
                level = i + 1,
                costGold = (i + 1) * 500,
                costSilver = (i + 1) * 1000,
                statBonus = (i + 1) * baseBonus
            };
        }
        return arr;
    }
}
