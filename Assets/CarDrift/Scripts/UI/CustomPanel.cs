using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomPanel : Panel<CustomPanel>
{
    [Title("Currency")]
    [SerializeField] private TextMeshProUGUI goldAmountTxt;
    [SerializeField] private TextMeshProUGUI silverAmountTxt;
    [SerializeField] private Transform itemContent;

    [Title("Config & Prefabs")]
    [SerializeField] private CustomConfigSO customConfigSO;
    [SerializeField] private WheelItem wheelItemPrefab;
    [SerializeField] private SpoilerItem spoilerItemPrefab;
    [SerializeField] private PaintItem paintItemPrefab;
    [SerializeField] private NeonItem neonItemPrefab;

    [Title("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    [Title("Buttons")]
    [SerializeField] private Button btnWheel;
    [SerializeField] private Button btnSpoiler;
    [SerializeField] private Button btnNeon;
    [SerializeField] private Button btnPaint;

    private Button currentSelectedBtn;
    private CustomType currentCustomType = CustomType.Wheels;

    /// <summary>
    /// Danh sách các item đã spawn — dùng để refresh state mà không cần destroy/re-spawn.
    /// </summary>
    private readonly List<BaseCustomItem> currentItems = new List<BaseCustomItem>();

    private void OnEnable()
    {
        CurrencyManager.OnCurrencyChanged += UpdateCurrencyUI;
        CarSaveManager.OnCustomizationUpdated += RefreshCurrentItems;
        UpdateCurrencyUI(CurrencyManager.Gold, CurrencyManager.Silver);

        if (btnWheel != null) btnWheel.onClick.AddListener(() => SelectTab(btnWheel, CustomType.Wheels));
        if (btnSpoiler != null) btnSpoiler.onClick.AddListener(() => SelectTab(btnSpoiler, CustomType.Spoiler));
        if (btnNeon != null) btnNeon.onClick.AddListener(() => SelectTab(btnNeon, CustomType.Neon));
        if (btnPaint != null) btnPaint.onClick.AddListener(() => SelectTab(btnPaint, CustomType.Paint));
        if (btnWheel != null)
        {
            SelectTab(btnWheel, CustomType.Wheels);
        }
    }

    private void OnDisable()
    {
        CurrencyManager.OnCurrencyChanged -= UpdateCurrencyUI;
        CarSaveManager.OnCustomizationUpdated -= RefreshCurrentItems;
    }

    /// <summary>
    /// Chỉ refresh state trên các item đã spawn, KHÔNG destroy/re-spawn.
    /// Gọi khi mua item hoặc thay đổi customization.
    /// </summary>
    private void RefreshCurrentItems()
    {
        for (int i = currentItems.Count - 1; i >= 0; i--)
        {
            if (currentItems[i] != null)
                currentItems[i].RefreshState();
            else
                currentItems.RemoveAt(i);
        }
    }

    private void UpdateCurrencyUI(int gold, int silver)
    {
        if (goldAmountTxt != null) goldAmountTxt.text = gold.FormatNumber();
        if (silverAmountTxt != null) silverAmountTxt.text = silver.FormatNumber();
    }

    public void SelectTab(Button clickedBtn, CustomType customType)
    {
        ResetAllButtonSprites();
        if (clickedBtn != null)
        {
            Image btnImg = clickedBtn.image != null ? clickedBtn.image : clickedBtn.GetComponent<Image>();
            if (btnImg != null && selectedSprite != null)
            {
                btnImg.sprite = selectedSprite;
            }
            currentSelectedBtn = clickedBtn;
        }
        SpawnItemsForType(customType);
    }

    private void ResetAllButtonSprites()
    {
        Button[] allButtons = new Button[] { btnWheel, btnSpoiler, btnNeon, btnPaint };
        foreach (Button btn in allButtons)
        {
            if (btn != null)
            {
                Image btnImg = btn.image != null ? btn.image : btn.GetComponent<Image>();
                if (btnImg != null && normalSprite != null)
                {
                    btnImg.sprite = normalSprite;
                }
            }
        }
    }

    /// <summary>
    /// Destroy tất cả items hiện tại, spawn mới cho customType, lưu vào currentItems.
    /// Chỉ gọi khi chuyển tab.
    /// </summary>
    private void SpawnItemsForType(CustomType customType)
    {
        currentCustomType = customType;
        ClearItems();

        if (customType == CustomType.Wheels)
        {
            SpawnWheelItems();
        }
        else if (customType == CustomType.Spoiler)
        {
            SpawnSpoilerItems();
        }
        else if (customType == CustomType.Paint)
        {
            SpawnPaintItems();
        }
        else if (customType == CustomType.Neon)
        {
            SpawnNeonItems();
        }
    }

    private void ClearItems()
    {
        currentItems.Clear();
        if (itemContent != null)
        {
            foreach (Transform child in itemContent)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void SpawnWheelItems()
    {
        if (itemContent == null || wheelItemPrefab == null) return;

        CustomConfigSO config = customConfigSO != null ? customConfigSO : CustomConfigSO.Instance;

        if (config == null || config.wheels == null) return;

        for (int i = 0; i < config.wheels.Length; i++)
        {
            WheelItem item = Instantiate(wheelItemPrefab, itemContent);
            item.Init(config.wheels[i], i);
            currentItems.Add(item);
        }
    }

    private void SpawnSpoilerItems()
    {
        if (itemContent == null || spoilerItemPrefab == null) return;

        CustomConfigSO config = customConfigSO != null ? customConfigSO : CustomConfigSO.Instance;

        if (config == null || config.spoilers == null) return;

        for (int i = 0; i < config.spoilers.Length; i++)
        {
            SpoilerItem item = Instantiate(spoilerItemPrefab, itemContent);
            item.Init(config.spoilers[i], config.spoilers[i].indexInConfig);
            currentItems.Add(item);
        }
    }

    private void SpawnPaintItems()
    {
        if (itemContent == null || paintItemPrefab == null) return;

        CustomConfigSO config = customConfigSO != null ? customConfigSO : CustomConfigSO.Instance;

        if (config == null || config.paints == null) return;

        for (int i = 0; i < config.paints.Length; i++)
        {
            PaintItem item = Instantiate(paintItemPrefab, itemContent);
            item.Init(config.paints[i], i);
            currentItems.Add(item);
        }
    }

    private void SpawnNeonItems()
    {
        if (itemContent == null || neonItemPrefab == null) return;

        CustomConfigSO config = customConfigSO != null ? customConfigSO : CustomConfigSO.Instance;

        if (config == null || config.neons == null) return;

        for (int i = 0; i < config.neons.Length; i++)
        {
            NeonItem item = Instantiate(neonItemPrefab, itemContent);
            item.Init(config.neons[i], i);
            currentItems.Add(item);
        }
    }

    public void BackToMainMenu()
    {
        HUDSystem.Instance.Hide<CustomPanel>();
    }
}
