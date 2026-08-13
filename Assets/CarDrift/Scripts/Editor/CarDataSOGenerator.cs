using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class CarDataSOGenerator
{
    private struct CarConfig
    {
        public CarType carType;
        public string carName;
        public CarRank rank;
        public int unlockCostGold;
        public bool isUnlockedByDefault;
        public float baseMaxSpeed;
        public float baseTorque;
        public float baseBrake;
        public float baseHandling;
    }

    private static readonly CarConfig[] AllCars = new CarConfig[]
    {
        // new CarConfig { carType = CarType.Jeep,            carName = "Jeep",            rank = CarRank.D,  unlockCostGold = 0,     isUnlockedByDefault = true,  baseMaxSpeed = 234f, baseTorque = 280f, baseBrake = 1800f, baseHandling = 0.8f },
        // new CarConfig { carType = CarType.ClassicRoadster, carName = "Classic Roadster", rank = CarRank.D,  unlockCostGold = 2000,  isUnlockedByDefault = false, baseMaxSpeed = 257f, baseTorque = 300f, baseBrake = 2000f, baseHandling = 0.9f },
        new CarConfig { carType = CarType.Sedan,           carName = "Sedan",           rank = CarRank.C,  unlockCostGold = 3000,  isUnlockedByDefault = true, baseMaxSpeed = 257f, baseTorque = 320f, baseBrake = 2100f, baseHandling = 1.0f },
        new CarConfig { carType = CarType.Coupe,           carName = "Coupe",           rank = CarRank.C,  unlockCostGold = 5000,  isUnlockedByDefault = false, baseMaxSpeed = 267f, baseTorque = 340f, baseBrake = 2200f, baseHandling = 1.0f },
        new CarConfig { carType = CarType.M5_E30,          carName = "BMW M5 E30",      rank = CarRank.B,  unlockCostGold = 8000,  isUnlockedByDefault = false, baseMaxSpeed = 278f, baseTorque = 360f, baseBrake = 2400f, baseHandling = 1.1f },
        new CarConfig { carType = CarType.M3_E36,          carName = "BMW M3 E36",      rank = CarRank.SS, unlockCostGold = 60000, isUnlockedByDefault = false, baseMaxSpeed = 386f, baseTorque = 560f, baseBrake = 3600f, baseHandling = 1.5f },
        new CarConfig { carType = CarType.M3_E46,          carName = "BMW M3 E46",      rank = CarRank.SS, unlockCostGold = 50000, isUnlockedByDefault = false, baseMaxSpeed = 365f, baseTorque = 540f, baseBrake = 3400f, baseHandling = 1.4f },
        new CarConfig { carType = CarType.GTR,             carName = "Nissan GTR",      rank = CarRank.B,  unlockCostGold = 12000, isUnlockedByDefault = false, baseMaxSpeed = 302f, baseTorque = 400f, baseBrake = 2600f, baseHandling = 1.2f },
        // new CarConfig { carType = CarType.SUV2,            carName = "SUV Sport",       rank = CarRank.A,  unlockCostGold = 22000, isUnlockedByDefault = false, baseMaxSpeed = 322f, baseTorque = 420f, baseBrake = 2800f, baseHandling = 1.1f },
        new CarConfig { carType = CarType.Muscle,          carName = "Muscle Car",      rank = CarRank.S,  unlockCostGold = 30000, isUnlockedByDefault = false, baseMaxSpeed = 325f, baseTorque = 480f, baseBrake = 2500f, baseHandling = 1.0f },
        new CarConfig { carType = CarType.CTR,             carName = "Porsche CTR",     rank = CarRank.S,  unlockCostGold = 40000, isUnlockedByDefault = false, baseMaxSpeed = 354f, baseTorque = 520f, baseBrake = 3200f, baseHandling = 1.3f },
        new CarConfig { carType = CarType.F1,              carName = "Formula 1",       rank = CarRank.A,  unlockCostGold = 18000, isUnlockedByDefault = false, baseMaxSpeed = 316f, baseTorque = 450f, baseBrake = 3000f, baseHandling = 1.4f },
    };

    [MenuItem("GameEditor/Data/Generate All Car Data SO")]
    static void GenerateAllCarDataSO()
    {
        string folderPath = "Assets/Resources/Data/Cars";

        // Tạo thư mục nếu chưa có
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Data"))
                AssetDatabase.CreateFolder("Assets/Resources", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Data/Cars"))
                AssetDatabase.CreateFolder("Assets/Resources/Data", "Cars");
        }

        List<CarDataSO> generatedCars = new List<CarDataSO>();

        for (int i = 0; i < AllCars.Length; i++)
        {
            CarConfig cfg = AllCars[i];
            string assetName = $"CarData_{cfg.carType}";
            string assetPath = $"{folderPath}/{assetName}.asset";

            // Kiểm tra nếu asset đã tồn tại thì cập nhật, không thì tạo mới
            CarDataSO carData = AssetDatabase.LoadAssetAtPath<CarDataSO>(assetPath);
            bool isNew = carData == null;

            if (isNew)
            {
                carData = ScriptableObject.CreateInstance<CarDataSO>();
            }

            carData.carID = $"car_{(int)cfg.carType}";
            carData.carName = cfg.carName;
            carData.carType = cfg.carType;
            carData.rank = cfg.rank;
            carData.unlockCostGold = cfg.unlockCostGold;
            carData.isUnlockedByDefault = cfg.isUnlockedByDefault;
            carData.baseMaxSpeed = cfg.baseMaxSpeed;
            carData.baseTorque = cfg.baseTorque;
            carData.baseBrake = cfg.baseBrake;
            carData.baseHandling = cfg.baseHandling;

            if (isNew)
            {
                AssetDatabase.CreateAsset(carData, assetPath);
                Debug.Log($"[CarDataSOGenerator] ✅ Tạo mới: {assetPath}");
            }
            else
            {
                EditorUtility.SetDirty(carData);
                Debug.Log($"[CarDataSOGenerator] 🔄 Cập nhật: {assetPath}");
            }

            generatedCars.Add(carData);
        }

        // Tìm và gán vào CarDatabase
        string[] dbGuids = AssetDatabase.FindAssets("t:CarDatabaseSO");
        if (dbGuids.Length > 0)
        {
            string dbPath = AssetDatabase.GUIDToAssetPath(dbGuids[0]);
            CarDatabaseSO db = AssetDatabase.LoadAssetAtPath<CarDatabaseSO>(dbPath);

            if (db != null)
            {
                db.cars = generatedCars;

                // Tự động gán UpgradeConfig và CustomConfig nếu chưa có
                if (db.upgradeConfig == null)
                {
                    string[] upgradeGuids = AssetDatabase.FindAssets("t:UpgradeConfigSO");
                    if (upgradeGuids.Length > 0)
                    {
                        db.upgradeConfig = AssetDatabase.LoadAssetAtPath<UpgradeConfigSO>(
                            AssetDatabase.GUIDToAssetPath(upgradeGuids[0]));
                    }
                }

                if (db.customConfig == null)
                {
                    string[] customGuids = AssetDatabase.FindAssets("t:CustomConfigSO");
                    if (customGuids.Length > 0)
                    {
                        db.customConfig = AssetDatabase.LoadAssetAtPath<CustomConfigSO>(
                            AssetDatabase.GUIDToAssetPath(customGuids[0]));
                    }
                }

                EditorUtility.SetDirty(db);
                Debug.Log($"[CarDataSOGenerator] ✅ Đã gán {generatedCars.Count} xe vào CarDatabase tại: {dbPath}");
            }
        }
        else
        {
            Debug.LogWarning("[CarDataSOGenerator] ⚠️ Không tìm thấy CarDatabaseSO. Vui lòng tạo CarDatabase asset trước.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CarDataSOGenerator] 🎉 Hoàn tất! Đã tạo/cập nhật {generatedCars.Count} CarDataSO assets.");
    }
}
