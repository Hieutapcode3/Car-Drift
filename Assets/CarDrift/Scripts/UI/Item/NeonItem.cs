using UnityEngine;
using UnityEngine.UI;

public class NeonItem : BaseCustomItem
{
    [SerializeField] private Image colorImg;
    [SerializeField] private Image noColorImg;

    private Material cachedNeonMat;

    protected override CustomType GetCustomType() => CustomType.Neon;

    public void Init(NeonCustomItem itemData, int indexInConfig)
    {
        cachedNeonMat = itemData.neonMat;
        bool isNullItem = itemData.neonMat == null;

        if (noColorImg != null)
        {
            noColorImg.gameObject.SetActive(isNullItem);
        }

        if (colorImg != null)
        {
            colorImg.gameObject.SetActive(!isNullItem);
            if (!isNullItem && itemData.neonMat != null)
            {
                colorImg.color = itemData.neonMat.HasProperty("_BaseColor") ? itemData.neonMat.GetColor("_BaseColor") : itemData.neonMat.color;
            }
        }

        // Common init: sets amountTxt, adds buy/select listeners, calls RefreshState
        InitBase(indexInConfig, itemData.priceGold);

        // Setup preview listener on selectBtn only (not buyBtn)
        SetupPreviewListener(selectBtn, itemData);
    }

    private void SetupPreviewListener(UIButton btn, NeonCustomItem itemData)
    {
        if (btn == null) return;

        btn.onPress.AddListener(() =>
        {
            RCCP_CarController playerVehicle = RCCP_SceneManager.Instance != null ? RCCP_SceneManager.Instance.activePlayerVehicle : null;
            if (playerVehicle != null && playerVehicle.Customizer != null && playerVehicle.Customizer.NeonManager != null)
            {
                if (itemData.neonMat != null)
                    playerVehicle.Customizer.NeonManager.UpgradeWithoutSave(itemData.neonMat);
                else
                    playerVehicle.Customizer.NeonManager.Restore();
            }
        });
    }
}
