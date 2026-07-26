
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StockHub.Database;
using StockHub.Models;

namespace StockHub.Crawlers.Price;

public class PcpPriceCrawler(
    HttpClient httpClient,
    ILogger<PcpPriceCrawler> logger
    ) : IPcpPriceCrawler
{
    public async Task<List<StockPrice>> Crawl(
        StockAdapter stockAdapter,
        DateOnly dateFrom,
        DateOnly dateTo)
    {
        var rtv = new List<StockPrice>();

        if (stockAdapter.GetStockId() != "CORE.PCP")
        {
            throw new InvalidDataException($"PCP Crawler only support CORE.PCP");
        }

        var url = $"https://www.principal.com.hk/api/mpf-funds";

        JsonNode json = null;
        using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url)) 
        {
            var response = await httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                return rtv;
            };
            
            json = await response.Content.ReadFromJsonAsync<JsonNode>();
        }

        if (json == null) return rtv;
        if (json.AsArray().Count == 0)
        {
            logger.LogWarning($"No data found for {stockAdapter.GetStockId()}");
        }
        
        foreach(var itm in json.AsArray())
        {
            if (
                itm["name"]?.ToString() == "Principal Core Accumulation Fund" &&
                itm["Scheme"]?.ToString() == "Principal MPF Scheme Series 800")
            {
                var price = new StockPrice
                {
                    StockId = stockAdapter.GetStockId(),
                    MarketDate = DateOnly.ParseExact(itm["Fund-price-date"]?.ToString() ?? "", "dd-MM-yyyy", CultureInfo.InvariantCulture),
                    OpenPrice = Convert.ToDecimal(itm["NAV"]?.ToString()),
                    DayHigh = Convert.ToDecimal(itm["NAV"]?.ToString()),
                    DayLow = Convert.ToDecimal(itm["NAV"]?.ToString()),
                    ClosePrice = Convert.ToDecimal(itm["NAV"]?.ToString()),
                    ClosePriceAdj = Convert.ToDecimal(itm["NAV"]?.ToString()),
                    Volume = 0,
                    IsFinalised = true
                };

                if (price.MarketDate >= dateFrom && price.MarketDate <= dateTo)
                {
                    rtv.Add(price);
                }
            }
        }

        return rtv;
    }
}