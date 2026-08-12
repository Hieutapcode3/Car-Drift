using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WheelItem : MonoBehaviour
{
    [SerializeField] private Image wheelIconImg;
    [SerializeField] private TextMeshProUGUI amountTxt;
    [SerializeField] private UIButton selectBtn;
    [SerializeField] private UIButton buyBtn;
    [SerializeField] private TextMeshProUGUI selectTxt;

    public void Init(WheelCustomItem itemData, int index)
    {
        if (wheelIconImg != null && itemData.icon != null)
        {
            wheelIconImg.sprite = itemData.icon;
            wheelIconImg.SetNativeSize();
        }

        if (amountTxt != null)
        {
            amountTxt.text = itemData.priceGold.FormatNumber();
        }

        string carID = GarageManager.Instance != null && GarageManager.Instance.CurrentCarData != null ? GarageManager.Instance.CurrentCarData.carID : "";
        bool isUnlocked = CarSaveManager.IsCustomUnlocked(CustomType.Wheels, index, itemData.priceGold);
        int equippedIndex = CarSaveManager.GetCustomIndex(carID, CustomType.Wheels);
        bool isEquipped = (equippedIndex == index) || (equippedIndex == -1 && index == 0);

        if (buyBtn != null)
        {
            buyBtn.gameObject.SetActive(!isUnlocked);
        }

        if (selectBtn != null)
        {
            selectBtn.gameObject.SetActive(isUnlocked);
            SetButtonInteractable(selectBtn, isUnlocked && !isEquipped);
        }

        if (selectTxt != null)
        {
            selectTxt.text = isEquipped ? "Selected" : "Select";
        }

        SetupButton(selectBtn, index, isBuy: false);
        SetupButton(buyBtn, index, isBuy: true);
    }

    private void SetButtonInteractable(UIButton btn, bool interactable)
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

    private void SetupButton(UIButton btn, int index, bool isBuy)
    {
        if (btn == null) return;

        RCCP_UI_Wheel rccpWheel = btn.GetComponent<RCCP_UI_Wheel>();
        if (rccpWheel == null)
        {
            rccpWheel = btn.gameObject.AddComponent<RCCP_UI_Wheel>();
        }

        rccpWheel.wheelIndex = index;

        btn.onPress.RemoveAllListeners();
        btn.onPress.AddListener(rccpWheel.OnClick);

        if (isBuy)
        {
            btn.onPress.AddListener(() =>
            {
                if (GarageManager.Instance != null)
                {
                    GarageManager.Instance.CustomCurrentCar(CustomType.Wheels, index);
                }
            });
        }
        else
        {
            btn.onPress.AddListener(() =>
            {
                CarDataSO currentCar = GarageManager.Instance != null ? GarageManager.Instance.CurrentCarData : null;
                if (currentCar != null)
                {
                    CarSaveManager.SetCustomIndex(currentCar.carID, CustomType.Wheels, index);
                    GarageManager.Instance.ApplySavedUpgradesAndCustoms(currentCar);
                }
            });
        }
    }
}
