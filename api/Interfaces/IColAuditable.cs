using System;

namespace StockHub.Interfaces;

public interface IColAuditable
{
    DateTimeOffset? CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? Sta { get; set; }
}