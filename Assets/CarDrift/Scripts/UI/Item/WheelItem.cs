using UnityEngine;
using UnityEngine.UI;

public class WheelItem : BaseCustomItem
{
    [SerializeField] private Image wheelIconImg;

    protected override CustomType GetCustomType() => CustomType.Wheels;

    public void Init(WheelCustomItem itemData, int index)
    {
        if (wheelIconImg != null && itemData.icon != null)
        {
            wheelIconImg.sprite = itemData.icon;
            wheelIconImg.SetNativeSize();
        }

        // Cleanup RCCP_UI_Wheel component from buyBtn if present
        if (buyBtn != null)
        {
            RCCP_UI_Wheel oldComp = buyBtn.GetComponent<RCCP_UI_Wheel>();
            if (oldComp != null) Destroy(oldComp);
        }

        // Common init: sets amountTxt, adds buy/select listeners, calls RefreshState
        InitBase(index, itemData.priceGold);

        // Setup preview listener on selectBtn only (not buyBtn)
        SetupPreviewListener(selectBtn, index);
    }

    private void SetupPreviewListener(UIButton btn, int index)
    {
        if (btn == null) return;

        RCCP_UI_Wheel rccpWheel = btn.GetComponent<RCCP_UI_Wheel>();
        if (rccpWheel == null)
        {
            rccpWheel = btn.gameObject.AddComponent<RCCP_UI_Wheel>();
        }
        rccpWheel.wheelIndex = index;

        btn.onPress.AddListener(rccpWheel.OnClick);
    }
}
