using System;

namespace StockHub.Models;

public static class Config
{
    public const int CrawlPriceTimeoutSeconds = 1;
    public const int CrawlPriceHsbcTimeoutSeconds = 3600;
    public const int CrawlPricePcpTimeoutSeconds = 3600 * 4;

    public const int CrawlDivTimeoutDays = 5;
    /// <summary>
    /// Number of crawls before stopping in a single crawl batch
    /// </summary>
    public const int CrawlDividendBatchLimit = 5;
    public const int DecimalDefaultPrecision = 4;
    
    /// <summary>
    /// For a stock last closed position, month of prices record should retroactively crawl
    /// </summary>
    public const int CrawlPriceHistoryDaysOnDemand = 180;
    public const int CrawlPriceHistoryDaysDaily = 10;

    public const string DefaultCurrency = CurrencyExchangeRate.HKD;
    /// Important to define user ObserveDate, as a workaround, set it as +8
    public const int SystemDateOffset = 8;

    public static readonly string? StockHubApiPyBaseUrl = Environment.GetEnvironmentVariable("STOCKHUB_API_PY_BASE_URL");
    public const string WebBrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; rv:153.0) Gecko/20100101 Firefox/153.0";
    
    public const string FirebaseScheme = "FirebaseBearer";
    public const string CustomScheme = "CustomBearer";
}