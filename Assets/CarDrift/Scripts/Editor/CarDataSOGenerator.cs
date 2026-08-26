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
    }

    private static readonly CarConfig[] AllCars = new CarConfig[]
    {
        new CarConfig { carType = CarType.Sedan,           carName = "Street Phantom", rank = CarRank.C,  unlockCostGold = 3000,  isUnlockedByDefault = true },
        new CarConfig { carType = CarType.Coupe,           carName = "Velocity",       rank = CarRank.C,  unlockCostGold = 5000,  isUnlockedByDefault = false },
        new CarConfig { carType = CarType.M5_E30,          carName = "Legacy M5",      rank = CarRank.B,  unlockCostGold = 8000,  isUnlockedByDefault = false },
        new CarConfig { carType = CarType.GTR,             carName = "Nightstrike",    rank = CarRank.B,  unlockCostGold = 12000, isUnlockedByDefault = false },
        new CarConfig { carType = CarType.F1,              carName = "Apex F1",        rank = CarRank.A,  unlockCostGold = 18000, isUnlockedByDefault = false },
        new CarConfig { carType = CarType.Muscle,          carName = "Thunderbolt",    rank = CarRank.S,  unlockCostGold = 30000, isUnlockedByDefault = false },
        new CarConfig { carType = CarType.CTR,             carName = "Redline CTR",    rank = CarRank.S,  unlockCostGold = 40000, isUnlockedByDefault = false },
        new CarConfig { carType = CarType.M3_E46,          carName = "Driftline E46",  rank = CarRank.SS, unlockCostGold = 50000, isUnlockedByDefault = false },
        new CarConfig { carType = CarType.M3_E36,          carName = "Roadster E36",   rank = CarRank.SS, unlockCostGold = 60000, isUnlockedByDefault = false },
    };

    private const string VEHICLE_PREFAB_DIR = "Assets/Realistic Car Controller Pro/Addons/Installed/Demo Content/Prefabs/Vehicles";

    [MenuItem("GameEditor/Data/Generate & Sync Car Data SO from RCCP")]
    public static void GenerateAllCarDataSO()
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

            // Đọc trực tiếp các thông số chuẩn từ prefab của RCCP
            SyncStatsFromPrefab(carData);

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

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CarDataSOGenerator] 🎉 Hoàn tất! Đã đồng bộ {generatedCars.Count} CarDataSO từ RCCP Prefabs.");
    }

    public static void SyncStatsFromPrefab(CarDataSO carData)
    {
        if (carData == null) return;

        string prefabPath = $"{VEHICLE_PREFAB_DIR}/{carData.carType}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[CarDataSOGenerator] ⚠️ Không tìm thấy prefab tại {prefabPath}");
            return;
        }

        RCCP_CarController rccp = prefab.GetComponent<RCCP_CarController>();
        if (rccp == null)
            rccp = prefab.GetComponentInChildren<RCCP_CarController>(true);

        if (rccp != null)
        {
            // 1. Produced / Max Torque (Nm)
            if (rccp.Engine != null)
            {
                carData.baseTorque = rccp.Engine.maximumTorqueAsNM;
            }

            // 2. Brake Torque
            if (rccp.FrontAxle != null)
            {
                carData.baseBrake = rccp.FrontAxle.maxBrakeTorque;
            }
            else if (rccp.AxleManager != null && rccp.AxleManager.Axles != null && rccp.AxleManager.Axles.Count > 0)
            {
                carData.baseBrake = rccp.AxleManager.Axles[0].maxBrakeTorque;
            }

            // 3. Handling (Traction helper strength)
            if (rccp.Stability != null)
            {
                carData.baseHandling = rccp.Stability.tractionHelperStrength;
            }

            // 4. Max Speed theo đúng công thức tính toán vật lý của RCCP
            float maxRPM = rccp.Engine != null ? rccp.Engine.maxEngineRPM : rccp.maxEngineRPM;
            float lastGearRatio = 1f;
            if (rccp.Gearbox != null && rccp.Gearbox.gearRatios != null && rccp.Gearbox.gearRatios.Length > 0)
            {
                lastGearRatio = rccp.Gearbox.gearRatios[rccp.Gearbox.gearRatios.Length - 1];
            }
            else if (rccp.lastGearRatio > 0)
            {
                lastGearRatio = rccp.lastGearRatio;
            }

            float diffRatio = rccp.Differential != null ? rccp.Differential.finalDriveRatio : 3.73f;
            float totalRadius = 0f;
            int count = 0;

            if (rccp.AxleManager != null && rccp.AxleManager.Axles != null)
            {
                foreach (var axle in rccp.AxleManager.Axles)
                {
                    if (axle == null) continue;
                    if (axle.leftWheelCollider != null && axle.leftWheelCollider.WheelCollider != null)
                    {
                        totalRadius += axle.leftWheelCollider.WheelCollider.radius;
                        count++;
                    }
                    if (axle.rightWheelCollider != null && axle.rightWheelCollider.WheelCollider != null && axle.rightWheelCollider.WheelCollider.radius > 0)
                    {
                        totalRadius += axle.rightWheelCollider.WheelCollider.radius;
                        count++;
                    }
                }
            }

            float wheelRadius = count > 0 ? (totalRadius / count) : 0.33f;

            if (lastGearRatio > 0 && diffRatio > 0)
            {
                carData.baseMaxSpeed = Mathf.Round((maxRPM / lastGearRatio / diffRatio) * (2f * Mathf.PI * wheelRadius) * 60f / 1000f);
            }
        }
    }
}
