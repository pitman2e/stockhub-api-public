using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Models;

namespace StockHub.Crawlers.Dividend;

/// <summary>
/// This class is copied from YfinancePriceCrawler.cs
/// </summary>
/// <param name="httpClient"></param>
/// <param name="logger"></param>
public class YfinanceDividendCrawler(
    HttpClient httpClient,
    ILogger<YfinanceDividendCrawler> logger)
    : IYfinanceDividendCrawler
{
    public async Task<IEnumerable<StockDividend>> CrawlAsync(StockAdapter stock)
    {
        var yahooStockId = YahooStockIdMapper.Map(stock);
        if (Config.StockHubApiPyBaseUrl == null) throw new SHArgumentException("StockHubApiPyBaseUrl is not set");
        var url = $"http://{Config.StockHubApiPyBaseUrl}/dividend/{yahooStockId}/";
        
        logger.LogDebug($"Querying {url}");
        string csv = await httpClient.GetStringAsync(url);
        
        var csvs = csv.Split('\r', '\n');
        if (csvs.Length == 0 || !csvs[0].StartsWith("Date,Dividends"))
        {
            var errMsg = $"Unexpected return message header \r\nActual header: {csvs[0]}";
            logger.LogError($"url: {url}");
            logger.LogError(errMsg);
            throw new InvalidDataException(errMsg);
        }
        
        //Date,Dividends
        //2010-09-24 00:00:00-04:00,0.558 2010-12-27 00:00:00-05:00,0.526 2011-03-25 00:00:00-04:00,0.536 2011-06-24
        var rtv = new List<StockDividend>();

        foreach (var c in csvs.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(c)) { continue; }
            var cs = c.Split(',');
            
            if (string.IsNullOrWhiteSpace(cs[1])) { continue; }
            if (cs[1] == "null") { continue; }
            
            var dividend = new StockDividend
            {
                StockId = stock.GetStockId(),
                //The API Date is probably exDate or ExDiv date
                ExDate = DateOnly.Parse(cs[0].Split(" ")[0])
            };
            dividend.DividendEvent = dividend.AnnounceDate.ToString("MM");
            dividend.DistributionType = StockDividend.DIST_TYPE_CASH_SCRIP;
            dividend.Amount = Convert.ToDecimal(cs[1]);
            dividend.Currency = CurrencyExchangeRate.USD;
            dividend.DividendType = "D";
            dividend.AnnounceDate = dividend.ExDate;
            dividend.PayableDate = dividend.ExDate;
            rtv.Add(dividend);
        }
        
        logger.LogDebug($"Crawled {rtv.Count} {yahooStockId} dividends");

        return rtv;
    }
}