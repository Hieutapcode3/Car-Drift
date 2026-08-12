using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuPanel : Panel<MenuPanel>
{
    [Title("Currency")]
    [SerializeField] private TextMeshProUGUI goldAmountTxt;
    [SerializeField] private TextMeshProUGUI silverAmountTxt;
    [Title("Parameters")]
    [SerializeField] private TextMeshProUGUI carNameTxt;
    [SerializeField] private TextMeshProUGUI carRankTxt;

    [SerializeField] private TextMeshProUGUI carSpeedTxt;
    [SerializeField] private Image carSpeedFillImg;
    [SerializeField] private TextMeshProUGUI carTorqueTxt;
    [SerializeField] private Image carTorqueFillImg;
    [SerializeField] private TextMeshProUGUI carBrakeTxt;
    [SerializeField] private Image carBrakeFillImg;
    [SerializeField] private TextMeshProUGUI carHandlingTxt;
    [SerializeField] private Image carHandlingFillImg;


    private void OnEnable()
    {
        CurrencyManager.OnCurrencyChanged += UpdateCurrencyUI;
        UpdateCurrencyUI(CurrencyManager.Gold, CurrencyManager.Silver);
    }

    private void OnDisable()
    {
        CurrencyManager.OnCurrencyChanged -= UpdateCurrencyUI;
    }

    private void UpdateCurrencyUI(int gold, int silver)
    {
        if (goldAmountTxt != null) goldAmountTxt.text = gold.FormatNumber();
        if (silverAmountTxt != null) silverAmountTxt.text = silver.FormatNumber();
    }

    public void OnClickCarShop()
    {

    }
    public void OnClickUpgrade()
    {
        HUDSystem.Instance.Show<UpgradePanel>();
    }
    public void OnClickCustomize()
    {
        HUDSystem.Instance.Show<CustomPanel>();
    }
    public void OnClickStartRace()
    {

    }
}
