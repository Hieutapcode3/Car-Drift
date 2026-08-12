using System;
using UnityEngine;

public enum CustomType
{
    Wheels,
    Paint,
    Spoiler,
    Neon
}

[Serializable]
public struct WheelCustomItem
{
    public int priceGold;
    public Sprite icon;
}

[Serializable]
public struct PaintCustomItem
{
    public int priceGold;
    public CarColorType colorType;

    public Color color => ColorParamSO.Instance != null ? ColorParamSO.Instance.GetColor(colorType) : Color.white;
}

[Serializable]
public struct SpoilerCustomItem
{
    public int indexInConfig;
    public int priceGold;
    public Sprite icon;
}

[Serializable]
public struct NeonCustomItem
{
    public int priceGold;
    public Material neonMat;
}

[CreateAssetMenu(fileName = "CustomConfig", menuName = "CarDrift/Custom Config")]
public class CustomConfigSO : ScriptableObject
{
    private const string ResourcePath = "Data/CustomConfig";
    private static ResourceAsset<CustomConfigSO> asset = new ResourceAsset<CustomConfigSO>(ResourcePath);
    public static CustomConfigSO Instance => asset.Value;

    [Header("Wheels Options")]
    public WheelCustomItem[] wheels;

    [Header("Paint Colors Options")]
    public PaintCustomItem[] paints;

    [Header("Spoiler Options")]
    public SpoilerCustomItem[] spoilers;

    [Header("Neon Light Options")]
    public NeonCustomItem[] neons;
}
