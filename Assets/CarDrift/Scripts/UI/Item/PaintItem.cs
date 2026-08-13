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

        // Cleanup RCCP_UI_Color component from buyBtn if present
        if (buyBtn != null)
        {
            RCCP_UI_Color oldComp = buyBtn.GetComponent<RCCP_UI_Color>();
            if (oldComp != null) Destroy(oldComp);
        }

        // Common init: sets amountTxt, adds buy/select listeners, calls RefreshState
        InitBase(indexInConfig, itemData.priceGold);

        // Setup preview listener on selectBtn only (not buyBtn)
        SetupPreviewListener(selectBtn, itemData.colorType);
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

        btn.onPress.AddListener(rccpColor.OnClick);
    }
}
