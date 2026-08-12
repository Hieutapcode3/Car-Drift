using System;

public static class NumberFormatter
{

    public static string FormatNumber(this long num)
    {
        if (num < 0)
            return "-" + FormatNumber(-num);

        if (num >= 1_000_000_000_000L)
            return (num / 1_000_000_000_000D).ToString("0.##") + "T";
        if (num >= 1_000_000_000L)
            return (num / 1_000_000_000D).ToString("0.##") + "B";
        if (num >= 1_000_000L)
            return (num / 1_000_000D).ToString("0.##") + "M";
        if (num >= 1_000L)
            return (num / 1_000D).ToString("0.##") + "K";

        return num.ToString();
    }

    public static string FormatNumber(this int num)
    {
        return FormatNumber((long)num);
    }

    public static string FormatNumber(this double num)
    {
        if (num < 0)
            return "-" + FormatNumber(-num);

        if (num >= 1_000_000_000_000D)
            return (num / 1_000_000_000_000D).ToString("0.##") + "T";
        if (num >= 1_000_000_000D)
            return (num / 1_000_000_000D).ToString("0.##") + "B";
        if (num >= 1_000_000D)
            return (num / 1_000_000D).ToString("0.##") + "M";
        if (num >= 1_000D)
            return (num / 1_000D).ToString("0.##") + "K";

        return num.ToString("0.##");
    }

    public static string FormatNumber(this float num)
    {
        return FormatNumber((double)num);
    }
}
