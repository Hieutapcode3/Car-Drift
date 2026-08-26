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
    [Tooltip("Phần trăm tăng thêm so với chỉ số gốc của xe (VD: 4 = +4%, 20 = +20%)")]
    public float percentBonus;

    [Obsolete("Use percentBonus instead.")]
    public float statBonus => percentBonus;
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

    /// <summary>
    /// Lấy % tăng thêm tại cấp độ level (1..5). Nếu level <= 0 trả về 0%.
    /// </summary>
    public float GetPercentBonus(UpgradeType type, int level)
    {
        if (level <= 0) return 0f;
        UpgradeLevelData data = GetUpgradeData(type, level - 1);
        return data.percentBonus;
    }

    /// <summary>
    /// Tính toán chỉ số sau khi nâng cấp: BaseStat * (1 + %bonus / 100)
    /// </summary>
    public float CalculateUpgradedStat(float baseStat, UpgradeType type, int level)
    {
        if (level <= 0) return baseStat;
        float percent = GetPercentBonus(type, level);
        return baseStat * (1f + percent / 100f);
    }

    /// <summary>
    /// Tính toán lượng chỉ số cộng thêm: BaseStat * (%bonus / 100)
    /// </summary>
    public float CalculateBonusValue(float baseStat, UpgradeType type, int level)
    {
        if (level <= 0) return 0f;
        float percent = GetPercentBonus(type, level);
        return baseStat * (percent / 100f);
    }

    private void Reset()
    {
        InitializeDefaultValues();
    }

    [ContextMenu("Populate Default Levels")]
    public void InitializeDefaultValues()
    {
        // Mặc định mỗi cấp tăng 4% (Level 1: 4%, Level 2: 8%, ..., Level 5: 20%) khớp với efficiency 1.2 của RCCP
        engineLevels = CreateDefaultArray(4f);
        handlingLevels = CreateDefaultArray(4f);
        brakeLevels = CreateDefaultArray(4f);
        speedLevels = CreateDefaultArray(4f);
    }

    private UpgradeLevelData[] CreateDefaultArray(float percentPerLevel)
    {
        UpgradeLevelData[] arr = new UpgradeLevelData[MAX_LEVEL];
        for (int i = 0; i < MAX_LEVEL; i++)
        {
            arr[i] = new UpgradeLevelData
            {
                level = i + 1,
                costGold = (i + 1) * 500,
                costSilver = (i + 1) * 1000,
                percentBonus = (i + 1) * percentPerLevel
            };
        }
        return arr;
    }
}
