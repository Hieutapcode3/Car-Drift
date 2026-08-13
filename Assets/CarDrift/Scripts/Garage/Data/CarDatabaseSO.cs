using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CarDatabase", menuName = "CarDrift/Car Database")]
public class CarDatabaseSO : ScriptableObject
{
    private const string ResourcePath = "Data/CarDatabase";
    private static ResourceAsset<CarDatabaseSO> asset = new ResourceAsset<CarDatabaseSO>(ResourcePath);
    public static CarDatabaseSO Instance => asset.Value;

    [Header("Cars Registry")]
    public List<CarDataSO> cars = new List<CarDataSO>();

    [Header("Configurations")]
    public UpgradeConfigSO upgradeConfig;
    public CustomConfigSO customConfig;

    public CarDataSO GetCarByIndex(int index)
    {
        if (cars == null || cars.Count == 0) return null;
        int clampedIdx = Mathf.Clamp(index, 0, cars.Count - 1);
        return cars[clampedIdx];
    }

    public CarDataSO GetCarByID(string id)
    {
        if (cars == null) return null;
        return cars.Find(c => c != null && c.carID == id);
    }
}
