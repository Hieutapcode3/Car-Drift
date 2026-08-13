using UnityEngine;
using UnityEngine.UI;

public class PaintItem : BaseCustomItem
{
    [SerializeField] private Image colorImg;

    private CarColorType cachedColorType;

    protected override CustomType GetCustomType() => CustomType.Paint;

    public void Init(PaintCustomItem itemData, int indexInConfig)
    {
        cachedColorType = itemData.colorType;

        if (colorImg != null)
        {
            colorImg.color = itemData.color;
        }

        // Setup preview listeners on both buttons (RemoveAll + add preview)
        SetupPreviewListener(buyBtn, itemData.colorType);
        SetupPreviewListener(selectBtn, itemData.colorType);

        // Common init: sets amountTxt, adds buy/select listeners, calls RefreshState
        InitBase(indexInConfig, itemData.priceGold);
    }

    private void SetupPreviewListener(UIButton btn, CarColorType colorType)
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
    }
}
