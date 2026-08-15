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
    [SerializeField] private TextMeshProUGUI carPriceTxt;
    [SerializeField] private Button buyBtn;
    [SerializeField] private Button selectBtn;

    [Title("Navigation")]
    [SerializeField] private Button nextCarBtn;
    [SerializeField] private Button prevCarBtn;

    [Title("3D Display Viewport")]
    [SerializeField] private RawImage carRenderRawImage;

    protected override void Awake()
    {
        if (carRenderRawImage != null && carRenderRawImage.GetComponent<CarRotateDragHandler>() == null)
        {
            carRenderRawImage.gameObject.AddComponent<CarRotateDragHandler>();
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
        GarageManager.OnCarChanged -= RefreshCarShopUI;
    }

    public void RefreshCarShopUI(CarDataSO data)
    {
        if (data == null && GarageManager.Instance != null)
        {
            data = GarageManager.Instance.CurrentCarData;
        }
        if (data == null) return;

        if (carNameTxt != null) carNameTxt.text = data.carName;

        bool isUnlocked = CarSaveManager.IsCarUnlocked(data);
        bool isSelected = CarSaveManager.GetSelectedCarIndex() == (GarageManager.Instance != null ? GarageManager.Instance.CurrentCarIndex : 0);

        if (buyBtn != null)
        {
            buyBtn.gameObject.SetActive(!isUnlocked);
            if (carPriceTxt != null) carPriceTxt.text = data.unlockCostGold.FormatNumber();
        }

        if (selectBtn != null)
        {
            selectBtn.gameObject.SetActive(isUnlocked);
            selectBtn.interactable = !isSelected;
        }
        int currentIdx = GarageManager.Instance != null ? GarageManager.Instance.CurrentCarIndex : 0;
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
