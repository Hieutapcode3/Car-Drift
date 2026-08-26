using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarShopPanel : Panel<CarShopPanel>
{
    [Title("Currency")]
    [SerializeField] private TextMeshProUGUI goldAmountTxt;
    [SerializeField] private TextMeshProUGUI silverAmountTxt;

    [Title("Car Info")]
    [SerializeField] private TextMeshProUGUI carNameTxt;
    [SerializeField] private TextMeshProUGUI carRankTxt;
    [SerializeField] private TextMeshProUGUI carPriceTxt;
    [SerializeField] private Button buyBtn;
    [SerializeField] private Button selectBtn;
    [SerializeField] private TextMeshProUGUI selectBtnTxt;

    [Title("Parameters")]
    [SerializeField] private TextMeshProUGUI carSpeedTxt;
    [SerializeField] private Image carSpeedFillImg;
    [SerializeField] private TextMeshProUGUI carTorqueTxt;
    [SerializeField] private Image carTorqueFillImg;
    [SerializeField] private TextMeshProUGUI carBrakeTxt;
    [SerializeField] private Image carBrakeFillImg;
    [SerializeField] private TextMeshProUGUI carHandlingTxt;
    [SerializeField] private Image carHandlingFillImg;

    [Title("Navigation")]
    [SerializeField] private Button nextCarBtn;
    [SerializeField] private Button prevCarBtn;

    [Title("3D Display Viewport")]
    [SerializeField] private RawImage carRenderRawImage;

    protected override void Awake()
    {
        base.Awake();

        if (carRenderRawImage != null && carRenderRawImage.GetComponent<CarRotateDragHandler>() == null)
        {
            carRenderRawImage.gameObject.AddComponent<CarRotateDragHandler>();
        }

        if (selectBtn != null && selectBtnTxt == null)
        {
            selectBtnTxt = selectBtn.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (nextCarBtn != null) nextCarBtn.onClick.AddListener(OnClickNextCar);
        if (prevCarBtn != null) prevCarBtn.onClick.AddListener(OnClickPrevCar);
        if (buyBtn != null) buyBtn.onClick.AddListener(OnClickBuyCar);
        if (selectBtn != null) selectBtn.onClick.AddListener(OnClickSelectCar);
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) return;

        CurrencyManager.OnCurrencyChanged += UpdateCurrencyUI;
        CarSaveManager.OnCustomizationUpdated += RefreshCustomization;
        GarageManager.OnCarChanged += RefreshCarShopUI;

        UpdateCurrencyUI(CurrencyManager.Gold, CurrencyManager.Silver);

        if (GarageManager.Instance != null)
        {
            RefreshCarShopUI(GarageManager.Instance.CurrentCarData);
        }
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;

        CurrencyManager.OnCurrencyChanged -= UpdateCurrencyUI;
        CarSaveManager.OnCustomizationUpdated -= RefreshCustomization;
        GarageManager.OnCarChanged -= RefreshCarShopUI;
    }

    private void RefreshCustomization()
    {
        RefreshCarShopUI(null);
    }

    public void RefreshCarShopUI(CarDataSO data)
    {
        if (data == null && GarageManager.Instance != null)
        {
            data = GarageManager.Instance.CurrentCarData;
        }
        if (data == null) return;

        if (carNameTxt != null) carNameTxt.text = data.carName;
        if (carRankTxt != null) carRankTxt.text = data.rank.ToString();

        bool isUnlocked = CarSaveManager.IsCarUnlocked(data);
        int currentIdx = GarageManager.Instance != null ? GarageManager.Instance.CurrentCarIndex : 0;
        bool isSelected = CarSaveManager.GetSelectedCarIndex() == currentIdx;

        if (buyBtn != null)
        {
            buyBtn.gameObject.SetActive(!isUnlocked);
            if (carPriceTxt != null) carPriceTxt.text = data.unlockCostGold.FormatNumber();
        }

        if (selectBtn != null)
        {
            selectBtn.gameObject.SetActive(isUnlocked);
            selectBtn.interactable = !isSelected;

            if (selectBtnTxt == null)
                selectBtnTxt = selectBtn.GetComponentInChildren<TextMeshProUGUI>(true);

            if (selectBtnTxt != null)
            {
                selectBtnTxt.text = isSelected ? "Selected" : "Select";
            }
        }

        // Stats & Fill calculations
        string id = data.carID;
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

        float finalSpeed = (cfg != null) ? cfg.CalculateUpgradedStat(data.baseMaxSpeed, UpgradeType.Speed, speedLvl) : data.baseMaxSpeed;
        float finalTorque = (cfg != null) ? cfg.CalculateUpgradedStat(data.baseTorque, UpgradeType.Engine, engineLvl) : data.baseTorque;
        float finalBrake = (cfg != null) ? cfg.CalculateUpgradedStat(data.baseBrake, UpgradeType.Brake, brakeLvl) : data.baseBrake;
        float finalHandling = (cfg != null) ? cfg.CalculateUpgradedStat(data.baseHandling, UpgradeType.Handling, handlingLvl) : data.baseHandling;

        float maxSpeed = (cfg != null) ? cfg.CalculateUpgradedStat(data.baseMaxSpeed, UpgradeType.Speed, UpgradeConfigSO.MAX_LEVEL) : finalSpeed;
        float maxTorque = (cfg != null) ? cfg.CalculateUpgradedStat(data.baseTorque, UpgradeType.Engine, UpgradeConfigSO.MAX_LEVEL) : finalTorque;
        float maxBrake = (cfg != null) ? cfg.CalculateUpgradedStat(data.baseBrake, UpgradeType.Brake, UpgradeConfigSO.MAX_LEVEL) : finalBrake;
        float maxHandling = (cfg != null) ? cfg.CalculateUpgradedStat(data.baseHandling, UpgradeType.Handling, UpgradeConfigSO.MAX_LEVEL) : finalHandling;

        if (carSpeedTxt != null) carSpeedTxt.text = finalSpeed.ToString("F0");
        if (carTorqueTxt != null) carTorqueTxt.text = finalTorque.ToString("F0");
        if (carBrakeTxt != null) carBrakeTxt.text = finalBrake.ToString("F0");
        if (carHandlingTxt != null) carHandlingTxt.text = finalHandling.ToString("F2");

        if (carSpeedFillImg != null) carSpeedFillImg.fillAmount = maxSpeed > 0 ? Mathf.Clamp01(finalSpeed / maxSpeed) : 1f;
        if (carTorqueFillImg != null) carTorqueFillImg.fillAmount = maxTorque > 0 ? Mathf.Clamp01(finalTorque / maxTorque) : 1f;
        if (carBrakeFillImg != null) carBrakeFillImg.fillAmount = maxBrake > 0 ? Mathf.Clamp01(finalBrake / maxBrake) : 1f;
        if (carHandlingFillImg != null) carHandlingFillImg.fillAmount = maxHandling > 0 ? Mathf.Clamp01(finalHandling / maxHandling) : 1f;
        
        int totalCars = 0;
        if (GarageManager.Instance != null && GarageManager.Instance.carDatabase != null && GarageManager.Instance.carDatabase.cars != null)
        {
            totalCars = GarageManager.Instance.carDatabase.cars.Count;
        }
        else if (CarDatabaseSO.Instance != null && CarDatabaseSO.Instance.cars != null)
        {
            totalCars = CarDatabaseSO.Instance.cars.Count;
        }
        if (prevCarBtn != null)
        {
            prevCarBtn.interactable = currentIdx > 0;
        }

        if (nextCarBtn != null)
        {
            nextCarBtn.interactable = currentIdx < totalCars - 1;
        }
    }

    public void OnClickNextCar()
    {
        if (GarageManager.Instance != null)
        {
            int totalCars = (GarageManager.Instance.carDatabase != null && GarageManager.Instance.carDatabase.cars != null)
                ? GarageManager.Instance.carDatabase.cars.Count
                : 0;
            if (GarageManager.Instance.CurrentCarIndex < totalCars - 1)
            {
                GarageManager.Instance.ShowCar(GarageManager.Instance.CurrentCarIndex + 1);
            }
        }
    }

    public void OnClickPrevCar()
    {
        if (GarageManager.Instance != null)
        {
            if (GarageManager.Instance.CurrentCarIndex > 0)
            {
                GarageManager.Instance.ShowCar(GarageManager.Instance.CurrentCarIndex - 1);
            }
        }
    }

    public void OnClickBuyCar()
    {
        if (GarageManager.Instance != null && GarageManager.Instance.BuyCurrentCar())
        {
            RefreshCarShopUI(GarageManager.Instance.CurrentCarData);
        }
    }

    public void OnClickSelectCar()
    {
        if (GarageManager.Instance != null)
        {
            GarageManager.Instance.SelectCurrentCar();
            RefreshCarShopUI(GarageManager.Instance.CurrentCarData);
        }
    }

    private void UpdateCurrencyUI(int gold, int silver)
    {
        if (goldAmountTxt != null) goldAmountTxt.text = gold.FormatNumber();
        if (silverAmountTxt != null) silverAmountTxt.text = silver.FormatNumber();
    }

    public void BackToMainMenu()
    {
        if (GarageManager.Instance != null)
        {
            int selectedCarIdx = CarSaveManager.GetSelectedCarIndex();
            GarageManager.Instance.ShowCar(selectedCarIdx);
            GarageManager.Instance.ResetCameraOrbit(true);
        }

        HUDSystem.Instance.Hide<CarShopPanel>();
        HUDSystem.Instance.Show<MenuPanel>();
    }
}
