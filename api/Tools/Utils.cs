using System;

namespace StockHub.Tools;

public class Utils
{
    public static decimal? GetPercentage(decimal? dividend, decimal? divisor)
    {
        if (divisor.GetValueOrDefault() == 0 || dividend == null)
        {
            return null;
        }

        return dividend * 100 / divisor;
    }

    public static decimal? GetChangePercentage(decimal? orgVal, decimal? newVal, int? decimals = null)
    {
        if (orgVal == null || newVal == null)
        {
            return null;
        }

        var percentage = GetPercentage(newVal.Value - orgVal.Value, orgVal.Value);

        if (decimals == null || percentage == null)
        {
            return percentage;
        }
        else
        {
            return Math.Round(percentage.Value, decimals.Value, MidpointRounding.AwayFromZero);
        }
    }
}