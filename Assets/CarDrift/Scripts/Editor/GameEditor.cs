using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class GameEditor
{
    private const string sceneFolder = "Assets/CarDrift/Scenes/";
    [MenuItem("GameEditor/Scenes/LoadingScene")]
    static void OpenLoadingScene()
    {
        OpenScene(sceneFolder + "LoadingScene.unity");
    }
    [MenuItem("GameEditor/Scenes/InGameScene")]
    static void OpenInGameScene()
    {
        OpenScene(sceneFolder + "InGameScene.unity");
    }
    [MenuItem("GameEditor/Scenes/MenuScene")]
    static void OpenMenuScene()
    {
        OpenScene(sceneFolder + "MenuScene.unity");
    }

    static void OpenScene(string path)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
        }
    }

    [MenuItem("GameEditor/Data/Clear All Custom Data")]
    static void ClearAllCustomData()
    {
        if (!EditorUtility.DisplayDialog(
            "Clear All Custom Data",
            "Xóa TOÀN BỘ dữ liệu customization (Paint, Wheel, Spoiler, Neon) của TẤT CẢ xe.\n\nHành động này không thể hoàn tác!",
            "Xóa", "Hủy"))
        {
            return;
        }

        // Tìm tất cả CarDatabaseSO trong project
        string[] guids = AssetDatabase.FindAssets("t:CarDatabaseSO");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CarDatabaseSO db = AssetDatabase.LoadAssetAtPath<CarDatabaseSO>(path);
            if (db != null && db.cars != null && db.customConfig != null)
            {
                CarSaveManager.ClearAllCustomData(db.cars, db.customConfig);
                Debug.Log($"[GameEditor] Đã xóa custom data cho {db.cars.Count} xe từ {path}");
            }
        }

        // Nếu đang Play Mode, reset visual trên xe hiện tại
        if (Application.isPlaying)
        {
            RCCP_CarController playerVehicle = RCCP_SceneManager.Instance != null
                ? RCCP_SceneManager.Instance.activePlayerVehicle : null;
            if (playerVehicle != null && playerVehicle.Customizer != null)
            {
                playerVehicle.Customizer.Delete();
                Debug.Log("[GameEditor] Đã reset visual customization trên xe hiện tại");
            }
        }

        Debug.Log("[GameEditor] Clear All Custom Data hoàn tất!");
    }

    [MenuItem("GameEditor/Currency/Add 10,000 Gold")]
    static void Add10kGold()
    {
        CurrencyManager.AddGold(10000);
        Debug.Log($"[GameEditor] Gold hiện tại: {CurrencyManager.Gold}");
    }

    [MenuItem("GameEditor/Currency/Add 100,000 Gold")]
    static void Add100kGold()
    {
        CurrencyManager.AddGold(100000);
        Debug.Log($"[GameEditor] Gold hiện tại: {CurrencyManager.Gold}");
    }

    [MenuItem("GameEditor/Currency/Add 10,000 Silver")]
    static void Add10kSilver()
    {
        CurrencyManager.AddSilver(10000);
        Debug.Log($"[GameEditor] Silver hiện tại: {CurrencyManager.Silver}");
    }

    [MenuItem("GameEditor/Currency/Add 100,000 Silver")]
    static void Add100kSilver()
    {
        CurrencyManager.AddSilver(100000);
        Debug.Log($"[GameEditor] Silver hiện tại: {CurrencyManager.Silver}");
    }

    [MenuItem("GameEditor/Currency/Reset Currency (5,000 Gold, 10,000 Silver)")]
    static void ResetCurrency()
    {
        CurrencyManager.SetGold(5000);
        CurrencyManager.SetSilver(10000);
        Debug.Log($"[GameEditor] Đã reset Currency: Gold={CurrencyManager.Gold}, Silver={CurrencyManager.Silver}");
    }
}