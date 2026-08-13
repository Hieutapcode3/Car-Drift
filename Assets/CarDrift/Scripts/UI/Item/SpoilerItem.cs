using UnityEngine;
using UnityEngine.UI;

public class SpoilerItem : BaseCustomItem
{
    [SerializeField] private Image spoilerIconImg;

    protected override CustomType GetCustomType() => CustomType.Spoiler;

    public void Init(SpoilerCustomItem itemData, int indexInConfig)
    {
        if (spoilerIconImg != null && itemData.icon != null)
        {
            spoilerIconImg.sprite = itemData.icon;
            spoilerIconImg.SetNativeSize();
        }

        // Cleanup RCCP_UI_Spoiler component from buyBtn if present
        if (buyBtn != null)
        {
            RCCP_UI_Spoiler oldComp = buyBtn.GetComponent<RCCP_UI_Spoiler>();
            if (oldComp != null) Destroy(oldComp);
        }

        // Common init: sets amountTxt, adds buy/select listeners, calls RefreshState
        InitBase(indexInConfig, itemData.priceGold);

        // Setup preview listener on selectBtn only (not buyBtn)
        SetupPreviewListener(selectBtn, indexInConfig);
    }

    private void SetupPreviewListener(UIButton btn, int indexInConfig)
    {
        if (btn == null) return;

        RCCP_UI_Spoiler rccpSpoiler = btn.GetComponent<RCCP_UI_Spoiler>();
        if (rccpSpoiler == null)
        {
            rccpSpoiler = btn.gameObject.AddComponent<RCCP_UI_Spoiler>();
        }
        rccpSpoiler.index = indexInConfig;

        btn.onPress.AddListener(rccpSpoiler.OnClick);
    }
}
