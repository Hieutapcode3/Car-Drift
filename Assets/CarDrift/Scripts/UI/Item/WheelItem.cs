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

        // Setup preview listeners on both buttons (RemoveAll + add preview)
        SetupPreviewListener(buyBtn, index);
        SetupPreviewListener(selectBtn, index);

        // Common init: sets amountTxt, adds buy/select listeners, calls RefreshState
        InitBase(index, itemData.priceGold);
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

        btn.onPress.RemoveAllListeners();
        btn.onPress.AddListener(rccpWheel.OnClick);
    }
}
