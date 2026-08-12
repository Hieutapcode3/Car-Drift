using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpoilerItem : MonoBehaviour
{
    [SerializeField] private Image spoilerIconImg;
    [SerializeField] private TextMeshProUGUI amountTxt;
    [SerializeField] private UIButton selectBtn;
    [SerializeField] private UIButton buyBtn;

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

        SetupButton(selectBtn, indexInConfig, isBuy: false);
        SetupButton(buyBtn, indexInConfig, isBuy: true);
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
                }
            });
        }
    }
}
