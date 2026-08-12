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
    [SerializeField] private TextMeshProUGUI selectTxt;

    public void Init(NeonCustomItem itemData, int indexInConfig)
    {
        string carID = GarageManager.Instance != null && GarageManager.Instance.CurrentCarData != null ? GarageManager.Instance.CurrentCarData.carID : "";
        bool isUnlocked = CarSaveManager.IsCustomUnlocked(CustomType.Neon, indexInConfig, itemData.priceGold);
        int equippedIndex = CarSaveManager.GetCustomIndex(carID, CustomType.Neon);
        bool isEquipped = (equippedIndex == indexInConfig) || (equippedIndex == -1 && indexInConfig == 0);

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

        if (amountTxt != null)
        {
            amountTxt.text = itemData.priceGold.FormatNumber();
        }

        if (buyBtn != null)
        {
            buyBtn.gameObject.SetActive(!isUnlocked);
        }

        if (selectBtn != null)
        {
            selectBtn.gameObject.SetActive(isUnlocked);
            SetButtonInteractable(selectBtn, isUnlocked && !isEquipped);
        }

        if (selectTxt != null)
        {
            selectTxt.text = isEquipped ? "Selected" : "Select";
        }

        SetupButton(selectBtn, itemData, indexInConfig, isBuy: false);
        SetupButton(buyBtn, itemData, indexInConfig, isBuy: true);
    }

    private void SetButtonInteractable(UIButton btn, bool interactable)
    {
        if (btn == null) return;
        Selectable sel = btn.GetComponent<Selectable>();
        if (sel != null)
        {
            sel.interactable = interactable;
        }
        else
        {
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = interactable;
            cg.blocksRaycasts = interactable;
        }
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
                    GarageManager.Instance.ApplySavedUpgradesAndCustoms(currentCar);
                }
            });
        }
    }
}
