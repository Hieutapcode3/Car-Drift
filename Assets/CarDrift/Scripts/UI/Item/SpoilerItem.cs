using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpoilerItem : MonoBehaviour
{
    [SerializeField] private Image spoilerIconImg;
    [SerializeField] private TextMeshProUGUI amountTxt;
    [SerializeField] private UIButton selectBtn;
    [SerializeField] private UIButton buyBtn;
    [SerializeField] private TextMeshProUGUI selectTxt;

    public void Init(SpoilerCustomItem itemData, int indexInConfig)
    {
        if (spoilerIconImg != null && itemData.icon != null)
        {
            spoilerIconImg.sprite = itemData.icon;
            spoilerIconImg.SetNativeSize();
        }

        if (amountTxt != null)
        {
            amountTxt.text = itemData.priceGold.FormatNumber();
        }

        string carID = GarageManager.Instance != null && GarageManager.Instance.CurrentCarData != null ? GarageManager.Instance.CurrentCarData.carID : "";
        bool isUnlocked = CarSaveManager.IsCustomUnlocked(CustomType.Spoiler, indexInConfig, itemData.priceGold);
        int equippedIndex = CarSaveManager.GetCustomIndex(carID, CustomType.Spoiler);
        bool isEquipped = (equippedIndex == indexInConfig) || (equippedIndex == -1 && indexInConfig == 0);

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

        SetupButton(selectBtn, indexInConfig, isBuy: false);
        SetupButton(buyBtn, indexInConfig, isBuy: true);
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

    private void SetupButton(UIButton btn, int indexInConfig, bool isBuy)
    {
        if (btn == null) return;

        RCCP_UI_Spoiler rccpSpoiler = btn.GetComponent<RCCP_UI_Spoiler>();
        if (rccpSpoiler == null)
        {
            rccpSpoiler = btn.gameObject.AddComponent<RCCP_UI_Spoiler>();
        }

        rccpSpoiler.index = indexInConfig;

        btn.onPress.RemoveAllListeners();
        btn.onPress.AddListener(rccpSpoiler.OnClick);

        if (isBuy)
        {
            btn.onPress.AddListener(() =>
            {
                if (GarageManager.Instance != null)
                {
                    GarageManager.Instance.CustomCurrentCar(CustomType.Spoiler, indexInConfig);
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
                    CarSaveManager.SetCustomIndex(currentCar.carID, CustomType.Spoiler, indexInConfig);
                    GarageManager.Instance.ApplySavedUpgradesAndCustoms(currentCar);
                }
            });
        }
    }
}
