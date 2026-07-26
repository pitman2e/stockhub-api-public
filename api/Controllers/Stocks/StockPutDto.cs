using System;
using StockHub.Extensions;

namespace StockHub.Controllers.Stocks;

public record StockPutDto : StockBaseDto
{
    public string key_stockId { get; init; }
    
    public string stockId => key_stockId; 

    public uint version { get; init; }
}