using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Models;

namespace StockHub.Crawlers.Price;

public class HsbcPriceCrawler(
    HttpClient httpClient,
    ILogger<HsbcPriceCrawler> logger
    ) : IHsbcPriceCrawler
{
    public async Task<List<StockPrice>> Crawl(
        StockAdapter stockAdapter,
        DateOnly dateFrom,
        DateOnly dateTo)
    {
        if (!stockAdapter.GetStockId().EndsWith("P.HSBC"))
        {
            throw new ArgumentException($"{stockAdapter.GetStockId()} not supported. Only stockId ends with 'P.HSBC' is supported.");
        }

        var stockIdApi = stockAdapter.GetStockId().Replace("P.HSBC", "");
        if (!stockIdApi.EndsWith("F") || stockIdApi.Length > 4)
        {
            logger.LogError("Official HSBC MPF fund code ends with 'F' and len<=4 (By observation), code modification needed (StockId {GetStockId})", stockAdapter.GetStockId());
        }
        var rtv = new List<StockPrice>();

        //This API only support 30 days, so need to divide the date range
        for (var lpDate = dateFrom; lpDate <= dateTo; lpDate = lpDate.AddDays(31))
        {
            HsbsMpfData json = null;
            var errCount = 0;
            var isSuccess = false;
            const int errCountLimit = 5;

            var nowDateO8 = DateTimeOffset.Now.ToOffset(8).ToUtcThenDateOnly();
            var apiFmDate = dateFrom;
            var apiToDate = dateTo > lpDate.AddDays(31) ? lpDate.AddDays(31) : dateTo; //Math.min(lpDate, dateTo)
            apiToDate = nowDateO8 > apiToDate ? apiToDate : nowDateO8; //Math.min(lpDate, nowDate)

            if ((nowDateO8.ToDateTime(TimeOnly.MinValue) - apiToDate.ToDateTime(TimeOnly.MinValue)).TotalDays <= 1)
            {
                //Cannot extract nearest 1 day
                apiToDate = apiToDate.AddDays((nowDateO8.ToDateTime(TimeOnly.MinValue) - apiToDate.ToDateTime(TimeOnly.MinValue)).Days - 1);
            }

            do
            {
                while (apiToDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    //Cannot extract Sunday/Saturday
                    apiToDate = apiToDate.AddDays(-1);
                }

                if (apiFmDate >= apiToDate)
                {
                    break; //From date must be larger than To Date
                }

                //https://rbwm-api.hsbc.com.hk/wpb-gpbw-mmw-hk-hbap-pa-p-wpp-mpf-market-data-prod-proxy/v1/funds?schemeCodes=HB&includes=fundPrice&fundPricePeriodFrom=2026-02-05&fundPricePeriodTo=2026-02-05
                var url = $"https://rbwm-api.hsbc.com.hk/wpb-gpbw-mmw-hk-hbap-pa-p-wpp-mpf-market-data-prod-proxy/v1/funds" +
                               $"?schemeCodes=HB" +
                               $"&includes=fundPrice" +
                               $"&fundPricePeriodFrom={apiFmDate.ToString("yyyy-MM-dd")}" +
                               $"&fundPricePeriodTo={apiToDate.ToString("yyyy-MM-dd")}";
                
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    var response = await httpClient.SendAsync(requestMessage);
                    isSuccess = response.IsSuccessStatusCode;
                    if (isSuccess)
                    {
                        json = await response.Content.ReadFromJsonAsync<HsbsMpfData>();
                        break;
                    };
                }

                errCount++;
                apiToDate = apiToDate.AddDays(-1);
            } while (errCount <= errCountLimit);

            if (!isSuccess || json is null)
            {
                break;
            }

            var schemeInfos = json.Data.SchemeInfos;
            
            foreach (var schemeInfo in schemeInfos)
            {
                foreach (var fpi in schemeInfo.FundPriceInfos)
                {
                    if (fpi.FundCode != stockIdApi)
                    {
                        continue;
                    }

                    foreach (var fundPrice in fpi.FundPrices)
                    {
                        var price = new StockPrice
                        {
                            StockId = stockAdapter.GetStockId(),
                            MarketDate = fundPrice.PriceDate.ToDateOnly(),
                            OpenPrice = Convert.ToDecimal(fundPrice.FundBuyPrice.Amount),
                            DayHigh = Convert.ToDecimal(fundPrice.FundBuyPrice.Amount),
                            DayLow = Convert.ToDecimal(fundPrice.FundBuyPrice.Amount),
                            ClosePrice = Convert.ToDecimal(fundPrice.FundBuyPrice.Amount),
                            ClosePriceAdj = Convert.ToDecimal(fundPrice.FundBuyPrice.Amount),
                            Volume = 0,
                            IsFinalised = true
                        };

                        if (price.MarketDate >= dateFrom && price.MarketDate <= dateTo)
                        {
                            rtv.Add(price);
                        }
                    }
                }
            }
        }

        return rtv;
    }
}