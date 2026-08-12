using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum GarageTab
{
    Car,
    Upgrade,
    Custom
}

public class GarageUIManager : MonoBehaviour
{
    [Header("Currency UI")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI silverText;

    [Header("Main Panels / Tabs")]
    public GameObject carPanel;
    public GameObject upgradePanel;
    public GameObject customPanel;

    [Header("Tab Buttons")]
    public Button tabCarBtn;
    public Button tabUpgradeBtn;
    public Button tabCustomBtn;

    [Header("Car Panel Elements")]
    public TextMeshProUGUI carNameText;
    public TextMeshProUGUI carRankText;
    public TextMeshProUGUI carPriceGoldText;
    public TextMeshProUGUI maxSpeedText;
    public TextMeshProUGUI torqueText;
    public TextMeshProUGUI brakeText;
    public TextMeshProUGUI handlingText;
    public Button buyCarBtn;
    public Button selectCarBtn;
    public GameObject selectedBadge;
    public Button prevCarBtn;
    public Button nextCarBtn;

    [Header("Upgrade Panel Elements")]
    public TextMeshProUGUI engineLevelText;
    public TextMeshProUGUI engineCostText;
    public Button engineUpgradeBtn;

    public TextMeshProUGUI handlingLevelText;
    public TextMeshProUGUI handlingCostText;
    public Button handlingUpgradeBtn;

    public TextMeshProUGUI brakeLevelText;
    public TextMeshProUGUI brakeCostText;
    public Button brakeUpgradeBtn;

    public TextMeshProUGUI speedLevelText;
    public TextMeshProUGUI speedCostText;
    public Button speedUpgradeBtn;

    private GarageTab currentTab = GarageTab.Car;

    private void OnEnable()
    {
        CurrencyManager.OnCurrencyChanged += UpdateCurrencyUI;
    }

    private void OnDisable()
    {
        CurrencyManager.OnCurrencyChanged -= UpdateCurrencyUI;
    }

    private void Start()
    {
        SetupButtonEvents();
        UpdateCurrencyUI(CurrencyManager.Gold, CurrencyManager.Silver);
        SwitchTab(GarageTab.Car);
    }

    private void SetupButtonEvents()
    {
        if (tabCarBtn) tabCarBtn.onClick.AddListener(() => SwitchTab(GarageTab.Car));
        if (tabUpgradeBtn) tabUpgradeBtn.onClick.AddListener(() => SwitchTab(GarageTab.Upgrade));
        if (tabCustomBtn) tabCustomBtn.onClick.AddListener(() => SwitchTab(GarageTab.Custom));

        if (prevCarBtn) prevCarBtn.onClick.AddListener(OnClickPrevCar);
        if (nextCarBtn) nextCarBtn.onClick.AddListener(OnClickNextCar);
        if (buyCarBtn) buyCarBtn.onClick.AddListener(OnClickBuyCar);
        if (selectCarBtn) selectCarBtn.onClick.AddListener(OnClickSelectCar);

        if (engineUpgradeBtn) engineUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(UpgradeType.Engine));
        if (handlingUpgradeBtn) handlingUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(UpgradeType.Handling));
        if (brakeUpgradeBtn) brakeUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(UpgradeType.Brake));
        if (speedUpgradeBtn) speedUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(UpgradeType.Speed));
    }

    public void SwitchTab(GarageTab tab)
    {
        currentTab = tab;
        if (carPanel) carPanel.SetActive(tab == GarageTab.Car);
        if (upgradePanel) upgradePanel.SetActive(tab == GarageTab.Upgrade);
        if (customPanel) customPanel.SetActive(tab == GarageTab.Custom);

        RefreshCurrentTab();
    }

    public void RefreshCurrentTab()
    {
        if (GarageManager.Instance == null) return;

        CarDataSO carData = GarageManager.Instance.CurrentCarData;
        if (carData == null) return;

        bool isUnlocked = CarSaveManager.IsCarUnlocked(carData);
        int selectedIndex = CarSaveManager.GetSelectedCarIndex();
        bool isSelected = selectedIndex == GarageManager.Instance.CurrentCarIndex;

        // Refresh Car Panel UI
        if (carNameText) carNameText.text = carData.carName;
        if (carRankText) carRankText.text = $"Rank: {carData.rank}";
        if (maxSpeedText) maxSpeedText.text = $"Speed: {carData.baseMaxSpeed:F0}";
        if (torqueText) torqueText.text = $"Torque: {carData.baseTorque:F0}";
        if (brakeText) brakeText.text = $"Brake: {carData.baseBrake:F0}";
        if (handlingText) handlingText.text = $"Handling: {carData.baseHandling:F1}";

        if (buyCarBtn) buyCarBtn.gameObject.SetActive(!isUnlocked);
        if (carPriceGoldText) carPriceGoldText.text = $"{carData.unlockCostGold.FormatNumber()} Gold";

        if (selectCarBtn) selectCarBtn.gameObject.SetActive(isUnlocked && !isSelected);
        if (selectedBadge) selectedBadge.SetActive(isUnlocked && isSelected);

        // Refresh Upgrade Panel UI
        RefreshUpgradeUI(carData);
    }

    private void RefreshUpgradeUI(CarDataSO carData)
    {
        if (carData == null || GarageManager.Instance == null || GarageManager.Instance.carDatabase == null) return;
        UpgradeConfigSO cfg = GarageManager.Instance.carDatabase.upgradeConfig;
        string id = carData.carID;

        UpdateSingleUpgradeUI(id, UpgradeType.Engine, cfg, engineLevelText, engineCostText, engineUpgradeBtn);
        UpdateSingleUpgradeUI(id, UpgradeType.Handling, cfg, handlingLevelText, handlingCostText, handlingUpgradeBtn);
        UpdateSingleUpgradeUI(id, UpgradeType.Brake, cfg, brakeLevelText, brakeCostText, brakeUpgradeBtn);
        UpdateSingleUpgradeUI(id, UpgradeType.Speed, cfg, speedLevelText, speedCostText, speedUpgradeBtn);
    }

    private void UpdateSingleUpgradeUI(string carID, UpgradeType type, UpgradeConfigSO cfg, TextMeshProUGUI levelTxt, TextMeshProUGUI costTxt, Button upgradeBtn)
    {
        int lvl = CarSaveManager.GetUpgradeLevel(carID, type);
        if (levelTxt) levelTxt.text = $"LV {lvl}/{UpgradeConfigSO.MAX_LEVEL}";

        if (lvl >= UpgradeConfigSO.MAX_LEVEL)
        {
            if (costTxt) costTxt.text = "MAX";
            if (upgradeBtn) upgradeBtn.interactable = false;
        }
        else
        {
            UpgradeLevelData nextData = cfg != null ? cfg.GetUpgradeData(type, lvl) : default;
            if (costTxt) costTxt.text = $"{nextData.costGold.FormatNumber()}G + {nextData.costSilver.FormatNumber()}S";
            if (upgradeBtn)
            {
                bool canAfford = CurrencyManager.HasEnough(nextData.costGold, nextData.costSilver);
                upgradeBtn.interactable = canAfford;
            }
        }
    }

    private void UpdateCurrencyUI(int gold, int silver)
    {
        if (goldText) goldText.text = gold.FormatNumber();
        if (silverText) silverText.text = silver.FormatNumber();
        RefreshCurrentTab();
    }

    private void OnClickPrevCar()
    {
        if (GarageManager.Instance) GarageManager.Instance.PreviousCar();
        RefreshCurrentTab();
    }

    private void OnClickNextCar()
    {
        if (GarageManager.Instance) GarageManager.Instance.NextCar();
        RefreshCurrentTab();
    }

    private void OnClickBuyCar()
    {
        if (GarageManager.Instance != null && GarageManager.Instance.BuyCurrentCar())
        {
            RefreshCurrentTab();
        }
    }

    private void OnClickSelectCar()
    {
        if (GarageManager.Instance != null)
        {
            GarageManager.Instance.SelectCurrentCar();
            RefreshCurrentTab();
        }
    }

    private void OnClickUpgrade(UpgradeType type)
    {
        if (GarageManager.Instance != null && GarageManager.Instance.UpgradeCurrentCar(type))
        {
            RefreshCurrentTab();
        }
    }

    public void OnClickCustomItem(int typeIndex, int itemIndex)
    {
        CustomType type = (CustomType)typeIndex;
        if (GarageManager.Instance != null && GarageManager.Instance.CustomCurrentCar(type, itemIndex))
        {
            RefreshCurrentTab();
        }
    }
}
