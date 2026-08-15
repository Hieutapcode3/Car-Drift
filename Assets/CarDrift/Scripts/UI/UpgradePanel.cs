using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradePanel : Panel<UpgradePanel>
{
    [Title("Currency")]
    [SerializeField] private TextMeshProUGUI goldAmountTxt;
    [SerializeField] private TextMeshProUGUI silverAmountTxt;

    [SerializeField] private Button engineUgBtn;
    [SerializeField] private Button handlingUgBtn;
    [SerializeField] private Button brakeUgBtn;
    [SerializeField] private Button speedUgBtn;


    public void BackToMainMenu()
    {
        HUDSystem.Instance.Hide<UpgradePanel>();
    }
}
