using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NeonItem : MonoBehaviour
{
    [SerializeField] private Image colorImg;
    [SerializeField] private TextMeshProUGUI amountTxt;
    [SerializeField] private UIButton selectBtn;
    [SerializeField] private UIButton buyBtn;
    [SerializeField] private Image noColorImg;

    public void Init(NeonCustomItem itemData, int indexInConfig)
    {
        bool isFree = itemData.priceGold <= 0;

        if (noColorImg != null)
        {
            noColorImg.gameObject.SetActive(isFree);
        }

        if (colorImg != null)
        {
            if (!isFree && itemData.neonMat != null)
            {
                colorImg.color = itemData.neonMat.HasProperty("_BaseColor") ? itemData.neonMat.GetColor("_BaseColor") : itemData.neonMat.color;
            }
        }

        if (amountTxt != null)
        {
            amountTxt.text = itemData.priceGold.FormatNumber();
        }

        if (buyBtn != null)
        {
            buyBtn.gameObject.SetActive(!isFree);
        }

        if (selectBtn != null)
        {
            selectBtn.gameObject.SetActive(isFree);
        }

        SetupButton(selectBtn, itemData, indexInConfig, isBuy: false);
        SetupButton(buyBtn, itemData, indexInConfig, isBuy: true);
    }

    private void SetupButton(UIButton btn, NeonCustomItem itemData, int indexInConfig, bool isBuy)
    {
        if (btn == null) return;

        btn.onPress.RemoveAllListeners();
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

        if (isBuy)
        {
            btn.onPress.AddListener(() =>
            {
                if (GarageManager.Instance != null)
                {
                    GarageManager.Instance.CustomCurrentCar(CustomType.Neon, indexInConfig);
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
                    CarSaveManager.SetCustomIndex(currentCar.carID, CustomType.Neon, indexInConfig);
                }
            });
        }
    }
}
