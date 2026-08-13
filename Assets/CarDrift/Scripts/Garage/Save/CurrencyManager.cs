using System;
using UnityEngine;

public static class CurrencyManager
{
    private const string KEY_GOLD = "Player_Gold";
    private const string KEY_SILVER = "Player_Silver";
    private const int DefaultGold = 5000;
    private const int DefaultSilver = 10000;

    public static event Action<int, int> OnCurrencyChanged; // gold, silver

    public static int Gold { get; private set; }
    public static int Silver { get; private set; }

    static CurrencyManager()
    {
        LoadCurrency();
    }

    private static void LoadCurrency()
    {
        Gold = PlayerPrefs.GetInt(KEY_GOLD, DefaultGold);
        Silver = PlayerPrefs.GetInt(KEY_SILVER, DefaultSilver);
    }

    private static void SaveCurrency()
    {
        PlayerPrefs.SetInt(KEY_GOLD, Gold);
        PlayerPrefs.SetInt(KEY_SILVER, Silver);
        PlayerPrefs.Save();
        OnCurrencyChanged?.Invoke(Gold, Silver);
    }

    public static bool HasEnoughGold(int amount)
    {
        return Gold >= amount;
    }

    public static bool HasEnoughSilver(int amount)
    {
        return Silver >= amount;
    }

    public static bool HasEnough(int goldAmount, int silverAmount)
    {
        return Gold >= goldAmount && Silver >= silverAmount;
    }

    public static bool SpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (!HasEnoughGold(amount)) return false;

        Gold -= amount;
        SaveCurrency();
        return true;
    }

    public static bool SpendSilver(int amount)
    {
        if (amount <= 0) return true;
        if (!HasEnoughSilver(amount)) return false;

        Silver -= amount;
        SaveCurrency();
        return true;
    }

    public static bool SpendCurrency(int goldAmount, int silverAmount)
    {
        if (!HasEnough(goldAmount, silverAmount)) return false;

        Gold -= goldAmount;
        Silver -= silverAmount;
        SaveCurrency();
        return true;
    }

    public static void SetGold(int amount)
    {
        Gold = Mathf.Max(0, amount);
        SaveCurrency();
    }

    public static void SetSilver(int amount)
    {
        Silver = Mathf.Max(0, amount);
        SaveCurrency();
    }

    public static void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        SaveCurrency();
    }

    public static void AddSilver(int amount)
    {
        if (amount <= 0) return;
        Silver += amount;
        SaveCurrency();
    }
}
