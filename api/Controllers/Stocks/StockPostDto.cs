using System;
using StockHub.Extensions;

namespace StockHub.Controllers.Stocks;

public record StockPostDto : StockBaseDto
{
    public string stockId { get; init; }
}