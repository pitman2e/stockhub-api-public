using System;
using System.Text.Json.Serialization;
using StockHub.Models.CustomJsonConverter;

namespace StockHub.Controllers.Transaction;

public record TransactionPutDto : TransactionModifyDto
{
    public uint version { get; init; }
};