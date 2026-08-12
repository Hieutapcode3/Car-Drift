using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaintItem : MonoBehaviour
{
    [SerializeField] private Image colorImg;
    [SerializeField] private TextMeshProUGUI amountTxt;
    [SerializeField] private UIButton selectBtn;
    [SerializeField] private UIButton buyBtn;

    public void Init(PaintCustomItem itemData, int indexInConfig)
    {
        if (colorImg != null)
        {
            colorImg.color = itemData.color;
        }

        if (amountTxt != null)
        {
            amountTxt.text = itemData.priceGold.FormatNumber();
        }

        SetupButton(selectBtn, indexInConfig, itemData.colorType, itemData.color, isBuy: false);
        SetupButton(buyBtn, indexInConfig, itemData.colorType, itemData.color, isBuy: true);
    }

    private void SetupButton(UIButton btn, int indexInConfig, CarColorType colorType, Color color, bool isBuy)
    {
        if (btn == null) return;

        RCCP_UI_Color rccpColor = btn.GetComponent<RCCP_UI_Color>();
        if (rccpColor == null)
        {
            rccpColor = btn.gameObject.AddComponent<RCCP_UI_Color>();
        }
        rccpColor.colorType = colorType;

        btn.onPress.RemoveAllListeners();
        btn.onPress.AddListener(rccpColor.OnClick);

        if (isBuy)
        {
            btn.onPress.AddListener(() =>
            {
                if (GarageManager.Instance != null)
                {
                    GarageManager.Instance.CustomCurrentCar(CustomType.Paint, indexInConfig);
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
                    CarSaveManager.SetCustomIndex(currentCar.carID, CustomType.Paint, indexInConfig);
                }
            });
        }
    }
}
