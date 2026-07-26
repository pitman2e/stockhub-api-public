using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Models;
using Yfinance;

namespace StockHub.Crawlers.Price;

public class YfinancePriceCrawler(    
    YFinanceService.YFinanceServiceClient? yFinanceGRPC = null
) : IYfinancePriceCrawler
{
    public async Task<List<StockPrice>> Crawl(
        StockAdapter stockAdapter,
        DateOnly dateFrom,
        DateOnly dateTo)
    {
        var dateFromActual = dateFrom; // According to API dev, start_date is inclusively
        var dateToActual = dateTo.AddDays(1); // According to API dev, end_date is exclusively
        var yahooStockId = YahooStockIdMapper.Map(stockAdapter);
        var curDateTime = DateTimeOffset.Now;

        if (yFinanceGRPC == null) throw new SHArgumentException("STOCKHUB_YFINANCE_GRPC is not set");

        var csv = (await yFinanceGRPC.GetHistoryAsync(new HistoryRequest
        {
            Ticker = yahooStockId,
            StartDate = dateFromActual.ToString("yyyy-MM-dd"),
            EndDate = dateToActual.ToString("yyyy-MM-dd"),

        })).CsvData;
        var parsedStockPrice = ParseCsv(csv, stockAdapter, curDateTime);
        var filteredParsedStockPrice = FilterInRangeAndDuplicate(parsedStockPrice, dateFrom, dateTo);
        
        return filteredParsedStockPrice;
    }

    public static List<StockPrice> FilterInRangeAndDuplicate(List<StockPrice> parsedStockPrice, DateOnly dateFrom, DateOnly dateTo)
    {
        //Sometimes the API returns duplicate record, therefore need to check with previous record
        StockPrice prevPrice = null;
        var rtv = new List<StockPrice>();
        foreach (var price in parsedStockPrice) 
        {
            if (price.MarketDate >= dateFrom && 
                price.MarketDate <= dateTo &&
                prevPrice?.MarketDate != price.MarketDate)
            {
                rtv.Add(price);
                prevPrice = price;
            }
        }

        return rtv;
    }

    
    public static List<StockPrice> ParseCsv(string csv, StockAdapter stockAdapter, DateTimeOffset curDateTime)
    {
        var dictCsvColMapping = new Dictionary<string, int>();
        foreach (var k in new []{ "Date", "Open", "High", "Low", "Close", "Volume", "Adj Close" })
        {
            dictCsvColMapping[k] = -1;
        }
        
        var csvs = csv.Split('\r', '\n');
        if (csvs.Length == 0)
        {
            throw new InvalidDataException($"csv returned is empty");
        }
        
        var csvHeader = csvs[0];

        var colIdx = -1;
        foreach(var col in csvHeader.Split(','))
        {
            colIdx++;
            if (dictCsvColMapping.ContainsKey(col))
            {
                dictCsvColMapping[col] = colIdx;
            }
        }
        
        if (dictCsvColMapping["Date"] == -1 || dictCsvColMapping["Close"] == -1)
        {
            throw new InvalidDataException($"Header does not contains at min 'Date' or 'Close' column, header is {csvHeader}");
        }
        
        //Date,Open,High,Low,Close,Volume,Dividends,Stock Splits,Capital Gains
        //2026-04-10 00:00:00-04:00,626.3300170898438,626.989990234375,623.719970703125,624.5999755859375,4609400,0.0,0.0,0.0 
        var rtv = new List<StockPrice>();
        
        foreach (var c in csvs.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(c)) { continue; }
            var cs = c.Split(',');
            
            if (string.IsNullOrWhiteSpace(cs[1])) { continue; }
            if (cs[1] == "null") { continue; }

            var price = new StockPrice
            {
                StockId = stockAdapter.GetStockId()
            };
            if (dictCsvColMapping["Date"] >= 0) price.MarketDate = DateOnly.Parse(cs[dictCsvColMapping["Date"]].Split(" ")[0]);
            if (dictCsvColMapping["Open"] >= 0) price.OpenPrice = Convert.ToDecimal(cs[dictCsvColMapping["Open"]]);
            if (dictCsvColMapping["High"] >= 0) price.DayHigh = Convert.ToDecimal(cs[dictCsvColMapping["High"]]);
            if (dictCsvColMapping["Close"] >= 0) price.ClosePrice = Convert.ToDecimal(cs[dictCsvColMapping["Close"]]);
            if (dictCsvColMapping["Volume"] >= 0) price.Volume = Convert.ToDecimal(cs[dictCsvColMapping["Volume"]]);
            if (dictCsvColMapping["Adj Close"] >= 0) price.ClosePriceAdj = Convert.ToDecimal(cs[dictCsvColMapping["Volume"]]);
            price.IsFinalised = stockAdapter.Exchange?.IsPriceFinalised(curDateTime, price.MarketDate) ?? true;
            
            rtv.Add(price);
        }

        return rtv;
    }

}