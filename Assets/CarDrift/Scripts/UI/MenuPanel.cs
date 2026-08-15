using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
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

        float speedBonus = (cfg != null && speedLvl > 0) ? cfg.GetUpgradeData(UpgradeType.Speed, speedLvl - 1).statBonus : 0f;
        float torqueBonus = (cfg != null && engineLvl > 0) ? cfg.GetUpgradeData(UpgradeType.Engine, engineLvl - 1).statBonus : 0f;
        float brakeBonus = (cfg != null && brakeLvl > 0) ? cfg.GetUpgradeData(UpgradeType.Brake, brakeLvl - 1).statBonus : 0f;
        float handlingBonus = (cfg != null && handlingLvl > 0) ? cfg.GetUpgradeData(UpgradeType.Handling, handlingLvl - 1).statBonus : 0f;

        float finalSpeed = carData.baseMaxSpeed + speedBonus;
        float finalTorque = carData.baseTorque + torqueBonus;
        float finalBrake = carData.baseBrake + brakeBonus;
        float finalHandling = carData.baseHandling + handlingBonus;

        if (carSpeedTxt != null) carSpeedTxt.text = finalSpeed.ToString("F0");
        if (carTorqueTxt != null) carTorqueTxt.text = finalTorque.ToString("F0");
        if (carBrakeTxt != null) carBrakeTxt.text = finalBrake.ToString("F0");
        if (carHandlingTxt != null) carHandlingTxt.text = finalHandling.ToString("F1");
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

    }
}
