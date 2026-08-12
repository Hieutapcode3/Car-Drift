using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WheelItem : MonoBehaviour
{
    [SerializeField] private Image wheelIconImg;
    [SerializeField] private TextMeshProUGUI amountTxt;
    [SerializeField] private UIButton selectBtn;
    [SerializeField] private UIButton buyBtn;

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

        SetupButton(selectBtn, index, isBuy: false);
        SetupButton(buyBtn, index, isBuy: true);
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
                }
            });
        }
    }
}
