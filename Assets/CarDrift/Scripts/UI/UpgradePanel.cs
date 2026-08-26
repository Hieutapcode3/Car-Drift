using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradePanel : Panel<UpgradePanel>
{
    [Title("Currency")]
    [SerializeField] private TextMeshProUGUI goldAmountTxt;
    [SerializeField] private TextMeshProUGUI silverAmountTxt;

    [Title("Upgrade Buttons")]
    [SerializeField] private Button engineUgBtn;
    [SerializeField] private Button handlingUgBtn;
    [SerializeField] private Button brakeUgBtn;
    [SerializeField] private Button speedUgBtn;

    [Title("Visual Settings")]
    [SerializeField] private Sprite activeProgressColor;
    [SerializeField] private Sprite inactiveProgressColor;

    private readonly Dictionary<UpgradeType, UpgradeSlotUI> slots = new Dictionary<UpgradeType, UpgradeSlotUI>();

    private class UpgradeSlotUI
    {
        public UpgradeType type;
        public Button button;
        public TextMeshProUGUI levelTxt;
        public TextMeshProUGUI goldCostTxt;
        public TextMeshProUGUI silverCostTxt;
        public TextMeshProUGUI buttonTxt;
        public GameObject costContainer;
        public List<Image> progressSegments = new List<Image>();

        public void Bind(UpgradeType upgradeType, Button btn)
        {
            type = upgradeType;
            button = btn;
            if (button == null) return;

            Transform root = button.transform.parent;
            if (root == null) return;

            // 1. Level text (thường nằm trong BGIcon/LvImg/Text (TMP))
            Transform lvImgTrans = root.Find("BGIcon/LvImg");
            if (lvImgTrans != null)
            {
                levelTxt = lvImgTrans.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // 2. Button text (nằm trong Button/Text (TMP))
            buttonTxt = button.GetComponentInChildren<TextMeshProUGUI>(true);

            // 3. Amount container & cost texts
            Transform amountTrans = root.Find("Amount");
            if (amountTrans != null)
            {
                costContainer = amountTrans.gameObject;
                Transform goldTrans = amountTrans.Find("Gold");
                if (goldTrans != null)
                {
                    goldCostTxt = goldTrans.GetComponentInChildren<TextMeshProUGUI>(true);
                }
                Transform silverTrans = amountTrans.Find("Silver");
                if (silverTrans != null)
                {
                    silverCostTxt = silverTrans.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }

            // 4. Progress segments (5 thanh tiến trình)
            Transform progressTrans = root.Find("Progress");
            if (progressTrans != null)
            {
                progressSegments.Clear();
                foreach (Transform child in progressTrans)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        progressSegments.Add(img);
                    }
                }
            }
        }

        public void UpdateUI(CarDataSO carData, UpgradeConfigSO cfg, Sprite activeColor, Sprite inactiveColor)
        {
            if (carData == null) return;

            int currentLvl = CarSaveManager.GetUpgradeLevel(carData.carID, type);

            // Level Text
            if (levelTxt != null)
            {
                levelTxt.text = $"Lv {currentLvl}";
            }

            // Progress segments (1..5)
            for (int i = 0; i < progressSegments.Count; i++)
            {
                if (progressSegments[i] != null)
                {
                    progressSegments[i].sprite = (i < currentLvl) ? activeColor : inactiveColor;
                }
            }

            // Max level check
            if (currentLvl >= UpgradeConfigSO.MAX_LEVEL)
            {
                if (button != null) button.interactable = false;
                if (buttonTxt != null) buttonTxt.text = "MAX";
                if (goldCostTxt != null) goldCostTxt.text = "-";
                if (silverCostTxt != null) silverCostTxt.text = "-";
            }
            else
            {
                UpgradeLevelData nextData = cfg != null ? cfg.GetUpgradeData(type, currentLvl) : default;

                if (goldCostTxt != null) goldCostTxt.text = nextData.costGold.FormatNumber();
                if (silverCostTxt != null) silverCostTxt.text = nextData.costSilver.FormatNumber();
                if (buttonTxt != null) buttonTxt.text = "UPGRADE";

                bool canAfford = CurrencyManager.HasEnough(nextData.costGold, nextData.costSilver);
                if (button != null)
                {
                    button.interactable = canAfford;
                }
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        InitSlots();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) return;

        CurrencyManager.OnCurrencyChanged += UpdateCurrencyUI;
        CarSaveManager.OnCustomizationUpdated += RefreshUpgradeUI;
        GarageManager.OnCarChanged += OnCarChanged;

        UpdateCurrencyUI(CurrencyManager.Gold, CurrencyManager.Silver);
        RefreshUpgradeUI();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;

        CurrencyManager.OnCurrencyChanged -= UpdateCurrencyUI;
        CarSaveManager.OnCustomizationUpdated -= RefreshUpgradeUI;
        GarageManager.OnCarChanged -= OnCarChanged;
    }

    private void InitSlots()
    {
        slots.Clear();

        RegisterSlot(UpgradeType.Engine, engineUgBtn);
        RegisterSlot(UpgradeType.Handling, handlingUgBtn);
        RegisterSlot(UpgradeType.Brake, brakeUgBtn);
        RegisterSlot(UpgradeType.Speed, speedUgBtn);
    }

    private void RegisterSlot(UpgradeType type, Button btn)
    {
        if (btn == null) return;

        UpgradeSlotUI slot = new UpgradeSlotUI();
        slot.Bind(type, btn);
        slots[type] = slot;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnClickUpgrade(type));
    }

    private CarDataSO GetCurrentCarData()
    {
        if (GarageManager.Instance != null && GarageManager.Instance.CurrentCarData != null)
        {
            return GarageManager.Instance.CurrentCarData;
        }

        CarDatabaseSO db = CarDatabaseSO.Instance;
        if (db != null)
        {
            return db.GetCarByIndex(CarSaveManager.GetSelectedCarIndex());
        }

        return null;
    }

    private UpgradeConfigSO GetUpgradeConfig()
    {
        if (GarageManager.Instance != null && GarageManager.Instance.carDatabase != null)
        {
            return GarageManager.Instance.carDatabase.upgradeConfig;
        }

        if (CarDatabaseSO.Instance != null)
        {
            return CarDatabaseSO.Instance.upgradeConfig;
        }

        return null;
    }

    public void RefreshUpgradeUI()
    {
        CarDataSO carData = GetCurrentCarData();
        UpgradeConfigSO cfg = GetUpgradeConfig();

        if (carData == null) return;

        if (slots.Count == 0)
        {
            InitSlots();
        }

        foreach (var kvp in slots)
        {
            kvp.Value.UpdateUI(carData, cfg, activeProgressColor, inactiveProgressColor);
        }
    }

    private void OnCarChanged(CarDataSO data)
    {
        RefreshUpgradeUI();
    }

    public void OnClickUpgrade(UpgradeType type)
    {
        CarDataSO carData = GetCurrentCarData();
        UpgradeConfigSO cfg = GetUpgradeConfig();
        if (carData == null || cfg == null) return;

        int curLvl = CarSaveManager.GetUpgradeLevel(carData.carID, type);
        if (curLvl >= UpgradeConfigSO.MAX_LEVEL) return;

        if (GarageManager.Instance != null)
        {
            if (GarageManager.Instance.UpgradeCurrentCar(type))
            {
                CarSaveManager.NotifyCustomizationUpdated();
                RefreshUpgradeUI();
            }
        }
        else
        {
            UpgradeLevelData nextData = cfg.GetUpgradeData(type, curLvl);
            if (CurrencyManager.SpendCurrency(nextData.costGold, nextData.costSilver))
            {
                CarSaveManager.SetUpgradeLevel(carData.carID, type, curLvl + 1);
                CarSaveManager.NotifyCustomizationUpdated();
                RefreshUpgradeUI();
            }
        }
    }

    private void UpdateCurrencyUI(int gold, int silver)
    {
        if (goldAmountTxt != null) goldAmountTxt.text = gold.FormatNumber();
        if (silverAmountTxt != null) silverAmountTxt.text = silver.FormatNumber();
        RefreshUpgradeUI();
    }

    public void BackToMainMenu()
    {
        HUDSystem.Instance.Hide<UpgradePanel>();
    }
}

