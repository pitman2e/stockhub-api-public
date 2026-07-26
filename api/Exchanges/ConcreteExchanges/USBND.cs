using System;

namespace StockHub.Exchanges.ConcreteExchanges;

public class USBND : BaseExchange
{
    public const string MARKET_ID = "USBND";
    public override string MarketId { get; } = MARKET_ID;
    public override int TimeOffset { get; } = -5;
    public override int MarketCloseHour { get; } = 17;
    public override bool IsPriceFinalised(DateTimeOffset nowDate, DateOnly priceMarketDate)
    {
        return true;
    }
}