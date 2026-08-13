using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseCustomItem : MonoBehaviour
{
    [SerializeField] protected TextMeshProUGUI amountTxt;
    [SerializeField] protected UIButton selectBtn;
    [SerializeField] protected UIButton buyBtn;
    [SerializeField] protected TextMeshProUGUI selectTxt;

    protected int itemIndex;
    protected int cachedPriceGold;
    protected abstract CustomType GetCustomType();
    protected void InitBase(int index, int priceGold)
    {
        itemIndex = index;
        cachedPriceGold = priceGold;

        if (amountTxt != null)
            amountTxt.text = priceGold.FormatNumber();

        SetupBuyAndSelectListeners();
        RefreshState();
    }
    public void RefreshState()
    {
        string carID = GarageManager.Instance != null && GarageManager.Instance.CurrentCarData != null
            ? GarageManager.Instance.CurrentCarData.carID : "";

        bool isUnlocked = CarSaveManager.IsCustomUnlocked(GetCustomType(), itemIndex, cachedPriceGold);
        int equippedIndex = CarSaveManager.GetCustomIndex(carID, GetCustomType());
        bool isEquipped = equippedIndex == itemIndex;

        if (buyBtn != null)
            buyBtn.gameObject.SetActive(!isUnlocked);

        if (selectBtn != null)
        {
            selectBtn.gameObject.SetActive(isUnlocked);
            SetButtonInteractable(selectBtn, isUnlocked && !isEquipped);
        }

        if (selectTxt != null)
            selectTxt.text = isEquipped ? "Selected" : "Select";
    }

    protected void SetButtonInteractable(UIButton btn, bool interactable)
    {
        if (btn == null) return;
        Selectable sel = btn.GetComponent<Selectable>();
        if (sel != null)
        {
            sel.interactable = interactable;
        }
        else
        {
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = interactable;
            cg.blocksRaycasts = interactable;
        }
    }
    private void SetupBuyAndSelectListeners()
    {
        if (buyBtn != null)
        {
            buyBtn.onPress.RemoveAllListeners();
            buyBtn.onPress.AddListener(OnBuyBtnClick);
        }

        if (selectBtn != null)
        {
            selectBtn.onPress.AddListener(OnSelectBtnClick);
        }
    }

    protected virtual void OnBuyBtnClick()
    {
        if (!CurrencyManager.HasEnoughGold(cachedPriceGold))
        {
            Debug.LogWarning($"Không đủ tiền: Cần {cachedPriceGold} Gold, hiện có {CurrencyManager.Gold} Gold.");
            return;
        }

        if (GarageManager.Instance != null)
        {
            bool success = GarageManager.Instance.CustomCurrentCar(GetCustomType(), itemIndex);
            if (success)
            {
                RefreshState();
            }
            else
            {
                Debug.LogWarning($"Không thể mua item (Xe chưa mở khóa hoặc lỗi cấu hình).");
            }
        }
        else
        {
            Debug.LogWarning("GarageManager.Instance is null!");
        }
    }

    protected virtual void OnSelectBtnClick()
    {
        CarDataSO currentCar = GarageManager.Instance != null ? GarageManager.Instance.CurrentCarData : null;
        if (currentCar != null)
        {
            CarSaveManager.SetCustomIndex(currentCar.carID, GetCustomType(), itemIndex);
            GarageManager.Instance.ApplySavedUpgradesAndCustoms(currentCar);
        }
    }
}
