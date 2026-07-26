using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using StockHub.Crawlers.Price;
using StockHub.Models;

namespace StockHub.Exchanges;

public interface IExchange
{
    public string MarketId { get; }
    public int TimeOffset { get; }
    public int MarketCloseHour { get; }
    public bool TryParseExact(string stockId, [NotNullWhen(true)] out StockAdapter? stockAdapter);
    public bool IsPriceFinalised(DateTimeOffset nowDate, DateOnly priceMarketDate);
    public string GetStockId(string stockRawId);
}