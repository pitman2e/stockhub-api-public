using System;

namespace StockHub.Services.Position;

public partial class PositionValueService
{
    private class StockPriceDiv()
    {
        public DateOnly MarketDate { get; init; }
        public decimal ClosePrice { get; init; }
        public decimal? DivExAmount { get; init; }
    }
}