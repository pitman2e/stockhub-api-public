using System;

namespace StockHub.Models;

public class CurrencyExchangeRate
{
    public const string HKD = "HKD";
    public const string USD = "USD";

    public static decimal GetExRate(string? fromCurrency, string? toCurrency)
    {
        decimal InnerGetExRate(string currency)
        {
            switch (currency)
            {
                case HKD:
                    return 1m;
                case USD:
                    return 7.8m;
                default:
                    throw new NotImplementedException("Unknown currency supplied: " + currency);
            }
        }

        return InnerGetExRate(fromCurrency ?? Config.DefaultCurrency) / InnerGetExRate(toCurrency ?? Config.DefaultCurrency);
    }

    public static decimal GetExRateToHKD(string fromCurrency)
    {
        return GetExRate(fromCurrency, HKD);
    }

    public static bool IsValidOrEmpty(string displayCurrency)
    {
        return string.IsNullOrWhiteSpace(displayCurrency) || new[] { HKD, USD }.Contains(displayCurrency);
    }
}