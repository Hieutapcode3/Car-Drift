using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CarCatalogEntry
{
    public CarType carType;
    public string displayName;
    public int price = 0;
    public RCCP_CarController prefab;
}

public static class CarCatalog
{
    public const string VehiclePrefabFolder = "Assets/Realistic Car Controller Pro/Addons/Installed/Demo Content/Prefabs/Vehicles";

    public static CarType[] AllCars => (CarType[])Enum.GetValues(typeof(CarType));

    public static string GetDisplayName(CarType carType)
    {
        switch (carType)
        {
            case CarType.CTR:
                return "CTR";
            case CarType.F1:
                return "Formula 1";
            case CarType.M3_E36:
                return "BMW M3 E36";
            case CarType.M3_E46:
                return "BMW M3 E46";
            case CarType.M5_E30:
                return "BMW M5 E30";
            case CarType.GTR:
                return "Skyline GTR";
            case CarType.ClassicRoadster:
                return "Classic Roadster";
            case CarType.SUV2:
                return "SUV 2";
            default:
                return SplitPascalCase(carType.ToString().Replace('_', ' '));
        }
    }

    public static string GetPrefabName(CarType carType)
    {
        switch (carType)
        {
            case CarType.GTR:
                return "Model_Skyline by BUMSTRUM(3DMaesen)";
            case CarType.ClassicRoadster:
                return "Model_Sofie@Driving by BUMSTRUM(3DMaesen)";
            default:
                return carType.ToString();
        }
    }

    public static string GetPrefabPath(CarType carType)
    {
        return $"{VehiclePrefabFolder}/{GetPrefabName(carType)}.prefab";
    }

    public static List<CarCatalogEntry> CreateDefaultEntries()
    {
        List<CarCatalogEntry> entries = new List<CarCatalogEntry>();
        CarType[] allCars = AllCars;

        for (int i = 0; i < allCars.Length; i++)
        {
            entries.Add(new CarCatalogEntry
            {
                carType = allCars[i],
                displayName = GetDisplayName(allCars[i]),
                price = i == 0 ? 0 : 1500 + i * 500
            });
        }

        return entries;
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        List<char> result = new List<char>(value.Length + 4) { value[0] };

        for (int i = 1; i < value.Length; i++)
        {
            char current = value[i];
            char previous = value[i - 1];

            if (char.IsUpper(current) && !char.IsWhiteSpace(previous) && !char.IsUpper(previous))
                result.Add(' ');

            result.Add(current);
        }

        return new string(result.ToArray());
    }
}
