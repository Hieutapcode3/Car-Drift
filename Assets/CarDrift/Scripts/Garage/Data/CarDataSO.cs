using UnityEngine;

public enum CarRank
{
    D,
    C,
    B,
    A,
    S,
    SS
}

[CreateAssetMenu(fileName = "NewCarData", menuName = "CarDrift/Car Data")]
public class CarDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string carID = "car_0";
    public string carName = "Default Car";
    public CarType carType = CarType.Sedan;
    public CarRank rank = CarRank.D;

    [Header("Unlock & Cost")]
    public int unlockCostGold = 1000;
    public bool isUnlockedByDefault = false;

    [Header("Base Stats")]
    public float baseMaxSpeed = 180f;
    public float baseTorque = 300f;
    public float baseBrake = 2000f;
    public float baseHandling = 1f;

    /// <summary>
    /// Key dùng để load prefab từ Addressables.
    /// Trùng với tên CarType (vd: "Jeep", "GTR", "M5_E30").
    /// </summary>
    public string AddressableKey => carType.ToString();
}
