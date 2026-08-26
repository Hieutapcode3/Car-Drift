using System.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPanel : Panel<MenuPanel>
{
    [Title("Currency")]
    [SerializeField] private TextMeshProUGUI goldAmountTxt;
    [SerializeField] private TextMeshProUGUI silverAmountTxt;
    [Title("Parameters")]
    [SerializeField] private TextMeshProUGUI carNameTxt;
    [SerializeField] private TextMeshProUGUI carRankTxt;

    [SerializeField] private TextMeshProUGUI carSpeedTxt;
    [SerializeField] private Image carSpeedFillImg;
    [SerializeField] private TextMeshProUGUI carTorqueTxt;
    [SerializeField] private Image carTorqueFillImg;
    [SerializeField] private TextMeshProUGUI carBrakeTxt;
    [SerializeField] private Image carBrakeFillImg;
    [SerializeField] private TextMeshProUGUI carHandlingTxt;
    [SerializeField] private Image carHandlingFillImg;


    private void OnEnable()
    {
        if (!Application.isPlaying) return;

        CurrencyManager.OnCurrencyChanged += UpdateCurrencyUI;
        CarSaveManager.OnCustomizationUpdated += OnCustomizationUpdated;
        GarageManager.OnCarChanged += OnCarChanged;
        UpdateCurrencyUI(CurrencyManager.Gold, CurrencyManager.Silver);
        RefreshCarInfoUI();
        // GarageManager.Instance.SetCarouselMode(false);

    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;

        CurrencyManager.OnCurrencyChanged -= UpdateCurrencyUI;
        CarSaveManager.OnCustomizationUpdated -= OnCustomizationUpdated;
        GarageManager.OnCarChanged -= OnCarChanged;
    }

    private void OnCustomizationUpdated()
    {
        RefreshCarInfoUI();
    }

    private void OnCarChanged(CarDataSO carData)
    {
        RefreshCarInfoUI(carData);
    }

    public void RefreshCarInfoUI(CarDataSO carData = null)
    {
        if (carData == null)
        {
            if (GarageManager.Instance != null)
            {
                carData = GarageManager.Instance.CurrentCarData;
            }
            if (carData == null)
            {
                CarDatabaseSO db = CarDatabaseSO.Instance;
                if (db != null)
                {
                    carData = db.GetCarByIndex(CarSaveManager.GetSelectedCarIndex());
                }
            }
        }

        if (carData == null) return;

        if (carNameTxt != null) carNameTxt.text = carData.carName;
        if (carRankTxt != null) carRankTxt.text = carData.rank.ToString();

        string id = carData.carID;
        int engineLvl = CarSaveManager.GetUpgradeLevel(id, UpgradeType.Engine);
        int handlingLvl = CarSaveManager.GetUpgradeLevel(id, UpgradeType.Handling);
        int brakeLvl = CarSaveManager.GetUpgradeLevel(id, UpgradeType.Brake);
        int speedLvl = CarSaveManager.GetUpgradeLevel(id, UpgradeType.Speed);

        UpgradeConfigSO cfg = null;
        if (GarageManager.Instance != null && GarageManager.Instance.carDatabase != null)
        {
            cfg = GarageManager.Instance.carDatabase.upgradeConfig;
        }
        if (cfg == null && CarDatabaseSO.Instance != null)
        {
            cfg = CarDatabaseSO.Instance.upgradeConfig;
        }

        float finalSpeed = (cfg != null) ? cfg.CalculateUpgradedStat(carData.baseMaxSpeed, UpgradeType.Speed, speedLvl) : carData.baseMaxSpeed;
        float finalTorque = (cfg != null) ? cfg.CalculateUpgradedStat(carData.baseTorque, UpgradeType.Engine, engineLvl) : carData.baseTorque;
        float finalBrake = (cfg != null) ? cfg.CalculateUpgradedStat(carData.baseBrake, UpgradeType.Brake, brakeLvl) : carData.baseBrake;
        float finalHandling = (cfg != null) ? cfg.CalculateUpgradedStat(carData.baseHandling, UpgradeType.Handling, handlingLvl) : carData.baseHandling;

        float maxSpeed = (cfg != null) ? cfg.CalculateUpgradedStat(carData.baseMaxSpeed, UpgradeType.Speed, UpgradeConfigSO.MAX_LEVEL) : finalSpeed;
        float maxTorque = (cfg != null) ? cfg.CalculateUpgradedStat(carData.baseTorque, UpgradeType.Engine, UpgradeConfigSO.MAX_LEVEL) : finalTorque;
        float maxBrake = (cfg != null) ? cfg.CalculateUpgradedStat(carData.baseBrake, UpgradeType.Brake, UpgradeConfigSO.MAX_LEVEL) : finalBrake;
        float maxHandling = (cfg != null) ? cfg.CalculateUpgradedStat(carData.baseHandling, UpgradeType.Handling, UpgradeConfigSO.MAX_LEVEL) : finalHandling;

        if (carSpeedTxt != null) carSpeedTxt.text = finalSpeed.ToString("F0");
        if (carTorqueTxt != null) carTorqueTxt.text = finalTorque.ToString("F0");
        if (carBrakeTxt != null) carBrakeTxt.text = finalBrake.ToString("F0");
        if (carHandlingTxt != null) carHandlingTxt.text = finalHandling.ToString("F2");

        if (carSpeedFillImg != null) carSpeedFillImg.fillAmount = maxSpeed > 0 ? Mathf.Clamp01(finalSpeed / maxSpeed) : 1f;
        if (carTorqueFillImg != null) carTorqueFillImg.fillAmount = maxTorque > 0 ? Mathf.Clamp01(finalTorque / maxTorque) : 1f;
        if (carBrakeFillImg != null) carBrakeFillImg.fillAmount = maxBrake > 0 ? Mathf.Clamp01(finalBrake / maxBrake) : 1f;
        if (carHandlingFillImg != null) carHandlingFillImg.fillAmount = maxHandling > 0 ? Mathf.Clamp01(finalHandling / maxHandling) : 1f;
    }

    private void UpdateCurrencyUI(int gold, int silver)
    {
        if (goldAmountTxt != null) goldAmountTxt.text = gold.FormatNumber();
        if (silverAmountTxt != null) silverAmountTxt.text = silver.FormatNumber();
    }

    public void OnClickCarShop()
    {
        HUDSystem.Instance.Show<CarShopPanel>();
    }
    public void OnClickUpgrade()
    {
        HUDSystem.Instance.Show<UpgradePanel>();
    }
    public void OnClickCustomize()
    {
        HUDSystem.Instance.Show<CustomPanel>();
    }
    public void OnClickStartRace()
    {
        if (HUDSystem.Instance != null)
        {
            HUDSystem.Instance.StartCoroutine(StartRaceRoutine());
        }
        else
        {
            StartCoroutine(StartRaceRoutine());
        }
    }

    private IEnumerator StartRaceRoutine()
    {
        LoadingPanel loadingPanel = null;
        if (HUDSystem.Instance != null)
        {
            loadingPanel = HUDSystem.Instance.Show<LoadingPanel>();
            if (loadingPanel != null)
            {
                loadingPanel.SetProgress(0f, "Loading ...");
            }
        }
        if (HUDSystem.Instance != null)
        {
            HUDSystem.Instance.Hide<MenuPanel>();
        }
        else
        {
            Hide();
        }
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("InGameScene");
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            float sceneProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f) * 0.20f;
            if (loadingPanel != null)
            {
                loadingPanel.SetProgress(sceneProgress, "Loading ...");
            }
            yield return null;
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetProgress(0.20f, "Loading...");
            yield return loadingPanel.WaitForVisualProgress();
        }
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        yield return null;
        StartSpawnPoint spawner = FindFirstObjectByType<StartSpawnPoint>();
        if (spawner != null)
        {
            spawner.spawnOnStart = false; 

            var spawnTask = spawner.SpawnAllVehiclesAsync((p, msg) =>
            {
                if (loadingPanel != null)
                {
                    float totalProgress = 0.20f + (p * 0.78f);
                    loadingPanel.SetProgress(totalProgress, msg);
                }
            });

            while (!spawnTask.IsCompleted)
            {
                yield return null;
            }

            if (spawnTask.IsFaulted && spawnTask.Exception != null)
            {
                Debug.LogError($"[MenuPanel] Lỗi trong quá trình spawn xe: {spawnTask.Exception.Message}");
            }
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetProgress(1f, "Loading ...");
            yield return loadingPanel.WaitForVisualProgress();
        }
        yield return new WaitForSeconds(0.2f);
        if (HUDSystem.Instance != null)
        {
            HUDSystem.Instance.Hide<LoadingPanel>();
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayIntro();
        }
    }
}
