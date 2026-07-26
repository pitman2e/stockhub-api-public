using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StockHub.Database;
using StockHub.Models;

namespace StockHub.Crawlers.Dividend;

public class NasdaqDividendCrawler(
    HttpClient httpClient,
    ILogger<NasdaqDividendCrawler> logger)
    : IDividendCrawler
{
    private readonly ILogger<NasdaqDividendCrawler> _logger = logger;

    public async Task<IEnumerable<StockDividend>> CrawlAsync(StockAdapter stock)
    {
        //https://www.nasdaq.com/search_api_autocomplete/symbols_autocomplete?q=VEA
        var url = $"https://api.nasdaq.com/api/autocomplete/slookup/10?search={stock.ToNasdaqStockId()}";

        async Task<string> GetNasdaqAssetClass(StockAdapter stock)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url) { Version = new Version(2, 0),  })
            {
                requestMessage.Headers.Add("Host", "api.nasdaq.com");
                requestMessage.Headers.Add("User-Agent", Config.WebBrowserUserAgent);
                requestMessage.Headers.Add("Accept", "application/json, text/plain, */*");
                requestMessage.Headers.Add("Accept-Language", "en-US,en;q=0.8");
                requestMessage.Headers.Add("Accept-Encoding", "gzip, deflate");
                requestMessage.Headers.Add("Referer", "https://www.nasdaq.com/");

                var responseAutocomplete = await httpClient.SendAsync(requestMessage);
                responseAutocomplete.EnsureSuccessStatusCode();
                var jsonAutocomplete = await responseAutocomplete.Content.ReadAsStringAsync();
                string assetClass = ((dynamic)JsonConvert.DeserializeObject(jsonAutocomplete)).data[0].asset;
                return assetClass;
            }
        }

        string assetClass = await GetNasdaqAssetClass(stock);
        dynamic dividends;

        using (var requestMessage = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.nasdaq.com/api/quote/{stock.ToNasdaqStockId()}/dividends?assetclass={assetClass}")
        { Version = new Version(2, 0) })
        {
            requestMessage.Headers.Add("Host", "api.nasdaq.com");
            requestMessage.Headers.Add("User-Agent", Config.WebBrowserUserAgent);
            requestMessage.Headers.Add("Accept", "*/*");
            requestMessage.Headers.Add("Accept-Language", "en-US,en;q=0.8");
            requestMessage.Headers.Add("Accept-Encoding", "gzip, deflate");
            requestMessage.Headers.Add("Referer", "https://www.nasdaq.com/");

            //https://api.nasdaq.com/api/quote/VEA/dividends?assetclass=etf
            var responseDividends = await httpClient.SendAsync(requestMessage);
            responseDividends.EnsureSuccessStatusCode();
            var jsonDividends = await responseDividends.Content.ReadAsStringAsync();
            dividends = ((dynamic)JsonConvert.DeserializeObject(jsonDividends))?.data.dividends.rows;
        }

        var rtv = new List<StockDividend>();

        const string dateFormat = "MM/dd/yyyy";
        var provider = CultureInfo.InvariantCulture;

        if (!dividends.HasValues)
        {
            return rtv; //Empty set
        }

        foreach (dynamic div in dividends)
        {
            var dividend = new StockDividend
            {
                StockId = stock.GetStockId()
            };
            var isSuccess = true;

            isSuccess &= DateOnly.TryParseExact(div.exOrEffDate.Value, dateFormat, provider, DateTimeStyles.None, out DateOnly exDate);
            isSuccess &= DateOnly.TryParseExact(div.paymentDate.Value, dateFormat, provider, DateTimeStyles.None, out DateOnly paymentDate);
            isSuccess &= !string.IsNullOrWhiteSpace(div.amount.Value);

            if (!DateOnly.TryParseExact(div.declarationDate.Value, dateFormat, provider, DateTimeStyles.None, out DateOnly announceDate))
            {
                announceDate = exDate;
            }

            if (!isSuccess)
            {
                continue;
            };

            dividend.AnnounceDate = announceDate;
            dividend.DividendEvent = dividend.AnnounceDate.ToString("MM");
            dividend.DistributionType = StockDividend.DIST_TYPE_CASH_SCRIP; //3 Types: 'Cash/Scrip'/'Cash'/'Scrip'
            dividend.Amount = Convert.ToDecimal(((string)div.amount.Value).Replace("$", ""));
            dividend.Currency = CurrencyExchangeRate.USD;
            dividend.DividendType = "D";
            dividend.ExDate = exDate;
            dividend.PayableDate = paymentDate;
            rtv.Add(dividend);
        }

        return rtv;
    }
}